using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class set_null_params : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions",
                column: "SelectedQuestionOptionId",
                principalTable: "QuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions",
                column: "SelectedQuestionOptionId",
                principalTable: "QuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
