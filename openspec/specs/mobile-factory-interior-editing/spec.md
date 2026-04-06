# mobile-factory-interior-editing Specification

## Purpose
TBD - updated by change add-complex-mobile-factory-test-scenario. Refine Purpose after archive.
## Requirements
### Requirement: Mobile factory editing uses a split-view workspace
The game SHALL open mobile factory interior editing as a side-sliding split view that keeps the world visible while dedicating most of the screen to the interior editor.

#### Scenario: Opening the editor preserves world context
- **WHEN** the player opens a mobile factory for interior editing
- **THEN** the screen shows an approximate `1:5` world-to-editor split where the world remains visible as a narrow strip and the editor occupies the main area

### Requirement: Mobile factories can be opened for interior editing in any lifecycle state
The game SHALL allow the player to open a mobile factory's interior editor whether the factory is currently deployed or in transit.

#### Scenario: Open editor while deployed
- **WHEN** the player selects a deployed mobile factory and enters interior editing
- **THEN** the split-view editor opens without requiring the factory to be recalled first

#### Scenario: Open editor while in transit
- **WHEN** the player selects a mobile factory that is not currently deployed and enters interior editing
- **THEN** the split-view editor opens and shows the factory's current interior layout

### Requirement: Mouse input follows hover ownership between panes
The game SHALL route mouse interactions to the pane the pointer is currently hovering instead of requiring an explicit focus toggle.

#### Scenario: Hovering the editor controls internal building
- **WHEN** the pointer is over the interior editor pane
- **THEN** mouse actions affect the internal building view and do not manipulate the world pane

#### Scenario: Hovering the world controls the world view
- **WHEN** the pointer is over the world strip
- **THEN** mouse actions affect world interaction and do not place or remove internal structures

### Requirement: Interior editing reuses factory-style build controls
The game SHALL provide interior building controls that mirror the existing factory-building interaction style for selecting, rotating, previewing, placing, and removing structures on the internal grid, including boundary attachments that span the factory edge and multi-cell structures that occupy several interior cells.

#### Scenario: Boundary attachment placement uses familiar controls
- **WHEN** the player is inside the mobile factory editor and chooses a boundary attachment, rotates it, and clicks a valid boundary mount
- **THEN** the attachment is previewed and placed using the same style of build controls used for ordinary internal structures while still respecting its cross-boundary shape rules

#### Scenario: Multi-cell interior structure preview shows the full footprint
- **WHEN** the player selects a compatible multi-cell interior structure and moves the cursor across the mobile-factory grid
- **THEN** the editor preview shows the full occupied footprint, current facing, and validity of every required interior cell before placement

#### Scenario: Removing any occupied cell clears the interior structure
- **WHEN** the player removes a placed multi-cell interior structure by targeting any occupied cell in its footprint
- **THEN** the editor resolves the owning structure and clears the full occupied footprint from the interior layout

### Requirement: Interior interaction mode opens independent structure detail windows
The game SHALL let the player inspect internal structures from the mobile factory editor by opening an independent detail window while the editor remains in interaction mode.

#### Scenario: Left click opens details during interaction mode
- **WHEN** the player is in the mobile factory interior editor interaction mode and left-clicks an existing internal structure
- **THEN** the editor opens or focuses that structure's detail window and does not switch to build placement

#### Scenario: Build mode still prioritizes placement actions
- **WHEN** the player has selected an interior build tool and left-clicks inside the mobile factory editor
- **THEN** the editor continues to treat the click as a build interaction and does not open a structure detail window

### Requirement: Interior editor shows ports and their external state
The game SHALL display mobile factory boundary attachments inside the editor along with their direction, cross-boundary shape, and external connection state.

#### Scenario: Editor shows attachment role and connection state
- **WHEN** the player views a boundary attachment in the mobile factory editor
- **THEN** the editor indicates whether it is an input or output attachment, which cells belong inside versus outside the hull, and whether it is currently connected, disconnected, or blocked at the world boundary

### Requirement: World miniature mirrors the shared interior layout
The game SHALL present a miniature world representation of the mobile factory that reflects the same internal layout and item flow shown in the editor.

#### Scenario: Layout changes are reflected in the world miniature
- **WHEN** the mobile factory's interior layout changes
- **THEN** the world-side factory representation updates to show the same structural arrangement in miniature form

#### Scenario: Item flow remains visible in the world miniature
- **WHEN** items move through the mobile factory interior
- **THEN** the world-side miniature shows a readable animated representation of that internal flow

### Requirement: Scenario mobile factories expose authored interior case studies
The game SHALL allow the player to inspect different mobile factories in the large-scale scenario and find distinct pre-authored interior layouts intended as separate sandbox case studies, and those layouts SHALL be built from real transport, storage, port, refining, assembly, ammo, and support structures instead of placeholder producer-driven templates.

#### Scenario: Different factories reveal different real-layout combinations
- **WHEN** the player opens the interior editor for multiple mobile factories included in the large-scale scenario
- **THEN** the inspected factories show different authored combinations of belts, splitters, mergers, loaders, unloaders, storage, ports, smelters, assemblers, ammo lines, or equivalent real structures instead of sharing one producer-led template

#### Scenario: Interior case studies match the factory's world role
- **WHEN** the player inspects the authored interior layout of a mobile factory assigned to extraction, processing, support, or defense logistics in the large-scale scenario
- **THEN** the internal layout reflects that world-side role through the structures and recipe flow it contains

### Requirement: Authored interior test cases sustain long-running flow
The game SHALL ensure the authored interior sandbox layouts used in the large-scale scenario include recovery paths and real machine-state behavior that keep items moving or being consumed during extended unattended runs without depending on permanent blockage or producer-only shortcuts as the steady state.

#### Scenario: Interior layouts include recovery or consumption paths
- **WHEN** the player inspects a mobile factory interior layout that is meant to run continuously in the large-scale scenario
- **THEN** that layout includes sink, recycler, recirculation, buffering, or equivalent recovery structures that prevent the case from depending on permanent belt blockage as its steady state

#### Scenario: Continuous interior flow survives without placeholder spawning
- **WHEN** the large mobile factory scenario runs unattended for an extended period
- **THEN** the authored interior cases continue to move or consume goods through their real structures without requiring a producer structure to keep the main loop alive

### Requirement: Mobile factory interiors can be saved as blueprints
The game SHALL let the player capture the current mobile factory interior layout through the editor and save it as a reusable blueprint.

#### Scenario: Saving an interior layout keeps the editor workflow active
- **WHEN** the player saves the current mobile factory interior layout as a blueprint from the split-view editor
- **THEN** the blueprint is added to the shared library while the mobile factory editor remains open and usable

### Requirement: Compatible blueprints can be previewed and applied inside the mobile editor
The game SHALL let the player select a compatible blueprint from the library, preview it inside the mobile interior editor, and apply it when the translated layout satisfies interior bounds and attachment constraints.

#### Scenario: Compatible blueprint applies to the current interior
- **WHEN** the player selects a compatible mobile-interior blueprint and confirms a valid apply preview inside the editor
- **THEN** the mobile factory interior recreates the blueprint layout without closing the split-view editing workspace

#### Scenario: Incompatible attachment requirements are rejected
- **WHEN** the player attempts to apply a mobile-interior blueprint whose required boundary attachments or bounds do not fit the current mobile factory interior
- **THEN** the editor reports the compatibility failure in the blueprint workflow UI and leaves the current interior layout unchanged

### Requirement: Interior editor top menu exposes contextual workspaces
The game SHALL add a top workspace menu to the mobile factory interior editor so blueprint actions and mobile factory detail content can be opened from the editor chrome without replacing the split-view editing surface.

#### Scenario: Selecting blueprint workspace keeps split-view editing available
- **WHEN** the player opens the interior editor and switches to the blueprint workspace from the top menu
- **THEN** the editor keeps the split-view world-plus-interior layout active while showing blueprint actions in the selected workspace panel

#### Scenario: Selecting factory detail workspace keeps the editor session active
- **WHEN** the player opens the interior editor and switches to the factory detail workspace from the top menu
- **THEN** the editor shows the current mobile factory detail content without closing the interior viewport or forcing the player out of the current edit session

