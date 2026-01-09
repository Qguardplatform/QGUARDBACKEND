using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddToScheduleExam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublishedMailSent",
                table: "ExamSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReminderMailSent",
                table: "ExamSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StartingSoonMailSent",
                table: "ExamSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedMailSent",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "ReminderMailSent",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "StartingSoonMailSent",
                table: "ExamSchedules");
        }
    }
}
