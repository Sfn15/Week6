// Caching/CacheMetrics.cs
using System.Threading;

namespace Week6.Caching;

public class CacheMetrics : ICacheMetrics
{
    private long _memoryHits;
    private long _redisHits;
    private long _misses;

    public void RecordHit(string layer)
    {
        if (layer == "memory") Interlocked.Increment(ref _memoryHits);
        else if (layer == "redis") Interlocked.Increment(ref _redisHits);
    }

    public void RecordMiss() => Interlocked.Increment(ref _misses);

    public CacheStatsSnapshot GetSnapshot() =>
        new(Interlocked.Read(ref _memoryHits), Interlocked.Read(ref _redisHits), Interlocked.Read(ref _misses));
}