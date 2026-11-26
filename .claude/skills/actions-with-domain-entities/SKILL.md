---
name: actions-with-domain-entities
description: Add, change, or delete a domain entity in the AR.Core.Domain project, following the solution's DDD conventions (Entity / AuditableEntity base types, EntityMap<T> / AuditableEntityMap<T> EF mappings, snake_case columns, auto-discovered configurations). Use when the user asks to add a domain entity/model, add or rename a property/column, change an entity mapping, or remove an entity. Other layer skills (repository, app service) invoke this skill when they need an entity to exist.
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

# Add / Change / Delete a Domain Entity (AR.Core.Domain)

Guidance for shaping the domain model the way this solution does it.
Keep changes surgical and match surrounding style — see `common-llm.md`.

This skill owns **only the domain layer** (`Core/AR.Core.Domain`): the entity type
and its EF mapping. It does **not** touch persistence, repositories, app services,
event types, or handlers. When the work needs those, use their layer skills:

| Need | Skill / layer |
| --- | --- |
| Read/write rows, queries | repository / `AR.Core.Persistence` skill |
| Business logic, transactions | app service / `AR.Core.AppServices` skill |
| A new domain **event type** | events / `AR.Core.Events` skill |
| Handling/consuming an event | event-handler / consumer skill |

If you are unsure whether a change belongs here or in another layer, ask before editing.

## Where each piece lives

A domain entity is **two files** in the same area folder:

| Piece | Project / folder |
| --- | --- |
| Entity | `Core/AR.Core.Domain/<Area>/<Name>Entity.cs` |
| EF mapping | `Core/AR.Core.Domain/<Area>/<Name>EntityMap.cs` |

`<Area>` groups a feature (e.g. `Users`). Reuse an existing area folder when the
entity belongs to it; create a new one otherwise. If you are unsure how `<Area>`
should be named, ask.

> **Namespace quirk:** the project's `RootNamespace` is `AR.Domain` (not
> `AR.Core.Domain`). Files use `namespace AR.Domain.<Area>;`. Match this — it is
> what every existing entity does.

## Base types — pick one

Every entity derives from a base in `AR.Domain`:

- **`Entity`** — bare. Exposes an in-memory `DomainEvents` list (`IList<BaseDomainEvent>`)
  and `ClearDomainEvents()`. Use for entities that have no audit/lifecycle columns.
- **`AuditableEntity : Entity`** — adds `CreatedOn`, `UpdatedOn`, `CreatedBy`,
  `UpdatedBy`, `ChangeSource`, and `Active` (default `true`). **Default choice** for
  business entities. The audit columns are filled automatically on save by
  `UpdateAuditableEntityInterceptor` — never set them by hand. `Active` is the
  **soft-delete flag** (see *Deleting* below).

## Adding a new entity

1. **Entity class** — plain POCO, `public`, properties only (no behaviour unless the
   domain genuinely needs an invariant). Use `required` for non-nullable members the
   caller must supply. Choose the `Id` type the feature needs (`int` for
   identity/auto-increment keys, `Guid` for client-assigned/distributed keys).

   ```csharp
   namespace AR.Domain.Widgets;
   
   public class WidgetEntity : AuditableEntity
   {
       public int Id { get; init; }
       public required string Name { get; init; }
   }
   ```

2. **EF mapping** — `sealed` class deriving from `AuditableEntityMap<T>` (or
   `EntityMap<T>` for a non-auditable entity). Override `ConfigureProperties`; set the
   table, key, and per-column config. **Columns are `snake_case`.** Do **not** map
   `DomainEvents` (the base `Configure` ignores it) and do **not** map the audit
   columns (`AuditableEntityMap` already does). The default schema is `main`.

   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;

   namespace AR.Domain.Widgets;

   public class WidgetEntityMap : AuditableEntityMap<WidgetEntity>
   {
       protected override void ConfigureProperties(EntityTypeBuilder<WidgetEntity> builder)
       {
           builder.ToTable("widgets");
           builder.HasKey(x => x.Id);
           builder.Property(x => x.Name).HasColumnName("name").IsRequired();
       }
   }
   ```

   The mapping is picked up automatically — `DbDomainContext` calls
   `ApplyConfigurationsFromAssembly(typeof(Entity).Assembly)`. **No manual
   registration, and no `DbSet<>` property** is needed; the generic
   `IRepository<T>` resolves any mapped entity.

3. **Build** to confirm the mapping compiles (warnings are errors):
   `dotnet build AR.slnx -c Release`.

> **Schema migrations are out of scope here.** This solution has no migrations.

## Changing an entity

- **Add a property** — add it to the entity class, then add its
  `builder.Property(x => x.Foo).HasColumnName("foo")...` line to the map. Mirror the
  existing column's style (`IsRequired`, `HasMaxLength`, etc.).
- **Rename a property/column** — rename in the class and update the matching
  `HasColumnName(...)` in the map. Search callers (`Grep`) before renaming a public
  property; this entity may be referenced by repositories/app services owned by other
  layers — coordinate via their skills rather than editing them blindly.
- **Remove a property** — delete it from the class and remove its `Property(...)` line
  from the map. Remove only mappings your change orphaned.
- Stay surgical: change only what the request needs, match the file's style, and don't
  refactor unrelated mappings.

## Deleting an entity

There are two distinct meanings — clarify which is intended:

- **Soft delete (row lifecycle, the common case):** there is *no domain-type change*.
  Auditable entities carry `Active`; "deleting" a record means setting `Active = false`
  in an app service. That is **not this skill's job** — defer to the app
  service / repository skill. Do not delete the entity type for this.
- **Removing the entity type itself (the model is gone):** delete both
  `<Name>Entity.cs` and `<Name>EntityMap.cs`. First `Grep` the solution for the type
  name — repositories, app services, events, and DTOs in other projects may depend on
  it. If references exist, stop and report them; their removal belongs to the owning
  layer's skill, not here.

## Domain events (boundary note)

`Entity` exposes a `DomainEvents` list so an entity *can* carry events, which
`IUnitOfWork` harvests and dispatches around the commit. **Defining the event type**
lives in `AR.Core.Events`; **handling it** lives in `AR.Core.EventHandlers` /
`EventQueueConsumers`. In this template, state-change events are usually emitted from
the app service via `IUnitOfWork.AddEventAsync(...)`, not from the entity. Keep all of
that out of this skill — only the entity/mapping shape belongs here; delegate the rest.

## Checklist before finishing

- [ ] Entity in `Core/AR.Core.Domain/<Area>/`, namespace `AR.Domain.<Area>`.
- [ ] Derives from `AuditableEntity` (default) or `Entity`; audit columns are **not** set or mapped by hand.
- [ ] A `sealed`/standard `*EntityMap` derives from `AuditableEntityMap<T>` / `EntityMap<T>`, overrides `ConfigureProperties`, sets `ToTable`, `HasKey`, and `snake_case` columns.
- [ ] `DomainEvents` is not mapped; no `DbSet<>` or manual configuration registration added.
- [ ] No persistence/app-service/event/handler code touched — those went through their own skills.
- [ ] `dotnet build AR.slnx -c Release` succeeds.