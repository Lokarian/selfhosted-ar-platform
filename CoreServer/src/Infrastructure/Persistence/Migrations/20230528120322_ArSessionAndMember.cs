using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ArSessionAndMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArSessions",
                columns: table => new
                {
                    BaseSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionType = table.Column<int>(type: "integer", nullable: false),
                    StoppedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArSessions", x => x.BaseSessionId);
                    table.ForeignKey(
                        name: "FK_ArSessions_BaseSessions_BaseSessionId",
                        column: x => x.BaseSessionId,
                        principalTable: "BaseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    AccessKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArMembers_ArSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ArSessions",
                        principalColumn: "BaseSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArMembers_SessionMembers_BaseMemberId",
                        column: x => x.BaseMemberId,
                        principalTable: "SessionMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArMembers_UserConnections_UserConnectionId",
                        column: x => x.UserConnectionId,
                        principalTable: "UserConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArMembers_BaseMemberId",
                table: "ArMembers",
                column: "BaseMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ArMembers_SessionId",
                table: "ArMembers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArMembers_UserConnectionId",
                table: "ArMembers",
                column: "UserConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArMembers");

            migrationBuilder.DropTable(
                name: "ArSessions");
        }
    }
}
