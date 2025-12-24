using System.ComponentModel.DataAnnotations;

namespace talim_platforma.Models.ViewModels
{
    public class ImtihonSavolViewModel
    {
        public int Id { get; set; }
        public int ImtihonId { get; set; }

        [Required(ErrorMessage = "Savol matni kiritilishi shart")]
        [Display(Name = "Savol matni")]
        public string SavolMatni { get; set; }

        [Required(ErrorMessage = "Variant A kiritilishi shart")]
        [Display(Name = "Variant A")]
        public string VariantA { get; set; }

        [Required(ErrorMessage = "Variant B kiritilishi shart")]
        [Display(Name = "Variant B")]
        public string VariantB { get; set; }

        [Required(ErrorMessage = "Variant C kiritilishi shart")]
        [Display(Name = "Variant C")]
        public string VariantC { get; set; }

        [Required(ErrorMessage = "Variant D kiritilishi shart")]
        [Display(Name = "Variant D")]
        public string VariantD { get; set; }

        [Required(ErrorMessage = "To'g'ri javob tanlanishi shart")]
        [Display(Name = "To'g'ri javob")]
        public string TogriJavob { get; set; } // A, B, C, yoki D

        [Required(ErrorMessage = "Ball qiymati kiritilishi shart")]
        [Display(Name = "Ball qiymati")]
        [Range(1, 100, ErrorMessage = "Ball qiymati 1 dan 100 gacha bo'lishi kerak")]
        public int BallQiymati { get; set; } = 1;
    }
}


