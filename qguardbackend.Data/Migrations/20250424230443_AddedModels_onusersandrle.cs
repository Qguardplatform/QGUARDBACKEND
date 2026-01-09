using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedModels_onusersandrle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_ApplicationUserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_LeveDepartmentCourses_Courses_CourseId",
                table: "LeveDepartmentCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_LeveDepartmentCourses_Departments_DepartmentId",
                table: "LeveDepartmentCourses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeveDepartmentCourses",
                table: "LeveDepartmentCourses");

            migrationBuilder.RenameTable(
                name: "LeveDepartmentCourses",
                newName: "DepartmentCourses");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "AspNetUserRoles",
                newName: "UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_ApplicationUserId",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_LeveDepartmentCourses_DepartmentId",
                table: "DepartmentCourses",
                newName: "IX_DepartmentCourses_DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_LeveDepartmentCourses_CourseId",
                table: "DepartmentCourses",
                newName: "IX_DepartmentCourses_CourseId");

            migrationBuilder.AddColumn<string>(
                name: "RoleId1",
                table: "AspNetUserRoles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DepartmentCourses",
                table: "DepartmentCourses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId1",
                table: "AspNetUserRoles",
                column: "RoleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId1",
                table: "AspNetUserRoles",
                column: "RoleId1",
                principalTable: "AspNetRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId1",
                table: "AspNetUserRoles",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentCourses_Courses_CourseId",
                table: "DepartmentCourses",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentCourses_Departments_DepartmentId",
                table: "DepartmentCourses",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentCourses_Courses_CourseId",
                table: "DepartmentCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentCourses_Departments_DepartmentId",
                table: "DepartmentCourses");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DepartmentCourses",
                table: "DepartmentCourses");

            migrationBuilder.DropColumn(
                name: "RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "DepartmentCourses",
                newName: "LeveDepartmentCourses");

            migrationBuilder.RenameColumn(
                name: "UserId1",
                table: "AspNetUserRoles",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_UserId1",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_DepartmentCourses_DepartmentId",
                table: "LeveDepartmentCourses",
                newName: "IX_LeveDepartmentCourses_DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_DepartmentCourses_CourseId",
                table: "LeveDepartmentCourses",
                newName: "IX_LeveDepartmentCourses_CourseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeveDepartmentCourses",
                table: "LeveDepartmentCourses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_ApplicationUserId",
                table: "AspNetUserRoles",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeveDepartmentCourses_Courses_CourseId",
                table: "LeveDepartmentCourses",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeveDepartmentCourses_Departments_DepartmentId",
                table: "LeveDepartmentCourses",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }
    }
}
