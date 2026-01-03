using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class KursApplication
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Ismni kiriting")]
        [StringLength(50)]
        public string Ism { get; set; }
        
        [Required(ErrorMessage = "Familiyani kiriting")]
        [StringLength(50)]
        public string Familiya { get; set; }
        
        [Required(ErrorMessage = "Telefon raqamni kiriting")]
        [Phone(ErrorMessage = "Telefon raqam noto'g'ri")]
        [StringLength(20)]
        public string Telefon { get; set; }
        
        public int? KursId { get; set; }
        
        [ForeignKey("KursId")]
        public virtual Kurs? Kurs { get; set; }
        
        public DateTime Sana { get; set; } = DateTime.Now;
        
        public bool Faol { get; set; } = true;
        
        public string? Izoh { get; set; }
    }
}
