## MODIFIED Requirements

### Requirement: Interior editor shows ports and their external state
The game SHALL display mobile factory boundary attachments inside the editor along with their direction, cross-boundary shape, external connection state, and whether they belong to the heavy world-bundle handoff layer or the cabin-carrier logistics layer.

#### Scenario: Editor shows cargo port role and heavy handoff semantics
- **WHEN** the player views an input or output cargo attachment in the mobile factory editor
- **THEN** the editor indicates that the attachment stages world bundles in a heavy handoff node, shows its current connection state, and does not describe it as a direct cabin-belt endpoint

#### Scenario: Editor shows bundle template or conversion target context
- **WHEN** the player selects an unpacker, packer, or adjacent cargo port in the editor
- **THEN** the editor can report the current bundle size tier, active or target bundle template, or equivalent conversion context needed to understand what heavy payload that node is handling

### Requirement: World miniature mirrors the shared interior layout
The game SHALL present a miniature world representation of the mobile factory that reflects the same internal layout and item flow shown in the editor, including a visible distinction between heavy world-bundle handoff nodes and cabin-carrier belt flow.

#### Scenario: Miniature keeps heavy nodes visually distinct from cabin belts
- **WHEN** the world-side miniature shows the current mobile factory interior
- **THEN** unpackers, packers, heavy buffers, and cargo ports read as heavy handoff nodes while ordinary cabin belts continue to show only cabin-carrier flow

#### Scenario: Miniature never shows a world bundle riding a cabin belt
- **WHEN** a world bundle is staged or processed inside the mobile factory
- **THEN** the miniature renders that heavy payload only at heavy handoff nodes and does not depict it as traveling directly along the ordinary cabin belt network
