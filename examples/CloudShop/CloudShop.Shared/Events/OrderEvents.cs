namespace CloudShop.Shared.Events;

public record OrderCreatedEvent(int OrderId, string CustomerEmail, decimal TotalAmount, DateTime CreatedAt);
public record OrderPaymentProcessedEvent(int OrderId, string PaymentMethod, DateTime ProcessedAt);
public record OrderFulfilledEvent(int OrderId, DateTime FulfilledAt);
