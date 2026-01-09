using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class adjustaddedtutorentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseTutors_Tutor_TutorId",
                table: "CourseTutors");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Departments_DepartmentId",
                table: "Tutor");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Institutions_InstitutionId",
                table: "Tutor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tutor",
                table: "Tutor");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tutor");

            migrationBuilder.RenameTable(
                name: "Tutor",
                newName: "Tutors");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Tutors",
                newName: "TutorName");

            migrationBuilder.RenameIndex(
                name: "IX_Tutor_InstitutionId",
                table: "Tutors",
                newName: "IX_Tutors_InstitutionId");

            migrationBuilder.RenameIndex(
                name: "IX_Tutor_DepartmentId",
                table: "Tutors",
                newName: "IX_Tutors_DepartmentId");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Tutors",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tutors",
                table: "Tutors",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Tutors_ApplicationUserId",
                table: "Tutors",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTutors_Tutors_TutorId",
                table: "CourseTutors",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutors_AspNetUsers_ApplicationUserId",
                table: "Tutors",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutors_Departments_DepartmentId",
                table: "Tutors",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutors_Institutions_InstitutionId",
                table: "Tutors",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseTutors_Tutors_TutorId",
                table: "CourseTutors");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutors_AspNetUsers_ApplicationUserId",
                table: "Tutors");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutors_Departments_DepartmentId",
                table: "Tutors");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutors_Institutions_InstitutionId",
                table: "Tutors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tutors",
                table: "Tutors");

            migrationBuilder.DropIndex(
                name: "IX_Tutors_ApplicationUserId",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Tutors");

            migrationBuilder.RenameTable(
                name: "Tutors",
                newName: "Tutor");

            migrationBuilder.RenameColumn(
                name: "TutorName",
                table: "Tutor",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Tutors_InstitutionId",
                table: "Tutor",
                newName: "IX_Tutor_InstitutionId");

            migrationBuilder.RenameIndex(
                name: "IX_Tutors_DepartmentId",
                table: "Tutor",
                newName: "IX_Tutor_DepartmentId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tutor",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tutor",
                table: "Tutor",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTutors_Tutor_TutorId",
                table: "CourseTutors",
                column: "TutorId",
                principalTable: "Tutor",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Departments_DepartmentId",
                table: "Tutor",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Institutions_InstitutionId",
                table: "Tutor",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");
        }
    }
}
