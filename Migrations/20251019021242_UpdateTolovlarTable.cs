using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTolovlarTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Summa",
                table: "Tolovlar",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<decimal>(
                name: "Haqdorlik",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Qarzdorlik",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Haqdorlik",
                table: "Tolovlar");

            migrationBuilder.DropColumn(
                name: "Qarzdorlik",
                table: "Tolovlar");

            migrationBuilder.AlterColumn<decimal>(
                name: "Summa",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
