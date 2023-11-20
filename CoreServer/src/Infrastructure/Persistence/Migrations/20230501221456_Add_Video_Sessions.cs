using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ReferencePoint = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoSessionMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Joined = table.Column<bool>(type: "boolean", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSessionMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoSessionMembers_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoSessionMembers_VideoSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VideoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoStreams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StoppedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VideoSessionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoStreams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoStreams_VideoSessionMembers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "VideoSessionMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoStreams_VideoSessions_VideoSessionId",
                        column: x => x.VideoSessionId,
                        principalTable: "VideoSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessionMembers_SessionId",
                table: "VideoSessionMembers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessionMembers_UserId",
                table: "VideoSessionMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoStreams_OwnerId",
                table: "VideoStreams",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoStreams_VideoSessionId",
                table: "VideoStreams",
                column: "VideoSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoStreams");

            migrationBuilder.DropTable(
                name: "VideoSessionMembers");

            migrationBuilder.DropTable(
                name: "VideoSessions");
        }
    }
}
