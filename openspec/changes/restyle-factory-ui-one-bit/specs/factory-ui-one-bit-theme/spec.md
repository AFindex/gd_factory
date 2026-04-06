## ADDED Requirements

### Requirement: Core UI surfaces use a shared one bit visual language
The game SHALL render its core UI surfaces with a shared one bit visual language that uses monochrome or near-monochrome contrast, square or near-square panel geometry, thin borders, and consistent spacing rather than scene-specific rounded, colorful chrome.

#### Scenario: Opening launcher and runtime HUDs shows consistent one bit chrome
- **WHEN** the player opens the demo launcher, the static factory demo, or the mobile factory demo
- **THEN** the visible launcher chrome, workspace navigation, HUD panels, and major action controls share the same one bit visual baseline instead of presenting unrelated color-heavy or highly rounded styles

### Requirement: Interactive UI states remain distinguishable without relying on multicolor accents
The game SHALL express hover, selected, active, disabled, and major mode states through inversion, border treatment, contrast changes, and explicit text cues so that core actions remain readable in a one bit presentation.

#### Scenario: Selecting and focusing controls produces a clear monochrome state change
- **WHEN** the player focuses or selects a workspace tab, build button, hotbar slot, blueprint action, or inventory slot
- **THEN** the control changes appearance through one bit state treatment such as inverted fill, stronger border, or explicit selection text without requiring multicolor accent coding to understand the state

#### Scenario: Runtime control modes remain readable after the theme shift
- **WHEN** the current scene enters a major mode such as player control, build mode, observer mode, factory command mode, or deploy preview
- **THEN** the HUD communicates that mode with explicit text and one bit emphasis that remains legible against the shared monochrome theme

### Requirement: Theme restyling preserves existing UI workflows and event routing
The game SHALL keep existing business logic, UI event routing, and scene workflows intact while applying the one bit restyle so that visual changes do not alter how the player navigates, inspects, or manipulates factory systems.

#### Scenario: Existing HUD workflows continue to function after restyling
- **WHEN** the player switches workspaces, opens a structure detail window, drags inventory stacks, uses blueprint commands, or toggles player inventory panels after the one bit theme is applied
- **THEN** the same workflows remain reachable and behave the same as before, with only visual presentation and non-functional layout polish changed

#### Scenario: Launcher navigation remains behaviorally unchanged
- **WHEN** the player uses the launcher to enter the available demo scenes after the restyle
- **THEN** the launcher still exposes the same scene entry points and navigation outcomes even though its presentation has been converted to the shared one bit theme

### Requirement: UI showcase acts as a one bit reference surface
The game SHALL expose the one bit theme in the UI showcase scene so developers can validate how common controls, panels, lists, logs, tabs, and dialogs look under the shared visual system before or alongside runtime HUD usage.

#### Scenario: Opening the showcase validates the shared one bit tokens
- **WHEN** the developer opens `UiShowcase`
- **THEN** the scene presents its controls with the shared one bit panel, button, text, and state styling instead of a separate colorful visual system
