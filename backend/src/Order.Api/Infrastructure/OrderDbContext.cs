using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using System;

namespace Order.Api.Infrastructure
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) {}

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<ConsumedMessage> ConsumedMessages { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().HasKey(o => o.Id);
            modelBuilder.Entity<OrderStatusHistory>().HasKey(h => h.Id);
            modelBuilder.Entity<ConsumedMessage>().HasKey(c => c.MessageId);
            modelBuilder.Entity<OutboxMessage>().HasKey(o => o.Id);
            modelBuilder.Entity<OutboxMessage>().HasIndex(o => new { o.Dispatched, o.OccurredAt });
            base.OnModelCreating(modelBuilder);
        }
    }

    public class ConsumedMessage
    {
        public string MessageId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
