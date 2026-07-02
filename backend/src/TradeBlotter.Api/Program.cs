using TradeBlotter.Api.Contracts;
using TradeBlotter.Api.Data;

// =============================================================================
// Trade Blotter API host.
//
// Minimal API composition root: wires configuration, the SQLite data layer, and
// the HTTP endpoints. The database schema is applied on startup from the checked-in
// schema.sql (idempotent), so a fresh clone gets a working DB with no manual setup.
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from configuration (appsettings / env). A relative
// "Data Source=blotter.db" resolves against the process working directory (the API
// project folder when run via `dotnet run`). Tests override this to a temp file.
var connectionString =
    builder.Configuration.GetConnectionString("Blotter") ?? "Data Source=blotter.db";

// Database is a singleton (holds only the connection string); the repository is a
// thin per-request wrapper over short-lived pooled connections.
builder.Services.AddSingleton(sp =>
    new Database(connectionString, sp.GetRequiredService<ILogger<Database>>()));
builder.Services.AddScoped<TradeRepository>();

var app = builder.Build();

// Apply schema on startup. schema.sql is copied next to the built assembly, so we
// resolve it relative to AppContext.BaseDirectory (stable across cwd/test hosts).
var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");
app.Services.GetRequiredService<Database>().ApplySchema(schemaPath);

// GET /trades — all trades, newest first.
app.MapGet("/trades", (TradeRepository repo) =>
    Results.Ok(repo.GetAllNewestFirst().Select(trade => trade.ToResponse())));

app.Run();

// Exposed so the test project's WebApplicationFactory<Program> can boot the host.
public partial class Program { }
