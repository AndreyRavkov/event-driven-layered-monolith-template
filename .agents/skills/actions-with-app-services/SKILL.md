---
name: actions-with-app-services
description: Add, change, or delete an application service in the AR.Core.AppServices project — orchestration of domain operations inside an IUnitOfWork transaction, emission of domain events, and convention-based DI. Use when the user asks to add a use case / business action (create / update / soft-delete / activate / "do X" on an aggregate), wire an AppService into a flow, change transactional / event behaviour, or remove an app service. Other layer skills (controller, ApiService, persistence, events, event handlers) invoke this skill when they need an app service to exist.
---

# Add / Change / Delete an Application Service (AR.Core.AppServices)

Guidance for shaping the application-service layer the way this solution does it.
Keep changes surgical and match surrounding style — see `AGENTS.md`.

This skill owns **only the application-service project** (`Core/AR.Core.AppServices`):
the `*AppService` types that orchestrate one use case, the `Try.CommitAsync`
transactional wrapper, the `IEventDispatcher` plumbing, and any
`*DomainService` helpers. It does **not** define HTTP shape, DTOs, validators,
entities, EF mappings, repositories, Dapper handlers, event types, or event
handlers. When the work needs those, delegate:

| Need | Skill / layer |
| --- | --- |
| HTTP endpoint, request DTO, validator, `*ApiService` adapter | `actions-with-controller` (`AR.API` / `AR.ApiServices`) |
| Entity type / EF mapping | `actions-with-domain-entities` (`AR.Core.Domain`) |
| Repository, Dapper query/command handler, UoW, interceptors | `actions-with-persistence` (`AR.Core.Persistence`) |
| Domain **event type** | events skill (`AR.Core.Events`) |
| Pre/post-commit handler, queue consumer | event-handler / consumer skill |

If a request crosses layers, edit only this layer here and ask the calling
skill to handle the rest. Do not touch controllers, ApiServices, entities,
mappings, repositories, or event handlers from this skill.

## Architectural shape — read this before editing

- **An app service represents one use case.** It is the natural place where
  transactions begin, business invariants are checked, repositories are
  composed, and domain events are emitted. It is **not** a CRUD facade over a
  repository — keep the interface intention-revealing (`PlaceOrderAsync`,
  `DeactivateUserAsync`), not data-shaped (`UpdateAsync(entity)`).
- **Layering from above and below.** The pipeline is
  `Controller → *ApiService → *AppService → IRepository / *QueryHandler / *CommandHandler`.
  App services are the **only** place that opens a `UnitOfWork` transaction
  and the **only** place that decides whether a domain event is raised.
  Controllers and `*ApiService` adapters never do either.
- **Unit of Work + event pipeline.** The canonical write path goes through
  `Try.CommitAsync(this IUnitOfWork, ..., IEventDispatcher?)` (see
  `Infrastructure/Try.cs`). It opens a transaction at
  `IsolationLevel.ReadUncommitted`, runs the action, harvests events from the
  change tracker **and** `IScopeQueue`, runs **pre-commit** handlers in the
  same transaction, `SaveChangesAsync`, `CommitAsync`, then **post-commit**
  handlers (which typically enqueue onto `IChannelQueue`). On exception the
  transaction is rolled back and the throw is re-raised.
- **Two ways to raise a domain event from an app service.** Pick one per
  event and stay consistent within the feature:
  1. Mutate a tracked entity and append to its `DomainEvents` list — used when
     the event is intrinsic to a state change on the aggregate.
  2. Call `IUnitOfWork.AddEventAsync(new FooEvent { ... })` — used when the
     event is not tied to a specific tracked entity (e.g. `UserLoginEvent`).
  Both sources are picked up by `IUnitOfWork.GetDomainEvents()`; do not invent
  a third channel.
- **`IEventDispatcher` is internal to this project.** It is resolved alongside
  the app service and passed into `Try.CommitAsync`. If the use case raises no
  events you may omit it, but injecting it costs nothing and matches the
  existing `AccountAppService` style — prefer including it when in doubt.
- **DI is convention-based.** `AppServicesDIModule.Load` scans this assembly
  via `AddServicesFromAssemblyExt(...)` and binds anything ending in
  `AppService` **or** `DomainService` to its matching `I<Name>` interface as
  **scoped**. The only manual registration is
  `IEventDispatcher → EventDispatcher`. If you follow the naming convention,
  **you do not edit `AppServicesDIModule`**.
- **Visibility.** App-service implementations and the `Try` helper are
  `internal`; only the `public` interfaces leak out. The `InternalsVisibleTo`
  attribute exposes them to `AR.Core.Configurations` for DI. Do not raise
  visibility just to make a test or a sibling layer compile — ask first.
- **Domain services (`*DomainService`).** Reach for one only when a piece of
  business logic is genuinely shared across multiple app services and has no
  natural home on an entity. They follow the same convention (`public`
  interface, `internal sealed` impl, `[UsedImplicitly]`, scoped via the
  `DomainService` postfix). Default to keeping logic in the app service.

## Where each piece lives

| Piece | Project / folder |
| --- | --- |
| Application service | `Core/AR.Core.AppServices/<Area>/<Name>AppService.cs` |
| Domain service (rare) | `Core/AR.Core.AppServices/<Area>/<Name>DomainService.cs` |
| Transactional wrapper | `Core/AR.Core.AppServices/Infrastructure/Try.cs` |
| Event dispatcher | `Core/AR.Core.AppServices/Infrastructure/EventDispatcher.cs` |

`<Area>` mirrors the domain area the use case lives in (e.g. `Users`,
`Account`). Reuse the existing folder; create a new one only when the area is
new. If you are unsure how `<Area>` should be named, ask.

---

## 1. Adding a write/command app service (the common case)

Use this shape whenever the operation **mutates state**: creating an
aggregate, changing fields, soft-deleting, raising a domain event, etc. The
transactional wrapper, event dispatch, and audit interceptor are wired for
you — your job is the business logic in between.

1. **Prerequisites.** Confirm the supporting layers exist; if any is missing,
   stop and hand off:
   - Entity + EF map — `actions-with-domain-entities`.
   - Feature repository (or Dapper handler) with the operations you need —
     `actions-with-persistence`. **Do not** broaden a repository surface from
     this skill.
   - Domain event type, if you intend to raise one — events skill.
   - Pre/post-commit handler for that event, if dispatch must succeed —
     event-handler skill. (An event raised with no handler will throw
     `NotSupportedException` from `EventDispatcher`.)

2. **Interface + class** in one file, area folder named after the aggregate:

   ```csharp
   using AR.Core.AppServices.Infrastructure;
   using AR.Core.Events.Widgets;
   using AR.Core.Persistence.Context;
   using AR.Core.Persistence.Repositories.Widgets;
   using AR.NetExt.Models.Exceptions;
   using JetBrains.Annotations;

   namespace AR.Core.AppServices.Widgets;

   public interface IWidgetAppService
   {
       Task<int> CreateAsync(string name);
       Task DeactivateAsync(int id);
   }

   [UsedImplicitly]
   internal sealed class WidgetAppService : IWidgetAppService
   {
       private readonly IEventDispatcher _eventDispatcher;
       private readonly IUnitOfWork _unitOfWork;
       private readonly IWidgetRepository _widgetRepository;

       public WidgetAppService(
           IEventDispatcher eventDispatcher,
           IUnitOfWork unitOfWork,
           IWidgetRepository widgetRepository)
       {
           _eventDispatcher = eventDispatcher;
           _unitOfWork = unitOfWork;
           _widgetRepository = widgetRepository;
       }

       public Task<int> CreateAsync(string name) =>
           _unitOfWork.CommitAsync(async () =>
           {
               var entity = new WidgetEntity { Name = name };
               await _widgetRepository.AddAsync(entity);

               await _unitOfWork.AddEventAsync(new WidgetCreatedEvent { WidgetId = entity.Id });

               return entity.Id;
           }, _eventDispatcher);

       public Task DeactivateAsync(int id) =>
           _unitOfWork.CommitAsync(async () =>
           {
               var widget = (await _widgetRepository.GetAsync(id))
                            .Match(w => w, () => throw new NotFoundExceptionExt($"Widget {id} not found."));

               widget.Active = false;

               await _unitOfWork.AddEventAsync(new WidgetDeactivatedEvent { WidgetId = id });
           }, _eventDispatcher);
   }
   ```

   - Interface is `public`; impl is `internal sealed` and `[UsedImplicitly]`
     (the convention scanner is the only "user").
   - Postfix is **exactly `AppService`** — `AddServicesFromAssemblyExt` keys
     on it, so a typo silently drops the binding.
   - Constructor injection only. Don't reach for `IServiceProvider`.
   - The whole transactional unit goes through **one** `Try.CommitAsync`
     call. Do not call `BeginTransactionAsync` / `SaveChangesAsync` /
     `CommitAsync` directly — the wrapper owns ordering with the event
     pipeline and the audit interceptor.
   - **Soft delete** = `entity.Active = false` on a tracked entity. The
     audit interceptor sets `UpdatedOn` / `UpdatedBy` / `ChangeSource`
     automatically; do not set them by hand. Hard delete (`Remove`) is a last
     resort and a different intent — ask before reaching for it.
   - `MayBe<T>` from `ARNetExt` is the return type of single-entity reads;
     pattern-match with `Match(...)` rather than null-checking.
   - Throw the project's `*ExceptionExt` types (`NotFoundExceptionExt`,
     `UnAuthorizationExceptionExt`, `BadRequestExceptionExt`, `ForbiddenExceptionExt`) for business
     failures. `GlobalExceptionHandler` in the API project maps them to
     `ProblemDetails`. Do not throw `Exception` / `InvalidOperationException`
     for business signals.

3. **Build** (warnings are errors):
   `dotnet build AR.slnx -c Release`.

### Which `Try.CommitAsync` overload to use

| Returning | Overload |
| --- | --- |
| Nothing, sync work | `CommitAsync(Action, IEventDispatcher?)` |
| Nothing, async work | `CommitAsync(Func<Task>, IEventDispatcher?)` |
| `T`, async work | `CommitAsync<T>(Func<Task<T>>, IEventDispatcher?)` |

There is no `Func<ValueTask>` overload — wrap with `.AsTask()` if a
dependency only exposes `ValueTask`. Default to the `Func<Task>` /
`Func<Task<T>>` overloads; the `Action` overload exists for the rare case
where the body is genuinely synchronous.

### Raising events — choose the right channel

- **From an entity's `DomainEvents` list** when the event is the natural
  consequence of a state mutation on that aggregate. Example: an aggregate
  method appends `DomainEvents.Add(new WidgetReassignedEvent(...))` as part
  of its mutation. `IUnitOfWork.GetDomainEvents()` harvests them after the
  action runs.
- **Via `IUnitOfWork.AddEventAsync(...)`** when the event is not tied to a
  tracked entity in this scope (cross-aggregate, identity events, etc.) —
  this is the pattern `AccountAppService.CreateTokenAsync` uses for
  `UserLoginEvent`.

Either way, every event raised inside the transaction **must** have a
matching `IEventHandler.EventType`, or `EventDispatcher` throws
`NotSupportedException`. Coordinate the handler via the event-handler skill
before raising a new event type.

---

## 2. Adding a read-only app service

Reads do not need a transaction or the event dispatcher. Stay minimal:

```csharp
using AR.Core.Persistence.Repositories.Widgets;
using AR.NetExt.Utils.Collections;
using JetBrains.Annotations;

namespace AR.Core.AppServices.Widgets;

public interface IWidgetAppService
{
    Task<IReadOnlyList<(int Id, string Name)>> GetAsync();
}

[UsedImplicitly]
internal sealed class WidgetAppService : IWidgetAppService
{
    private readonly IWidgetRepository _widgetRepository;

    public WidgetAppService(IWidgetRepository widgetRepository) => _widgetRepository = widgetRepository;

    public async Task<IReadOnlyList<(int Id, string Name)>> GetAsync()
    {
        var widgets = await _widgetRepository.GetAsync();
        return widgets.Select(w => (w.Id, w.Name)).AsReadOnlyListExt();
    }
}
```

- No `IUnitOfWork`, no `IEventDispatcher`, no transaction — match
  `UserAppService` exactly.
- Reach for a `*QueryHandler` (Dapper) via the persistence skill when the
  read is hot, projected, or would fight the EF change tracker. From the app
  service's perspective the injection shape is identical — `IFooQueryHandler`
  instead of `IFooRepository`.
- Project to DTOs / tuples at this layer; never leak `IQueryable<T>` or
  `DbSet<T>` out of the app service. Use `AsReadOnlyListExt()` (from
  `AR.NetExt.Utils.Collections`) to materialise collections.

---

## 3. Mixed read + write in one use case

If a use case both reads and writes (the typical case — load aggregate,
mutate, save), wrap the whole thing in `Try.CommitAsync` so the read sees a
consistent snapshot with the write. The default isolation level is
`ReadUncommitted` (set in `UnitOfWork.BeginTransactionAsync`); if a use case
genuinely needs stricter semantics, **do not** change the default — pass a
stricter level at the call site through the persistence skill rather than
silently widening it for everyone, and flag the change to the user.

---

## Changing existing app services

- **Add a method to an existing interface** — extend `IFooAppService`,
  implement on the existing class, reuse the already-injected dependencies.
  If you need a new dependency, add it to the constructor; DI resolves it
  automatically by convention.
- **Change transactional behaviour** — keep it inside `Try.CommitAsync`. Do
  not split a single use case into two transactions to "make events fire
  sooner" — post-commit handlers are the supported asynchrony seam (they
  enqueue onto `IChannelQueue`, which `ChannelQueueBackgroundService` drains
  outside the request).
- **Add a new event raise to an existing flow** — pick the right channel
  (see above), confirm the handler exists, and place the `AddEventAsync` /
  `entity.DomainEvents.Add` call **inside** the `Try.CommitAsync` action so
  it is harvested. Events added outside the transactional action are lost.
- **Rename an app service** — keep the `AppService` postfix or DI will
  silently drop the binding. `Grep` for callers in `AR.ApiServices` first;
  the rename across layers is the calling skill's responsibility, not this
  one's.
- Stay surgical: don't reformat unrelated files, don't broaden the interface
  beyond what the request needs, and don't "improve" the transactional
  shape opportunistically.

## Deleting an app service

Two distinct meanings — clarify which is intended:

- **Drop a use case from the interface** — remove the method here and stop.
  `Grep` `AR.ApiServices` for callers; if there are any, stop and report
  them. Their removal belongs to the controller / ApiService skill.
- **Remove the app service entirely** — delete `<Name>AppService.cs` only
  after `Grep` shows no `I<Name>AppService` references in `AR.ApiServices`
  or any other project. If references exist, stop and report them. Do not
  modify those projects from this skill.

Removing the file also removes its DI binding because registration is
convention-based on assembly scan — no `AppServicesDIModule` edit is needed.

---

## Boundary notes

- **`IEventDispatcher` is internal — keep it inside this project.** The
  persistence skill explicitly does not import it; the API layer does not
  see it. Do not expose it through a `public` interface.
- **No HTTP or DTO concerns here.** App-service signatures take primitives,
  tuples, or domain types — never `BaseValidationModel<T>` request DTOs,
  `IActionResult`, `HttpContext`, etc. The `*ApiService` adapter in the API
  project does the mapping.
- **No EF or Dapper specifics here.** App services do not reference
  `DbContext`, `DbSet<T>`, `Connection`, `Transaction`, or LINQ-to-Entities
  expression trees. Compose `IRepository<T>` wrappers or `*QueryHandler` /
  `*CommandHandler` types; if the shape you need does not exist, ask the
  persistence skill to add it.
- **Audit columns are off-limits.** `CreatedOn` / `UpdatedOn` /
  `CreatedBy` / `UpdatedBy` / `ChangeSource` are set by
  `UpdateAuditableEntityInterceptor` on `SavingChangesAsync`. The app
  service that bypasses EF via a Dapper command takes on the obligation to
  set them in SQL — flag that case to the user.

## Checklist before finishing

- [ ] File placed under `Core/AR.Core.AppServices/<Area>/`, namespace `AR.Core.AppServices.<Area>`.
- [ ] Class is `internal sealed` and `[UsedImplicitly]`; interface is `public`.
- [ ] Naming postfix is exact: `AppService` (or `DomainService` for the rare shared helper). DI scan depends on it.
- [ ] No manual DI registration added — convention scan handles it.
- [ ] Write paths run inside a single `Try.CommitAsync(...)`; no direct `BeginTransactionAsync` / `SaveChangesAsync` / `CommitAsync` calls.
- [ ] `IEventDispatcher` is passed to `Try.CommitAsync` whenever the action raises events; events are added **inside** the action.
- [ ] Single-entity lookups use `MayBe<T>.Match(...)`; no `null` leaks.
- [ ] Business failures throw `*ExceptionExt` (e.g. `NotFoundExceptionExt`, `BadRequestExceptionExt`), not raw `Exception`.
- [ ] Audit columns are not set by hand; soft delete sets `Active = false` on a tracked entity.
- [ ] No `DbContext`, `DbSet<T>`, `IQueryable<T>`, HTTP types, or DTOs from `AR.ApiServices` reach this layer.
- [ ] No persistence / domain / event / handler / controller / ApiService code touched — those went through their own skills.
- [ ] `dotnet build AR.slnx -c Release` succeeds.
