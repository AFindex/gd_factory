## Why

The project now relies on authored `.nfmap` files for sandbox and mobile-factory content, but the current checks mostly stop at document parsing, bounds validation, and round-trip smoke coverage. That leaves room for authored maps that deserialize successfully yet still fail real placement rules, attachment mount rules, or expected connectivity when replayed through the runtime.

We need a headless validation pass that developers can run before opening a scene so bad map data is caught early, reported with actionable diagnostics, and handled correctly for the special mobile-factory world/interior pairing.

## What Changes

- Add a headless map-validation workflow that can validate authored factory maps and exit with a failing status when hard errors are found.
- Extend map validation beyond schema checks so authored structures are replayed against the real placement rules and produce actionable diagnostics for out-of-bounds cells, overlap conflicts, invalid placement order, unsupported mounts, and other reconstruction failures.
- Add advisory connectivity reporting that summarizes likely logistics, power, and port-connection issues without requiring a full interactive demo run.
- Treat mobile-factory authored content as a profile-aware validation target instead of a plain interior map so boundary attachments, interior bounds, and world-side deployment projections are checked with the correct factory profile.
- Fold the new validator into existing headless smoke coverage so current factory and focused mobile maps are checked through one shared validation path.

## Capabilities

### New Capabilities
- `factory-map-headless-validation`: Headless validation entry points, target catalogs, and diagnostic reporting for authored world maps and mobile-factory map bundles.

### Modified Capabilities
- `factory-map-runtime-loading`: Shared map validation/loading support gains reusable preflight diagnostics and profile-aware mobile interior validation rules that can run without reconstructing a full demo scene.

## Impact

- Affected code includes `scripts/factory/maps/FactoryMapData.cs`, `scripts/factory/maps/FactoryMapRuntimeLoader.cs`, `scripts/factory/smoke/FactoryMapSmokeSupport.cs`, and mobile-factory profile/runtime helpers under `scripts/factory/`.
- This change introduces a shared headless validation/reporting path for `.nfmap` assets and mobile world/interior bundles.
- Existing smoke flows for the static sandbox and focused mobile demo should keep working, but their map checks will become richer and more failure-oriented than the current round-trip-only validation.
