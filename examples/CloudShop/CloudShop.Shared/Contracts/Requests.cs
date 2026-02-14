namespace CloudShop.Shared.Contracts;

public record CreateProductRequest(string Name, string Category, decimal Price, string? Description = null, int StockQuantity = 100);
public record UpdateProductRequest(string? Name = null, string? Category = null, decimal? Price = null, string? Description = null, int? StockQuantity = null);

public record CreateOrderRequest(List<OrderItemRequest> Items, string PaymentMethod = "credit_card", string ShippingOption = "standard");
public record OrderItemRequest(int ProductId, int Quantity);
public record ProcessPaymentRequest(string PaymentMethod, string PaymentToken);

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string Name);
