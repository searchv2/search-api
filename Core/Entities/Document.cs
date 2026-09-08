namespace SearchAPI.Core.Entities;

/// <summary>
/// A document known to the search index. This is the domain's own shape - the outward
/// wire form (<c>Shared.Model.BEDocument</c>) is a concern of the API layer only.
/// </summary>
public sealed class Document
{
    public required int Id { get; init; }

    public required string Url { get; init; }

    public required string IndexedAt { get; init; }

    public required string CreatedAt { get; init; }
}
