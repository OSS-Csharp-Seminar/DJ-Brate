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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DJBrate.Application.Models.Spotify;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMoodSessionService, MoodSessionService>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();
builder.Services.AddScoped<ISpotifyTokenService, SpotifyTokenService>();
builder.Services.AddScoped<ISpotifyDataSyncService, SpotifyDataSyncService>();
builder.Services.AddScoped<ISpotifyApiClient, SpotifyApiClient>();
builder.Services.AddScoped<IAiClient, GeminiClient>();
builder.Services.AddScoped<IAiMoodService, AiMoodService>();
builder.Services.AddScoped<McpDispatcher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.AiModelConfigs.Any())
    {
        db.AiModelConfigs.Add(new AiModelConfig
        {
            ConfigName   = "default",
            ModelName    = "gemini-2.5-flash",
            Temperature  = 0.8f,
            MaxTokens    = 2048,
            IsActive     = true,
            SystemPrompt = """
                You are DJ Brate, an AI DJ that creates personalized Spotify playlists based on the user's mood.

                Your job:
                1. Read the user's mood prompt carefully
                2. Use the available tools to understand their music taste (call get_user_top_tracks and/or get_user_top_artists)
                3. Based on their taste + mood, call get_recommendations with appropriate seed artists/tracks and audio feature targets
                4. After getting recommendations, respond with a final JSON result

                Audio feature guidelines:
                - valence: 0.0 = sad/angry, 1.0 = happy/cheerful
                - energy: 0.0 = calm/relaxed, 1.0 = intense/energetic
                - tempo: 60-80 = slow, 100-120 = moderate, 130-160 = fast
                - danceability: 0.0 = not danceable, 1.0 = very danceable
                - acousticness: 0.0 = electronic/produced, 1.0 = acoustic/unplugged

                When you have the final track list, respond with ONLY this JSON (no markdown, no extra text):
                {
                    "playlist_name": "a creative playlist name based on the mood",
                    "playlist_description": "a short description of the playlist vibe",
                    "detected_mood": "the mood you detected",
                    "reasoning": "brief explanation of your choices",
                    "audio_features": {
                        "valence": 0.0-1.0,
                        "energy": 0.0-1.0,
                        "tempo": 60-180,
                        "danceability": 0.0-1.0,
                        "acousticness": 0.0-1.0
                    }
                }

                Important: You MUST call at least get_user_top_artists or get_user_top_tracks first, then call get_recommendations. Do not skip tool calls.
                """
        });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/auth/spotify/login", (HttpContext ctx, IConfiguration config) =>
{
    var state = Guid.NewGuid().ToString("N");
    ctx.Response.Cookies.Append(SpotifyConstants.OAuthStateCookie, state, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        MaxAge   = TimeSpan.FromMinutes(SpotifyConstants.OAuthStateCookieMaxAgeMinutes)
    });

    var clientId    = config["Spotify:ClientId"];
    var redirectUri = Uri.EscapeDataString(config["Spotify:RedirectUri"]!);
    var scopes      = Uri.EscapeDataString(SpotifyConstants.Scopes);

    var url = $"{SpotifyConstants.AuthorizeUrl}" +
              $"?client_id={clientId}" +
              $"&response_type=code" +
              $"&redirect_uri={redirectUri}" +
              $"&scope={scopes}" +
              $"&state={state}";

    return Results.Redirect(url);
});

app.MapGet("/auth/spotify/callback", async (
    HttpContext ctx,
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

    var savedState = ctx.Request.Cookies[SpotifyConstants.OAuthStateCookie];
    if (string.IsNullOrEmpty(savedState) || savedState != state)
        return Results.Redirect("/login?spotifyError=invalid_state");

    ctx.Response.Cookies.Delete(SpotifyConstants.OAuthStateCookie);

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
