using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTolovModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Haqdorlik",
                table: "Tolovlar");

            migrationBuilder.DropColumn(
                name: "Holati",
                table: "Tolovlar");

            migrationBuilder.DropColumn(
                name: "OyliklarSoni",
                table: "Tolovlar");

            migrationBuilder.DropColumn(
                name: "Qarzdorlik",
                table: "Tolovlar");

            migrationBuilder.DropColumn(
                name: "TolovSana",
                table: "Tolovlar");

            migrationBuilder.RenameColumn(
                name: "YaratilganVaqt",
                table: "Tolovlar",
                newName: "TolovUsuli");

            migrationBuilder.RenameColumn(
                name: "YangilanganVaqt",
                table: "Tolovlar",
                newName: "Sana");

            migrationBuilder.RenameColumn(
                name: "Summa",
                table: "Tolovlar",
                newName: "Miqdor");

            migrationBuilder.AddColumn<int>(
                name: "TalabaKursId",
                table: "Tolovlar",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tolovlar_TalabaKursId",
                table: "Tolovlar",
                column: "TalabaKursId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tolovlar_TalabaKurslar_TalabaKursId",
                table: "Tolovlar",
                column: "TalabaKursId",
                principalTable: "TalabaKurslar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tolovlar_TalabaKurslar_TalabaKursId",
                table: "Tolovlar");

            migrationBuilder.DropIndex(
                name: "IX_Tolovlar_TalabaKursId",
                table: "Tolovlar");

            migrationBuilder.DropColumn(
                name: "TalabaKursId",
                table: "Tolovlar");

            migrationBuilder.RenameColumn(
                name: "TolovUsuli",
                table: "Tolovlar",
                newName: "YaratilganVaqt");

            migrationBuilder.RenameColumn(
                name: "Sana",
                table: "Tolovlar",
                newName: "YangilanganVaqt");

            migrationBuilder.RenameColumn(
                name: "Miqdor",
                table: "Tolovlar",
                newName: "Summa");

            migrationBuilder.AddColumn<decimal>(
                name: "Haqdorlik",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Holati",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OyliklarSoni",
                table: "Tolovlar",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Qarzdorlik",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "TolovSana",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
