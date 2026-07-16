namespace Week6.Dtos.Shared;

public record CacheStatsResponse(long MemoryHits, long RedisHits, long Misses, string HitRatio);