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
    config.AddSecurity("ApiKey", new NSwag.OpenApiSecurityScheme
    {
        Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = NSwag.OpenApiSecurityApiKeyLocation.Header,
        Description = "Enter your API key"
    });
    config.OperationProcessors.Add(new NSwag.Generation.Processors.Security.OperationSecurityScopeProcessor("ApiKey"));
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
app.MapPost("/wallet/add", async (WalletRequest req, WalletDb db, HttpContext context) =>
{
    var userId = GetUserId(context);
    if (userId is null)
        return Results.Unauthorized();

    db.WalletEntries.Add(new WalletEntry { UserId = userId, Currency = req.Currency!, Amount = req.Amount });
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Currency added" });
});

// POST /wallet/subtract
app.MapPost("/wallet/subtract", async (WalletRequest req, WalletDb db, HttpContext context) =>
{
    var userId = GetUserId(context);
    if (userId is null)
        return Results.Unauthorized();

    var existing = await db.WalletEntries
        .FirstOrDefaultAsync(e => e.UserId == userId && e.Currency == req.Currency);

    existing!.Amount -= req.Amount;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Currency subtracted" });
});

// GET /wallet
app.MapGet("/wallet", async (WalletDb db, IHttpClientFactory httpFactory, HttpContext context) =>
{
    var userId = GetUserId(context);
    if (userId is null)
        return Results.Unauthorized();

    var entries = await db.WalletEntries
        .Where(e => e.UserId == userId)
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

string? GetUserId(HttpContext context)
{
    var apiKey = context.Request.Headers["X-API-Key"].ToString();

    if (apiKey == "key-123") return "user1";
    if (apiKey == "key-456") return "user2";
    if (apiKey == "key-789") return "user3";

    return null;
}

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