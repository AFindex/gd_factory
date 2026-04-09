# factory-transport-render-performance Specification

## Purpose
Define the transport-render pipeline behavior for high-density logistics scenes so moving payloads remain readable while the renderer can batch, cull, and degrade visuals without affecting simulation correctness.

## Requirements
### Requirement: Transport rendering batches visible logistics items by shared render descriptors
The game SHALL render moving logistics items through a shared transport-render pipeline that groups visible items by compatible render descriptor instead of requiring one scene node per transport item.

#### Scenario: Visible belt payloads share batchable rendering
- **WHEN** many moving logistics items with compatible placeholder, textured, billboard, or other batchable transport visuals are visible in the same factory view
- **THEN** the transport-render pipeline groups those visible items into shared render buckets keyed by compatible geometry and material descriptors rather than creating a dedicated scene-node visual for each item

#### Scenario: Unsupported special visuals preserve visibility through a deterministic fallback
- **WHEN** a moving item's preferred transport visual cannot participate in the shared batch path
- **THEN** the game still renders that item through a deterministic supported fallback descriptor instead of hiding the payload or forcing the entire transport view back to per-item scene nodes

### Requirement: Transport rendering culls non-visible logistics items by camera-driven world coverage
The game SHALL submit transport visuals only for logistics items that fall within the active camera's relevant world coverage plus a small safety margin, while continuing to simulate off-screen logistics items normally.

#### Scenario: Off-screen transport items remain simulated but are not submitted for rendering
- **WHEN** moving logistics items travel outside the active camera's visible world coverage
- **THEN** those items continue advancing through deterministic transport simulation without requiring submitted world-space transport visuals until they re-enter the renderable area

#### Scenario: View-edge safety margin avoids abrupt visibility loss
- **WHEN** a moving logistics item travels near the edge of the active camera's visible world coverage
- **THEN** the transport-render pipeline keeps that item renderable within a small padded margin so the item does not disappear abruptly at the exact viewport boundary

### Requirement: Transport rendering supports distance-based visual degradation without changing logistics behavior
The game SHALL allow moving logistics items to degrade from their higher-fidelity transport presentation to lighter-weight fallback presentations as view distance increases, while preserving transport ordering, timing, and item-kind readability.

#### Scenario: Nearby items retain their primary transport presentation
- **WHEN** a moving logistics item is within the near observation band around the camera
- **THEN** the item uses its primary configured transport presentation or the closest supported equivalent for that profile

#### Scenario: Distant items use lighter-weight fallbacks while remaining distinguishable
- **WHEN** a moving logistics item is still in the renderable area but outside the near observation band
- **THEN** the item may render with a lighter-weight fallback presentation that still preserves enough item-kind distinction for the player to tell that logistics are moving
