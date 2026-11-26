---
name: actions-with-persistence
description: Add, change, or remove persistence-layer pieces in the AR.Core.Persistence project — repositories (Repository pattern over EF Core), Dapper query/command handlers (CQRS-lite read/write split), the DbDomainContext, the UnitOfWork, and SaveChanges interceptors. Use when the user asks to add a repository, add a Dapper query/command handler, change EF Core wiring, register an interceptor, or alter the unit-of-work / transaction shape. Other layer skills (app service, controller) invoke this skill when they need a repository or handler to exist.
---

# Add / Change / Delete Persistence Code (AR.Core.Persistence)

Guidance for shaping the persistence layer the way this solution does it.
Keep changes surgical and match surrounding style — see `AGENTS.md`.

This skill owns **only the persistence project** (`Core/AR.Core.Persistence`):
the EF `DbDomainContext`, generic `IRepository<T>`, feature-scoped
`*Repository` wrappers, `IUnitOfWork`, EF `SaveChangesInterceptor`s, and the
Dapper-backed `*QueryHandler` / `*CommandHandler` types. It does **not** define
domain types, business logic, events, event handlers, or HTTP shape. When the
work needs those, delegate to their skills:

| Need | Skill / layer |
| --- | --- |
| Entity type / EF mapping | `actions-with-domain-entities` (`AR.Core.Domain`) |
| Business logic, transactions, soft-delete intent | app-service skill (`AR.Core.AppServices`) |
| Domain **event type** | events skill (`AR.Core.Events`) |
| Pre/post-commit handler, queue consumer | event-handler / consumer skill |
| HTTP endpoint, request DTO, validator | `actions-with-controller` (`AR.API` / `AR.ApiServices`) |

If a request crosses layers, edit only this layer here and ask the calling
skill to handle the rest. Do not touch entity classes, app services, or
controllers from this skill.

## Architectural shape — read this before editing

- **Repository pattern** sits in front of EF Core. The generic
  `IRepository<T>` is the workhorse; per-feature `IFooRepository` interfaces
  wrap it to expose a small, intention-revealing API to app services.
- **Unit of Work** (`IUnitOfWork`) owns the transaction lifetime, the
  `SaveChangesAsync` call, and the **domain-event collection** that
  `IEventDispatcher` (in `AR.Core.AppServices.Infrastructure`) drains around
  the commit. Repositories never save or commit on their own — the app
  service does that via `Try.CommitAsync(...)`.
- **CQRS-lite read/write split via Dapper** lives alongside EF: for hot reads
  or set-based writes that don't benefit from the change tracker, write a
  `*QueryHandler` or `*CommandHandler` deriving from `BaseHandler` and use the
  shared `DbConnection` / current `DbTransaction`. EF and Dapper share the
  same connection and ambient transaction.
- **Auditing** is cross-cutting: `UpdateAuditableEntityInterceptor` (an EF
  `SaveChangesInterceptor`) sets `CreatedOn/UpdatedOn/CreatedBy/UpdatedBy/ChangeSource`
  on `AuditableEntity` rows during `SavingChangesAsync`. Never set those by hand.
- **DI is convention-based.** `RepositoriesDIModule.Load` scans the assembly
  via `AddServicesFromAssemblyExt(...)` and binds anything ending in
  `Repository`, `QueryHandler`, or `CommandHandler` to its matching
  `I<Name>` interface as **scoped**. The only manual registrations are
  `IUnitOfWork → UnitOfWork`, the open generic
  `IRepository<> → Repository<>`, and the singleton
  `UpdateAuditableEntityInterceptor`. If you follow the naming convention,
  **you do not edit `RepositoriesDIModule`**.
- **Visibility:** `DbDomainContext`, `Repository<T>`, `UnitOfWork`, and the
  interceptor are `internal`. Only the public interfaces leak out of the
  project. `InternalsVisibleTo("AR.Core.Configurations")` is set so the DI
  module can see them.

## Where each piece lives

| Piece | Project / folder |
| --- | --- |
| EF context | `Core/AR.Core.Persistence/Context/DbDomainContext.cs` |
| Unit of Work | `Core/AR.Core.Persistence/Context/UnitOfWork.cs` |
| DI/options wiring for the context | `Core/AR.Core.Persistence/Context/DbContextConfiguration.cs` |
| Generic repository | `Core/AR.Core.Persistence/Repositories/Repository.cs` |
| Feature repository | `Core/AR.Core.Persistence/Repositories/<Area>/<Name>Repository.cs` |
| `SaveChanges` interceptor | `Core/AR.Core.Persistence/Interceptors/<Name>Interceptor.cs` |
| Dapper base handler | `Core/AR.Core.Persistence/Handlers/BaseHandler.cs` |
| Dapper query | `Core/AR.Core.Persistence/Handlers/Queries/<Name>QueryHandler.cs` |
| Dapper command | `Core/AR.Core.Persistence/Handlers/Commands/<Name>CommandHandler.cs` |

`<Area>` mirrors the domain area the entity lives in (e.g. `Users`). Reuse
the existing folder; create a new one only when the area is new. If you are
unsure how `<Area>` should be named, ask.

---

## 1. Adding a feature repository (the common case)

Use a feature repository when an app service needs a small, intention-revealing
read/write surface for one aggregate. The class wraps `IRepository<T>` and is
auto-registered by naming convention — **no DI edits**.

1. **Prereq:** the entity exists in `AR.Core.Domain` and is mapped. If it
   doesn't, hand off to `actions-with-domain-entities` first.

2. **Interface + class** in one file, same area folder as the entity:

   ```csharp
   using AR.Domain.Widgets;
   using AR.NetExt.MayBe;
   using JetBrains.Annotations;

   namespace AR.Core.Persistence.Repositories.Widgets;

   public interface IWidgetRepository
   {
       Task<IReadOnlyList<WidgetEntity>> GetAsync();
       ValueTask<MayBe<WidgetEntity>> GetAsync(int id);
       Task AddAsync(WidgetEntity entity);
   }

   [UsedImplicitly]
   internal sealed class WidgetRepository : IWidgetRepository
   {
       private readonly IRepository<WidgetEntity> _repository;

       public WidgetRepository(IRepository<WidgetEntity> repository) => _repository = repository;

       public Task<IReadOnlyList<WidgetEntity>> GetAsync() => _repository.GetAsync();
       public ValueTask<MayBe<WidgetEntity>> GetAsync(int id) => _repository.GetAsync(id);
       public Task AddAsync(WidgetEntity entity) => _repository.AddAsync(entity);
   }
   ```

   - Interface is `public`; impl is `internal sealed`, decorated `[UsedImplicitly]`
     (the convention scanner is the only "user").
   - Single-entity lookups return `MayBe<T>` — never `null`. Callers pattern-match.
   - Keep the surface **narrow**: expose only the operations the consuming app
     service actually needs. No `IQueryable` leaks unless an app service genuinely
     needs to compose a projection.
   - **Do not** call `SaveChangesAsync`, `BeginTransactionAsync`, or commit. The
     app service owns the unit of work.

3. **Build** (warnings are errors):
   `dotnet build AR.slnx -c Release`.

### `IRepository<T>` surface — what's available

| Method | Notes |
| --- | --- |
| `ValueTask<MayBe<T>> GetAsync(object id)` | EF `FindAsync` — checks change tracker, then DB. |
| `Task<IReadOnlyList<T>> GetAsync(predicate?, limit, offset)` | Materialised; eager paging via `Skip/Take`. |
| `IQueryable<T> GetBy(predicate?)` / `GetAll()` | Deferred — for composition by the caller. |
| `Task AddAsync(T)` / `Task AddRangeAsync(IReadOnlyList<T>)` | Adds to the change tracker; no save. |
| `void Remove(T)` / `RemoveRange(IReadOnlyList<T>)` | Hard delete on save. **Soft delete is not this method's job** — see below. |
| `void UpdateRange(IReadOnlyList<T>)` | Marks entities `Modified`. **There is no single-entity `Update`** — for a tracked entity, mutate its properties and let the tracker handle it; only call `UpdateRange` for detached entities. |

`IRepository<T>` methods deliberately don't accept a `CancellationToken`. Don't
add one unless the user asks — keep the surface consistent.

### Soft delete vs hard delete

`AuditableEntity` carries an `Active` flag. "Deleting a record" in this
solution almost always means setting `Active = false` in an **app service**
(business decision), not calling `Remove(...)`. This skill exposes both;
choose `Remove*` only when the row must physically leave the table and the
calling skill has confirmed that intent. Repositories **do not** auto-filter
by `Active` — the caller decides.

---

## 2. Adding a Dapper query handler (read path)

Use a `*QueryHandler` when EF's change tracker is unwanted (hot reads,
projections, set-based queries). It shares the EF context's connection and
ambient transaction, so reads inside `Try.CommitAsync` see uncommitted writes
from the same unit of work.

```csharp
using AR.Core.Persistence.Context;
using Dapper;
using JetBrains.Annotations;

namespace AR.Core.Persistence.Handlers.Queries;

public interface IWidgetQueryHandler
{
    Task<string?> GetNameAsync(int id);
}

[UsedImplicitly]
internal sealed class WidgetQueryHandler(DbDomainContext context)
    : BaseHandler(context), IWidgetQueryHandler
{
    public Task<string?> GetNameAsync(int id) =>
        Connection.QueryFirstOrDefaultAsync<string?>(
            "SELECT name FROM widgets WHERE id = @id",
            new { id },
            Transaction);
}
```

- Postfix **`QueryHandler`** triggers convention DI — no manual registration.
- Always pass `Transaction` to Dapper so the call enlists in the current
  unit-of-work transaction when there is one.
- Column names follow the entity map (`snake_case`); the default schema is
  `main` (see `DbDomainContext`). Quote/qualify only if you actually need to.

## 3. Adding a Dapper command handler (write path)

Same shape as a query handler, postfix **`CommandHandler`**. Reach for this
only when an EF-tracked write is a bad fit (bulk update/delete, vendor-
specific SQL). The caller still owns the transaction via `IUnitOfWork`.

```csharp
[UsedImplicitly]
internal sealed class WidgetCommandHandler(DbDomainContext context)
    : BaseHandler(context), IWidgetCommandHandler
{
    public Task DeactivateAllAsync() =>
        Connection.ExecuteAsync(
            "UPDATE widgets SET active = 0 WHERE active = 1",
            transaction: Transaction);
}
```

Bypassing EF means **the audit interceptor does not run** for these writes —
if you need `UpdatedOn` / `UpdatedBy` set, write them in SQL explicitly, or
prefer the EF path.

---

## 4. Changing the `DbContext` / wiring

Touch `Context/DbDomainContext.cs` only for global model concerns (default
schema, applying configurations). Mapping per-entity lives in
`actions-with-domain-entities`. The context auto-discovers mappings via
`ApplyConfigurationsFromAssembly(typeof(Entity).Assembly)` — **do not** add
`DbSet<>` properties; the generic `IRepository<T>` resolves any mapped entity.

`Context/DbContextConfiguration.LoadDbContext` is where the provider
(SQLite/SQL Server) and interceptor list live. If you add a new interceptor:

1. Implement it under `Interceptors/` deriving from the appropriate EF base
   (`SaveChangesInterceptor`, `DbConnectionInterceptor`, etc.).
2. Register the interceptor as a **singleton** in `RepositoriesDIModule.LoadCustomDiServices`
   (mirroring `UpdateAuditableEntityInterceptor`) and attach it in
   `DbContextConfiguration.LoadDbContext` via `.AddInterceptors(...)`.
3. Resolve scoped services inside the interceptor by creating a scope from
   `IServiceProvider` (see `UpdateAuditableEntityInterceptor`) — the interceptor
   itself is a singleton; do not capture scoped services in fields.

## 5. Changing `IUnitOfWork` / transaction shape

The default isolation level on `BeginTransactionAsync` is
**`IsolationLevel.ReadUncommitted`**. That is intentional for this template
but unusual — flag any change to it to the user before editing, since
event-pipeline correctness depends on the read/save/commit ordering in
`Try.CommitAsync`. If you genuinely need a different isolation level for a
specific use case, pass it from the **app service** call site
(`unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted)`) — don't
change the default.

`GetDomainEvents()` harvests events from both tracked entities **and** the
scoped `IScopeQueue` (events added via `IUnitOfWork.AddEventAsync`). If you
add a new event source, route it through one of those — don't bolt a third
collection on.

---

## Changing / deleting existing persistence pieces

- **Add a method to a repository** — extend the interface, implement it on
  top of `IRepository<T>` (or `Connection`/`Transaction` for Dapper). Keep the
  return shape consistent (`MayBe<T>` for single, `IReadOnlyList<T>` for many).
- **Rename a repository / handler** — keep the `Repository` / `QueryHandler` /
  `CommandHandler` postfix or DI will silently drop the binding. `Grep` for
  callers in `AR.Core.AppServices` first; the rename across layers is the
  calling skill's responsibility, not this one's.
- **Remove a repository / handler** — only if no app service references it.
  `Grep` for the interface name. If references exist, stop and report them.
- Stay surgical: don't "improve" adjacent code, don't reformat unrelated
  files, don't broaden a repository's surface beyond what the request needs.

---

## Domain events (boundary note)

`IUnitOfWork.GetDomainEvents()` and `AddEventAsync(...)` are the contract this
project exposes to the app-service layer; the actual **dispatch** is in
`AR.Core.AppServices.Infrastructure.EventDispatcher` and the
`Try.CommitAsync` helper. **Do not** call `IEventDispatcher` from this
project, do not add event-handling code here, and do not import
`AR.Core.EventHandlers`. Defining new event types belongs in the events skill.

## Checklist before finishing

- [ ] Files placed under the correct folder (`Repositories/<Area>/`, `Handlers/Queries/`, `Handlers/Commands/`, `Interceptors/`, or `Context/`).
- [ ] Class is `internal sealed` and `[UsedImplicitly]`; interface is `public`.
- [ ] Naming postfix is exact: `Repository`, `QueryHandler`, or `CommandHandler` (DI scan depends on it).
- [ ] No manual DI registration added unless a new interceptor or non-conventional service was introduced.
- [ ] Single-entity lookups return `MayBe<T>`; no `null` leaks.
- [ ] No `SaveChangesAsync` / `BeginTransactionAsync` / `CommitAsync` calls inside repositories or handlers — the unit of work belongs to the app service.
- [ ] Dapper calls pass the current `Transaction`; no new `DbConnection` is opened.
- [ ] Audit columns (`CreatedOn/UpdatedOn/CreatedBy/UpdatedBy/ChangeSource`) are not set by hand on EF writes; Dapper writes that bypass the interceptor set them explicitly only when needed.
- [ ] No domain / app-service / event / handler / controller code touched.
- [ ] `dotnet build AR.slnx -c Release` succeeds.
