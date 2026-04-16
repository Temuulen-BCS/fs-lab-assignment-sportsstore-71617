# Sports Solution

This repository contains a small multi-project commerce solution centered around the `SportsStore` backend.

## Projects

- `SportsStore`: ASP.NET Core 8 MVC application with Razor Pages, Server-Side Blazor, SQL Server persistence, Stripe checkout, RabbitMQ messaging, and ASP.NET Identity.
- `CustomerPortal`: ASP.NET Core 8 Razor Components frontend that calls the `SportsStore` HTTP API.
- `AdminDashboard`: Vite + React frontend intended for admin-facing UI work. It currently lives in the repo but is not wired into the .NET solution or backend runtime.
- `SportsStore.Tests`: xUnit test project with controller-level tests around checkout behavior.

## System Architecture

The system is currently centered on a single backend service, `SportsStore`, with two supporting infrastructure services in Docker:

- SQL Server for product, order, identity, and workflow-related tables.
- RabbitMQ for simple order event delivery.

At application startup, `SportsStore`:

- Loads configuration from `appsettings*.json`, environment variables, and user secrets in development.
- Configures EF Core contexts for the store database and identity database.
- Configures ASP.NET Identity for admin login flows.
- Registers a hosted RabbitMQ consumer and a RabbitMQ publisher service.
- Registers Stripe payment integration.
- Seeds product and identity data on startup.

The backend exposes three kinds of UI/API surfaces:

- MVC storefront and checkout pages.
- Razor Pages and Blazor endpoints.
- JSON APIs such as `/api/products` and `/api/orders`.

## Service Responsibilities

### SportsStore

Primary responsibilities:

- Serve catalog and storefront pages.
- Persist products and paid orders in SQL Server.
- Handle Stripe checkout session creation and payment verification.
- Publish order messages to RabbitMQ.
- Consume order messages and update the in-memory order status view used by `/api/orders`.
- Provide admin authentication with ASP.NET Identity.

Important implementation notes:

- Paid MVC checkout orders are stored in SQL Server through `IOrderRepository`.
- API-created orders use `OrderMemoryStore`, which is an in-memory store and not persisted to SQL Server.
- `RabbitMQConsumer` runs as a hosted background service and updates in-memory order status after messages are consumed.

### CustomerPortal

Primary responsibilities:

- Provide a separate customer-facing UI built with Razor Components.
- Call the backend API through an `HttpClient` configured with base address `http://localhost:5000/`.

### AdminDashboard

Primary responsibilities:

- Intended admin-facing frontend built with React + Vite.
- Not currently hosted by `SportsStore`.
- Not included in the Visual Studio solution as a buildable project.
- Not yet connected to authenticated backend admin workflows in this repo.

## Event Flow

There are currently two order flows in the codebase.

### 1. MVC checkout with Stripe

1. A user adds products to the session-backed cart in `SportsStore`.
2. `POST /Order/Checkout` validates the order and stores a pending order payload in session.
3. `StripePaymentService` creates a Stripe Checkout session and redirects the user to Stripe.
4. Stripe redirects back to `/Order/PaymentSuccess?session_id=...`.
5. `SportsStore` verifies the Stripe session.
6. On successful payment, the order is saved to SQL Server.
7. A message is published to RabbitMQ queue `orderQueue`.
8. `RabbitMQConsumer` reads the message and updates the in-memory order status view.
9. The user is redirected to the completion page.

### 2. API-based order creation

1. A client posts JSON to `POST /api/orders`.
2. The backend stores the order in `OrderMemoryStore`.
3. The backend publishes the order payload to RabbitMQ.
4. `RabbitMQConsumer` consumes the message and updates the in-memory order status to `Processed`.
5. Clients can query `GET /api/orders` or `GET /api/orders/{id}`.

## Storage Boundaries

- SQL Server stores catalog data, identity data, paid checkout orders, and related workflow entities.
- Session state stores the temporary pending checkout order before Stripe redirects back.
- `OrderMemoryStore` stores API-created orders and status snapshots only in process memory.
- RabbitMQ carries order messages between the publisher and consumer inside the same backend service boundary.

## How To Run

## Prerequisites

- .NET 8 SDK
- Docker Desktop
- Node.js if you want to run `AdminDashboard`
- A valid Stripe secret key and publishable key

## Option 1: Run `SportsStore` with Docker Compose

From `C:\code\SportsStore`:

```powershell
docker compose build --no-cache
docker compose up
```

This starts:

- `sportsstore` on `http://localhost:5000`
- `sqlserver`
- `rabbitmq` on `http://localhost:15672` and AMQP `5672`

Configuration comes from:

- `SportsStore\.env` for `STRIPE_SECRET_KEY` and `STRIPE_PUBLISHABLE_KEY`
- `SportsStore\docker-compose.yml` for connection strings and RabbitMQ hostname

## Option 2: Run `SportsStore` locally from Visual Studio or CLI

Required local dependencies:

- A reachable SQL Server instance
- A reachable RabbitMQ instance
- Stripe credentials available through user secrets or environment variables

Key settings expected by the app:

- `ConnectionStrings:SportsStoreConnection`
- `ConnectionStrings:IdentityConnection`
- `RabbitMQ:HostName`
- `Stripe:SecretKey`
- `Stripe:PublishableKey`

Run with:

```powershell
dotnet run --project C:\code\SportsStore\SportsStore.csproj
```

## Run `CustomerPortal`

`CustomerPortal` assumes the backend is already available at `http://localhost:5000/`.

Run with:

```powershell
dotnet run --project C:\code\CustomerPortal\CustomerPortal.csproj
```

## Run `AdminDashboard`

From `C:\code\AdminDashboard`:

```powershell
npm install
npm run dev
```

This project is currently standalone and must be started separately.

## Operational Assumptions

- `SportsStore` is the source of truth for catalog and paid order processing.
- Stripe is required for the MVC checkout flow to complete successfully.
- RabbitMQ may be temporarily unavailable during startup; the backend is configured to ignore hosted service failures rather than terminate the whole app.
- `CustomerPortal` and `AdminDashboard` are expected to call the backend over HTTP rather than share code directly.
- Docker is the easiest supported way to run the backend with the expected infrastructure.

## Current Limitations

- `OrderMemoryStore` is process-local and non-persistent. API-created orders disappear on restart.
- The API order flow and the MVC + Stripe checkout flow do not persist to the same storage path.
- `AdminDashboard` is present in the repo but not integrated into the .NET solution, auth flow, or deployment path.
- `CustomerPortal` uses a hard-coded backend base URL of `http://localhost:5000/`.
- Identity seeding currently creates a default admin user in startup code and should be treated as development/demo setup.
- Data protection keys inside the Docker container are not persisted by compose yet.
- RabbitMQ is used within the same service boundary; there is no separate fulfillment or inventory worker in this repo yet.
- There is limited automated test coverage beyond controller-level checkout tests.

## Default Credentials And Endpoints

- Backend: `http://localhost:5000`
- RabbitMQ management: `http://localhost:15672`
- Seeded admin username: `Admin`
- Seeded admin password: `SomePassword123!`

Review the seed and configuration code before using this setup outside local development.
