using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeToBaseSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_UserSessions_BaseSessionId",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionMembers_UserSessions_SessionId",
                table: "SessionMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_AppUsers_CreatedById",
                table: "UserSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_AppUsers_LastModifiedById",
                table: "UserSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoSessions_UserSessions_BaseSessionId",
                table: "VideoSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSessions",
                table: "UserSessions");

            migrationBuilder.RenameTable(
                name: "UserSessions",
                newName: "BaseSessions");

            migrationBuilder.RenameIndex(
                name: "IX_UserSessions_LastModifiedById",
                table: "BaseSessions",
                newName: "IX_BaseSessions_LastModifiedById");

            migrationBuilder.RenameIndex(
                name: "IX_UserSessions_CreatedById",
                table: "BaseSessions",
                newName: "IX_BaseSessions_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BaseSessions",
                table: "BaseSessions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseSessions_AppUsers_CreatedById",
                table: "BaseSessions",
                column: "CreatedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseSessions_AppUsers_LastModifiedById",
                table: "BaseSessions",
                column: "LastModifiedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_BaseSessions_BaseSessionId",
                table: "ChatSessions",
                column: "BaseSessionId",
                principalTable: "BaseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionMembers_BaseSessions_SessionId",
                table: "SessionMembers",
                column: "SessionId",
                principalTable: "BaseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoSessions_BaseSessions_BaseSessionId",
                table: "VideoSessions",
                column: "BaseSessionId",
                principalTable: "BaseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseSessions_AppUsers_CreatedById",
                table: "BaseSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_BaseSessions_AppUsers_LastModifiedById",
                table: "BaseSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_BaseSessions_BaseSessionId",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionMembers_BaseSessions_SessionId",
                table: "SessionMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoSessions_BaseSessions_BaseSessionId",
                table: "VideoSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BaseSessions",
                table: "BaseSessions");

            migrationBuilder.RenameTable(
                name: "BaseSessions",
                newName: "UserSessions");

            migrationBuilder.RenameIndex(
                name: "IX_BaseSessions_LastModifiedById",
                table: "UserSessions",
                newName: "IX_UserSessions_LastModifiedById");

            migrationBuilder.RenameIndex(
                name: "IX_BaseSessions_CreatedById",
                table: "UserSessions",
                newName: "IX_UserSessions_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSessions",
                table: "UserSessions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_UserSessions_BaseSessionId",
                table: "ChatSessions",
                column: "BaseSessionId",
                principalTable: "UserSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionMembers_UserSessions_SessionId",
                table: "SessionMembers",
                column: "SessionId",
                principalTable: "UserSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_AppUsers_CreatedById",
                table: "UserSessions",
                column: "CreatedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_AppUsers_LastModifiedById",
                table: "UserSessions",
                column: "LastModifiedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoSessions_UserSessions_BaseSessionId",
                table: "VideoSessions",
                column: "BaseSessionId",
                principalTable: "UserSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
