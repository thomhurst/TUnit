namespace CloudShop.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
