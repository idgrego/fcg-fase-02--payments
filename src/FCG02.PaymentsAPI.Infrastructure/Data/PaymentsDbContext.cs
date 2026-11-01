using FCG02.PaymentsAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG02.PaymentsAPI.Infrastructure.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options) { }

    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // varredura por reflexão: registra todas as classes IEntityTypeConfiguration<T> da infra

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}