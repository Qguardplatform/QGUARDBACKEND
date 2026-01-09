using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class include_candidate_selected_option : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedQuestionOptionId",
                table: "CandidateExamsSubmissions");
        }
    }
}
