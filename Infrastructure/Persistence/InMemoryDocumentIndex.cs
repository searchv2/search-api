using Shared;
using SearchAPI.Core.Abstractions;
using SearchAPI.Core.Entities;

namespace SearchAPI.Infrastructure.Persistence;

/// <summary>
/// In-memory stand-in for <see cref="SqliteDocumentIndex"/>. Holds a small canned dataset
/// so /api/search has something to return without running the indexer. Swap the DI
/// registration in <c>DependencyInjection</c> back to <see cref="SqliteDocumentIndex"/> to
/// serve the real index - no other code changes.
/// </summary>
public sealed class InMemoryDocumentIndex : IDocumentIndex
{
    private readonly Dictionary<string, int> _words = new();
    private readonly Dictionary<int, Document> _documents = new();
    private readonly Dictionary<int, HashSet<int>> _occurrences = new(); // docId -> wordIds

    public InMemoryDocumentIndex()
    {
        Seed("apple banana cherry", "/mock/fruits.txt");
        Seed("apple orange cherry grape", "/mock/more-fruits.txt");
        Seed("banana bread recipe", "/mock/recipes.txt");
    }

    private void Seed(string content, string url)
    {
        int docId = _documents.Count + 1;
        _documents[docId] = new Document
        {
            Id = docId,
            Url = url,
            IndexedAt = "2026-01-01",
            CreatedAt = "2026-01-01",
        };

        var wordIds = new HashSet<int>();
        foreach (var word in Tokenizer.Tokenize(content))
        {
            var folded = TextNormalizer.Fold(word);
            if (!_words.TryGetValue(folded, out var wordId))
            {
                wordId = _words.Count + 1;
                _words[folded] = wordId;
            }
            wordIds.Add(wordId);
        }
        _occurrences[docId] = wordIds;
    }

    public List<int> GetWordIds(string[] query, out List<string> outIgnored)
    {
        var res = new List<int>();
        outIgnored = new List<string>();

        foreach (var aWord in query)
        {
            if (_words.TryGetValue(TextNormalizer.Fold(aWord), out var id))
                res.Add(id);
            else
                outIgnored.Add(aWord);
        }
        return res;
    }

    public List<KeyValuePair<int, int>> GetDocuments(List<int> wordIds)
    {
        return _occurrences
            .Select(p => new KeyValuePair<int, int>(p.Key, p.Value.Count(wordIds.Contains)))
            .Where(p => p.Value > 0)
            .OrderByDescending(p => p.Value)
            .ToList();
    }

    public IReadOnlyDictionary<int, Document> GetDocDetails(IReadOnlyList<int> docIds)
    {
        return docIds.ToDictionary(id => id, id => _documents[id]);
    }

    public IReadOnlyDictionary<int, List<string>> GetMissingWords(
        IReadOnlyList<int> docIds, IReadOnlyList<int> wordIds)
    {
        var namesById = _words.ToDictionary(p => p.Value, p => p.Key);
        var res = new Dictionary<int, List<string>>();

        foreach (var docId in docIds)
        {
            var has = _occurrences[docId];
            res[docId] = wordIds.Where(w => !has.Contains(w)).Select(w => namesById[w]).ToList();
        }
        return res;
    }
}
