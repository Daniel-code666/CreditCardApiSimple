using CreditCardApiSimple.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApiSimple.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<User> User { get; set; }

        public DbSet<CreditCard> CreditCard { get; set; }
    }
}
