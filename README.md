# Pricing Engine

A microservice that calculates insurance product quotes. Built with **.NET 8**, **PostgreSQL**, and **RabbitMQ**, following Clean Architecture principles.

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│  HTTP Client                                         │
└────────────────────┬─────────────────────────────────┘
                     │ POST /api/quotes
┌────────────────────▼─────────────────────────────────┐
│  PricingEngine.Api                                   │
│  QuotesController → QuoteService                     │
└────────────────────┬─────────────────────────────────┘
                     │
        ┌────────────▼────────────┐
        │  PricingEngine.Domain   │  ← Zero external deps
        │  IPricingStrategy       │
        │  HomeBasicPricingStrategy│
        └─────────────────────────┘
                     │
        ┌────────────▼────────────┐
        │  PricingEngine.Application │
        │  QuoteService           │
        │  IQuoteRepository       │
        │  IUnitOfWork            │
        └─────────────────────────┘
                     │
        ┌────────────▼────────────┐
        │  PricingEngine.Infrastructure │
        │  EF Core + PostgreSQL   │
        │  MassTransit + RabbitMQ │
        │  OutboxProcessorService │
        └─────────────────────────┘
```

**Flow:**
1. `POST /api/quotes` → `QuoteService` resolves the strategy, loads config, calculates price.
2. In a single DB transaction: `Quote` record + `OutboxMessage` are written atomically.
3. `OutboxProcessorService` (background) polls the outbox and publishes `QuoteCreatedEvent` to RabbitMQ.
4. `QuoteAuditConsumer` receives the event and writes a permanent audit record to `quote_audit_log`.

Steps 3–4 happen asynchronously. If RabbitMQ is temporarily unavailable, the outbox retries until it succeeds — the audit trail is guaranteed.

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the one-command run)
- Or: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) + PostgreSQL 16 + RabbitMQ 3.13 (for local dev)

---

## Running with Docker Compose (recommended)

```bash
# Clone and enter the repo
git clone https://github.com/nikideli/Nikolay-Delikostov-PricingEngine.git
cd Nikolay-Delikostov-PricingEngine

# Start everything: PostgreSQL, RabbitMQ, and the API
docker compose up --build
```

That single command builds the image, starts all three containers, and runs database migrations. No other setup required.

| Service | URL |
|---|---|
| Public API (Quotes) | http://localhost:8080/api/quotes |
| Swagger UI | http://localhost:8080/swagger |
| Health check | http://localhost:8080/health |
| RabbitMQ Management | http://localhost:15672 |

> **Swagger tip:** use the dropdown in the top-right to switch between **Public API v1** and **Admin API**.
> Admin endpoints require the header `X-Admin-Key: admin-secret` — add it in the field Swagger shows at the top of each admin operation.

EF Core migrations run automatically on startup — no manual database setup needed.

To tear down (including volumes):
```bash
docker-compose down -v
```

---

## Running Locally (without Docker)

1. Start PostgreSQL and RabbitMQ (Docker or native):
   ```bash
   docker run -d --name pg -e POSTGRES_DB=pricing_engine -e POSTGRES_USER=pricing -e POSTGRES_PASSWORD=pricing_secret -p 5432:5432 postgres:16-alpine
   docker run -d --name rmq -e RABBITMQ_DEFAULT_USER=pricing -e RABBITMQ_DEFAULT_PASS=pricing_secret -p 5672:5672 rabbitmq:3.13-alpine
   ```

2. Update `src/PricingEngine.Api/appsettings.json` with your connection strings (defaults match the Docker commands above).

3. Run the API:
   ```bash
   dotnet run --project src/PricingEngine.Api
   ```

4. Run the tests:
   ```bash
   dotnet test
   ```

---

## API Usage

### Create a Quote

```http
POST http://localhost:8080/api/quotes
Content-Type: application/json

{
  "productCode": "HOME_BASIC",
  "inputs": {
    "insuredSum": 250000
  }
}
```

**Response (201 Created):**
```json
{
  "quoteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productCode": "HOME_BASIC",
  "breakdown": {
    "netPremium": 450.00,
    "taxes": 54.00,
    "fees": 25.00,
    "total": 529.00
  },
  "installmentPlans": [
    { "numberOfInstallments": 1, "amountPerInstallment": 529.00,  "totalAmount": 529.00 },
    { "numberOfInstallments": 2, "amountPerInstallment": 268.48,  "totalAmount": 536.96 },
    { "numberOfInstallments": 4, "amountPerInstallment": 136.50,  "totalAmount": 546.00 }
  ],
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**HOME_BASIC formula:**
```
NetPremium  = Round(insuredSum × 0.0018, 2)
Taxes       = Round(NetPremium × 0.12,   2)
Fees        = 25.00 (flat)
Total       = NetPremium + Taxes + Fees
```
All config values (tariff rate, tax rate, fees) live in the `product_configs` table. Changing a tariff requires only a DB row update — no redeployment.

### Retrieve a Quote

```http
GET http://localhost:8080/api/quotes/{quoteId}
```

---

## Admin API

Admin endpoints live under `/api/admin/` and are protected by the `X-Admin-Key` header.
The key is configured in `appsettings.json` under `Admin:ApiKey` (empty = open access for local dev; Docker sets it to `admin-secret`).

All three endpoints are visible in the **Admin API** Swagger document.

### List registered products

```http
GET http://localhost:8080/api/admin/products
X-Admin-Key: admin-secret
```

Returns every product code that has a registered pricing strategy — useful for validating deployments.

### Read active configuration

```http
GET http://localhost:8080/api/admin/configs/HOME_BASIC
X-Admin-Key: admin-secret
```

### Update (or create) a config value

```http
PUT http://localhost:8080/api/admin/configs/HOME_BASIC/tariff_rate
Content-Type: application/json
X-Admin-Key: admin-secret

{ "value": "0.0020" }
```

If the key already exists its value is updated in place. If it does not exist a new row is inserted, effective immediately. The next quote request picks up the new value — no restart required.

---

## Adding a New Product

This section demonstrates that the engine core never changes when adding a product. Here is the complete procedure for adding a **Motor Comprehensive** product with a tiered tariff and an age-based surcharge.

### Step 1 — Implement `IPricingStrategy`

Create `src/PricingEngine.Domain/Strategies/MotorComprehensivePricingStrategy.cs`:

```csharp
public sealed class MotorComprehensivePricingStrategy : IPricingStrategy
{
    public string ProductCode => "MOTOR_COMPREHENSIVE";

    public Task<PricingResult> CalculateAsync(PricingContext context, CancellationToken ct = default)
    {
        var vehicleValue  = context.GetRequiredDecimal("vehicleValue");
        var driverAge     = context.GetRequiredInt("driverAge");
        var vehicleAge    = context.GetRequiredInt("vehicleAge");

        // Tiered tariff: young drivers pay more
        var baseTariff          = context.GetConfigDecimal("base_tariff");
        var youngDriverSurcharge = driverAge < 25 ? context.GetConfigDecimal("young_driver_surcharge") : 0m;
        var oldVehicleSurcharge  = vehicleAge > 10 ? context.GetConfigDecimal("old_vehicle_surcharge")  : 0m;

        var effectiveTariff = baseTariff + youngDriverSurcharge + oldVehicleSurcharge;
        var netPremium      = Round(vehicleValue * effectiveTariff);
        var taxes           = Round(netPremium * context.GetConfigDecimal("tax_rate"));
        var fees            = context.GetConfigDecimal("fixed_fee");

        var breakdown = new PriceBreakdown(netPremium, taxes, fees);

        var plans = new[]
        {
            BuildPlan(1, breakdown.Total, 0m),
            BuildPlan(2, breakdown.Total, context.GetConfigDecimal("installment_fee_2x")),
            BuildPlan(4, breakdown.Total, context.GetConfigDecimal("installment_fee_4x")),
        };

        return Task.FromResult(new PricingResult(breakdown, plans));
    }

    private static InstallmentPlan BuildPlan(int count, decimal baseTotal, decimal surchargeRate)
    {
        var planTotal = Round(baseTotal * (1m + surchargeRate));
        return new InstallmentPlan(count, Round(planTotal / count), planTotal);
    }

    private static decimal Round(decimal v) =>
        decimal.Round(v, 2, MidpointRounding.AwayFromZero);
}
```

### Step 2 — Register in DI

In `src/PricingEngine.Infrastructure/Extensions/ServiceCollectionExtensions.cs`, add **one line**:

```csharp
services.AddSingleton<IPricingStrategy, HomeBasicPricingStrategy>();
services.AddSingleton<IPricingStrategy, MotorComprehensivePricingStrategy>();  // ← add this
```

### Step 3 — Seed configuration

**Option A — Source-controlled (recommended for baseline defaults):** add rows via EF `HasData` in a new migration so a fresh container always has working defaults.

**Option B — Admin API (no code change, no redeploy):**

```http
PUT http://localhost:8080/api/admin/configs/MOTOR_COMPREHENSIVE/base_tariff
Content-Type: application/json
X-Admin-Key: admin-secret

{ "value": "0.0450" }
```

Repeat for each key (`young_driver_surcharge`, `old_vehicle_surcharge`, `tax_rate`, `fixed_fee`, `installment_fee_2x`, `installment_fee_4x`). Use Option A for CI/CD environments; use Option B for quick adjustments in staging or production.

**That is the entire change.** The engine, the API, the outbox, and the audit trail all work immediately for `"productCode": "MOTOR_COMPREHENSIVE"` with no modifications to any shared code.

---

## Project Structure

```
src/
  PricingEngine.Domain/          # Entities, value objects, IPricingStrategy, strategies
  PricingEngine.Application/     # QuoteService, interfaces, DTOs, events
  PricingEngine.Infrastructure/  # EF Core, repositories, MassTransit, outbox processor
  PricingEngine.Api/             # ASP.NET Core controllers, middleware, startup

tests/
  PricingEngine.Tests.Unit/      # Pure unit tests; no DB or network dependencies
```

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **Strategy Pattern + DI** | Adding a product = 1 class + 1 DI line. The engine has zero knowledge of product specifics. |
| **JSONB for inputs** | Arbitrary product input shapes stored without schema migrations. |
| **Transactional Outbox** | Quote and OutboxMessage committed atomically. Audit delivery is guaranteed even if the broker is temporarily down. |
| **`decimal` + `MidpointRounding.AwayFromZero`** | Financial calculations must be deterministic. `float`/`double` are banned. |
| **Config in DB** | Tariff rates change without redeployment. EffectiveFrom/EffectiveTo support time-boxed tariffs. |
| **MassTransit** | Abstracts the broker (swap RabbitMQ for Azure Service Bus in one config change). Provides retry and dead-lettering out of the box. |
| **SELECT FOR UPDATE SKIP LOCKED** | Safe outbox processing under horizontal scale — competing instances take different rows. |
| **Admin API + key filter** | Config changes happen at runtime through protected endpoints. `AdminApiKeyFilter` is a no-op when the key is not configured (no friction locally), enforced in deployed environments. |
| **Dual Swagger documents** | Public and admin endpoints are separated into distinct OpenAPI specs. External consumers never see internal operations; the admin spec auto-injects the `X-Admin-Key` header field. |

## What I Would Add for Production

- **Authentication/Authorization** (OAuth2 + JWT with channel-level scopes for broker vs portal).
- **Rate limiting** on the quotes endpoint.
- **Proper EF Core migrations** (committed, reviewed, and run by a migration job, not auto-applied on startup).
- **OpenTelemetry** traces spanning the HTTP request through to the audit consumer.
- **Caching** for product configs (they rarely change; a short TTL cache avoids DB round-trips on every quote).
- **Integration tests** against a real PostgreSQL/RabbitMQ using `Testcontainers`.
