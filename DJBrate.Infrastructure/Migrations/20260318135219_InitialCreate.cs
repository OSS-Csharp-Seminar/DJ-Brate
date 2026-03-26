using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DJBrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_model_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_name = table.Column<string>(type: "text", nullable: false),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    model_name = table.Column<string>(type: "text", nullable: false),
                    temperature = table.Column<float>(type: "real", nullable: true),
                    max_tokens = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_model_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    spotify_access_token = table.Column<string>(type: "text", nullable: false),
                    spotify_refresh_token = table.Column<string>(type: "text", nullable: false),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "listening_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stat_date = table.Column<DateOnly>(type: "date", nullable: false),
                    dominant_mood = table.Column<string>(type: "text", nullable: true),
                    top_genre = table.Column<string>(type: "text", nullable: true),
                    avg_energy = table.Column<float>(type: "real", nullable: true),
                    avg_valence = table.Column<float>(type: "real", nullable: true),
                    avg_tempo = table.Column<float>(type: "real", nullable: true),
                    playlists_generated = table.Column<int>(type: "integer", nullable: false),
                    tracks_liked = table.Column<int>(type: "integer", nullable: false),
                    tracks_skipped = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listening_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_listening_stats_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mood_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prompt_text = table.Column<string>(type: "text", nullable: true),
                    selected_mood = table.Column<string>(type: "text", nullable: true),
                    selected_genres = table.Column<string[]>(type: "text[]", nullable: true),
                    energy_level = table.Column<float>(type: "real", nullable: true),
                    danceability = table.Column<float>(type: "real", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mood_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_mood_sessions_ai_model_configs_ai_config_id",
                        column: x => x.ai_config_id,
                        principalTable: "ai_model_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mood_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_top_artists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_artist_id = table.Column<string>(type: "text", nullable: false),
                    artist_name = table.Column<string>(type: "text", nullable: false),
                    genres = table.Column<string[]>(type: "text[]", nullable: true),
                    time_range = table.Column<string>(type: "text", nullable: false),
                    rank_position = table.Column<int>(type: "integer", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_top_artists", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_top_artists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_top_tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_track_id = table.Column<string>(type: "text", nullable: false),
                    track_name = table.Column<string>(type: "text", nullable: false),
                    spotify_artist_id = table.Column<string>(type: "text", nullable: false),
                    artist_name = table.Column<string>(type: "text", nullable: false),
                    time_range = table.Column<string>(type: "text", nullable: false),
                    rank_position = table.Column<int>(type: "integer", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_top_tracks", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_top_tracks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_conversation_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sequence_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_conversation_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_conversation_messages_mood_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "mood_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_mood_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    detected_mood = table.Column<string>(type: "text", nullable: true),
                    detected_genres = table.Column<string[]>(type: "text[]", nullable: true),
                    target_valence = table.Column<float>(type: "real", nullable: true),
                    target_energy = table.Column<float>(type: "real", nullable: true),
                    target_tempo = table.Column<float>(type: "real", nullable: true),
                    target_danceability = table.Column<float>(type: "real", nullable: true),
                    target_acousticness = table.Column<float>(type: "real", nullable: true),
                    flow_used = table.Column<string>(type: "text", nullable: false),
                    ai_reasoning = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_mood_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_mood_mappings_mood_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "mood_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mcp_tool_calls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_name = table.Column<string>(type: "text", nullable: false),
                    input_parameters = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    output_result = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    called_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_tool_calls", x => x.id);
                    table.ForeignKey(
                        name: "FK_mcp_tool_calls_mood_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "mood_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_playlist_id = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    track_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlists", x => x.id);
                    table.ForeignKey(
                        name: "FK_playlists_mood_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "mood_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_playlists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_track_id = table.Column<string>(type: "text", nullable: false),
                    track_name = table.Column<string>(type: "text", nullable: false),
                    spotify_artist_id = table.Column<string>(type: "text", nullable: false),
                    artist_name = table.Column<string>(type: "text", nullable: false),
                    album_name = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    preview_url = table.Column<string>(type: "text", nullable: true),
                    valence = table.Column<float>(type: "real", nullable: true),
                    energy = table.Column<float>(type: "real", nullable: true),
                    tempo = table.Column<float>(type: "real", nullable: true),
                    danceability = table.Column<float>(type: "real", nullable: true),
                    acousticness = table.Column<float>(type: "real", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_tracks", x => x.id);
                    table.ForeignKey(
                        name: "FK_playlist_tracks_playlists_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "playlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "track_feedbacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_track_id = table.Column<string>(type: "text", nullable: false),
                    feedback_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_feedbacks_playlist_tracks_playlist_track_id",
                        column: x => x.playlist_track_id,
                        principalTable: "playlist_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_track_feedbacks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversation_messages_session_id",
                table: "ai_conversation_messages",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_mood_mappings_session_id",
                table: "ai_mood_mappings",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listening_stats_user_id_stat_date",
                table: "listening_stats",
                columns: new[] { "user_id", "stat_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mcp_tool_calls_session_id",
                table: "mcp_tool_calls",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_mood_sessions_ai_config_id",
                table: "mood_sessions",
                column: "ai_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_mood_sessions_user_id",
                table: "mood_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_tracks_playlist_id",
                table: "playlist_tracks",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlists_session_id",
                table: "playlists",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlists_user_id",
                table: "playlists",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_track_feedbacks_playlist_track_id",
                table: "track_feedbacks",
                column: "playlist_track_id");

            migrationBuilder.CreateIndex(
                name: "IX_track_feedbacks_user_id_playlist_track_id",
                table: "track_feedbacks",
                columns: new[] { "user_id", "playlist_track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_top_artists_user_id_spotify_artist_id_time_range",
                table: "user_top_artists",
                columns: new[] { "user_id", "spotify_artist_id", "time_range" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_top_tracks_user_id_spotify_track_id_time_range",
                table: "user_top_tracks",
                columns: new[] { "user_id", "spotify_track_id", "time_range" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_spotify_id",
                table: "users",
                column: "spotify_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_conversation_messages");

            migrationBuilder.DropTable(
                name: "ai_mood_mappings");

            migrationBuilder.DropTable(
                name: "listening_stats");

            migrationBuilder.DropTable(
                name: "mcp_tool_calls");

            migrationBuilder.DropTable(
                name: "track_feedbacks");

            migrationBuilder.DropTable(
                name: "user_top_artists");

            migrationBuilder.DropTable(
                name: "user_top_tracks");

            migrationBuilder.DropTable(
                name: "playlist_tracks");

            migrationBuilder.DropTable(
                name: "playlists");

            migrationBuilder.DropTable(
                name: "mood_sessions");

            migrationBuilder.DropTable(
                name: "ai_model_configs");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
