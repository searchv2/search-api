using SearchAPI.Core.Entities;

namespace SearchAPI.Application;

/// <summary>
/// The application's single use case: search the document base for the query words. When
/// <paramref name="caseSensitive"/> is true, a document counts as a hit for a query word
/// only when it contains that word with the exact (NFKC) casing.
/// </summary>
public interface ISearchService
{
    SearchOutcome Search(string[] query, bool caseSensitive);
}
