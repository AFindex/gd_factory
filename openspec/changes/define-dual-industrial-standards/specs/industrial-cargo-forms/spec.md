## ADDED Requirements

### Requirement: Resource identity is independent from cargo form
The system SHALL represent transportable goods using at least a resource identity layer and a cargo-form layer so the same resource can appear in different transport forms across industrial standards without becoming a different resource.

#### Scenario: Same resource resolves multiple cargo forms
- **WHEN** the game resolves iron ore for world transport and interior transport
- **THEN** both items remain the same iron-ore resource identity while being allowed to use different cargo forms and visual payload descriptors

### Requirement: Buildings declare which cargo forms they accept and emit
Each transport, storage, manufacturing, or conversion structure SHALL declare the cargo forms it can accept, buffer, transform, and emit instead of implicitly accepting every form of a resource.

#### Scenario: Interior machine rejects unsupported world cargo form
- **WHEN** an interior-standard machine receives a resource in a world-only cargo form that is not listed in its accepted-form contract
- **THEN** the machine refuses the item and does not treat the resource identity alone as sufficient for acceptance

#### Scenario: Conversion machine emits the configured target cargo form
- **WHEN** a conversion structure completes a valid world-to-interior or interior-to-world transformation
- **THEN** it emits the resource using the cargo form declared by its output contract

### Requirement: Cross-standard logistics require explicit cargo-form conversion
The system SHALL require an explicit conversion step whenever a logistics flow crosses between `World` and `Interior` industrial standards and the source cargo form is not directly valid in the destination standard.

#### Scenario: World supply must be unpacked before entering interior feed network
- **WHEN** world-standard cargo reaches a mobile-factory interior chain that only accepts interior feed forms
- **THEN** the flow requires an unpacking or equivalent conversion step before downstream interior structures can process the resource

#### Scenario: Interior output must be packed before entering world delivery network
- **WHEN** an interior-standard production line finishes an item that needs to travel through world logistics
- **THEN** the flow requires a packing or equivalent conversion step before world-standard delivery structures accept the item
