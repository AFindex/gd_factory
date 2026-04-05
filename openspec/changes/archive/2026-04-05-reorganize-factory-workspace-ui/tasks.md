## 1. Shared Workspace Shell

- [x] 1.1 Add shared workspace-menu models and reusable HUD shell helpers under `scripts/factory/` so scenes can register labeled workspace panels and switch the active panel from a top menu bar.
- [x] 1.2 Update `FactoryBlueprintPanel` so the blueprint workflow can be hosted cleanly inside a workspace body instead of only acting like a free-floating sibling overlay.

## 2. Static Sandbox Workspace Reorganization

- [x] 2.1 Refactor `FactoryHud` to replace the one always-expanded sidebar with categorized workspace panels for build, blueprints, telemetry, combat, and testing while keeping the compact default summary readable.
- [x] 2.2 Update `FactoryDemo` to publish workspace state, route workspace-selection events, and keep existing build/blueprint interactions working after the sandbox HUD is reorganized.

## 3. Mobile Demo And Interior Editor Workspaces

- [x] 3.1 Refactor `MobileFactoryHud` to add the shared top workspace menu, compact status strip, contextual world/editor workspace hosts, and a dedicated mobile factory detail workspace.
- [x] 3.2 Update `MobileFactoryDemo` so command controls, interior build tools, blueprint workflow, and mobile factory detail content are exposed through workspace selection without interrupting split-view editing or control-mode flow.

## 4. Scenario Panels And Verification

- [x] 4.1 Add large-scenario workspace definitions so sandbox-style build testing, diagnostics, and observation content are split into separate panels instead of one combined HUD stack.
- [x] 4.2 Extend static sandbox and mobile-factory smoke coverage to verify workspace availability, panel switching, blueprint reachability, and the dedicated scenario build-test panel.
