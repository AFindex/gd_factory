## Context

The project currently starts in `res://scenes/factory_demo.tscn`, even though the repository now includes multiple runnable slices: the static factory demo, the focused mobile-factory demo, the large mobile-factory test scenario, and the UI showcase. Scene discovery therefore depends on opening files manually in the editor, and there is no in-game way to move between demos.

The current scene layout also matters for implementation strategy. `factory_demo.tscn`, `mobile_factory_demo.tscn`, and `mobile_factory_test_scenario.tscn` are thin scene files whose behavior and HUD trees are composed from C# root scripts at runtime. `ui_showcase.tscn` is a `Control` scene that also builds most of its interface in code. That means launcher navigation should favor reusable code-driven UI and scene routing helpers instead of large manual scene rewrites.

## Goals / Non-Goals

**Goals:**
- Add a dedicated launcher scene that becomes the startup entry point for the project.
- Present the existing demos from one hub with clear labels and short descriptions.
- Provide a consistent, visible path back to the launcher from every launcher-managed demo scene.
- Keep each demo scene directly runnable in the editor without depending on the launcher first.
- Centralize demo metadata so launcher labels and target scene paths do not drift across multiple files.

**Non-Goals:**
- Redesigning the content or controls of the existing demo scenes beyond the minimal navigation affordance.
- Preserving per-demo runtime state when returning to the launcher; scenes may reload from their authored startup state.
- Building a save system, async loading screen, or thumbnail-heavy content browser in this change.
- Refactoring the factory demo, mobile demo, and UI showcase onto a shared gameplay base class.

## Decisions

### Use a dedicated launcher `Control` scene as the new project entry point

Add a standalone launcher scene under `scenes/` and set `project.godot` to boot into it. The launcher should be a lightweight UI-first screen that lists the supported demos and routes into them with `SceneTree.ChangeSceneToFile`.

This keeps the startup contract easy to understand: the project always opens at a hub, and the hub owns cross-demo navigation. A `Control` root is the best fit because the launcher is primarily layout, copy, and buttons rather than 3D simulation.

Alternative considered: keeping `factory_demo` as the main scene and opening other demos from an overlay menu. This was rejected because the launcher should be the neutral top-level entry point rather than a feature embedded inside one specific demo.

### Define demo entries in a shared catalog instead of scattering scene paths across UI code

Introduce a small shared catalog or helper type that provides launcher-managed demo entries, including a stable identifier, display title, supporting description, and target scene path. The launcher reads from this catalog to build its entry list, and the return-navigation helper uses the same launcher scene path constant for backwards navigation.

This avoids duplicating string literals such as `res://scenes/mobile_factory_demo.tscn` across several scripts and reduces the risk that a scene gets renamed in one place but not another. It also makes future launcher expansion a small, localized edit.

Alternative considered: hard-coding one button per scene directly in the launcher script. This was rejected because button definitions would then become the routing source of truth, which scales poorly as demo count grows.

### Implement return navigation as a reusable overlay component that demos attach locally

Add a small reusable navigation UI component, such as a `CanvasLayer` or `Control` helper, that renders a compact "Back to Launcher" button near the top edge of the viewport. Each launcher-managed demo scene attaches this component during setup instead of reimplementing scene-switching buttons separately.

This approach matches the current project structure: the demo scenes already assemble their runtime UI in code, so adding one reusable overlay is lower-risk than introducing a new global singleton. It also lets the button stay visually consistent across Node3D-rooted scenes and the `Control`-rooted UI showcase.

Alternative considered: using only a keyboard shortcut like `Esc` to return to the launcher. This was rejected because the user explicitly asked for each interface to be able to return to the launcher, and a visible button is easier to discover and verify.

Alternative considered: a global autoload that injects navigation into every scene automatically. This was rejected for the first pass because it adds lifecycle coupling across the whole project, including scenes that may never belong to the launcher flow.

### Keep direct scene execution supported and treat launcher navigation as additive

The launcher should manage project startup and in-game navigation, but each demo scene must still load correctly when opened directly from the editor. The return overlay should therefore depend only on the shared launcher path helper, not on transient state passed from the launcher scene.

This preserves the current development workflow for targeted scene iteration while still giving normal startup and manual playtesting a cleaner route through the launcher.

Alternative considered: requiring scenes to be opened only through launcher-injected state. This was rejected because it would complicate iteration and violate the proposal's requirement that scenes remain independently runnable.

### Update the factory startup requirement without changing the demo content contract

The existing `factory-production-demo` capability currently owns the expectation that the project boots into a playable factory slice. That requirement should be updated so startup first reaches the launcher and the launcher exposes the static factory demo as a selectable experience. The rest of the factory demo content contract remains intact: once entered, it still needs to boot into the same playable automation slice.

This keeps the spec surface clean. Cross-demo navigation behavior belongs in the new launcher capability, while the existing factory-production capability only changes where the startup flow begins.

Alternative considered: moving all startup and launcher concerns into the factory spec. This was rejected because launcher navigation is broader than one demo and should not make the factory capability the owner of global scene routing.

## Risks / Trade-offs

- [The launcher catalog can drift from actual scene files if future demos are added without updating it] -> Mitigation: keep the catalog as the single source of truth and include smoke coverage for every listed scene path.
- [A top-edge back button could overlap existing HUD content in the factory or mobile demos] -> Mitigation: keep the overlay compact, anchor it consistently, and allow individual demos to offset it slightly if their own HUD already occupies that corner.
- [Returning to the launcher will reset demo state because scenes are reloaded] -> Mitigation: accept full scene reload as the initial behavior and document state preservation as out of scope for this change.
- [Node3D and Control demo roots have different UI composition patterns] -> Mitigation: implement the navigation affordance as a reusable overlay that can be attached from either root type without assuming a specific gameplay base class.

## Migration Plan

1. Add the shared launcher catalog/helper and create the launcher scene/script that renders the available demo entries.
2. Change `project.godot` so the launcher becomes the new startup main scene.
3. Add the reusable back-to-launcher overlay and integrate it into `FactoryDemo`, `MobileFactoryDemo`/`MobileFactoryTestScenario`, and `UiShowcase`.
4. Validate that each launcher entry opens the expected scene and that each scene can return to the launcher without editor intervention.
5. If rollback is needed, restore `run/main_scene` to the previous demo scene and remove launcher-only scene switching hooks while leaving the individual demo scenes independently runnable.

## Open Questions

- Should the launcher expose the UI showcase as a full demo card alongside gameplay demos, or should it be visually separated as a tooling/sample scene while still remaining launchable?
- Does the team want a secondary keyboard shortcut for returning to the launcher in addition to the visible button, or is the visible button alone sufficient for the first pass?
