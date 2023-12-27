using System.ComponentModel.DataAnnotations;

namespace LSharp.Client.ViewModels
{
    public class LoginViewModel
    {
        [EmailAddress(ErrorMessage = "Введите действительный email-адрес")]
        [Required(ErrorMessage = "Укажите Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль")]
        public string Password { get; set; } = string.Empty;
    }
}
