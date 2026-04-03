## 1. Shared Blueprint Domain

- [x] 1.1 Add shared serializable blueprint records, entry/config DTOs, and a session-scoped blueprint library service under `scripts/factory/`.
- [x] 1.2 Add structure-specific config extract/apply hooks so recipes, attachment placements, and other stable layout settings round-trip through blueprints while transient runtime state is excluded.

## 2. Capture And Apply Planning

- [x] 2.1 Add blueprint site adapters and capture helpers for `GridManager` and mobile interiors, including normalized local-coordinate selection capture and a full-interior mobile capture path.
- [x] 2.2 Add a transactional blueprint apply planner that translates blueprint entries to a target anchor, reports blockers, and commits placements only when the full plan is valid.

## 3. Static Sandbox Blueprint UI

- [x] 3.1 Update `FactoryDemo` to support blueprint capture/apply controller modes, selection rectangles, and multi-structure world-grid preview overlays.
- [x] 3.2 Add reusable blueprint panel support to `FactoryHud` for saving captures, browsing the library, selecting an active blueprint, and confirming or canceling application.

## 4. Mobile Interior Blueprint Integration

- [x] 4.1 Update `MobileFactoryDemo` to capture interior layouts as blueprints and apply compatible blueprints while refreshing attachment bindings and runtime topology correctly.
- [x] 4.2 Update `MobileFactoryHud` to expose the shared blueprint workflow inside the split-view editor without breaking pane focus, build/delete controls, or structure detail windows.

## 5. Verification And Seed Content

- [x] 5.1 Add static sandbox smoke coverage for capture -> save -> select -> valid apply -> invalid apply so the full blueprint workflow is regression tested.
- [x] 5.2 Add mobile interior smoke coverage for saving a blueprint, applying it to a compatible interior, and rejecting incompatible bounds or attachment requirements.
- [x] 5.3 Seed at least one reusable blueprint test case from an existing sandbox layout and one from a mobile factory preset so manual QA has known-good library entries to exercise.
