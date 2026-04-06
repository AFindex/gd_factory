## ADDED Requirements

### Requirement: Factory demo scenes share a common runtime composition contract
The project SHALL provide a shared runtime composition contract for factory demo scenes so both the static sandbox and the mobile-factory demo bootstrap through the same lifecycle skeleton for common services, scene roots, player support, and preview infrastructure.

#### Scenario: Static sandbox composes the shared runtime skeleton
- **WHEN** the static `FactoryDemo` scene is initialized
- **THEN** it acquires its common environment, simulation, camera, player-support, and preview infrastructure through the shared demo composition layer while still supplying static-sandbox-specific authored world content and HUD state

#### Scenario: Mobile demo composes the shared runtime skeleton
- **WHEN** the `MobileFactoryDemo` scene is initialized
- **THEN** it acquires the same common runtime skeleton through the shared demo composition layer while still supplying mobile-specific deploy, editor, and authored scenario behavior

### Requirement: Shared interaction bridges coordinate detail and player inventory flows
The project SHALL route repeated demo-controller glue for structure inspection, detail models, player inventory selection, and inventory transfer actions through shared interaction bridges instead of maintaining near-identical controller logic in both demo scripts.

#### Scenario: Shared detail bridge publishes selected structure details
- **WHEN** either demo selects a structure that exposes inspection or detail-provider interfaces
- **THEN** the shared interaction bridge resolves the inspection/detail payload and delivers it to that demo's HUD adapter without requiring duplicate selection-to-detail code paths in both scene controllers

#### Scenario: Shared player bridge handles backpack-driven placement selection
- **WHEN** either demo receives player hotbar or backpack slot input that arms a placeable structure
- **THEN** the shared player interaction bridge updates the selected inventory context and placement intent while leaving demo-specific placement validation to the concrete demo

### Requirement: Shared preview helpers provide reusable preview primitives
The project SHALL expose reusable preview helpers for shared preview primitives such as facing arrows, preview materials, and power-link-style overlays so both demos can render common preview elements from shared helper code.

#### Scenario: Shared helper renders common facing-arrow styling
- **WHEN** either demo requests a facing arrow for build or deploy preview feedback
- **THEN** the arrow mesh creation and shared styling originate from the shared preview helper layer instead of separate duplicated implementations in each demo controller

#### Scenario: Shared helper renders common preview materials
- **WHEN** either demo applies success, warning, or invalid-state coloring to common preview meshes
- **THEN** those materials are produced through the shared preview helper layer so both demos keep the same baseline visual language for shared preview primitives

### Requirement: Demo-specific orchestration remains outside the shared layer
The project SHALL keep authored scenarios, site-specific state machines, and demo-exclusive workflow rules in the concrete demo controllers or their dedicated helpers rather than absorbing them into the shared runtime composition layer.

#### Scenario: Static sandbox keeps authored world districts local
- **WHEN** the static sandbox changes its starter layout, authored districts, or sandbox-only smoke cases
- **THEN** those changes can remain within `FactoryDemo` or static-demo-specific helpers without requiring mobile-demo orchestration changes in the shared layer

#### Scenario: Mobile demo keeps deploy and interior-editing orchestration local
- **WHEN** the mobile demo changes deploy-preview flow, control modes, or interior-editor-specific scenario behavior
- **THEN** those changes can remain within `MobileFactoryDemo` or mobile-demo-specific helpers without forcing static-demo behavior changes in the shared layer
