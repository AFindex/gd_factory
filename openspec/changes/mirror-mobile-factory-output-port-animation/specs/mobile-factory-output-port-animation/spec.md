## ADDED Requirements

### Requirement: Mobile-factory output ports use a staged outbound animation contract
The game SHALL animate each outbound packed world cargo through an ordered output-port handoff chain that mirrors the input-port rebuild method: packer handoff, inner buffering, bridge-out transfer, outer buffering, and world release.

#### Scenario: Packed cargo settles in the inner buffer before bridge-out transfer
- **WHEN** a `CargoPacker` finishes a packed world cargo and the connected output port accepts it
- **THEN** the player can observe that cargo arrive at the output port's cabin-side buffer as a distinct stage before any bridge-out movement begins

#### Scenario: Packed cargo waits at the outer buffer for world pickup
- **WHEN** an outbound packed cargo has already completed the bridge-out transfer but the world route cannot accept it yet
- **THEN** the output port keeps that cargo visibly staged at the world-side outer buffer instead of skipping the wait or releasing it early

### Requirement: Outbound output-port animation preserves one visible cargo owner
The game SHALL keep exactly one visible owner for a packed outbound world cargo while it moves from the packer through the output port to the world route.

#### Scenario: Packer handoff does not duplicate the packed cargo
- **WHEN** the `CargoPacker` hands a finished packed cargo to the output port
- **THEN** the packer chamber relinquishes the visible cargo body before the output port shows the same cargo in its own handoff chain

#### Scenario: World route takes ownership at the visible release edge
- **WHEN** the output port releases a packed cargo from its world-side stage back to the world route
- **THEN** the world route begins showing that cargo at the same visible release edge where the output port stops owning it, without a second disconnected spawn
