using Microsoft.AspNetCore.Mvc;
using Week6.Caching;
using Week6.Dtos.Products;
using Week6.Dtos.Shared;

namespace Week6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductCacheService _cache;
    private readonly ICacheMetrics _metrics;

    public ProductsController(IProductCacheService cache, ICacheMetrics metrics)
    {
        _cache = cache;
        _metrics = metrics;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _cache.GetProductAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("cache-stats")]
    public ActionResult<CacheStatsResponse> GetCacheStats()
    {
        var s = _metrics.GetSnapshot();
        return Ok(new CacheStatsResponse(s.MemoryHits, s.RedisHits, s.Misses, Math.Round(s.HitRatio * 100, 1) + "%"));
    }
}