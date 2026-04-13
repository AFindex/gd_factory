## ADDED Requirements

### Requirement: Focused mobile-factory demo shows a readable single-owner inbound heavy handoff
The focused mobile-factory demo SHALL let a player observe one full-size inbound cargo move from the world route into the heavy input path and into the unpacker without duplicate flashes, premature cabin-side spawn, or delayed world-route consumption.

#### Scenario: Player observes one continuous inbound cargo body
- **WHEN** the focused demo runs an active inbound heavy-cargo exchange from the world into the factory
- **THEN** the player can follow one continuous full-size cargo body from route handoff through the heavy input path into the unpacker chamber without seeing extra copies appear on the bridge or cabin interface

#### Scenario: Demo does not show cargo before the route hands it off
- **WHEN** the focused demo has not yet transferred logical ownership of the next inbound cargo from the world route to the heavy input attachment
- **THEN** the cabin-side interface and unpacker path do not pre-spawn that cargo before the world route actually hands it off

### Requirement: Focused mobile-factory demo shows a readable single-owner outbound heavy handoff
The focused mobile-factory demo SHALL let a player observe one full-size outbound cargo move from the packer through the heavy output path to the world route without double rendering or disconnected release timing.

#### Scenario: Player observes one continuous outbound cargo body
- **WHEN** the focused demo runs an active outbound heavy-cargo exchange from the factory back to the world
- **THEN** the player can follow one continuous full-size cargo body from the packer chamber through the heavy output path to the world route without a duplicate flash between chamber, bridge, and route

#### Scenario: Demo world-route release happens at the visible release edge
- **WHEN** the focused demo releases a packed outbound cargo from the heavy output path to the world route
- **THEN** the world route accepts the cargo at the same visible release edge where the heavy output path relinquishes ownership instead of after a second disconnected animation beat
