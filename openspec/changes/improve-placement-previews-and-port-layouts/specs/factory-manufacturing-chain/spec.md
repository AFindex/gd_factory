## ADDED Requirements

### Requirement: Multi-port assemblers expose side-separated logistics lanes
The game SHALL define the assembler and ammo assembler multi-port logistics contract from shared footprint metadata so each `3x2` machine exposes three input cells on one long side of its footprint and three output cells on the opposite long side, with rotation preserving that separation.

#### Scenario: East-facing assembler exposes three ports per long edge
- **WHEN** the player inspects or connects belts to an east-facing assembler or ammo assembler
- **THEN** the machine exposes three input cells along one `3-cell` long edge of the footprint and three output cells along the opposite `3-cell` long edge

#### Scenario: Rotating an assembler preserves separated input and output sides
- **WHEN** the player rotates an assembler or ammo assembler before placement, after placement, or through blueprint application preview
- **THEN** the six port cells rotate with the footprint and continue presenting three inputs on one side and three outputs on the opposite side

#### Scenario: Assemblers no longer show permanent port cubes outside belt preview
- **WHEN** the player is looking at an assembler or ammo assembler without an active belt placement preview
- **THEN** the machine does not show always-on port marker cubes on its model
