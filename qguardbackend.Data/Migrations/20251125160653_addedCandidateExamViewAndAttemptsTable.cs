using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedCandidateExamViewAndAttemptsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateExamViewAndAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<long>(type: "bigint", nullable: false),
                    ExamScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    NoOfViewTimes = table.Column<long>(type: "bigint", nullable: false),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateExamViewAndAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateExamViewAndAttempts_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateExamViewAndAttempts_ExamSchedules_ExamScheduleId",
                        column: x => x.ExamScheduleId,
                        principalTable: "ExamSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateExamViewAndAttempts_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamViewAndAttempts_CandidateId",
                table: "CandidateExamViewAndAttempts",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamViewAndAttempts_ExamScheduleId",
                table: "CandidateExamViewAndAttempts",
                column: "ExamScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamViewAndAttempts_InstitutionId",
                table: "CandidateExamViewAndAttempts",
                column: "InstitutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateExamViewAndAttempts");
        }
    }
}
