using Microsoft.EntityFrameworkCore;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WalletDb>(opt => opt.UseInMemoryDatabase("WalletDb"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "CurrencyWalletAPI";
    config.Title = "Currency Wallet API";
    config.Version = "v1";
});

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
app.MapGet("/wallet", async (WalletDb db) =>
{
    var entries = await db.WalletEntries
        .Where(e => e.UserId == "user1")
        .ToListAsync();

    return Results.Ok(new { wallet = entries });
});

app.Run();
 
record WalletRequest(string? Currency, decimal Amount);
