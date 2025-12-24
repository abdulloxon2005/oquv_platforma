using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class ImtihonNatija
    {
        public int Id { get; set; }
        public int ImtihonId { get; set; }
        public int TalabaId { get; set; }

        [Display(Name = "Umumiy ball")]
        public int UmumiyBall { get; set; }

        [Display(Name = "Maksimal ball")]
        public int MaksimalBall { get; set; }

        [Display(Name = "Foiz natija")]
        public decimal FoizNatija { get; set; }

        [Display(Name = "O'tdimi?")]
        public bool Otdimi { get; set; }

        [Display(Name = "Imtihon berilgan sana")]
        public DateTime Sana { get; set; } = DateTime.Now;

        [Display(Name = "Boshlanish vaqti")]
        public DateTime? BoshlanishVaqti { get; set; }

        [Display(Name = "Tugash vaqti")]
        public DateTime? TugashVaqti { get; set; }

        [Display(Name = "Javoblar (JSON)")]
        public string? JavoblarJson { get; set; } // Talabaning javoblarini saqlash uchun

        [Display(Name = "Izoh")]
        public string? Izoh { get; set; }

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

        // Navigation properties
        public Imtihon Imtihon { get; set; }
        public Foydalanuvchi Talaba { get; set; }
    }
}