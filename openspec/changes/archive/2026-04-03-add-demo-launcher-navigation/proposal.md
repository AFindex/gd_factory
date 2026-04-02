## Why

The project now contains multiple useful demo scenes, but it still boots straight into `factory_demo` and leaves scene discovery to the editor or manual file selection. We need a launcher scene so players and developers can enter the right demo from one consistent starting point and always return to that hub without restarting the project.

## What Changes

- Add a dedicated launcher main scene that becomes the project's startup entry point and presents the available demo scenes in one screen.
- Add launcher actions for the existing demo scenes so the player can open `factory_demo`, `mobile_factory_demo`, `mobile_factory_test_scenario`, and `ui_showcase` from the hub.
- Add a shared in-demo return affordance so every launched demo can navigate back to the launcher scene without using the editor or closing the game.
- Standardize the routing metadata needed to label demo entries and resolve their target scene paths from a single source of truth.
- Preserve direct scene loading in the editor so each demo remains independently runnable while also participating in the launcher flow.

## Capabilities

### New Capabilities
- `demo-launcher-navigation`: A startup launcher scene that lists available demos, opens the selected demo, and provides a consistent return path back to the launcher.

### Modified Capabilities
- `factory-production-demo`: Project startup changes from loading the static factory demo directly to loading the launcher while keeping the factory demo available as one of the selectable experiences.

## Impact

- Affected systems include `project.godot`, the scene set under `scenes/`, and UI/navigation scripts that currently assume a single default entry scene.
- The change will likely add a launcher scene plus reusable navigation UI or helper scripts that demo scenes can embed for returning to the hub.
- Existing demo scene scripts and/or HUD layers will need light integration so each scene exposes a visible back-to-launcher action without disrupting current controls.
