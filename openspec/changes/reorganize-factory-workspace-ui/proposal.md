## Why

The current factory demo and mobile factory HUDs expose most controls as always-on stacked panels, which makes the editing workspace feel crowded and leaves newer features like blueprints and mobile-factory state mixed into one long overlay. We need a more formal workspace UI now so the mobile editor, focused demo, and large sandbox scenario can surface build, blueprint, test, and factory-detail tools through a clearer top menu without hiding important functionality.

## What Changes

- Add a shared factory workspace navigation shell that provides a top menu bar and scene-specific workspaces instead of showing every HUD section by default.
- Reorganize the focused mobile factory demo UI so command controls, editor tools, blueprint tools, and mobile factory detail information live in clearly labeled workspaces that can be opened on demand.
- Expand the mobile factory interior editor UI so blueprint actions are reachable from the top workspace menu and the player can open dedicated mobile factory detail content without leaving split-view editing.
- Reorganize the large mobile factory test scenario UI into categorized workspace panels, including a sandbox-style build test panel and other scenario-facing inspection panels.
- Rework the static factory sandbox/demo HUD into formal menu-driven categories so build, blueprint, telemetry, combat, and testing content are split into focused panels rather than one always-expanded stack.

## Capabilities

### New Capabilities
- `factory-workspace-navigation`: Shared top-menu workspace navigation and panel switching for factory-focused demo HUDs.

### Modified Capabilities
- `factory-production-demo`: The static sandbox demo HUD changes from one always-visible stack into categorized workspace panels that preserve viewport readability.
- `mobile-factory-demo`: The focused mobile factory demo exposes its primary gameplay controls and reference information through menu-selected workspaces instead of one default-expanded overlay.
- `mobile-factory-interior-editing`: The interior editor gains top-menu access to blueprint tools and mobile factory detail content while keeping split-view editing active.
- `mobile-factory-test-scenario`: The large scenario HUD exposes categorized sandbox/test panels so building tools and scenario diagnostics are separated instead of always shown together.

## Impact

- Affected code will center on `scripts/factory/FactoryHud.cs`, `scripts/factory/MobileFactoryHud.cs`, `scripts/factory/FactoryBlueprintPanel.cs`, `scripts/factory/FactoryDemo.cs`, and `scripts/factory/MobileFactoryDemo.cs`.
- The change will likely introduce a reusable workspace-menu control or pattern that both the static sandbox and mobile demo HUDs can share while still allowing scene-specific panel content.
- Existing blueprint, structure-detail, telemetry, and command-mode UI flows will need to be rehomed into workspace panels without regressing current build and inspection interactions.
- Demo smoke coverage will need updates so both the focused mobile demo and the large scenario verify that the expected workspace panels are reachable and that critical tools remain available after the UI reorganization.
