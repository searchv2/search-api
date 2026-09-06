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
- Search logic (`SearchLogic.cs`) is backed by an `IDatabase` abstraction
  (`IDatabase.cs`). The real implementation, `DatabasePostgres.cs`, talks to a
  Postgres database holding `word`, `document`, and `Occ` (word-document
  occurrence) tables. Until that database is provisioned, `Program.cs` wires
  up `MockDatabase.cs` instead, an in-memory stand-in seeded with a handful of
  sample documents.

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

This starts the API with `MockDatabase` (no real database needed) and its
canned sample data. Switching to `DatabasePostgres` just means changing the
single DI registration in `Program.cs`; it connects using the connection
string in `Paths.POSTGRES_DATABASE` (from `SearchUtilities`).

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
