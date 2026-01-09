using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModiyQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Answer",
                table: "QuestionBanks");

            migrationBuilder.RenameColumn(
                name: "Option",
                table: "QuestionOptions",
                newName: "OptionTag");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "QuestionOptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrectOption",
                table: "QuestionOptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OptionDescription",
                table: "QuestionOptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMultipleChoice",
                table: "QuestionBanks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "QuestionOptions");

            migrationBuilder.DropColumn(
                name: "IsCorrectOption",
                table: "QuestionOptions");

            migrationBuilder.DropColumn(
                name: "OptionDescription",
                table: "QuestionOptions");

            migrationBuilder.DropColumn(
                name: "IsMultipleChoice",
                table: "QuestionBanks");

            migrationBuilder.RenameColumn(
                name: "OptionTag",
                table: "QuestionOptions",
                newName: "Option");

            migrationBuilder.AddColumn<string>(
                name: "Answer",
                table: "QuestionBanks",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
