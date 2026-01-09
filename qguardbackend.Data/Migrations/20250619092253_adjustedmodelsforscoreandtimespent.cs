using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qguardbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class adjustedmodelsforscoreandtimespent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterColumn<long>(
            //    name: "TimeSpent",
            //    table: "CandidateExamsSubmissions",
            //    type: "bigint",
            //    nullable: false,
            //    oldClrType: typeof(DateTime),
            //    oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TimeSpent",
                table: "CandidateExamsSubmissions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
