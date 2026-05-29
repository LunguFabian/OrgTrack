using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrgTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleCalendarTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleCalendarAccessToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleCalendarRefreshToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoogleCalendarConnected",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleCalendarAccessToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleCalendarRefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsGoogleCalendarConnected",
                table: "Users");
        }
    }
}
