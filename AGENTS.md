# AGENTS.md

This file provides guidance to AI coding agents working with code in this solution.

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

## Layer rules

Rules for each layer. Skills live in `.agents/skills/<name>/SKILL.md`; use the skill named for a layer when adding, changing, or deleting code there.

### Core / Domain layer (`Core/`)

Skills: `actions-with-domain-entities` (`AR.Core.Domain`), `actions-with-app-services` (`AR.Core.AppServices`), `actions-with-persistence` (`AR.Core.Persistence`).

Classes are placed by postfix; each postfix maps to one project and folder. Group files by business area subfolder (e.g. `Users/`, `Account/`). Most are auto-registered (scoped) by their postfix via `AddServicesFromAssemblyExt()` — follow the convention and no manual DI is needed.

| Postfix | Project | Purpose |
| --- | --- | --- |
| `AppService` | `AR.Core.AppServices` | Use-case orchestration; runs business logic inside `IUnitOfWork.BeginTransactionAsync()` and emits domain events. |
| `DomainService` | `AR.Core.AppServices` | Reusable domain logic shared across app services. |
| `Entity` | `AR.Core.Domain` | Domain entities (inherit `Entity` / `AuditableEntity`). |
| `EntityMap` | `AR.Core.Domain` | EF `IEntityTypeConfiguration` mapping (inherit `EntityMap<T>` / `AuditableEntityMap<T>`). |
| `Event` | `AR.Core.Events` | Domain events (inherit `BaseDomainEvent`). |
| `EventHandler` | `AR.Core.EventHandlers` | Pre/post-commit handlers (`IEventHandler`), matched to events by `Type`. |
| `EventQueueConsumer` | `AR.Core.EventQueueConsumers` | Async consumers (`IQueueConsumer`) draining the channel queue, matched by `Type`. |
| `Repository` | `AR.Core.Persistence/Repositories` | Concrete repos wrapping generic `Repository<T>`; `GetAsync(id)` returns `MayBe<T>`. |
| `QueryHandler` | `AR.Core.Persistence/Handlers/Queries` | Dapper read handlers (inherit `BaseHandler`). |
| `CommandHandler` | `AR.Core.Persistence/Handlers/Commands` | Dapper write handlers (inherit `BaseHandler`). |
| `DIModule` | `AR.Core.Configurations` | Static `Load(IServiceCollection, ...)` composition modules. |

- Postfix-based DI registration is wired in `AR.Core.Configurations` (`AppServicesDIModule`, `RepositoriesDIModule`, `EventHandlersDIModule`). Services that can't follow the convention go in that module's `LoadCustomDiServices`.
- Keep the layer free of API/web and infrastructure concerns — depend only on `AR.Shared` and other Core projects.

### Infrastructure layer (`Infrastructure/`)

Technical capabilities behind interfaces, one capability per project (`Queues`, `Hosting`, `Jwt`, `Json`, `Providers`, `BackgroundServices`, `DI`). Each component is `internal sealed` with a `public` interface; interface and class live in the same file.

Unlike the Core layer, Infrastructure is **not** postfix auto-registered. Each project owns an `internal static *DIModule.Load(IServiceCollection)` in its `DI` folder with explicit registrations, and `InfrastructureDIConfiguration.Load` (in `AR.Infrastructure.DI`) orchestrates them.

| Postfix | Project | Purpose |
| --- | --- | --- |
| `Provider` | `AR.Infrastructure.Providers` / `Jwt` / `Json` | Stateless technical services behind an `IFooProvider` interface (e.g. `ExecutionContextProvider`, `JwtTokenProvider`, `JsonProvider`). |
| `Queue` | `AR.Infrastructure.Queues` | Event queues — singleton `ChannelQueue` (`IChannelQueue`) and scoped `ScopeQueue` (`IScopeQueue`). |
| `BackgroundService` | `AR.Infrastructure.BackgroundServices` | Hosted services draining queues (e.g. `ChannelQueueBackgroundService`). |
| `DtoModel` | `<project>/DTO` | Data-transfer models for a capability (e.g. `JwtTokenDtoModel`). |
| `DIModule` | `<project>/DI` | `internal static Load(IServiceCollection)` registration module, one per project. |

- `AREnvironment` (`AR.Infrastructure.Hosting`) is the static environment gate (`IsLocal()` / `IsQa()` / `IsProduction()`) used throughout startup — keep environment branching there, not scattered.
- Register components in their project's `*DIModule`; add the module to `InfrastructureDIConfiguration.Load` so it is composed. Match each registration's lifetime to the contract (singleton queue, scoped providers).
- Depend only on `AR.Shared` and Core abstractions (e.g. `BaseDomainEvent`); never reference the API layer.

### API Service layer (`API/AR.ApiServices/`)

Skill: `actions-with-api-services`.

- Each Service should be internal sealed with a public interface. Interface and class live in the same file.
- The layer is a thin adapter — no business logic. Just a simple mapping between the request and the response. Or validation request. Or multiple requests to `AR.AppServices` with combining results to response.
- Each API Service should has `ApiService` postfix. DI isn’t needed, because it resolved automatically inside `ApiServicesDIModule.cs` file.
- All request Models should be located in the `Models` folder under each business subfolder. Each model should be inherited from `BaseValidationModel`. Validators should be located in the `Validators` folder under each business subfolder.

### API layer for Web API (`API/AR.API/`)

Skill: `actions-with-controller`.

- Each controller should inherit from `BaseApiV1Controller` or default `ApiController`.
- Controllers should be located in the `Controllers` folder.
- Controllers must be [Route("v{version:apiVersion}/...")] and carry a [Tags("...")] group. One tag per controller. Action methods should be tagged with the http verb name.
- Controllers stay thin: delegate to an injected `ApiService` from `AR.ApiServices`; no business logic, no direct repository/DbContext access.
- All Infrastructure (Middlewares, Auth, Exceptions, Attributes) should be located in the `Infrastructure` folder.
- All application settings should be located in the `appsettings.json` use environment-specific variants.
- All request Models should be used from `AR.ApiServices` project. See the **API Service layer** rules above.

#### Cross-cutting / what NOT to do

- Don't write try/catch in controllers — let GlobalExceptionHandler map exceptions to ProblemDetails.
- Don't validate manually — rely on [RequestModelFluentValidator] (inherited) + the request model's IValidator<T>.
- Get the caller's identity from UserId/UserName on the base controller, not from HttpContext.User directly.

## Behavioral guidelines


Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume it. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them – don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines, and it could be 50, rewrite it.

Ask yourself: "Would a senior .NET engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it – don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should be traced directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multistep tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

## Code style (build-breaking)

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, and `EnforceCodeStyleInBuild=true`, so **the build fails on any analyzer or code-style violation**. Rules promoted to errors via `.editorconfig`:

- `IDE0005` — unused `using` directives are errors; remove them.
- File-scoped namespaces (`namespace X;`), `using` directives **outside** the namespace.
- `var` required when the type is clear; expression-bodied members required.
- Private fields must be `_underscorePrefixed`.
- Async methods returning `Task`/`ValueTask` must end with `Async`.

Target `net10.0`, nullable + implicit usings enabled, XML doc generation on (`CS1591` suppressed).

# Code Development Rules

- each class should follow the solution naming convention.
- every `internal` class is declared `internal sealed` (exceptions: `static`, `abstract`, or a class used as a base class).
- each class (or object) is located in the right folder (or layer) and be in the right namespace.
- each class should have a single responsibility.
- each class should have a clear and concise name.
- each class should have a clear and concise purpose.
