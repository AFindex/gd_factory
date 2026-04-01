## Context

The current mobile factory runtime is centered around a single focused demo scene that hard-codes a small set of anchors, a single player-operated `MobileFactoryInstance`, and a few simple world-side sink loops. That is enough for validating the deploy, recall, redeploy, command-mode, and split-view editing flows, but it is not enough for broader regression testing where we want many mobile factories, varied footprints, richer world dressing, and interior layouts that can run unattended for a long time without jamming.

There are also two structural constraints in the current code that matter for this change. First, `MobileFactoryInstance` currently assumes a fixed interior size and a fixed deployment footprint/port layout. Second, `MobileFactoryDemo` owns most scene setup imperatively, which makes it awkward to author a larger scenario by copy-pasting more one-off setup code. This change therefore needs both a new scenario and a more data-driven way to describe mobile factory variants, world placement, and interior test layouts.

## Goals / Non-Goals

**Goals:**
- Add a separate large-scale mobile factory test scenario without removing the existing focused mobile factory demo.
- Support multiple simultaneous mobile factories in the same map, including both deployed and in-transit factories.
- Allow different mobile factories to use different footprint sizes, interior sizes, and initial poses.
- Provide several pre-authored interior conveyor case studies that exercise branching, merging, loops, lane handoff, loader/unloader use, and recovery behavior.
- Ensure the authored scenario can run for extended periods by including recycler-backed or sink-backed flow exits that prevent permanent belt deadlock.
- Keep one player-controlled mobile factory in the same world so the existing manual interaction loop can be tested alongside autonomous background activity.

**Non-Goals:**
- Do not replace the current `mobile_factory_demo.tscn` as the primary focused onboarding scene for the feature.
- Do not introduce full RTS pathfinding, collision avoidance, or complex autonomous vehicle AI for non-player factories.
- Do not add an open-ended procedural scenario generator in this change; authored presets are sufficient.
- Do not require brand-new production building types if the existing belt, splitter, merger, loader, unloader, producer, sink, and bridge set can express the needed test topologies.

## Decisions

### Add a dedicated large test scene instead of overloading the focused demo

The project will gain a new scenario scene dedicated to large-scale testing, while the current focused mobile demo remains available for targeted interaction verification. The larger scenario should reuse as much runtime code as possible from the existing mobile demo flow, but it should not be hidden behind a mode toggle inside the same scene because the setup goals are different: one scene is a guided interaction sandbox, the other is a busy systems-validation map.

This keeps the current demo approachable and gives us freedom to enlarge the map, add many factories, and author more environmental detail without making the focused scene harder to understand.

Alternative considered: keep a single `MobileFactoryDemo` scene and add a "large scenario" switch inside it. This was rejected because it would push more conditional setup into an already imperative script and make both authoring and regression debugging harder.

### Introduce scenario definitions, factory profiles, and interior presets as authored data

Instead of hard-coding each additional mobile factory directly in scene setup, the runtime should describe the large test map through a small authoring model:
- a scenario definition for map bounds, world dressing zones, and actor roster
- a mobile factory profile for footprint cells, port cells, interior bounds, default tint/label, and optional preview metadata
- an actor definition for spawn pose, initial lifecycle state, whether it is player-controlled, and any scripted movement/deployment route
- an interior preset for the structure layout and intent of each test case

For the first version, these definitions can live in C# data builders rather than new external data files. That gives us a maintainable, typed way to author multiple factories and layouts without committing to a serialization format too early.

Alternative considered: author everything directly in `.tscn` nodes and one-off setup code. This was rejected because the scenario needs many repeated patterns, and code-defined presets are easier to diff, validate, and reuse across smoke tests.

### Parameterize `MobileFactoryInstance` so size and topology are no longer fixed

`MobileFactoryInstance` should stop assuming a single hard-coded interior rectangle and a single `2x2` deployment footprint. Instead, the constructor or configuration path should accept a profile that defines:
- interior min/max bounds
- footprint offsets relative to the anchor
- world port offsets relative to the anchor
- transit parking offset or spawn pose defaults
- optional display metadata such as label or accent color

This allows the scenario to include smaller and larger mobile factories while keeping lifecycle logic centralized in one runtime type. Deployment validity, world preview rendering, hull sizing, and interior camera framing should all consult the same profile so size changes propagate consistently.

Alternative considered: create separate subclasses for small, medium, and large mobile factories. This was rejected because the behavioral differences are mostly data shape differences, not different lifecycle logic.

### Use simple scripted actor loops for non-player factories

Only one mobile factory in the large test scenario needs full player command and HUD interaction. The remaining background factories should use simple scripted behavior loops: some start deployed and continuously feed world outputs, while others start in transit and follow deterministic move, turn, deploy, wait, recall, and repeat sequences.

These loops should be authored as compact scripts or data-driven steps rather than full AI. The point is to keep the world visibly alive and to exercise lifecycle state transitions concurrently, not to build generalized autonomous gameplay.

Alternative considered: let every factory share the full player command stack or add an RTS-style command layer for all actors. This was rejected as unnecessary complexity for a test scenario.

### Build a library of named interior test cases using the existing structure set

Each scenario factory should receive a named interior preset that targets a specific logistics pattern. Example categories include:
- straight throughput baseline
- branch and recombine
- recirculating buffer loop
- loader/unloader relay
- asymmetrical multi-lane balancing
- dense merger priority stress

Every preset should declare its intended long-run exit strategy, such as a sink/recycler line, a recirculating recovery loop with bounded injection, or a bridge into a world-side consumer lane. The important constraint is that no preset should rely on infinite accumulation with no escape path, because that defeats the purpose of long unattended runs.

Alternative considered: manually improvise each interior in the scene editor. This was rejected because reproducible preset names and definitions are more useful for testing, debugging, and future automation.

### World presentation should make lifecycle state and size easy to read at a glance

Because the large test scenario is meant for observation, it should visually distinguish deployed factories, moving factories, and the player factory. The design should combine several lightweight cues:
- different initial poses and headings for in-transit actors
- hull scale or footprint preview that reflects each factory's actual size
- status labels, accent colors, or world markers that identify deployed versus moving factories
- richer world landmarks and lane groupings so the larger map remains legible

The goal is not cinematic presentation; it is quick debugging clarity. A tester should be able to tell which factories are active, where outputs are going, and which actor is currently under player control without opening code.

Alternative considered: rely only on the existing HUD and miniature visuals. This was rejected because a busier map needs clearer at-a-glance separation.

### Verification will focus on scenario composition contracts as well as runtime behavior

This change is partly content and partly runtime plumbing, so verification should cover both. In addition to behavior tests for deployment and movement, the project should add checks that the large scenario composes the expected actor roster and logistics intent:
- the scene loads with one player-controlled mobile factory
- the scenario includes multiple background factories in mixed lifecycle states
- at least one deployed factory exports to the world and at least one in-transit factory is visibly moving
- varied factory profiles are present
- interior presets include sink/recycler paths that allow sustained flow

Alternative considered: rely only on manual playtesting. This was rejected because the new scenario is explicitly intended as regression infrastructure.

## Risks / Trade-offs

- [More data-driven authoring increases setup abstraction] -> Mitigation: keep the first version in simple typed C# builders close to the current demo code instead of introducing a larger content pipeline.
- [Parameterizing factory size touches deployment, previews, visuals, and camera framing] -> Mitigation: define one shared factory profile model and make every size-sensitive system read from it instead of duplicating shape math.
- [Background actor loops could look repetitive or artificial] -> Mitigation: author a small mix of deployed, patrolling, and redeploying actors with distinct start poses and wait timings rather than one identical loop.
- [Long-running topology presets may still jam if injection and consumption are unbalanced] -> Mitigation: require each preset to identify its sink/recovery path and add validation notes or smoke checks around sustained flow assumptions.
- [A bigger scene may cost more in rendering and simulation] -> Mitigation: prefer moderate actor counts with richer topology per actor, lightweight world dressing, and deterministic scripted behavior instead of excessive unit counts.

## Migration Plan

1. Extract or introduce shared setup helpers so the large scenario can reuse mobile-factory world building without destabilizing the focused demo scene.
2. Introduce mobile factory profiles and interior preset builders, then refit the current demo to use the same profile path where practical.
3. Create the new large test scenario scene and author its world zones, actor roster, and state markers.
4. Add scripted behavior for non-player factories and wire the player-controlled factory to the existing HUD and command flow.
5. Add scenario-focused smoke coverage and update documentation to explain the test-map purpose and the interior case labels.

Rollback strategy: if the shared authoring abstractions cause regressions in the focused demo, the large scenario can temporarily keep its own setup helpers while preserving the new scene and preset content. The existing mobile factory demo remains intact throughout, so the team retains a known-good fallback validation scene.

## Open Questions

- How many background mobile factories is the target machine budget expected to support in the first version: three to five, or a denser stress pass?
- Should size variation affect only world footprint and hull visuals, or should interior editable bounds also scale per factory in the first pass?
- Do we want named in-world labels for each interior preset/test case, or is a scene-level test guide in docs sufficient?
