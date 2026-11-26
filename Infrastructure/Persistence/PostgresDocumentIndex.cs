using System.Diagnostics;
using System.Diagnostics.Metrics;
using Npgsql;
using Shared;
using SearchAPI.Core.Abstractions;
using SearchAPI.Core.Entities;

namespace SearchAPI.Infrastructure.Persistence;

/// <summary>
/// Reads the inverted index from the PostgreSQL database (see <see cref="SearchDatabase"/>)
/// that the indexer writes. Each operation borrows a pooled connection, so concurrent
/// requests do not share one connection.
/// </summary>
public sealed class PostgresDocumentIndex : IDocumentIndex, IDisposable
{
    internal const string MeterName = "SearchV2.Database";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        "searchv2.database.operation.duration", "s",
        "PostgreSQL index operation duration, including SQL execution and result reading.");
    private readonly NpgsqlDataSource _dataSource;

    public PostgresDocumentIndex(string connectionString)
    {
        _dataSource = NpgsqlDataSource.Create(connectionString);
    }

    // key is the id of the document, the value is number of search words in the document
    public List<KeyValuePair<int, int>> GetDocuments(List<int> wordIds)
    {
        using var timing = new OperationTiming(nameof(GetDocuments));
        var res = new List<KeyValuePair<int, int>>();

        /* Example sql statement looking for doc id's that
           contain words with id 2 and 3

           SELECT docId, COUNT(wordId) as count
             FROM occ
            WHERE wordId = ANY('{2,3}')
         GROUP BY docId
         ORDER BY COUNT(wordId) DESC
         */

        using var selectCmd = _dataSource.CreateCommand(
            "SELECT docId, COUNT(wordId)::int AS count FROM occ WHERE wordId = ANY(@wordIds) "
            + "GROUP BY docId ORDER BY count DESC");
        selectCmd.Parameters.AddWithValue("wordIds", wordIds.ToArray());

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var docId = reader.GetInt32(0);
                var count = reader.GetInt32(1);

                res.Add(new KeyValuePair<int, int>(docId, count));
            }
        }

        return res;
    }

    public IReadOnlyDictionary<int, Document> GetDocDetails(IReadOnlyList<int> docIds)
    {
        var res = new Dictionary<int, Document>();
        if (docIds.Count == 0)
            return res;

        using var timing = new OperationTiming(nameof(GetDocDetails));
        using var selectCmd = _dataSource.CreateCommand(
            "SELECT id, url, idxTime, creationTime FROM document WHERE id = ANY(@docIds)");
        selectCmd.Parameters.AddWithValue("docIds", docIds.ToArray());

        using var reader = selectCmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            res[id] = new Document
            {
                Id = id,
                Url = reader.GetString(1),
                IndexedAt = reader.GetString(2),
                CreatedAt = reader.GetString(3),
            };
        }
        return res;
    }

    public IReadOnlyDictionary<int, List<string>> GetMissingWords(
        IReadOnlyList<int> docIds, IReadOnlyList<int> wordIds)
    {
        var res = new Dictionary<int, List<string>>();
        if (docIds.Count == 0)
            return res;

        using var timing = new OperationTiming(nameof(GetMissingWords));
        using var connection = _dataSource.OpenConnection();

        // query word id -> name
        var names = new Dictionary<int, string>();
        using (var nameCmd = new NpgsqlCommand("SELECT id, name FROM word WHERE id = ANY(@wordIds)", connection))
        {
            nameCmd.Parameters.AddWithValue("wordIds", wordIds.ToArray());
            using var reader = nameCmd.ExecuteReader();
            while (reader.Read())
                names[reader.GetInt32(0)] = reader.GetString(1);
        }

        // which query words each document actually has
        var present = new Dictionary<int, HashSet<int>>();
        using (var occCmd = new NpgsqlCommand(
            "SELECT docId, wordId FROM occ WHERE docId = ANY(@docIds) AND wordId = ANY(@wordIds)", connection))
        {
            occCmd.Parameters.AddWithValue("docIds", docIds.ToArray());
            occCmd.Parameters.AddWithValue("wordIds", wordIds.ToArray());
            using var reader = occCmd.ExecuteReader();
            while (reader.Read())
            {
                var docId = reader.GetInt32(0);
                if (!present.TryGetValue(docId, out var has))
                    present[docId] = has = new HashSet<int>();
                has.Add(reader.GetInt32(1));
            }
        }

        foreach (var docId in docIds)
        {
            present.TryGetValue(docId, out var has);
            var missing = new List<string>();
            foreach (var wordId in wordIds)
                if (has == null || !has.Contains(wordId))
                    missing.Add(names[wordId]);
            res[docId] = missing;
        }
        return res;
    }

    public List<int> GetWordIds(string[] query, out List<string> outIgnored)
    {
        var res = new List<int>();
        outIgnored = new List<string>();
        if (query.Length == 0)
            return res;

        using var timing = new OperationTiming(nameof(GetWordIds));
        using var connection = _dataSource.OpenConnection();
        using var selectCmd = new NpgsqlCommand("SELECT id FROM word WHERE name = @name", connection);
        var nameParam = new NpgsqlParameter<string>("name", "");
        selectCmd.Parameters.Add(nameParam);

        foreach (var aWord in query)
        {
            nameParam.TypedValue = TextNormalizer.Fold(aWord);
            var id = selectCmd.ExecuteScalar();

            if (id != null)
                res.Add(Convert.ToInt32(id));
            else
                outIgnored.Add(aWord);
        }
        return res;
    }

    private readonly struct OperationTiming : IDisposable
    {
        private readonly string _operation;
        private readonly long _started;

        public OperationTiming(string operation)
        {
            _operation = operation;
            _started = Stopwatch.GetTimestamp();
        }

        // Dispose also records failed operations without changing exception handling.
        public void Dispose() => OperationDuration.Record(
            Stopwatch.GetElapsedTime(_started).TotalSeconds,
            new KeyValuePair<string, object?>("operation", _operation));
    }

    public void Dispose() => _dataSource.Dispose();
}
