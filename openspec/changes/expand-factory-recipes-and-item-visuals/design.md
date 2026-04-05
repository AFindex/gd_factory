## Context

The powered sandbox now has the right structural pieces for a factory game, but its authored content and item presentation are still heavily prototype-shaped. `FactoryItemKind` only covers a very small economy, `FactoryRecipeCatalog` is still a compact in-code list with short chains, `FactoryResourceCatalog` only exposes coal and iron, and `FlowTransportStructure` renders every moving payload as the same yellow box regardless of item type.

This change is cross-cutting across data definitions, authored sandbox content, transport rendering, and smoke coverage. It also has two constraints we should preserve:
- deterministic fixed-step logistics must stay independent from presentation choices;
- the first pass should improve readability immediately even before the project has a full custom art set.

## Goals / Non-Goals

**Goals:**
- Expand the factory economy into a more Factorio-inspired starter ladder with additional raw resources, intermediates, and branching multi-step recipes.
- Move recipe and item balance toward declarative configuration so adding future content does not require bespoke logic in each structure class.
- Introduce item visual profiles that can choose among tinted placeholders, textured meshes, authored 3D models, and billboarded 2D sprites for moving logistics items.
- Give the current item set an immediate color-distinction pass so belt readability improves even before richer assets are authored.
- Update the default sandbox layout and smoke expectations so the richer chain and new item visuals are exercised in normal play.

**Non-Goals:**
- Reproducing the full Factorio content stack, including fluids, research, trains, split belt lanes, or deep tech progression.
- Building a general-purpose asset import pipeline or editor tooling for external content packs in this change.
- Replacing inventory/detail-panel rendering with the same 3D presentation system; this pass focuses on world and transport visuals.
- Solving large-scale rendering optimization beyond what is needed for the current sandbox item counts.

## Decisions

### 1. Separate simulation item identity from presentation profile

Add a shared item-definition layer that keeps gameplay identity and visuals related but distinct. Each item kind should resolve to a definition that includes display metadata, category/balance metadata, and a presentation profile reference. The simulation continues to move `FactoryItem` instances by kind and id, while transport visuals resolve a profile from the item kind at render-time.

This is preferred over storing mesh and texture choices directly on runtime `FactoryItem` objects because visuals are configuration data, not per-instance state. It also keeps deterministic transfer, save-ish future serialization, and smoke assertions stable if a profile changes.

Alternative considered: keep the current enum-only item model and add one large switch statement inside `FlowTransportStructure`. Rejected because it scales poorly once several new items, models, and fallbacks are introduced.

### 2. Expand the starter economy through declarative content tables

Restructure the item, resource, and recipe catalogs so they express a broader authored chain instead of a few hard-coded outputs. The first content pack should stay intentionally bounded but meaningful:
- resources: coal, iron ore, copper ore;
- refined products: iron plate, copper plate, steel plate;
- intermediates: gear, copper wire, circuit board;
- finals: machine part, ammo magazine, high-velocity ammo.

The intended authored chain is:
- coal -> generator fuel, plus optional upgraded-ammo ingredient;
- iron ore -> iron plate -> gear and steel plate;
- copper ore -> copper plate -> copper wire;
- iron plate + copper wire -> circuit board;
- gear + steel plate + circuit board -> machine part;
- iron plate + copper wire -> ammo magazine;
- ammo magazine + steel plate + coal -> high-velocity ammo.

This is preferred over a larger first-wave content dump because it gives the project clear branch points, multiple resource families, and better balance knobs while remaining small enough to author and debug in one sandbox.

Alternative considered: add many more resources and machine classes immediately. Rejected because the simulation and authored-layout complexity is already significant once transport visuals become item-aware.

### 3. Replace transport-item meshes with a visual factory that can return model or billboard nodes

Refactor moving-payload visuals from a hard-coded `MeshInstance3D` cube into a transport-visual factory that returns a `Node3D` root. `TransitItemState.Visual` should become a node root instead of a single mesh so one item can render as:
- a simple tinted placeholder mesh;
- a textured 3D mesh;
- an authored model scene or mesh subtree;
- a billboarded `Sprite3D`/quad fallback using a 2D image.

The factory should follow a deterministic fallback order:
1. use configured 3D model when present and allowed;
2. otherwise use configured textured mesh;
3. otherwise use configured billboard sprite;
4. otherwise use a tinted placeholder shape.

This is preferred over adding billboard support as a one-off flag on the existing cube because the user request explicitly needs multiple representation modes per item, not just color changes.

Alternative considered: instantiate separate transport-structure subclasses for 2D and 3D item rendering. Rejected because rendering mode is an item concern, not a belt concern, and belts already support mixed payload kinds.

### 4. Use billboard sprites as the low-cost art fallback for item-specific silhouettes

For items without a full 3D model, use a billboarded `Sprite3D` or equivalent unshaded textured quad so 2D art can stand in for a model on belts and attachment transport paths. The billboard root still inherits the same path interpolation and item height as 3D payloads, so movement stays consistent across all visual modes.

This is preferred over requiring every item to ship with a 3D mesh before the feature is usable. It also gives the project a much faster content-authoring path for future items.

Alternative considered: use only colored primitive meshes until real models exist. Rejected because it does not satisfy the requirement to support per-item textures and 2D image placeholders.

### 5. Author the demo around one clear branching showcase instead of scattering new items everywhere

Update the static sandbox so one visible district demonstrates the expanded content ladder end-to-end, with separate iron and copper ingress converging into shared assembly outputs. Existing defense and regression lanes can keep their role, but at least one authored line should visibly show:
- different raw materials entering the factory;
- intermediate transformation across multiple machines;
- a final item whose prerequisites come from more than one branch;
- moving belt items that are immediately distinguishable by color and, where configured, by sprite/model silhouette.

This is preferred over lightly sprinkling new recipes across unrelated lanes because the user asked for explicit crafting paths and better visual readability. One strong showcase provides a better implementation target and smoke surface.

Alternative considered: keep the current authored chain and only expand the catalogs for later use. Rejected because unused content would not validate the visuals or the balance configuration.

## Risks / Trade-offs

- [More item definitions and recipe paths increase balance complexity] -> Keep the first content pack intentionally small and centralize cycle/power numbers in shared definitions rather than structure logic.
- [Node-based transport visuals can increase scene-tree cost] -> Prefer lightweight roots, reuse materials/textures where possible, and keep the placeholder fallback cheap.
- [Billboard sprites can look flat or hard to read at some camera angles] -> Use clear silhouettes, unshaded materials, and keep billboard usage optional per item so higher-value items can use 3D models later.
- [Refactoring `TransitItemState.Visual` from mesh to node touches several transport classes] -> Limit movement/path logic changes to the shared transport base and keep structure-specific path evaluation unchanged.
- [A richer starter chain can make the authored map harder to read] -> Group the new branches into a single labeled district with consistent color language and preserve open spacing between each stage.

## Migration Plan

1. Add shared item definitions, expanded enums/catalog entries, and the initial recipe/resource balance pack for iron, copper, coal, intermediates, and upgraded outputs.
2. Introduce item visual profile definitions plus a transport-item visual factory, then refactor shared transport code to host node-based payload visuals.
3. Apply the first-pass color differentiation to all existing items, then add per-item texture/model/billboard config for the newly expanded content set.
4. Re-author the relevant `factory_demo` starter lines and any recipe/detail summaries so the expanded chain is visible and inspectable.
5. Extend smoke coverage to verify both the richer production path and at least one differentiated transport-visual path.

Rollback is straightforward because the simulation and visuals remain decoupled: the old placeholder visuals can be restored while leaving the expanded item and recipe catalogs dormant if integration becomes unstable.

## Open Questions

- Should the first implementation load item models from reusable mesh resources, packed scenes, or a small code-built mesh library?
- Do we want the billboard fallback to be selectable automatically when a model is missing, or explicitly controlled per item profile even when a model exists?
- Should the initial authored showcase end in machine parts, upgraded ammo, or both if the larger chain still fits cleanly on the current sandbox map?
