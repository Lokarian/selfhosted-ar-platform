using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserConnectionToVideoMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserConnectionId",
                table: "VideoMembers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_VideoMembers_UserConnectionId",
                table: "VideoMembers",
                column: "UserConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoMembers_UserConnections_UserConnectionId",
                table: "VideoMembers",
                column: "UserConnectionId",
                principalTable: "UserConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VideoMembers_UserConnections_UserConnectionId",
                table: "VideoMembers");

            migrationBuilder.DropIndex(
                name: "IX_VideoMembers_UserConnectionId",
                table: "VideoMembers");

            migrationBuilder.DropColumn(
                name: "UserConnectionId",
                table: "VideoMembers");
        }
    }
}
