# CLAUDE.md

Shared conventions: @common-llm.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this solution.

## Overview

`AR` is a layered .NET 10 ASP.NET Core Web API solution template (DDD-flavored) using central package management. The solution file is `AR.slnx`; `Directory.Build.props` and `Directory.Packages.props` apply settings and pin package versions across every project.

## Commands

Run from the repo root (where `AR.slnx` lives).

```bash
# Build entire solution — warnings ARE errors (see Code style)
dotnet build AR.slnx -c Release

# Run the API — HTTPS https://localhost:31111, Scalar API docs at /api/doc, health at /ping
dotnet run --project API/AR.API

# Restore
dotnet restore AR.slnx

# Verify code style without building
dotnet format AR.slnx --verify-no-changes
```

There is **no test project** in the solution yet. The `Dockerfile` is present but fully commented out.

## Architecture

Dependency direction is `API → Core ← Infrastructure`, with `Shared` referenced everywhere.

- **`Shared/AR.Shared`** — cross-cutting primitives: option/settings models (`AppSettings`, `DataBaseSettings`, `JwtSettings`, `CorsSettings`), `Constants`, and the reflection-based DI helper `AddServicesFromAssemblyExt()`.
- **`Core/`** — domain and business logic (see the event pipeline below), split across `AR.Core.Domain`, `AppServices`, `Events`, `EventHandlers`, `EventQueueConsumers`, `Persistence`, and `Configurations`.
- **`Infrastructure/`** — technical capabilities behind interfaces: `Queues`, `Hosting`, `Jwt`, `Json`, `Providers`, `BackgroundServices`, and `DI` composition.
- **`API/`** — `AR.ApiServices` (request DTOs, validators, thin `*ApiService` adapters) and `AR.API` (web host, controllers, middleware, OpenAPI).

### DI module convention

DI is composed of static `*DIModule.Load(IServiceCollection, ...)` methods (not a marker interface). `AR.Core.Configurations` and `AR.Infrastructure.DI` orchestrate the submodules. Many registrations are **convention-based via `AddServicesFromAssemblyExt()`**: a class named `FooApiService` / `FooRepository` / `FooQueryHandler` is auto-bound to its `IFoo...` interface (scoped). FluentValidation validators register via `AddValidatorsFromAssemblyContaining<>`. Follow the naming convention and a service wires itself up — no manual registration needed.

### Event pipeline — the key architectural pattern

Domain events flow through **two phases and two queues** (in-process; no external broker):

1. An app service (`AR.Core.AppServices`) runs business logic inside `IUnitOfWork.BeginTransactionAsync()`, emitting events via entities' `DomainEvents` or `IUnitOfWork.AddEventAsync` (scoped `IScopeQueue`).
2. `IEventDispatcher.DispatchPreEventsAsync` runs **pre-commit** `IEventHandler.PreCommitHandlerAsync` (in-transaction, DB-aware, can abort).
3. Changes are saved and the transaction commits.
4. `DispatchPostEventsAsync` runs **post-commit** handlers, which typically enqueue events onto the singleton `IChannelQueue` (`System.Threading.Channels`-backed).
5. `ChannelQueueBackgroundService` (hosted service in `AR.Infrastructure.BackgroundServices`) drains `IChannelQueue` and dispatches each event to the matching `IQueueConsumer` for async processing.

Handlers/consumers are matched to events by `Type` (`IEventHandler.EventType` / `IQueueConsumer.Type` vs `BaseDomainEvent.Type`).

### Persistence (`AR.Core.Persistence`)

- `DbDomainContext : DbContext` applies `IEntityTypeConfiguration` mappings from the Domain assembly; provider is **SQLite or SQL Server** (connection from `DataBaseSettings:MainDb`). `DB.sqlite` at the root is the local dev database.
- Generic `IRepository<T>` / `Repository<T>`; concrete repos (e.g. `UserRepository`) wrap it. `GetAsync(id)` returns `MayBe<T>` (monadic, from the `ARNetExt` packages) rather than null.
- `IUnitOfWork` owns transactions and event collection; `UpdateAuditableEntityInterceptor` autofills audit columns on save using `IExecutionContextProvider`.
- `BaseHandler` exposes `DbConnection`/`DbTransaction` for Dapper-based query/command handlers.

### Domain entities (`AR.Core.Domain`)

`Entity` holds an in-memory `DomainEvents` list; `AuditableEntity` adds `CreatedOn/UpdatedOn/CreatedBy/UpdatedBy/ChangeSource/Active`. EF mappings derive from `EntityMap<T>` / `AuditableEntityMap<T>` (snake_case columns; `DomainEvents` ignored).

### API layer (`AR.API`)

- **Controllers** derive from `BaseApiV1Controller`: `[Authorize]` + `[ApiVersion("1.0")]` by default, URL-segment versioning via `Asp.Versioning` (`/v1/...`, unsupported version → 406). `UserId`/`UserName` come from `IExecutionContextProvider`.
- **Validation**: `[RequestModelFluentValidator]` action filter validates any argument implementing `IBaseValidationModel` (DTOs derive from `BaseValidationModel<T>` in `AR.ApiServices`) against its `IValidator<T>`.
- **Errors**: `GlobalExceptionHandler` (`IExceptionHandler`) maps custom `*ExceptionExt` types and `ValidationException` to `ProblemDetails`, hiding internals in production.
- **Startup wiring** lives in `AR.API/Infrastructure/ConfigurationExtensions` as `Configure*Ext` / `Use*Ext` extension methods chained in `Program.cs` (config, Serilog, option binding, API versions, CORS, JWT auth, OpenAPI/Scalar, JSON options).
- `CorrelationIdMiddleware` reads/sets `X-Correlation-Id` and pushes it into the Serilog `LogContext`.

### Environments (`AR.Infrastructure.Hosting`)

`AREnvironment` is the static gate used throughout startup. Environments: **Development** (`IsLocal()`, default), **qa** (`IsQa()`), **prod** (`IsProduction()`). It reads `AZURE_FUNCTIONS_ENVIRONMENT` then `ASPNETCORE_ENVIRONMENT`. Local enables the developer exception page and Debug logging; production raises the log level to Warning and suppresses error detail.

## Config sections

`appsettings*.json` bind to option models in `AR.Shared`: `AppSettings` (`MaxRequestBodySizeBytes`), `DataBaseSettings` (`MainDb`), `JwtSettings` (`Issuer`, `Secret` HS512, `ExpireMinutes`), `CorsSettings` (`Origins`, `Methods`, semicolon-delimited), plus `Serilog`. User secrets are loaded in Development.