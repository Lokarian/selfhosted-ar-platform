using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFileType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FileType",
                table: "UserFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileType",
                table: "UserFiles");
        }
    }
}
