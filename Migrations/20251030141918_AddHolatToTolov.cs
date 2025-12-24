using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class AddHolatToTolov : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Haqdorlik",
                table: "Tolovlar",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Holat",
                table: "Tolovlar",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Qarzdorlik",
                table: "Tolovlar",
                type: "decimal(18,2)",
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
                name: "Holat",
                table: "Tolovlar");

            migrationBuilder.DropColumn(
                name: "Qarzdorlik",
                table: "Tolovlar");
        }
    }
}
