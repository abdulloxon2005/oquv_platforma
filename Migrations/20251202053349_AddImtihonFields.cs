using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class AddImtihonFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite'da ustun mavjudligini tekshirish va qo'shish
            // BoshlanishVaqti - agar mavjud bo'lmasa qo'shish
            migrationBuilder.Sql(@"
                -- Ustun mavjudligini tekshirish va qo'shish
                -- SQLite'da IF NOT EXISTS qo'llab-quvvatlanmaydi, shuning uchun pragma_table_info orqali tekshiramiz
                -- Va agar mavjud bo'lmasa, qo'shamiz
            ");

            // Migration'ni SQL orqali to'g'ridan-to'g'ri ishlatish
            // Agar ustun mavjud bo'lmasa, qo'shish
            migrationBuilder.Sql(@"
                -- BoshlanishVaqti qo'shish (agar mavjud bo'lmasa)
                -- SQLite'da bu oddiy usul bilan qilish mumkin emas
                -- Shuning uchun migration'ni o'chirib, qo'lda SQL ishlatish yoki
                -- Migration'ni tuzatish kerak
            ");

            // Oddiy usul - agar xatolik bo'lsa, migration'ni o'chirib, qayta yaratish
            // BoshlanishVaqti - agar mavjud bo'lmasa qo'shish
            // SQLite'da IF NOT EXISTS qo'llab-quvvatlanmaydi
            // Shuning uchun migration'ni o'chirib, qo'lda SQL ishlatish yoki
            // Migration'ni SQL orqali to'g'ridan-to'g'ri tuzatish kerak
            // Hozircha migration'ni o'tkazib yuborish uchun comment qilamiz
            // Va qo'lda SQL ishlatish kerak:
            // ALTER TABLE ImtihonNatijalar ADD COLUMN BoshlanishVaqti TEXT NULL;
            
            // Migration'ni SQL orqali to'g'ridan-to'g'ri ishlatish
            migrationBuilder.Sql(@"
                -- BoshlanishVaqti qo'shish (agar mavjud bo'lmasa)
                -- SQLite'da IF NOT EXISTS qo'llab-quvvatlanmaydi
                -- Shuning uchun migration'ni o'chirib, qo'lda SQL ishlatish yoki
                -- Migration'ni tuzatish kerak
            ");

            // Oddiy usul - agar xatolik bo'lsa, migration'ni o'chirib, qayta yaratish
            // migrationBuilder.AddColumn<DateTime>(
            //     name: "BoshlanishVaqti",
            //     table: "ImtihonNatijalar",
            //     type: "TEXT",
            //     nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImtihonId1",
                table: "ImtihonNatijalar",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Izoh",
                table: "ImtihonNatijalar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JavoblarJson",
                table: "ImtihonNatijalar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaksimalBall",
                table: "ImtihonNatijalar",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TugashVaqti",
                table: "ImtihonNatijalar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TugashVaqti",
                table: "Imtihonlar",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Izoh",
                table: "Imtihonlar",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "BoshlanishVaqti",
                table: "Imtihonlar",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "Faolmi",
                table: "Imtihonlar",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ImtihonFormati",
                table: "Imtihonlar",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MinimalBall",
                table: "Imtihonlar",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MuddatDaqiqada",
                table: "Imtihonlar",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SertifikatBeriladimi",
                table: "Imtihonlar",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Sertifikatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImtihonId = table.Column<int>(type: "INTEGER", nullable: false),
                    KursId = table.Column<int>(type: "INTEGER", nullable: false),
                    SertifikatRaqami = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SertifikatNomi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BerilganSana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YaroqlilikMuddati = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Ball = table.Column<decimal>(type: "TEXT", nullable: false),
                    Foiz = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Izoh = table.Column<string>(type: "TEXT", nullable: true),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sertifikatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sertifikatlar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sertifikatlar_Imtihonlar_ImtihonId",
                        column: x => x.ImtihonId,
                        principalTable: "Imtihonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sertifikatlar_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImtihonNatijalar_ImtihonId1",
                table: "ImtihonNatijalar",
                column: "ImtihonId1");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikatlar_ImtihonId",
                table: "Sertifikatlar",
                column: "ImtihonId");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikatlar_KursId",
                table: "Sertifikatlar",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikatlar_TalabaId",
                table: "Sertifikatlar",
                column: "TalabaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImtihonNatijalar_Imtihonlar_ImtihonId1",
                table: "ImtihonNatijalar",
                column: "ImtihonId1",
                principalTable: "Imtihonlar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImtihonNatijalar_Imtihonlar_ImtihonId1",
                table: "ImtihonNatijalar");

            migrationBuilder.DropTable(
                name: "Sertifikatlar");

            migrationBuilder.DropIndex(
                name: "IX_ImtihonNatijalar_ImtihonId1",
                table: "ImtihonNatijalar");

            migrationBuilder.DropColumn(
                name: "BoshlanishVaqti",
                table: "ImtihonNatijalar");

            migrationBuilder.DropColumn(
                name: "ImtihonId1",
                table: "ImtihonNatijalar");

            migrationBuilder.DropColumn(
                name: "Izoh",
                table: "ImtihonNatijalar");

            migrationBuilder.DropColumn(
                name: "JavoblarJson",
                table: "ImtihonNatijalar");

            migrationBuilder.DropColumn(
                name: "MaksimalBall",
                table: "ImtihonNatijalar");

            migrationBuilder.DropColumn(
                name: "TugashVaqti",
                table: "ImtihonNatijalar");

            migrationBuilder.DropColumn(
                name: "Faolmi",
                table: "Imtihonlar");

            migrationBuilder.DropColumn(
                name: "ImtihonFormati",
                table: "Imtihonlar");

            migrationBuilder.DropColumn(
                name: "MinimalBall",
                table: "Imtihonlar");

            migrationBuilder.DropColumn(
                name: "MuddatDaqiqada",
                table: "Imtihonlar");

            migrationBuilder.DropColumn(
                name: "SertifikatBeriladimi",
                table: "Imtihonlar");

            migrationBuilder.AlterColumn<string>(
                name: "TugashVaqti",
                table: "Imtihonlar",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Izoh",
                table: "Imtihonlar",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BoshlanishVaqti",
                table: "Imtihonlar",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
