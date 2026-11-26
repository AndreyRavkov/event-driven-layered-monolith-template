---
paths:
  - "solution/AR/Infrastructure/**/*.cs"
---

## Rule for **Infrastructure Layer**

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
