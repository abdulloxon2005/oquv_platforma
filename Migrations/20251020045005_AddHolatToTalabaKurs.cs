using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class AddHolatToTalabaKurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Aktivmi",
                table: "TalabaKurslar",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FoydalanuvchiId",
                table: "TalabaKurslar",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KursId1",
                table: "TalabaKurslar",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TalabaKurslar_FoydalanuvchiId",
                table: "TalabaKurslar",
                column: "FoydalanuvchiId");

            migrationBuilder.CreateIndex(
                name: "IX_TalabaKurslar_KursId1",
                table: "TalabaKurslar",
                column: "KursId1");

            migrationBuilder.AddForeignKey(
                name: "FK_TalabaKurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "TalabaKurslar",
                column: "FoydalanuvchiId",
                principalTable: "Foydalanuvchilar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TalabaKurslar_Kurslar_KursId1",
                table: "TalabaKurslar",
                column: "KursId1",
                principalTable: "Kurslar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TalabaKurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "TalabaKurslar");

            migrationBuilder.DropForeignKey(
                name: "FK_TalabaKurslar_Kurslar_KursId1",
                table: "TalabaKurslar");

            migrationBuilder.DropIndex(
                name: "IX_TalabaKurslar_FoydalanuvchiId",
                table: "TalabaKurslar");

            migrationBuilder.DropIndex(
                name: "IX_TalabaKurslar_KursId1",
                table: "TalabaKurslar");

            migrationBuilder.DropColumn(
                name: "Aktivmi",
                table: "TalabaKurslar");

            migrationBuilder.DropColumn(
                name: "FoydalanuvchiId",
                table: "TalabaKurslar");

            migrationBuilder.DropColumn(
                name: "KursId1",
                table: "TalabaKurslar");
        }
    }
}
