## ADDED Requirements

### Requirement: Factory demos spawn a controllable player character
The game SHALL spawn a controllable player character as the default interaction anchor in both the static factory sandbox and the focused mobile-factory demo, using a simple capsule-bodied placeholder until an authored avatar replaces it.

#### Scenario: Static sandbox starts with a player avatar
- **WHEN** the player opens the static factory demo from the launcher
- **THEN** the scene includes a controllable player character on the world floor and routes default movement input through that character instead of treating the camera as the only interaction anchor

#### Scenario: Focused mobile demo starts with a player avatar
- **WHEN** the player opens the focused mobile-factory demo scene
- **THEN** the world includes the same controllable player character and the initial interaction state is anchored to that character rather than immediately commanding the mobile factory

### Requirement: Player HUD exposes a hotbar and independent personal panels
The game SHALL provide the player with a bottom hotbar plus independent backpack, item-information, and personal-stat panels that can be opened, closed, and arranged without collapsing into one monolithic HUD page.

#### Scenario: Bottom hotbar reflects the active quick slot
- **WHEN** the player switches the active quick slot by keyboard or clicking the hotbar
- **THEN** the bottom HUD highlights the selected slot and shows the current item icon and stack count for that slot

#### Scenario: Personal panels stay independent
- **WHEN** the player opens the backpack panel and then opens the item-information or personal-stat panel
- **THEN** each panel appears as its own standalone window and opening one panel does not forcibly close the others

### Requirement: Player inventory supports shared slot-based item handling
The game SHALL represent the player's hotbar and backpack as slot-based inventories that follow the same deterministic stack, merge, split, and move rules used by factory containers.

#### Scenario: Backpack stack behavior matches other inventories
- **WHEN** the player receives more items of a kind that already exists in a non-full backpack stack
- **THEN** the player's inventory merges into the compatible stack before consuming another empty slot

#### Scenario: Empty player slots do not start drag actions
- **WHEN** the player presses on an empty hotbar or backpack slot
- **THEN** the UI does not enter an item-drag state and no transfer request is emitted

### Requirement: Placeable structures exist as inventory items
The game SHALL represent buildable structures as placeable inventory items with their own icon, display name, stack rules, and mapping to the world placement prototype they produce.

#### Scenario: Structure item shows placeable metadata in inventory
- **WHEN** the player inspects a buildable structure item in the hotbar or backpack
- **THEN** the slot shows that item's configured icon, display name, stack count, and enough metadata for the game to resolve which structure prototype it places

#### Scenario: Selected structure item arms world placement
- **WHEN** the player selects a hotbar slot containing a placeable structure item
- **THEN** the game treats left-click on a valid build cell as a placement attempt for that structure instead of as a pure inspection click
