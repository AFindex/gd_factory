## MODIFIED Requirements

### Requirement: Interior editor shows ports and their external state
The game SHALL display mobile factory boundary attachments inside the editor along with their direction, cross-boundary shape, external connection state, and whether they hand off world-scale payloads into conversion/staging structures before cargo reaches cabin rails.

#### Scenario: Editor distinguishes world handoff versus cabin flow
- **WHEN** the player views a boundary attachment that connects to unpacking, packing, or staging structures in the mobile factory editor
- **THEN** the editor indicates which side handles world payloads, which side emits or receives cabin carriers, and whether the boundary is currently connected, disconnected, or blocked

### Requirement: World miniature mirrors the shared interior layout
The game SHALL present a miniature world representation of the mobile factory that reflects the same internal layout and item flow shown in the editor, while preserving the visual distinction between world-scale payload exchange and cabin-scale carrier flow.

#### Scenario: Miniature preserves the conversion scale break
- **WHEN** items move from the world side through a boundary attachment into the mobile factory interior
- **THEN** the world-side miniature shows a readable conversion step from large external payload to compact cabin carrier instead of showing one unchanged payload size throughout the whole route

#### Scenario: Layout changes preserve cargo-standard readability
- **WHEN** the mobile factory's interior layout changes around unpackers, packers, buffers, and cabin modules
- **THEN** the world-side miniature updates to reflect the same arrangement while keeping world-payload structures visually distinct from cabin-rail structures

## ADDED Requirements

### Requirement: Interior editing previews teach cabin module scale
The game SHALL make interior build previews and structure labels communicate that cabin modules are approximately world-payload-sized equipment blocks and that only compact carriers traverse cabin rails.

#### Scenario: Conversion module preview shows world-payload handling volume
- **WHEN** the player previews an unpacker, packer, or staging buffer in the interior editor
- **THEN** the preview footprint and/or model callout communicates handling volume for one world payload rather than reading as a tiny rail accessory

#### Scenario: Cabin rail preview omits world-payload affordances
- **WHEN** the player previews a standard interior feed rail or compact carrier-only logistics piece
- **THEN** the preview reads as a small carrier path and does not imply that world-standard payloads can be placed onto it directly

#### Scenario: Preview does not imply that world payloads shrink indoors
- **WHEN** the player previews a cabin-side structure that handles world cargo in the interior editor
- **THEN** the preview communicates that the handled world payload keeps its world-scale size class and that spatial accommodation comes from module volume instead of cargo downscaling
