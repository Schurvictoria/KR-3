using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

namespace PaymentsService.Data
{
    public class PaymentsDbContext : DbContext
    {
        public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<PaymentInboxEvent> PaymentInboxEvents { get; set; }
        public DbSet<PaymentOutboxEvent> PaymentOutboxEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .Property(a => a.RowVersion)
                .IsRowVersion();
        }
    }
}
