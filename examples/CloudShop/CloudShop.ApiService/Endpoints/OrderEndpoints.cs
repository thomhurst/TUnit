using System.Security.Claims;
using CloudShop.ApiService.Data;
using CloudShop.ApiService.Services;
using CloudShop.Shared.Contracts;
using CloudShop.Shared.Events;
using CloudShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudShop.ApiService.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        group.MapGet("/mine", async (ClaimsPrincipal user, AppDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            var email = user.FindFirstValue(ClaimTypes.Email)!;

            var query = db.Orders
                .Where(o => o.CustomerEmail == email)
                .OrderByDescending(o => o.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.Items)
                .Select(o => new OrderResponse(
                    o.Id, o.CustomerEmail, o.Status, o.PaymentMethod, o.ShippingOption,
                    o.TotalAmount, o.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList(),
                    o.CreatedAt, o.FulfilledAt))
                .ToListAsync();

            return Results.Ok(new PagedResult<OrderResponse>(items, totalCount, page, pageSize));
        });

        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var email = user.FindFirstValue(ClaimTypes.Email)!;
            var role = user.FindFirstValue(ClaimTypes.Role);

            var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();

            // Customers can only see their own orders
            if (role != "admin" && order.CustomerEmail != email)
                return Results.Forbid();

            return Results.Ok(new OrderResponse(
                order.Id, order.CustomerEmail, order.Status, order.PaymentMethod, order.ShippingOption,
                order.TotalAmount, order.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList(),
                order.CreatedAt, order.FulfilledAt));
        });

        group.MapPost("/", async (CreateOrderRequest request, ClaimsPrincipal user, AppDbContext db, OrderEventPublisher publisher) =>
        {
            if (request.Items.Count == 0)
                return Results.BadRequest(new ErrorResponse("Order must contain at least one item"));

            var email = user.FindFirstValue(ClaimTypes.Email)!;
            var order = new Order
            {
                CustomerEmail = email,
                PaymentMethod = request.PaymentMethod,
                ShippingOption = request.ShippingOption,
                Items = []
            };

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return Results.BadRequest(new ErrorResponse("Item quantity must be positive"));

                if (item.Quantity > 10000)
                    return Results.BadRequest(new ErrorResponse("Item quantity exceeds maximum of 10000"));

                var product = await db.Products.FindAsync(item.ProductId);
                if (product is null)
                    return Results.BadRequest(new ErrorResponse($"Product {item.ProductId} not found"));

                if (product.StockQuantity < item.Quantity)
                    return Results.BadRequest(new ErrorResponse($"Insufficient stock for {product.Name}"));

                product.StockQuantity -= item.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity
                });
            }

            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            try
            {
                await publisher.PublishOrderCreatedAsync(new OrderCreatedEvent(order.Id, email, order.TotalAmount, order.CreatedAt));
            }
            catch
            {
                // Don't fail the order if messaging is down
            }

            var response = new OrderResponse(
                order.Id, order.CustomerEmail, order.Status, order.PaymentMethod, order.ShippingOption,
                order.TotalAmount, order.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList(),
                order.CreatedAt, order.FulfilledAt);

            return Results.Created($"/api/orders/{order.Id}", response);
        });

        group.MapPost("/{id:int}/pay", async (int id, ProcessPaymentRequest request, ClaimsPrincipal user, AppDbContext db, OrderEventPublisher publisher) =>
        {
            var email = user.FindFirstValue(ClaimTypes.Email)!;
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.CustomerEmail == email);
            if (order is null) return Results.NotFound();

            if (order.Status != OrderStatus.Pending)
                return Results.BadRequest(new ErrorResponse("Order is not in pending status"));

            order.Status = OrderStatus.PaymentProcessed;
            await db.SaveChangesAsync();

            try
            {
                await publisher.PublishPaymentProcessedAsync(new OrderPaymentProcessedEvent(order.Id, request.PaymentMethod, DateTime.UtcNow));
            }
            catch
            {
                // Don't fail payment if messaging is down
            }

            return Results.Ok(new { order.Id, order.Status });
        });
    }
}
