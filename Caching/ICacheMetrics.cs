namespace Week6.Caching;

public interface ICacheMetrics
{
    void RecordHit(string layer);
    void RecordMiss();
    CacheStatsSnapshot GetSnapshot();
}

public record CacheStatsSnapshot(long MemoryHits, long RedisHits, long Misses)
{
    public long Total => MemoryHits + RedisHits + Misses;
    public double HitRatio => Total == 0 ? 0 : (double)(MemoryHits + RedisHits) / Total;
}