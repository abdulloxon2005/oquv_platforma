using System.ComponentModel.DataAnnotations;

namespace talim_platforma.ViewModels
{
    public class FoydalanuvchiTahrirlashViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ism kiritilishi shart")]
        public string Ism { get; set; }

        [Required(ErrorMessage = "Familiya kiritilishi shart")]
        public string Familiya { get; set; }

        public string? OtasiningIsmi { get; set; }

        // Telefon: minimal 9, maksimal 13 belgidan iborat (o'zgartiring agar boshqa talablaringiz bo'lsa)
        [Phone(ErrorMessage = "Telefon raqam noto‘g‘ri formatda")]
        [StringLength(13, MinimumLength = 9, ErrorMessage = "Telefon raqam {2} dan {1} gacha belgidan bo‘lishi kerak")]
        public string? TelefonRaqam { get; set; }

        // Login: 4-20 belgi, faqat harf va raqam, va kamida 1 ta harf bo‘lsin
        [RegularExpression(@"^(?=.*[A-Za-z])[A-Za-z0-9]{4,20}$",
            ErrorMessage = "Login 4–20 ta belgidan iborat bo‘lsin; kamida bitta harf va faqat harf/raqam ruxsat etiladi")]
        public string? Login { get; set; }   // tahrirlashda ixtiyoriy, create da Required qo'yasiz

        // Parol: agar kiritsa — kamida 6 belgidan va kamida bitta harf va bitta raqam bo'lsin
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{6,}$",
            ErrorMessage = "Parol kamida 6 belgidan bo‘lsin va kamida bitta harf va bitta raqam bo‘lishi kerak")]
        public string? Parol { get; set; }   // tahrirlashda ixtiyoriy

        [Required(ErrorMessage = "Rol tanlanishi kerak")]
        public string Rol { get; set; }
    }
}
