using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
public partial class FactoryDemo
{
        private static bool RunFactoryMapSmokeChecks()
    {
        return FactoryMapSmokeSupport.VerifyDocuments(FactoryMapPaths.StaticSandboxWorld);
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
        var previewArrowReady = _previewArrow is not null && _previewArrow.GetChildCount() >= 3;
        var playerInteractionVerified = await RunPlayerCharacterSmoke(probeCell);

        var initialStructureCount = _simulation.RegisteredStructureCount;
        var poweredFactoryVerified = await RunPoweredFactorySmoke();
        var sinkStats = CollectSinkStats();
        ConfigureCombatScenarios();
        var profilerText = _hud?.ProfilerText ?? string.Empty;
        var splitterFallbackRecovered = await RunSplitterFallbackSmoke();
        var bridgeLaneRecovered = await RunBridgeLaneIndependenceSmoke();
        var storageFlowVerified = await RunStorageInserterSmoke();
        var inspectionVerified = VerifyStorageInspectionPanel();
        var detailWindowVerified = await RunStructureDetailSmoke();
        var blueprintVerified = RunBlueprintWorkflowSmoke();
        var workspaceVerified = RunWorkspaceNavigationSmoke();
        var itemVisualProfilesVerified = RunItemVisualProfileSmoke();
        var structureVisualProfilesVerified = RunStructureVisualProfileSmoke();
        var mapFormatVerified = RunFactoryMapSmokeChecks();
        var combatVerified = await VerifyCombatScenarios();

        if (!placed
            || !removed
            || initialStructureCount < 40
            || !poweredFactoryVerified
            || sinkStats.deliveredTotal <= 0
            || string.IsNullOrWhiteSpace(profilerText)
            || !profilerText.Contains("FPS", global::System.StringComparison.Ordinal)
            || !splitterFallbackRecovered
            || !bridgeLaneRecovered
            || !storageFlowVerified
            || !inspectionVerified
            || !detailWindowVerified
            || !blueprintVerified
              || !workspaceVerified
              || !itemVisualProfilesVerified
              || !structureVisualProfilesVerified
              || !mapFormatVerified
              || !combatVerified
              || !multiCellPlacementVerified
            || !assemblerPortPreviewVerified
            || !previewArrowReady
            || !playerInteractionVerified)
        {
              GD.PushError($"FACTORY_SMOKE_FAILED placed={placed} removed={removed} multiCell={multiCellPlacementVerified} assemblerPortPreview={assemblerPortPreviewVerified} playerInteraction={playerInteractionVerified} structures={initialStructureCount} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} profiler={(!string.IsNullOrWhiteSpace(profilerText))} splitterFallback={splitterFallbackRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} workspace={workspaceVerified} itemVisualProfiles={itemVisualProfilesVerified} structureVisualProfiles={structureVisualProfilesVerified} mapFormat={mapFormatVerified} combat={combatVerified} previewArrowReady={previewArrowReady}");
            GetTree().Quit(1);
            return;
        }

          GD.Print($"FACTORY_SMOKE_OK structures={initialStructureCount} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} splitterFallback={splitterFallbackRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} workspace={workspaceVerified} itemVisualProfiles={itemVisualProfilesVerified} structureVisualProfiles={structureVisualProfilesVerified} mapFormat={mapFormatVerified} combat={combatVerified} multiCell={multiCellPlacementVerified} assemblerPortPreview={assemblerPortPreviewVerified} previewArrowReady={previewArrowReady} playerInteraction={playerInteractionVerified}");
        GetTree().Quit();
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

        EnterInteractionMode();
        return eastContractVerified && beltHintsVisible && southPreviewSizeVerified && nonBeltHintsHidden;
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
        var required = new[] { BuildWorkspaceId, BlueprintWorkspaceId, TelemetryWorkspaceId, CombatWorkspaceId, TestingWorkspaceId };
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

        _hud.SelectWorkspace(BuildWorkspaceId);
        var buildVisible = _hud.ActiveWorkspaceId == BuildWorkspaceId && _hud.IsWorkspaceVisible(BuildWorkspaceId);

        return blueprintVisible
            && telemetryVisible
            && combatVisible
            && testingVisible
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
        var ammoAssemblerFound = _grid.TryGetStructure(new Vector2I(-26, -20), out var ammoAssemblerStructure) && ammoAssemblerStructure is AmmoAssemblerStructure;
        var maintenanceGeneratorFound = _grid.TryGetStructure(new Vector2I(10, 8), out var maintenanceGeneratorStructure) && maintenanceGeneratorStructure is GeneratorStructure;
        var batteryAssemblerFound = _grid.TryGetStructure(new Vector2I(14, 2), out var batteryAssemblerStructure) && batteryAssemblerStructure is AssemblerStructure;
        var maintenanceSinkFound = _grid.TryGetStructure(new Vector2I(23, 2), out var maintenanceSinkStructure) && maintenanceSinkStructure is SinkStructure;
        var successStorageFound = _grid.TryGetStructure(new Vector2I(13, 20), out var successStorageStructure) && successStorageStructure is StorageStructure;
        var successTurretFound = _grid.TryGetStructure(new Vector2I(15, 20), out var successTurretStructure) && successTurretStructure is GunTurretStructure;
        if (!coalDrillFound || !generatorFound || !ironDrillFound || !copperDrillFound || !ironSmelterFound || !copperSmelterFound || !wireAssemblerFound || !ammoAssemblerFound || !maintenanceGeneratorFound || !batteryAssemblerFound || !maintenanceSinkFound || !successStorageFound || !successTurretFound)
        {
            GD.Print($"FACTORY_POWERED_SMOKE_MISSING coalDrill={coalDrillFound} generator={generatorFound} ironDrill={ironDrillFound} copperDrill={copperDrillFound} ironSmelter={ironSmelterFound} copperSmelter={copperSmelterFound} wireAssembler={wireAssemblerFound} ammoAssembler={ammoAssemblerFound} maintenanceGenerator={maintenanceGeneratorFound} batteryAssembler={batteryAssemblerFound} maintenanceSink={maintenanceSinkFound} successStorage={successStorageFound} successTurret={successTurretFound}");
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
        var maintenanceSink = (SinkStructure)maintenanceSinkStructure!;
        var successStorage = (StorageStructure)successStorageStructure!;
        var successTurret = (GunTurretStructure)successTurretStructure!;

        await ToSignal(GetTree().CreateTimer(40.0f), SceneTreeTimer.SignalName.Timeout);
        var ironSummary = ironSmelter.GetDetailModel().SummaryLines;
        var copperSummary = copperSmelter.GetDetailModel().SummaryLines;
        var wireSummary = wireAssembler.GetDetailModel().SummaryLines;
        var ammoSummary = ammoAssembler.GetDetailModel().SummaryLines;
        var batterySummary = batteryAssembler.GetDetailModel().SummaryLines;
        var totalDeliveredToSinks = 0;
        for (var x = _grid.MinCell.X; x <= _grid.MaxCell.X; x++)
        {
            for (var y = _grid.MinCell.Y; y <= _grid.MaxCell.Y; y++)
            {
                if (_grid.TryGetStructure(new Vector2I(x, y), out var structure) && structure is SinkStructure sink)
                {
                    totalDeliveredToSinks += sink.DeliveredTotal;
                }
            }
        }

        var successStorageHasAmmo = false;
        var successStorageDetail = successStorage.GetDetailModel();
        if (successStorageDetail.InventorySections.Count > 0)
        {
            var slots = successStorageDetail.InventorySections[0].Slots;
            for (var index = 0; index < slots.Count; index++)
            {
                if ((slots[index].ItemLabel?.Contains("弹药", global::System.StringComparison.Ordinal) ?? false)
                    && slots[index].StackCount > 0)
                {
                    successStorageHasAmmo = true;
                    break;
                }
            }
        }

        var verified = coalDrill.ResourceKind == FactoryResourceKind.Coal
            && ironDrill.ResourceKind == FactoryResourceKind.IronOre
            && copperDrill.ResourceKind == FactoryResourceKind.CopperOre
            && totalDeliveredToSinks > 0
            && (generator.IsGenerating || generator.HasFuelBuffered)
            && ContainsSummaryLine(ironSummary, "铁板")
            && ContainsSummaryLine(copperSummary, "铜板")
            && ContainsSummaryLine(wireSummary, "铜线")
            && ContainsSummaryLine(ammoSummary, "弹药")
            && (maintenanceGenerator.IsGenerating || maintenanceGenerator.HasFuelBuffered)
            && maintenanceSink.DeliveredTotal > 0
            && ContainsSummaryLine(batterySummary, "电池组")
            && (successStorageHasAmmo || successTurret.BufferedAmmo > 0 || successTurret.ShotsFired > 0);

        if (!verified)
        {
            GD.Print($"FACTORY_POWERED_SMOKE coalKind={coalDrill.ResourceKind} ironKind={ironDrill.ResourceKind} copperKind={copperDrill.ResourceKind} totalSinks={totalDeliveredToSinks} generator={generator.IsGenerating} generatorFuel={generator.HasFuelBuffered} maintenanceGenerator={maintenanceGenerator.IsGenerating} maintenanceFuel={maintenanceGenerator.HasFuelBuffered} maintenanceSink={maintenanceSink.DeliveredTotal} successStorageHasAmmo={successStorageHasAmmo} successShots={successTurret.ShotsFired} ironSummary={string.Join('|', ironSummary)} copperSummary={string.Join('|', copperSummary)} wireSummary={string.Join('|', wireSummary)} ammoSummary={string.Join('|', ammoSummary)} batterySummary={string.Join('|', batterySummary)}");
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

    private bool RunItemVisualProfileSmoke()
    {
        var placeholderVisual = FactoryTransportVisualFactory.CreateVisual(new FactoryItem(-101, BuildPrototypeKind.MiningDrill, FactoryItemKind.IronOre), FactoryConstants.CellSize);
        var billboardVisual = FactoryTransportVisualFactory.CreateVisual(new FactoryItem(-102, BuildPrototypeKind.Assembler, FactoryItemKind.CopperWire), FactoryConstants.CellSize);
        var modelVisual = FactoryTransportVisualFactory.CreateVisual(new FactoryItem(-103, BuildPrototypeKind.Assembler, FactoryItemKind.AmmoMagazine), FactoryConstants.CellSize);

        var placeholderMesh = FindFirstMesh(placeholderVisual);
        var billboardMesh = FindFirstMesh(billboardVisual);
        var modelMeshCount = CountMeshes(modelVisual);
        var distinctBaselineColors =
            !FactoryItemCatalog.GetAccentColor(FactoryItemKind.Coal).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.IronOre))
            && !FactoryItemCatalog.GetAccentColor(FactoryItemKind.IronOre).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.CopperOre))
            && !FactoryItemCatalog.GetAccentColor(FactoryItemKind.AmmoMagazine).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.HighVelocityAmmo));
        var iconsPresent =
            FactoryItemCatalog.GetIconTexture(FactoryItemKind.IronOre) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.IronPlate) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.CopperWire) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.HighVelocityAmmo) is not null;

        placeholderVisual.QueueFree();
        billboardVisual.QueueFree();
        modelVisual.QueueFree();

        return placeholderMesh?.Mesh is BoxMesh
            && billboardMesh?.Mesh is QuadMesh
            && billboardMesh.MaterialOverride is StandardMaterial3D billboardMaterial
            && billboardMaterial.BillboardMode == BaseMaterial3D.BillboardModeEnum.Enabled
            && modelMeshCount >= 2
            && distinctBaselineColors
            && iconsPresent;
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

        authoredController.Root.Free();
        fallbackController.Root.Free();
        genericController.Root.Free();
        legacyController.Root.Free();
        smelterController.Root.Free();

        return authoredResolved
            && fallbackResolved
            && genericResolved
            && legacyResolved
            && smelterHotCoolVerified;
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
