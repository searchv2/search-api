using Npgsql;

namespace SearchAPI.Infrastructure.Persistence;

/// <summary>
/// Connection to the PostgreSQL database that holds the index. The indexer writes it;
/// SearchAPI connects to the same database to serve queries.
/// </summary>
/// <remarks>
/// Set <c>SEARCH_DB_CONNECTION</c> to an Npgsql connection string and
/// <c>SEARCH_DB_PASSWORD</c> to the password, so the password can come from a Kubernetes
/// Secret. The default targets a local port-forward to the cluster database. The indexer
/// must have run at least once to create the tables.
/// </remarks>
public static class SearchDatabase
{
    public static string ConnectionString
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("SEARCH_DB_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=searchengine;Username=searcher;Gss Encryption Mode=Disable");
            var password = Environment.GetEnvironmentVariable("SEARCH_DB_PASSWORD");
            if (!string.IsNullOrEmpty(password))
                builder.Password = password;
            return builder.ConnectionString;
        }
    }
}
