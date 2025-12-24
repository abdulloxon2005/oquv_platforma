using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTolovTuriFromTolovlar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TolovTuri",
                table: "Tolovlar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TolovTuri",
                table: "Tolovlar",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
