# Contributing

## Running tests

This repository does not yet include an automated test project (there is no `*.Tests` project or test framework referenced in `Directory.Packages.props`), so verification is currently manual: run the API locally with `dotnet run`, which wires up `MockDatabase` in `Program.cs` in place of the not-yet-provisioned Postgres backend, and exercise the `POST /api/Search` endpoint (e.g. with `curl` or a REST client) to confirm search results come back as expected for both case-sensitive and case-insensitive queries.
