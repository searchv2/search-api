using System.Diagnostics.Metrics;
using SearchAPI.Application;
using SearchAPI.Core.Abstractions;
using SearchAPI.Core.Entities;
using SearchAPI.Infrastructure.Persistence;
using Xunit;

namespace SearchAPI.Tests;

public sealed class ActiveSearchMetricsTests
{
    [Fact]
    public async Task Gauge_counts_overlapping_searches_and_resets_after_returns_and_failures()
    {
        var observed = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "SearchV2.Search")
            {
                Assert.Equal("searchv2.search.active", instrument.Name);
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            Assert.Empty(tags.ToArray());
            observed.Add(value);
        });
        listener.Start();

        using var entered = new CountdownEvent(2);
        using var release = new ManualResetEventSlim();
        var service = new SearchService(new CallbackIndex(() =>
        {
            entered.Signal();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Search was not released by the test.");
        }), new DocumentReader());

        void AssertActive(long expected)
        {
            observed.Clear();
            listener.RecordObservableInstruments();
            Assert.Equal(expected, Assert.Single(observed));
        }

        // Construction initializes the observable gauge, even before the first search.
        AssertActive(0);
        var first = Task.Run(() => service.Search(["apple"], caseSensitive: false));
        var second = Task.Run(() => service.Search(["apple"], caseSensitive: true));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)), "Both searches should enter the index.");
            AssertActive(2);
        }
        finally
        {
            release.Set();
            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));
        }
        Assert.Equal(2, (await first).TotalHits);
        Assert.Equal(2, (await second).TotalHits);
        AssertActive(0);

        var noResults = new SearchService(new InMemoryDocumentIndex(), new DocumentReader());
        Assert.Equal(0, noResults.Search(["unknown"], false).TotalHits);
        AssertActive(0);

        var failing = new SearchService(new CallbackIndex(() =>
        {
            AssertActive(1);
            throw new InvalidOperationException("Synthetic database failure.");
        }), new DocumentReader());
        Assert.Throws<InvalidOperationException>(() => failing.Search(["apple"], false));
        AssertActive(0);
    }

    private sealed class CallbackIndex(Action onLookup) : IDocumentIndex
    {
        private readonly InMemoryDocumentIndex _inner = new();

        public List<int> GetWordIds(string[] query, out List<string> ignored)
        {
            onLookup();
            return _inner.GetWordIds(query, out ignored);
        }

        public List<KeyValuePair<int, int>> GetDocuments(List<int> wordIds) => _inner.GetDocuments(wordIds);
        public IReadOnlyDictionary<int, Document> GetDocDetails(IReadOnlyList<int> ids) => _inner.GetDocDetails(ids);
        public IReadOnlyDictionary<int, List<string>> GetMissingWords(IReadOnlyList<int> docs, IReadOnlyList<int> words) =>
            _inner.GetMissingWords(docs, words);
    }

    private sealed class DocumentReader : IDocumentContentReader
    {
        public string? TryRead(string url) => "apple cherry";
    }
}
