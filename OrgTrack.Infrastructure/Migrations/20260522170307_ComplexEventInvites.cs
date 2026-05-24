using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrgTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ComplexEventInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetAudience",
                table: "Events");

            migrationBuilder.CreateTable(
                name: "EventInvitedUnits",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInvitedUnits", x => new { x.EventId, x.OrganizationUnitId });
                    table.ForeignKey(
                        name: "FK_EventInvitedUnits_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventInvitedUnits_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventInvitedUsers",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInvitedUsers", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EventInvitedUsers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventInvitedUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitedUnits_OrganizationUnitId",
                table: "EventInvitedUnits",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitedUsers_UserId",
                table: "EventInvitedUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventInvitedUnits");

            migrationBuilder.DropTable(
                name: "EventInvitedUsers");

            migrationBuilder.AddColumn<int>(
                name: "TargetAudience",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
