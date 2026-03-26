# DJ Brate - AI-Powered Spotify Playlist Generator

> A full-stack web application that reads your mood, understands your taste, and builds the perfect playlist using Spotify + AI.

---

## Project Overview

DJ Brate is a project built as a full-stack web application. Users describe how they're feeling - by writing a free prompt, selecting a mood, picking genres, or adjusting energy sliders - and the app uses the **Spotify API** combined with an **AI layer** to generate personalized playlists tailored to their actual taste, not just generic recommendations.

The app has a fully responsive UI where users can browse their generated playlists, preview songs directly in the browser, check their listening statistics, and generate new playlists at any time. The AI gets smarter with every session - it learns what you actually enjoy and builds playlists around your real listening habits, not just the first song you happened to play once.

The project integrates **Model Context Protocol (MCP)** to allow the AI model to directly interact with Spotify as a tool, making suggestions dynamic and context-aware rather than rule-based.

---

## Features

### Mood & Preference Input

1. Write a free-text prompt to describe what you want
2. Select a mood from predefined tags (happy, sad, energetic, focused, chill, romantic) as an alternative to typing
3. Pick genres and set energy level via sliders for more precise control

### Spotify Integration

4. OAuth 2.0 login - users connect their real Spotify account securely
5. Reads the user's full listening history, top tracks, and top artists to understand their genuine taste
6. Generates a playlist using Spotify's Recommendations API seeded by mood-mapped audio parameters and the user's actual favourite artists and tracks
7. Saves the generated playlist directly to the user's Spotify library
8. Plays 30-second song previews inside the app with a built-in player

### AI-Powered Suggestions

9. AI interprets the user's prompt and maps it to Spotify audio feature targets (valence, energy, tempo, danceability)
10. Automatically generates a playlist name and description based on the detected mood
11. Learns from user feedback over time - liked and skipped tracks are tracked so future playlists reflect what you actually enjoy, not just what you listened to once
12. Uses MCP to let the AI call Spotify tools dynamically as part of its reasoning, rather than following static rules

### Responsive UI

13. Clean, responsive interface that works on desktop and mobile
14. Dedicated playlist view where users can browse all their generated playlists and preview songs without leaving the app
15. Separate statistics page showing mood history, top genres, and listening trends over time - with the option to generate a new playlist at any point

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor Server (C#) |
| Backend | ASP.NET Core (.NET 10) |
| Real-time | SignalR (live playlist updates) |
| Database | PostgreSQL |
| ORM | Entity Framework Core |
| AI | Any AI API (OpenAI, Claude, Gemini, etc.) |
| Music | Spotify Web API + Web Playback SDK |
| MCP | Custom MCP Server (C#) exposing Spotify tools |
| Auth | Cookie Auth + Spotify OAuth 2.0 |
| Containerization | Docker + Docker Compose |
| Background Jobs | Hangfire (periodic stats refresh) |

---

## Spotify API - What's Possible

| Feature | Available | Notes |
|---|---|---|
| User login (OAuth) | Free | Users connect their Spotify account |
| Get top tracks & artists | Free | Read user listening history |
| Song recommendations | Free | Seed by mood/genre/audio features |
| Create playlists | Free | Saved to user's actual Spotify |
| Audio features per track | Free | Tempo, energy, valence, danceability |
| 30-second song previews | Free | Play short clips inside your app |
| Full song playback | Premium only | Requires Spotify Premium + Playback SDK |
| Search songs/albums | Free | Full search capability |

> **Note:** Full track playback requires the user to have a Spotify Premium subscription. For the purposes of this project, 30-second previews are used for in-app listening, while full songs open in the user's Spotify app via deep links.

---

## Getting Started

### Prerequisites
- Docker & Docker Compose installed
- Spotify Developer account (free) - [developer.spotify.com](https://developer.spotify.com)
- Any AI API key

---