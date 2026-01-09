using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class adjustmodelBindingOnOptionSelected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_QuestionOptions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                onDelete: ReferentialAction.SetNull);
        }
    }
}
