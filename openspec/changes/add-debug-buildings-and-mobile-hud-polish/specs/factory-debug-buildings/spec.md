## ADDED Requirements

### Requirement: Debug source structures provide zero-cost site-compatible items
The game SHALL provide dedicated debug source structures for both world and interior factory sites, and those structures SHALL emit site-compatible test items without consuming upstream inputs, fuel, or placement-time resource cost.

#### Scenario: World debug source emits test cargo without setup chains
- **WHEN** the player places a world-site debug source structure from a build category in a supported demo
- **THEN** the structure begins producing its configured test item family without requiring mined input, crafted intermediates, or player-supplied fuel

#### Scenario: Interior debug module emits interior-compatible cargo
- **WHEN** the player places an interior debug source module inside a mobile factory editor session
- **THEN** the module outputs cargo that is compatible with the interior logistics and machine rules used by the mobile factory layout

### Requirement: Debug source set covers multiple common test item families
The game SHALL expose more than one debug source entry so developers can spawn representative raw-resource, processed-material, or combat/logistics test goods without opening a secondary configuration workflow.

#### Scenario: Build palette shows multiple debug source entries
- **WHEN** a build UI renders the debug structure entries for a supported site kind
- **THEN** it shows multiple clearly labeled debug source options instead of only one catch-all producer entry

### Requirement: Permanent test generators provide stable power without fuel
The game SHALL provide a dedicated permanent test generator for world and interior sites, and that generator SHALL continuously supply power without consuming fuel items or stalling due to empty internal inventory.

#### Scenario: World permanent test generator powers nearby structures immediately
- **WHEN** the player places the world-site permanent test generator on a valid world grid cell
- **THEN** nearby powered world structures can receive electricity from it without requiring a coal or fuel delivery chain

#### Scenario: Interior permanent test generator sustains editor test layouts
- **WHEN** the player places the interior permanent test generator inside a mobile factory layout
- **THEN** connected interior machines remain powered for as long as the structure stays placed, without needing manual refueling
