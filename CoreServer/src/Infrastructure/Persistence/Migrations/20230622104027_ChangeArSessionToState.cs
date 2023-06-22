using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeArSessionToState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoppedAt",
                table: "ArSessions");

            migrationBuilder.AddColumn<int>(
                name: "ServerState",
                table: "ArSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServerState",
                table: "ArSessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "StoppedAt",
                table: "ArSessions",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
