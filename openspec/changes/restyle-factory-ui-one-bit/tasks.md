## 1. Shared One Bit Theme Foundation

- [x] 1.1 Add a shared UI style helper for one bit tokens, including monochrome palette values, panel/button/input/slot style factories, spacing constants, and low-radius or zero-radius defaults.
- [x] 1.2 Refactor reusable chrome components such as `FactoryWorkspaceChrome`, `FactoryBlueprintPanel`, `FactoryStructureDetailWindow`, and `FactoryPlayerHud` to consume the shared one bit styles instead of maintaining their own rounded colorful `StyleBoxFlat` definitions.

## 2. Runtime HUD Restyle

- [x] 2.1 Update `FactoryHud` to use the shared one bit panel, workspace, button, and status treatments while preserving build selection, inspection, blueprint, telemetry, combat, and testing workflows.
- [x] 2.2 Update `MobileFactoryHud` and `MobileFactoryHud.Workspaces` to use the shared one bit theme for top chrome, info/editor panels, workspace bodies, and mode/status cues without changing command-mode, deploy, or editor interaction behavior.
- [x] 2.3 Replace color-dependent runtime mode/readout emphasis with one bit-safe cues such as inversion, stronger borders, and explicit text prefixes where needed.

## 3. Launcher And Showcase Restyle

- [x] 3.1 Restyle `DemoLauncher` and `DemoNavigation` to match the shared one bit visual language while keeping the same scene entry points and navigation flow.
- [x] 3.2 Convert `UiShowcase` to the shared one bit theme, removing the current colorful visual system as the default presentation and using the scene as a validation surface for common control states.

## 4. Verification And Cleanup

- [ ] 4.1 Run targeted verification for launcher navigation, static factory HUD workspace switching, mobile factory HUD/editor visibility, player inventory panels, structure detail windows, and blueprint actions to confirm business logic is unchanged after the visual refresh.
- [x] 4.2 Sweep remaining duplicated rounded/color-heavy style definitions across the touched UI files and align them with the shared one bit helper so the final theme stays consistent.
