## ADDED Requirements

### Requirement: Static sandbox layouts can be captured as blueprints
The game SHALL let the player capture a selected occupied area in the static factory sandbox and save it as a blueprint from the in-game blueprint workflow UI.

#### Scenario: Saving a selected sandbox area creates a blueprint entry
- **WHEN** the player enters blueprint capture mode in the static sandbox, selects an occupied area, and confirms save
- **THEN** the sandbox creates a named blueprint entry from the structures inside that selection without removing the existing layout from the scene

### Requirement: Static sandbox previews and validates blueprint application
The game SHALL preview the translated footprint of the active blueprint on the world grid and only allow application when every required placement validates for that target anchor.

#### Scenario: Valid sandbox target shows a full blueprint preview
- **WHEN** the player selects a saved world-grid blueprint and hovers a valid target anchor in the static sandbox
- **THEN** the sandbox shows a multi-structure blueprint preview at the translated target cells and allows the player to confirm application

#### Scenario: Blocked sandbox target cannot be confirmed
- **WHEN** the player previews a saved world-grid blueprint over occupied or out-of-bounds cells
- **THEN** the sandbox marks the blocked cells as invalid, explains that the blueprint cannot be applied there, and prevents confirmation until the preview becomes valid
