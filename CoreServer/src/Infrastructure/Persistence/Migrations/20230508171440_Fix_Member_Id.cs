using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMemberId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VideoStreams_VideoMembers_OwnerId",
                table: "VideoStreams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoMembers",
                table: "VideoMembers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoMembers",
                table: "VideoMembers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMembers_BaseMemberId",
                table: "VideoMembers",
                column: "BaseMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStreams_VideoMembers_OwnerId",
                table: "VideoStreams",
                column: "OwnerId",
                principalTable: "VideoMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VideoStreams_VideoMembers_OwnerId",
                table: "VideoStreams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoMembers",
                table: "VideoMembers");

            migrationBuilder.DropIndex(
                name: "IX_VideoMembers_BaseMemberId",
                table: "VideoMembers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoMembers",
                table: "VideoMembers",
                column: "BaseMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStreams_VideoMembers_OwnerId",
                table: "VideoStreams",
                column: "OwnerId",
                principalTable: "VideoMembers",
                principalColumn: "BaseMemberId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
