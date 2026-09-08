using SearchAPI.Core.Entities;

namespace SearchAPI.Core.Abstractions;

/// <summary>
/// The port through which the domain reads the inverted index. Infrastructure supplies the
/// implementation (SQLite, or an in-memory stand-in).
/// </summary>
public interface IDocumentIndex
{
    /// <summary>
    /// Get id's for the words in <paramref name="query"/>. <paramref name="outIgnored"/>
    /// receives those query words that are not present in any document.
    /// </summary>
    List<int> GetWordIds(string[] query, out List<string> outIgnored);

    /// <summary>
    /// The essential search: returns one KeyValuePair per matching document - the key is the
    /// document id, the value is the number of query words it contains - ordered by
    /// descending value.
    /// </summary>
    List<KeyValuePair<int, int>> GetDocuments(List<int> wordIds);

    /// <summary>
    /// Look up the details of many documents in one query, keyed by document id. Every id in
    /// <paramref name="docIds"/> comes from <see cref="GetDocuments"/>, so every id is
    /// present in the result.
    /// </summary>
    IReadOnlyDictionary<int, Document> GetDocDetails(IReadOnlyList<int> docIds);

    /// <summary>
    /// For each document in <paramref name="docIds"/>, the names of the query words (given as
    /// <paramref name="wordIds"/>) that the document does NOT contain, in
    /// <paramref name="wordIds"/> order. Intended to be called only with documents already
    /// known to be missing at least one query word.
    /// </summary>
    IReadOnlyDictionary<int, List<string>> GetMissingWords(
        IReadOnlyList<int> docIds, IReadOnlyList<int> wordIds);
}
