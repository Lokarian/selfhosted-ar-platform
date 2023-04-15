using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatmember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMember_AppUsers_UserId",
                table: "ChatMember");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMember_ChatSessions_SessionId",
                table: "ChatMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMember",
                table: "ChatMember");

            migrationBuilder.RenameTable(
                name: "ChatMember",
                newName: "ChatMembers");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMember_UserId",
                table: "ChatMembers",
                newName: "IX_ChatMembers_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMembers",
                table: "ChatMembers",
                columns: new[] { "SessionId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_AppUsers_UserId",
                table: "ChatMembers",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_ChatSessions_SessionId",
                table: "ChatMembers",
                column: "SessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_AppUsers_UserId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_ChatSessions_SessionId",
                table: "ChatMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMembers",
                table: "ChatMembers");

            migrationBuilder.RenameTable(
                name: "ChatMembers",
                newName: "ChatMember");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMembers_UserId",
                table: "ChatMember",
                newName: "IX_ChatMember_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMember",
                table: "ChatMember",
                columns: new[] { "SessionId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMember_AppUsers_UserId",
                table: "ChatMember",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMember_ChatSessions_SessionId",
                table: "ChatMember",
                column: "SessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
