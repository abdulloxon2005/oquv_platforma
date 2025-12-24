using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Faolmi",
                table: "Foydalanuvchilar",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DavomatSabablar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DavomatId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sabab = table.Column<string>(type: "TEXT", nullable: false),
                    Sana = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DavomatSabablar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DavomatSabablar_Davomatlar_DavomatId",
                        column: x => x.DavomatId,
                        principalTable: "Davomatlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DavomatSabablar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DavomatSabablar_DavomatId",
                table: "DavomatSabablar",
                column: "DavomatId");

            migrationBuilder.CreateIndex(
                name: "IX_DavomatSabablar_TalabaId",
                table: "DavomatSabablar",
                column: "TalabaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DavomatSabablar");

            migrationBuilder.DropColumn(
                name: "Faolmi",
                table: "Foydalanuvchilar");
        }
    }
}
