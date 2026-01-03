using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;

namespace talim_platforma.Models
{
    public class Foydalanuvchi
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ism kiritilishi shart")]
        [Display(Name = "Ism")]
        public string Ism { get; set; }

        [Required(ErrorMessage = "Familiya kiritilishi shart")]
        [Display(Name = "Familiya")]
        public string Familiya { get; set; }

        [Display(Name = "Otasining ismi")]
        public string OtasiningIsmi { get; set; }

        [Required(ErrorMessage = "Telefon raqam kiritilishi shart")]
        [Phone(ErrorMessage = "Telefon raqam formati noto‘g‘ri")]
        [Display(Name = "Telefon raqami")]
        public string TelefonRaqam { get; set; }

        [Required(ErrorMessage = "Login kiritilishi shart")]
        [Display(Name = "Login")]
        // Remote attribute: AuthController ichidagi IsLoginAvailable actioniga so'rov yuboradi
        [Remote(action: "IsLoginAvailable", controller: "Auth", ErrorMessage = "Bu login allaqachon band")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Parol kiritilishi shart")]
        [Display(Name = "Parol")]
        public string Parol { get; set; }
        public string? ChatId { get; set; }
        [Display(Name = "Rol")]
        public string Rol { get; set; } = "User";
        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;
        public ICollection<Davomat>? OqituvchiDavomatlar { get; set; }
        public ICollection<Kurs>? Kurs { get; set; }
    // ✅ Talaba sifatida bo‘lgan davomatlar
        public ICollection<TalabaKurs>? TalabaKurslar { get; set; }
        public ICollection<Davomat>? TalabaDavomatlar { get; set; }
        public FoydalanuvchiToliqMalumoti? FoydalanuvchiToliqMalumoti { get; set; }
        public ICollection<Guruh>? Guruhlar { get; set; }
        //public string? DeviceToken { get; set; }
        public bool Faolmi { get; set; } = true;
        
        // Arxivlash uchun maydonlar
        public DateTime? ArxivlanganSana { get; set; }
        public bool Arxivlanganmi => ArxivlanganSana.HasValue;
        
        // Tangacha (o'quvchi uchun to'plangan tangacha miqdori)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tangacha { get; set; } = 0;
    }
}
