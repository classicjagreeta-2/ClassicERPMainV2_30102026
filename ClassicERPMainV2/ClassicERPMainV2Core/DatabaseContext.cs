using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore;
namespace Core_APIOracle.Models
{
    public class DatabaseContext : DbContext
    {
        public DbSet<ProductsInfo> Products { get; set; }
        public DbSet<MODULE> Modules { get; set; }
        public DatabaseContext(DbContextOptions<DatabaseContext> options):base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
