using FluentAssertions;
using FCG02.PaymentsAPI.Application.Events;
using FCG02.PaymentsAPI.Application.Interfaces;
using FCG02.PaymentsAPI.Application.Services;
using FCG02.PaymentsAPI.Domain.Entities;
using FCG02.PaymentsAPI.Domain.Enums;
using FCG02.PaymentsAPI.Domain.Interfaces;
using Moq;
using FCG02.PaymentsAPI.Application.DTOs;

namespace FCG02.PaymentsAPI.UnitTests.Application;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _repositoryMock;
    private readonly Mock<IPaymentGatewaySimulator> _gatewayMock;
    private readonly Mock<IEventPublisher> _publisherMock;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _repositoryMock = new Mock<IPaymentRepository>();
        _gatewayMock = new Mock<IPaymentGatewaySimulator>();
        _publisherMock = new Mock<IEventPublisher>();

        _sut = new PaymentService(
            _repositoryMock.Object,
            _gatewayMock.Object,
            _publisherMock.Object
        );
    }

    [Fact]
    public async Task ProcessOrderPaymentAsync_WhenGatewayApproves_ShouldApproveSaveAndUpdateAndPublishEvent()
    {
        // Arrange
        var orderEvent = new OrderPlacedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            GameId: Guid.NewGuid(),
            Price: 199.90m
        );

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderEvent.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        _gatewayMock
            .Setup(g => g.ProcessAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(true, "AUTH-12345", null));

        // Act
        var result = await _sut.ProcessOrderPaymentAsync(orderEvent);

        // Assert
        result.Status.Should().Be("Approved");
        result.Reason.Should().Contain("AUTH-12345");
        result.Amount.Should().Be(199.90m);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<PaymentTransaction>(t => t.Status == PaymentStatus.Approved), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(
            It.Is<PaymentProcessedIntegrationEvent>(e => e.Status == "Approved" && e.OrderId == orderEvent.OrderId),
            "payment.approved",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessOrderPaymentAsync_WhenGatewayRejects_ShouldRejectSaveAndPublishRejectedEvent()
    {
        // Arrange
        var orderEvent = new OrderPlacedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            GameId: Guid.NewGuid(),
            Price: 999.99m
        );

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderEvent.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        _gatewayMock
            .Setup(g => g.ProcessAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(false, null, "Saldo insuficiente."));

        // Act
        var result = await _sut.ProcessOrderPaymentAsync(orderEvent);

        // Assert
        result.Status.Should().Be("Rejected");
        result.Reason.Should().Be("Saldo insuficiente.");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<PaymentTransaction>(t => t.Status == PaymentStatus.Rejected), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(
            It.Is<PaymentProcessedIntegrationEvent>(e => e.Status == "Rejected" && e.OrderId == orderEvent.OrderId),
            "payment.rejected",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessOrderPaymentAsync_WhenOrderAlreadyProcessed_ShouldReturnExistingWithoutReprocessing()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existing = new PaymentTransaction(orderId, Guid.NewGuid(), Guid.NewGuid(), 80m);
        existing.Approve("Transação anterior");

        var orderEvent = new OrderPlacedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            OrderId: orderId,
            UserId: existing.UserId,
            GameId: existing.GameId,
            Price: 80m
        );

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.ProcessOrderPaymentAsync(orderEvent);

        // Assert
        result.Status.Should().Be("Approved");
        _gatewayMock.Verify(g => g.ProcessAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<PaymentProcessedIntegrationEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenFound_ShouldReturnPaymentResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var transaction = new PaymentTransaction(orderId, Guid.NewGuid(), Guid.NewGuid(), 120m);

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _sut.GetByOrderIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        var result = await _sut.GetByOrderIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}
