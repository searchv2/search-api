namespace SearchAPI.Core.Abstractions;

/// <summary>
/// Reads the raw text of a document from wherever it is stored. Keeps file/network I/O out
/// of the domain so case-sensitive matching can be tested without a filesystem.
/// </summary>
public interface IDocumentContentReader
{
    /// <summary>The document's text, or <c>null</c> if it cannot be read.</summary>
    string? TryRead(string url);
}
