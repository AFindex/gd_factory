# factory-workspace-navigation Specification

## Purpose
TBD - created by archiving change reorganize-factory-workspace-ui. Update Purpose after archive.

## Requirements
### Requirement: Factory-facing demos expose a shared top workspace menu
The game SHALL provide a shared workspace navigation shell for factory-facing demos that shows a top menu of scene-appropriate workspaces and limits the main HUD body to the currently selected workspace instead of rendering every section at once.

#### Scenario: Opening a supported demo shows a workspace menu
- **WHEN** the player opens the static factory demo, focused mobile factory demo, or large mobile factory test scenario
- **THEN** the HUD shows a top workspace menu with the workspaces available for that scene and only the active workspace panel is expanded by default

### Requirement: Workspace switching preserves the active play context
The game SHALL allow the player to switch workspace panels without closing the current demo state, cancelling the current editor session, or discarding the currently selected tool unless the newly selected workspace explicitly replaces that tool.

#### Scenario: Switching workspaces keeps the current interaction context
- **WHEN** the player changes from one workspace menu entry to another while a demo scene is already running
- **THEN** the scene remains in the same world or editor state and the previously active control, selection, or opened play session stays intact until the player performs a new action inside the newly selected workspace
