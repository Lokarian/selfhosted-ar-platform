using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccessKeyVideoSessionmember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessKey",
                table: "VideoStreams");

            migrationBuilder.AddColumn<string>(
                name: "AccessKey",
                table: "VideoSessionMembers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessKey",
                table: "VideoSessionMembers");

            migrationBuilder.AddColumn<string>(
                name: "AccessKey",
                table: "VideoStreams",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
