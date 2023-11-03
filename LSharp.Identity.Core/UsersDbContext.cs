

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LSharp.Identity.Core
{
    public class UsersDbContext : IdentityDbContext<LsharpUser>
    {
        public UsersDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<IdentityRole>().HasData(new IdentityRole[]
            {
                new IdentityRole("User"),
                new IdentityRole("Admin"),
            });
        }
    }
}