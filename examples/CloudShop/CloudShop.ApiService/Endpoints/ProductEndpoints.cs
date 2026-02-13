using CloudShop.ApiService.Data;
using CloudShop.ApiService.Services;
using CloudShop.Shared.Contracts;
using CloudShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudShop.ApiService.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", async (
            AppDbContext db,
            ProductCacheService cache,
            [FromQuery] string? category,
            [FromQuery] string sort = "name",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25) =>
        {
            var query = db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" or "name_asc" => query.OrderBy(p => p.Name),
                _ => query.OrderBy(p => p.Name)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductResponse(p.Id, p.Name, p.Category, p.Price, p.Description, p.StockQuantity, p.CreatedAt))
                .ToListAsync();

            return Results.Ok(new PagedResult<ProductResponse>(items, totalCount, page, pageSize));
        });

        group.MapGet("/{id:int}", async (int id, AppDbContext db, ProductCacheService cache) =>
        {
            // Try cache first
            var cached = await cache.GetAsync(id);
            if (cached is not null)
                return Results.Ok(new ProductResponse(cached.Id, cached.Name, cached.Category, cached.Price, cached.Description, cached.StockQuantity, cached.CreatedAt));

            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            // Cache for next time
            await cache.SetAsync(product);

            return Results.Ok(new ProductResponse(product.Id, product.Name, product.Category, product.Price, product.Description, product.StockQuantity, product.CreatedAt));
        });

        group.MapPost("/", async (CreateProductRequest request, AppDbContext db, ProductCacheService cache) =>
        {
            var product = new Product
            {
                Name = request.Name,
                Category = request.Category,
                Price = request.Price,
                Description = request.Description,
                StockQuantity = request.StockQuantity
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var response = new ProductResponse(product.Id, product.Name, product.Category, product.Price, product.Description, product.StockQuantity, product.CreatedAt);
            return Results.Created($"/api/products/{product.Id}", response);
        }).RequireAuthorization("admin");

        group.MapPut("/{id:int}", async (int id, UpdateProductRequest request, AppDbContext db, ProductCacheService cache) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            if (request.Name is not null) product.Name = request.Name;
            if (request.Category is not null) product.Category = request.Category;
            if (request.Price.HasValue) product.Price = request.Price.Value;
            if (request.Description is not null) product.Description = request.Description;
            if (request.StockQuantity.HasValue) product.StockQuantity = request.StockQuantity.Value;

            await db.SaveChangesAsync();
            await cache.InvalidateAsync(id);

            return Results.Ok(new ProductResponse(product.Id, product.Name, product.Category, product.Price, product.Description, product.StockQuantity, product.CreatedAt));
        }).RequireAuthorization("admin");

        group.MapDelete("/{id:int}", async (int id, AppDbContext db, ProductCacheService cache) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            db.Products.Remove(product);
            await db.SaveChangesAsync();
            await cache.InvalidateAsync(id);

            return Results.NoContent();
        }).RequireAuthorization("admin");
    }
}
