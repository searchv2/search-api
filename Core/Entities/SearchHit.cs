namespace SearchAPI.Core.Entities;

/// <summary>
/// One document matched by a search: how many of the query words it contains, and which
/// query words (or ignored words) it is missing.
/// </summary>
public sealed class SearchHit
{
    public required Document Document { get; init; }

    public required int MatchedWords { get; init; }

    public required IReadOnlyList<string> MissingWords { get; init; }
}
