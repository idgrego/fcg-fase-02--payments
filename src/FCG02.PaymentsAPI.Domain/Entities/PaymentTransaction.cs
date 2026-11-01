using FCG02.PaymentsAPI.Domain.Enums;

namespace FCG02.PaymentsAPI.Domain.Entities;

/// <summary>
/// Representa uma transação financeira no gateway da PaymentsAPI.
/// </summary>
public class PaymentTransaction
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // suporte para o Entity Framework Core
    private PaymentTransaction() { }

    public PaymentTransaction(
        Guid orderId,
        Guid userId,
        Guid gameId,
        decimal amount,
        PaymentMethod method = PaymentMethod.CreditCard
    )
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId é obrigatório", nameof(orderId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId é obrigatório", nameof(userId));
        if (gameId == Guid.Empty)
            throw new ArgumentException("GameId é obrigatório", nameof(gameId));
        if (amount < 0)
            throw new ArgumentException("Valor da transação não pode ser negativo", nameof(amount));

        Id = Guid.CreateVersion7();
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Aprova a transação financeira caso ainda esteja pendente
    /// </summary>
    /// <param name="note"></param>
    public void Approve(string? note = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Não é possível aprovar uma transação com status '{Status}'.");
        Status = PaymentStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
        Reason = string.IsNullOrWhiteSpace(note) ? "Pagamento autorizado com sucesso." : note.Trim();
    }

    /// <summary>
    /// Recusa a transação financeira registrando o motivo obrigatório
    /// </summary>
    /// <param name="reason"></param>
    public void Reject(string reason)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Não é possível recusar uma transação com status '{Status}'.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("O motivo da recusa é obrigatório.", nameof(reason));
        Status = PaymentStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
        Reason = reason.Trim();
    }
}