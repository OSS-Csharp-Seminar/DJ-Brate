using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DJBrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlaylistTrackFromFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_track_feedbacks_playlist_tracks_playlist_track_id",
                table: "track_feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_track_feedbacks_playlist_track_id",
                table: "track_feedbacks");

            migrationBuilder.DropColumn(
                name: "playlist_track_id",
                table: "track_feedbacks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "playlist_track_id",
                table: "track_feedbacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_track_feedbacks_playlist_track_id",
                table: "track_feedbacks",
                column: "playlist_track_id");

            migrationBuilder.AddForeignKey(
                name: "FK_track_feedbacks_playlist_tracks_playlist_track_id",
                table: "track_feedbacks",
                column: "playlist_track_id",
                principalTable: "playlist_tracks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
