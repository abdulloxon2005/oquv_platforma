using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class Qurilma
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Foydalanuvchi")]
        public int FoydalanuvchiId { get; set; }

        [Required]
        public string Token { get; set; }  // FCM token

        [Required]
        public string Platforma { get; set; } = "android"; // yoki ios

        public DateTime OxirgiFoydalanish { get; set; } = DateTime.Now;

        public bool Aktiv { get; set; } = true;

        public Foydalanuvchi Foydalanuvchi { get; set; }
    }
}
