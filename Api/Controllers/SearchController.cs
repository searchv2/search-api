using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
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
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(1),
    };
    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };
    private readonly ISearchService _search;
    private readonly IDistributedCache _cache;
    private readonly IFeatureManager _features;

    public SearchController(ISearchService search, IDistributedCache cache, IFeatureManager features)
    {
        _search = search;
        _cache = cache;
        _features = features;
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
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        if (cached is not null)
        {
            CacheHits.Add(1);
            return Ok(JsonSerializer.Deserialize<SearchResult>(cached, JsonOptions));
        }

        CacheMisses.Add(1);
        var result = Search(request);
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(result, JsonOptions), CacheOptions, cancellationToken);
        return Ok(result);
    }

    private SearchResult Search(SearchRequest request) =>
        _search.Search(request.Query, request.CaseSensitive).ToContract();
}
