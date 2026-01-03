using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class AddKursApplicationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KursApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ism = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Familiya = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    KursId = table.Column<int>(type: "INTEGER", nullable: true),
                    Sana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Faol = table.Column<bool>(type: "INTEGER", nullable: false),
                    Izoh = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KursApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KursApplications_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_KursApplications_KursId",
                table: "KursApplications",
                column: "KursId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KursApplications");
        }
    }
}
