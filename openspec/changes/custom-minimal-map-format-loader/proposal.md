## Why

The current factory demos still rely heavily on runtime-authored C# scene/bootstrap code to construct world layouts, interior layouts, and authored scenario content. That makes maps harder to iterate on, keeps map data coupled to demo controllers, and encourages repeated setup logic even when the underlying factory rules are already shared.

We now need a project-owned map format that stores only the gameplay data required to rebuild a factory map, plus a loader that can reconstruct playable maps from that data without carrying editor-only or scene-only noise.

## What Changes

- Add a custom minimal factory map data format for authored factory layouts, with a schema focused on gameplay data such as grid size, authored anchors, resource deposits, structures, facings, and lightweight scenario metadata.
- Add a runtime map loading pipeline that reads the custom map files and rebuilds world maps and factory interior maps using existing factory simulation/building primitives.
- Define a clear separation between map data, map loading, and demo-specific orchestration so demo controllers stop owning large authored layout payloads directly.
- Support loading at least the static factory sandbox layout and the focused mobile-factory authored layout through the new map pipeline.
- Keep the format intentionally minimal: no redundant transform data, no scene-tree serialization, and no editor-generated metadata unless runtime reconstruction truly requires it.

## Capabilities

### New Capabilities
- `factory-map-data-format`: Defines the minimal authored file format for static world maps and mobile-factory interior maps.
- `factory-map-runtime-loading`: Defines how authored map files are validated, loaded, and rebuilt into playable runtime factory layouts.

### Modified Capabilities
- `factory-production-demo`: The static sandbox startup layout is authored through the custom map format and reconstructed by the runtime loader instead of being fully hardcoded in demo bootstrap logic.
- `mobile-factory-demo`: The focused mobile-factory demo loads its authored world/interior layout data through the custom map format and runtime loader while preserving current gameplay behavior.

## Impact

- Affected code includes `scripts/factory/FactoryDemo.cs`, `scripts/factory/MobileFactoryDemo.cs`, and new shared map-data / map-loading support code under the factory runtime.
- This change introduces a project-owned serialized map artifact format and a runtime reconstruction path for authored factory content.
- Existing demo behavior should stay functionally equivalent, but authored scenario data will move out of controller code and into dedicated map files.
