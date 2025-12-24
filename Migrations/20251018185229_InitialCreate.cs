using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talim_platforma.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foydalanuvchilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ism = table.Column<string>(type: "TEXT", nullable: false),
                    Familiya = table.Column<string>(type: "TEXT", nullable: false),
                    OtasiningIsmi = table.Column<string>(type: "TEXT", nullable: false),
                    TelefonRaqam = table.Column<string>(type: "TEXT", nullable: false),
                    Login = table.Column<string>(type: "TEXT", nullable: false),
                    Parol = table.Column<string>(type: "TEXT", nullable: false),
                    ChatId = table.Column<string>(type: "TEXT", nullable: true),
                    Rol = table.Column<string>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foydalanuvchilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kurslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nomi = table.Column<string>(type: "TEXT", nullable: false),
                    Tavsif = table.Column<string>(type: "TEXT", nullable: false),
                    Darajasi = table.Column<string>(type: "TEXT", nullable: false),
                    Narxi = table.Column<decimal>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kurslar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoydalanuvchiToliqMalumotlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FoydalanuvchiId = table.Column<int>(type: "INTEGER", nullable: false),
                    QarindoshlikDarajasi = table.Column<string>(type: "TEXT", nullable: false),
                    QarindoshIsmi = table.Column<string>(type: "TEXT", nullable: false),
                    QarindoshFamilyasi = table.Column<string>(type: "TEXT", nullable: false),
                    QarindoshTelefonRaqami = table.Column<string>(type: "TEXT", nullable: false),
                    Manzil = table.Column<string>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoydalanuvchiToliqMalumotlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoydalanuvchiToliqMalumotlari_Foydalanuvchilar_FoydalanuvchiId",
                        column: x => x.FoydalanuvchiId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guruhlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nomi = table.Column<string>(type: "TEXT", nullable: false),
                    BoshlanishSanasi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DarsKunlari = table.Column<string>(type: "TEXT", nullable: false),
                    DarsVaqti = table.Column<string>(type: "TEXT", nullable: false),
                    Xona = table.Column<string>(type: "TEXT", nullable: false),
                    Holati = table.Column<string>(type: "TEXT", nullable: false),
                    KursId = table.Column<int>(type: "INTEGER", nullable: false),
                    OqituvchiId = table.Column<int>(type: "INTEGER", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FoydalanuvchiId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guruhlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guruhlar_Foydalanuvchilar_FoydalanuvchiId",
                        column: x => x.FoydalanuvchiId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Guruhlar_Foydalanuvchilar_OqituvchiId",
                        column: x => x.OqituvchiId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Guruhlar_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TalabaKurslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KursId = table.Column<int>(type: "INTEGER", nullable: false),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    QoshilganSana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Holati = table.Column<string>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalabaKurslar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalabaKurslar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TalabaKurslar_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tolovlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    KursId = table.Column<int>(type: "INTEGER", nullable: false),
                    Summa = table.Column<decimal>(type: "TEXT", nullable: false),
                    TolovTuri = table.Column<string>(type: "TEXT", nullable: false),
                    Holati = table.Column<string>(type: "TEXT", nullable: false),
                    TolovSana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tolovlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tolovlar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tolovlar_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Darslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuruhId = table.Column<int>(type: "INTEGER", nullable: false),
                    OqituvchiId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Mavzu = table.Column<string>(type: "TEXT", nullable: false),
                    DarsTuri = table.Column<string>(type: "TEXT", nullable: false),
                    Davomiylik = table.Column<string>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Darslar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Darslar_Foydalanuvchilar_OqituvchiId",
                        column: x => x.OqituvchiId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Darslar_Guruhlar_GuruhId",
                        column: x => x.GuruhId,
                        principalTable: "Guruhlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Imtihonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuruhId = table.Column<int>(type: "INTEGER", nullable: false),
                    OqituvchiId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nomi = table.Column<string>(type: "TEXT", nullable: false),
                    ImtihonTuri = table.Column<string>(type: "TEXT", nullable: false),
                    Sana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BoshlanishVaqti = table.Column<string>(type: "TEXT", nullable: false),
                    TugashVaqti = table.Column<string>(type: "TEXT", nullable: false),
                    Izoh = table.Column<string>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imtihonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Imtihonlar_Foydalanuvchilar_OqituvchiId",
                        column: x => x.OqituvchiId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Imtihonlar_Guruhlar_GuruhId",
                        column: x => x.GuruhId,
                        principalTable: "Guruhlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TalabaGuruhlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuruhId = table.Column<int>(type: "INTEGER", nullable: false),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    QoshilganSana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalabaGuruhlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalabaGuruhlar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TalabaGuruhlar_Guruhlar_GuruhId",
                        column: x => x.GuruhId,
                        principalTable: "Guruhlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Davomatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DarsId = table.Column<int>(type: "INTEGER", nullable: false),
                    GuruhId = table.Column<int>(type: "INTEGER", nullable: false),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    OqituvchiId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Holati = table.Column<string>(type: "TEXT", nullable: false),
                    Izoh = table.Column<string>(type: "TEXT", nullable: true),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Davomatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Davomatlar_Darslar_DarsId",
                        column: x => x.DarsId,
                        principalTable: "Darslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Davomatlar_Foydalanuvchilar_OqituvchiId",
                        column: x => x.OqituvchiId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Davomatlar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Davomatlar_Guruhlar_GuruhId",
                        column: x => x.GuruhId,
                        principalTable: "Guruhlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Baholar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DarsId = table.Column<int>(type: "INTEGER", nullable: true),
                    ImtihonId = table.Column<int>(type: "INTEGER", nullable: true),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ball = table.Column<int>(type: "INTEGER", nullable: false),
                    Izoh = table.Column<string>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baholar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Baholar_Darslar_DarsId",
                        column: x => x.DarsId,
                        principalTable: "Darslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Baholar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Baholar_Imtihonlar_ImtihonId",
                        column: x => x.ImtihonId,
                        principalTable: "Imtihonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImtihonNatijalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImtihonId = table.Column<int>(type: "INTEGER", nullable: false),
                    TalabaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UmumiyBall = table.Column<int>(type: "INTEGER", nullable: false),
                    FoizNatija = table.Column<decimal>(type: "TEXT", nullable: false),
                    Otdimi = table.Column<bool>(type: "INTEGER", nullable: false),
                    Sana = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImtihonNatijalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImtihonNatijalar_Foydalanuvchilar_TalabaId",
                        column: x => x.TalabaId,
                        principalTable: "Foydalanuvchilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImtihonNatijalar_Imtihonlar_ImtihonId",
                        column: x => x.ImtihonId,
                        principalTable: "Imtihonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImtihonSavollar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImtihonId = table.Column<int>(type: "INTEGER", nullable: false),
                    SavolMatni = table.Column<string>(type: "TEXT", nullable: false),
                    VariantA = table.Column<string>(type: "TEXT", nullable: false),
                    VariantB = table.Column<string>(type: "TEXT", nullable: false),
                    VariantC = table.Column<string>(type: "TEXT", nullable: false),
                    VariantD = table.Column<string>(type: "TEXT", nullable: false),
                    TogriJavob = table.Column<string>(type: "TEXT", nullable: false),
                    BallQiymati = table.Column<int>(type: "INTEGER", nullable: false),
                    YaratilganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YangilanganVaqt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImtihonSavollar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImtihonSavollar_Imtihonlar_ImtihonId",
                        column: x => x.ImtihonId,
                        principalTable: "Imtihonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Baholar_DarsId",
                table: "Baholar",
                column: "DarsId");

            migrationBuilder.CreateIndex(
                name: "IX_Baholar_ImtihonId",
                table: "Baholar",
                column: "ImtihonId");

            migrationBuilder.CreateIndex(
                name: "IX_Baholar_TalabaId",
                table: "Baholar",
                column: "TalabaId");

            migrationBuilder.CreateIndex(
                name: "IX_Darslar_GuruhId",
                table: "Darslar",
                column: "GuruhId");

            migrationBuilder.CreateIndex(
                name: "IX_Darslar_OqituvchiId",
                table: "Darslar",
                column: "OqituvchiId");

            migrationBuilder.CreateIndex(
                name: "IX_Davomatlar_DarsId",
                table: "Davomatlar",
                column: "DarsId");

            migrationBuilder.CreateIndex(
                name: "IX_Davomatlar_GuruhId",
                table: "Davomatlar",
                column: "GuruhId");

            migrationBuilder.CreateIndex(
                name: "IX_Davomatlar_OqituvchiId",
                table: "Davomatlar",
                column: "OqituvchiId");

            migrationBuilder.CreateIndex(
                name: "IX_Davomatlar_TalabaId",
                table: "Davomatlar",
                column: "TalabaId");

            migrationBuilder.CreateIndex(
                name: "IX_FoydalanuvchiToliqMalumotlari_FoydalanuvchiId",
                table: "FoydalanuvchiToliqMalumotlari",
                column: "FoydalanuvchiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guruhlar_FoydalanuvchiId",
                table: "Guruhlar",
                column: "FoydalanuvchiId");

            migrationBuilder.CreateIndex(
                name: "IX_Guruhlar_KursId",
                table: "Guruhlar",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_Guruhlar_OqituvchiId",
                table: "Guruhlar",
                column: "OqituvchiId");

            migrationBuilder.CreateIndex(
                name: "IX_Imtihonlar_GuruhId",
                table: "Imtihonlar",
                column: "GuruhId");

            migrationBuilder.CreateIndex(
                name: "IX_Imtihonlar_OqituvchiId",
                table: "Imtihonlar",
                column: "OqituvchiId");

            migrationBuilder.CreateIndex(
                name: "IX_ImtihonNatijalar_ImtihonId",
                table: "ImtihonNatijalar",
                column: "ImtihonId");

            migrationBuilder.CreateIndex(
                name: "IX_ImtihonNatijalar_TalabaId",
                table: "ImtihonNatijalar",
                column: "TalabaId");

            migrationBuilder.CreateIndex(
                name: "IX_ImtihonSavollar_ImtihonId",
                table: "ImtihonSavollar",
                column: "ImtihonId");

            migrationBuilder.CreateIndex(
                name: "IX_TalabaGuruhlar_GuruhId",
                table: "TalabaGuruhlar",
                column: "GuruhId");

            migrationBuilder.CreateIndex(
                name: "IX_TalabaGuruhlar_TalabaId",
                table: "TalabaGuruhlar",
                column: "TalabaId");

            migrationBuilder.CreateIndex(
                name: "IX_TalabaKurslar_KursId",
                table: "TalabaKurslar",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_TalabaKurslar_TalabaId",
                table: "TalabaKurslar",
                column: "TalabaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tolovlar_KursId",
                table: "Tolovlar",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_Tolovlar_TalabaId",
                table: "Tolovlar",
                column: "TalabaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Baholar");

            migrationBuilder.DropTable(
                name: "Davomatlar");

            migrationBuilder.DropTable(
                name: "FoydalanuvchiToliqMalumotlari");

            migrationBuilder.DropTable(
                name: "ImtihonNatijalar");

            migrationBuilder.DropTable(
                name: "ImtihonSavollar");

            migrationBuilder.DropTable(
                name: "TalabaGuruhlar");

            migrationBuilder.DropTable(
                name: "TalabaKurslar");

            migrationBuilder.DropTable(
                name: "Tolovlar");

            migrationBuilder.DropTable(
                name: "Darslar");

            migrationBuilder.DropTable(
                name: "Imtihonlar");

            migrationBuilder.DropTable(
                name: "Guruhlar");

            migrationBuilder.DropTable(
                name: "Foydalanuvchilar");

            migrationBuilder.DropTable(
                name: "Kurslar");
        }
    }
}
