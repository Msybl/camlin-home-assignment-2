using Microsoft.EntityFrameworkCore;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlite<WalletDb>("Data Source=wallet.db");
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "CurrencyWalletAPI";
    config.Title = "Currency Wallet API";
    config.Version = "v1";
});
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "Currency Wallet API";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

app.MapGet("/", () => "Hello World!");

// POST /wallet/add
app.MapPost("/wallet/add", async (WalletRequest req, WalletDb db) =>
{
    db.WalletEntries.Add(new WalletEntry { UserId = "user1", Currency = req.Currency!, Amount = req.Amount });
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Currency added" });
});

// POST /wallet/subtract
app.MapPost("/wallet/subtract", async (WalletRequest req, WalletDb db) =>
{
    var existing = await db.WalletEntries
        .FirstOrDefaultAsync(e => e.UserId == "user1" && e.Currency == req.Currency);

    existing!.Amount -= req.Amount;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Currency subtracted" });
});

// GET /wallet
app.MapGet("/wallet", async (WalletDb db, IHttpClientFactory httpFactory) =>
{
    var entries = await db.WalletEntries
        .Where(e => e.UserId == "user1")
        .ToListAsync();

    var client = httpFactory.CreateClient();
    var walletResult = new List<WalletEntryResult>();
    decimal totalPln = 0;

    foreach (var entry in entries)
    {
        var url = $"https://api.nbp.pl/api/exchangerates/rates/c/{entry.Currency}/?format=json";
        var response = await client.GetFromJsonAsync<NbpResponse>(url);
        var rate = response?.Rates?.FirstOrDefault()?.Ask ?? 0;
        var plnValue = Math.Round(entry.Amount * rate, 2);
        totalPln += plnValue;

        walletResult.Add(new WalletEntryResult { Currency = entry.Currency, Amount = entry.Amount, Rate = rate, PlnValue = plnValue });
    }

    return Results.Ok(new { wallet = walletResult, total_pln = Math.Round(totalPln, 2) });
});

app.Run();
 
public class WalletRequest
{
    public string? Currency { get; set; }
    public decimal Amount { get; set; }
}

public class NbpResponse
{
    public string Currency { get; set; } = "";
    public string Code { get; set; } = "";
    public List<NbpRate> Rates { get; set; } = [];
}

public class NbpRate
{
    public string No { get; set; } = "";
    public string EffectiveDate { get; set; } = "";
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
}

public class WalletEntryResult
{
    public string? Currency { get; set; }
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
    public decimal PlnValue { get; set; }
}