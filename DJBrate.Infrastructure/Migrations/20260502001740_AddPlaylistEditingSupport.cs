using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DJBrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistEditingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "refines_playlist_id",
                table: "mood_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "session_type",
                table: "mood_sessions",
                type: "text",
                nullable: false,
                defaultValue: "create");

            migrationBuilder.Sql("UPDATE mood_sessions SET session_type = 'create' WHERE session_type = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refines_playlist_id",
                table: "mood_sessions");

            migrationBuilder.DropColumn(
                name: "session_type",
                table: "mood_sessions");
        }
    }
}
