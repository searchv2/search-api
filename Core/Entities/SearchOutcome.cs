namespace SearchAPI.Core.Entities;

/// <summary>
/// The result of a search.
/// <see cref="TotalHits"/> is the number of documents containing at least one query word.
/// <see cref="Hits"/> holds those documents, ordered by number of query words present.
/// <see cref="IgnoredWords"/> are query words not present anywhere in the document base.
/// <see cref="Elapsed"/> is the time spent performing the search.
/// </summary>
public sealed class SearchOutcome
{
    public required IReadOnlyList<string> Query { get; init; }

    public required int TotalHits { get; init; }

    public required IReadOnlyList<SearchHit> Hits { get; init; }

    public required IReadOnlyList<string> IgnoredWords { get; init; }

    public required TimeSpan Elapsed { get; init; }
}
