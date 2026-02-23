using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
builder.Services.AddMemoryCache();

var app = builder.Build();

app.Services.GetRequiredService<WalletDb>().Database.EnsureCreated();

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

app.MapGet("/", () => "Camlin Home Assignment with C#");

// POST /wallet/add
app.MapPost("/wallet/add", async (WalletRequest req, WalletDb db, HttpContext context) =>
{
    var userId = GetUserId(context);
    if (userId == null)
        return Results.Unauthorized();

    if (string.IsNullOrEmpty(req.Currency) || req.Currency.Length != 3)
        return Results.BadRequest(new { error = "Currency must be a 3 letter code" });

    if (req.Amount <= 0)
        return Results.BadRequest(new { error = "Amount must be positive" });

    var currency = req.Currency.ToUpper();

    var existing = await db.WalletEntries
        .FirstOrDefaultAsync(e => e.UserId == userId && e.Currency == currency);

    if (existing == null)
        db.WalletEntries.Add(new WalletEntry { UserId = userId, Currency = currency, Amount = req.Amount });
    else
        existing.Amount += req.Amount;

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Currency added" });
});

// POST /wallet/subtract
app.MapPost("/wallet/subtract", async (WalletRequest req, WalletDb db, HttpContext context) =>
{
    var userId = GetUserId(context);
    if (userId == null)
        return Results.Unauthorized();

    if (string.IsNullOrEmpty(req.Currency) || req.Currency.Length != 3)
        return Results.BadRequest(new { error = "Currency must be a 3 letter code" });

    if (req.Amount <= 0)
        return Results.BadRequest(new { error = "Amount must be positive" });

    var currency = req.Currency.ToUpper();

    var existing = await db.WalletEntries
        .FirstOrDefaultAsync(e => e.UserId == userId && e.Currency == currency);

    if (existing == null)
        return Results.BadRequest(new { error = "Currenccy doesn't exist in the wallet" });

    if (existing.Amount < req.Amount)
        return Results.BadRequest(new { error = "Insufficient funds", available = existing.Amount, requested = req.Amount });

    existing.Amount -= req.Amount;

    if (existing.Amount == 0)
        db.WalletEntries.Remove(existing);

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Currency subtracted" });
});

// GET /wallet
app.MapGet("/wallet", async (WalletDb db, IHttpClientFactory httpFactory, HttpContext context, IMemoryCache cache) =>
{
    var userId = GetUserId(context);
    if (userId == null)
        return Results.Unauthorized();

    var entries = await db.WalletEntries
        .Where(e => e.UserId == userId)
        .ToListAsync();

    var client = httpFactory.CreateClient();
    var walletResult = new List<WalletEntryResult>();
    decimal totalPln = 0;

    foreach (var entry in entries)
{
    // Check cache. If not exist fetch from NBP
    if (!cache.TryGetValue(entry.Currency, out decimal rate))
    {
        var url = $"https://api.nbp.pl/api/exchangerates/rates/c/{entry.Currency}/?format=json";
        var response = await client.GetFromJsonAsync<NbpResponse>(url);
        rate = response?.Rates?.FirstOrDefault()?.Ask ?? 0;
        cache.Set(entry.Currency, rate, TimeSpan.FromHours(1));
    }

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