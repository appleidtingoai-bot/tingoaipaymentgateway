using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace TingoAI.PaymentGateway.Infrastructure.Caching;

public class RedisCache
{
    private readonly IDatabase? _database;
    private readonly IConnectionMultiplexer? _redis;

    public RedisCache(IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        try
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _database = _redis.GetDatabase();
        }
        catch
        {
            // If Redis is not available locally, continue without caching.
            _redis = null;
            _database = null;
        }
    }

    public async Task<string?> GetAsync(string key)
    {
        if (_database == null) return null;
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        if (_database == null) return;
        if (expiry.HasValue)
        {
            await _database.StringSetAsync(key, value, expiry.Value);
        }
        else
        {
            await _database.StringSetAsync(key, value);
        }
    }

    public async Task<long> IncrementAsync(string key, TimeSpan? expiry = null)
    {
        if (_database == null) return 0;
        var value = await _database.StringIncrementAsync(key);

        if (expiry.HasValue && value == 1)
        {
            await _database.KeyExpireAsync(key, expiry);
        }

        return value;
    }

    public async Task<bool> DeleteAsync(string key)
    {
        if (_database == null) return false;
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (_database == null) return false;
        return await _database.KeyExistsAsync(key);
    }
}
