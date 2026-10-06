# Payment Gateway

A merchant sends card details and an amount. The gateway checks them, asks the acquiring bank (the simulator) for approval, stores the result and lets the merchant look the payment up later.

The full write-up with the design options I considered, diagrams and the user journey is in [docs/Design and Implementation.pdf](docs/Design%20and%20Implementation.pdf).

## How to run it

You need Docker. The .NET 8 SDK is only needed if you want to run the API outside Docker or run the tests.

**Everything in Docker (API + bank simulator)**

```bash
docker compose up -d --build
```

The API is then on http://localhost:5080.

**API locally, simulator in Docker**

```bash
docker compose up -d bank_simulator
dotnet run --project src/PaymentGateway.Api
```

Swagger is on http://localhost:5067/swagger when running locally.

**Tests**

```bash
dotnet test
```

The tests don't need Docker. They use fakes for the bank, so they also run in CI.

## Trying it out

```bash
curl -i -X POST http://localhost:5080/api/payments \
  -H "Content-Type: application/json" \
  -d '{"cardNumber":"2222405343248877","expiryMonth":4,"expiryYear":2030,"currency":"GBP","amount":100,"cvv":"123"}'
```

The simulator decides by the last digit of the card number:

- odd => Authorized
- even => Declined
- 0 => bank unavailable

## Endpoints

| Request | When | Response |
| --- | --- | --- |
| POST /api/payments | Bank authorizes or declines | 201 Created + Location header |
| POST /api/payments | Invalid request | 400, status Rejected + list of errors |
| POST /api/payments | Bank is down or times out | 502 Bad Gateway |
| GET /api/payments/{id} | Payment exists | 200 OK |
| GET /api/payments/{id} | Payment doesn't exist | 404 Not Found |
| GET /health | App is alive | 200 Healthy |

## How it's structured

```
PaymentsController   -> HTTP in, status codes out
PaymentService       -> validate, ask the bank, store, return a result
  PaymentRequestValidator     -> the six field rules
  AcquiringBankClient         -> talks to the bank, the only class that knows its format
  InMemoryPaymentsRepository  -> stores payments
```

I went with simple layers in one project. Each class has one job and can be tested on its own. I looked at Clean Architecture with separate projects and at a queue-based design, but for two endpoints both felt like over-engineering. More on that in the design document.

## Decisions worth mentioning

- **Card number and CVV are strings, not numbers.** As an int, a CVV of 012 becomes 12 and the bank rejects it.
- **The stored payment has no field for the CVV or the full card number.** Only the last four digits are kept, so card data can't be stored or returned by mistake.
- **Bank down means 502, never Declined.** A false decline would tell the shopper their card is bad when it isn't.
- **No automatic retries to the bank.** If the bank charged the card but the reply got lost, a retry could charge twice. Safe retries need idempotency keys.
- **All validation errors come back at once**, so the merchant can fix everything in one go.
- **A card is valid until the end of its expiry month.**

## What I changed in the template

Before writing anything I ran the template as it was, so I'd know if something broke because of me or was already broken.

- Updated NuGet packages that had known vulnerabilities.
- Fixed GET returning 204 instead of 404 for an unknown payment (the template's own test was failing).
- The request only had the last four card digits as an int, so the full card number couldn't be sent to the bank. Changed card number and CVV to strings.
- Merged the two identical response classes into one.
- Status is sent as text ("Authorized") instead of a number.
- Replaced the public List in the repository with a thread-safe dictionary behind an interface.
- Removed HTTPS redirection, since TLS for an API is normally handled at the load balancer.

## Testing

46 tests in four layers:

- **Validator** -> every rule at its boundaries, with a fixed clock so the expiry tests don't change month to month
- **Bank client** -> the exact format sent to the bank and every bank response, using a fake HTTP handler
- **Payment service** -> invalid requests never reach the bank, only the last four digits get stored
- **End to end** -> the real API running in memory with a fake bank

GitHub Actions runs the build and tests on every push.

## Assumptions

- Supported currencies are GBP, USD and EUR (uppercase).
- Amount is in minor units and must be above zero.
- Rejected requests aren't stored and don't get an id.
- Declined payments are stored, without an authorization code.
- Merchant authentication is out of scope.

## What I'd do next

- Idempotency keys so merchants can retry safely
- Merchant authentication, so each merchant only sees their own payments
- A real database, and tokenising card numbers
- A Pending state and reconciliation for bank timeouts
- Metrics and alerts on authorization rate and bank latency
- Upgrade to .NET 10, since .NET 8 support ends in November 2026
