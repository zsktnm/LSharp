using System.ComponentModel.DataAnnotations;

namespace LSharp.Client.Common
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        
        public StrongPasswordAttribute() 
        {
            ErrorMessage = "Пароль должен содержать буквы в нижнем и верхнем регистре, цифры и спец. символы";
        }

        public override bool IsValid(object? value)
        {
            string password = value?.ToString() ?? "";
            bool hasDigit = password.Any(char.IsDigit);
            bool hasUppercase = password.Any(char.IsUpper);
            bool hasLowercase = password.Any(char.IsLower);
            bool hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));
            return hasDigit && hasUppercase && hasLowercase && hasSymbol;
        }
    }
}
