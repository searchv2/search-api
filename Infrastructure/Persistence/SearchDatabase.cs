namespace SearchAPI.Infrastructure.Persistence;

/// <summary>
/// Location of the local SQLite index that lives in <c>searchv2/db</c>. The indexer writes
/// this file; SearchAPI opens the same file to serve queries. Point both services at one
/// path so a full local stack is just "run the indexer, then run the API" - no server to
/// provision.
/// </summary>
/// <remarks>
/// Override with the <c>SEARCH_DB_PATH</c> environment variable when the checkout is not at
/// the default location. The indexer must have run at least once to create the file.
/// </remarks>
public static class SearchDatabase
{
    /// <summary>Absolute path to the SQLite database file.</summary>
    public static string DatabaseFile { get; } =
        Environment.GetEnvironmentVariable("SEARCH_DB_PATH")
        ?? "/home/miso/School/apip/searchv2/db/searchmedium.db";

    /// <summary>Connection string for <see cref="DatabaseFile"/>.</summary>
    public static string ConnectionString { get; } = $"Data Source={DatabaseFile}";
}
