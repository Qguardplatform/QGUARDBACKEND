using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class addinstIdtoalltable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "FacultyPrograms",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "ExamQuestions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "Departments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "CourseTutors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "CourseLevels",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "CourseDepartments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "CandidatesCurrentStates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "CandidateExamsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacultyPrograms_InstitutionId",
                table: "FacultyPrograms",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_InstitutionId",
                table: "ExamQuestions",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_InstitutionId",
                table: "Departments",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTutors_InstitutionId",
                table: "CourseTutors",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLevels_InstitutionId",
                table: "CourseLevels",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDepartments_InstitutionId",
                table: "CourseDepartments",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidatesCurrentStates_InstitutionId",
                table: "CandidatesCurrentStates",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamsSubmissions_InstitutionId",
                table: "CandidateExamsSubmissions",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamsSubmissions_Institutions_InstitutionId",
                table: "CandidateExamsSubmissions",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidatesCurrentStates_Institutions_InstitutionId",
                table: "CandidatesCurrentStates",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseDepartments_Institutions_InstitutionId",
                table: "CourseDepartments",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseLevels_Institutions_InstitutionId",
                table: "CourseLevels",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTutors_Institutions_InstitutionId",
                table: "CourseTutors",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Institutions_InstitutionId",
                table: "Departments",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamQuestions_Institutions_InstitutionId",
                table: "ExamQuestions",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacultyPrograms_Institutions_InstitutionId",
                table: "FacultyPrograms",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamsSubmissions_Institutions_InstitutionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidatesCurrentStates_Institutions_InstitutionId",
                table: "CandidatesCurrentStates");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseDepartments_Institutions_InstitutionId",
                table: "CourseDepartments");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseLevels_Institutions_InstitutionId",
                table: "CourseLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseTutors_Institutions_InstitutionId",
                table: "CourseTutors");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Institutions_InstitutionId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamQuestions_Institutions_InstitutionId",
                table: "ExamQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_FacultyPrograms_Institutions_InstitutionId",
                table: "FacultyPrograms");

            migrationBuilder.DropIndex(
                name: "IX_FacultyPrograms_InstitutionId",
                table: "FacultyPrograms");

            migrationBuilder.DropIndex(
                name: "IX_ExamQuestions_InstitutionId",
                table: "ExamQuestions");

            migrationBuilder.DropIndex(
                name: "IX_Departments_InstitutionId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CourseTutors_InstitutionId",
                table: "CourseTutors");

            migrationBuilder.DropIndex(
                name: "IX_CourseLevels_InstitutionId",
                table: "CourseLevels");

            migrationBuilder.DropIndex(
                name: "IX_CourseDepartments_InstitutionId",
                table: "CourseDepartments");

            migrationBuilder.DropIndex(
                name: "IX_CandidatesCurrentStates_InstitutionId",
                table: "CandidatesCurrentStates");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamsSubmissions_InstitutionId",
                table: "CandidateExamsSubmissions");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "FacultyPrograms");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "ExamQuestions");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "CourseTutors");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "CourseLevels");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "CourseDepartments");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "CandidatesCurrentStates");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "CandidateExamsSubmissions");
        }
    }
}
