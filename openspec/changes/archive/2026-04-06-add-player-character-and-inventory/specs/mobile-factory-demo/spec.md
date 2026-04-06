## ADDED Requirements

### Requirement: Focused mobile demo starts from a player-anchored world view
The game SHALL present the focused mobile-factory demo as a player-anchored experience first, with the mobile factory remaining an important interactive system rather than the only initial control target.

#### Scenario: Mobile demo loads with player HUD and avatar
- **WHEN** the player opens the focused mobile-factory demo scene directly
- **THEN** the scene loads the player avatar, hotbar, and player-facing panels needed to begin interacting from the character perspective while keeping the mobile-factory systems available

#### Scenario: Returning from factory interaction restores player-first flow
- **WHEN** the player finishes inspecting or commanding the mobile factory and returns to the default world interaction state
- **THEN** the demo continues from the player-controlled viewpoint without restarting the scene or discarding the current mobile-factory state
