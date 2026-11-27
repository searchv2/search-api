using System.Diagnostics.Metrics;
using Npgsql;
using SearchAPI.Infrastructure.Persistence;
using Xunit;

namespace SearchAPI.Tests;

/// <summary>Runs only when SEARCH_DB_TEST_CONNECTION names a PostgreSQL database to test against.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SEARCH_DB_TEST_CONNECTION")))
            Skip = "Set SEARCH_DB_TEST_CONNECTION to a PostgreSQL connection string to run this test.";
    }
}

public sealed class DatabaseMetricsTests
{
    [PostgresFact]
    public void Operations_record_duration_preserve_results_and_skip_empty_lookups()
    {
        // A throwaway schema keeps the test away from the real index tables.
        var schema = $"test_{Guid.NewGuid():N}";
        var admin = Environment.GetEnvironmentVariable("SEARCH_DB_TEST_CONNECTION")!;
        using var setup = new NpgsqlConnection(admin);
        setup.Open();
        using var command = setup.CreateCommand();
        command.CommandText = $"CREATE SCHEMA {schema}";
        command.ExecuteNonQuery();
        try
        {
            command.CommandText = $"""
                SET search_path TO {schema};
                CREATE TABLE word(id INTEGER PRIMARY KEY, name TEXT);
                CREATE TABLE document(id INTEGER PRIMARY KEY, url TEXT, idxTime TEXT, creationTime TEXT);
                CREATE TABLE occ(wordId INTEGER, docId INTEGER);
                INSERT INTO word VALUES (1, 'apple'), (2, 'cherry');
                INSERT INTO document VALUES (1, '/test/one.txt', '2026-01-01', '2026-01-01'),
                                            (2, '/test/two.txt', '2026-01-01', '2026-01-01');
                INSERT INTO occ VALUES (1, 1), (2, 1), (1, 2);
                """;
            command.ExecuteNonQuery();

            var measurements = new List<(double Duration, KeyValuePair<string, object?>[] Tags)>();
            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "SearchV2.Database")
                {
                    Assert.Equal("searchv2.database.operation.duration", instrument.Name);
                    Assert.Equal("s", instrument.Unit);
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
                measurements.Add((value, tags.ToArray())));
            listener.Start();

            var connection = new NpgsqlConnectionStringBuilder(admin) { SearchPath = schema }.ConnectionString;
            using var index = new PostgresDocumentIndex(connection);
            Assert.Equal(new[] { 1, 2 }, index.GetWordIds(["apple", "CHERRY", "unknown"], out var ignored));
            Assert.Equal(new[] { "unknown" }, ignored);
            Assert.Equal(new[] { new KeyValuePair<int, int>(1, 2), new KeyValuePair<int, int>(2, 1) },
                index.GetDocuments([1, 2]));
            Assert.Equal("/test/one.txt", index.GetDocDetails([1, 2])[1].Url);
            Assert.Equal(new[] { "cherry" }, index.GetMissingWords([2], [1, 2])[2]);

            Assert.Equal(new[] { "GetWordIds", "GetDocuments", "GetDocDetails", "GetMissingWords" },
                measurements.Select(m => Assert.Single(m.Tags).Value));
            Assert.All(measurements, m =>
            {
                Assert.True(double.IsFinite(m.Duration) && m.Duration >= 0);
                Assert.Equal("operation", Assert.Single(m.Tags).Key);
            });

            Assert.Empty(index.GetDocDetails([]));
            Assert.Empty(index.GetMissingWords([], [1]));
            Assert.Empty(index.GetWordIds([], out _));
            Assert.Equal(4, measurements.Count);

            command.CommandText = "DROP TABLE word";
            command.ExecuteNonQuery();
            Assert.Throws<PostgresException>(() => index.GetWordIds(["apple"], out _));
            Assert.Equal(5, measurements.Count);
            Assert.Equal("GetWordIds", Assert.Single(measurements[^1].Tags).Value);
        }
        finally
        {
            command.CommandText = $"DROP SCHEMA {schema} CASCADE";
            command.ExecuteNonQuery();
        }
    }
}
