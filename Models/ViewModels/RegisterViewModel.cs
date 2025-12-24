using System.ComponentModel.DataAnnotations;

namespace talim_platforma.ViewModels
{
    public class RegisterViewModel
    {
        [Required] public string Ism { get; set; }
        [Required] public string Familiya { get; set; }
        public string OtasiningIsmi { get; set; }
        [Required] public string TelefonRaqam { get; set; }
        [Required] public string Login { get; set; }
        [Required, DataType(DataType.Password)] public string Parol { get; set; }
    }
}
