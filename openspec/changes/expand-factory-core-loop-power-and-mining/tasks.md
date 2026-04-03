## 1. Shared Production Domain

- [x] 1.1 Extend the shared factory item and recipe domain to represent raw resources, intermediates, final outputs, machine input requirements, produced outputs, fuel values, and machine power demand.
- [x] 1.2 Add new build definitions and factory structure registrations for the first powered-production set, including extraction, generation, relay, and manufacturing structures.
- [x] 1.3 Refactor recipe-capable production logic so new manufacturing machines and any retained legacy producers can use the same deterministic process/buffer helpers.

## 2. Resource Extraction Layer

- [x] 2.1 Add a world resource-deposit model plus authored deposit data for the static sandbox, with rendering support that makes deposit footprints readable on the enlarged map.
- [x] 2.2 Integrate deposit-aware placement validation so only compatible mining structures can claim extractable cells while other structures are rejected there.
- [x] 2.3 Implement mining-drill simulation, buffering, and logistics handoff so powered drills emit raw resources into belts, storage, or inserter-fed chains.

## 3. Power Generation And Distribution

- [x] 3.1 Add power-node contracts and simulation support for connected electrical networks, including topology rebuild, network supply/demand accounting, and deterministic power-satisfaction results.
- [x] 3.2 Implement the first fuel-fed generator and power-relay structures, including fuel intake from logistics and visible powered versus unpowered states.
- [x] 3.3 Update mining and manufacturing structures to respect power availability, including reduced or stalled behavior under disconnected or underpowered networks.

## 4. Manufacturing Machines And Recipes

- [x] 4.1 Implement at least one refining machine and one true assembler that consume declared recipe ingredients, produce intermediate/final items, and expose their recipe state through the detail window.
- [x] 4.2 Add the first authored recipe chain that links mined resources, generator fuel, refined intermediates, and a higher-tier manufactured output used by the sandbox.
- [x] 4.3 Remove placeholder producer-driven starter behavior from the default sandbox path, or clearly demote it to compatibility/debug-only usage outside the main authored loop.

## 5. Expanded Sandbox Integration

- [x] 5.1 Expand static sandbox bounds, camera limits, and hover/build support to an approximately three-times-larger playable footprint.
- [x] 5.2 Rebuild the default `factory_demo` starter layout into named districts for extraction, power, refining, assembly, delivery, and retained regression or combat coverage.
- [x] 5.3 Give the new deposits and powered structures a consistent industrial presentation language that stays readable at sandbox camera distance without copying external game assets.

## 6. UI, Inspection, And Verification

- [x] 6.1 Update HUD and structure-detail output so miners, generators, relays, smelters, and assemblers expose resource, recipe, and power status clearly.
- [x] 6.2 Extend static sandbox smoke coverage to verify resource extraction, generator fueling, power satisfaction, recipe progression, and final delivery on the default authored map.
- [x] 6.3 Refresh supporting notes or demo documentation so the enlarged sandbox and its new powered factory use cases are discoverable for manual QA.
