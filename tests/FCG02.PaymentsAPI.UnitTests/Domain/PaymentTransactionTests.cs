using System.Diagnostics;
using FCG02.PaymentsAPI.Domain.Entities;
using FCG02.PaymentsAPI.Domain.Enums;
using FluentAssertions;

namespace FCG02.PaymentsAPI.UnitTests.Domain;

public class PaymentTransactionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreatePendingTransaction()
    {
        // arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var amount = 149.9m;
        var method = PaymentMethod.CreditCard;

        // Act
        var trans = new PaymentTransaction(orderId, userId, gameId, amount, method);

        // Assert
        trans.Id.Should().NotBeEmpty();
        trans.OrderId.Should().Be(orderId);
        trans.UserId.Should().Be(userId);
        trans.GameId.Should().Be(gameId);
        trans.Amount.Should().Be(amount);
        trans.Method.Should().Be(method);
        trans.Status.Should().Be(PaymentStatus.Pending);
        trans.Reason.Should().BeNull();
        trans.ProcessedAt.Should().BeNull();
        trans.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Contructor_WithZeroAmount_ShouldAllowFreeGameTransaction()
    {
        // Arrange & Act
        var trans = new PaymentTransaction(
            orderId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            gameId: Guid.NewGuid(),
            0m
            );

        // assert
        trans.Amount.Should().Be(0m);
        trans.Status.Should().Be(PaymentStatus.Pending);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-50.00)]
    public void Constructor_WithNegAmount_ShouldThrowArgumentException(decimal negAmount)
    {
        // Arrange & Act
        Action act = () => new PaymentTransaction(
            orderId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            gameId: Guid.NewGuid(),
            negAmount
            );

        // assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*não pode ser negativo*");
    }

    [Fact]
    public void Constructor_WithEmptyOrderId_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new PaymentTransaction(
            orderId: Guid.Empty,
            userId: Guid.NewGuid(),
            gameId: Guid.NewGuid(),
            0m
            );

        // assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*é obrigatório*");
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new PaymentTransaction(
            orderId: Guid.NewGuid(),
            userId: Guid.Empty,
            gameId: Guid.NewGuid(),
            0m
            );

        // assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*é obrigatório*");
    }

    [Fact]
    public void Constructor_WithEmptyGameId_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new PaymentTransaction(
            orderId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            gameId: Guid.Empty,
            0m
            );

        // assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*é obrigatório*");
    }

    [Fact]
    public void Approve_WhenPending_ShouldTransitionToApprovedAndSetProcessedAt()
    {
        // Arrange
        var transaction = new PaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 99.90m);

        // Act
        transaction.Approve("Cartão autorizado pela adquirente");

        // Assert
        transaction.Status.Should().Be(PaymentStatus.Approved);
        transaction.ProcessedAt.Should().NotBeNull();
        transaction.ProcessedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        transaction.Reason.Should().Be("Cartão autorizado pela adquirente");
    }

    [Fact]
    public void Reject_WhenPending_ShouldTransitionToRejectedAndSetProcessedAt()
    {
        // Arrange
        var transaction = new PaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 99.90m);

        // Act
        transaction.Reject("Saldo insuficiente na conta");

        // Assert
        transaction.Status.Should().Be(PaymentStatus.Rejected);
        transaction.ProcessedAt.Should().NotBeNull();
        transaction.ProcessedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        transaction.Reason.Should().Be("Saldo insuficiente na conta");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithEmptyReason_ShouldThrowArgumentException(string? invalidReason)
    {
        // Arrange
        var transaction = new PaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 99.90m);

        // Act
        Action act = () => transaction.Reject(invalidReason!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*motivo da recusa*");
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transaction = new PaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        transaction.Approve();

        // Act
        Action act = () => transaction.Approve();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não é possível aprovar uma transação com status 'Approved'*");
    }

    [Fact]
    public void Reject_WhenAlreadyApproved_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transaction = new PaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        transaction.Approve();

        // Act
        Action act = () => transaction.Reject("Erro tardio");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não é possível recusar uma transação com status 'Approved'*");
    }

    [Fact]
    public void Approve_WhenAlreadyRejected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transaction = new PaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        transaction.Reject("Cartão bloqueado");

        // Act
        Action act = () => transaction.Approve();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não é possível aprovar uma transação com status 'Rejected'*");
    }
}
