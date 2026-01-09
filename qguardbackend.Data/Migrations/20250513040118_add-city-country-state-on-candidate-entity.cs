using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class addcitycountrystateoncandidateentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "Candidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "Candidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegionId",
                table: "Candidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateExamsSubmissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<long>(type: "bigint", nullable: false),
                    CourseId = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<long>(type: "bigint", nullable: true),
                    SemesterId = table.Column<long>(type: "bigint", nullable: true),
                    LevelId = table.Column<long>(type: "bigint", nullable: true),
                    QuestionBankId = table.Column<long>(type: "bigint", nullable: true),
                    QuestionOptionId = table.Column<long>(type: "bigint", nullable: true),
                    IsCorrectOption = table.Column<bool>(type: "bit", nullable: true),
                    IsGraded = table.Column<bool>(type: "bit", nullable: true),
                    IsResultPublishable = table.Column<bool>(type: "bit", nullable: true),
                    SubmissionDateAndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateExamsSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateExamsSubmissions_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateExamsSubmissions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateExamsSubmissions_Levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateExamsSubmissions_QuestionBanks_QuestionBankId",
                        column: x => x.QuestionBankId,
                        principalTable: "QuestionBanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateExamsSubmissions_QuestionOptions_QuestionOptionId",
                        column: x => x.QuestionOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateExamsSubmissions_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateExamsSubmissions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_CityId",
                table: "Candidates",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_CountryId",
                table: "Candidates",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_RegionId",
                table: "Candidates",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_CandidateId",
                table: "CandidateExamsSubmissions",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_CourseId",
                table: "CandidateExamsSubmissions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_LevelId",
                table: "CandidateExamsSubmissions",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_QuestionBankId",
                table: "CandidateExamsSubmissions",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_QuestionOptionId",
                table: "CandidateExamsSubmissions",
                column: "QuestionOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_SemesterId",
                table: "CandidateExamsSubmissions",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_SessionId",
                table: "CandidateExamsSubmissions",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Cities_CityId",
                table: "Candidates",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Countries_CountryId",
                table: "Candidates",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Regions_RegionId",
                table: "Candidates",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Cities_CityId",
                table: "Candidates");

            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Countries_CountryId",
                table: "Candidates");

            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Regions_RegionId",
                table: "Candidates");

            migrationBuilder.DropTable(
                name: "CandidateExamsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_CityId",
                table: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_CountryId",
                table: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_RegionId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Candidates");
        }
    }
}
