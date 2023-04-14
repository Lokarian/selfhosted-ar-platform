using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInStorageFilename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InStorageFileName",
                table: "UserFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InStorageFileName",
                table: "UserFiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
