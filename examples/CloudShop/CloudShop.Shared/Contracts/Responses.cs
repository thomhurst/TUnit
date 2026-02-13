using CloudShop.Shared.Models;

namespace CloudShop.Shared.Contracts;

public record ProductResponse(int Id, string Name, string Category, decimal Price, string? Description, int StockQuantity, DateTime CreatedAt);
public record OrderResponse(int Id, string CustomerEmail, OrderStatus Status, string PaymentMethod, string ShippingOption, decimal TotalAmount, List<OrderItemResponse> Items, DateTime CreatedAt, DateTime? FulfilledAt);
public record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public bool HasNextPage => Page * PageSize < TotalCount;
}

public record TokenResponse(string AccessToken, string Email, string Role, DateTime ExpiresAt);

public record ErrorResponse(string Message, Dictionary<string, string[]>? Errors = null);
