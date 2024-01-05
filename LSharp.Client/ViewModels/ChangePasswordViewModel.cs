using LSharp.Client.Common;
using LSharp.Client.DataTransfer;
using System.ComponentModel.DataAnnotations;

namespace LSharp.Client.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Введите старый пароль")]
        public string OldPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Укажите пароль")]
        [StrongPassword(ErrorMessage = "Укажите надежный пароль")]
        public string Password { get; set; } = string.Empty;


        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
        public string RepeatPassword { get; set; } = string.Empty;

        public ChangePasswordDTO ToDto() => 
            new ChangePasswordDTO { OldPassword = OldPassword, NewPassword = Password };
    }
}
