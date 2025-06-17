using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace OrdersService.Data
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
            : base(options)
        {
        }

        public DbSet<Shared.Models.Order> Orders { get; set; }
        public DbSet<Shared.Models.OutboxEvent> Outbox { get; set; }
        public DbSet<Shared.Models.OutboxEvent> OutboxEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Shared.Models.OutboxEvent>()
                        .ToTable("Outbox");
        }
    }
} 