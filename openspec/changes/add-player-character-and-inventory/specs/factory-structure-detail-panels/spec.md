## ADDED Requirements

### Requirement: Player information uses standalone draggable windows
The game SHALL expose the player's backpack, item-information, and personal-stat views as standalone draggable windows alongside structure detail windows.

#### Scenario: Backpack window opens without replacing structure details
- **WHEN** the player already has a structure detail window open and then opens the backpack panel
- **THEN** the backpack appears as its own standalone window and the structure detail window remains available

#### Scenario: Player stat window can be arranged independently
- **WHEN** the player drags the personal-stat window after opening it
- **THEN** that window moves independently and remains clamped inside the visible viewport like other detail windows

### Requirement: Item-information panels reflect the current selected item
The game SHALL provide a dedicated item-information panel that reports details for the currently selected or hovered item from the player inventory or another open slot grid.

#### Scenario: Selecting an inventory item opens its info panel
- **WHEN** the player selects or activates an occupied slot in the backpack or hotbar
- **THEN** the item-information panel shows that item's icon, name, stack information, and descriptive text

#### Scenario: Clearing the selection clears the item details
- **WHEN** the player closes the info panel or clears the current item selection
- **THEN** the item-information panel no longer shows stale details from a previously selected slot
