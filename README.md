# search-api

A minimal ASP.NET Core API that indexes documents by word and returns the ones
matching a search query, ranked by how many query words each document
contains.

## How it works

- `POST /api/Search` takes a query (a list of words) and a case-sensitivity
  flag, and returns matching documents ordered by the number of query words
  they contain.
- Query words that aren't in the index at all are reported back as `Ignored`.
- For documents that match but don't contain every query word, the response
  also lists which words are `Missing` from that document.
- The search itself (`Application/SearchService.cs`) is backed by the
  `IDocumentIndex` port (`Core/Abstractions/`). The default implementation,
  `Infrastructure/Persistence/SqliteDocumentIndex.cs`, reads the local SQLite
  file in `searchv2/db/` (see `SearchDatabase`) that the `indexer` produces,
  holding `word`, `document`, and `Occ` (word-document occurrence) tables. Run
  the indexer first, or switch `Infrastructure/DependencyInjection.cs` to
  `InMemoryDocumentIndex`, an in-memory stand-in seeded with a handful of sample
  documents.

## Architecture

Onion architecture; dependencies point inward only:

| Layer | Folder | Contents |
|-------|--------|----------|
| Core | `Core/` | Domain entities (`Document`, `SearchHit`, `SearchOutcome`) and ports (`IDocumentIndex`, `IDocumentContentReader`). No framework or package dependencies. |
| Application | `Application/` | The `SearchService` use case and its `ISearchService` interface. Depends only on Core. |
| Infrastructure | `Infrastructure/` | Adapters behind the Core ports: `SqliteDocumentIndex`, `InMemoryDocumentIndex`, `FileDocumentContentReader`, `SearchDatabase`, plus DI wiring. |
| API | `Api/` | Controllers (`endpoints`), request/response `filters`, and the mapping between domain types and the wire models. |

The wire models (`SearchRequest`, `SearchResult`, `DocumentHit`, `BEDocument`)
live in the shared `SearchUtilities` package and are shared with the frontend.
Only the API layer references them; `Api/Mapping/SearchContractMapper.cs` maps
them to and from the domain's `SearchOutcome`, so the wire shape never leaks
inward.

## Setup

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

This project depends on `SearchUtilities`, a package that isn't published to
nuget.org yet. `NuGet.config` points at a local package source
(`../search-utilities/nupkg`), so a sibling checkout of that repo (built and
packed) needs to exist alongside this one for restore to succeed.

```bash
dotnet restore
```

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run
```

This starts the API against `SqliteDocumentIndex`, reading the SQLite file the
indexer wrote to `searchv2/db/` (override the location with the `SEARCH_DB_PATH`
environment variable). To run without the indexer, switch the single DI
registration in `Infrastructure/DependencyInjection.cs` to `InMemoryDocumentIndex`
and its canned sample data.

## Usage

Send a search request to `POST /api/Search`:

```bash
curl -X POST http://localhost:5071/api/Search \
  -H "Content-Type: application/json" \
  -d '{"Query": ["apple", "cherry"], "CaseSensitive": false}'
```

The response includes the matching documents (each with its hit count and any
missing query words), the words that were ignored because they aren't in the
index, and how long the search took.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for how to verify changes; there's no
automated test suite yet, so verification is manual against a locally running
instance.
