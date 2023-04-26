using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeChatSessionAuditable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Created",
                table: "TodoLists",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Created",
                table: "TodoItems",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "ChatSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "ChatSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedById",
                table: "ChatSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CreatedById",
                table: "ChatSessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_LastModifiedById",
                table: "ChatSessions",
                column: "LastModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_AppUsers_CreatedById",
                table: "ChatSessions",
                column: "CreatedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_AppUsers_LastModifiedById",
                table: "ChatSessions",
                column: "LastModifiedById",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_AppUsers_CreatedById",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_AppUsers_LastModifiedById",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_CreatedById",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_LastModifiedById",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "LastModifiedById",
                table: "ChatSessions");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "TodoLists",
                newName: "Created");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "TodoItems",
                newName: "Created");
        }
    }
}
