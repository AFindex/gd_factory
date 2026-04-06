## MODIFIED Requirements

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
