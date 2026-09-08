using Microsoft.Extensions.DependencyInjection;

namespace SearchAPI.Application;

public static class DependencyInjection
{
    /// <summary>Registers the application layer's use cases.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IDocumentService, DocumentService>();
        return services;
    }
}
