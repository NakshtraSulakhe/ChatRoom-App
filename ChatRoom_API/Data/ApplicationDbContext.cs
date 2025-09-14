using ChatRoom_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom_API.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>

    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base (options)
        {
            
        }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }


    }
}
