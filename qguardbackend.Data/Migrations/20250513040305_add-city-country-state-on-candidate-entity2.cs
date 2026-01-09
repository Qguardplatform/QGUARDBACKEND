using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class addcitycountrystateoncandidateentity2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ExamScheduleId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_ExamScheduleId",
                table: "CandidateExamsSubmissions",
                column: "ExamScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_ExamSchedules_ExamScheduleId",
                table: "CandidateExamsSubmissions",
                column: "ExamScheduleId",
                principalTable: "ExamSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_ExamSchedules_ExamScheduleId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_ExamScheduleId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "ExamScheduleId",
                table: "CandidateExamsSubmissions");
        }
    }
}
