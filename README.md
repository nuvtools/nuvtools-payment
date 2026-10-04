# NuvTools Payment Libraries

Payment abstraction library and API clients for Brazilian financial services. Part of the [NuvTools Framework](https://nuvtools.com).

## Projects

| Project | Target | Description |
|---------|--------|-------------|
| **NuvTools.Payment** | net8.0, net9.0, net10.0 | Core payment abstractions: the card, payee, charge, refund and webhook contracts a card provider implements, their DTOs, and the shared HttpClient infrastructure. |
| **NuvTools.Payment.BancoDoBrasil.ApiClient** | net10.0 | API client for Banco do Brasil bank slip payment services. |
| **NuvTools.Payment.Sicoob.ApiClient** | net10.0 | API client for Sicoob bank slip services. |
| **NuvTools.Payment.Omie.ApiClient** | net10.0 | API client for Omie ERP: service orders, accounts receivable, and bank slip (boleto) generation. |
| **NuvTools.Payment.Stripe.ApiClient** | net10.0 | Stripe's implementation of the `NuvTools.Payment` contracts: customers and saved payment methods, Connect Express onboarding, separate charges and transfers, refunds, and webhook signature verification. |

## NuvTools.Payment — the card-provider contracts

A marketplace that charges customers and pays several payees out of the same money needs five
capabilities, and they are declared here so that the application layer never references a provider:

| Contract | What it is for |
|---|---|
| `IPaymentCustomerClient` | Who is charged, and the payment method they saved — including a provider-hosted page, so no card and no provider script reach the caller's own origin |
| `IPayeeAccountClient` | An account the platform can pay, and the hosted onboarding that makes it payable |
| `IPaymentChargeClient` | Charging a customer, and transferring to a payee — two calls, never one destination charge |
| `IPaymentRefundClient` | Giving money back, in whole or in part. Its own contract because refunding is not the inverse of charging: it names a payment rather than a customer, and a caller that may charge is not automatically one that may refund |
| `IPaymentWebhookVerifier` | The signature check, and the payload read into the fields a caller acts on |

Amounts are always in the currency's **minor units**; `PaymentMoney` converts, rounding once.
`PaymentStatusType` and `PaymentEventType` are the provider-neutral vocabularies a client maps its
own onto — four payment outcomes and five kinds of event, because those are the ones that change what
the caller does next.

**Refunding does not claw back a payee.** The money already transferred is the caller's to reconcile,
by netting it off what that payee is owed next. Providers do offer a transfer reversal, and using it
would take money out of an account the payee may have already spent from — so who absorbs a refund
stays a decision the caller makes in its own records.

**The provider-shaped reading belongs in the provider's package.** `IPaymentWebhookVerifier` answers
a `PaymentEventDTO` whose `CustomerId`, `PaymentMethodId` and `FailureReason` are already extracted:
which field of which payload holds the customer is the provider's business, and a caller that dug it
out itself would be coupled to a shape it does not control.

> These contracts are for **card and marketplace** providers. The bank-slip clients below stay
> direct, for the reason given under *Common Patterns*.

## NuvTools.Payment.Stripe.ApiClient

Stripe's implementation of those contracts, over the official `Stripe.net` SDK.

### Features

- Customers and saved payment methods (`SetupIntent`, off-session, and a hosted Checkout page)
- Connect **Express** onboarding: account, hosted account link, and account status
- **Separate charges and transfers** - one charge can pay several connected accounts, which a
  destination charge cannot express
- Full and partial **refunds**, deliberately without `ReverseTransfer`
- Caller-supplied idempotency keys on every money-moving call
- Webhook signature verification, with a separate secret for the Connect endpoint

### Configuration

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "ConnectWebhookSecret": "",
    "ConnectAccountType": "express",
    "ConnectCapabilities": [ "transfers" ],
    "PaymentMethodTypes": [],
    "RequestTimeoutSeconds": 30,
    "MaxNetworkRetries": 2
  }
}
```

The key and both webhook secrets are secrets: they are what authenticate a call and make an incoming
webhook trustworthy.

The rest are **the application's decisions, not this package's**:

| Setting | Default | |
|---|---|---|
| `ConnectAccountType` | `express` | The kind of connected account created for a payee: `express`, `standard` or `custom`. |
| `ConnectCapabilities` | `transfers` | What a connected account asks for, by Stripe's names. Stripe decides per country which combinations it accepts — Brazil refuses `transfers` without `card_payments` — so an application lists what its payees' countries need. |
| `PaymentMethodTypes` | empty | The methods a customer may save on the hosted page, by Stripe's names. Empty offers whatever the Stripe account has enabled for the currency of the request; an application that can only charge cards afterwards lists `card`. |

The currency is not a setting: it changes per customer, so it travels in
`HostedPaymentMethodPageRequest.CurrencyCode`.

### Registration

```csharp
services.AddStripeApiClient(configuration);
```

Injects Stripe behind `IPaymentCustomerClient`, `IPayeeAccountClient`, `IPaymentChargeClient`,
`IPaymentRefundClient` and `IPaymentWebhookVerifier` — so this call, in the composition root, is the
only place the application names Stripe. Each method returns `IResult<T>`, so a declined card is a result to read rather than an
exception to catch.

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

Typed HTTP client for the Omie ERP API (service orders, accounts receivable, and bank slip issuance).

### Features

- Client lookup
- Service registration query
- Service order (OS) creation
- Accounts receivable entry creation
- Bank slip (boleto) generation and retrieval

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

All API clients share the same infrastructure, provided by `NuvTools.Payment`:

- **Result pattern**: every method returns `IResult<T>` from `NuvTools.Common.ResultWrapper` (`Succeeded`, `Data`, `Message`, `Messages`, `ResultType`).
- **Standard resilience**: registered via `services.AddPaymentResilientHttpClient<TInterface, TImpl>(name)` — applies retry, circuit breaker, and timeout policy across all providers.
- **Configuration binding** from `IConfiguration` sections using the Options pattern.

The bank-slip and ERP clients expose their own contracts (`ISicoobBankSlipApiClient`, `IBbBankSlipPaymentApiClient`, `IOmieApiClient`) and return provider-specific DTOs, deliberately: Brazilian banking integrations are intrinsically provider-shaped, and a neutral layer over them would have to invent a domain none of them share. The card and marketplace side is different — the four capabilities are the same everywhere — so it is declared in `NuvTools.Payment` and implemented by the provider's package.

> **Note (Omie):** `OmieApiClient` is registered as a singleton and uses an internal static `HttpClient`. It intentionally bypasses `HttpClientFactory` and the standard resilience pipeline — Omie's gateway misbehaves with HTTP/2 and Polly retry. Do not change this without re-validating against the Omie sandbox.

## Build

```bash
dotnet build NuvTools.Payment.slnx
```

## Releasing

Publishing to nuget.org is irreversible per version, so the workflow makes it the last and smallest
step.

**One workflow per library.** Each package has its own, so a run and an approval each concern
exactly one package. The steps themselves are shared, in the reusable `package-publish.yml`.

| Package | Workflow | Release tag |
|---|---|---|
| `NuvTools.Payment` | `payment-publish.yml` | `payment-vX.Y.Z` |
| `NuvTools.Payment.Stripe.ApiClient` | `stripe-publish.yml` | `stripe-vX.Y.Z` |
| `NuvTools.Payment.BancoDoBrasil.ApiClient` | `bancodobrasil-publish.yml` | `bancodobrasil-vX.Y.Z` |
| `NuvTools.Payment.Omie.ApiClient` | `omie-publish.yml` | `omie-vX.Y.Z` |
| `NuvTools.Payment.Sicoob.ApiClient` | `sicoob-publish.yml` | `sicoob-vX.Y.Z` |

1. Set the package's `<Version>` in its `.csproj` and merge it. That is the version that gets
   published; the workflow reads it and never computes one.
2. Run the library's workflow by hand (*Actions → Stripe Publish → Run workflow*) with *push*
   checked.
3. The workflow builds the solution with `-warnaserror`, runs the tests, takes **that package only**
   from the build output, verifies it carries its README, licence, icon, symbols and every target
   framework, and then waits for approval on the `nuget` GitHub Environment before pushing.
4. **After the push succeeds, the workflow tags the commit** with the tag in the table above. The tag is a
   record of what is on nuget.org, never what starts a release, so it cannot exist for a version
   that did not get there. A run whose tag already exists stops before building: the version has
   been released and `<Version>` needs a bump.

**`NuvTools.Payment` goes first.** The Stripe, Banco do Brasil and Sicoob clients reference it as a
project, so each of their packages depends on whatever `<Version>` `NuvTools.Payment` has in that
commit. The workflow checks that version is on nuget.org and refuses to push a client whose
dependency is not there yet.

**No API key is stored.** The push uses nuget.org's trusted publishing: the job presents a GitHub
OIDC token, nuget.org checks it against the policy registered for this repository and answers with a
key that lives for one hour. The repository needs one secret, `NUGET_USER` — the nuget.org profile
name the policy belongs to — and the policy itself, registered under *Trusted Publishing* on
nuget.org with the owner `nuvtools`, the repository `nuvtools-payment`, the workflow file and the
environment `nuget`.

A version that is already on nuget.org is skipped, not replaced: the push uses `--skip-duplicate`,
so re-running a release is harmless and publishing a change always needs a new `<Version>`.

Run a library's workflow by hand with *push* unchecked for a dry run that stops after the
verification.
