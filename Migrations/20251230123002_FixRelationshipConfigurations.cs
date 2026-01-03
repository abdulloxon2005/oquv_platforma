using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationshipConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArxivlanganSana",
                table: "Foydalanuvchilar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tangacha",
                table: "Foydalanuvchilar",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BahoFoiz",
                table: "Davomatlar",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArxivlanganSana",
                table: "Foydalanuvchilar");

            migrationBuilder.DropColumn(
                name: "Tangacha",
                table: "Foydalanuvchilar");

            migrationBuilder.DropColumn(
                name: "BahoFoiz",
                table: "Davomatlar");
        }
    }
}
