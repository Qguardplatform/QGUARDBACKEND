using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class addinstIdtocandidate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_ExamSchedules_ExamScheduleId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "Candidates",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_InstitutionId",
                table: "Candidates",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_ExamSchedules_ExamScheduleId",
                table: "CandidateExamsSubmissions",
                column: "ExamScheduleId",
                principalTable: "ExamSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Institutions_InstitutionId",
                table: "Candidates",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_ExamSchedules_ExamScheduleId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Institutions_InstitutionId",
                table: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_InstitutionId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Candidates");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_ExamSchedules_ExamScheduleId",
                table: "CandidateExamsSubmissions",
                column: "ExamScheduleId",
                principalTable: "ExamSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
