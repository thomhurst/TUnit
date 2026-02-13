using System.Text.Json;
using CloudShop.Shared.Models;
using StackExchange.Redis;

namespace CloudShop.ApiService.Services;

public class ProductCacheService(IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Product?> GetAsync(int id)
    {
        var cached = await _db.StringGetAsync($"product:{id}");
        return cached.HasValue ? JsonSerializer.Deserialize<Product>(cached.ToString()) : null;
    }

    public async Task SetAsync(Product product)
    {
        var json = JsonSerializer.Serialize(product);
        await _db.StringSetAsync($"product:{product.Id}", json, CacheDuration);
    }

    public async Task InvalidateAsync(int id)
    {
        await _db.KeyDeleteAsync($"product:{id}");
    }
}
