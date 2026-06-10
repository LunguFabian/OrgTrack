using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrgTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateRsvpAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "EventRsvps",
                newName: "Rsvp");

            // Convert old PresenceStatus values to new RsvpStatus values
            migrationBuilder.Sql(@"
                UPDATE ""EventRsvps"" SET ""Rsvp"" = CASE
                    WHEN ""Rsvp"" = 'Present' THEN 'Going'
                    WHEN ""Rsvp"" = 'Absent'  THEN 'NotGoing'
                    WHEN ""Rsvp"" = 'Maybe'   THEN 'Maybe'
                    WHEN ""Rsvp"" = 'Excused'  THEN 'NotGoing'
                    ELSE 'NoResponse'
                END;
            ");

            migrationBuilder.AddColumn<string>(
                name: "Attendance",
                table: "EventRsvps",
                type: "text",
                nullable: false,
                defaultValue: "Unmarked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attendance",
                table: "EventRsvps");

            migrationBuilder.RenameColumn(
                name: "Rsvp",
                table: "EventRsvps",
                newName: "Status");
        }
    }
}
