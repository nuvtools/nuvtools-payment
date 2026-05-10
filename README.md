# NuvTools Payment Libraries

Payment abstraction library and API clients for Brazilian financial services. Part of the [NuvTools Framework](https://nuvtools.com).

## Projects

| Project | Target | Description |
|---------|--------|-------------|
| **NuvTools.Payment** | net8.0, net9.0, net10.0 | Core payment abstractions for credit card, bank payment slip and other operations. |
| **NuvTools.Payment.BancoDoBrasil.ApiClient** | net10.0 | API client for Banco do Brasil bank slip payment services. |
| **NuvTools.Payment.Sicoob.ApiClient** | net10.0 | API client for Sicoob bank slip services. |
| **NuvTools.Payment.Omie.ApiClient** | net10.0 | API client for Omie ERP invoicing and service order services. |

## NuvTools.Payment.BancoDoBrasil.ApiClient

Typed HTTP client for the Banco do Brasil Payments API with OAuth2 authentication.

### Features

- OAuth2 token generation (`client_credentials`)
- Batch bank slip payment creation
- Payment query by ID

### Configuration

```json
{
  "BancoDoBrasil": {
    "AuthUrl": "https://...",
    "BaseUrl": "https://...",
    "ClientId": "",
    "ClientSecret": "",
    "ApiKey": ""
  }
}
```

### Registration

```csharp
services.AddBancoDoBrasilApiClient(configuration);
```

## NuvTools.Payment.Sicoob.ApiClient

Typed HTTP client for the Sicoob Banking API (bank slips).

### Features

- Bank slip query by number or period
- Bank slip creation
- Second copy generation
- Cancellation, due date extension, and amount change

### Configuration

```json
{
  "Sicoob": {
    "BaseUrl": "https://...",
    "ClientId": "",
    "Token": ""
  }
}
```

### Registration

```csharp
services.AddSicoobApiClient(configuration);
```

## NuvTools.Payment.Omie.ApiClient

Typed HTTP client for the Omie ERP API (service orders and invoicing).

### Features

- Client lookup
- Service registration query
- Service order creation
- Service order invoicing

### Configuration

```json
{
  "Omie": {
    "AppKey": "",
    "AppSecret": "",
    "BaseUrlClient": "https://...",
    "BaseUrlService": "https://...",
    "BaseUrlOrderService": "https://...",
    "BaseUrlOrderBilling": "https://..."
  }
}
```

### Registration

```csharp
services.AddOmieApiClient(configuration);
```

## Common Patterns

All API clients share the same architecture, defined in `NuvTools.Payment`:

- **Result pattern**: every method returns `IResult<T>` from `NuvTools.Common.ResultWrapper` (`Succeeded`, `Data`, `Message`, `Messages`, `ResultType`).
- **Standard resilience**: registered via `services.AddPaymentResilientHttpClient<TInterface, TImpl>(name)` — applies retry, circuit breaker, and timeout policy across all providers.
- **Configuration binding** from `IConfiguration` sections using the Options pattern.

Two-tier API:

- **Provider-neutral contracts** in `NuvTools.Payment.Contracts` — portable code targets these:
  - `IBankSlipIssuanceClient` (Sicoob) — issue/get/list/cancel/extend/change bank slips.
  - `IBankSlipBatchPaymentClient` (Banco do Brasil) — authenticate, create batch, query batch.
  - `IBankSlipBilletQuery` (Omie) — retrieve billet info from an existing receivable.
- **Provider-specific contracts** in each `*.ApiClient` package (e.g. `ISicoobBankSlipApiClient`, `IBbBankSlipPaymentApiClient`, `IOmieApiClient`) — inherit the neutral contract and additionally expose raw provider DTOs and provider-only fields (e.g. Sicoob's `codigoNegativacao`, Omie's full ERP workflow).

Both interfaces resolve to the same instance, so use whichever fits the call site.

> **Note (Omie):** `OmieApiClient` is registered as a singleton and uses an internal static `HttpClient`. It intentionally bypasses `HttpClientFactory` and the standard resilience pipeline — Omie's gateway misbehaves with HTTP/2 and Polly retry. Do not change this without re-validating against the Omie sandbox.

## Build

```bash
dotnet build NuvTools.Payment.slnx
```
