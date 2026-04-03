## Why

The factory sandbox and mobile factory editor currently let players place, delete, and inspect structures, but they do not provide any way to capture a proven layout and reuse it elsewhere. We need blueprint support now so iteration can move beyond hand-rebuilding the same logistics patterns, and so the project has a concrete end-to-end workflow for authoring, saving, testing, and reapplying layouts across both the world grid and mobile factory interiors.

## What Changes

- Add a shared blueprint workflow that can capture a selected layout from the current scene, save it as a reusable blueprint record, and apply that blueprint back into a compatible target site with preview and validation.
- Add a lightweight in-game blueprint library UI in the sandbox so players can name, inspect, save, select, and apply blueprints without leaving the demo scenes.
- Support full-world sandbox blueprints for multi-structure logistics layouts, including structure kind, facing, relative cell offsets, and the build metadata needed to recreate the layout.
- Support mobile factory interior blueprints so current authored presets can be saved from live scenes and re-applied to compatible mobile factory interiors through the same workflow.
- Define clear apply-time rules for incompatible cells, blocked placements, mobile boundary attachments, and what runtime state is intentionally excluded from blueprints.
- Add smoke-testable blueprint scenarios that cover capture from an existing scene, persistence within the running session, and successful reapplication into a clean or partially occupied target area.

## Capabilities

### New Capabilities
- `factory-blueprint-workflow`: Shared blueprint capture, library, preview, validation, and apply behavior for factory sites and mobile factory interiors.

### Modified Capabilities
- `factory-grid-building`: Grid building must support area selection, multi-structure blueprint previews, and validated application of saved blueprints in the static sandbox.
- `mobile-factory-interior-editing`: Interior editing must support saving the current layout as a blueprint and applying compatible blueprints while preserving split-view editing and boundary-attachment constraints.

## Impact

- Affected code will include shared factory-domain models under `scripts/factory/`, site/build validation in `GridManager` and mobile interior site logic, and demo controllers/HUDs in `FactoryDemo`, `FactoryHud`, `MobileFactoryDemo`, and `MobileFactoryHud`.
- The change will likely add blueprint serialization models, capture/apply services, and scene-level UI state for selection rectangles, blueprint library entries, and apply previews.
- Existing authored mobile presets and sandbox starter layouts will become important seed content for blueprint capture/apply regression coverage.
- No external dependency is required, but the change will expand smoke-test scope for both the static sandbox and the focused mobile factory demo.
