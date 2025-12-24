using System.ComponentModel.DataAnnotations;

namespace talim_platforma.Models.ViewModels;

public class StudentProfileUpdateViewModel
{
    public Foydalanuvchi User { get; set; } = null!;

    [Display(Name = "Telefon raqami")]
    [Required(ErrorMessage = "Telefon raqami majburiy")]
    public string TelefonRaqam { get; set; } = string.Empty;

    [Display(Name = "Joriy parol")]
    [DataType(DataType.Password)]
    public string? OldPassword { get; set; }

    [Display(Name = "Yangi parol")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Parol kamida 6 ta belgi bo'lishi kerak")]
    public string? NewPassword { get; set; }

    [Display(Name = "Parolni tasdiqlang")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Parollar mos emas")]
    public string? ConfirmPassword { get; set; }
}

