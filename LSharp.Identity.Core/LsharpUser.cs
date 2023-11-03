using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSharp.Identity.Core
{
    public class LsharpUser : IdentityUser
    {
        public void GenerateRefreshToken()
        {
            byte[] rawToken = new byte[100];
            Random.Shared.NextBytes(rawToken);
            RefreshToken = Convert.ToBase64String(rawToken);
        }

        public LsharpUser() {  }

        public LsharpUser(string email)
        {
            UserName = email;
            Email = email;
        }

        public string? RefreshToken { get; set; }

    }
}
