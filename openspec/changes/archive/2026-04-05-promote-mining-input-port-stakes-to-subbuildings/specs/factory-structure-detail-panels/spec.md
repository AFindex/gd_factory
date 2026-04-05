## ADDED Requirements

### Requirement: Mining input port detail windows expose mining stake status and rebuild actions
The game SHALL show mining-input-specific deployment counts in the structure detail window and provide direct controls to build replacement mining stakes from that same window.

#### Scenario: Detail window shows deployed and available stake counts
- **WHEN** the player opens the detail window for a mining input port
- **THEN** the window shows that port's built stake stock, currently deployed stake count, and eligible mining-cell count for the current deployment state

#### Scenario: Player rebuilds a stake from the detail window
- **WHEN** the player uses a mining-stake build action from the mining input port detail window and the port has remaining stake capacity
- **THEN** the port increases its built stake stock and the detail window refreshes to show the updated count without requiring unrelated editor actions
