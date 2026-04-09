# factory-logistics-routing Specification

## Purpose
TBD - created by archiving change add-belt-midspan-merging-and-three-way-merger. Update Purpose after archive.

## Requirements
### Requirement: Belts support midspan transport merges
The logistics system SHALL allow a transport structure to send items into the occupied cell of another belt when that target belt can continue forwarding those items through its normal forward output, so T-shaped merges do not require a dedicated merger building.

#### Scenario: Orthogonal feeder merges into a running belt
- **WHEN** a feeder belt points into the occupied cell of a perpendicular or straight-ahead target belt whose forward output path is valid
- **THEN** the target belt accepts the item from the feeder and continues moving it toward the target belt's forward output instead of treating the feeder as disconnected

#### Scenario: Midspan merge does not create a reverse output
- **WHEN** a belt receives items through a midspan merge while it already has a defined forward-facing output
- **THEN** the merged items serialize onto that belt's existing forward flow and do not cause the belt to emit items sideways or backward

### Requirement: Belt merge semantics remain deterministic under shared throughput
The logistics system SHALL keep belt midspan merges deterministic by preserving one forward output path per belt segment and serializing competing inbound items according to the transport structure's normal spacing and dispatch rules.

#### Scenario: Mainline and feeder items share one forward queue
- **WHEN** both the belt's main upstream connection and a side feeder are able to provide items into the same target belt
- **THEN** the target belt queues those items onto one forward-moving stream without dropping items or creating a second independent output lane

#### Scenario: Blocked downstream output stalls the merged segment
- **WHEN** a belt that is receiving items from a midspan feeder has no downstream structure currently able to receive its forward output
- **THEN** items from both the mainline and the feeder remain buffered according to the normal transport rules until the downstream path becomes available

### Requirement: Merger provides three inbound faces
The logistics system SHALL treat the merger as a single-cell three-input, one-output transport structure that accepts items from its rear, left, and right faces and forwards all accepted items through its front output.

#### Scenario: Rear feed joins the side inputs
- **WHEN** belts or other transport providers send items into the rear, left, and right faces of a merger over time
- **THEN** the merger accepts items from all three inbound faces and routes them toward the same forward output

#### Scenario: Congested output retains accepted merger items
- **WHEN** a merger has accepted items from one or more of its three inputs but its forward output is temporarily blocked
- **THEN** the accepted items remain buffered at the merger until the output path can receive them, instead of being discarded or redirected
