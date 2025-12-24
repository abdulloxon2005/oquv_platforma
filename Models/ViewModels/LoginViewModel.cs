using System.ComponentModel.DataAnnotations;

namespace talim_platforma.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Login kiritilishi shart")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Parol kiritilishi shart")]
        [DataType(DataType.Password)]
        public string Parol { get; set; }

        public bool RememberMe { get; set; }
    }
}
