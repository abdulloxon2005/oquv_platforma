using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class Sertifikat
    {
        public int Id { get; set; }

        [Required]
        public int TalabaId { get; set; }

        [Required]
        public int ImtihonId { get; set; }

        [Required]
        public int KursId { get; set; }

        [Required]
        [StringLength(100)]
        public string SertifikatRaqami { get; set; }

        [Required]
        [StringLength(200)]
        public string SertifikatNomi { get; set; }

        [Display(Name = "Berilgan sana")]
        [DataType(DataType.Date)]
        public DateTime BerilganSana { get; set; } = DateTime.Now;

        [Display(Name = "Yaroqlilik muddati")]
        [DataType(DataType.Date)]
        public DateTime? YaroqlilikMuddati { get; set; }

        [Display(Name = "Ball")]
        public decimal Ball { get; set; }

        [Display(Name = "Foiz")]
        public decimal Foiz { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Faol"; // Faol, Muddat o'tgan, Bekor qilingan

        [Display(Name = "Izoh")]
        public string? Izoh { get; set; }

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

        // Navigation properties
        public Foydalanuvchi Talaba { get; set; }
        public Imtihon Imtihon { get; set; }
        public Kurs Kurs { get; set; }
    }
}


