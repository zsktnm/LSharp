using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace LSharp.Client.ViewModels
{
    public class UserInfoViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя пользователя не может быть пустым")]
        [RegularExpression(@"[\w ]*", ErrorMessage = "Имя может содержать только буквы, цифры и пробелы")]
        [MinLength(1, ErrorMessage = "Имя не может быть пустым")]
        [MaxLength(36, ErrorMessage = "Имя не может превышать 36 символов")]
        public string UserName { get; set; } = string.Empty;


        public int Level { get; set; }
        public int Next { get; set; }
        public int Exp { get; set; }

    }
}
