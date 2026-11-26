---
paths:
  - "solution/AR/Core/**/*.cs"
---

## Rule for **Domain / Core Layer**

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
