using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedmappingParametersForExamSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ResultId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultId",
                table: "CandidateExamsSubmissions");
        }
    }
}
