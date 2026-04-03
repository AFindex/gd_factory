# factory-blueprint-workflow Specification

## Purpose
Define the shared blueprint capture, library, validation, and application workflow used across factory-building surfaces.

## Requirements
### Requirement: Blueprints capture reusable relative factory layouts
The game SHALL let the player capture a live layout into a reusable blueprint record that stores structure kinds, relative cell offsets, facing, and stable structure configuration needed to recreate the layout.

#### Scenario: Saving a world selection creates a normalized blueprint
- **WHEN** the player selects an occupied area in the static factory sandbox and saves it as a blueprint
- **THEN** the game creates a blueprint record whose entries are normalized to a local origin instead of storing absolute world cells

#### Scenario: Saving a mobile interior layout preserves attachment configuration
- **WHEN** the player saves a mobile factory interior layout that includes boundary attachments and recipe-capable structures
- **THEN** the blueprint record preserves attachment placement requirements and stable recipe configuration needed to recreate that interior layout

### Requirement: Blueprints live in a reusable library separate from the active placement cursor
The game SHALL store captured blueprints in a reusable library UI so the player can review, select, and apply saved layouts without losing the saved record itself.

#### Scenario: Saved blueprint appears in the library
- **WHEN** the player confirms the name for a newly captured blueprint
- **THEN** the blueprint appears in the library with its display name, site type, and layout summary

#### Scenario: Library selection prepares apply mode
- **WHEN** the player selects a saved blueprint from the library
- **THEN** the game keeps that blueprint in the library and also marks it as the active blueprint for preview and application

### Requirement: Blueprint application validates site compatibility before mutation
The game SHALL generate a validation result for the selected target site before mutating any structures so incompatible blueprint applications are blocked cleanly.

#### Scenario: Valid blueprint application recreates the layout
- **WHEN** the player previews a saved blueprint at a compatible target and confirms application after validation succeeds
- **THEN** the game instantiates the full translated layout in the destination site using the blueprint's stored relative structure data

#### Scenario: Invalid blueprint application is blocked without partial mutation
- **WHEN** the selected target site is incompatible because of blocked cells, out-of-bounds cells, or missing required attachment mounts
- **THEN** the game refuses to apply the blueprint, reports the blocking reason in the blueprint workflow UI, and leaves the destination site unchanged

### Requirement: Blueprints exclude transient simulation state
The game SHALL preserve layout-defining configuration in blueprints while excluding transient simulation state that belongs only to the current runtime.

#### Scenario: Runtime buffers are not copied into a blueprint
- **WHEN** the player saves a blueprint from a scene that currently contains buffered items, turret ammo, or delivery counters in flight
- **THEN** the saved blueprint omits those transient runtime values instead of reproducing them on future application

#### Scenario: Stable recipe configuration is preserved
- **WHEN** the player saves a blueprint that contains recipe-capable structures with non-default recipe selections
- **THEN** the saved blueprint keeps those recipe selections so the recreated layout uses the same structural configuration after application
