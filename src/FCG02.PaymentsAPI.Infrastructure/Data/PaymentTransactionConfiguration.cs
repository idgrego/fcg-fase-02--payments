using FCG02.PaymentsAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG02.PaymentsAPI.Infrastructure.Data.Configurations;

public class PaymentTransactionConfiguration
    : IEntityTypeConfiguration<PaymentTransaction>
{

    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        // definição da tabela
        builder.ToTable("payment_transactions");

        // chave primária (Guid v7 gerado no domínio)
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .IsRequired();

        // identificador de negócio
        builder.Property(t => t.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.GameId)
            .HasColumnName("game_id")
            .IsRequired();

        // valor monetário pago com precisão estrita
        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        // enums convertidos para texto (legibilidade em auditoria)
        builder.Property(t => t.Method)
            .HasColumnName("method")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // justificativa / código de autorização
        builder.Property(t => t.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500)
            .IsRequired(false);

        // auditoria temporal
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired(false);

        // índices

        // índice único para o orderId impedindo pagamentos duplicados
        builder.HasIndex(t => t.OrderId)
            .IsUnique()
            .HasDatabaseName("ix_payment_transactions_order_id");

        // índice para buscas rápidas no extrato de compras do jogados
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("ix_payment_transactions_user_id");

        // índice para relatórios operacionais por status
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("ix_payment_transactions_status");
    }
}