using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InstantAnswerGrading",
                table: "ExamSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InstantResultPublishing",
                table: "ExamSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ExamSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "CandidateExamsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnswerSelected",
                table: "CandidateExamsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswer",
                table: "CandidateExamsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "CandidateExamsSubmissions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstantAnswerGrading",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "InstantResultPublishing",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "AnswerSelected",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "CorrectAnswer",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "CandidateExamsSubmissions");

            migrationBuilder.AlterColumn<string>(
                name: "Score",
                table: "CandidateExamsSubmissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
