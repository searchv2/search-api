# search-api

A minimal ASP.NET Core API that searches documents indexed by word and returns the ones
matching a search query, ranked by how many query words each document
contains.

## How it works

- `POST /api/Search` takes a query (a list of words) and a case-sensitivity
  flag, and returns matching documents ordered by the number of query words
  they contain.
- `GET /api/documents/{id}` returns the raw text of an indexed document, for a
  client that wants to display it. The id comes from a search hit (`mId`). The
  document is read by the path stored in the index, so the API must run where
  those paths resolve (same machine or same mount as the indexer).
- Query words that aren't in the index at all are reported back as `Ignored`.
- For documents that match but don't contain every query word, the response
  also lists which words are `Missing` from that document.
- The search itself (`Application/SearchService.cs`) is backed by the
  `IDocumentIndex` port (`Core/Abstractions/`). The default implementation,
  `Infrastructure/Persistence/PostgresDocumentIndex.cs`, reads the PostgreSQL
  database (see `SearchDatabase`) that the `indexer` produces, holding `word`,
  `document`, and `occ` (word-document occurrence) tables. Run
  the indexer first, or switch `Infrastructure/DependencyInjection.cs` to
  `InMemoryDocumentIndex`, an in-memory stand-in seeded with a handful of sample
  documents.
- Search responses are shared between API replicas through Redis. Each query
  and case-sensitivity setting has its own cache entry with a one-minute sliding
  expiration. Cache hits and misses are exported as `searchv2.cache_hits` and
  `searchv2.cache_misses` when OpenTelemetry is enabled.
  The cache can be switched off at runtime with the `SearchCache` feature flag (see
  `k8s/README.md`, "Disabling a feature"); searches then go straight to PostgreSQL.

## Architecture

Onion architecture; dependencies point inward only:

| Layer | Folder | Contents |
|-------|--------|----------|
| Core | `Core/` | Domain entities (`Document`, `SearchHit`, `SearchOutcome`) and ports (`IDocumentIndex`, `IDocumentContentReader`). No framework or package dependencies. |
| Application | `Application/` | The `SearchService` use case and its `ISearchService` interface. Depends only on Core. |
| Infrastructure | `Infrastructure/` | Adapters behind the Core ports: `PostgresDocumentIndex`, `InMemoryDocumentIndex`, `FileDocumentContentReader`, `SearchDatabase`, plus DI wiring. |
| API | `Api/` | Controllers (`endpoints`), Redis search-response caching, and mapping between domain types and wire models. |

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

This starts the API against `PostgresDocumentIndex`, reading the tables the
indexer wrote. Configure the database with `SEARCH_DB_CONNECTION` (an Npgsql
connection string, default
`Host=localhost;Port=5432;Database=searchengine;Username=searcher;Gss Encryption Mode=Disable`)
and `SEARCH_DB_PASSWORD`. Each operation uses a pooled connection. To run without the indexer, switch the single DI
registration in `Infrastructure/DependencyInjection.cs` to `InMemoryDocumentIndex`
and its canned sample data.

The API connects to Redis at `RedisConnectionString` (default
`localhost:6379`). Set `REDISPASSWORD` and optionally `REDISUSER` when Redis
requires authentication. Redis is required for the search endpoint; local
Docker Compose starts it automatically. A Redis outage currently makes search
requests fail; document requests still read PostgreSQL directly.
After reindexing with Compose, restart `redis-cache` to clear results from the
old index. The Kubernetes startup script does this automatically.

## Docker

Use `../docker-compose.yml` for a local stack with Redis and the API replicas.
For a standalone API container, also provide a reachable Redis endpoint through
`RedisConnectionString` and credentials when authentication is enabled.

The image listens on `8080`; pass `SEARCH_DB_CONNECTION` and
`SEARCH_DB_PASSWORD` for the database the indexer wrote. Case-sensitive search
re-reads source files by the path stored in the index, so mount the document
folder at the same path the indexer used. See `../docker-compose.yml` for the
full local stack.

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

See [CONTRIBUTING.md](CONTRIBUTING.md) for how to verify changes. API tests live
in `../SearchAPI.Tests`; run them with `dotnet test ../SearchAPI.Tests/SearchAPI.Tests.csproj`.
