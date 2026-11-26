# Event-Driven Layered Monolith Template

A production-minded .NET 10 solution template for building a **layered monolith with event-driven internals**. It gives you a clean starting point with strict separation of concerns, while keeping the option to split modules into services later.

## Architecture

The solution is organized into layers with one-directional dependencies:

| Layer | Projects | Responsibility |
|---|---|---|
| **API** | `AR.API`, `AR.ApiServices` | HTTP entry point, versioned controllers, request validation, API services |
| **Core** | `Domain`, `AppServices`, `Persistence`, `Events`, `EventHandlers`, `EventQueueConsumers`, `Configurations` | Business logic, domain model, persistence abstractions, domain events and their handlers |
| **Infrastructure** | `DI`, `Hosting`, `Queues`, `BackgroundServices`, `Providers`, `Jwt`, `Json` | Technical concerns: composition root, event queues, background processing, auth, serialization |
| **Shared** | `AR.Shared` | Cross-cutting helpers shared between layers |

Business operations publish **domain events** to internal queues; background consumers process them through dedicated handlers. This decouples use cases, keeps request latency low, and makes side effects explicit and testable.

## Tech Stack

- .NET 10 / ASP.NET Core
- Entity Framework Core and Dapper (SQL Server, SQLite for local development)
- JWT bearer authentication
- API versioning (`Asp.Versioning`) with Scalar API documentation
- FluentValidation
- Serilog structured logging
- Central package management (`Directory.Packages.props`) and shared build settings (`Directory.Build.props`)
- Docker support

## Getting Started

```bash
dotnet restore AR.slnx
dotnet build AR.slnx
dotnet run --project API/AR.API
```

## When to Use It

Use this template when you want the simplicity of a single deployable unit, but with the boundaries and messaging patterns that make future extraction into microservices straightforward.

## License

See [LICENSE](LICENSE).
