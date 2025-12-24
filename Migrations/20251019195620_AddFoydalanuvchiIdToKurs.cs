using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class AddFoydalanuvchiIdToKurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FoydalanuvchiId",
                table: "Kurslar",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Kurslar_FoydalanuvchiId",
                table: "Kurslar",
                column: "FoydalanuvchiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "Kurslar",
                column: "FoydalanuvchiId",
                principalTable: "Foydalanuvchilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "Kurslar");

            migrationBuilder.DropIndex(
                name: "IX_Kurslar_FoydalanuvchiId",
                table: "Kurslar");

            migrationBuilder.DropColumn(
                name: "FoydalanuvchiId",
                table: "Kurslar");
        }
    }
}
