using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shared.Model;
using SearchAPI.Api.Mapping;
using SearchAPI.Application;

namespace SearchAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SearchController : ControllerBase
{
    public const string SearchCacheFlag = "SearchCache";
    public static readonly Meter Meter = new("SearchV2.Cache");
    private static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("searchv2.cache_hits");
    private static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>("searchv2.cache_misses");
    private static readonly Counter<long> CacheErrors = Meter.CreateCounter<long>("searchv2.cache_errors");
    // Sliding keeps popular queries warm; the absolute limit stops a query that is repeated
    // every minute from serving the same (possibly stale) result forever.
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(1),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };
    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };
    private readonly ISearchService _search;
    private readonly IDistributedCache _cache;
    private readonly IFeatureManager _features;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService search, IDistributedCache cache, IFeatureManager features,
        ILogger<SearchController> logger)
    {
        _search = search;
        _cache = cache;
        _features = features;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SearchResult>> Post([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        // Markdown: with the flag off the cache is skipped, so Redis is out of the request path.
        if (!await _features.IsEnabledAsync(SearchCacheFlag))
        {
            return Ok(Search(request));
        }

        var key = "search:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        // A cache is an optimisation: if Redis fails or holds an unreadable entry, treat it as a miss
        // and answer from the database instead of failing the request.
        string? cached = null;
        try
        {
            cached = await _cache.GetStringAsync(key, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CacheFailed(ex, "read");
        }

        if (cached is not null)
        {
            try
            {
                var hit = JsonSerializer.Deserialize<SearchResult>(cached, JsonOptions);
                CacheHits.Add(1);
                return Ok(hit);
            }
            catch (JsonException ex)
            {
                CacheFailed(ex, "deserialize");
            }
        }

        CacheMisses.Add(1);
        var result = Search(request);
        try
        {
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(result, JsonOptions), CacheOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CacheFailed(ex, "write");
        }
        return Ok(result);
    }

    private void CacheFailed(Exception ex, string operation)
    {
        CacheErrors.Add(1, new KeyValuePair<string, object?>("operation", operation));
        _logger.LogWarning(ex, "Search cache {Operation} failed; falling back to the database.", operation);
    }

    private SearchResult Search(SearchRequest request) =>
        _search.Search(request.Query, request.CaseSensitive).ToContract();
}
