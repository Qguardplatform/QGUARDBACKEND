using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class adjustexamsschdulequestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "Instruction",
                table: "ExamSchedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MakeQuestionsAppearRandom",
                table: "ExamSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "QuestionBankId",
                table: "ExamQuestions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_QuestionBankId",
                table: "ExamQuestions",
                column: "QuestionBankId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamQuestions_QuestionBanks_QuestionBankId",
                table: "ExamQuestions",
                column: "QuestionBankId",
                principalTable: "QuestionBanks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamQuestions_QuestionBanks_QuestionBankId",
                table: "ExamQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ExamQuestions_QuestionBankId",
                table: "ExamQuestions");

            migrationBuilder.DropColumn(
                name: "Instruction",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "MakeQuestionsAppearRandom",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "QuestionBankId",
                table: "ExamQuestions");

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamQuestionId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionBankId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_ExamQuestions_ExamQuestionId",
                        column: x => x.ExamQuestionId,
                        principalTable: "ExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Questions_QuestionBanks_QuestionBankId",
                        column: x => x.QuestionBankId,
                        principalTable: "QuestionBanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamQuestionId",
                table: "Questions",
                column: "ExamQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionBankId",
                table: "Questions",
                column: "QuestionBankId");
        }
    }
}
