using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Services;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using DJBrate.Infrastructure.Repositories;
using DJBrate.Infrastructure.Spotify;
using DJBrate.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMoodSessionRepository, MoodSessionRepository>();
builder.Services.AddScoped<IPlaylistRepository, PlaylistRepository>();
builder.Services.AddScoped<IUserTopTrackRepository, UserTopTrackRepository>();
builder.Services.AddScoped<IUserTopArtistRepository, UserTopArtistRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMoodSessionService, MoodSessionService>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();
builder.Services.AddScoped<ISpotifyTokenService, SpotifyTokenService>();
builder.Services.AddScoped<ISpotifyDataSyncService, SpotifyDataSyncService>();

builder.Services.AddHttpClient<ISpotifyApiClient, SpotifyApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.spotify.com/v1/");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
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
    ctx.Response.Cookies.Append("spotify_oauth_state", state, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        MaxAge = TimeSpan.FromMinutes(10)
    });

    var clientId    = config["Spotify:ClientId"];
    var redirectUri = Uri.EscapeDataString(config["Spotify:RedirectUri"]!);
    var scopes      = Uri.EscapeDataString(
        "user-read-private user-read-email user-top-read " +
        "playlist-modify-public playlist-modify-private");

    var url = $"https://accounts.spotify.com/authorize" +
              $"?client_id={clientId}" +
              $"&response_type=code" +
              $"&redirect_uri={redirectUri}" +
              $"&scope={scopes}" +
              $"&state={state}";

    return Results.Redirect(url);
});

app.MapGet("/auth/spotify/callback", async (HttpContext ctx, IConfiguration config, IUserService userService, ISpotifyDataSyncService syncService) =>
{
    var code  = ctx.Request.Query["code"].ToString();
    var state = ctx.Request.Query["state"].ToString();
    var error = ctx.Request.Query["error"].ToString();

    if (!string.IsNullOrEmpty(error))
        return Results.Redirect("/login?spotifyError=access_denied");

    var savedState = ctx.Request.Cookies["spotify_oauth_state"];
    if (string.IsNullOrEmpty(savedState) || savedState != state)
        return Results.Redirect("/login?spotifyError=invalid_state");

    ctx.Response.Cookies.Delete("spotify_oauth_state");

    var clientId     = config["Spotify:ClientId"]!;
    var clientSecret = config["Spotify:ClientSecret"]!;
    var redirectUri  = config["Spotify:RedirectUri"]!;
    var credentials  = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

    string accessToken, refreshToken;
    int expiresIn;
    using (var tokenHttp = new HttpClient())
    {
        tokenHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        var tokenResponse = await tokenHttp.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]   = "authorization_code",
                ["code"]         = code,
                ["redirect_uri"] = redirectUri
            }));

        if (!tokenResponse.IsSuccessStatusCode)
            return Results.Redirect($"/login?spotifyError=token_failed_{(int)tokenResponse.StatusCode}");

        using var tokenDoc = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync());
        accessToken  = tokenDoc.RootElement.GetProperty("access_token").GetString()!;
        refreshToken = tokenDoc.RootElement.GetProperty("refresh_token").GetString()!;
        expiresIn    = tokenDoc.RootElement.GetProperty("expires_in").GetInt32();
    }

    using var profileHttp = new HttpClient();
    profileHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var profileResponse = await profileHttp.GetAsync("https://api.spotify.com/v1/me");

    if (!profileResponse.IsSuccessStatusCode)
    {
        var errBody = await profileResponse.Content.ReadAsStringAsync();
        return Results.Redirect($"/login?spotifyError={Uri.EscapeDataString((int)profileResponse.StatusCode + ": " + errBody)}");
    }

    using var profileDoc = await JsonDocument.ParseAsync(await profileResponse.Content.ReadAsStreamAsync());
    var profile = profileDoc.RootElement;

    var spotifyId   = profile.GetProperty("id").GetString()!;
    var displayName = profile.TryGetProperty("display_name", out var dn) && dn.ValueKind != JsonValueKind.Null
        ? dn.GetString() ?? spotifyId
        : spotifyId;
    var email = profile.TryGetProperty("email", out var em) && em.ValueKind != JsonValueKind.Null
        ? em.GetString() ?? $"{spotifyId}@spotify.placeholder"
        : $"{spotifyId}@spotify.placeholder";
    string? avatarUrl = null;
    if (profile.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
        avatarUrl = images[0].GetProperty("url").GetString();

    var existingUser = await userService.GetUserBySpotifyIdAsync(spotifyId);
    var needsSync = existingUser is null
        || !existingUser.LastLoginAt.HasValue
        || existingUser.LastLoginAt < DateTime.UtcNow.AddHours(-24);

    var user = await userService.CreateOrUpdateUserAsync(new User
    {
        SpotifyId           = spotifyId,
        DisplayName         = displayName,
        Email               = email,
        AvatarUrl           = avatarUrl,
        SpotifyAccessToken  = accessToken,
        SpotifyRefreshToken = refreshToken,
        TokenExpiresAt      = DateTime.UtcNow.AddSeconds(expiresIn),
        Role                = "user"
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
