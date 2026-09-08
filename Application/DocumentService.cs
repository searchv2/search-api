using SearchAPI.Core.Abstractions;

namespace SearchAPI.Application;

/// <summary>
/// Looks the document's url up in the index, then reads its content through the
/// <see cref="IDocumentContentReader"/> port. Pure application logic - depends only on Core.
/// </summary>
public sealed class DocumentService : IDocumentService
{
    private readonly IDocumentIndex _index;
    private readonly IDocumentContentReader _content;

    public DocumentService(IDocumentIndex index, IDocumentContentReader content)
    {
        _index = index;
        _content = content;
    }

    public string? GetContent(int documentId)
    {
        var details = _index.GetDocDetails(new[] { documentId });
        return details.TryGetValue(documentId, out var doc)
            ? _content.TryRead(doc.Url)
            : null;
    }
}
