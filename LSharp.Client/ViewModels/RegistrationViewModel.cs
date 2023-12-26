using LSharp.Client.Common;
using System.ComponentModel.DataAnnotations;

namespace LSharp.Client.ViewModels
{
    public class RegisterAccountViewModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Укажите действительный Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(30, ErrorMessage = "Пароль должен сожержать не менее 8 символов", MinimumLength = 8)]
        [StrongPassword]
        public string Password { get; set; } = null!;

        [Required]
        [Compare(nameof(Password))]
        public string RepeatPassword { get; set; } = null!;
    }
}
