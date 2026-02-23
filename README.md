# Currency Wallet API
A REST API for tracking the current Polish złoty (PLN) value of money held in foreign currencies. It uses realtime exchange rates from the National Bank of Poland (NBP). Built with C# and ASP.NET Core. 

- Framework: ASP.NET Core Minimal API
- Database: SQLite (Entity Framework Core)
- HTTP Client: HttpClient from IHttpClientFactory
- Caching: IMemoryCache
- API Testing: Swagger UI

## Prerequisites
### Running with Docker
- Docker (v20+)
- Docker Compose (v2+)

### Running Locally
- .NET 10 SDK

## Quick Start
### Docker
```bash
# Clone the repository
git clone <repository-url>
cd camlin-home-assignment-2/CurrencyWalletApi

# Build and start
docker-compose up -d

# Test the API
curl http://localhost:5127/

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

### Local
```bash
# Run
dotnet run

# Build only
dotnet build
```

## Swagger UI
When running locally Swagger UI is available:
```
http://localhost:<port>/swagger
```
Use the "Authorize" button to enter your API key to test endpoints.

## Authentication
All endpoints require an API key passed in the `X-API-Key` header:

### Valid API Keys
| API Key | User |
|---------|------|
| `key-123` | user1 |
| `key-456` | user2 |
| `key-789` | user3 |

### Example
```bash
curl -H "X-API-Key: key-123" http://localhost:5127/wallet
```
## API Endpoints

### Get Wallet

```
GET /wallet
```

Returns the current wallet composition with realtime PLN values for each currency

**Headers:**
```
X-API-Key: key-123
```

**Response:**
```json
{
  "wallet": [
    {
      "currency": "EUR",
      "amount": 100.0,
      "rate": 4.26,
      "pln_value": 426.0
    },
    {
      "currency": "USD",
      "amount": 50.0,
      "rate": 3.61,
      "pln_value": 180.5
    }
  ],
  "total_pln": 606.5
}
```

---

### Add

```
POST /wallet/add
```

Adds an amount of a currency to the wallet. Creates the currency if it doesn't exist

**Headers:**
```
X-API-Key: key-123
Content-Type: application/json
```

**Request Body:**
```json
{
  "currency": "USD",
  "amount": 100.0
}
```

**Response:**
```json
{
  "message": "Currency added",
  "currency": "USD",
  "amount": 100.0,
  "total": 100.0
}
```

---

### Sub

```
POST /wallet/sub
```

Subtracts an amount of a currency from the wallet. Removes the currency if balance becomes zero

**Headers:**
```
X-API-Key: key-123
Content-Type: application/json
```

**Request Body:**
```json
{
  "currency": "USD",
  "amount": 30.0
}
```

**Response:**
```json
{
  "message": "Currency subtracted",
  "currency": "USD",
  "amount": 30.0,
  "total": 70.0
}
```
## Error Handling
| Code | Meaning |
|------|---------|
| 400 | Bad Request (invalid input, insufficient funds) |
| 401 | Unauthorized (missing/invalid API key) |
| 404 | Not Found |
| 500 | Internal Server Error (NBP API unavailable) |

## Example Usage
```bash
# Add EUR to wallet
curl -s -X POST http://localhost:5127/wallet/add \
    -H "X-API-Key: key-123" \
    -H "Content-Type: application/json" \
    -d '{"currency":"EUR","amount":75}'

# Add USD to wallet
curl -s -X POST http://localhost:5127/wallet/add \
    -H "X-API-Key: key-123" \
    -H "Content-Type: application/json" \
    -d '{"currency":"USD","amount":100}'

# Subtract from wallet
curl -s -X POST http://localhost:5127/wallet/subtract \
    -H "X-API-Key: key-123" \
    -H "Content-Type: application/json" \
    -d '{"currency":"USD","amount":30}'

# Get wallet PLN values (with live NBP rates)
curl -s -H "X-API-Key: key-123" http://localhost:5127/wallet
```

## Decisions & Notes
- EF Core replaced the raw SQLite3 C API used in the C++ version. No manual SQL strings, no parameter binding. EF Core generates and executes SQL from C# LINQ queries automatically
- Decimal type is used for all currency amounts and rates. No floating-point errors (double used in the C++ version for simplicity)
- Exchange rates are cached for 1 hour using IMemoryCache
- Hardcoded API keys with X-API-Key header. It is the same simple approach as the C++ version. As an improvement, I would use JWT tokens
- A currency is removed from the wallet when the balance reaches zero. It is the same as the C++ version (avoids storing unnecessary empty records)
