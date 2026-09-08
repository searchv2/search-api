using Shared.Model;
using SearchAPI.Core.Entities;

namespace SearchAPI.Api.Mapping;

/// <summary>
/// Translates between the domain's <see cref="SearchOutcome"/> and the wire contract
/// (<c>Shared.Model.SearchResult</c>) shipped in the SearchUtilities package. This mapping
/// is the only place the API's inward and outward shapes meet - the domain never sees the
/// wire types, and callers never see the domain types.
/// </summary>
internal static class SearchContractMapper
{
    public static SearchResult ToContract(this SearchOutcome outcome) => new()
    {
        Query = outcome.Query.ToArray(),
        Hits = outcome.TotalHits,
        DocumentHits = outcome.Hits.Select(ToContract).ToList(),
        Ignored = outcome.IgnoredWords.ToList(),
        TimeUsed = outcome.Elapsed,
    };

    private static DocumentHit ToContract(SearchHit hit) => new()
    {
        Document = new BEDocument
        {
            mId = hit.Document.Id,
            mUrl = hit.Document.Url,
            mIdxTime = hit.Document.IndexedAt,
            mCreationTime = hit.Document.CreatedAt,
        },
        NoOfHits = hit.MatchedWords,
        Missing = hit.MissingWords.ToList(),
    };
}
