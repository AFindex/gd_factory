using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
public partial class FactoryDemo
{
    private static bool RunFactoryMapSmokeChecks()
    {
        return FactoryMapSmokeSupport.VerifyTargets(FactoryMapValidationCatalog.StaticSandboxWorldTargetId);
    }
        private static bool HasSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--factory-smoke-test", global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async void RunSmokeChecks()
    {
        if (_grid is null || _cameraRig is null || _cameraRig.Camera is null || _simulation is null)
        {
            GD.PushError("FACTORY_SMOKE_FAILED missing grid or camera rig.");
            GetTree().Quit(1);
            return;
        }

        var probeCell = new Vector2I(4, 4);
        if (!_grid.CanPlace(probeCell))
        {
            GD.PushError("FACTORY_SMOKE_FAILED probe cell is unexpectedly occupied.");
            GetTree().Quit(1);
            return;
        }

        PlaceStructure(BuildPrototypeKind.Belt, probeCell, FacingDirection.South);
        var placed = _grid.TryGetStructure(probeCell, out var placedStructure) && placedStructure is BeltStructure;
        RemoveStructure(probeCell);
        var removed = _grid.CanPlace(probeCell);
        var multiCellPlacementVerified = RunMultiCellPlacementSmoke();
        var assemblerPortPreviewVerified = RunAssemblerPortPreviewSmoke();
        var beltDragAutoFacingVerified = RunBeltDragAutoFacingSmoke();
        var beltExistingJoinAutoFacingVerified = RunBeltExistingJoinAutoFacingSmoke();
        var previewArrowReady = _previewArrow is not null && _previewArrow.GetChildCount() >= 3;
        var playerInteractionVerified = await RunPlayerCharacterSmoke(probeCell);

        var initialStructureCount = _simulation.RegisteredStructureCount;
        var debugWorldSupportVerified = await RunDebugWorldSupportSmoke();
        var poweredFactoryVerified = await RunPoweredFactorySmoke();
        var sinkStats = CollectSinkStats();
        ConfigureCombatScenarios();
        var profilerText = _hud?.ProfilerText ?? string.Empty;
        var splitterFallbackRecovered = await RunSplitterFallbackSmoke();
        var midspanMergeRecovered = await RunMidspanMergeSmoke();
        var threeWayMergerRecovered = await RunThreeWayMergerSmoke();
        var bridgeLaneRecovered = await RunBridgeLaneIndependenceSmoke();
        var storageFlowVerified = await RunStorageInserterSmoke();
        var inspectionVerified = VerifyStorageInspectionPanel();
        var detailWindowVerified = await RunStructureDetailSmoke();
        var blueprintVerified = RunBlueprintWorkflowSmoke();
        var miningBlueprintVerified = RunMiningBlueprintWorkflowSmoke();
        var workspaceVerified = RunWorkspaceNavigationSmoke();
        var itemVisualProfilesVerified = RunItemVisualProfileSmoke();
        var structureVisualProfilesVerified = RunStructureVisualProfileSmoke();
        var (transportRenderTelemetryVerified, transportRenderCullingVerified) = await RunTransportRenderSmoke();
        var mapFormatVerified = RunFactoryMapSmokeChecks();
        var combatVerified = await VerifyCombatScenarios();

        if (!placed
            || !removed
            || initialStructureCount < 40
            || !debugWorldSupportVerified
            || !poweredFactoryVerified
            || string.IsNullOrWhiteSpace(profilerText)
            || !profilerText.Contains("FPS", global::System.StringComparison.Ordinal)
            || !splitterFallbackRecovered
            || !midspanMergeRecovered
            || !threeWayMergerRecovered
            || !bridgeLaneRecovered
            || !storageFlowVerified
            || !inspectionVerified
            || !detailWindowVerified
            || !blueprintVerified
            || !miningBlueprintVerified
              || !workspaceVerified
              || !itemVisualProfilesVerified
              || !structureVisualProfilesVerified
            || !transportRenderTelemetryVerified
            || !transportRenderCullingVerified
            || !mapFormatVerified
            || !combatVerified
            || !multiCellPlacementVerified
            || !beltDragAutoFacingVerified
            || !beltExistingJoinAutoFacingVerified
            || !assemblerPortPreviewVerified
            || !previewArrowReady
            || !playerInteractionVerified)
        {
              GD.PushError($"FACTORY_SMOKE_FAILED placed={placed} removed={removed} multiCell={multiCellPlacementVerified} beltDragAutoFacing={beltDragAutoFacingVerified} beltExistingJoinAutoFacing={beltExistingJoinAutoFacingVerified} assemblerPortPreview={assemblerPortPreviewVerified} playerInteraction={playerInteractionVerified} structures={initialStructureCount} debugWorldSupport={debugWorldSupportVerified} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} profiler={(!string.IsNullOrWhiteSpace(profilerText))} splitterFallback={splitterFallbackRecovered} midspanMerge={midspanMergeRecovered} threeWayMerger={threeWayMergerRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} miningBlueprint={miningBlueprintVerified} workspace={workspaceVerified} itemVisualProfiles={itemVisualProfilesVerified} structureVisualProfiles={structureVisualProfilesVerified} transportRenderTelemetry={transportRenderTelemetryVerified} transportRenderCulling={transportRenderCullingVerified} mapFormat={mapFormatVerified} combat={combatVerified} previewArrowReady={previewArrowReady}");
            GetTree().Quit(1);
            return;
        }

          GD.Print($"FACTORY_SMOKE_OK structures={initialStructureCount} debugWorldSupport={debugWorldSupportVerified} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} splitterFallback={splitterFallbackRecovered} midspanMerge={midspanMergeRecovered} threeWayMerger={threeWayMergerRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} miningBlueprint={miningBlueprintVerified} workspace={workspaceVerified} itemVisualProfiles={itemVisualProfilesVerified} structureVisualProfiles={structureVisualProfilesVerified} transportRenderTelemetry={transportRenderTelemetryVerified} transportRenderCulling={transportRenderCullingVerified} mapFormat={mapFormatVerified} combat={combatVerified} multiCell={multiCellPlacementVerified} beltDragAutoFacing={beltDragAutoFacingVerified} beltExistingJoinAutoFacing={beltExistingJoinAutoFacingVerified} assemblerPortPreview={assemblerPortPreviewVerified} previewArrowReady={previewArrowReady} playerInteraction={playerInteractionVerified}");
        GetTree().Quit();
    }

    private async Task<bool> RunDebugWorldSupportSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var expectedKinds = new[]
        {
            BuildPrototypeKind.DebugOreSource,
            BuildPrototypeKind.DebugPartSource,
            BuildPrototypeKind.DebugCombatSource,
            BuildPrototypeKind.DebugPowerGenerator
        };
        var catalogReady = CatalogContainsDebugCategory(
            FactoryIndustrialStandards.GetBuildCatalog(FactorySiteKind.World),
            "调试支援",
            expectedKinds);

        var authoredLayoutDebugFree = true;
        foreach (var structure in _grid.GetStructures())
        {
            if (!FactoryIndustrialStandards.IsDebugStructure(structure.Kind))
            {
                continue;
            }

            authoredLayoutDebugFree = false;
            break;
        }

        var sourceCell = new Vector2I(-40, 40);
        var sourceFacing = FacingDirection.East;
        var sourceOutputCell = FactoryStructureFactory.GetFootprint(BuildPrototypeKind.DebugOreSource).ResolveOutputCell(sourceCell, sourceFacing);
        var generatorCell = new Vector2I(-40, 37);
        if (!TryValidateWorldPlacement(BuildPrototypeKind.DebugOreSource, sourceCell, sourceFacing, out _)
            || !TryValidateWorldPlacement(BuildPrototypeKind.Sink, sourceOutputCell, sourceFacing, out _)
            || !TryValidateWorldPlacement(BuildPrototypeKind.DebugPowerGenerator, generatorCell, sourceFacing, out _))
        {
            return false;
        }

        var source = PlaceStructure(BuildPrototypeKind.DebugOreSource, sourceCell, sourceFacing) as DebugOreSourceStructure;
        var sink = PlaceStructure(BuildPrototypeKind.Sink, sourceOutputCell, sourceFacing) as SinkStructure;
        var generator = PlaceStructure(BuildPrototypeKind.DebugPowerGenerator, generatorCell, sourceFacing) as DebugPowerGeneratorStructure;
        if (source is null || sink is null || generator is null)
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(2.4f), SceneTreeTimer.SignalName.Timeout);
        var sourceDelivered = sink.DeliveredTotal > 0;
        var generatorStable = Mathf.IsEqualApprox(generator.GetAvailablePower(_simulation), generator.NominalPowerSupply);

        await ToSignal(GetTree().CreateTimer(0.8f), SceneTreeTimer.SignalName.Timeout);
        var generatorStillStable = Mathf.IsEqualApprox(generator.GetAvailablePower(_simulation), generator.NominalPowerSupply);
        return catalogReady && authoredLayoutDebugFree && sourceDelivered && generatorStable && generatorStillStable;
    }

    private async Task<bool> RunPlayerCharacterSmoke(Vector2I placementCell)
    {
        if (_playerController is null || _cameraRig is null || _grid is null)
        {
            return false;
        }

        var playerSpawned = _playerController.IsInsideTree() && _playerController.BackpackInventory.Count > 0;
        var startPosition = _playerController.GlobalPosition;
        Input.ActionPress("player_move_right");
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        Input.ActionRelease("player_move_right");
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

        var playerMoved = _playerController.GlobalPosition.DistanceTo(startPosition) > 0.2f;
        var cameraFollowed = new Vector2(_cameraRig.Position.X, _cameraRig.Position.Z)
            .DistanceTo(new Vector2(_playerController.GlobalPosition.X, _playerController.GlobalPosition.Z)) < 3.4f;

        HandlePlayerHotbarPressed(1);
        var hotbarSelected = _playerController.ActiveHotbarIndex == 1 && _playerController.GetArmedPlaceablePrototype().HasValue;
        var stackBeforePlacement = GetInventorySlotCount(_playerController.BackpackInventory, new Vector2I(1, 0));
        var validPlacement = hotbarSelected
            && TryValidateWorldPlacement(_playerController.GetArmedPlaceablePrototype()!.Value, placementCell, FacingDirection.East, out _);

        var placedFromHotbar = false;
        var consumedOneItem = false;
        if (validPlacement)
        {
            _selectedFacing = FacingDirection.East;
            _hoveredCell = placementCell;
            _hasHoveredCell = true;
            _canPlaceCurrentCell = true;
            HandleBuildPrimaryPress();
            HandleBuildPrimaryRelease();
            placedFromHotbar = _grid.TryGetStructure(placementCell, out var placedStructure) && placedStructure is not null;
            consumedOneItem = GetInventorySlotCount(_playerController.BackpackInventory, new Vector2I(1, 0)) == stackBeforePlacement - 1;
            RemoveStructure(placementCell);
        }

        if (!_playerController.GetArmedPlaceablePrototype().HasValue)
        {
            HandlePlayerHotbarPressed(1);
        }

        var dragStartCell = Vector2I.Zero;
        var dragNextCell = Vector2I.Zero;
        var stackBeforeDragPlacement = GetInventorySlotCount(_playerController.BackpackInventory, new Vector2I(1, 0));
        var dragPlacementVerified = false;
        var dragSkipVerified = false;
        var buildModeStayedActive = false;
        if (_playerController.GetArmedPlaceablePrototype() is BuildPrototypeKind dragPlacementKind
            && TryFindHorizontalPlacementPair(dragPlacementKind, placementCell, out dragStartCell, out dragNextCell))
        {
            _selectedFacing = FacingDirection.East;
            _hoveredCell = dragStartCell;
            _hasHoveredCell = true;
            _canPlaceCurrentCell = true;
            HandleBuildPrimaryPress();

            _hoveredCell = dragNextCell;
            _hasHoveredCell = true;
            _canPlaceCurrentCell = TryValidateWorldPlacement(dragPlacementKind, dragNextCell, FacingDirection.East, out _);
            var dragPlacedSecond = TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: true);

            _hoveredCell = dragStartCell;
            _hasHoveredCell = true;
            _canPlaceCurrentCell = TryValidateWorldPlacement(dragPlacementKind, dragStartCell, FacingDirection.East, out _);
            var skippedRevisit = !TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: true);

            HandleBuildPrimaryRelease();

            var dragPlacedFirst = _grid.TryGetStructure(dragStartCell, out var dragStartStructure) && dragStartStructure is not null;
            dragPlacementVerified = dragPlacedFirst
                && dragPlacedSecond
                && _grid.TryGetStructure(dragNextCell, out var dragNextStructure)
                && dragNextStructure is not null
                && GetInventorySlotCount(_playerController.BackpackInventory, new Vector2I(1, 0)) == stackBeforeDragPlacement - 2;
            dragSkipVerified = skippedRevisit
                && GetInventorySlotCount(_playerController.BackpackInventory, new Vector2I(1, 0)) == stackBeforeDragPlacement - 2;
            buildModeStayedActive = _interactionMode == FactoryInteractionMode.Build;

            RemoveStructure(dragStartCell);
            RemoveStructure(dragNextCell);
            RefreshInteractionModeFromBuildSource();
        }

        var crossTransferWorked = false;
        if (_grid.TryGetStructure(new Vector2I(17, 2), out var storageStructure)
            && storageStructure is StorageStructure storage
            && storage.TryResolveInventoryEndpoint("storage-buffer", out var storageEndpoint)
            && _playerController.TryResolveInventoryEndpoint(FactoryPlayerController.BackpackInventoryId, out var playerEndpoint))
        {
            if (_simulation is not null)
            {
                var seededCargo = _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo);
                storage.TryReceiveProvidedItem(seededCargo, storage.Cell + Vector2I.Left, _simulation);
            }

            var sourceSnapshot = storageEndpoint.Inventory.Snapshot();
            var targetSnapshot = playerEndpoint.Inventory.Snapshot();
            var sourceSlot = new Vector2I(-1, -1);
            var targetSlot = new Vector2I(-1, -1);
            for (var index = 0; index < sourceSnapshot.Length; index++)
            {
                if (sourceSnapshot[index].HasItem)
                {
                    sourceSlot = sourceSnapshot[index].Position;
                    break;
                }
            }

            for (var index = 0; index < targetSnapshot.Length; index++)
            {
                if (!targetSnapshot[index].HasItem && targetSnapshot[index].Position.Y > 0)
                {
                    targetSlot = targetSnapshot[index].Position;
                    break;
                }
            }

            if (sourceSlot.X >= 0 && targetSlot.X >= 0)
            {
                var moved = storageEndpoint.Inventory.TryMoveItemTo(playerEndpoint.Inventory, sourceSlot, targetSlot, false, playerEndpoint.CanInsert);
                crossTransferWorked = moved && playerEndpoint.Inventory.GetItemOrDefault(targetSlot) is not null;
            }
        }

        var passed = playerSpawned
            && playerMoved
            && cameraFollowed
            && hotbarSelected
            && placedFromHotbar
            && consumedOneItem
            && dragPlacementVerified
            && dragSkipVerified
            && buildModeStayedActive
            && crossTransferWorked;

        if (!passed)
        {
            GD.Print($"FACTORY_PLAYER_SMOKE playerSpawned={playerSpawned} playerMoved={playerMoved} cameraFollowed={cameraFollowed} hotbarSelected={hotbarSelected} placedFromHotbar={placedFromHotbar} consumedOneItem={consumedOneItem} dragPlacement={dragPlacementVerified} dragSkip={dragSkipVerified} buildModeStayedActive={buildModeStayedActive} crossTransferWorked={crossTransferWorked}");
        }

        return passed;
    }

    private static int GetInventorySlotCount(FactorySlottedItemInventory inventory, Vector2I slot)
    {
        var snapshot = inventory.Snapshot();
        for (var index = 0; index < snapshot.Length; index++)
        {
            if (snapshot[index].Position == slot)
            {
                return snapshot[index].StackCount;
            }
        }

        return 0;
    }

    private static bool CatalogContainsDebugCategory(FactoryBuildCatalogDefinition catalog, string categoryTitle, IReadOnlyList<BuildPrototypeKind> expectedKinds)
    {
        for (var categoryIndex = 0; categoryIndex < catalog.Categories.Count; categoryIndex++)
        {
            var category = catalog.Categories[categoryIndex];
            if (!string.Equals(category.Title, categoryTitle, global::System.StringComparison.Ordinal))
            {
                continue;
            }

            for (var kindIndex = 0; kindIndex < expectedKinds.Count; kindIndex++)
            {
                var foundKind = false;
                for (var categoryKindIndex = 0; categoryKindIndex < category.Kinds.Count; categoryKindIndex++)
                {
                    if (category.Kinds[categoryKindIndex] != expectedKinds[kindIndex])
                    {
                        continue;
                    }

                    foundKind = true;
                    break;
                }

                if (!foundKind)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    private static Vector2I GetPrimaryInputCell(FactoryStructure structure)
    {
        var inputCells = structure.GetInputCells();
        return inputCells.Count > 0 ? inputCells[0] : structure.Cell + Vector2I.Left;
    }

    private bool RunMultiCellPlacementSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var anchor = new Vector2I(18, 18);
        var overflowAnchor = new Vector2I(FactoryConstants.GridMax, FactoryConstants.GridMax);
        if (!TryValidateWorldPlacement(BuildPrototypeKind.LargeStorageDepot, anchor, FacingDirection.East, out _))
        {
            return false;
        }

        if (TryValidateWorldPlacement(BuildPrototypeKind.LargeStorageDepot, overflowAnchor, FacingDirection.East, out _))
        {
            return false;
        }

        var depot = PlaceStructure(BuildPrototypeKind.LargeStorageDepot, anchor, FacingDirection.East) as LargeStorageDepotStructure;
        if (depot is null)
        {
            return false;
        }

        var occupiedCell = anchor + Vector2I.One;
        var resolvedFromSecondaryCell = _grid.TryGetStructure(occupiedCell, out var resolvedStructure) && resolvedStructure == depot;
        RemoveStructure(occupiedCell);
        var released = _grid.CanPlace(anchor) && _grid.CanPlace(occupiedCell);
        var singleCellStillValid = TryValidateWorldPlacement(BuildPrototypeKind.Belt, anchor, FacingDirection.East, out _);
        return resolvedFromSecondaryCell && released && singleCellStillValid;
    }

    private bool RunAssemblerPortPreviewSmoke()
    {
        if (_grid is null
            || _previewPortHintRoot is null
            || _previewCell is null
            || !_grid.TryGetStructure(new Vector2I(14, 2), out var structure)
            || structure is not AssemblerStructure assembler)
        {
            return false;
        }

        var footprint = FactoryStructureFactory.GetFootprint(BuildPrototypeKind.Assembler);
        var eastInputCells = footprint.ResolveInputCells(Vector2I.Zero, FacingDirection.East);
        var eastOutputCells = footprint.ResolveOutputCells(Vector2I.Zero, FacingDirection.East);
        var eastContractVerified = eastInputCells.Count == 3
            && eastOutputCells.Count == 3
            && eastInputCells[0] == new Vector2I(0, -1)
            && eastInputCells[1] == new Vector2I(1, -1)
            && eastInputCells[2] == new Vector2I(2, -1)
            && eastOutputCells[0] == new Vector2I(0, 2)
            && eastOutputCells[1] == new Vector2I(1, 2)
            && eastOutputCells[2] == new Vector2I(2, 2);

        var previewCell = assembler.GetInputCells()[0];
        _selectedFacing = FacingDirection.East;
        _hoveredCell = previewCell;
        _hasHoveredCell = true;

        SelectBuildKind(BuildPrototypeKind.Belt);
        _canPlaceCurrentCell = TryValidateWorldPlacement(BuildPrototypeKind.Belt, previewCell, FacingDirection.East, out _);
        UpdatePreview();
        var beltHintsVisible = _previewPortHintRoot.Visible;

        _selectedFacing = FacingDirection.South;
        _hoveredCell = new Vector2I(18, 18);
        _hasHoveredCell = true;
        _canPlaceCurrentCell = TryValidateWorldPlacement(BuildPrototypeKind.Assembler, _hoveredCell, _selectedFacing, out _);
        SelectBuildKind(BuildPrototypeKind.Assembler);
        UpdatePreview();
        var southPreviewSizeVerified = _previewCell.Mesh is BoxMesh rotatedPreviewMesh
            && Mathf.IsEqualApprox(rotatedPreviewMesh.Size.X, FactoryConstants.CellSize * 3.0f - (_grid.CellSize * 0.08f))
            && Mathf.IsEqualApprox(rotatedPreviewMesh.Size.Z, FactoryConstants.CellSize * 2.0f - (_grid.CellSize * 0.08f));

        SelectBuildKind(BuildPrototypeKind.Inserter);
        _canPlaceCurrentCell = TryValidateWorldPlacement(BuildPrototypeKind.Inserter, previewCell, FacingDirection.East, out _);
        UpdatePreview();
        var nonBeltHintsHidden = !_previewPortHintRoot.Visible;

        _selectedFacing = FacingDirection.East;
        _hoveredCell = new Vector2I(19, 18);
        _hasHoveredCell = true;
        _canPlaceCurrentCell = TryValidateWorldPlacement(BuildPrototypeKind.Merger, _hoveredCell, FacingDirection.East, out _);
        SelectBuildKind(BuildPrototypeKind.Merger);
        UpdatePreview();
        var mergerHintsVisible = CountVisiblePreviewPortHints() >= 4;

        EnterInteractionMode();
        return eastContractVerified && beltHintsVisible && southPreviewSizeVerified && nonBeltHintsHidden && mergerHintsVisible;
    }

    private int CountVisiblePreviewPortHints()
    {
        var count = 0;
        for (var index = 0; index < _previewPortHintMeshes.Count; index++)
        {
            if (_previewPortHintMeshes[index].Visible)
            {
                count++;
            }
        }

        return count;
    }

    private bool RunBeltDragAutoFacingSmoke()
    {
        if (_grid is null || !TryFindBeltTurnPlacementTriple(new Vector2I(18, 18), out var firstCell, out var secondCell, out var thirdCell))
        {
            return false;
        }

        SelectBuildKind(BuildPrototypeKind.Belt);
        _selectedFacing = FacingDirection.East;

        _hoveredCell = firstCell;
        _hasHoveredCell = true;
        _canPlaceCurrentCell = TryValidateWorldPlacement(BuildPrototypeKind.Belt, firstCell, _selectedFacing, out _);
        HandleBuildPrimaryPress();

        _hoveredCell = secondCell;
        _canPlaceCurrentCell = TryValidateWorldPlacement(BuildPrototypeKind.Belt, secondCell, _selectedFacing, out _);
        var placedSecond = TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: true);

        _hoveredCell = thirdCell;
        _canPlaceCurrentCell = TryValidateWorldPlacement(BuildPrototypeKind.Belt, thirdCell, _selectedFacing, out _);
        var placedThird = TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: true);
        HandleBuildPrimaryRelease();

        var firstFacingOk = _grid.TryGetStructure(firstCell, out var firstStructure)
            && firstStructure is BeltStructure firstBelt
            && firstBelt.Facing == FacingDirection.East;
        var secondFacingOk = _grid.TryGetStructure(secondCell, out var secondStructure)
            && secondStructure is BeltStructure secondBelt
            && secondBelt.Facing == FacingDirection.South;
        var thirdFacingOk = _grid.TryGetStructure(thirdCell, out var thirdStructure)
            && thirdStructure is BeltStructure thirdBelt
            && thirdBelt.Facing == FacingDirection.South;
        var selectedFacingUpdated = _selectedFacing == FacingDirection.South;

        RemoveStructure(thirdCell);
        RemoveStructure(secondCell);
        RemoveStructure(firstCell);
        EnterInteractionMode();

        return placedSecond && placedThird && firstFacingOk && secondFacingOk && thirdFacingOk && selectedFacingUpdated;
    }

    private bool RunBeltExistingJoinAutoFacingSmoke()
    {
        if (_grid is null)
        {
            return false;
        }

        var sourceCell = new Vector2I(30, 18);
        var joinCell = sourceCell + Vector2I.Right;
        var targetCell = joinCell + Vector2I.Down;
        if (!TryValidateWorldPlacement(BuildPrototypeKind.Belt, sourceCell, FacingDirection.East, out _)
            || !TryValidateWorldPlacement(BuildPrototypeKind.Belt, targetCell, FacingDirection.South, out _)
            || !TryValidateWorldPlacement(BuildPrototypeKind.Belt, joinCell, FacingDirection.East, out _))
        {
            return false;
        }

        var sourcePlaced = PlaceStructure(BuildPrototypeKind.Belt, sourceCell, FacingDirection.East) is BeltStructure;
        var targetPlaced = PlaceStructure(BuildPrototypeKind.Belt, targetCell, FacingDirection.South) is BeltStructure;
        if (!sourcePlaced || !targetPlaced)
        {
            RemoveStructure(targetCell);
            RemoveStructure(sourceCell);
            return false;
        }

        SelectBuildKind(BuildPrototypeKind.Belt);
        _selectedFacing = FacingDirection.East;
        var resolvedFacing = ResolveWorldPlacementFacing(BuildPrototypeKind.Belt, joinCell, trackCurrentCellForStroke: false);
        _hoveredCell = joinCell;
        _hasHoveredCell = true;
        var placedJoin = TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: false);

        var joinFacingOk = _grid.TryGetStructure(joinCell, out var joinStructure)
            && joinStructure is BeltStructure joinBelt
            && joinBelt.Facing == FacingDirection.South;
        var sourceFacingUnchanged = _grid.TryGetStructure(sourceCell, out var sourceStructure)
            && sourceStructure is BeltStructure sourceBelt
            && sourceBelt.Facing == FacingDirection.East;
        var targetFacingUnchanged = _grid.TryGetStructure(targetCell, out var targetStructure)
            && targetStructure is BeltStructure targetBelt
            && targetBelt.Facing == FacingDirection.South;

        RemoveStructure(joinCell);
        RemoveStructure(targetCell);
        RemoveStructure(sourceCell);
        EnterInteractionMode();

        return resolvedFacing == FacingDirection.South && placedJoin && joinFacingOk && sourceFacingUnchanged && targetFacingUnchanged;
    }

    private bool TryFindBeltTurnPlacementTriple(Vector2I nearCell, out Vector2I firstCell, out Vector2I secondCell, out Vector2I thirdCell)
    {
        firstCell = Vector2I.Zero;
        secondCell = Vector2I.Zero;
        thirdCell = Vector2I.Zero;
        if (_grid is null)
        {
            return false;
        }

        for (var y = nearCell.Y; y <= nearCell.Y + 6; y++)
        {
            for (var x = nearCell.X; x <= nearCell.X + 6; x++)
            {
                var candidate = new Vector2I(x, y);
                var horizontal = candidate + Vector2I.Right;
                var turn = horizontal + Vector2I.Down;
                if (!TryValidateWorldPlacement(BuildPrototypeKind.Belt, candidate, FacingDirection.East, out _)
                    || !TryValidateWorldPlacement(BuildPrototypeKind.Belt, horizontal, FacingDirection.East, out _)
                    || !TryValidateWorldPlacement(BuildPrototypeKind.Belt, turn, FacingDirection.South, out _))
                {
                    continue;
                }

                firstCell = candidate;
                secondCell = horizontal;
                thirdCell = turn;
                return true;
            }
        }

        return false;
    }

    private bool TryFindHorizontalPlacementPair(BuildPrototypeKind kind, Vector2I nearCell, out Vector2I startCell, out Vector2I nextCell)
    {
        startCell = Vector2I.Zero;
        nextCell = Vector2I.Zero;
        if (_grid is null)
        {
            return false;
        }

        for (var y = nearCell.Y; y <= nearCell.Y + 4; y++)
        {
            for (var x = nearCell.X; x <= nearCell.X + 4; x++)
            {
                var candidate = new Vector2I(x, y);
                var next = candidate + Vector2I.Right;
                if (!TryValidateWorldPlacement(kind, candidate, FacingDirection.East, out _)
                    || !TryValidateWorldPlacement(kind, next, FacingDirection.East, out _))
                {
                    continue;
                }

                startCell = candidate;
                nextCell = next;
                return true;
            }
        }

        return false;
    }

    private bool RunWorkspaceNavigationSmoke()
    {
        if (_hud is null)
        {
            return false;
        }

        var workspaceIds = _hud.GetWorkspaceIds();
        var required = new[] { BuildWorkspaceId, BlueprintWorkspaceId, TelemetryWorkspaceId, CombatWorkspaceId, TestingWorkspaceId, SavesWorkspaceId };
        if (!FactoryDemoSmokeSupport.ContainsAllWorkspaces(workspaceIds, required))
        {
            return false;
        }

        _hud.SelectWorkspace(BlueprintWorkspaceId);
        var blueprintVisible = _hud.ActiveWorkspaceId == BlueprintWorkspaceId && _hud.IsWorkspaceVisible(BlueprintWorkspaceId);

        _hud.SelectWorkspace(TelemetryWorkspaceId);
        var telemetryVisible = _hud.ActiveWorkspaceId == TelemetryWorkspaceId && _hud.IsWorkspaceVisible(TelemetryWorkspaceId);

        _hud.SelectWorkspace(CombatWorkspaceId);
        var combatVisible = _hud.ActiveWorkspaceId == CombatWorkspaceId && _hud.IsWorkspaceVisible(CombatWorkspaceId);

        _hud.SelectWorkspace(TestingWorkspaceId);
        var testingVisible = _hud.ActiveWorkspaceId == TestingWorkspaceId && _hud.IsWorkspaceVisible(TestingWorkspaceId);

        _hud.SelectWorkspace(SavesWorkspaceId);
        var savesVisible = _hud.ActiveWorkspaceId == SavesWorkspaceId && _hud.IsWorkspaceVisible(SavesWorkspaceId);

        _hud.SelectWorkspace(BuildWorkspaceId);
        var buildVisible = _hud.ActiveWorkspaceId == BuildWorkspaceId && _hud.IsWorkspaceVisible(BuildWorkspaceId);

        return blueprintVisible
            && telemetryVisible
            && combatVisible
            && testingVisible
            && savesVisible
            && buildVisible;
    }

    private static bool HasWorkspace(IReadOnlyList<string> workspaceIds, string workspaceId)
    {
        return FactoryDemoSmokeSupport.HasWorkspace(workspaceIds, workspaceId);
    }

    private async Task<bool> RunPoweredFactorySmoke()
    {
        if (_grid is null)
        {
            return false;
        }

        var coalDrillFound = _grid.TryGetStructure(new Vector2I(-36, -30), out var coalDrillStructure) && coalDrillStructure is MiningDrillStructure;
        var generatorFound = _grid.TryGetStructure(new Vector2I(-31, -30), out var generatorStructure) && generatorStructure is GeneratorStructure;
        var ironDrillFound = _grid.TryGetStructure(new Vector2I(-36, -22), out var ironDrillStructure) && ironDrillStructure is MiningDrillStructure;
        var copperDrillFound = _grid.TryGetStructure(new Vector2I(-36, -18), out var copperDrillStructure) && copperDrillStructure is MiningDrillStructure;
        var ironSmelterFound = _grid.TryGetStructure(new Vector2I(-30, -22), out var ironSmelterStructure) && ironSmelterStructure is SmelterStructure;
        var copperSmelterFound = _grid.TryGetStructure(new Vector2I(-30, -18), out var copperSmelterStructure) && copperSmelterStructure is SmelterStructure;
        var wireAssemblerFound = _grid.TryGetStructure(new Vector2I(-28, -18), out var wireAssemblerStructure) && wireAssemblerStructure is AssemblerStructure;
        var ammoAssemblerFound = _grid.TryGetStructure(new Vector2I(-25, -20), out var ammoAssemblerStructure) && ammoAssemblerStructure is AmmoAssemblerStructure;
        var maintenanceGeneratorFound = _grid.TryGetStructure(new Vector2I(10, 8), out var maintenanceGeneratorStructure) && maintenanceGeneratorStructure is GeneratorStructure;
        var batteryAssemblerFound = _grid.TryGetStructure(new Vector2I(14, 2), out var batteryAssemblerStructure) && batteryAssemblerStructure is AssemblerStructure;
        var maintenanceBufferFound = _grid.TryGetStructure(new Vector2I(23, 2), out var maintenanceBufferStructure) && maintenanceBufferStructure is StorageStructure;
        var successStorageFound = _grid.TryGetStructure(new Vector2I(13, 20), out var successStorageStructure) && successStorageStructure is StorageStructure;
        var successTurretFound = _grid.TryGetStructure(new Vector2I(15, 20), out var successTurretStructure) && successTurretStructure is GunTurretStructure;
        if (!coalDrillFound || !generatorFound || !ironDrillFound || !copperDrillFound || !ironSmelterFound || !copperSmelterFound || !wireAssemblerFound || !ammoAssemblerFound || !maintenanceGeneratorFound || !batteryAssemblerFound || !maintenanceBufferFound || !successStorageFound || !successTurretFound)
        {
            GD.Print($"FACTORY_POWERED_SMOKE_MISSING coalDrill={coalDrillFound} generator={generatorFound} ironDrill={ironDrillFound} copperDrill={copperDrillFound} ironSmelter={ironSmelterFound} copperSmelter={copperSmelterFound} wireAssembler={wireAssemblerFound} ammoAssembler={ammoAssemblerFound} maintenanceGenerator={maintenanceGeneratorFound} batteryAssembler={batteryAssemblerFound} maintenanceBuffer={maintenanceBufferFound} successStorage={successStorageFound} successTurret={successTurretFound}");
            return false;
        }

        var coalDrill = (MiningDrillStructure)coalDrillStructure!;
        var generator = (GeneratorStructure)generatorStructure!;
        var ironDrill = (MiningDrillStructure)ironDrillStructure!;
        var copperDrill = (MiningDrillStructure)copperDrillStructure!;
        var ironSmelter = (SmelterStructure)ironSmelterStructure!;
        var copperSmelter = (SmelterStructure)copperSmelterStructure!;
        var wireAssembler = (AssemblerStructure)wireAssemblerStructure!;
        var ammoAssembler = (AmmoAssemblerStructure)ammoAssemblerStructure!;
        var maintenanceGenerator = (GeneratorStructure)maintenanceGeneratorStructure!;
        var batteryAssembler = (AssemblerStructure)batteryAssemblerStructure!;
        var maintenanceBuffer = (StorageStructure)maintenanceBufferStructure!;
        var successStorage = (StorageStructure)successStorageStructure!;
        var successTurret = (GunTurretStructure)successTurretStructure!;

        await ToSignal(GetTree().CreateTimer(40.0f), SceneTreeTimer.SignalName.Timeout);
        var ironSummary = ironSmelter.GetDetailModel().SummaryLines;
        var copperSummary = copperSmelter.GetDetailModel().SummaryLines;
        var wireSummary = wireAssembler.GetDetailModel().SummaryLines;
        var ammoSummary = ammoAssembler.GetDetailModel().SummaryLines;
        var batterySummary = batteryAssembler.GetDetailModel().SummaryLines;
        var maintenanceBufferHasBattery = InventoryContainsLabel(maintenanceBuffer.GetDetailModel(), "电池组");
        var successStorageHasAmmo = InventoryContainsLabel(successStorage.GetDetailModel(), "弹药");

        var verified = coalDrill.ResourceKind == FactoryResourceKind.Coal
            && ironDrill.ResourceKind == FactoryResourceKind.IronOre
            && copperDrill.ResourceKind == FactoryResourceKind.CopperOre
            && (generator.IsGenerating || generator.HasFuelBuffered)
            && ContainsSummaryLine(ironSummary, "铁板")
            && ContainsSummaryLine(copperSummary, "铜板")
            && ContainsSummaryLine(wireSummary, "铜线")
            && ContainsSummaryLine(ammoSummary, "弹药")
            && (maintenanceGenerator.IsGenerating || maintenanceGenerator.HasFuelBuffered)
            && maintenanceBufferHasBattery
            && ContainsSummaryLine(batterySummary, "电池组")
            && (successStorageHasAmmo || successTurret.BufferedAmmo > 0 || successTurret.ShotsFired > 0);

        if (!verified)
        {
            GD.Print($"FACTORY_POWERED_SMOKE coalKind={coalDrill.ResourceKind} ironKind={ironDrill.ResourceKind} copperKind={copperDrill.ResourceKind} generator={generator.IsGenerating} generatorFuel={generator.HasFuelBuffered} maintenanceGenerator={maintenanceGenerator.IsGenerating} maintenanceFuel={maintenanceGenerator.HasFuelBuffered} maintenanceBufferHasBattery={maintenanceBufferHasBattery} successStorageHasAmmo={successStorageHasAmmo} successShots={successTurret.ShotsFired} ironSummary={string.Join('|', ironSummary)} copperSummary={string.Join('|', copperSummary)} wireSummary={string.Join('|', wireSummary)} ammoSummary={string.Join('|', ammoSummary)} batterySummary={string.Join('|', batterySummary)}");
        }

        return verified;
    }

    private static bool ContainsSummaryLine(IReadOnlyList<string> summaryLines, string pattern)
    {
        for (var index = 0; index < summaryLines.Count; index++)
        {
            if (summaryLines[index].Contains(pattern, global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InventoryContainsLabel(FactoryStructureDetailModel detailModel, string pattern)
    {
        if (detailModel.InventorySections.Count == 0)
        {
            return false;
        }

        var slots = detailModel.InventorySections[0].Slots;
        for (var index = 0; index < slots.Count; index++)
        {
            if ((slots[index].ItemLabel?.Contains(pattern, global::System.StringComparison.Ordinal) ?? false)
                && slots[index].StackCount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool RunItemVisualProfileSmoke()
    {
        var placeholderVisual = FactoryTransportVisualFactory.CreateVisual(new FactoryItem(-101, BuildPrototypeKind.MiningDrill, FactoryItemKind.IronOre), FactoryConstants.CellSize);
        var billboardVisual = FactoryTransportVisualFactory.CreateVisual(FactoryItemKind.CopperOre, FactoryConstants.CellSize);
        var ammoVisual = FactoryTransportVisualFactory.CreateVisual(FactoryItemKind.AmmoMagazine, FactoryConstants.CellSize);
        var worldBulkItem = new FactoryItem(-104, BuildPrototypeKind.MiningDrill, FactoryItemKind.IronOre, FactoryCargoForm.WorldBulk);
        var worldPackedItem = new FactoryItem(-105, BuildPrototypeKind.CargoPacker, FactoryItemKind.IronOre, FactoryCargoForm.WorldPacked);
        var interiorFeedItem = new FactoryItem(-106, BuildPrototypeKind.CargoUnpacker, FactoryItemKind.IronOre, FactoryCargoForm.InteriorFeed);
        var cabinBoundaryCellSize = MobileFactoryScenarioLibrary.CreateFocusedDemoProfile().InteriorCellSize;
        var copperDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(FactoryItemKind.CopperOre, FactoryConstants.CellSize);
        var ammoDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(FactoryItemKind.AmmoMagazine, FactoryConstants.CellSize);
        var worldBulkProfile = FactoryItemCatalog.ResolveVisualProfile(worldBulkItem);
        var worldPackedProfile = FactoryItemCatalog.ResolveVisualProfile(worldPackedItem);
        var interiorFeedProfile = FactoryItemCatalog.ResolveVisualProfile(interiorFeedItem);
        var worldBulkDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(worldBulkItem, FactoryConstants.CellSize);
        var worldPackedDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(worldPackedItem, FactoryConstants.CellSize);
        var interiorFeedDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(interiorFeedItem, FactoryConstants.CellSize);
        var interiorConversionDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(worldPackedItem, FactoryConstants.CellSize, FactoryTransportVisualContext.InteriorConversion);
        var boundaryHandoffDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(worldPackedItem, cabinBoundaryCellSize, FactoryTransportVisualContext.BoundaryHandoff);
        var worldBulkOccupiedLength = FactoryTransportVisualFactory.EstimateOccupiedLengthProgress(worldBulkDescriptors, FactoryConstants.CellSize);
        var worldPackedOccupiedLength = FactoryTransportVisualFactory.EstimateOccupiedLengthProgress(worldPackedDescriptors, FactoryConstants.CellSize);
        var interiorFeedOccupiedLength = FactoryTransportVisualFactory.EstimateOccupiedLengthProgress(interiorFeedDescriptors, FactoryConstants.CellSize);

        var placeholderMesh = FindFirstMesh(placeholderVisual);
        var billboardMesh = FindFirstMesh(billboardVisual);
        var ammoMesh = FindFirstMesh(ammoVisual);
        var distinctBaselineColors =
            !FactoryItemCatalog.GetAccentColor(FactoryItemKind.Coal).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.IronOre))
            && !FactoryItemCatalog.GetAccentColor(FactoryItemKind.IronOre).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.CopperOre))
            && !FactoryItemCatalog.GetAccentColor(FactoryItemKind.AmmoMagazine).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.HighVelocityAmmo));
        var iconsPresent =
            FactoryItemCatalog.GetIconTexture(FactoryItemKind.IronOre) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.IronPlate) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.CopperWire) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.HighVelocityAmmo) is not null;
        var cargoDisplayNamesDiffer =
            FactoryPresentation.GetItemDisplayName(worldBulkItem) != FactoryPresentation.GetItemDisplayName(worldPackedItem)
            && FactoryPresentation.GetItemDisplayName(worldPackedItem) != FactoryPresentation.GetItemDisplayName(interiorFeedItem);
        var cargoProfilesDiffer =
            !worldBulkProfile.Tint.IsEqualApprox(worldPackedProfile.Tint)
            && !worldPackedProfile.Tint.IsEqualApprox(interiorFeedProfile.Tint)
            && worldBulkProfile.PlaceholderScale.X > worldPackedProfile.PlaceholderScale.X
            && worldPackedProfile.PlaceholderScale.X > interiorFeedProfile.PlaceholderScale.X
            && worldPackedProfile.PlaceholderScale.X >= interiorFeedProfile.PlaceholderScale.X * 1.8f;
        var interiorCarrierResolved =
            interiorFeedDescriptors.Primary.Mode == FactoryTransportRenderMode.ModelNode
            && !interiorFeedDescriptors.Primary.IsBatchable
            && interiorFeedDescriptors.Primary.BatchKey.Contains("interior:", global::System.StringComparison.Ordinal)
            && FactoryPresentation.GetItemDisplayName(interiorFeedItem).Contains("舱内", global::System.StringComparison.Ordinal);
        var cargoContextMetadataResolved =
            worldPackedDescriptors.Primary.PresentationStandard == FactoryCargoPresentationStandard.WorldPayload
            && worldPackedDescriptors.Primary.VisualContext == FactoryTransportVisualContext.WorldRoute
            && worldPackedDescriptors.Primary.KeepsWorldScaleInsideCabin
            && interiorConversionDescriptors.Primary.PresentationStandard == FactoryCargoPresentationStandard.WorldPayload
            && interiorConversionDescriptors.Primary.VisualContext == FactoryTransportVisualContext.InteriorConversion
            && interiorConversionDescriptors.Primary.KeepsWorldScaleInsideCabin
            && boundaryHandoffDescriptors.Primary.PresentationStandard == FactoryCargoPresentationStandard.WorldPayload
            && boundaryHandoffDescriptors.Primary.VisualContext == FactoryTransportVisualContext.BoundaryHandoff
            && boundaryHandoffDescriptors.Primary.KeepsWorldScaleInsideCabin
            && interiorFeedDescriptors.Primary.PresentationStandard == FactoryCargoPresentationStandard.CabinCarrier
            && interiorFeedDescriptors.Primary.VisualContext == FactoryTransportVisualContext.InteriorRail
            && worldPackedDescriptors.Primary.MeshScale.X >= interiorFeedDescriptors.Primary.MeshScale.X * 1.8f;
        var cabinBoundaryPreservesWorldScale =
            Mathf.IsEqualApprox(boundaryHandoffDescriptors.Primary.MeshScale.X, worldPackedDescriptors.Primary.MeshScale.X)
            && Mathf.IsEqualApprox(boundaryHandoffDescriptors.Primary.MeshScale.Y, worldPackedDescriptors.Primary.MeshScale.Y)
            && Mathf.IsEqualApprox(boundaryHandoffDescriptors.Primary.MeshScale.Z, worldPackedDescriptors.Primary.MeshScale.Z);
        var transportFootprintsResolved =
            worldBulkOccupiedLength >= worldPackedOccupiedLength
            && worldPackedOccupiedLength > interiorFeedOccupiedLength
            && worldPackedOccupiedLength >= ItemSpacingThresholdForWorldCargo()
            && interiorFeedOccupiedLength >= 0.14f;

        placeholderVisual.QueueFree();
        billboardVisual.QueueFree();
        ammoVisual.QueueFree();

        return placeholderMesh?.Mesh is BoxMesh
            && placeholderMesh.CastShadow == GeometryInstance3D.ShadowCastingSetting.Off
            && billboardMesh?.Mesh is QuadMesh
            && billboardMesh.MaterialOverride is StandardMaterial3D billboardMaterial
            && billboardMaterial.BillboardMode == BaseMaterial3D.BillboardModeEnum.Enabled
            && ammoMesh?.Mesh is QuadMesh
            && copperDescriptors.Primary.BatchKey == copperDescriptors.ResolveForTier(FactoryTransportRenderTier.Near).BatchKey
            && copperDescriptors.Primary.Mode == FactoryTransportRenderMode.Billboard
            && copperDescriptors.Mid.IsBatchable
            && copperDescriptors.Far.IsBatchable
            && ammoDescriptors.Primary.Mode == FactoryTransportRenderMode.Billboard
            && ammoDescriptors.Primary.IsBatchable
            && ammoDescriptors.ResolveBatchableForTier(FactoryTransportRenderTier.Near).Mode == FactoryTransportRenderMode.Billboard
            && worldBulkDescriptors.Primary.Mode == FactoryTransportRenderMode.TexturedBox
            && worldPackedDescriptors.Primary.Mode == FactoryTransportRenderMode.TexturedBox
            && worldBulkDescriptors.Primary.BatchKey != worldPackedDescriptors.Primary.BatchKey
            && worldPackedDescriptors.Primary.BatchKey != interiorFeedDescriptors.Primary.BatchKey
            && interiorCarrierResolved
            && cargoContextMetadataResolved
            && cabinBoundaryPreservesWorldScale
            && transportFootprintsResolved
            && distinctBaselineColors
            && cargoDisplayNamesDiffer
            && cargoProfilesDiffer
            && iconsPresent;
    }

    private static float ItemSpacingThresholdForWorldCargo()
    {
        return 0.42f;
    }

    private async Task<(bool TelemetryVerified, bool CullingVerified)> RunTransportRenderSmoke()
    {
        if (_hud is null || _transportRenderManager is null || _cameraRig is null)
        {
            return (false, false);
        }

        var previousFollowState = _cameraRig.FollowTargetEnabled;
        _cameraRig.FollowTargetEnabled = false;
        _cameraRig.FocusWorldPosition(new Vector3(36.0f, 0.0f, -28.0f));
        _hud.SelectWorkspace(TelemetryWorkspaceId);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);

        var stats = _transportRenderManager.GetStats();
        var profilerText = _hud.ProfilerText ?? string.Empty;
        var telemetryVerified =
            stats.OptimizedPathActive
            && stats.TotalActiveItems > 0
            && stats.VisibleItems > 0
            && stats.ActiveBuckets > 0
            && profilerText.Contains("渲染中", global::System.StringComparison.Ordinal)
            && profilerText.Contains("优化 ON", global::System.StringComparison.Ordinal);
        var cullingVerified = stats.TotalActiveItems > stats.VisibleItems;
        _cameraRig.FollowTargetEnabled = previousFollowState;
        return (telemetryVerified, cullingVerified);
    }

    private bool RunStructureVisualProfileSmoke()
    {
        var authoredRoot = new Node3D { Name = "AuthoredVisualRoot" };
        var authoredMesh = new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = new BoxMesh { Size = new Vector3(0.4f, 0.4f, 0.4f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("38BDF8") }
        };
        authoredRoot.AddChild(authoredMesh);
        authoredMesh.Owner = authoredRoot;
        var authoredScene = new PackedScene();
        var authoredPacked = authoredScene.Pack(authoredRoot) == Error.Ok;
        authoredRoot.Free();

        var authoredController = FactoryStructureVisualFactory.CreateDetachedController(
            new FactoryStructureVisualProfile(
                authoredScene: authoredScene,
                nodeAnchors: new Dictionary<string, NodePath>
                {
                    ["mesh"] = new NodePath("Mesh")
                },
                materialAnchors: new Dictionary<string, NodePath>
                {
                    ["mesh-material"] = new NodePath("Mesh")
                }),
            FactoryConstants.CellSize);
        var authoredResolved = authoredPacked
            && authoredController.SourceKind == FactoryStructureVisualSourceKind.AuthoredScene
            && authoredController.GetNodeAnchor<MeshInstance3D>("mesh") is not null
            && authoredController.GetMaterialAnchor("mesh-material") is not null
            && CountMeshes(authoredController.Root) >= 1;

        var fallbackController = FactoryStructureVisualFactory.CreateDetachedController(
            new FactoryStructureVisualProfile(
                authoredScenePath: "res://missing/structure_visual_profile_smoke.tscn",
                proceduralBuilder: controller =>
                {
                    controller.Root.AddChild(FactoryStructureVisualFactory.CreateGenericPlaceholderNode(controller.CellSize * 0.8f));
                }),
            FactoryConstants.CellSize);
        var fallbackResolved = fallbackController.SourceKind == FactoryStructureVisualSourceKind.Procedural
            && fallbackController.Root.GetChildCount() > 0;

        var genericController = FactoryStructureVisualFactory.CreateDetachedController(
            new FactoryStructureVisualProfile(authoredScenePath: "res://missing/structure_visual_profile_placeholder.tscn"),
            FactoryConstants.CellSize);
        var genericResolved = genericController.SourceKind == FactoryStructureVisualSourceKind.GenericPlaceholder
            && CountMeshes(genericController.Root) >= 3;

        var legacyController = new GeneratorStructure().CreateDetachedVisualControllerForTesting();
        var legacyResolved = legacyController.SourceKind == FactoryStructureVisualSourceKind.Procedural
            && CountMeshes(legacyController.Root) >= 4;

        var smelter = new SmelterStructure();
        var smelterController = smelter.CreateDetachedVisualControllerForTesting();
        var coolState = new FactoryStructureVisualState(
            IsVisible: true,
            IsHovered: false,
            IsSelected: false,
            IsUnderAttack: false,
            IsDestroyed: false,
            IsActive: true,
            IsProcessing: false,
            ProcessRatio: 0.0f,
            HasBufferedOutput: false,
            PowerStatus: FactoryPowerStatus.Disconnected,
            PowerSatisfaction: 0.0f,
            PresentationTimeSeconds: 0.0);
        var hotState = coolState with
        {
            IsProcessing = true,
            ProcessRatio = 0.72f,
            HasBufferedOutput = true,
            PowerStatus = FactoryPowerStatus.Powered,
            PowerSatisfaction = 1.0f,
            PresentationTimeSeconds = 1.6
        };

        for (var index = 0; index < 6; index++)
        {
            smelter.ApplyVisualStateForTesting(smelterController, coolState with { PresentationTimeSeconds = index * 0.1 }, 1.0f);
        }

        var coolCoreEnergy = smelterController.GetMaterialAnchor("core-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var coolFireboxEnergy = smelterController.GetMaterialAnchor("firebox-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var coolPlumeScale = smelterController.GetNodeAnchor<Node3D>("heat-plume")?.Scale.Y ?? 0.0f;

        for (var index = 0; index < 6; index++)
        {
            smelter.ApplyVisualStateForTesting(smelterController, hotState with { PresentationTimeSeconds = 1.6 + (index * 0.12) }, 1.0f);
        }

        var hotCoreEnergy = smelterController.GetMaterialAnchor("core-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var hotFireboxEnergy = smelterController.GetMaterialAnchor("firebox-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var hotPlumeScale = smelterController.GetNodeAnchor<Node3D>("heat-plume")?.Scale.Y ?? 0.0f;
        var smelterHotCoolVerified = smelterController.SourceKind == FactoryStructureVisualSourceKind.Procedural
            && hotCoreEnergy > coolCoreEnergy
            && hotFireboxEnergy > coolFireboxEnergy
            && hotPlumeScale > coolPlumeScale;
        var interiorNamingVerified =
            FactoryIndustrialStandards.GetInteriorPresentationLabel(BuildPrototypeKind.Belt).Contains("供料", global::System.StringComparison.Ordinal)
            && FactoryIndustrialStandards.GetInteriorCarrierLabel(FactoryItemKind.IronOre).Contains("矿罐", global::System.StringComparison.Ordinal)
            && FactoryIndustrialStandards.GetInteriorPreviewSummary(BuildPrototypeKind.CargoUnpacker).Contains("原尺寸", global::System.StringComparison.Ordinal);

        authoredController.Root.Free();
        fallbackController.Root.Free();
        genericController.Root.Free();
        legacyController.Root.Free();
        smelterController.Root.Free();

        return authoredResolved
            && fallbackResolved
            && genericResolved
            && legacyResolved
            && smelterHotCoolVerified
            && interiorNamingVerified;
    }

    private static MeshInstance3D? FindFirstMesh(Node node)
    {
        if (node is MeshInstance3D mesh)
        {
            return mesh;
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                var found = FindFirstMesh(childNode);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }


    private static int CountMeshes(Node node)
    {
        var total = node is MeshInstance3D ? 1 : 0;
        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                total += CountMeshes(childNode);
            }
        }

        return total;
    }

    private bool RunBlueprintWorkflowSmoke()
    {
        if (_blueprintSite is null || _grid is null || _simulation is null)
        {
            return false;
        }

        var blueprintOrigin = new Vector2I(30, 34);
        var blueprintAnchors = new[]
        {
            blueprintOrigin,
            blueprintOrigin + new Vector2I(1, 0),
            blueprintOrigin + new Vector2I(2, 0),
            blueprintOrigin + new Vector2I(3, 0),
            blueprintOrigin + new Vector2I(0, 2),
            blueprintOrigin + new Vector2I(4, 2)
        };
        for (var index = 0; index < blueprintAnchors.Length; index++)
        {
            if (!_grid.CanPlace(blueprintAnchors[index]))
            {
                return false;
            }
        }

        if (PlaceStructure(BuildPrototypeKind.Storage, blueprintOrigin.X, blueprintOrigin.Y, FacingDirection.East) is null
            || PlaceStructure(BuildPrototypeKind.Inserter, blueprintOrigin.X + 1, blueprintOrigin.Y, FacingDirection.East) is null
            || PlaceStructure(BuildPrototypeKind.Belt, blueprintOrigin.X + 2, blueprintOrigin.Y, FacingDirection.East) is null
            || PlaceStructure(BuildPrototypeKind.Sink, blueprintOrigin.X + 3, blueprintOrigin.Y, FacingDirection.East) is null
            || PlaceStructure(BuildPrototypeKind.Assembler, blueprintOrigin.X, blueprintOrigin.Y + 2, FacingDirection.East) is null
            || PlaceStructure(BuildPrototypeKind.PowerPole, blueprintOrigin.X + 4, blueprintOrigin.Y + 2, FacingDirection.East) is null)
        {
            return false;
        }

        var captured = FactoryBlueprintCaptureService.CaptureSelection(
            _blueprintSite,
            new Rect2I(blueprintOrigin.X, blueprintOrigin.Y, 5, 4),
            "Smoke Utility Blueprint");
        if (captured is null || captured.StructureCount < 6)
        {
            return false;
        }

        captured = FactoryBlueprintWorkflowBridge.SavePendingCapture(captured, "Smoke Utility Blueprint");

        var invalidPlan = FactoryBlueprintPlanner.CreatePlan(captured, _blueprintSite, captured.SuggestedAnchorCell);
        var structureCountBefore = _simulation.RegisteredStructureCount;
        if (!TryFindBlueprintAnchor(captured, FacingDirection.South, out var validAnchor))
        {
            return false;
        }

        var validPlan = FactoryBlueprintPlanner.CreatePlan(captured, _blueprintSite, validAnchor, FacingDirection.South);
        _blueprintMode = FactoryBlueprintWorkflowMode.ApplyPreview;
        _blueprintApplyRotation = FacingDirection.South;
        _blueprintApplyPlan = validPlan;
        _hoveredCell = validAnchor;
        _hasHoveredCell = true;
        UpdatePreview();

        var previewAligned = false;
        for (var index = 0; index < validPlan.Entries.Count; index++)
        {
            var entry = validPlan.Entries[index];
            if (entry.SourceEntry.Kind != BuildPrototypeKind.Assembler)
            {
                continue;
            }

            var expectedPosition = FactoryPlacement.GetPreviewCenter(_grid, entry.SourceEntry.Kind, entry.TargetCell, entry.TargetFacing) + new Vector3(0.0f, 0.06f, 0.0f);
            var expectedRotation = _grid.WorldRotationRadians + FactoryDirection.ToYRotationRadians(entry.TargetFacing);
            var mesh = _blueprintPreviewMeshes[index];
            previewAligned = mesh.Visible
                && mesh.Position.DistanceTo(expectedPosition) < 0.05f
                && Mathf.IsEqualApprox(mesh.Rotation.Y, expectedRotation);

            break;
        }

        var committed = validPlan.IsValid && FactoryBlueprintPlanner.CommitPlan(validPlan, _blueprintSite);
        if (!committed)
        {
            return false;
        }

        var placedEntries = FactoryDemoSmokeSupport.CountMatchingPlacedEntries(
            validPlan.Entries,
            cell => _grid.TryGetStructure(cell, out var structure) ? structure : null);

        CancelBlueprintWorkflow(clearActiveBlueprint: false);
        EnterInteractionMode();

        return !invalidPlan.IsValid
            && previewAligned
            && validPlan.IsValid
            && placedEntries == captured.Entries.Count
            && _simulation.RegisteredStructureCount >= structureCountBefore + captured.Entries.Count;
    }

    private bool RunMiningBlueprintWorkflowSmoke()
    {
        if (_blueprintSite is null || _grid is null)
        {
            return false;
        }

        var originalDeposits = new List<FactoryResourceDepositDefinition>(_grid.GetResourceDeposits());
        if (!TryFindMiningBlueprintLaneCandidate(new Vector2I(34, 26), out var sourceDrillCell, out var sourceBeltCell, out var sourceSinkCell)
            || !TryFindMiningBlueprintLaneCandidate(new Vector2I(42, 26), out var targetDrillCell, out var targetBeltCell, out var targetSinkCell))
        {
            return false;
        }

        _grid.SetResourceDeposits(new List<FactoryResourceDepositDefinition>(originalDeposits)
        {
            new("smoke-mining-source", FactoryResourceKind.IronOre, "Smoke Source Iron", FactoryResourceCatalog.GetTint(FactoryResourceKind.IronOre), new[] { sourceDrillCell }),
            new("smoke-mining-target", FactoryResourceKind.IronOre, "Smoke Target Iron", FactoryResourceCatalog.GetTint(FactoryResourceKind.IronOre), new[] { targetDrillCell })
        });

        try
        {
            if (PlaceStructure(BuildPrototypeKind.MiningDrill, sourceDrillCell, FacingDirection.East) is not MiningDrillStructure
                || PlaceStructure(BuildPrototypeKind.Belt, sourceBeltCell, FacingDirection.East) is null
                || PlaceStructure(BuildPrototypeKind.Sink, sourceSinkCell, FacingDirection.East) is null)
            {
                return false;
            }

            var captured = FactoryBlueprintCaptureService.CaptureSelection(
                _blueprintSite,
                new Rect2I(sourceDrillCell.X, sourceDrillCell.Y, 3, 1),
                "Smoke Mining Blueprint");
            if (captured is null || captured.StructureCount != 3)
            {
                return false;
            }

            var validPlan = FactoryBlueprintPlanner.CreatePlan(captured, _blueprintSite, targetDrillCell);
            var committed = validPlan.IsValid && FactoryBlueprintPlanner.CommitPlan(validPlan, _blueprintSite);
            var drillPlaced = _grid.TryGetStructure(targetDrillCell, out var drillStructure) && drillStructure is MiningDrillStructure drill && drill.ResourceKind == FactoryResourceKind.IronOre;
            var beltPlaced = _grid.TryGetStructure(targetBeltCell, out var beltStructure) && beltStructure is BeltStructure;
            var sinkPlaced = _grid.TryGetStructure(targetSinkCell, out var sinkStructure) && sinkStructure is SinkStructure;
            return committed && drillPlaced && beltPlaced && sinkPlaced;
        }
        finally
        {
            RemoveStructure(targetSinkCell);
            RemoveStructure(targetBeltCell);
            RemoveStructure(targetDrillCell);
            RemoveStructure(sourceSinkCell);
            RemoveStructure(sourceBeltCell);
            RemoveStructure(sourceDrillCell);
            _grid.SetResourceDeposits(originalDeposits);
        }
    }

    private bool TryFindMiningBlueprintLaneCandidate(Vector2I nearCell, out Vector2I drillCell, out Vector2I beltCell, out Vector2I sinkCell)
    {
        drillCell = Vector2I.Zero;
        beltCell = Vector2I.Zero;
        sinkCell = Vector2I.Zero;
        if (_grid is null)
        {
            return false;
        }

        for (var y = nearCell.Y; y <= nearCell.Y + 8; y++)
        {
            for (var x = nearCell.X; x <= nearCell.X + 8; x++)
            {
                var candidateDrill = new Vector2I(x, y);
                var candidateBelt = candidateDrill + Vector2I.Right;
                var candidateSink = candidateDrill + (Vector2I.Right * 2);
                if (!_grid.CanPlace(candidateDrill) || !_grid.CanPlace(candidateBelt) || !_grid.CanPlace(candidateSink))
                {
                    continue;
                }

                drillCell = candidateDrill;
                beltCell = candidateBelt;
                sinkCell = candidateSink;
                return true;
            }
        }

        return false;
    }

    private bool TryFindBlueprintAnchor(FactoryBlueprintRecord blueprint, out Vector2I anchor)
    {
        return TryFindBlueprintAnchor(blueprint, FacingDirection.East, out anchor);
    }

    private bool TryFindBlueprintAnchor(FactoryBlueprintRecord blueprint, FacingDirection rotation, out Vector2I anchor)
    {
        anchor = Vector2I.Zero;
        if (_blueprintSite is null || _grid is null)
        {
            return false;
        }

        for (var y = _grid.MinCell.Y; y <= _grid.MaxCell.Y; y++)
        {
            for (var x = _grid.MinCell.X; x <= _grid.MaxCell.X; x++)
            {
                var candidate = new Vector2I(x, y);
                var plan = FactoryBlueprintPlanner.CreatePlan(blueprint, _blueprintSite, candidate, rotation);
                if (!plan.IsValid)
                {
                    continue;
                }

                anchor = candidate;
                return true;
            }
        }

        return false;
    }

    private async Task<bool> RunSplitterFallbackSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(24, -12),
            new Vector2I(25, -12),
            new Vector2I(26, -12),
            new Vector2I(26, -13),
            new Vector2I(27, -13),
            new Vector2I(26, -11),
            new Vector2I(27, -11),
            new Vector2I(28, -11)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        var sourceStorage = PlaceStructure(BuildPrototypeKind.Storage, 24, -12, FacingDirection.East) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 25, -12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, 26, -12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 26, -13, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 27, -13, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 26, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 27, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 28, -11, FacingDirection.East);

        if (sourceStorage is null
            || !_grid.TryGetStructure(new Vector2I(28, -11), out var sinkStructure) || sinkStructure is not SinkStructure sink
            || !_grid.TryGetStructure(new Vector2I(27, -13), out var blockerStructure) || blockerStructure is not BeltStructure blockedBelt)
        {
            return false;
        }

        for (var index = 0; index < 8; index++)
        {
            sourceStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), sourceStorage.Cell + Vector2I.Left, _simulation);
        }

        await ToSignal(GetTree().CreateTimer(10.0f), SceneTreeTimer.SignalName.Timeout);
        var blockedBranchOccupied = blockedBelt.TransitItemCount > 0;
        var deliveredAfter = sink.DeliveredTotal;

        var passed = blockedBranchOccupied || deliveredAfter > 0;
        if (!passed)
        {
            GD.Print($"FACTORY_SPLITTER_SMOKE blockedBranchOccupied={blockedBranchOccupied} deliveredAfter={deliveredAfter} sourceBuffered={sourceStorage.BufferedCount}");
        }

        return passed;
    }

    private async Task<bool> RunBridgeLaneIndependenceSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(24, -4),
            new Vector2I(25, -4),
            new Vector2I(26, -4),
            new Vector2I(27, -4),
            new Vector2I(26, -6),
            new Vector2I(26, -5),
            new Vector2I(26, -3),
            new Vector2I(26, -2)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        var eastStorage = PlaceStructure(BuildPrototypeKind.Storage, 24, -4, FacingDirection.East) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 25, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Bridge, 26, -4, FacingDirection.East);
        var eastSink = PlaceStructure(BuildPrototypeKind.Sink, 27, -4, FacingDirection.East) as SinkStructure;

        var southStorage = PlaceStructure(BuildPrototypeKind.Storage, 26, -6, FacingDirection.South) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 26, -5, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 26, -3, FacingDirection.South);
        var southSink = PlaceStructure(BuildPrototypeKind.Sink, 26, -2, FacingDirection.South) as SinkStructure;

        if (eastStorage is null
            || southStorage is null
            || eastSink is null
            || southSink is null)
        {
            return false;
        }

        for (var index = 0; index < 4; index++)
        {
            eastStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), eastStorage.Cell + Vector2I.Left, _simulation);
            southStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), southStorage.Cell + Vector2I.Left, _simulation);
        }

        await ToSignal(GetTree().CreateTimer(8.0f), SceneTreeTimer.SignalName.Timeout);

        var passed = eastSink.DeliveredTotal > 0 && southSink.DeliveredTotal > 0;
        if (!passed)
        {
            GD.Print($"FACTORY_BRIDGE_SMOKE eastDelivered={eastSink.DeliveredTotal} southDelivered={southSink.DeliveredTotal} eastBuffered={eastStorage.BufferedCount} southBuffered={southStorage.BufferedCount}");
        }

        return passed;
    }

    private async Task<bool> RunMidspanMergeSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(35, -12),
            new Vector2I(36, -12),
            new Vector2I(37, -12),
            new Vector2I(38, -12),
            new Vector2I(36, -15),
            new Vector2I(36, -14),
            new Vector2I(36, -13)
        };

        for (var index = 0; index < requiredCells.Length; index++)
        {
            if (!_grid.CanPlace(requiredCells[index]))
            {
                return false;
            }
        }

        PlaceStructure(BuildPrototypeKind.Belt, 35, -12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 36, -12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 37, -12, FacingDirection.East);
        var sink = PlaceStructure(BuildPrototypeKind.Sink, 38, -12, FacingDirection.East) as SinkStructure;
        var feederStorage = PlaceStructure(BuildPrototypeKind.Storage, 36, -15, FacingDirection.South) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 36, -14, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 36, -13, FacingDirection.South);

        if (sink is null || feederStorage is null)
        {
            return false;
        }

        for (var index = 0; index < 4; index++)
        {
            feederStorage.TryReceiveProvidedItem(
                _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
                GetPrimaryInputCell(feederStorage),
                _simulation);
        }

        await ToSignal(GetTree().CreateTimer(8.0f), SceneTreeTimer.SignalName.Timeout);

        var passed = sink.DeliveredTotal > 0 && feederStorage.BufferedCount < 4;
        if (!passed)
        {
            GD.Print($"FACTORY_MIDSPAN_MERGE_SMOKE delivered={sink.DeliveredTotal} feederBuffered={feederStorage.BufferedCount}");
        }

        return passed;
    }

    private async Task<bool> RunThreeWayMergerSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(34, -20),
            new Vector2I(35, -20),
            new Vector2I(36, -20),
            new Vector2I(37, -20),
            new Vector2I(38, -20),
            new Vector2I(36, -23),
            new Vector2I(36, -22),
            new Vector2I(36, -21),
            new Vector2I(36, -19),
            new Vector2I(36, -18),
            new Vector2I(36, -17)
        };

        for (var index = 0; index < requiredCells.Length; index++)
        {
            if (!_grid.CanPlace(requiredCells[index]))
            {
                return false;
            }
        }

        var westStorage = PlaceStructure(BuildPrototypeKind.Storage, 34, -20, FacingDirection.East) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 35, -20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Merger, 36, -20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 37, -20, FacingDirection.East);
        var sink = PlaceStructure(BuildPrototypeKind.Sink, 38, -20, FacingDirection.East) as SinkStructure;

        var northStorage = PlaceStructure(BuildPrototypeKind.Storage, 36, -23, FacingDirection.South) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 36, -22, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 36, -21, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Belt, 36, -19, FacingDirection.North);
        var southStorage = PlaceStructure(BuildPrototypeKind.Storage, 36, -17, FacingDirection.North) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 36, -18, FacingDirection.North);

        if (westStorage is null || northStorage is null || southStorage is null || sink is null)
        {
            return false;
        }

        for (var index = 0; index < 4; index++)
        {
            westStorage.TryReceiveProvidedItem(
                _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
                GetPrimaryInputCell(westStorage),
                _simulation);
            northStorage.TryReceiveProvidedItem(
                _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
                GetPrimaryInputCell(northStorage),
                _simulation);
            southStorage.TryReceiveProvidedItem(
                _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
                GetPrimaryInputCell(southStorage),
                _simulation);
        }

        await ToSignal(GetTree().CreateTimer(10.0f), SceneTreeTimer.SignalName.Timeout);

        var passed = sink.DeliveredTotal > 0
            && westStorage.BufferedCount < 4
            && northStorage.BufferedCount < 4
            && southStorage.BufferedCount < 4;
        if (!passed)
        {
            GD.Print($"FACTORY_THREE_WAY_MERGER_SMOKE delivered={sink.DeliveredTotal} westBuffered={westStorage.BufferedCount} northBuffered={northStorage.BufferedCount} southBuffered={southStorage.BufferedCount}");
        }

        return passed;
    }

    private async Task<bool> RunStorageInserterSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(24, 4),
            new Vector2I(25, 4),
            new Vector2I(26, 4),
            new Vector2I(27, 4),
            new Vector2I(28, 4)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        PlaceStructure(BuildPrototypeKind.Storage, 24, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 25, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 26, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 27, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 28, 4, FacingDirection.East);

        if (!_grid.TryGetStructure(new Vector2I(25, 4), out var storageStructure) || storageStructure is not StorageStructure storage
            || !_grid.TryGetStructure(new Vector2I(28, 4), out var sinkStructure) || sinkStructure is not SinkStructure sink)
        {
            return false;
        }

        var injectedItems = new List<FactoryItem>();
        for (var index = 0; index < 3; index++)
        {
            var injectedItem = _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo);
            if (!storage.TryReceiveProvidedItem(injectedItem, storage.Cell + Vector2I.Left, _simulation))
            {
                return false;
            }

            injectedItems.Add(injectedItem);
        }

        var deterministicPeek = storage.TryPeekProvidedItem(storage.GetOutputCell(), _simulation, out var peekedItem)
            && peekedItem?.Id == injectedItems[0].Id;
        var deterministicTake = storage.TryTakeProvidedItem(storage.GetOutputCell(), _simulation, out var takenItem)
            && takenItem?.Id == injectedItems[0].Id;

        var stackedBuffered = false;
        var storageDetail = storage.GetDetailModel();
        if (storageDetail.InventorySections.Count > 0)
        {
            for (var index = 0; index < storageDetail.InventorySections[0].Slots.Count; index++)
            {
                if (storageDetail.InventorySections[0].Slots[index].StackCount > 1)
                {
                    stackedBuffered = true;
                    break;
                }
            }
        }

        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        var bufferedBefore = storage.BufferedCount;
        var deliveredBefore = sink.DeliveredTotal;

        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        var deliveredAfter = sink.DeliveredTotal;

        var passed = deterministicPeek
            && deterministicTake
            && stackedBuffered
            && bufferedBefore >= 0
            && deliveredAfter > 0;
        if (!passed)
        {
            GD.Print($"FACTORY_STORAGE_SMOKE deterministicPeek={deterministicPeek} deterministicTake={deterministicTake} stackedBuffered={stackedBuffered} bufferedBefore={bufferedBefore} deliveredBefore={deliveredBefore} deliveredAfter={deliveredAfter}");
        }

        return passed;
    }

    private bool VerifyStorageInspectionPanel()
    {
        if (_grid is null || _hud is null)
        {
            return false;
        }

        EnterInteractionMode();
        if (!_grid.TryGetStructure(new Vector2I(17, 2), out var structure) || structure is null)
        {
            return false;
        }

        _selectedStructure = structure;
        UpdateHud();

        return _hud.InspectionTitleText.Contains("仓储", global::System.StringComparison.Ordinal)
            && _hud.InspectionBodyText.Contains("容量", global::System.StringComparison.Ordinal)
            && _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains("仓储", global::System.StringComparison.Ordinal);
    }

    private async Task<bool> RunStructureDetailSmoke()
    {
        if (_grid is null || _hud is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(26, 26),
            new Vector2I(27, 26),
            new Vector2I(26, 28),
            new Vector2I(26, 32),
            new Vector2I(29, 32)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        var feederStorage = PlaceStructure(BuildPrototypeKind.Storage, 26, 26, FacingDirection.East) as StorageStructure;
        var storage = PlaceStructure(BuildPrototypeKind.Storage, 27, 26, FacingDirection.East) as StorageStructure;
        var generator = PlaceStructure(BuildPrototypeKind.Generator, 24, 28, FacingDirection.East) as GeneratorStructure;
        PlaceStructure(BuildPrototypeKind.PowerPole, 25, 29, FacingDirection.East);
        var recipeAssembler = PlaceStructure(BuildPrototypeKind.Assembler, 26, 28, FacingDirection.East) as AssemblerStructure;
        var ammoAssembler = PlaceStructure(BuildPrototypeKind.AmmoAssembler, 26, 32, FacingDirection.East) as AmmoAssemblerStructure;
        var turret = PlaceStructure(BuildPrototypeKind.GunTurret, 29, 32, FacingDirection.East) as GunTurretStructure;

        if (feederStorage is null
            || storage is null
            || generator is null
            || recipeAssembler is null
            || ammoAssembler is null
            || turret is null)
        {
            return false;
        }

        generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
        generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);

        var assemblerRecipeChanged = recipeAssembler.TrySetDetailRecipe("gear");
        var ammoRecipeChanged = ammoAssembler.TrySetDetailRecipe("high-velocity-ammo");

        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);

        var stackLimit = FactoryItemCatalog.GetMaxStackSize(FactoryItemKind.GenericCargo);
        for (var index = 0; index < stackLimit + 2; index++)
        {
            var seededCargo = _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo);
            if (!storage.TryReceiveProvidedItem(seededCargo, storage.Cell + Vector2I.Left, _simulation))
            {
                return false;
            }
        }

        feederStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), feederStorage.Cell + Vector2I.Left, _simulation);
        recipeAssembler.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate), GetPrimaryInputCell(recipeAssembler), _simulation);
        recipeAssembler.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate), GetPrimaryInputCell(recipeAssembler), _simulation);

        if (turret.BufferedAmmo <= 0)
        {
            var injectedAmmo = _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.HighVelocityAmmo);
            turret.TryReceiveProvidedItem(injectedAmmo, GetPrimaryInputCell(turret), _simulation);
        }

        _selectedStructure = storage;
        UpdateHud();
        var storageDetailVisible = _hud.IsDetailVisible && _hud.DetailTitleText.Contains("仓储", global::System.StringComparison.Ordinal);

        var storageDetail = storage.GetDetailModel();
        var storageSection = storageDetail.InventorySections.Count > 0 ? storageDetail.InventorySections[0] : null;
        var mergeSourceSlot = new Vector2I(-1, -1);
        var mergeTargetSlot = new Vector2I(-1, -1);
        var emptySlot = new Vector2I(-1, -1);
        var targetStackBeforeMove = 0;
        var totalStackCountBeforeMove = 0;
        var stackCountsVisible = false;
        if (storageSection is null)
        {
            return false;
        }

        for (var index = 0; index < storageSection.Slots.Count; index++)
        {
            var slot = storageSection.Slots[index];
            totalStackCountBeforeMove += slot.StackCount;
            if (slot.StackCount > 1)
            {
                stackCountsVisible = true;
            }

            if (!slot.HasItem && emptySlot.X < 0)
            {
                emptySlot = slot.Position;
            }

            if (!slot.HasItem)
            {
                continue;
            }

            if (slot.StackCount < slot.MaxStackSize && mergeTargetSlot.X < 0)
            {
                mergeTargetSlot = slot.Position;
                targetStackBeforeMove = slot.StackCount;
                continue;
            }

            if (mergeTargetSlot.X >= 0
                && slot.ItemKind == FactoryItemKind.GenericCargo
                && slot.Position != mergeTargetSlot
                && mergeSourceSlot.X < 0)
            {
                mergeSourceSlot = slot.Position;
            }
        }

        if (mergeTargetSlot.X >= 0 && mergeSourceSlot.X < 0)
        {
            for (var index = 0; index < storageSection.Slots.Count; index++)
            {
                var slot = storageSection.Slots[index];
                if (slot.HasItem
                    && slot.ItemKind == FactoryItemKind.GenericCargo
                    && slot.Position != mergeTargetSlot)
                {
                    mergeSourceSlot = slot.Position;
                    break;
                }
            }
        }

        var emptyDragRejected = mergeTargetSlot.X >= 0
            && emptySlot.X >= 0
            && !storage.TryMoveDetailInventoryItem("storage-buffer", emptySlot, mergeTargetSlot);
        var storageMoved = mergeSourceSlot.X >= 0
            && mergeTargetSlot.X >= 0
            && storage.TryMoveDetailInventoryItem("storage-buffer", mergeSourceSlot, mergeTargetSlot);

        var movedDetail = storage.GetDetailModel();
        var movedSection = movedDetail.InventorySections[0];
        var movedTargetStackCount = 0;
        var movedTotalStackCount = 0;
        var splitSourceSlot = new Vector2I(-1, -1);
        var splitSourceCountBefore = 0;
        for (var index = 0; index < movedSection.Slots.Count; index++)
        {
            var slot = movedSection.Slots[index];
            movedTotalStackCount += slot.StackCount;
            if (slot.Position == mergeTargetSlot)
            {
                movedTargetStackCount = slot.StackCount;
            }

            if (slot.HasItem && slot.StackCount > 1 && slot.Position != emptySlot && splitSourceSlot.X < 0)
            {
                splitSourceSlot = slot.Position;
                splitSourceCountBefore = slot.StackCount;
            }
        }

        var splitMoveWorked = splitSourceSlot.X >= 0
            && emptySlot.X >= 0
            && storage.TryMoveDetailInventoryItem("storage-buffer", splitSourceSlot, emptySlot, true);

        var splitDetail = storage.GetDetailModel();
        var splitTotalStackCount = 0;
        var splitTargetCount = 0;
        var splitSourceCountAfter = 0;
        for (var index = 0; index < splitDetail.InventorySections[0].Slots.Count; index++)
        {
            var slot = splitDetail.InventorySections[0].Slots[index];
            splitTotalStackCount += slot.StackCount;
            if (slot.Position == emptySlot)
            {
                splitTargetCount = slot.StackCount;
            }
            else if (slot.Position == splitSourceSlot)
            {
                splitSourceCountAfter = slot.StackCount;
            }
        }

        await ToSignal(GetTree().CreateTimer(1.8f), SceneTreeTimer.SignalName.Timeout);

        _selectedStructure = recipeAssembler;
        UpdateHud();
        var recipeOutputCells = recipeAssembler.GetOutputCells();
        recipeAssembler.TryPeekProvidedItem(recipeOutputCells.Count > 0 ? recipeOutputCells[0] : recipeAssembler.GetOutputCell(), _simulation, out var producedItem);
        var assemblerRecipeVerified = assemblerRecipeChanged
            && _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains("组装机", global::System.StringComparison.Ordinal)
            && producedItem?.ItemKind == FactoryItemKind.Gear;

        _selectedStructure = turret;
        UpdateHud();
        var turretDetail = turret.GetDetailModel();
        var turretHasAmmo = turret.BufferedAmmo > 0;
        var turretShowsHighVelocityAmmo = false;
        if (turretDetail.InventorySections.Count > 0)
        {
            var ammoSlots = turretDetail.InventorySections[0].Slots;
            for (var index = 0; index < ammoSlots.Count; index++)
            {
                if (ammoSlots[index].ItemLabel?.Contains("高速弹药", global::System.StringComparison.Ordinal) ?? false)
                {
                    turretShowsHighVelocityAmmo = true;
                    break;
                }
            }
        }

        var passed = storageDetailVisible
            && stackCountsVisible
            && emptyDragRejected
            && storageMoved
            && movedTargetStackCount > targetStackBeforeMove
            && movedTotalStackCount == totalStackCountBeforeMove
            && splitMoveWorked
            && splitTargetCount > 0
            && splitTargetCount < splitSourceCountBefore
            && splitSourceCountAfter > 0
            && splitTotalStackCount == totalStackCountBeforeMove
            && assemblerRecipeVerified
            && ammoRecipeChanged
            && turretHasAmmo
            && turretShowsHighVelocityAmmo;
        if (!passed)
        {
            GD.Print($"FACTORY_DETAIL_SMOKE storageDetailVisible={storageDetailVisible} stackCountsVisible={stackCountsVisible} emptyDragRejected={emptyDragRejected} storageMoved={storageMoved} movedTargetStackCount={movedTargetStackCount} targetStackBeforeMove={targetStackBeforeMove} movedTotalStackCount={movedTotalStackCount} totalStackCountBeforeMove={totalStackCountBeforeMove} splitMoveWorked={splitMoveWorked} splitTargetCount={splitTargetCount} splitSourceCountBefore={splitSourceCountBefore} splitSourceCountAfter={splitSourceCountAfter} splitTotalStackCount={splitTotalStackCount} assemblerRecipeVerified={assemblerRecipeVerified} ammoRecipeChanged={ammoRecipeChanged} turretHasAmmo={turretHasAmmo} turretShowsHighVelocityAmmo={turretShowsHighVelocityAmmo}");
        }

        return passed;
    }

    private async Task<bool> VerifyCombatScenarios()
    {
        if (_grid is null || _simulation is null || _hud is null)
        {
            return false;
        }

        _grid.TryGetStructure(new Vector2I(16, 14), out var breachWallStructure);
        var breachWall = breachWallStructure as WallStructure;

        await ToSignal(GetTree().CreateTimer(20.0f), SceneTreeTimer.SignalName.Timeout);

        var totalTurretShots = 0;
        var totalHeavyTurretShots = 0;
        for (var x = FactoryConstants.GridMin; x <= FactoryConstants.GridMax; x++)
        {
            for (var y = FactoryConstants.GridMin; y <= FactoryConstants.GridMax; y++)
            {
                if (_grid.TryGetStructure(new Vector2I(x, y), out var structure) && structure is GunTurretStructure turret)
                {
                    totalTurretShots += turret.ShotsFired;
                }
                else if (_grid.TryGetStructure(new Vector2I(x, y), out structure) && structure is HeavyGunTurretStructure heavyTurret)
                {
                    totalHeavyTurretShots += heavyTurret.ShotsFired;
                }
            }
        }

        var combatPressureVisible = totalTurretShots > 0
            || totalHeavyTurretShots > 0
            || _simulation.ActiveEnemyCount > 0
            || _simulation.DefeatedEnemyCount > 0
            || _simulation.DestroyedStructureCount > 0;
        var heavyProjectileVerified = totalHeavyTurretShots > 0
            || _simulation.ActiveProjectileCount > 0
            || _simulation.TotalProjectileLaunchCount > 0;
        var breachOccurred = breachWall is null
            || !GodotObject.IsInstanceValid(breachWall)
            || breachWall.CurrentHealth < breachWall.MaxHealth
            || _simulation.DestroyedStructureCount > 0;

        GD.Print($"FACTORY_COMBAT_SMOKE totalTurretShots={totalTurretShots} heavyTurretShots={totalHeavyTurretShots} activeProjectiles={_simulation.ActiveProjectileCount} totalProjectileLaunches={_simulation.TotalProjectileLaunchCount} kills={_simulation.DefeatedEnemyCount} activeEnemies={_simulation.ActiveEnemyCount} destroyedStructures={_simulation.DestroyedStructureCount} breachWallPresent={breachWall is not null} breachWallHealth={(breachWall is not null && GodotObject.IsInstanceValid(breachWall) ? breachWall.CurrentHealth : -1.0f)}");

        return combatPressureVisible && heavyProjectileVerified;
    }
}
