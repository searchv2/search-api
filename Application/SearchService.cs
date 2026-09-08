using Shared;
using SearchAPI.Core.Abstractions;
using SearchAPI.Core.Entities;

namespace SearchAPI.Application;

/// <summary>
/// Orchestrates a search over the <see cref="IDocumentIndex"/>. Pure application logic - it
/// depends only on Core ports, never on Infrastructure or the API wire models.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly IDocumentIndex _index;
    private readonly IDocumentContentReader _content;

    public SearchService(IDocumentIndex index, IDocumentContentReader content)
    {
        _index = index;
        _content = content;
    }

    public SearchOutcome Search(string[] query, bool caseSensitive)
    {
        var start = DateTime.Now;

        // Convert words to word ids.
        var wordIds = _index.GetWordIds(query, out var ignored);

        if (wordIds.Count == 0) // no query word is known to the index
            return new SearchOutcome
            {
                Query = query,
                TotalHits = 0,
                Hits = Array.Empty<SearchHit>(),
                IgnoredWords = ignored,
                Elapsed = DateTime.Now - start,
            };

        // Perform the search - all docIds, ordered by number of query words matched.
        var docIds = _index.GetDocuments(wordIds);

        if (caseSensitive)
        {
            var caseHits = CaseSensitiveHits(query, ignored, docIds);
            return new SearchOutcome
            {
                Query = query,
                TotalHits = caseHits.Count,
                Hits = caseHits,
                IgnoredWords = ignored,
                Elapsed = DateTime.Now - start,
            };
        }

        // Compose the result - one SearchHit per matching document. Everything the loop needs
        // is fetched in two batch queries, not one query per document.
        var details = _index.GetDocDetails(docIds.Select(p => p.Key).ToList());

        // p.Value is how many query words a document contains; when it holds all of them
        // nothing is missing, so only the rest need a missing-words lookup.
        var shortIds = docIds.Where(p => p.Value < wordIds.Count).Select(p => p.Key).ToList();
        var missingByDoc = _index.GetMissingWords(shortIds, wordIds);

        var hits = new List<SearchHit>();
        foreach (var p in docIds)
        {
            var missing = missingByDoc.TryGetValue(p.Key, out var m)
                ? new List<string>(m)
                : new List<string>();
            missing.AddRange(ignored);
            hits.Add(new SearchHit
            {
                Document = details[p.Key],
                MatchedWords = p.Value,
                MissingWords = missing,
            });
        }

        return new SearchOutcome
        {
            Query = query,
            TotalHits = docIds.Count,
            Hits = hits,
            IgnoredWords = ignored,
            Elapsed = DateTime.Now - start,
        };
    }

    /// <summary>
    /// Walk every ranked candidate, re-reading each source document with the shared
    /// tokenizer, and keep those that contain at least one query word with its exact NFKC
    /// casing. A document whose content cannot be read is skipped.
    /// </summary>
    private List<SearchHit> CaseSensitiveHits(
        string[] query, List<string> ignored, List<KeyValuePair<int, int>> docIds)
    {
        var wanted = query
            .Where(w => !ignored.Contains(w))
            .Select(TextNormalizer.Normalize)
            .Distinct()
            .ToList();

        var result = new List<SearchHit>();
        var details = _index.GetDocDetails(docIds.Select(d => d.Key).ToList());

        foreach (var candidate in docIds)
        {
            var doc = details[candidate.Key];

            var text = _content.TryRead(doc.Url);
            if (text is null)
                continue;

            var tokens = new HashSet<string>(Tokenizer.Tokenize(text));

            var present = wanted.Where(tokens.Contains).ToList();
            if (present.Count == 0)
                continue;

            var missing = wanted.Where(w => !tokens.Contains(w)).ToList();
            missing.AddRange(ignored);
            result.Add(new SearchHit
            {
                Document = doc,
                MatchedWords = present.Count,
                MissingWords = missing,
            });
        }

        return result;
    }
}
