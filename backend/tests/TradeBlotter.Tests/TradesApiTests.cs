using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using TradeBlotter.Api.Contracts;
using Xunit;

namespace TradeBlotter.Tests;

/// <summary>
/// End-to-end endpoint smoke tests over a real (temporary) SQLite database. Each test
/// gets its own DB file via <see cref="ApiFactory"/>, and the app applies schema.sql on
/// startup exactly as it does in production.
/// </summary>
public class TradesApiTests
{
    /// <summary>
    /// Boots the API against a throwaway SQLite file so tests don't share state and don't
    /// touch the developer's dev database.
    /// </summary>
    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath =
            Path.Combine(Path.GetTempPath(), $"blotter-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Overrides ConnectionStrings:Blotter that Program.cs reads.
            builder.UseSetting("ConnectionStrings:Blotter", $"Data Source={_dbPath}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // Release pooled handles before deleting the file.
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task PostThenGet_RoundTripsAndPersists()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        // Lowercase + trailing space to prove server-side normalization.
        var response = await client.PostAsJsonAsync("/trades",
            new { symbol = "aapl ", side = "Buy", quantity = 100, price = 150.25 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TradeResponse>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("AAPL", created.Symbol);
        Assert.Equal(15025.00m, created.Notional);
        Assert.False(string.IsNullOrWhiteSpace(created.Timestamp));

        var trades = await client.GetFromJsonAsync<List<TradeResponse>>("/trades");
        Assert.Single(trades!);
        Assert.Equal("AAPL", trades![0].Symbol);
    }

    [Fact]
    public async Task Positions_ReflectMovingAverageAccounting()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/trades", new { symbol = "AAPL", side = "Buy", quantity = 100, price = 150 });
        await client.PostAsJsonAsync("/trades", new { symbol = "AAPL", side = "Buy", quantity = 50, price = 155 });
        await client.PostAsJsonAsync("/trades", new { symbol = "AAPL", side = "Sell", quantity = 60, price = 160 });

        var positions = await client.GetFromJsonAsync<List<PositionResponse>>("/positions");
        var aapl = Assert.Single(positions!);
        Assert.Equal("AAPL", aapl.Symbol);
        Assert.Equal(90, aapl.NetQuantity);
        Assert.Equal(151.67m, aapl.AverageCost);
    }

    [Fact]
    public async Task Post_ZeroQuantity_Returns400WithEnvelope()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/trades",
            new { symbol = "AAPL", side = "Buy", quantity = 0, price = 150 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("VALIDATION_ERROR", envelope!.Error.Code);
        Assert.Equal("quantity", envelope.Error.Field);
    }

    [Fact]
    public async Task Post_MissingSymbol_Returns400OnSymbolField()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/trades",
            new { symbol = "   ", side = "Buy", quantity = 10, price = 150 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("symbol", envelope!.Error.Field);
    }

    [Fact]
    public async Task Post_FractionalQuantity_Returns400()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/trades",
            new { symbol = "AAPL", side = "Buy", quantity = 100.5, price = 150 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("quantity", envelope!.Error.Field);
    }

    [Fact]
    public async Task Post_MalformedJson_Returns400()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/trades",
            new StringContent("{ this is not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
