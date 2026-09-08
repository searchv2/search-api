using Microsoft.Extensions.DependencyInjection;
using SearchAPI.Core.Abstractions;
using SearchAPI.Infrastructure.Persistence;

namespace SearchAPI.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the concrete adapters behind the Core ports.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Reads the local SQLite index in searchv2/db that the indexer produces. Swap for
        // InMemoryDocumentIndex to serve a canned dataset without running the indexer.
        services.AddSingleton<IDocumentIndex, SqliteDocumentIndex>();
        services.AddSingleton<IDocumentContentReader, FileDocumentContentReader>();
        return services;
    }
}
