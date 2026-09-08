using SearchAPI.Core.Abstractions;

namespace SearchAPI.Infrastructure;

/// <summary>Reads document content from the local filesystem.</summary>
public sealed class FileDocumentContentReader : IDocumentContentReader
{
    public string? TryRead(string url)
    {
        try
        {
            return File.ReadAllText(url);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
