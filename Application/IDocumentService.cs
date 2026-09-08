namespace SearchAPI.Application;

/// <summary>
/// Reads the full text of an indexed document, for a client that wants to display it.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// The document's text, or <c>null</c> if no indexed document has that id or its
    /// content cannot be read.
    /// </summary>
    string? GetContent(int documentId);
}
