## ADDED Requirements

### Requirement: Interior interaction mode opens independent structure detail windows
The game SHALL let the player inspect internal structures from the mobile factory editor by opening an independent detail window while the editor remains in interaction mode.

#### Scenario: Left click opens details during interaction mode
- **WHEN** the player is in the mobile factory interior editor interaction mode and left-clicks an existing internal structure
- **THEN** the editor opens or focuses that structure's detail window and does not switch to build placement

#### Scenario: Build mode still prioritizes placement actions
- **WHEN** the player has selected an interior build tool and left-clicks inside the mobile factory editor
- **THEN** the editor continues to treat the click as a build interaction and does not open a structure detail window
