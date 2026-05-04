using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DJBrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistShareToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_shared",
                table: "playlists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "share_token",
                table: "playlists",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlists_share_token",
                table: "playlists",
                column: "share_token",
                unique: true,
                filter: "share_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_playlists_share_token",
                table: "playlists");

            migrationBuilder.DropColumn(
                name: "is_shared",
                table: "playlists");

            migrationBuilder.DropColumn(
                name: "share_token",
                table: "playlists");
        }
    }
}
