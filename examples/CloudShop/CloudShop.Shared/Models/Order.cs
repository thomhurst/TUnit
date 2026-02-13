namespace CloudShop.Shared.Models;

public class Order
{
    public int Id { get; set; }
    public required string CustomerEmail { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public required string PaymentMethod { get; set; }
    public required string ShippingOption { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAt { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending,
    PaymentProcessed,
    Fulfilled,
    Shipped,
    Delivered,
    Cancelled
}
