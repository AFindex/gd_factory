## ADDED Requirements

### Requirement: Mobile factory interiors provide a traversable maintenance space at player scale
The game SHALL define mobile-factory interiors as real maintenance spaces that a player character can occupy at normal character scale rather than as a micro-scale copy of the world factory floor.

#### Scenario: Player enters interior without character rescaling
- **WHEN** the player transitions from the world into a mobile-factory interior space
- **THEN** the character retains the same body scale used in the world and the interior remains navigable through authored maintenance-space geometry

### Requirement: Interior maintenance spaces separate human routes from cargo routes
The game SHALL organize mobile-factory interiors into at least a maintenance route layer for player navigation and a logistics layer for internal cargo movement so internal item scale can differ from world cargo scale without implying a miniature world.

#### Scenario: Player walks maintenance route while cargo uses embedded logistics layer
- **WHEN** the player observes or traverses an active mobile-factory interior
- **THEN** the player pathing occurs on maintenance walkways, platforms, or catwalks while cargo movement remains visually assigned to embedded rails, channels, ducts, or equivalent logistics paths

### Requirement: Interior modules present as embedded industrial equipment
The game SHALL present interior manufacturing, buffering, and transfer structures as embedded or modular interior equipment rather than as directly scaled-down world structures placed on an open floor.

#### Scenario: Interior machine uses modular presentation contract
- **WHEN** an interior-only machine is previewed or rendered inside a mobile-factory interior
- **THEN** its presentation emphasizes module housings, maintenance faces, access panels, or embedded interfaces instead of the world structure's exterior facility silhouette
