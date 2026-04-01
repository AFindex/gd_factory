## ADDED Requirements

### Requirement: Large-scale mobile factory test scenario ships as a separate scene
The game SHALL provide a separate large-scale mobile factory test scenario scene for regression and observation without replacing the focused mobile factory demo scene.

#### Scenario: Large scenario can be opened independently
- **WHEN** the project content is inspected after the change is implemented
- **THEN** a dedicated large-scale mobile factory test scenario exists as its own scene entry point alongside the focused mobile factory demo

### Requirement: Large scenario starts with mixed mobile factory activity
The large-scale mobile factory test scenario SHALL load with multiple mobile factories already present in mixed lifecycle states and with varied size profiles.

#### Scenario: Deployed and moving factories coexist on load
- **WHEN** the large-scale mobile factory test scenario finishes loading
- **THEN** the map contains multiple mobile factories, with at least one already deployed into the world, at least one still in transit, and the participating factories covering more than one footprint size

### Requirement: Scenario interiors cover diverse logistics case studies
The large-scale mobile factory test scenario SHALL assign distinct pre-authored interior layouts to its mobile factories so the scene exercises varied conveyor and transfer topologies.

#### Scenario: Factories showcase different topology categories
- **WHEN** a tester inspects the interior layouts of the mobile factories included in the large-scale scenario
- **THEN** the set of layouts includes multiple named logistics patterns such as branching, merging, recirculation, relay transfer, or other non-trivial topology cases instead of repeating one simple belt line

### Requirement: Scenario supports long unattended operation
The large-scale mobile factory test scenario SHALL provide sink, recycler, or equivalent recovery paths so its active logistics loops can continue running without permanent belt blockage during extended observation.

#### Scenario: Recovery paths prevent permanent belt lockup
- **WHEN** the large-scale mobile factory test scenario runs unattended for an extended period
- **THEN** the authored interior and world loops continue to consume, recycle, or discharge produced items in a way that prevents permanent congestion from halting every active conveyor route

### Requirement: Scenario includes a player-controlled factory alongside autonomous actors
The large-scale mobile factory test scenario SHALL keep one mobile factory available for direct player control while background mobile factories continue executing their own authored behavior.

#### Scenario: Player control coexists with background activity
- **WHEN** the player drives, deploys, recalls, or edits the designated player-controlled mobile factory in the large-scale scenario
- **THEN** the other scenario mobile factories continue to move, stay deployed, or run their logistics loops according to their authored roles instead of pausing with the player's actions
