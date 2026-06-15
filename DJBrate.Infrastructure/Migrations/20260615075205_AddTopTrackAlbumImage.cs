using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DJBrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTopTrackAlbumImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE user_top_tracks ADD COLUMN IF NOT EXISTS album_image_url text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE user_top_tracks DROP COLUMN IF EXISTS album_image_url;");
        }
    }
}
