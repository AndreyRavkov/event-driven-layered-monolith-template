---
name: actions-with-api-services
description: Add, change, or delete an API service in the AR.ApiServices project — the thin HTTP-facing adapter that maps request DTOs (BaseValidationModel<T> + FluentValidation) onto the corresponding AppService, with convention-based DI. Use when the user asks to add / change / remove a *ApiService, add a request DTO + validator, expose a new AppService method to the API, or reshape the contract a controller talks to. Other layer skills (controller, app service) invoke this skill when they need an ApiService or request DTO to exist.
---

# Add / Change / Delete an API Service (AR.ApiServices)

Guidance for shaping the API-service layer the way this solution does it.
Keep changes surgical and match surrounding style — see `AGENTS.md`.

This skill owns **only the API-service project** (`API/AR.ApiServices`):

- `*ApiService` adapters (`I<Name>ApiService` + `internal <Name>ApiService`).
- Request DTOs that derive from `BaseValidationModel<T>`.
- The `AbstractValidator<T>` for each DTO.
- The local `ApiServicesDIModule` wiring (rarely touched — it is convention-based).

It does **not** own controllers, business logic, transactions, events,
entities, EF mappings, repositories, or Dapper handlers. When the work needs
those, delegate:

| Need | Skill / layer |
| --- | --- |
| HTTP endpoint, route, action filter, MVC plumbing | `actions-with-controller` (`AR.API`) |
| Use-case / business logic / transaction / event emission | `actions-with-app-services` (`AR.Core.AppServices`) |
| Entity type / EF mapping | `actions-with-domain-entities` (`AR.Core.Domain`) |
| Repository, Dapper query/command handler, UoW | `actions-with-persistence` (`AR.Core.Persistence`) |

If a request crosses layers, change only this layer here and ask the calling
skill to handle the rest. Do not touch controllers, app services, entities, or
persistence from this skill.

---

## Architectural shape — read this before editing

- **An ApiService is an Adapter (GoF), not a use case.** It exists to translate
  the **HTTP-shaped contract** (request DTOs, primitives, tuples) into a call
  on the matching `I<Name>AppService`. The body is typically a one-liner
  delegation. Look at `UserApiService` / `AccountApiService` — that is the
  ceiling of complexity allowed here.
- **No business logic.** No `if`-branches on domain state, no transactions,
  no event raising, no `MayBe<T>.Match(...)`, no `*ExceptionExt` throws. If
  you are tempted to add any of those, the logic belongs in the AppService —
  stop and hand off to `actions-with-app-services`.
- **No infrastructure dependencies.** Do not inject `DbContext`,
  `IRepository<T>`, `IUnitOfWork`, `IEventDispatcher`, `HttpContext`,
  `IHttpContextAccessor`, `ILogger<T>` (logging belongs in the controller or
  the app service — pick one, but not here), `JsonProvider`, or anything from
  `AR.Core.Persistence` / `AR.Core.Events` / `Microsoft.AspNetCore.*`. The
  only legitimate dependency is `I<Name>AppService` (and occasionally a
  second app service when the use case genuinely spans two — flag that case).
- **Layering from above and below.** The pipeline is
  `Controller → *ApiService → *AppService`. Controllers never call
  `AppService` directly, and `AppService` never sees a DTO — both rules are
  enforced by passing through this layer.
- **`csproj` references `AR.Core.AppServices` with `ExcludeAssets="compile"`.**
  That is intentional: the assembly is reachable at runtime (so DI can wire
  it), but at compile time the API-services project is meant to feel like an
  isolated contract layer. Treat it as the boundary it pretends to be —
  don't widen the project's references chasing a shortcut.
- **DI is convention-based.** `ApiServicesDIModule.Load` scans this assembly
  via `AddServicesFromAssemblyExt(typeof(ApiServicesDIModule).Assembly, "ApiService")`
  and binds anything **whose class name ends in `ApiService`** to the matching
  interface (`I<Name>ApiService`) as **scoped**. Validators register via
  `AddValidatorsFromAssemblyContaining<ApiServicesDIModule>()`. If you follow
  the naming convention, **you do not edit `ApiServicesDIModule`**.
- **Visibility.** ApiService implementations are `internal`; only the `public`
  interfaces leak out. Request DTOs and validators are `public` (controllers
  in `AR.API` consume them). Do not raise an impl's visibility — ask first.
- **Validation is automatic.** Any action argument whose type implements
  `IBaseValidationModel` (i.e. derives from `BaseValidationModel<T>`) is
  validated by the global `RequestModelFluentValidatorAttribute` filter using
  the matching `IValidator<T>`. No attribute on the controller, no manual
  `.Validate()` call. If you forget the base class, validation **silently
  does not run** — there is no compile-time check. Always derive.

---

## Where each piece lives

| Piece | Project / folder |
| --- | --- |
| ApiService interface + impl | `API/AR.ApiServices/<Area>/<Name>ApiService.cs` |
| Request DTO | `API/AR.ApiServices/<Area>/Models/<Name>Request.cs` |
| Request validator | `API/AR.ApiServices/<Area>/Models/Validators/<Name>RequestValidator.cs` |
| DI module (rarely touched) | `API/AR.ApiServices/ApiServicesDIModule.cs` |
| Validation base class | `API/AR.ApiServices/BaseValidationModel.cs` |

`<Area>` mirrors the feature area used in `AR.Core.AppServices` and
`AR.API/Controllers` (e.g. `Users`, `Account`). Reuse the existing folder;
create a new one only when the area is genuinely new. If unsure how `<Area>`
should be named, ask.

---

## 1. Adding a new ApiService (the common case)

Use this shape whenever the controller needs a new entry point and the
matching `*AppService` already exposes (or will expose) the operation.

1. **Prerequisites.** Confirm the supporting layers exist; if any is missing,
   stop and hand off:
   - `I<Name>AppService` with the method you want to call —
     `actions-with-app-services`. **Do not** invent business logic here to
     compensate for a missing app-service method.
   - Entity / repository / handlers it depends on — their respective skills.

2. **Interface + class** in one file, area folder named after the aggregate:

   ```csharp
   using AR.Core.AppServices.Widgets;
   using JetBrains.Annotations;

   namespace AR.ApiServices.Widgets;

   public interface IWidgetApiService
   {
       Task<int> CreateAsync(string name);
       Task<IReadOnlyList<(int Id, string Name)>> GetAsync();
       Task DeactivateAsync(int id);
   }

   [UsedImplicitly]
   internal class WidgetApiService : IWidgetApiService
   {
       private readonly IWidgetAppService _widgetAppService;

       public WidgetApiService(IWidgetAppService widgetAppService) => _widgetAppService = widgetAppService;

       public Task<int> CreateAsync(string name) => _widgetAppService.CreateAsync(name);

       public Task<IReadOnlyList<(int Id, string Name)>> GetAsync() => _widgetAppService.GetAsync();

       public Task DeactivateAsync(int id) => _widgetAppService.DeactivateAsync(id);
   }
   ```

   - Interface is `public`; impl is `internal` and `[UsedImplicitly]` (the
     convention scanner is the only "user"; the attribute also keeps Rider /
     ReSharper from flagging the class as unused).
   - Postfix is **exactly `ApiService`** — `AddServicesFromAssemblyExt` keys
     on it, so a typo silently drops the binding.
   - Constructor injection only. Use the single-line `=>` initialiser when
     there is one dependency (matches `UserApiService` / `AccountApiService`);
     for two or more, use a normal constructor body assigning each field.
   - Method bodies are **delegations**, not orchestrations. Prefer expression
     bodies (`=> _appService.X(...)`); do not introduce locals, branches,
     `await`, or `try/catch` here. Pass `Task<T>` through unchanged — do not
     `await` only to re-wrap.
   - Signatures take **primitives, tuples, request DTOs, or `IReadOnlyList<>`**
     of those. They return primitives, tuples, `IReadOnlyList<>`, or `Task<...>`
     of the same. No `IActionResult`, no `ProblemDetails`, no `HttpContext`,
     no `IQueryable<T>`, no entities, no `DbSet<T>`, no `MayBe<T>`.

3. **Build** (warnings are errors):
   `dotnet build AR.slnx -c Release`.

---

## 2. Adding a request DTO

Whenever a controller posts a body (or accepts a complex query payload), it
takes a request DTO from this project. Place the DTO under
`<Area>/Models/`, and its validator under `<Area>/Models/Validators/`.

```csharp
// API/AR.ApiServices/Widgets/Models/CreateWidgetRequest.cs
namespace AR.ApiServices.Widgets.Models;

public class CreateWidgetRequest : BaseValidationModel<CreateWidgetRequest>
{
    public required string Name { get; init; }
    public int? ParentId { get; init; }
}
```

```csharp
// API/AR.ApiServices/Widgets/Models/Validators/CreateWidgetRequestValidator.cs
using FluentValidation;
using JetBrains.Annotations;

namespace AR.ApiServices.Widgets.Models.Validators;

[UsedImplicitly]
public class CreateWidgetRequestValidator : AbstractValidator<CreateWidgetRequest>
{
    public CreateWidgetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.ParentId).GreaterThan(0).When(x => x.ParentId.HasValue);
    }
}
```

Rules:

- DTOs are **`public class`** (controllers consume them) and **must** derive
  from `BaseValidationModel<T>` where `T` is the DTO itself. Without that
  base class, `RequestModelFluentValidatorAttribute` skips the argument and
  validation silently does nothing.
- Use **`required`** + **`init`** for input properties. Both pieces matter:
  `required` makes JSON deserialisation fail fast on a missing field;
  `init` keeps the DTO immutable after binding. Use `?` (nullable) only when
  the field is genuinely optional, and reflect that in the validator.
- DTOs are **dumb data**. No methods, no computed properties, no validation
  attributes (`[Required]`, `[StringLength]`, …) — FluentValidation owns all
  rules. Do not implement `IValidatableObject`.
- Validators are **`public class`**, derive from `AbstractValidator<T>`,
  carry `[UsedImplicitly]`, and live next to the DTO under
  `Models/Validators/`. They are picked up automatically by
  `AddValidatorsFromAssemblyContaining<ApiServicesDIModule>()` — **do not**
  register them manually.
- The DTO **must not** be an entity, an EF type, or anything from
  `AR.Core.Domain`. Keep the HTTP contract separate from the domain model
  even when the shapes look identical today — they will drift.
- One DTO per use case. Do not share a `FooRequest` between `Create` and
  `Update` paths by making fields optional — split into
  `CreateFooRequest` / `UpdateFooRequest`. The validator rules diverge
  almost immediately and you lose the `required`-based contract guarantee.

The controller signature is then `[FromBody] CreateWidgetRequest request`;
no `[RequestModelFluentValidator]` attribute is needed on the action — the
filter is wired globally and triggers off `IBaseValidationModel`.

---

## 3. Read-only ApiService

Same shape; just simpler signatures and no DTOs (a controller route value is
fine):

```csharp
public interface IWidgetApiService
{
    Task<IReadOnlyList<(int Id, string Name)>> GetAsync();
    Task<(int Id, string Name)?> GetAsync(int id);
}

[UsedImplicitly]
internal class WidgetApiService : IWidgetApiService
{
    private readonly IWidgetAppService _widgetAppService;

    public WidgetApiService(IWidgetAppService widgetAppService) => _widgetAppService = widgetAppService;

    public Task<IReadOnlyList<(int Id, string Name)>> GetAsync() => _widgetAppService.GetAsync();

    public Task<(int Id, string Name)?> GetAsync(int id) => _widgetAppService.GetAsync(id);
}
```

Project tuples / DTOs at the app-service layer (or earlier with a Dapper
query handler); do not project here. The ApiService is type-passthrough.

---

## Changing existing ApiServices

- **Add a method to an existing interface** — extend `IFooApiService`,
  implement on the existing class, reuse the already-injected `IFooAppService`.
  If the new method needs a second app service, add it to the constructor;
  DI resolves it automatically by convention.
- **Add a property to an existing request DTO** — add it with the right
  nullability semantics (`required` for mandatory, `T?` for optional), then
  extend the matching `AbstractValidator<T>`. Do not split the DTO unless the
  shape actually fragments across two use cases.
- **Rename an ApiService** — keep the `ApiService` postfix or DI will
  silently drop the binding. `Grep` for callers in `AR.API/Controllers`
  first; the controller-side rename is the controller skill's responsibility,
  not this one's.
- **Tighten a validator** — edit the validator file directly. Behaviour is
  observable end-to-end (DTO → `ValidationException` →
  `GlobalExceptionHandler` → `ProblemDetails` 400). Don't add equivalent
  guards in the ApiService or controller; the validator is the single
  source of truth for request-shape rules.
- Stay surgical: don't reformat unrelated files, don't widen the interface
  beyond what the request needs, and don't "improve" an adapter into an
  orchestrator opportunistically.

---

## Deleting an ApiService

Two distinct meanings — clarify which is intended:

- **Drop a method from the interface** — remove it here and stop. `Grep`
  `AR.API/Controllers` for callers; if there are any, stop and report them.
  Their removal belongs to the controller skill.
- **Remove the ApiService entirely** — delete `<Name>ApiService.cs` only
  after `Grep` shows no `I<Name>ApiService` references in
  `AR.API/Controllers`. If references exist, stop and report them. Do not
  modify those projects from this skill.

When deleting a DTO, also delete its validator (and vice versa). Both are
orphans if the consumer is gone, and an unused validator stays alive in DI
because the registration is assembly-scan-based.

Removing the files also removes their DI bindings because both ApiService
registration (`AddServicesFromAssemblyExt`) and validator registration
(`AddValidatorsFromAssemblyContaining<>`) are convention-based on assembly
scan — no `ApiServicesDIModule` edit is needed.

---

## Boundary notes

- **No HTTP plumbing reaches this project.** No `IActionResult`,
  `ControllerBase`, `[FromBody]`, `HttpContext`, `IHttpContextAccessor`,
  status codes, or `ProblemDetails`. The controller layer owns all of that.
- **No `AR.Core` runtime types in DTOs.** A request DTO never references a
  domain entity, an event, a `MayBe<T>`, or a `*ExceptionExt`. The contract
  is HTTP-shaped; the domain is domain-shaped. If they coincide, copy the
  shape — do not alias the type.
- **No logging here.** The controller logs the request envelope when needed;
  the app service logs business outcomes. An ApiService that needs to log
  something usually has business logic creeping into it — refactor instead.
- **No `async`/`await` for the sake of `await`.** Return the `Task<T>` from
  the app service directly. `await` only when you genuinely transform the
  result — and if you do, that transformation is a red flag (it usually
  belongs in the app service).
- **No DI registration glue.** If you find yourself opening
  `ApiServicesDIModule`, the naming convention is probably wrong. Fix the
  name; leave the module alone. The only legitimate edits to that file are
  bulk wiring concerns the user explicitly asks for.

---

## Checklist before finishing

- [ ] File placed under `API/AR.ApiServices/<Area>/`, namespace `AR.ApiServices.<Area>`.
- [ ] Class is `internal` and `[UsedImplicitly]`; interface is `public`.
- [ ] Naming postfix is exact: `ApiService` on both class and interface (`IFooApiService` / `FooApiService`). DI scan depends on it.
- [ ] No manual DI registration added — convention scan handles it.
- [ ] Methods are delegations; no business logic, no transactions, no event raising, no `MayBe<T>.Match`, no `*ExceptionExt` throws.
- [ ] Only `I<Name>AppService` (and rarely a sibling app service) is injected. No `DbContext`, `IUnitOfWork`, `IEventDispatcher`, `HttpContext`, `ILogger<T>`, or persistence types.
- [ ] Request DTOs live under `<Area>/Models/`, are `public`, derive from `BaseValidationModel<T>`, use `required` + `init`, contain no methods or `[Required]`/`[StringLength]`-style attributes.
- [ ] Validators live under `<Area>/Models/Validators/`, are `public`, derive from `AbstractValidator<T>`, and carry `[UsedImplicitly]`.
- [ ] No HTTP types (`IActionResult`, `HttpContext`, …) and no domain types (`Entity`, `MayBe<T>`, events) leak through the ApiService contract.
- [ ] No controller, app-service, persistence, or domain code touched — those go through their own skills.
- [ ] `dotnet build AR.slnx -c Release` succeeds.
