# factory-runtime-save-snapshots Specification

## Purpose
Define the runtime save/load behavior for active factory sessions, including world and interior site reconstruction, runtime participant restoration, and dedicated save-slot persistence.

## Requirements

### Requirement: Runtime save snapshots capture every active factory site
The system SHALL persist a runtime save snapshot document that declares an explicit schema version, save metadata, and one site snapshot for each active factory site participating in the current session.

#### Scenario: Static sandbox save captures the current world site
- **WHEN** the player saves progress from the static factory sandbox
- **THEN** the save document contains a world-site snapshot whose embedded map document is sufficient to rebuild the current saved layout instead of only referencing the original authored source path

#### Scenario: Mobile factory save captures both world and interior sites
- **WHEN** the player saves progress from the focused mobile-factory demo while an interior site is active
- **THEN** the save document contains separate site snapshots for the world site and the mobile interior site so both can be reconstructed during load

### Requirement: Runtime save snapshots preserve logistics and structure runtime state
The system SHALL persist the runtime state needed to resume structure behavior after site reconstruction, including structure health, structure-owned inventory contents, selected recipes and production progress where applicable, and transit items with their source, target, and travel progress.

#### Scenario: Belt items resume from saved in-transit positions
- **WHEN** a save is created while one or more transport structures contain items in flight
- **THEN** loading that save restores those items onto the corresponding reconstructed transport structures with equivalent routing targets and progress instead of dropping them back into source inventories

#### Scenario: Structure internals resume from saved inventories and progress
- **WHEN** a save is created while a structure has stored items, a non-default recipe, damage, or partially completed processing state
- **THEN** loading that save restores the structure's internal runtime state so its gameplay behavior continues from the saved point on the next simulation tick

### Requirement: Runtime save snapshots preserve player and combat progression
The system SHALL persist the player's transform and inventory state, all active hostile actors, and the combat director state required for future enemy spawns to continue from the saved point in time.

#### Scenario: Player resumes from the saved position and hotbar state
- **WHEN** the player loads a previously saved snapshot
- **THEN** the player character appears at the saved location with the saved backpack contents, active hotbar selection, and placement-armed state

#### Scenario: Hostiles and spawn cadence resume from the saved combat state
- **WHEN** the player loads a snapshot captured during an active attack
- **THEN** the restored session contains the saved living enemies with their saved health and movement progress, and future lane spawns continue using the saved spawn index and countdown state

### Requirement: Snapshot loading validates before mutating the live session
The system SHALL validate snapshot versioning, required site data, and runtime participant records before replacing the current playable session or partially hydrating restored objects.

#### Scenario: Unsupported save version is rejected before world teardown
- **WHEN** the runtime encounters a save snapshot whose schema version is unknown or unsupported
- **THEN** loading fails with a visible validation error and the current active session remains unchanged

#### Scenario: Missing runtime target aborts hydration
- **WHEN** a save snapshot refers to a structure or participant that cannot be matched after site reconstruction
- **THEN** loading fails before partial runtime state is applied so the player is not left in a partially restored world

### Requirement: Runtime saves use dedicated save-slot persistence
The system SHALL store save snapshots in dedicated runtime save-slot storage that is separate from authored map sources and blueprint persistence, and SHALL expose clear success or failure feedback for save/load operations.

#### Scenario: Saving progress writes a named runtime slot
- **WHEN** the player saves progress with a chosen slot name
- **THEN** the runtime writes the snapshot and slot metadata under the runtime save directory without overwriting `res://` map source files

#### Scenario: Save or load failure is surfaced to the player
- **WHEN** writing or loading a runtime save fails because of validation, IO, or serialization errors
- **THEN** the HUD exposes a clear failure message that identifies the save action as failed instead of silently falling back to map export behavior
