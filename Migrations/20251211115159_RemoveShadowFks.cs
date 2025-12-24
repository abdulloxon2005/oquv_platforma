using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove shadow FK columns if they exist
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_ImtihonNatijalar_Imtihonlar_ImtihonId1",
                    table: "ImtihonNatijalar");
            }
            catch { }

            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_TalabaKurslar_Kurslar_KursId1",
                    table: "TalabaKurslar");
            }
            catch { }

            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_TalabaKurslar_Foydalanuvchilar_FoydalanuvchiId",
                    table: "TalabaKurslar");
            }
            catch { }

            try
            {
                migrationBuilder.DropIndex(
                    name: "IX_ImtihonNatijalar_ImtihonId1",
                    table: "ImtihonNatijalar");
            }
            catch { }

            try
            {
                migrationBuilder.DropIndex(
                    name: "IX_TalabaKurslar_KursId1",
                    table: "TalabaKurslar");
            }
            catch { }

            try
            {
                migrationBuilder.DropColumn(
                    name: "ImtihonId1",
                    table: "ImtihonNatijalar");
            }
            catch { }

            try
            {
                migrationBuilder.DropColumn(
                    name: "KursId1",
                    table: "TalabaKurslar");
            }
            catch { }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op rollback: columns were cleanup only
        }
    }
}
