## ADDED Requirements

### Requirement: Factory demos reuse one baseline interaction shell
The project SHALL provide one shared baseline interaction shell for factory demo scenes so the static sandbox and the mobile-factory demo reuse the same core workflow for HUD-facing interaction state, structure selection, detail publishing, build-delete-blueprint mode switching, placement intent, and common preview coordination.

#### Scenario: Static sandbox runs through the shared baseline interaction shell
- **WHEN** the player uses `factory_demo.tscn` to inspect a structure, arm a buildable item, switch between interact/build/delete/blueprint flows, or update the shared preview state
- **THEN** `FactoryDemo` routes those baseline interactions through the shared interaction shell while keeping static-sandbox-specific placement validation, authored layout rules, and testing-only commands in static-demo-owned code

#### Scenario: Mobile demo layers extra gameplay on top of the shared shell
- **WHEN** the player uses the world-facing baseline factory interactions inside the mobile-factory demo
- **THEN** `MobileFactoryDemo` reuses the same shared interaction shell for the overlapping workflow and adds only mobile-factory-specific deploy, anchor, lifecycle, and interior-editor behavior in mobile-demo-owned code

### Requirement: Shared interaction shell projects a common baseline HUD contract
The project SHALL expose the overlapping interaction state of both demos through a shared HUD projection contract so baseline factory interaction feedback does not require separate controller-owned state-to-HUD translation in each demo.

#### Scenario: Shared HUD projection reports baseline interaction state in the static demo
- **WHEN** the static sandbox updates its selected structure, active baseline interaction mode, placement source, blueprint state, or preview message
- **THEN** the shared interaction shell publishes that baseline state through the shared HUD projection contract and the static HUD consumes it without duplicating the controller translation logic used by the mobile demo

#### Scenario: Mobile HUD consumes the same baseline projection before mobile-only overlays
- **WHEN** the mobile-factory demo updates the overlapping baseline factory interaction state
- **THEN** its HUD consumes the same shared projection contract before adding deploy-preview, editor-session, or other mobile-only overlays

### Requirement: Shared interaction shell centralizes baseline input and preview gating
The project SHALL centralize the overlapping baseline input guards and preview-gating rules for both demos so shared inventory, pointer-over-UI, selection, and preview activation behavior stays consistent across the reusable interaction flow.

#### Scenario: Shared guards block baseline world actions while UI owns the pointer
- **WHEN** either demo has an active inventory interaction or the pointer is over a baseline UI surface that should consume interaction
- **THEN** the shared interaction shell suppresses the overlapping world-facing build, delete, selection, and preview actions through one reusable guard path instead of separate controller-specific checks

#### Scenario: Demo-specific preview branches stay outside the shared gate
- **WHEN** the mobile-factory demo needs deploy-anchor previews or interior-editor-only preview behavior that has no static-demo equivalent
- **THEN** those preview branches remain in mobile-demo-owned code while the shared shell continues to own only the overlapping baseline preview gating and state transitions
