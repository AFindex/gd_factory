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
The game SHALL provide interior building controls that mirror the existing factory-building interaction style for selecting, rotating, placing, and removing structures on the internal grid.

#### Scenario: Internal placement uses familiar controls
- **WHEN** the player is inside the mobile factory editor and chooses a structure, rotates it, and clicks a valid internal cell
- **THEN** the structure is previewed and placed using the same style of build controls used in the main factory-building experience

### Requirement: Interior editor shows ports and their external state
The game SHALL display mobile factory port cells inside the editor along with direction and external connection state.

#### Scenario: Editor shows whether a port is connected
- **WHEN** the player views a port cell in the mobile factory editor
- **THEN** the editor indicates the port direction and whether it is currently connected to a world-side route

### Requirement: World miniature mirrors the shared interior layout
The game SHALL present a miniature world representation of the mobile factory that reflects the same internal layout and item flow shown in the editor.

#### Scenario: Layout changes are reflected in the world miniature
- **WHEN** the mobile factory's interior layout changes
- **THEN** the world-side factory representation updates to show the same structural arrangement in miniature form

#### Scenario: Item flow remains visible in the world miniature
- **WHEN** items move through the mobile factory interior
- **THEN** the world-side miniature shows a readable animated representation of that internal flow

### Requirement: Scenario mobile factories expose authored interior case studies
The game SHALL allow the player to inspect different mobile factories in the large-scale scenario and find distinct pre-authored interior layouts intended as separate logistics test cases.

#### Scenario: Different factories reveal different authored layouts
- **WHEN** the player opens the interior editor for multiple mobile factories included in the large-scale scenario
- **THEN** the inspected factories show different authored combinations of belts, splitters, mergers, loaders, unloaders, producers, sinks, or bridges instead of sharing one identical template

### Requirement: Authored interior test cases sustain long-running flow
The game SHALL ensure the authored interior test layouts used in the large-scale scenario include recovery paths that keep items moving or being consumed during extended unattended runs.

#### Scenario: Interior layouts include recovery or consumption paths
- **WHEN** the player inspects a mobile factory interior layout that is meant to run continuously in the large-scale scenario
- **THEN** that layout includes sink, recycler, recirculation, or equivalent recovery structures that prevent the test case from depending on permanent belt blockage as its steady state
