## ADDED Requirements

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
