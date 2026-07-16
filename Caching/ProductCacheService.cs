using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Week6.Data;
using Week6.Models;

namespace Week6.Caching;

public interface IProductCacheService
{
    Task<Product?> GetProductAsync(int productId, CancellationToken ct = default);
    Task InvalidateProductAsync(int productId, CancellationToken ct = default);
}

public class ProductCacheService : IProductCacheService
{
    private readonly Week6DbContext _db;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly ICacheMetrics _metrics;
    private readonly ILogger<ProductCacheService> _logger;

    private static readonly TimeSpan MemoryTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RedisTtl = TimeSpan.FromMinutes(15);


    public ProductCacheService(
        Week6DbContext db,
        IMemoryCache memoryCache,
        IDistributedCache redisCache,
        ICacheMetrics metrics,
        ILogger<ProductCacheService> logger)
    {
        _db = db;
        _memoryCache = memoryCache;
        _redisCache = redisCache;
        _metrics = metrics;
        _logger = logger;
    }

    private static string MemoryKey(int id) => $"product:mem:{id}";
    private static string RedisKey(int id) => $"product:{id}";


    public async Task<Product?> GetProductAsync(int productId, CancellationToken ct = default)
    {
        // Layer 1: in-memory (fastest, per-instance)
        if (_memoryCache.TryGetValue(MemoryKey(productId), out Product? cached))
        {
            _metrics.RecordHit("memory");
            _logger.LogDebug("Memory cache hit for product {Id}", productId);
            return cached;
        }

        // Layer 2: Redis (shared across instances, survives app restarts)
        var redisValue = await _redisCache.GetStringAsync(RedisKey(productId), ct);
        if (redisValue is not null)
        {
            _metrics.RecordHit("redis");
            _logger.LogDebug("Redis cache hit for product {Id}", productId);
            var product = JsonSerializer.Deserialize<Product>(redisValue);
            PopulateMemoryCache(productId, product);
            return product;
        }

        // Layer 3: miss — go to the database (the "aside" part of cache-aside:
        // the caller/service manages the fallback, not the cache itself)
        _metrics.RecordMiss();
        _logger.LogDebug("Cache miss for product {Id}, querying database", productId);

        var dbProduct = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (dbProduct is not null)
        {
            await PopulateRedisCacheAsync(productId, dbProduct, ct);
            PopulateMemoryCache(productId, dbProduct);
        }

        return dbProduct;
    }

    public async Task InvalidateProductAsync(int productId, CancellationToken ct = default)
    {
        _memoryCache.Remove(MemoryKey(productId));
        await _redisCache.RemoveAsync(RedisKey(productId), ct);
        _logger.LogInformation("Invalidated cache for product {Id}", productId);
    }

    private void PopulateMemoryCache(int productId, Product? product)
    {
        if (product is null) return;
        _memoryCache.Set(MemoryKey(productId), product, MemoryTtl);
    }

    private Task PopulateRedisCacheAsync(int productId, Product product, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(product);
        return _redisCache.SetStringAsync(RedisKey(productId), json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = RedisTtl }, ct);
    }
}