using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DJBrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FeedbackPerSpotifyTrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_track_feedbacks_user_id_playlist_track_id",
                table: "track_feedbacks");

            migrationBuilder.Sql("DELETE FROM track_feedbacks;");

            migrationBuilder.CreateIndex(
                name: "IX_track_feedbacks_user_id_spotify_track_id",
                table: "track_feedbacks",
                columns: new[] { "user_id", "spotify_track_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_track_feedbacks_user_id_spotify_track_id",
                table: "track_feedbacks");

            migrationBuilder.CreateIndex(
                name: "IX_track_feedbacks_user_id_playlist_track_id",
                table: "track_feedbacks",
                columns: new[] { "user_id", "playlist_track_id" },
                unique: true);
        }
    }
}
