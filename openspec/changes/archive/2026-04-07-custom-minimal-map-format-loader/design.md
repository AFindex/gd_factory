## Context

`FactoryDemo` and `MobileFactoryDemo` currently own large amounts of authored layout data in C# bootstrap methods such as `CreateStarterLayout` and mobile-factory world/interior setup flows. Although the underlying building, grid, power, logistics, and blueprint rules are increasingly shared, authored maps are still embedded in controller code.

That coupling creates three problems:

1. Authored maps are difficult to inspect and edit because gameplay content is mixed with controller orchestration.
2. Runtime map construction logic is repeated in demo-specific setup code instead of flowing through one loading pipeline.
3. Godot scene files are not a good fit for factory map content because they carry scene-tree and editor metadata that the factory runtime does not actually need.

This change introduces a project-owned minimal map format plus a runtime loader that rebuilds playable maps from semantic data only.

## Goals / Non-Goals

**Goals:**
- Define a minimal factory map file schema that stores only runtime-relevant authored data.
- Support both world maps and mobile-factory interior maps through one shared map document model.
- Separate the system into four layers: file format, validation/normalization, runtime reconstruction, and demo adapters.
- Move authored layout payloads out of demo controllers while preserving current gameplay behavior.
- Keep reconstruction based on existing factory runtime primitives such as grid/resource/structure placement instead of introducing a parallel simulation model.

**Non-Goals:**
- Replacing the Godot launcher, HUD scene graph, or other UI composition with file-driven scene-tree serialization.
- Building a full in-game map editor in this change.
- Supporting arbitrary Godot node graphs, freeform transforms, or generic scene instancing inside map files.
- Reworking the entire simulation or structure type system.

## Decisions

### Decision: Use a project-owned semantic map document instead of `.tscn` or generic node serialization

The map file will describe gameplay concepts such as bounds, deposits, anchors, and structures rather than Godot nodes. This keeps the file compact, reviewable, and portable across demos.

Why this over `.tscn` or serialized nodes:
- `.tscn` includes editor- and node-graph-oriented data that is not the source of truth for the factory simulation.
- Generic node serialization would leak rendering/layout concerns into authored map data.
- A semantic format lets us reconstruct the same gameplay from shared runtime placement APIs.

### Decision: Keep the first version text-based and deterministic

The first map format should be human-readable and diff-friendly, with stable field order and explicit versioning. A text-based format reduces authoring friction and makes regression review easier while the schema is still evolving.

Why this over binary or resource-only storage:
- Text files are easier to debug during early iteration.
- Deterministic output is helpful for regression review and eventual export tooling.
- We can still keep the content minimal by constraining the schema rather than changing the encoding.

### Decision: Split responsibilities into four layers

1. **Map document layer**: raw DTOs for map files and small enums/value objects.
2. **Validation/normalization layer**: verifies schema version, required sections, duplicate occupancy, invalid kinds/facings, and map-kind-specific constraints.
3. **Runtime reconstruction layer**: converts validated documents into calls against existing world/interior placement APIs, deposit registration, anchor registration, and lightweight authored state hooks.
4. **Demo adapter layer**: each demo chooses which map files to load and how to apply scenario-only extras, smoke hooks, and controller wiring.

Why this split:
- It prevents loader code from depending directly on controller state.
- It keeps validation reusable for future authoring/export tools.
- It preserves room for demo-specific orchestration without embedding authored map data back into controllers.

### Decision: The schema stores only semantic minimums plus optional per-entry payloads

Common required fields should include:
- format version
- map kind (`world`, `interior`, or another explicit supported kind)
- logical bounds or dimensions
- authored deposits/resources
- authored structures with kind, cell, and facing

Optional sections should be allowed only when runtime reconstruction needs them, such as:
- deployment anchors / named landmarks
- lightweight scenario tags or identifiers
- per-structure authored options for recipe or inventory seed data when current runtime behavior requires it

Explicitly excluded fields:
- absolute 3D transforms
- node names / scene paths
- mesh/material references
- duplicated derived data that can be recomputed from kind + cell + facing

### Decision: Reconstruction targets existing placement APIs instead of bypassing them

The loader should rebuild maps through the same core placement/resource registration flows already used by demos, rather than mutating deep runtime collections directly.

Why:
- Preserves behavior parity with current authored setup.
- Reuses existing validation side effects where appropriate.
- Reduces risk of creating “loaded maps” that behave differently from “manually placed maps”.

### Decision: Demo migration should be incremental, starting with one static map and one focused mobile map

The initial implementation should cover:
- the static factory sandbox authored startup layout
- the focused mobile-factory authored world/interior layout

Large-scenario mobile variants or more exotic authored content can migrate after the pipeline proves stable.

Why:
- Limits migration risk while still validating the shared system against both major map shapes.
- Keeps task scope aligned with the user goal: own map format + rebuild system, not a full content rewrite.

## Risks / Trade-offs

- **[Schema grows beyond “minimal”]** → Mitigate by requiring every new field to justify a runtime reconstruction need and by keeping derived/render-only data out of the document.
- **[Loaded maps diverge from existing hardcoded behavior]** → Mitigate by rebuilding through existing placement/resource flows and by preserving smoke coverage for both demos during migration.
- **[Mobile demo needs map-kind-specific exceptions]** → Mitigate by isolating demo-specific logic in adapter code instead of polluting the shared schema with one-off fields.
- **[Authoring becomes fragile without tooling]** → Mitigate by providing strong validation errors, deterministic examples, and keeping the initial file syntax simple.
- **[Migration touches large demo methods]** → Mitigate by extracting only authored layout payloads first and leaving HUD/input/controller orchestration in place.
