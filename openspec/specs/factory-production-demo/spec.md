# factory-production-demo Specification

## Purpose
TBD - created by archiving change add-3d-factory-core-demo. Update Purpose after archive.
## Requirements
### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout is built from real mining, refining, assembly, power, logistics, and defense structures, and the authored districts SHALL form multiple observable loops without relying on legacy producer structures as the primary source of throughput.

#### Scenario: Startup layout uses real structures for the core loop
- **WHEN** the player starts the default static factory demo
- **THEN** at least one authored loop mines resources, refines or assembles them with powered machines, and delivers the result through normal logistics to a receiving endpoint without requiring a producer shortcut

#### Scenario: Defense resupply comes from the same factory economy
- **WHEN** the static sandbox runs with its authored starter layout
- **THEN** at least one defensive lane receives ammunition that originates from mined and manufactured inputs inside the sandbox instead of directly spawned test cargo

### Requirement: Demo sandbox spans multiple authored districts
The game SHALL expand the static sandbox world to include visibly separated authored districts for dense mining fields, receiving stations, manufacturing, power maintenance, defense, and regression-friendly logistics observation so material movement across map-scale roles is readable on one map.

#### Scenario: Expanded sandbox loads mining fields and receiving districts together
- **WHEN** the player enters the static factory demo from the launcher
- **THEN** the playable world includes authored mining regions with more than the baseline three resource families plus separate receiving or depot-style logistics districts connected to the wider sandbox

#### Scenario: Camera and interaction remain usable across the richer sandbox
- **WHEN** the player pans, zooms, and builds across the expanded sandbox
- **THEN** camera bounds, hover targeting, and build previews continue to function across the enlarged world that now includes additional mining and station districts

### Requirement: Starter use cases verify the new core factory systems
The game SHALL include authored starter-layout use cases and smoke coverage that verify real resource extraction, expanded recipe progression, power maintenance, and defense resupply on the default sandbox, and producer-only lanes SHALL NOT count as satisfying those startup use cases.

#### Scenario: Smoke flow validates a real powered production chain
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that mined resources flow through powered manufacturing and reach a receiving endpoint without depending on a producer structure to create the main goods

#### Scenario: Smoke flow validates real defense resupply and power upkeep
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that at least one authored defense lane is resupplied by the sandbox's real production chain while at least one authored powered district remains sensitive to fuel or network maintenance state

### Requirement: Demo showcases differentiated logistics item visuals
The game SHALL render moving logistics items in the static sandbox using their configured visual profiles so different item kinds are readable during normal play.

#### Scenario: Existing items receive immediate color differentiation
- **WHEN** the starter layout is running with the current baseline item set
- **THEN** coal, ore, plates, parts, and ammo payloads each appear with distinct first-pass colors while moving through the logistics network

#### Scenario: Configured billboard or model items appear in the authored layout
- **WHEN** the starter layout includes an item kind configured with a billboard sprite or 3D model transport profile
- **THEN** that moving item appears in the authored logistics line using its configured representation instead of the generic placeholder cube

### Requirement: Demo verification covers richer chain readability
The game SHALL include authored use cases or smoke coverage that verify both the expanded production ladder and the visibility of differentiated moving items in the default sandbox.

#### Scenario: Smoke flow validates expanded crafted output chain
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that at least one multi-stage crafted output depending on more than one resource branch is produced and delivered without manual intervention

#### Scenario: Visual readability checks do not require identical meshes
- **WHEN** smoke or regression verification inspects the authored starter layout after item-visual profiles are enabled
- **THEN** it confirms that moving payloads remain visible and distinguishable by profile configuration without depending on every item using the same geometry

### Requirement: Prototype logistics update on a deterministic simulation step
The game SHALL advance factory entities on a controlled simulation cadence so prototype production behavior is predictable and testable.

#### Scenario: Unconnected output does not teleport items
- **WHEN** a producer has no valid downstream transport path
- **THEN** produced items remain blocked or buffered according to the prototype rules instead of skipping to a destination

### Requirement: Demo feedback exposes loop success
The game SHALL expose compact on-screen feedback in the static factory demo that lets the player confirm both automation health and runtime performance without obscuring most of the playfield.

#### Scenario: Delivered item count is visible
- **WHEN** items reach a destination structure during play
- **THEN** the player can see a visible throughput or delivery readout indicating that the automation loop is functioning

#### Scenario: Compact HUD preserves most of the viewport
- **WHEN** the static factory demo HUD is shown during normal play
- **THEN** its primary panel remains confined to a compact region near one edge of the screen rather than expanding into a large overlay over the factory floor

#### Scenario: Runtime profiler telemetry is visible
- **WHEN** the static factory demo is running
- **THEN** the HUD shows runtime telemetry including current frame rate and at least one performance-hotspot-oriented metric relevant to the demo simulation

### Requirement: Demo scene is immediately playable
The project SHALL boot into a launcher scene that exposes the static factory demo as a selectable experience, and entering that demo from the launcher SHALL still load a playable factory slice without requiring manual scene switching in the editor.

#### Scenario: Project startup enters launcher first
- **WHEN** the player starts the project normally
- **THEN** the main scene loads the launcher and presents the static factory demo as one of the available entries

#### Scenario: Launcher opens the factory gameplay slice
- **WHEN** the player selects the static factory demo from the launcher
- **THEN** the project loads the factory demo scene with the camera, building, and automation loop defined by this capability

### Requirement: Splitter preserves available output under asymmetric blockage
The game SHALL allow a splitter in the static factory simulation to keep forwarding items through any output that is currently able to receive them, even if the other output is blocked.

#### Scenario: One blocked branch does not stall the free branch
- **WHEN** one splitter output is blocked by downstream congestion while the other output path can still accept items
- **THEN** the splitter continues dispatching items through the available output instead of stalling both branches

#### Scenario: Splitter waits only when both branches are blocked
- **WHEN** both splitter outputs are unable to accept the next item
- **THEN** the item remains buffered at the splitter until one of the outputs becomes available

### Requirement: Static sandbox HUD uses categorized workspaces
The game SHALL reorganize the static factory sandbox HUD into categorized workspace panels so build, blueprint, telemetry, combat, and testing content are reachable from the workspace menu without occupying one always-expanded stack.

#### Scenario: Sandbox starts with a compact default workspace
- **WHEN** the player enters the static factory demo from the launcher
- **THEN** the HUD shows the workspace menu and a compact default workspace while the build palette, blueprint workflow, telemetry, and test content remain hidden until their workspace is selected

### Requirement: Sandbox test tools live in a dedicated panel
The game SHALL place sandbox-specific test and verification controls into a dedicated workspace panel separate from the core build workspace so experimental tools do not crowd the main construction UI.

#### Scenario: Build test panel opens independently from build tools
- **WHEN** the player selects the sandbox testing workspace
- **THEN** the HUD shows the build-test and verification controls in their own panel without automatically expanding the normal construction workspace at the same time

### Requirement: Static sandbox includes authored receiving-station case studies
The game SHALL include authored receiving-station or depot-style logistics case studies in the static sandbox so mined and manufactured goods visibly accumulate, transfer, or recycle through map buildings instead of ending only at isolated sinks.

#### Scenario: Receiving station accepts output from a remote production district
- **WHEN** a remote mining or manufacturing district is running in the authored sandbox
- **THEN** at least one receiving-station-style node accepts its output through loaders, unloaders, storage, depots, or equivalent logistics buildings on the shared world map

#### Scenario: Receiving district participates in a closed loop
- **WHEN** the authored sandbox runs for an extended observation period
- **THEN** at least one receiving district forwards, buffers, recycles, or consumes incoming goods in a way that keeps the wider loop readable without depending on permanent blockage

