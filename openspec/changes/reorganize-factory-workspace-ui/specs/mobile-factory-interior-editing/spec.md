## ADDED Requirements

### Requirement: Interior editor top menu exposes contextual workspaces
The game SHALL add a top workspace menu to the mobile factory interior editor so blueprint actions and mobile factory detail content can be opened from the editor chrome without replacing the split-view editing surface.

#### Scenario: Selecting blueprint workspace keeps split-view editing available
- **WHEN** the player opens the interior editor and switches to the blueprint workspace from the top menu
- **THEN** the editor keeps the split-view world-plus-interior layout active while showing blueprint actions in the selected workspace panel

#### Scenario: Selecting factory detail workspace keeps the editor session active
- **WHEN** the player opens the interior editor and switches to the factory detail workspace from the top menu
- **THEN** the editor shows the current mobile factory detail content without closing the interior viewport or forcing the player out of the current edit session
