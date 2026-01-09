using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlagProctorLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateProctorLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlagId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CandidateId = table.Column<long>(type: "bigint", nullable: false),
                    ExamScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    EnforcementModeId = table.Column<long>(type: "bigint", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Event = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProctorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateProctorLogs_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateProctorLogs_EnforcementModes_EnforcementModeId",
                        column: x => x.EnforcementModeId,
                        principalTable: "EnforcementModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateProctorLogs_ExamSchedules_ExamScheduleId",
                        column: x => x.ExamScheduleId,
                        principalTable: "ExamSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateProctorLogs_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProctorLogs_CandidateId",
                table: "CandidateProctorLogs",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProctorLogs_CandidateId_ExamScheduleId",
                table: "CandidateProctorLogs",
                columns: new[] { "CandidateId", "ExamScheduleId" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProctorLogs_EnforcementModeId",
                table: "CandidateProctorLogs",
                column: "EnforcementModeId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProctorLogs_ExamScheduleId",
                table: "CandidateProctorLogs",
                column: "ExamScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProctorLogs_FlagId",
                table: "CandidateProctorLogs",
                column: "FlagId",
                unique: true,
                filter: "[FlagId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProctorLogs_InstitutionId",
                table: "CandidateProctorLogs",
                column: "InstitutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateProctorLogs");
        }
    }
}
