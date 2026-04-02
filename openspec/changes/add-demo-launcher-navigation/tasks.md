## 1. Shared Navigation Infrastructure

- [x] 1.1 Add a shared demo catalog/helper that defines the launcher scene path plus the display metadata and target scene path for each launcher-managed demo.
- [x] 1.2 Add a reusable back-to-launcher UI component or helper that can be attached from both `Node3D`-rooted demos and the `Control`-rooted UI showcase.

## 2. Launcher Scene

- [x] 2.1 Create the launcher main scene and script under `scenes/` and `scripts/` so it renders the available demo entries with titles, descriptions, and launch buttons from the shared catalog.
- [x] 2.2 Wire launcher actions to change into `factory_demo`, `mobile_factory_demo`, `mobile_factory_test_scenario`, and `ui_showcase`.
- [x] 2.3 Update `project.godot` so the launcher scene becomes the project's startup main scene.

## 3. Demo Return Integration

- [x] 3.1 Integrate the reusable return-to-launcher affordance into `FactoryDemo` without breaking existing HUD or input behavior.
- [x] 3.2 Integrate the reusable return-to-launcher affordance into `MobileFactoryDemo` so both the focused mobile demo and the large test scenario can navigate back to the launcher.
- [x] 3.3 Integrate the reusable return-to-launcher affordance into `UiShowcase` and ensure it stays visible without disrupting the showcase layout.

## 4. Verification

- [x] 4.1 Verify that normal project startup opens the launcher and that every launcher entry loads the expected target scene.
- [x] 4.2 Verify that each launcher-managed demo can return to the launcher through the visible in-scene action and still remains directly runnable from the editor.
