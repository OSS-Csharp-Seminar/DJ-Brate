using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DJBrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistTrackAiNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_note",
                table: "playlist_tracks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_note",
                table: "playlist_tracks");
        }
    }
}
