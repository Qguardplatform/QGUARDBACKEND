using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedasystemadminothertenanttables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemAdminOtherTenantsRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAdminOtherTenantsRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemAdminOtherTenantsRole_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemAdminOtherTenantsRole_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemAdminOtherTenantsRole_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdminOtherTenantsRole_InstitutionId",
                table: "SystemAdminOtherTenantsRole",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdminOtherTenantsRole_RoleId",
                table: "SystemAdminOtherTenantsRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdminOtherTenantsRole_UserId",
                table: "SystemAdminOtherTenantsRole",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemAdminOtherTenantsRole");
        }
    }
}
