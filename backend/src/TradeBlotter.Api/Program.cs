using System.Text.Json;
using TradeBlotter.Api.Contracts;
using TradeBlotter.Api.Data;
using TradeBlotter.Api.Services;
using TradeBlotter.Api.Validation;

// =============================================================================
// Trade Blotter API host.
//
// Minimal API composition root: wires configuration, the SQLite data layer, and
// the HTTP endpoints. The database schema is applied on startup from the checked-in
// schema.sql (idempotent), so a fresh clone gets a working DB with no manual setup.
// =============================================================================

// `--seed` is a one-shot flag, not host configuration. Detect it up front and keep it
// out of the args passed to the builder (the command-line config provider would choke on
// a valueless switch).
var seedRequested = args.Contains("--seed");
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--seed").ToArray());

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

// CORS: the Vue dev server runs on a different origin than the API, so the SPA needs an
// explicit allowance to call it in development. Both localhost and 127.0.0.1 forms of the
// Vite port are permitted since browsers may use either.
const string SpaCorsPolicy = "SpaCors";
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Unexpected errors -> logged by the middleware and returned as a generic 500 envelope
// (no stack traces leak to the client).
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(
        ErrorEnvelope.Of("INTERNAL_ERROR", "An unexpected error occurred."));
}));

app.UseCors(SpaCorsPolicy);

// Apply schema on startup. schema.sql is copied next to the built assembly, so we
// resolve it relative to AppContext.BaseDirectory (stable across cwd/test hosts).
var database = app.Services.GetRequiredService<Database>();
database.ApplySchema(Path.Combine(AppContext.BaseDirectory, "schema.sql"));

// `dotnet run -- --seed`: load demo data, then exit without starting the web server.
if (seedRequested)
{
    database.Seed(Path.Combine(AppContext.BaseDirectory, "seed.sql"));
    return;
}

// POST /trades — validate, persist, and return the created trade (201).
// The body is read manually so malformed JSON and every validation failure return the
// same error envelope, rather than the framework's default problem-details 400.
app.MapPost("/trades", async (HttpContext http, TradeRepository repo) =>
{
    CreateTradeRequest? request;
    try
    {
        request = await http.Request.ReadFromJsonAsync<CreateTradeRequest>();
    }
    catch (JsonException)
    {
        return Results.Json(
            ErrorEnvelope.Validation("Request body is not valid JSON.", null),
            statusCode: StatusCodes.Status400BadRequest);
    }

    var (command, error) = TradeValidator.Validate(request);
    if (error is not null)
        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);

    var trade = repo.InsertTrade(command!);
    return Results.Created($"/trades/{trade.Id}", trade.ToResponse());
});

// GET /trades — all trades, newest first.
app.MapGet("/trades", (TradeRepository repo) =>
    Results.Ok(repo.GetAllNewestFirst().Select(trade => trade.ToResponse())));

// GET /positions — positions derived from trade history (net qty + moving-average cost).
// Zero-net symbols are omitted by the calculator.
app.MapGet("/positions", (TradeRepository repo) =>
    Results.Ok(PositionCalculator.Calculate(repo.GetAllChronological())
        .Select(position => position.ToResponse())));

// Unknown routes -> 404 with the same error-envelope shape as other errors.
app.MapFallback(() => Results.Json(
    ErrorEnvelope.Of("NOT_FOUND", "The requested resource was not found."),
    statusCode: StatusCodes.Status404NotFound));

app.Run();

// Exposed so the test project's WebApplicationFactory<Program> can boot the host.
public partial class Program { }
