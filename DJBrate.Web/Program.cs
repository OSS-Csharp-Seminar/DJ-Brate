using System.Security.Claims;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Services;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using DJBrate.Infrastructure.Repositories;
using DJBrate.Infrastructure.Spotify;
using DJBrate.Infrastructure.Ai;
using DJBrate.Application.Mcp;
using DJBrate.Web.Components;
using DJBrate.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DJBrate.Application.Models.Spotify;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var dpKeysPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrEmpty(dpKeysPath))
{
    Directory.CreateDirectory(dpKeysPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("DJBrate")
        .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));
}

builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan   = TimeSpan.FromDays(SpotifyConstants.CookieExpiryDays);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMoodSessionRepository, MoodSessionRepository>();
builder.Services.AddScoped<IPlaylistRepository, PlaylistRepository>();
builder.Services.AddScoped<IUserTopTrackRepository, UserTopTrackRepository>();
builder.Services.AddScoped<IUserTopArtistRepository, UserTopArtistRepository>();
builder.Services.AddScoped<IAiModelConfigRepository, AiModelConfigRepository>();
builder.Services.AddScoped<IMcpToolCallRepository, McpToolCallRepository>();
builder.Services.AddScoped<IAiMoodMappingRepository, AiMoodMappingRepository>();
builder.Services.AddScoped<IAiConversationMessageRepository, AiConversationMessageRepository>();
builder.Services.AddScoped<ITrackFeedbackRepository, TrackFeedbackRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMoodSessionService, MoodSessionService>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();
builder.Services.AddScoped<ISpotifyTokenService, SpotifyTokenService>();
builder.Services.AddScoped<ISpotifyDataSyncService, SpotifyDataSyncService>();
builder.Services.AddScoped<ISpotifyApiClient, SpotifyApiClient>();
builder.Services.AddScoped<IAiClient, GeminiClient>();
builder.Services.AddScoped<IAiMoodService, AiMoodService>();
builder.Services.AddScoped<McpDispatcher>();
builder.Services.AddScoped<PlaybackState>();
builder.Services.AddScoped<GenerationState>();
builder.Services.AddScoped<RefinementState>();
builder.Services.AddScoped<ITrackFeedbackService, TrackFeedbackService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.AiModelConfigs.Any(c => c.ConfigName == "default"))
    {
        db.AiModelConfigs.Add(new AiModelConfig
        {
            ConfigName   = "default",
            ModelName    = "gemini-2.5-flash",
            Temperature  = 1.0f,
            MaxTokens    = 3000,
            IsActive     = true,
            SystemPrompt = ""
        });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapStaticAssets();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

const string OAuthStateCacheKeyPrefix = "spotify_oauth_state:";
var oauthStateTtl = TimeSpan.FromMinutes(SpotifyConstants.OAuthStateTtlMinutes);

app.MapGet("/auth/spotify/login", (IMemoryCache cache, IConfiguration config) =>
{
    var state = Guid.NewGuid().ToString("N");
    cache.Set(OAuthStateCacheKeyPrefix + state, true, oauthStateTtl);

    var clientId    = config["Spotify:ClientId"];
    var redirectUri = Uri.EscapeDataString(config["Spotify:RedirectUri"]!);
    var scopes      = Uri.EscapeDataString(SpotifyConstants.Scopes);

    var url = $"{SpotifyConstants.AuthorizeUrl}" +
              $"?client_id={clientId}" +
              $"&response_type=code" +
              $"&redirect_uri={redirectUri}" +
              $"&scope={scopes}" +
              $"&state={state}" +
              $"&show_dialog=true";

    return Results.Redirect(url);
});

app.MapGet("/auth/spotify/callback", async (
    HttpContext ctx,
    IMemoryCache cache,
    IConfiguration config,
    IUserService userService,
    ISpotifyTokenService tokenService,
    ISpotifyApiClient spotifyClient,
    ISpotifyDataSyncService syncService) =>
{
    var code  = ctx.Request.Query["code"].ToString();
    var state = ctx.Request.Query["state"].ToString();
    var error = ctx.Request.Query["error"].ToString();

    if (!string.IsNullOrEmpty(error))
        return Results.Redirect("/login?spotifyError=access_denied");

    var stateKey = OAuthStateCacheKeyPrefix + state;
    if (string.IsNullOrEmpty(state) || !cache.TryGetValue(stateKey, out _))
        return Results.Redirect("/login?spotifyError=invalid_state");
    cache.Remove(stateKey);

    var tokens  = await tokenService.ExchangeCodeForTokensAsync(code, config["Spotify:RedirectUri"]!);
    var profile = await spotifyClient.GetProfileAsync(tokens.AccessToken);

    var spotifyId   = profile.Id;
    var displayName = profile.DisplayName ?? spotifyId;
    var email       = profile.Email ?? $"{spotifyId}{SpotifyConstants.PlaceholderEmailSuffix}";
    var avatarUrl   = profile.Images.FirstOrDefault()?.Url;

    var existingUser = await userService.GetUserBySpotifyIdAsync(spotifyId);
    var needsSync = existingUser is null
        || !existingUser.LastLoginAt.HasValue
        || existingUser.LastLoginAt < DateTime.UtcNow.AddHours(-SpotifyConstants.SyncIntervalHours);

    var user = await userService.CreateOrUpdateUserAsync(new User
    {
        SpotifyId           = spotifyId,
        DisplayName         = displayName,
        Email               = email,
        AvatarUrl           = avatarUrl,
        SpotifyAccessToken  = tokens.AccessToken,
        SpotifyRefreshToken = tokens.RefreshToken,
        TokenExpiresAt      = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn),
        Role                = SpotifyConstants.DefaultUserRole
    });

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name,           user.DisplayName),
        new(ClaimTypes.Email,          user.Email),
        new(ClaimTypes.Role,           user.Role)
    };
    var principal = new ClaimsPrincipal(
        new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    if (needsSync)
        await syncService.SyncUserTopDataAsync(user.Id);

    return Results.Redirect("/");
});

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
