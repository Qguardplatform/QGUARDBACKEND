using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateonexamsubmissiontable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_QuestionOptionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_QuestionOptionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.RenameColumn(
                name: "QuestionOptionId",
                table: "CandidateExamsSubmissions",
                newName: "CorrectQuestionOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions",
                column: "SelectedQuestionOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions",
                column: "SelectedQuestionOptionId",
                principalTable: "QuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.RenameColumn(
                name: "CorrectQuestionOptionId",
                table: "CandidateExamsSubmissions",
                newName: "QuestionOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_QuestionOptionId",
                table: "CandidateExamsSubmissions",
                column: "QuestionOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_QuestionOptionId",
                table: "CandidateExamsSubmissions",
                column: "QuestionOptionId",
                principalTable: "QuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
