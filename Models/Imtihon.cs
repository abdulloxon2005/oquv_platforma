using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace talim_platforma.Models
{
    public class Imtihon
    {
        public int Id { get; set; }
        public int GuruhId { get; set; }
        public int OqituvchiId { get; set; }

        [Required(ErrorMessage = "Imtihon nomi kiritilishi shart")]
        [Display(Name = "Imtihon nomi")]
        public string Nomi { get; set; }

        [Required(ErrorMessage = "Imtihon turi tanlanishi shart")]
        [Display(Name = "Imtihon turi")]
        public string ImtihonTuri { get; set; } // haftalik, oylik, yakuniy

        [Required(ErrorMessage = "Imtihon formati tanlanishi shart")]
        [Display(Name = "Imtihon formati")]
        public string ImtihonFormati { get; set; } = "Offline"; // Online yoki Offline

        [Required(ErrorMessage = "Sana kiritilishi shart")]
        [Display(Name = "Sana")]
        [DataType(DataType.Date)]
        public DateTime Sana { get; set; } = DateTime.Now;

        [Display(Name = "Boshlanish vaqti")]
        public string? BoshlanishVaqti { get; set; }

        [Display(Name = "Tugash vaqti")]
        public string? TugashVaqti { get; set; }

        [Display(Name = "Muddat (daqiqa)")]
        public int? MuddatDaqiqada { get; set; }

        [Display(Name = "Minimal ball (%)")]
        public int MinimalBall { get; set; } = 60; // Sertifikat berish uchun minimal ball

        [Display(Name = "Sertifikat beriladimi?")]
        public bool SertifikatBeriladimi { get; set; } = false;

        [Display(Name = "Izoh")]
        public string? Izoh { get; set; }

        public bool Faolmi { get; set; } = true;

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

        // Navigation properties
        public Guruh Guruh { get; set; }
        public Foydalanuvchi Oqituvchi { get; set; }
        public ICollection<ImtihonSavol> Savollar { get; set; } = new List<ImtihonSavol>();
        public ICollection<ImtihonNatija> Natijalar { get; set; } = new List<ImtihonNatija>();
    }
}