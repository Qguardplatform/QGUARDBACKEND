using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class navigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_Courses_CourseId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_Levels_LevelId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_Semesters_SemesterId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_Sessions_SessionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_CourseId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_LevelId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_SemesterId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_SessionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "LevelId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "CandidateExamsSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CourseId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LevelId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SemesterId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SessionId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_CourseId",
                table: "CandidateExamsSubmissions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_LevelId",
                table: "CandidateExamsSubmissions",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_SemesterId",
                table: "CandidateExamsSubmissions",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_SessionId",
                table: "CandidateExamsSubmissions",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_Courses_CourseId",
                table: "CandidateExamsSubmissions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_Levels_LevelId",
                table: "CandidateExamsSubmissions",
                column: "LevelId",
                principalTable: "Levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_Semesters_SemesterId",
                table: "CandidateExamsSubmissions",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_Sessions_SessionId",
                table: "CandidateExamsSubmissions",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
