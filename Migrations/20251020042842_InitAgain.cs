using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class InitAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "Kurslar");

            migrationBuilder.AlterColumn<int>(
                name: "FoydalanuvchiId",
                table: "Kurslar",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Kurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "Kurslar",
                column: "FoydalanuvchiId",
                principalTable: "Foydalanuvchilar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "Kurslar");

            migrationBuilder.AlterColumn<int>(
                name: "FoydalanuvchiId",
                table: "Kurslar",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kurslar_Foydalanuvchilar_FoydalanuvchiId",
                table: "Kurslar",
                column: "FoydalanuvchiId",
                principalTable: "Foydalanuvchilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
