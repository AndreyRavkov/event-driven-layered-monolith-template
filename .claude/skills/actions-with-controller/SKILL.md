---
name: actions-with-controller
description: Create a new API controller or extend an existing one in the AR.API project, following the solution's conventions (BaseApiV1Controller, thin *ApiService adapters, FluentValidation request DTOs, convention-based DI). Use when the user asks to add an endpoint, add a controller, add an action/route, or modify an existing controller.
user-invocable: true
allowed-tools:
  - Read
  - Write
  - Edit
  - Bash(dotnet build *)
  - Bash(dotnet format *)
  - Grep
  - Glob
---

# Create or Update or Delete a Controller (AR.API)

Guidance for adding or extending controllers the way this solution does it.
Keep changes surgical and match surrounding style — see `common-llm.md`.
Keep the controller thin and use the `ApiServices` skill. 

## Layering — where each piece lives

A request flows **Controller → `ApiService` → `AppService` (Core)**.
The controller is a thin HTTP adapter; it never talks to repositories or the DB directly.
The skill responsible only actions with Controllers, don't touch other layers. Use layer skills for actions with other layers.

Put files in:

| Piece | Project / folder |
| --- | --- |
| Controller | `API/AR.API/Controllers/<Area>/<Name>Controller.cs` |

`<Area>` groups a feature (e.g. `Account`, `Users`). Reuse an existing area folder when the endpoint belongs to it.
Locate the controller in the `API` project, under `Controllers` folder. Detect `<Area>` from the context or create a new one.
Find a ApiService which is responsible for your action and if it not exists use skill for actions for `ApiService`.
Request DTOs and its Validators live in `ApiServices` project.
If you use need to use skill, then ask. If you are not sure how `<Area>` should be named or used, then ask.


## Creating a new controller
**Controller** — derive from `BaseApiV1Controller`, route via the version
   token, tag for OpenAPI grouping. Use the API service(s):

   ```csharp
   using AR.ApiServices.<Area>;
   using AR.ApiServices.<Area>.Models;
   using Microsoft.AspNetCore.Mvc;

   namespace AR.API.Controllers.<Area>;

   [Route("v{version:apiVersion}/widgets")]
   [Tags("Widgets")]
   public class WidgetsController : BaseApiV1Controller
   {
       private readonly ILogger<WidgetsController> _logger;
       private readonly IWidgetApiService _widgetApiService;

       public WidgetsController(ILogger<WidgetsController> logger, IWidgetApiService widgetApiService)
       {
           _logger = logger;
           _widgetApiService = widgetApiService;
       }

       /// <summary>
       /// Create a widget
       /// </summary>
       /// <returns>The new widget id.</returns>
       [HttpPost]
       [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
       public async Task<IActionResult> Create([FromBody] CreateWidgetRequest request)
       {
           var id = await _widgetApiService.CreateAsync(request.Name);
           return CreatedAtAction(nameof(Get), GetControllerName(nameof(WidgetsController)), new { id }, id);
       }

       /// <summary>
       /// Get a widget by id
       /// </summary>
       [HttpGet("{id:guid}")]
       public async Task<IActionResult> Get(Guid id) => Ok(await _widgetApiService.GetAsync(id));
   }
   ```

   - Need the caller's identity? Use `UserId` / `UserName` from the base — don't
     read claims directly.
   - For `CreatedAtAction`, pass the controller name through
     `GetControllerName(nameof(WidgetsController))` (strips the `Controller` suffix).

**Build** to confirm wiring (warnings are errors):
`dotnet build AR.slnx -c Release`.

## Updating an existing controller

- **Add an action** — add the method following the patterns above. Reuse the
  already-injected services; if you need a new dependency, add it to the
  constructor (the DI container resolves it automatically by convention).
- **Add a request body** — create the DTO + validator as in steps 1–2;
  validation activates automatically (no attribute needed).
- **Add business logic** — extend the relevant `*ApiService` interface and its
  `*AppService`; keep the controller thin.
- Stay surgical: change only what the request needs, match the existing file's
  style, and don't refactor unrelated code.

## Checklist before finishing

- [ ] Controller derives from `BaseApiV1Controller`; route uses `v{version:apiVersion}/...` and has `[Tags(...)]`.
- [ ] Request DTOs derive from `BaseValidationModel<T>`; each has an `AbstractValidator<T>`.
- [ ] Logic lives in `*AppService`; the `*ApiService` only adapts. Controller has no DB/repository access.
- [ ] No manual DI registration is added (naming convention handles it).
- [ ] `[AllowAnonymous]` only where a public endpoint is intended.
- [ ] `dotnet build AR.slnx -c Release` succeeds.