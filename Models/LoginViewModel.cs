using System.ComponentModel.DataAnnotations;

namespace TalimPlatformasi.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Login kiritilishi shart")]
        [Display(Name = "Login")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Parol kiritilishi shart")]
        [DataType(DataType.Password)]
        [Display(Name = "Parol")]
        public string Parol { get; set; }
        
    }
}
