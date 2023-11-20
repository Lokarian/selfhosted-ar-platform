using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_AppUsers_UserId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_ChatSessions_SessionId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_AppUsers_CreatedById",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_AppUsers_LastModifiedById",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStreams_VideoSessionMembers_OwnerId",
                table: "VideoStreams");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStreams_VideoSessions_VideoSessionId",
                table: "VideoStreams");

            migrationBuilder.DropTable(
                name: "VideoSessionMembers");

            migrationBuilder.DropIndex(
                name: "IX_VideoStreams_VideoSessionId",
                table: "VideoStreams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoSessions",
                table: "VideoSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatSessions",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_CreatedById",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_LastModifiedById",
                table: "ChatSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMembers",
                table: "ChatMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_UserId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "VideoSessionId",
                table: "VideoStreams");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "ReferencePoint",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "Id",
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

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ChatSessions");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ChatMembers",
                newName: "BaseMemberId");

            migrationBuilder.AddColumn<Guid>(
                name: "BaseSessionId",
                table: "VideoSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BaseSessionId",
                table: "ChatSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoSessions",
                table: "VideoSessions",
                column: "BaseSessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatSessions",
                table: "ChatSessions",
                column: "BaseSessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMembers",
                table: "ChatMembers",
                column: "BaseMemberId");

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AppUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSessions_AppUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SessionMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionMembers_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionMembers_UserSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UserSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoMembers",
                columns: table => new
                {
                    BaseMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessKey = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoMembers", x => x.BaseMemberId);
                    table.ForeignKey(
                        name: "FK_VideoMembers_SessionMembers_BaseMemberId",
                        column: x => x.BaseMemberId,
                        principalTable: "SessionMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoMembers_VideoSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VideoSessions",
                        principalColumn: "BaseSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_SessionId",
                table: "ChatMembers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionMembers_SessionId",
                table: "SessionMembers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionMembers_UserId",
                table: "SessionMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_CreatedById",
                table: "UserSessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LastModifiedById",
                table: "UserSessions",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMembers_SessionId",
                table: "VideoMembers",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_ChatSessions_SessionId",
                table: "ChatMembers",
                column: "SessionId",
                principalTable: "ChatSessions",
                principalColumn: "BaseSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_SessionMembers_BaseMemberId",
                table: "ChatMembers",
                column: "BaseMemberId",
                principalTable: "SessionMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionId",
                table: "ChatMessages",
                column: "SessionId",
                principalTable: "ChatSessions",
                principalColumn: "BaseSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_UserSessions_BaseSessionId",
                table: "ChatSessions",
                column: "BaseSessionId",
                principalTable: "UserSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoSessions_UserSessions_BaseSessionId",
                table: "VideoSessions",
                column: "BaseSessionId",
                principalTable: "UserSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStreams_VideoMembers_OwnerId",
                table: "VideoStreams",
                column: "OwnerId",
                principalTable: "VideoMembers",
                principalColumn: "BaseMemberId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_ChatSessions_SessionId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_SessionMembers_BaseMemberId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_UserSessions_BaseSessionId",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoSessions_UserSessions_BaseSessionId",
                table: "VideoSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoStreams_VideoMembers_OwnerId",
                table: "VideoStreams");

            migrationBuilder.DropTable(
                name: "VideoMembers");

            migrationBuilder.DropTable(
                name: "SessionMembers");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoSessions",
                table: "VideoSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatSessions",
                table: "ChatSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMembers",
                table: "ChatMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_SessionId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "BaseSessionId",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "BaseSessionId",
                table: "ChatSessions");

            migrationBuilder.RenameColumn(
                name: "BaseMemberId",
                table: "ChatMembers",
                newName: "UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "VideoSessionId",
                table: "VideoStreams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "VideoSessions",
                type: "uuid",
                nullable: false,
                defaultValueSql: "uuid_generate_v4()");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferencePoint",
                table: "VideoSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "ChatSessions",
                type: "uuid",
                nullable: false,
                defaultValueSql: "uuid_generate_v4()");

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

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ChatSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoSessions",
                table: "VideoSessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatSessions",
                table: "ChatSessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMembers",
                table: "ChatMembers",
                columns: new[] { "SessionId", "UserId" });

            migrationBuilder.CreateTable(
                name: "VideoSessionMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessKey = table.Column<string>(type: "text", nullable: false),
                    Joined = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_VideoStreams_VideoSessionId",
                table: "VideoStreams",
                column: "VideoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CreatedById",
                table: "ChatSessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_LastModifiedById",
                table: "ChatSessions",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_UserId",
                table: "ChatMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessionMembers_SessionId",
                table: "VideoSessionMembers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessionMembers_UserId",
                table: "VideoSessionMembers",
                column: "UserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionId",
                table: "ChatMessages",
                column: "SessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStreams_VideoSessionMembers_OwnerId",
                table: "VideoStreams",
                column: "OwnerId",
                principalTable: "VideoSessionMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoStreams_VideoSessions_VideoSessionId",
                table: "VideoStreams",
                column: "VideoSessionId",
                principalTable: "VideoSessions",
                principalColumn: "Id");
        }
    }
}
