using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class deptexamschedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSchedules_Departments_DepartmentId",
                table: "ExamSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ExamSchedules_DepartmentId",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ExamSchedules");

            migrationBuilder.CreateTable(
                name: "DepartmentExamSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    ExamScheduleId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentExamSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentExamSchedules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DepartmentExamSchedules_ExamSchedules_ExamScheduleId",
                        column: x => x.ExamScheduleId,
                        principalTable: "ExamSchedules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentExamSchedules_DepartmentId",
                table: "DepartmentExamSchedules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentExamSchedules_ExamScheduleId",
                table: "DepartmentExamSchedules",
                column: "ExamScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentExamSchedules");

            migrationBuilder.AddColumn<long>(
                name: "DepartmentId",
                table: "ExamSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedules_DepartmentId",
                table: "ExamSchedules",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSchedules_Departments_DepartmentId",
                table: "ExamSchedules",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }
    }
}
