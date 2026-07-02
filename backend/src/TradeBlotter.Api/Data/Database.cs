using Microsoft.Data.Sqlite;

namespace TradeBlotter.Api.Data;

/// <summary>
/// Owns the SQLite connection string and the schema/seed bootstrap.
/// </summary>
/// <remarks>
/// Registered as a singleton. Callers open a short-lived connection per unit of work
/// via <see cref="OpenConnection"/> — <c>Microsoft.Data.Sqlite</c> pools the underlying
/// handles, so this is cheap and keeps each operation self-contained. All SQL that
/// touches money is confined to reading/writing the canonical decimal string; the
/// numeric math happens in C# <see cref="decimal"/>.
/// </remarks>
public sealed class Database
{
    private readonly string _connectionString;
    private readonly ILogger<Database> _logger;

    public Database(string connectionString, ILogger<Database> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>Open a new, already-opened connection. Caller disposes it.</summary>
    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Apply <c>schema.sql</c> (idempotent — every statement is <c>CREATE ... IF NOT EXISTS</c>).
    /// Called once at startup so the DB structure comes from the checked-in script, not from
    /// implicit migrations.
    /// </summary>
    /// <param name="schemaSqlPath">Absolute path to the schema script.</param>
    public void ApplySchema(string schemaSqlPath)
    {
        var sql = File.ReadAllText(schemaSqlPath);
        ExecuteScript(sql);
        _logger.LogInformation("Applied database schema from {Path}", schemaSqlPath);
    }

    /// <summary>Execute a multi-statement SQL script in a single transaction.</summary>
    /// <remarks>
    /// Wrapping in a transaction keeps schema/seed application atomic: if any statement
    /// fails, nothing is left half-applied.
    /// </remarks>
    public void ExecuteScript(string sql)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}
