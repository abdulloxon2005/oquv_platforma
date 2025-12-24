using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class Tolov
    {
        public int Id { get; set; }

        [Required]
        public int TalabaId { get; set; }
        [ForeignKey("TalabaId")]
        public Foydalanuvchi Talaba { get; set; }

        [Required]
        public int KursId { get; set; }
        [ForeignKey("KursId")]
        public Kurs Kurs { get; set; }

        // To'langan summa
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Miqdor { get; set; }

        [Required]
        public DateTime Sana { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string TolovUsuli { get; set; }  // Naqd, Click, Payme, Humo, Karta va h.k.

        // Talaba bilan kurs bog'lanishi (ixtiyoriy)
        public int? TalabaKursId { get; set; }
        [ForeignKey("TalabaKursId")]
        public TalabaKurs? TalabaKurs { get; set; }

        // Avtomatik hisoblangan maydonlar
        [Column(TypeName = "decimal(18,2)")]
        public decimal Qarzdorlik { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Haqdorlik { get; set; } = 0;

        [MaxLength(100)]
        public string Holat { get; set; } = "Hisoblanmoqda";

        [MaxLength(50)]
        public string ChekRaqami { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10);
    }
}
