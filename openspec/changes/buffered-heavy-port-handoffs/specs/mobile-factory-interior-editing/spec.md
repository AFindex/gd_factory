## ADDED Requirements

### Requirement: Interior editing shows staged heavy-handoff status
The mobile-factory interior editor SHALL show staged heavy-handoff status for input and output interfaces, including whether a full-size cargo is buffered on the world side, buffered on the cabin side, currently bridging, waiting for unpacking, or waiting for world pickup.

#### Scenario: Editor identifies inbound cargo waiting for the unpacker
- **WHEN** the player opens the interior editor while an input interface has a full-size world cargo buffered on its cabin side and the unpacker is still busy
- **THEN** the editor details and attachment status indicate that the cargo is waiting for unpacker handoff rather than only showing a generic connected state

#### Scenario: Editor identifies outbound cargo waiting for the world route
- **WHEN** the player opens the interior editor while an output interface has a full-size packed cargo buffered before world release
- **THEN** the editor details and attachment status indicate that the cargo is waiting for world pickup rather than only showing a generic blocked state

### Requirement: Interior editing distinguishes heavy-cargo staging from cabin-feed rails
The mobile-factory interior editor SHALL visually distinguish heavy-cargo staging areas from ordinary cabin-feed rail segments so players can tell where full-size world cargo may appear and where only cabin-feed items may flow.

#### Scenario: Heavy input staging is not previewed as a cabin rail
- **WHEN** the player previews or inspects a heavy input interface in the interior editor
- **THEN** the editor presents its heavy-cargo staging and bridge-facing cells as interface staging space rather than as an ordinary cabin-feed rail segment

#### Scenario: Converter chamber is shown as heavy processing space
- **WHEN** the player previews or inspects a heavy unpacker or packer in the interior editor
- **THEN** the editor presents the visible chamber and staging footprint as heavy processing space that can hold a full-size world cargo
