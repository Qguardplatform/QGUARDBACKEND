using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoreMigDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnforcementActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnforcementActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnforcementModes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnforcementModes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProctorConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParameterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExamScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProctorConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProctorConfigurations_ExamSchedules_ExamScheduleId",
                        column: x => x.ExamScheduleId,
                        principalTable: "ExamSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProctorMeTrackers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivityDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivityResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivityResponseDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProctorMeTrackers", x => x.Id);
                });

            //migrationBuilder.CreateTable(
            //    name: "CandidateProctorLogs",
            //    columns: table => new
            //    {
            //        Id = table.Column<long>(type: "bigint", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        FlagId = table.Column<string>(type: "nvarchar(450)", nullable: true),
            //        CandidateId = table.Column<long>(type: "bigint", nullable: false),
            //        ExamScheduleId = table.Column<long>(type: "bigint", nullable: false),
            //        EnforcementModeId = table.Column<long>(type: "bigint", nullable: false),
            //        MediaUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        IsDeleted = table.Column<bool>(type: "bit", nullable: false),
            //        IsActive = table.Column<bool>(type: "bit", nullable: false),
            //        CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            //        UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_CandidateProctorLogs", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_CandidateProctorLogs_Candidates_CandidateId",
            //            column: x => x.CandidateId,
            //            principalTable: "Candidates",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_CandidateProctorLogs_EnforcementModes_EnforcementModeId",
            //            column: x => x.EnforcementModeId,
            //            principalTable: "EnforcementModes",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_CandidateProctorLogs_ExamSchedules_ExamScheduleId",
            //            column: x => x.ExamScheduleId,
            //            principalTable: "ExamSchedules",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CandidateProctorLogs_CandidateId",
            //    table: "CandidateProctorLogs",
            //    column: "CandidateId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CandidateProctorLogs_CandidateId_ExamScheduleId",
            //    table: "CandidateProctorLogs",
            //    columns: new[] { "CandidateId", "ExamScheduleId" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CandidateProctorLogs_EnforcementModeId",
            //    table: "CandidateProctorLogs",
            //    column: "EnforcementModeId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CandidateProctorLogs_ExamScheduleId",
            //    table: "CandidateProctorLogs",
            //    column: "ExamScheduleId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CandidateProctorLogs_FlagId",
            //    table: "CandidateProctorLogs",
            //    column: "FlagId",
            //    unique: true,
            //    filter: "[FlagId] IS NOT NULL");

            //migrationBuilder.CreateIndex(
            //    name: "IX_ProctorConfigurations_ExamScheduleId",
            //    table: "ProctorConfigurations",
            //    column: "ExamScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateProctorLogs");

            migrationBuilder.DropTable(
                name: "EnforcementActions");

            migrationBuilder.DropTable(
                name: "ProctorConfigurations");

            migrationBuilder.DropTable(
                name: "ProctorMeTrackers");

            migrationBuilder.DropTable(
                name: "EnforcementModes");
        }
    }
}
