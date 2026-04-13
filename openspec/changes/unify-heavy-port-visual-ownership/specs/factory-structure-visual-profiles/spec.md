## ADDED Requirements

### Requirement: Heavy-port and converter visual profiles consume shared ownership state
Heavy-port structures and converter structures SHALL derive full-size cargo visibility from a shared heavy-cargo ownership contract instead of from local heuristics about buffered occupancy or processing state alone.

#### Scenario: Unpacker-owned cargo suppresses duplicate port payloads
- **WHEN** an unpacker chamber has accepted the active full-size cargo from an input heavy port
- **THEN** the unpacker visual profile may display that cargo in its chamber while the connected heavy-port visual profile does not display a second full-size copy

#### Scenario: Output-port-owned cargo suppresses duplicate packer payloads
- **WHEN** an output heavy port has accepted a packed full-size cargo from a packer chamber for outbound transfer
- **THEN** the heavy-port visual profile may display that cargo along its staged path while the packer chamber no longer displays the same cargo body

### Requirement: Converter chamber presentation keeps full-size cargo visible until the explicit transition point
Unpacker and packer visual profiles SHALL keep accepted full-size world cargo visible in the chamber until the explicit unpack-complete or pack-release transition rather than fading it out mid-handoff.

#### Scenario: Unpacker retires the cargo only at unpack completion
- **WHEN** an unpacker is processing an accepted full-size cargo
- **THEN** the cargo remains visible in the chamber until the unpack-complete transition, after which the chamber may retire that cargo and begin emitting cabin-scale outputs

#### Scenario: Packer releases the cargo only after pack completion
- **WHEN** a packer has accumulated the required cabin inputs and is sealing a full-size outbound cargo
- **THEN** the cargo remains visible as the chamber-owned outbound payload until the pack-release transition assigns it to the connected heavy output port
