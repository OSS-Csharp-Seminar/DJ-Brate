# DJ Brate - Implementation Plan

## Context

The foundation is in place: Clean Architecture, all 11 DB entities, EF Core migrations, local auth, basic Blazor pages. The app needs all functional features implemented on top of this foundation. The goal is a working end-to-end flow: Spotify OAuth login → mood input → AI (Gemini) interprets mood via MCP → Spotify recommendations → save playlist → browse playlists → view stats.

**Key decisions:** Google Gemini (free tier) for AI, Bootstrap for styling, full MCP server in C#.

---

## Phase 1 — Spotify OAuth 2.0 Integration

**Goal:** Let users connect their Spotify account.

### 1.1 Add Spotify OAuth handler
- File: `DJBrate.Web/Program.cs`
- Add `Microsoft.AspNetCore.Authentication.OAuth` NuGet package
- Configure Spotify OAuth scheme alongside existing cookie auth
- Callback endpoint: `/auth/spotify/callback`
- Store Spotify tokens in `User.AccessToken`, `RefreshToken`, `TokenExpiresAt`

### 1.2 Create Spotify auth controller/endpoints
- File: `DJBrate.API/Controllers/SpotifyAuthController.cs` (or minimal API endpoints in Program.cs)
- `GET /auth/spotify/login` → redirect to Spotify authorization URL
- `GET /auth/spotify/callback` → exchange code for tokens, call `UserService.CreateOrUpdateUserAsync`, sign in cookie

### 1.3 Update Login page
- File: `DJBrate.Web/Components/Pages/Login.razor`
- Add "Connect with Spotify" button that redirects to `/auth/spotify/login`
- Style: Bootstrap green button with Spotify logo

### 1.4 Add token refresh logic
- File: `DJBrate.Application/Services/SpotifyTokenService.cs`
- `EnsureValidTokenAsync(User)` → check `TokenExpiresAt`, refresh via Spotify `/api/token` if expired
- Register as scoped service

---

## Phase 2 — Spotify API Client

**Goal:** A typed C# client that wraps the Spotify Web API.

### 2.1 Create SpotifyApiClient
- File: `DJBrate.Infrastructure/Spotify/SpotifyApiClient.cs`
- Interface: `DJBrate.Application/Interfaces/ISpotifyApiClient.cs`
- Use `HttpClient` (registered as named/typed client in DI)
- Methods:
  - `GetTopTracksAsync(accessToken, timeRange)` → `List<SpotifyTrack>`
  - `GetTopArtistsAsync(accessToken, timeRange)` → `List<SpotifyArtist>`
  - `GetRecommendationsAsync(accessToken, seedArtists, seedTracks, audioFeatures)` → `List<SpotifyTrack>`
  - `CreatePlaylistAsync(accessToken, userId, name, description)` → `string` (playlistId)
  - `AddTracksToPlaylistAsync(accessToken, playlistId, trackUris)` → `bool`
  - `GetCurrentUserProfileAsync(accessToken)` → Spotify user object

### 2.2 Add Spotify data sync service
- File: `DJBrate.Application/Services/SpotifyDataSyncService.cs`
- `SyncUserTopDataAsync(userId)` → pull top tracks + artists for all 3 time ranges, upsert into `UserTopTracks` / `UserTopArtists` tables
- Called after first login and periodically via Hangfire

### 2.3 Register in DI
- File: `DJBrate.Web/Program.cs`
- Add `HttpClient` for Spotify base URL `https://api.spotify.com/v1`
- Register `ISpotifyApiClient → SpotifyApiClient`
- Register `SpotifyDataSyncService`

---

## Phase 3 — MCP Server (Spotify Tools for AI)

**Goal:** AI can call Spotify tools dynamically during playlist generation reasoning.

### 3.1 Create MCP server project or embedded server
- Options: standalone `DJBrate.MCP` project or embedded in `DJBrate.Application`
- Recommended: embed MCP tool definitions in Application layer, expose via minimal API endpoint `POST /mcp/invoke`

### 3.2 Define MCP tools
- File: `DJBrate.Application/Mcp/SpotifyMcpTools.cs`
- Tool: `get_user_top_tracks` — returns user's top tracks (seeded from DB, no live call needed)
- Tool: `get_user_top_artists` — returns user's top artists from DB
- Tool: `get_recommendations` — calls `ISpotifyApiClient.GetRecommendationsAsync` with provided parameters
- Tool: `search_tracks` — calls Spotify search API with query
- Each tool logs to `McpToolCall` entity

### 3.3 MCP dispatcher
- File: `DJBrate.Application/Mcp/McpDispatcher.cs`
- Takes `toolName` + `inputParameters` (JSON), dispatches to correct tool, returns result JSON
- Logs execution to `McpToolCall` table via repository

### 3.4 MCP API endpoint
- File: `DJBrate.Web/Program.cs` or `DJBrate.API`
- `POST /mcp/invoke` — receives `{sessionId, toolName, inputParameters}`, returns tool result
- This endpoint is called by the AI integration layer (not directly by the browser)

---

## Phase 4 — AI Service (Google Gemini)

**Goal:** Interpret user mood prompt → map to audio features → orchestrate MCP tool calls → return playlist track list.

### 4.1 Add Gemini client
- NuGet: `Google.AI.Generative` or use raw `HttpClient` to `https://generativelanguage.googleapis.com`
- File: `DJBrate.Infrastructure/Ai/GeminiClient.cs`
- Interface: `DJBrate.Application/Interfaces/IAiClient.cs`
- Method: `SendMessageAsync(systemPrompt, messages, tools)` → AI response with optional tool calls

### 4.2 Create AI Mood Service
- File: `DJBrate.Application/Services/AiMoodService.cs`
- Interface: `DJBrate.Application/Interfaces/IAiMoodService.cs`
- `GeneratePlaylistAsync(session, user)`:
  1. Build system prompt with user's top tracks/artists context
  2. Send user prompt to Gemini with MCP tool definitions
  3. Handle tool call loop: AI requests tool → call McpDispatcher → feed result back
  4. Parse final AI response → extract `AiMoodMapping` (valence, energy, tempo, etc.)
  5. Get Spotify recommendations using the mapped audio features
  6. Return ordered `List<PlaylistTrack>`
  7. Persist `AiConversationMessage` records for full conversation history

### 4.3 System prompt design
- Instructs Gemini to: analyze mood → use tools to get user taste → call `get_recommendations` with refined parameters → return structured playlist
- Prompt stored/configurable via `AiModelConfig` entity (already in DB)

### 4.4 Config
- `appsettings.json`: add `Gemini:ApiKey` and `Gemini:ModelName` (e.g. `gemini-2.0-flash`)
- Read via `IConfiguration` in `GeminiClient`

---

## Phase 5 — Core Playlist Generation Flow

**Goal:** Wire everything together into the Home page "Generate Playlist" action.

### 5.1 Extend MoodSessionService
- File: `DJBrate.Application/Services/MoodSessionService.cs`
- Add `StartGenerationAsync(session, user)`:
  1. Set session status → `creating`
  2. Call `SpotifyDataSyncService.SyncUserTopDataAsync` (if data stale)
  3. Call `AiMoodService.GeneratePlaylistAsync`
  4. Create `Playlist` entity + `PlaylistTrack` list
  5. Call `SpotifyApiClient.CreatePlaylistAsync` → get Spotify playlist ID
  6. Call `SpotifyApiClient.AddTracksToPlaylistAsync`
  7. Save all to DB
  8. Set session status → `completed`
  9. On error → set status → `failed`

### 5.2 SignalR hub for live updates
- File: `DJBrate.Web/Hubs/PlaylistHub.cs`
- Hub method: `JoinSession(sessionId)` → user joins group
- Server sends: `PlaylistUpdate(status, tracks[])` as generation progresses
- Register in `Program.cs`

### 5.3 Home page wiring
- File: `DJBrate.Web/Components/Pages/Home.razor`
- Full implementation:
  - Mood text area (free text)
  - Mood tag chips (happy, sad, energetic, focused, chill, romantic)
  - Genre multi-select
  - Energy + Danceability sliders
  - "Generate Playlist" button → calls `MoodSessionService.StartGenerationAsync`
  - SignalR connection to show live progress (spinner → track list appearing)
  - Display generated tracks inline (track name, artist, 30s preview player)

---

## Phase 6 — Playlists Page

### 6.1 Create page
- File: `DJBrate.Web/Components/Pages/Playlists.razor` (`@page "/playlists"`)
- Shows all user's generated playlists as Bootstrap cards
- Each card: playlist name, track count, creation date, mood tag
- Click → expand inline OR navigate to playlist detail

### 6.2 Playlist detail
- File: `DJBrate.Web/Components/Pages/PlaylistDetail.razor` (`@page "/playlists/{id}"`)
- Full track list with 30s preview player (HTML5 `<audio>` element)
- Like/Skip buttons per track → saves `TrackFeedback`
- "Open in Spotify" deep link button per track

### 6.3 Track feedback service
- File: `DJBrate.Application/Services/TrackFeedbackService.cs`
- `SaveFeedbackAsync(userId, playlistTrackId, feedbackType)` → upsert to `TrackFeedbacks`

---

## Phase 7 — Statistics Page

### 7.1 Create page
- File: `DJBrate.Web/Components/Pages/Statistics.razor` (`@page "/statistics"`)
- Uses `ListeningStats` table data
- Sections:
  - Mood history chart (last 30 days, simple Bootstrap progress bars or inline SVG)
  - Top genres
  - Energy/Valence trends
  - Total playlists generated, tracks liked vs skipped

### 7.2 Stats calculation service
- File: `DJBrate.Application/Services/ListeningStatsService.cs`
- `CalculateDailyStatsAsync(userId, date)` → aggregate mood sessions + feedback into `ListeningStat`
- Called by Hangfire daily job

### 7.3 Hangfire setup
- File: `DJBrate.Web/Program.cs`
- Activate Hangfire dashboard at `/hangfire` (admin only)
- Register recurring job: `ListeningStatsService.CalculateDailyStatsAsync` runs nightly

---

## Phase 8 — Admin Panel

### 8.1 Complete Admin page
- File: `DJBrate.Web/Components/Pages/Admin.razor`
- Users table: list all users, show Spotify connection status
- AI Config management: view/edit `AiModelConfig` rows (system prompt, temperature, model name)
- Playlists overview: recent playlists across all users

---

## Phase 9 — Polish & Configuration

### 9.1 appsettings configuration
- `Spotify:ClientId`, `Spotify:ClientSecret`, `Spotify:RedirectUri`
- `Gemini:ApiKey`, `Gemini:ModelName`
- `Hangfire:DashboardPath`

### 9.2 Docker Compose update
- Ensure `docker-compose.yml` passes all env vars
- Verify PostgreSQL container, app container wiring

### 9.3 Error handling & UX
- Global error boundary in `Routes.razor`
- Toast notifications for success/failure states

---

## Critical Files to Modify / Create

| File | Action |
|---|---|
| `DJBrate.Web/Program.cs` | Add Spotify OAuth, Gemini DI, Hangfire, SignalR, HttpClients |
| `DJBrate.Web/appsettings.json` | Add Spotify + Gemini config keys |
| `DJBrate.Application/Services/` | Add 5 new services |
| `DJBrate.Application/Interfaces/` | Add 5 new interfaces |
| `DJBrate.Infrastructure/Spotify/SpotifyApiClient.cs` | New - Spotify HTTP client |
| `DJBrate.Infrastructure/Ai/GeminiClient.cs` | New - Gemini HTTP client |
| `DJBrate.Application/Mcp/` | New folder - McpDispatcher + SpotifyMcpTools |
| `DJBrate.Web/Components/Pages/Home.razor` | Full implementation |
| `DJBrate.Web/Components/Pages/Playlists.razor` | New page |
| `DJBrate.Web/Components/Pages/PlaylistDetail.razor` | New page |
| `DJBrate.Web/Components/Pages/Statistics.razor` | New page |
| `DJBrate.Web/Hubs/PlaylistHub.cs` | New - SignalR hub |
| `DJBrate.Web/Components/Pages/Login.razor` | Add Spotify button |
| `DJBrate.Web/Components/Pages/Admin.razor` | Complete implementation |

---

## Implementation Order (Dependency Chain)

```
Phase 1 (Spotify Auth)
  └─► Phase 2 (Spotify API Client)
        └─► Phase 3 (MCP Server)
              └─► Phase 4 (AI/Gemini Service)
                    └─► Phase 5 (Generation Flow + Home Page)
                          └─► Phase 6 (Playlists Page)
                                └─► Phase 7 (Statistics Page)
                                      └─► Phase 8 (Admin Panel)
                                            └─► Phase 9 (Polish)
```

---

## Verification

Everything runs via Docker. After each phase:
1. `docker compose up --build` — rebuild and start all containers
2. Phase 1: test Spotify login flow end-to-end in browser
3. Phase 2: check `user_top_tracks` and `user_top_artists` tables populated after login
4. Phase 3: test `/mcp/invoke` endpoint via HTTP tool
5. Phase 4: test AI returns valid mood mapping + track list for a sample prompt
6. Phase 5: generate a full playlist from Home page, verify it appears in Spotify app
7. Phase 6-7: verify playlists and stats display correct data
