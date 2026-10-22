using Microsoft.EntityFrameworkCore;
using minimalAPILesson1.Models;


namespace minimalAPILesson1.Data
{
    public class AppDBContext : DbContext //NOTE: We can name it anything at all, as long as it is inheriting from DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
        }

        // This defines the 'Items' table in SQL Server
        public DbSet<Player> Players { get; set; } // NOTE: sample models that we have Player.cs
    }
}