## MODIFIED Requirements

### Requirement: Mobile demo demonstrates deploy-recall-redeploy gameplay
The game SHALL use the dedicated mobile-factory demo scene to demonstrate the core mobile-factory loop of maneuvering in transit, issuing a placement-style deployment command, connecting to the world, recalling, and redeploying the same factory instance.

#### Scenario: Confirmed deployment auto-approaches and connects the world loop
- **WHEN** the player confirms a valid deployment target while the mobile factory is in transit
- **THEN** the factory automatically moves to the selected anchor, aligns to the chosen facing, deploys, and feeds an observable world-side logistics loop through its active port

#### Scenario: Redeployment restores the concept loop after recall
- **WHEN** the player recalls the deployed mobile factory, repositions it in transit, and confirms another valid deployment target
- **THEN** the same mobile factory can deploy again and restore the world-side logistics loop without recreating its internal setup
