## Context

The project already has a solid first layer of map validation in `FactoryMapValidator`: schema version, bounds, duplicate occupancy, deposit overlap, placement eligibility by map kind, and basic drill-to-deposit checks. It also has lightweight smoke coverage through `FactoryMapSmokeSupport`, but that coverage is currently limited to malformed-document rejection and serializer round trips.

That leaves a gap between "the document parses" and "the authored map is actually usable." A map can still fail when replayed through real placement rules, and mobile-factory interiors are even more context-sensitive because boundary attachments depend on the selected `MobileFactoryProfile`, legal mount definitions, and deployment-facing world projections. The new validation pass needs to stay close to the existing runtime so it catches real problems without requiring an interactive scene boot.

## Goals / Non-Goals

**Goals:**
- Provide a headless validation path for authored `.nfmap` assets and mobile-factory map bundles.
- Reuse the real placement/runtime rules instead of creating a second handwritten validator that can drift from gameplay behavior.
- Produce actionable diagnostics for hard failures and advisory diagnostics for likely logistics/power/connectivity issues.
- Validate mobile-factory interiors with the correct profile-aware mount and deployment rules.
- Make the shared validation path callable from smoke tests and future CI-style workflows.

**Non-Goals:**
- Building a full map editor or authoring UI in this change.
- Simulating long-running production balance, throughput tuning, or combat outcomes as part of map validation.
- Auto-fixing broken maps or reordering authored structures automatically.
- Replacing the existing map loader or changing the semantic `.nfmap` file format itself.

## Decisions

### Decision: Use an explicit validation-target catalog instead of inferring everything from filenames

The validator will work from named validation targets, not from raw directory guesses alone. A target can represent either:
- a standalone world map, or
- a mobile-factory bundle that pairs a world map, an interior map, and the mobile factory profile needed to interpret boundary attachments correctly.

Why this over filename inference:
- Mobile interiors are not self-describing enough to know which profile and attachment mounts should be applied.
- Explicit targets make headless output stable and human-readable.
- The same catalog can later cover scenario-specific maps without teaching the validator brittle naming conventions.

### Decision: Split validation into three stages

1. **Document stage**: existing parse/schema checks, malformed-file rejection, bounds checks, duplicate occupancy, deposit rules, and unsupported kinds.
2. **Replay stage**: reconstruct the authored content into temporary runtime harnesses that reuse real placement APIs and mobile-factory profile rules, collecting actionable errors when replay fails.
3. **Connectivity stage**: inspect the replayed result and topology to emit advisory warnings or info about likely isolated logistics, power, and port segments.

Why this split:
- It preserves the current fast deterministic document checks.
- It lets replay failures surface the same reasons players or loaders would see at runtime.
- It keeps connectivity analysis separate from hard placement validity so advisory findings do not incorrectly block every intentionally incomplete sandbox fragment.

### Decision: Validate through temporary runtime harnesses instead of booting full demo scenes

World-map validation will create a temporary `GridManager`, `SimulationController`, and structure root, then replay the map through the same placement flow the loader uses. Mobile-factory validation will create a temporary `MobileFactoryInstance` with the selected `MobileFactoryProfile` and validate the interior layout through the same attachment and interior-placement rules used by the focused demo.

Why this over scene bootstrapping:
- The validator stays headless, fast, and deterministic.
- It avoids coupling validation to HUD, camera, or input setup.
- It still exercises the real placement/runtime code paths that matter for authored maps.

### Decision: Emit structured diagnostics with severity, source context, and authored coordinates

Each finding will carry structured fields such as target id, map path, severity (`error`, `warning`, `info`), category (`document`, `placement`, `mobile-profile`, `connectivity`, etc.), authored entry context, and relevant cells or mount identifiers when available. The command-line output can then render a readable report without losing machine-usable detail.

Why this over plain free-form strings:
- Headless failures need to tell content authors exactly which map entry broke.
- Smoke tests can summarize failures consistently.
- Future tooling can reuse the same report model without parsing ad hoc text.

### Decision: Treat connectivity as advisory by default

Connectivity analysis will not fail validation merely because a structure has no clear upstream/downstream partner or a power consumer appears isolated. Those findings will be surfaced as warnings or informational summaries unless they are already covered by hard placement/profile rules.

Why this over making connectivity fatal:
- Sandbox maps intentionally contain probes, stubs, and partial production chains.
- Static graph checks are useful, but they are heuristics and can produce false positives.
- The user explicitly wants these details surfaced, which is better served by rich reporting than by turning every topology note into a blocker.

### Decision: Keep serializer regression checks and route them through the new validation workflow

The existing malformed-document rejection and round-trip checks remain valuable and will stay in the smoke path, but they will be grouped under the broader validation report so map smoke coverage no longer stops at serialization correctness.

Why this over deleting the current smoke behavior:
- Serializer stability is still part of authored-map correctness.
- We can expand coverage without losing the cheap deterministic checks already in place.

## Risks / Trade-offs

- **[Replay harness drifts from the real loader]** -> Mitigate by making the validator call shared runtime-loading helpers instead of duplicating placement logic in a separate subsystem.
- **[Connectivity warnings are noisy]** -> Mitigate by keeping them advisory, tagging them with a dedicated category, and avoiding failure exit codes for warning-only runs.
- **[Mobile validation misses future profiles or scenario bundles]** -> Mitigate by centralizing target registration so each new authored map/profile pair is added explicitly.
- **[Temporary runtime objects become too expensive]** -> Mitigate by keeping harnesses minimal: no HUD, no camera, no full demo orchestration, and only enough topology rebuild to support diagnostics.
- **[Authors cannot tell whether a failure is document-level or runtime-level]** -> Mitigate by separating diagnostic categories and rendering a per-target summary with error/warning/info counts.

## Migration Plan

1. Introduce shared validation target and diagnostic models next to the current map runtime helpers.
2. Refactor existing document validation and round-trip smoke checks to feed the new report model.
3. Add temporary replay validators for standalone world maps and mobile-factory bundles.
4. Switch map smoke helpers to use the shared headless validator while retaining serializer coverage.
5. Add developer-facing documentation for the new headless command and current validation targets.

## Open Questions

- Should a future follow-up add a stricter mode that can escalate selected connectivity warnings to errors for CI, or should that remain outside this change?
