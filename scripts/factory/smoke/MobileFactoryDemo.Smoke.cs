using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
public partial class MobileFactoryDemo
{
    private const float MiningPortSmokeDeployTimeoutSeconds = 24.0f;
    private static bool RunFactoryMapSmokeChecks()
    {
        return FactoryMapSmokeSupport.VerifyTargets(
            FactoryMapValidationCatalog.FocusedMobileBundleTargetId,
            FactoryMapValidationCatalog.DualStandardsMobileBundleTargetId);
    }
    private static Vector2I GetPrimaryInputCell(FactoryStructure structure)
    {
        var inputCells = structure.GetInputCells();
        return inputCells.Count > 0 ? inputCells[0] : structure.Cell + Vector2I.Left;
    }
    private static bool HasFocusedSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--mobile-factory-smoke-test", global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMiningPortSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--mobile-factory-mining-port-smoke-test", global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasLargeScenarioSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--mobile-factory-large-smoke-test", global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async void RunSmokeChecks()
    {
        if (_grid is null || _mobileFactory is null || _sinkA is null || _sinkB is null || _hud is null || _cameraRig is null || _simulation is null || _playerController is null || _playerHud is null)
        {
            GD.PushError("MOBILE_FACTORY_SMOKE_FAILED missing grid, factory, player, hud, camera, or sinks.");
            GetTree().Quit(1);
            return;
        }

        await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
        var startsInPlayerMode = _controlMode == MobileFactoryControlMode.Player;
        var playerHudReady = _playerHud is not null;
        var playerStartPosition = _playerController.GlobalPosition;
        Input.ActionPress("player_move_right");
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        Input.ActionRelease("player_move_right");
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var playerMoved = _playerController.GlobalPosition.DistanceTo(playerStartPosition) > 0.2f;
        var cameraFollowedPlayer = new Vector2(_cameraRig.Position.X, _cameraRig.Position.Z)
            .DistanceTo(new Vector2(_playerController.GlobalPosition.X, _playerController.GlobalPosition.Z)) < 1.8f;
        var debugWorldSupportVerified = await RunDebugWorldSupportSmoke();
        var playerWorldPlacementWorked = false;
        if (TryArmPlayerWorldBuildForSmoke(out var smokeBuildKind))
        {
            _selectedWorldFacing = FacingDirection.East;
            var playerWorldBuildCell = FindPlayerWorldBuildSmokeCell(smokeBuildKind, _selectedWorldFacing);
            if (playerWorldBuildCell is Vector2I worldBuildCell)
            {
                _hoveredWorldCell = worldBuildCell;
                _hasHoveredWorldCell = true;
                HandlePlayerWorldPrimaryPress();
                HandlePlayerWorldPrimaryRelease();
                playerWorldPlacementWorked = _grid.TryGetStructure(worldBuildCell, out var placedWorldStructure)
                    && placedWorldStructure is not null
                    && placedWorldStructure.Kind == smokeBuildKind;
                CancelPlayerWorldPlacement();
            }
        }

        ToggleFactoryCommandMode();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var commandActive = _controlMode == MobileFactoryControlMode.FactoryCommand;
        var cameraLockedInCommand = !_cameraRig.AllowPanInput;
        var initialPosition = _mobileFactory.WorldFocusPoint;
        _mobileFactory.ApplyTransitInput(_grid, 1.0f, -1.0f, 0.5);
        var movedInTransit = _mobileFactory.WorldFocusPoint.DistanceTo(initialPosition) > 0.05f;
        ToggleFactoryCommandMode();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var returnedToPlayerFromCommand = _controlMode == MobileFactoryControlMode.Player;

        ToggleObserverMode();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var observerActive = _controlMode == MobileFactoryControlMode.Observer;
        var observerCameraActive = _cameraRig.AllowPanInput;
        ToggleObserverMode();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var returnedToPlayerFromObserver = _controlMode == MobileFactoryControlMode.Player;

        ToggleDeployPreview();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var deployPreviewEntered = _controlMode == MobileFactoryControlMode.DeployPreview;
        ToggleDeployPreview();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var returnedToPlayerFromDeploy = _controlMode == MobileFactoryControlMode.Player;

        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        SetEditorOpenState(false);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var editRestoresPlayerMode = _controlMode == MobileFactoryControlMode.Player;

        SetControlMode(MobileFactoryControlMode.FactoryCommand);
        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        SetEditorOpenState(false);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var editRestoresFactoryMode = _controlMode == MobileFactoryControlMode.FactoryCommand;

        SetControlMode(MobileFactoryControlMode.Observer);
        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        SetEditorOpenState(false);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var editRestoresObserverMode = _controlMode == MobileFactoryControlMode.Observer;

        SetControlMode(MobileFactoryControlMode.DeployPreview);
        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        SetEditorOpenState(false);
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var editRestoresDeployMode = _controlMode == MobileFactoryControlMode.DeployPreview;
        SetControlMode(MobileFactoryControlMode.Player);

        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
        var interiorRunsInTransit = _mobileFactory.InteriorSite.IsSimulationActive;
        var inputTransitBaseline = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.InputPort);
        _mobileFactory.TryGetInteriorStructure(FocusedTurretCell, out var escortTurretStructure);
        var escortTurret = escortTurretStructure as GunTurretStructure;
        var turretShotsBeforeDeploy = escortTurret?.ShotsFired ?? 0;

        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);

        var openedInTransit = _hud.IsEditorVisible;
        var workspaceNavigationVerified = await RunWorkspaceNavigationSmoke();
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var operationPanelHover = _hud.IsPointerOverEditorOperationPanel(new Vector2(viewportSize.X - 32.0f, viewportSize.Y * 0.5f));
        var editorViewportHover = _hud.IsPointerOverEditorViewport(new Vector2(viewportSize.X * 0.62f, viewportSize.Y * 0.52f));
        var worldHover = !_hud.IsPointerOverEditor(new Vector2(10.0f, 40.0f));
        var detailWindowInTransit = await RunEditorDetailSmoke();
        var worldDetailInTransit = await RunWorldDetailSmoke();
        var blueprintWorkflowInTransit = await RunInteriorBlueprintSmoke();
        var multiCellInteriorVerified = RunInteriorMultiCellSmoke();
        var debugInteriorSupportVerified = await RunDebugInteriorSupportSmoke();
        PrimeMobileFactoryShowcase(_mobileFactory);
        await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
        inputTransitBaseline = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.InputPort);
        _mobileFactory.TryGetInteriorStructure(FocusedTurretCell, out escortTurretStructure);
        escortTurret = escortTurretStructure as GunTurretStructure;
        turretShotsBeforeDeploy = escortTurret?.ShotsFired ?? 0;

        var placedInterior = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.Splitter, new Vector2I(2, 0), FacingDirection.East);
        var interiorPlacedExists = _mobileFactory.TryGetInteriorStructure(new Vector2I(2, 0), out var placedStructure) && placedStructure is SplitterStructure;
        var placedInteriorSink = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.Sink, new Vector2I(3, 0), FacingDirection.East);
        var interiorSinkExists = _mobileFactory.TryGetInteriorStructure(new Vector2I(3, 0), out var sinkStructure) && sinkStructure is SinkStructure;
        FactoryStructure? unpackerStructure = null;
        FactoryStructure? packerStructure = null;
        var cabinPresentationVerified =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(2, 2), out unpackerStructure)
            && unpackerStructure is CargoUnpackerStructure
            && CountNamedNodes(unpackerStructure, "UnpackerChamberBaseSkid") > 0
            && CountNamedNodes(unpackerStructure, "UnpackerCradle") > 0
            && CountNamedNodes(unpackerStructure, "ProcessingPayloadAnchor") > 0
            && _mobileFactory.TryGetInteriorStructure(new Vector2I(4, 3), out var conversionBufferStructure)
            && conversionBufferStructure is TransferBufferStructure
            && CountNamedNodes(conversionBufferStructure, "BufferCradle") > 0
            && CountNamedNodes(conversionBufferStructure, "BufferPayloadAnchor") > 0
            && _mobileFactory.TryGetInteriorStructure(new Vector2I(5, 2), out packerStructure)
            && packerStructure is CargoPackerStructure
            && CountNamedNodes(packerStructure, "PackerChamberBaseSkid") > 0
            && CountNamedNodes(packerStructure, "PackerInputLane0Tray") > 0
            && CountNamedNodes(packerStructure, "PackerInputLane1Tray") > 0
            && CountNamedNodes(packerStructure, "PackerCompressionDeck") > 0
            && CountNamedNodes(packerStructure, "DispatchPayloadAnchor") > 0
            &&
            _mobileFactory.TryGetInteriorStructure(FocusedSmelterCell, out var smelterStructure)
            && smelterStructure is SmelterStructure
            && CountNamedNodes(smelterStructure, "CabinLabelPlate") > 0
            && CountNamedNodes(smelterStructure, "SmelterCabinShell") > 0
            && _mobileFactory.TryGetInteriorStructure(FocusedIronBufferCell, out var bufferStructure)
            && bufferStructure is StorageStructure
            && CountNamedNodes(bufferStructure, "StorageCabinetShell") > 0
            && escortTurret is not null
            && CountNamedNodes(escortTurret, "Well") > 0;
        var unpackerRecipeSection = (unpackerStructure as CargoUnpackerStructure)?.GetDetailModel().RecipeSection;
        var packerRecipeSection = (packerStructure as CargoPackerStructure)?.GetDetailModel().RecipeSection;
        var unpackerHasAutoRecipeOption = false;
        if (unpackerRecipeSection is not null)
        {
            for (var recipeIndex = 0; recipeIndex < unpackerRecipeSection.Options.Count; recipeIndex++)
            {
                if (unpackerRecipeSection.Options[recipeIndex].RecipeId == "cargo-unpacker-auto")
                {
                    unpackerHasAutoRecipeOption = true;
                    break;
                }
            }
        }
        var packerHasAutoRecipeOption = false;
        if (packerRecipeSection is not null)
        {
            for (var recipeIndex = 0; recipeIndex < packerRecipeSection.Options.Count; recipeIndex++)
            {
                if (packerRecipeSection.Options[recipeIndex].RecipeId == "cargo-packer-auto")
                {
                    packerHasAutoRecipeOption = true;
                    break;
                }
            }
        }
        var bundleTemplateChainConfigured =
            unpackerStructure is CargoUnpackerStructure configuredUnpacker
            && packerStructure is CargoPackerStructure configuredPacker
            && configuredUnpacker.CaptureBlueprintConfiguration().TryGetValue("bundle_template_id", out var unpackerTemplateId)
            && unpackerTemplateId == "bulk-iron-ore-standard"
            && configuredPacker.CaptureBlueprintConfiguration().TryGetValue("bundle_template_id", out var packerTemplateId)
            && packerTemplateId == "packed-iron-plate-standard"
            && unpackerRecipeSection is not null
            && unpackerRecipeSection.ActiveRecipeId == "bulk-iron-ore-standard"
            && unpackerRecipeSection.Options.Count > 1
            && unpackerHasAutoRecipeOption
            && packerRecipeSection is not null
            && packerRecipeSection.ActiveRecipeId == "packed-iron-plate-standard"
            && packerRecipeSection.Options.Count > 1
            && packerHasAutoRecipeOption;
        var worldBundleBlockedOnInteriorBelt =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(3, 7), out var interiorBeltStructure)
            && interiorBeltStructure is BeltStructure interiorBelt
            && !interiorBelt.CanAcceptItem(
                _simulation.CreateItem(
                    FactorySiteKind.World,
                    BuildPrototypeKind.MiningDrill,
                    FactoryItemKind.IronOre,
                    FactoryCargoForm.WorldBulk,
                    "bulk-iron-ore-standard"),
                interiorBelt.Cell + Vector2I.Left,
                _simulation);
        var placedSplitterPresentationVerified = placedStructure is not null
            && CountNamedNodes(placedStructure, "SplitterLamp") > 0;
        var miniatureSyncedInTransit = placedStructure is not null
            && placedStructure.GlobalPosition.DistanceTo(_mobileFactory.InteriorSite.CellToWorld(new Vector2I(2, 0))) < 0.05f;
        var inputBlockedInTransit = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.InputPort) == inputTransitBaseline;
        var projectionConflictAnchor = new Vector2I(-10, -8);
        var placedConflictBlocker = false;
        foreach (var projectionConflictCell in _mobileFactory.GetPortCells(projectionConflictAnchor, FacingDirection.East))
        {
            if (PlaceWorldStructure(BuildPrototypeKind.Sink, projectionConflictCell, FacingDirection.East) is not null)
            {
                placedConflictBlocker = true;
                break;
            }
        }
        var blockedDeploy = placedConflictBlocker && !_mobileFactory.CanDeployAt(_grid, projectionConflictAnchor, FacingDirection.East);
        var edgeBlockedDeploy = !_mobileFactory.CanDeployAt(_grid, new Vector2I(GetWorldMaxCell(), GetWorldMaxCell()), FacingDirection.East);
        var eastPortCells = new HashSet<Vector2I>(_mobileFactory.GetPortCells(AnchorA, FacingDirection.East));
        var southPortCells = new HashSet<Vector2I>(_mobileFactory.GetPortCells(AnchorA, FacingDirection.South));
        var eastFootprintCells = new HashSet<Vector2I>(_mobileFactory.GetFootprintCells(AnchorA, FacingDirection.East));
        var westFootprintCells = new HashSet<Vector2I>(_mobileFactory.GetFootprintCells(AnchorA, FacingDirection.West));
        var facingAwareCells = eastPortCells.Count > 0
            && southPortCells.Count > 0
            && !southPortCells.SetEquals(eastPortCells)
            && eastFootprintCells.Count == westFootprintCells.Count
            && westFootprintCells.Contains(AnchorA);
        var mapFormatVerified = RunFactoryMapSmokeChecks();
        var bundleTemplateRulesVerified = RunBundleTemplateRulesSmoke();
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        HandleCommandSlot(MobileFactoryCommandSlot.Auxiliary);
        var contextualRotateWorks = _selectedDeployFacing == FacingDirection.South;
        _hoveredAnchor = AnchorA;
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, AnchorA, _selectedDeployFacing);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        UpdateWorldPreview();
        var previewArrowTracksFacing = _worldPreviewFacingArrow is not null
            && _worldPreviewFacingArrow.Visible
            && _worldPreviewFacingArrow.GetChildCount() >= 3
            && Mathf.Abs(_worldPreviewFacingArrow.Rotation.Y - FactoryDirection.ToYRotationRadians(_selectedDeployFacing)) <= 0.001f;
        _selectedDeployFacing = FacingDirection.East;
        _currentDeployEvaluation = null;

        SetEditorOpenState(false);
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = AnchorA;
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, AnchorA, FacingDirection.East);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.Deployed, 4.5f);
        var firstDeploy = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        var moveWhileDeployedStart = _mobileFactory.WorldFocusPoint;
        _mobileFactory.ApplyTransitInput(_grid, 1.0f, 1.0f, 0.6);
        var moveRejectedWhileDeployed = _mobileFactory.WorldFocusPoint.DistanceTo(moveWhileDeployedStart) < 0.01f;
        await ToSignal(GetTree().CreateTimer(4.5f), SceneTreeTimer.SignalName.Timeout);
        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        var openedWhileDeployed = _hud.IsEditorVisible;
        var portConnected = _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.OutputPort);
        var portOverlayConnected = _hud.PortStatusText.Contains("已连接");
        var boundaryInterfaceVerified = false;
        foreach (var attachment in _mobileFactory.BoundaryAttachments)
        {
            if (attachment is MobileFactoryOutputPortStructure outputAttachment)
            {
                boundaryInterfaceVerified |= CountNamedNodes(outputAttachment, "OutputLatch") > 0
                    && CountNamedNodes(outputAttachment, "HullMouth") > 0
                    && CountNamedNodes(outputAttachment, "BoundaryHandoffCradle") > 0
                    && CountNamedNodes(outputAttachment, "BoundaryDeckRailNorth") > 0;
            }
            else if (attachment is MobileFactoryInputPortStructure inputAttachment)
            {
                boundaryInterfaceVerified |= CountNamedNodes(inputAttachment, "InputReceiver") > 0
                    && CountNamedNodes(inputAttachment, "HullMouth") > 0
                    && CountNamedNodes(inputAttachment, "BoundaryHandoffCradle") > 0
                    && CountNamedNodes(inputAttachment, "BoundaryDeckRailNorth") > 0;
            }
        }
        var boundaryConnectorFlowVerified = _structureRoot is not null
            && CountNamedNodes(_structureRoot, "ConnectorTrackDeck") > 0
            && CountNamedNodes(_structureRoot, "ConnectorTrackRailNorth") > 0
            && CountNamedNodes(_structureRoot, "TransitPayloadAnchor") > 0;
        var miniatureSyncedDeployed = placedStructure is not null
            && placedStructure.GlobalPosition.DistanceTo(_mobileFactory.InteriorSite.CellToWorld(new Vector2I(2, 0))) < 0.05f;
        PrimeFocusedOutputPorts(_mobileFactory);
        var heavyHandoffVerified = await RunHeavyBufferedHandoffSmoke();
        var converterPortResolutionVerified = await RunPackerInserterPortResolutionSmoke()
            && await RunPackerBeltPortResolutionSmoke();
        var firstDeliveredBaseline = GetScenarioDeliveryTotal();
        await ToSignal(GetTree().CreateTimer(8.0f), SceneTreeTimer.SignalName.Timeout);
        var firstDelivered = GetScenarioDeliveryTotal() - firstDeliveredBaseline;
        var inputAttachmentTransit = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.InputPort);
        var inputDeliveredWhileDeployed = inputAttachmentTransit > inputTransitBaseline || firstDelivered > 0;
        var turretTrackedThreats = (escortTurret?.ShotsFired ?? 0) > turretShotsBeforeDeploy;
        var mobileCombatActive = _simulation.ActiveEnemyCount > 0 || _simulation.DefeatedEnemyCount > 0;

        SetEditorOpenState(false);
        var blockedOutputBeforeRecall = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.OutputPort, onlyDisconnected: true);
        var deployedPositionBeforeRecall = _mobileFactory.WorldFocusPoint;
        var recalled = _mobileFactory.ReturnToTransitMode();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 1.2f);
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        var blockedOutputAfterRecall = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.OutputPort, onlyDisconnected: true);
        var blockedOutputActive = blockedOutputAfterRecall > blockedOutputBeforeRecall;
        var stayedInPlaceAfterReturn = _mobileFactory.WorldFocusPoint.DistanceTo(deployedPositionBeforeRecall) < 0.05f;
        var reservationsReleased =
            _grid.CanReserveAll(_mobileFactory.GetFootprintCells(AnchorA, FacingDirection.East), _mobileFactory.ReservationOwnerId)
            && _grid.CanReserveAll(_mobileFactory.GetPortCells(AnchorA, FacingDirection.East), _mobileFactory.ReservationOwnerId);

        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = AnchorB;
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, AnchorB, FacingDirection.East);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.Deployed, 4.5f);
        var secondDeploy = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
        PrimeFocusedOutputPorts(_mobileFactory);
        var secondDeliveredBaseline = GetScenarioDeliveryTotal();
        await ToSignal(GetTree().CreateTimer(10.0f), SceneTreeTimer.SignalName.Timeout);
        var secondDelivered = GetScenarioDeliveryTotal() - secondDeliveredBaseline;

        if (!startsInPlayerMode || !playerHudReady || !playerMoved || !cameraFollowedPlayer || !debugWorldSupportVerified || !playerWorldPlacementWorked || !commandActive || !cameraLockedInCommand || !returnedToPlayerFromCommand || !observerActive || !returnedToPlayerFromObserver || !deployPreviewEntered || !returnedToPlayerFromDeploy || !editRestoresPlayerMode || !editRestoresFactoryMode || !editRestoresObserverMode || !editRestoresDeployMode || !interiorRunsInTransit || !movedInTransit || !openedInTransit || !workspaceNavigationVerified || !operationPanelHover || !editorViewportHover || !worldHover || !detailWindowInTransit || !worldDetailInTransit || !blueprintWorkflowInTransit || !multiCellInteriorVerified || !debugInteriorSupportVerified || !placedInterior || !interiorPlacedExists || !placedInteriorSink || !interiorSinkExists || !cabinPresentationVerified || !bundleTemplateChainConfigured || !worldBundleBlockedOnInteriorBelt || !placedSplitterPresentationVerified || !miniatureSyncedInTransit || !inputBlockedInTransit || !blockedDeploy || !edgeBlockedDeploy || !facingAwareCells || !mapFormatVerified || !bundleTemplateRulesVerified || !contextualRotateWorks || !previewArrowTracksFacing || !firstDeploy || !moveRejectedWhileDeployed || !openedWhileDeployed || !portConnected || !portOverlayConnected || !boundaryInterfaceVerified || !boundaryConnectorFlowVerified || !miniatureSyncedDeployed || !heavyHandoffVerified || !converterPortResolutionVerified || !inputDeliveredWhileDeployed || firstDelivered <= 0 || !turretTrackedThreats || !mobileCombatActive || !recalled || !stayedInPlaceAfterReturn || !reservationsReleased || !secondDeploy || secondDelivered <= 0)
        {
            GD.PushError($"MOBILE_FACTORY_SMOKE_FAILED startsPlayer={startsInPlayerMode} playerHudReady={playerHudReady} playerMoved={playerMoved} cameraFollowedPlayer={cameraFollowedPlayer} debugWorldSupport={debugWorldSupportVerified} playerWorldPlacementWorked={playerWorldPlacementWorked} commandActive={commandActive} cameraLocked={cameraLockedInCommand} returnedPlayerFromCommand={returnedToPlayerFromCommand} observerActive={observerActive} observerCamera={observerCameraActive} returnedPlayerFromObserver={returnedToPlayerFromObserver} deployPreviewEntered={deployPreviewEntered} returnedPlayerFromDeploy={returnedToPlayerFromDeploy} editRestoresPlayer={editRestoresPlayerMode} editRestoresFactory={editRestoresFactoryMode} editRestoresObserver={editRestoresObserverMode} editRestoresDeploy={editRestoresDeployMode} interiorTransit={interiorRunsInTransit} movedInTransit={movedInTransit} openedTransit={openedInTransit} workspaceNavigation={workspaceNavigationVerified} operationHover={operationPanelHover} viewportHover={editorViewportHover} worldHover={worldHover} detailWindow={detailWindowInTransit} worldDetail={worldDetailInTransit} blueprintWorkflow={blueprintWorkflowInTransit} multiCellInterior={multiCellInteriorVerified} debugInteriorSupport={debugInteriorSupportVerified} placedInterior={placedInterior} interiorPlacedExists={interiorPlacedExists} placedSink={placedInteriorSink} sinkExists={interiorSinkExists} cabinPresentation={cabinPresentationVerified} bundleTemplateChain={bundleTemplateChainConfigured} worldBundleBlockedOnInteriorBelt={worldBundleBlockedOnInteriorBelt} bundleTemplateRules={bundleTemplateRulesVerified} splitterPresentation={placedSplitterPresentationVerified} miniatureTransit={miniatureSyncedInTransit} inputBlockedInTransit={inputBlockedInTransit} blocked={blockedDeploy} edgeBlocked={edgeBlockedDeploy} facingAware={facingAwareCells} mapFormat={mapFormatVerified} contextualRotateWorks={contextualRotateWorks} previewArrowTracksFacing={previewArrowTracksFacing} firstDeploy={firstDeploy} moveRejected={moveRejectedWhileDeployed} openedDeployed={openedWhileDeployed} portConnected={portConnected} portOverlay={portOverlayConnected} boundaryInterface={boundaryInterfaceVerified} boundaryConnectorFlow={boundaryConnectorFlowVerified} miniatureDeployed={miniatureSyncedDeployed} heavyHandoff={heavyHandoffVerified} converterPortResolution={converterPortResolutionVerified} firstDelivered={firstDelivered} inputAttachmentTransit={inputAttachmentTransit} inputDeliveredWhileDeployed={inputDeliveredWhileDeployed} turretShots={(escortTurret?.ShotsFired ?? -1)} mobileCombatActive={mobileCombatActive} recalled={recalled} blockedOutputActive={blockedOutputActive} stayedInPlaceAfterReturn={stayedInPlaceAfterReturn} released={reservationsReleased} secondDeploy={secondDeploy} secondDelivered={secondDelivered}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_SMOKE_OK playerMoved={playerMoved} debugWorldSupport={debugWorldSupportVerified} commandActive={commandActive} observerActive={observerActive} mapFormat={mapFormatVerified} firstDelivered={firstDelivered} secondDelivered={secondDelivered} workspaceNavigation={workspaceNavigationVerified} debugInteriorSupport={debugInteriorSupportVerified} detailWindow={detailWindowInTransit} worldDetail={worldDetailInTransit} blueprintWorkflow={blueprintWorkflowInTransit} multiCellInterior={multiCellInteriorVerified} bundleTemplateChain={bundleTemplateChainConfigured} worldBundleBlockedOnInteriorBelt={worldBundleBlockedOnInteriorBelt} bundleTemplateRules={bundleTemplateRulesVerified} heavyHandoff={heavyHandoffVerified} converterPortResolution={converterPortResolutionVerified} turretShots={(escortTurret?.ShotsFired ?? -1)} combatKills={_simulation.DefeatedEnemyCount}");
        GetTree().Quit();
    }

    private async Task<bool> RunHeavyBufferedHandoffSmoke()
    {
        if (_mobileFactory is null
            || _simulation is null
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(0, 3), out var inputPortStructure)
            || inputPortStructure is not MobileFactoryInputPortStructure inputPort
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(7, 3), out var outputPortStructure)
            || outputPortStructure is not MobileFactoryOutputPortStructure outputPort
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(2, 2), out var unpackerStructure)
            || unpackerStructure is not CargoUnpackerStructure unpacker
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(5, 2), out var packerStructure)
            || packerStructure is not CargoPackerStructure packer)
        {
            return false;
        }

        var suspendCombatForVerification = HasFocusedSmokeTestFlag();
        if (suspendCombatForVerification)
        {
            _combatDirector?.ClearLanes();
            _simulation.ClearCombatActors();
        }

        try
        {
            if (inputPort.IsConnectedToWorld && inputPort.StagedCargoCount == 0)
            {
                inputPort.TryReceiveProvidedItem(
                    _simulation.CreateItem(
                        FactorySiteKind.World,
                        BuildPrototypeKind.MiningDrill,
                        FactoryItemKind.IronOre,
                        FactoryCargoForm.WorldBulk,
                        "bulk-iron-ore-standard"),
                    inputPort.WorldAdjacentCell,
                    _simulation);
            }

            if (outputPort.IsConnectedToWorld && outputPort.StagedCargoCount == 0)
            {
                outputPort.TryAcceptPackedBundle(
                    _simulation.CreateItem(
                        FactorySiteKind.World,
                        BuildPrototypeKind.CargoPacker,
                        FactoryItemKind.IronPlate,
                        FactoryCargoForm.WorldPacked,
                        "packed-iron-plate-standard"),
                    outputPort.Cell - FactoryDirection.ToCellOffset(outputPort.Facing),
                    _simulation);
            }

            var packerInputCell = GetPrimaryInputCell(packer);

            var sawInboundStage = false;
            var sawBufferedInner = false;
            var sawOutboundStage = false;
            var sawOutboundInnerBuffer = false;
            var sawOutboundBridgeStage = false;
            var sawOutboundOuterWait = false;
            var sawOutboundRelease = false;
            var sawOutboundReleaseAfterWait = false;
            var sawInboundConverterOwnership = false;
            var sawInboundRelease = false;
            var sawOutboundConverterOwnership = false;
            var boundedOwnership = true;
            var singleVisiblePayloadOwner = true;
            var outputReleasePickupPrepared = false;
            var remaining = 10.0f;

            while (remaining > 0.0f)
            {
                if (!sawOutboundConverterOwnership)
                {
                    packer.TryReceiveProvidedItem(
                        _simulation.CreateItem(
                            packer.Site,
                            BuildPrototypeKind.Smelter,
                            FactoryItemKind.IronPlate,
                            FactoryCargoForm.InteriorFeed),
                        packerInputCell,
                        _simulation);
                }

                var inputHasPresentation = inputPort.TryGetCurrentPresentationState(out var inputChainPresentation);
                var outputHasPresentation = outputPort.TryGetCurrentPresentationState(out var outputChainPresentation);
                var unpackerHasPresentation = unpacker.TryGetHeavyCargoPresentationState(out _);
                var packerHasPresentation = packer.TryGetHeavyCargoPresentationState(out _);
                sawInboundStage |= inputPort.StagedCargoCount > 0;
                sawBufferedInner |= inputPort.HandoffPhase is MobileFactoryHeavyHandoffPhase.BufferedInner or MobileFactoryHeavyHandoffPhase.WaitingForUnpacker;
                sawBufferedInner |= inputHasPresentation
                    && inputChainPresentation.Host == MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer;
                sawInboundConverterOwnership |= unpackerHasPresentation
                    || unpacker.HasProcessingBundle
                    || unpacker.IsEmittingManifest
                    || unpacker.PendingManifestCount > 0;
                sawInboundRelease |= inputPort.InnerBufferedItem is null && sawInboundConverterOwnership;
                sawOutboundStage |= outputPort.StagedCargoCount > 0
                    && (outputPort.HandoffPhase is MobileFactoryHeavyHandoffPhase.BufferedInner
                        or MobileFactoryHeavyHandoffPhase.ReceivingFromPacker
                        or MobileFactoryHeavyHandoffPhase.SlidingToBridgeOutward
                        or MobileFactoryHeavyHandoffPhase.BridgingOutward
                        or MobileFactoryHeavyHandoffPhase.WaitingWorldPickup
                        or MobileFactoryHeavyHandoffPhase.ReleasingToWorld);
                sawOutboundInnerBuffer |= outputPort.HandoffPhase is MobileFactoryHeavyHandoffPhase.ReceivingFromPacker
                    or MobileFactoryHeavyHandoffPhase.BufferedInner
                    || outputHasPresentation && outputChainPresentation.Host == MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer;
                sawOutboundBridgeStage |= outputPort.HandoffPhase is MobileFactoryHeavyHandoffPhase.SlidingToBridgeOutward
                    or MobileFactoryHeavyHandoffPhase.BridgingOutward
                    || outputHasPresentation && outputChainPresentation.Host is MobileFactoryHeavyCargoPresentationHost.InteriorBridge or MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff
                        && outputPort.TransferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer;
                sawOutboundOuterWait |= outputPort.HandoffPhase == MobileFactoryHeavyHandoffPhase.WaitingWorldPickup
                    || outputHasPresentation && outputChainPresentation.Host == MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer;
                sawOutboundRelease |= outputPort.HandoffPhase == MobileFactoryHeavyHandoffPhase.ReleasingToWorld
                    && outputHasPresentation
                    && outputChainPresentation.Host == MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff;
                sawOutboundReleaseAfterWait |= sawOutboundOuterWait && sawOutboundRelease;
                sawOutboundConverterOwnership |= packerHasPresentation
                    || packer.HasProcessingBundle
                    || packer.HasPackedBundleBuffered;
                boundedOwnership &= inputPort.StagedCargoCount <= 3 && outputPort.StagedCargoCount <= 3;
                singleVisiblePayloadOwner &= inputPort.CountVisiblePayloadVisuals() <= 1 && outputPort.CountVisiblePayloadVisuals() <= 1;
                singleVisiblePayloadOwner &= !(inputHasPresentation && unpackerHasPresentation);
                singleVisiblePayloadOwner &= !(outputHasPresentation && packerHasPresentation);

                if (!outputReleasePickupPrepared
                    && sawOutboundOuterWait
                    && outputPort.IsConnectedToWorld
                    && outputPort.OuterBufferedItem is FactoryItem bufferedBundle
                    && _grid is not null)
                {
                    var releaseEdgeCell = outputPort.WorldAdjacentCell;
                    var downstreamCell = releaseEdgeCell + FactoryDirection.ToCellOffset(outputPort.WorldFacing);
                    var sinkCell = downstreamCell + FactoryDirection.ToCellOffset(outputPort.WorldFacing);
                    if (_grid.TryGetStructure(releaseEdgeCell, out var releaseEdgeTarget) && releaseEdgeTarget is not null)
                    {
                        RemoveWorldStructure(releaseEdgeCell);
                    }

                    if (!_grid.TryGetStructure(downstreamCell, out var releaseTarget)
                        || releaseTarget is not BeltStructure)
                    {
                        if (releaseTarget is not null)
                        {
                            RemoveWorldStructure(downstreamCell);
                        }

                        releaseTarget = PlaceWorldStructure(BuildPrototypeKind.Belt, downstreamCell, outputPort.WorldFacing);
                    }

                    if (!_grid.TryGetStructure(sinkCell, out var releaseSink)
                        || releaseSink is not SinkStructure)
                    {
                        if (releaseSink is not null)
                        {
                            RemoveWorldStructure(sinkCell);
                        }

                        releaseSink = PlaceWorldStructure(BuildPrototypeKind.Sink, sinkCell, outputPort.WorldFacing);
                    }

                    outputReleasePickupPrepared = releaseTarget is BeltStructure releaseBelt
                        && releaseBelt.CanAcceptExternalHandoff(bufferedBundle, releaseEdgeCell, _simulation);
                }

                await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
                remaining -= 0.2f;
            }

            var verified = sawInboundStage
                && sawBufferedInner
                && sawInboundConverterOwnership
                && sawInboundRelease
                && sawOutboundStage
                && sawOutboundInnerBuffer
                && sawOutboundBridgeStage
                && sawOutboundOuterWait
                && sawOutboundRelease
                && sawOutboundReleaseAfterWait
                && boundedOwnership
                && singleVisiblePayloadOwner;

            if (!verified && HasFocusedSmokeTestFlag())
            {
                GD.Print(
                    $"MOBILE_FACTORY_HEAVY_HANDOFF_SMOKE " +
                    $"inboundStage={sawInboundStage} " +
                    $"bufferedInner={sawBufferedInner} " +
                    $"inboundConverter={sawInboundConverterOwnership} " +
                    $"inboundRelease={sawInboundRelease} " +
                    $"outboundStage={sawOutboundStage} " +
                    $"outboundInner={sawOutboundInnerBuffer} " +
                    $"outboundBridge={sawOutboundBridgeStage} " +
                    $"outboundOuterWait={sawOutboundOuterWait} " +
                    $"outboundRelease={sawOutboundRelease} " +
                    $"outboundReleaseAfterWait={sawOutboundReleaseAfterWait} " +
                    $"outboundConverter={sawOutboundConverterOwnership} " +
                    $"bounded={boundedOwnership} " +
                    $"singleVisible={singleVisiblePayloadOwner} " +
                    $"inputPhase={inputPort.HandoffPhase} " +
                    $"outputPhase={outputPort.HandoffPhase} " +
                    $"outputMode={outputPort.TransferMode} " +
                    $"outputProgress={outputPort.BridgeTransferProgress:0.000} " +
                    $"outputStaged={outputPort.StagedCargoCount} " +
                    $"outputConnected={outputPort.IsConnectedToWorld} " +
                    $"outputWorldReady={outputPort.IsWorldFlowReady} " +
                    $"inputVisible={inputPort.CountVisiblePayloadVisuals()} " +
                    $"outputVisible={outputPort.CountVisiblePayloadVisuals()} " +
                    $"inputState={(inputPort.TryGetCurrentPresentationState(out var inputPresentation) ? $"{inputPresentation.Owner}/{inputPresentation.Host}" : "none")} " +
                    $"outputState={(outputPort.TryGetCurrentPresentationState(out var outputPresentation) ? $"{outputPresentation.Owner}/{outputPresentation.Host}" : "none")} " +
                    $"unpackerState={(unpacker.TryGetHeavyCargoPresentationState(out var unpackerPresentation) ? $"{unpackerPresentation.Owner}/{unpackerPresentation.Host}" : "none")} " +
                    $"packerState={(packer.TryGetHeavyCargoPresentationState(out var packerPresentation) ? $"{packerPresentation.Owner}/{packerPresentation.Host}" : "none")}");
            }

            return verified;
        }
        finally
        {
            if (suspendCombatForVerification)
            {
                ConfigureWorldCombatScenarios();
            }
        }
    }

    private async Task<bool> RunPackerInserterPortResolutionSmoke()
    {
        if (_simulation is null || _structureRoot is null)
        {
            return false;
        }

        var scratchRoot = new Node3D
        {
            Name = "PackerPortResolutionSmokeRoot",
            Visible = false
        };
        _structureRoot.AddChild(scratchRoot);

        var scratchFactory = new MobileFactoryInstance(
            "smoke-packer-port-resolution",
            scratchRoot,
            _simulation,
            MobileFactoryScenarioLibrary.CreateFocusedDemoProfile(),
            new MobileFactoryInteriorPreset(
                "smoke-packer-port-resolution",
                "封包端口回归",
                "验证机械臂可以把物品直接送进封包舱主体边缘的舱内输入口。",
                "仓储经机械臂向前投送时，应直接命中封包舱主体左侧输入位，不再依赖外挂共享输入格。",
                global::System.Array.Empty<FactoryPlacementSpec>(),
                global::System.Array.Empty<MobileFactoryAttachmentPlacementSpec>()));

        var storageCell = new Vector2I(0, 1);
        var inserterCell = new Vector2I(1, 1);
        var packerCell = new Vector2I(2, 1);
        var configured = false;
        var recipeActive = false;
        var packerIntegratedInputs = false;
        var seededAll = false;
        var storageDrained = false;
        var packerAcceptedInput = false;
        var removedAll = true;

        try
        {
            var placedStorage = scratchFactory.PlaceInteriorStructure(BuildPrototypeKind.Storage, storageCell, FacingDirection.East);
            var placedInserter = scratchFactory.PlaceInteriorStructure(BuildPrototypeKind.Inserter, inserterCell, FacingDirection.East);
            var placedPacker = scratchFactory.PlaceInteriorStructure(BuildPrototypeKind.CargoPacker, packerCell, FacingDirection.East);
            if (!placedStorage
                || !placedInserter
                || !placedPacker
                || !scratchFactory.TryGetInteriorStructure(storageCell, out var storageStructure)
                || storageStructure is not StorageStructure storage
                || !scratchFactory.TryGetInteriorStructure(inserterCell, out var inserterStructure)
                || inserterStructure is not InserterStructure
                || !scratchFactory.TryGetInteriorStructure(packerCell, out var packerStructure)
                || packerStructure is not CargoPackerStructure packer)
            {
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print($"MOBILE_FACTORY_PACKER_PORT_SMOKE setup_failed storage={placedStorage} inserter={placedInserter} packer={placedPacker}");
                }
                return false;
            }

            configured = packer.TrySetDetailRecipe("packed-iron-plate-standard");
            recipeActive = packer.GetDetailModel().RecipeSection?.ActiveRecipeId == "packed-iron-plate-standard";
            packerIntegratedInputs = new HashSet<Vector2I>(packer.GetInputCells()).SetEquals(new[]
            {
                packer.Cell,
                packer.Cell + Vector2I.Down
            });
            if (!configured || !recipeActive || !packerIntegratedInputs)
            {
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print($"MOBILE_FACTORY_PACKER_PORT_SMOKE recipe_failed configured={configured} recipeActive={recipeActive} integratedInputs={packerIntegratedInputs}");
                }
                return false;
            }

            if (!FactoryBundleCatalog.TryResolveSingleItemRequirement(
                    FactoryBundleCatalog.Get("packed-iron-plate-standard"),
                    out var requiredItemKind,
                    out var requiredUnits))
            {
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print("MOBILE_FACTORY_PACKER_PORT_SMOKE requirement_failed=true");
                }
                return false;
            }

            seededAll = true;
            for (var index = 0; index < requiredUnits; index++)
            {
                seededAll &= storage.TryReceiveProvidedItem(
                    _simulation.CreateItem(scratchFactory.InteriorSite, BuildPrototypeKind.Smelter, requiredItemKind, FactoryCargoForm.InteriorFeed),
                    storage.Cell + Vector2I.Left,
                    _simulation);
            }

            if (!seededAll)
            {
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print($"MOBILE_FACTORY_PACKER_PORT_SMOKE seed_failed buffered={storage.BufferedCount}");
                }
                return false;
            }

            var remaining = 8.0f;
            while (remaining > 0.0f)
            {
                storageDrained |= storage.BufferedCount == 0;
                packerAcceptedInput |= packer.HasProcessingBundle || packer.HasPackedBundleBuffered;

                if (!packerAcceptedInput)
                {
                    foreach (var line in packer.GetInspectionLines())
                    {
                        if (line.Contains("累计装箱：", global::System.StringComparison.Ordinal)
                            && !line.EndsWith("/0", global::System.StringComparison.Ordinal))
                        {
                            packerAcceptedInput = true;
                            break;
                        }
                    }
                }

                if (storageDrained && packerAcceptedInput)
                {
                    break;
                }

                await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
                remaining -= 0.2f;
            }

            if ((!storageDrained || !packerAcceptedInput) && HasFocusedSmokeTestFlag())
            {
                GD.Print(
                    $"MOBILE_FACTORY_PACKER_PORT_SMOKE " +
                    $"configured={configured} " +
                    $"recipeActive={recipeActive} " +
                    $"integratedInputs={packerIntegratedInputs} " +
                    $"seeded={seededAll} " +
                    $"storageBuffered={storage.BufferedCount} " +
                    $"packerProcessing={packer.HasProcessingBundle} " +
                    $"packerBuffered={packer.HasPackedBundleBuffered} " +
                    $"inspection={string.Join(" || ", packer.GetInspectionLines())}");
            }

            return configured
                && recipeActive
                && packerIntegratedInputs
                && seededAll
                && storageDrained
                && packerAcceptedInput;
        }
        finally
        {
            var cellsToRemove = new List<Vector2I>();
            foreach (var structure in scratchFactory.InteriorSite.GetStructures())
            {
                cellsToRemove.Add(structure.Cell);
            }

            for (var index = 0; index < cellsToRemove.Count; index++)
            {
                removedAll &= scratchFactory.RemoveInteriorStructure(cellsToRemove[index]);
            }

            scratchRoot.QueueFree();

            if (!removedAll && HasFocusedSmokeTestFlag())
            {
                GD.Print("MOBILE_FACTORY_PACKER_PORT_SMOKE cleanup_failed=true");
            }
        }
    }

    private async Task<bool> RunPackerBeltPortResolutionSmoke()
    {
        if (_simulation is null || _structureRoot is null)
        {
            return false;
        }

        var scratchRoot = new Node3D
        {
            Name = "PackerBeltPortResolutionSmokeRoot",
            Visible = false
        };
        _structureRoot.AddChild(scratchRoot);

        var scratchFactory = new MobileFactoryInstance(
            "smoke-packer-belt-port-resolution",
            scratchRoot,
            _simulation,
            MobileFactoryScenarioLibrary.CreateFocusedDemoProfile(),
            new MobileFactoryInteriorPreset(
                "smoke-packer-belt-port-resolution",
                "封包供料轨回归",
                "验证供料轨可以把物品直接送进封包舱主体边缘的舱内输入口。",
                "供料轨正对封包舱时，应直接把物品推进主体左侧输入位，不再隔着一格喂入。",
                global::System.Array.Empty<FactoryPlacementSpec>(),
                global::System.Array.Empty<MobileFactoryAttachmentPlacementSpec>()));

        var beltCell = new Vector2I(1, 1);
        var packerCell = new Vector2I(2, 1);
        var configured = false;
        var recipeActive = false;
        var packerIntegratedInputs = false;
        var beltInjected = false;
        var beltDrained = false;
        var packerAcceptedInput = false;
        var removedAll = true;

        try
        {
            var placedBelt = scratchFactory.PlaceInteriorStructure(BuildPrototypeKind.Belt, beltCell, FacingDirection.East);
            var placedPacker = scratchFactory.PlaceInteriorStructure(BuildPrototypeKind.CargoPacker, packerCell, FacingDirection.East);
            if (!placedBelt
                || !placedPacker
                || !scratchFactory.TryGetInteriorStructure(beltCell, out var beltStructure)
                || beltStructure is not BeltStructure belt
                || !scratchFactory.TryGetInteriorStructure(packerCell, out var packerStructure)
                || packerStructure is not CargoPackerStructure packer)
            {
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print($"MOBILE_FACTORY_PACKER_BELT_PORT_SMOKE setup_failed belt={placedBelt} packer={placedPacker}");
                }
                return false;
            }

            configured = packer.TrySetDetailRecipe("packed-iron-plate-standard");
            recipeActive = packer.GetDetailModel().RecipeSection?.ActiveRecipeId == "packed-iron-plate-standard";
            packerIntegratedInputs = new HashSet<Vector2I>(packer.GetInputCells()).SetEquals(new[]
            {
                packer.Cell,
                packer.Cell + Vector2I.Down
            });
            if (!configured || !recipeActive || !packerIntegratedInputs)
            {
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print($"MOBILE_FACTORY_PACKER_BELT_PORT_SMOKE recipe_failed configured={configured} recipeActive={recipeActive} integratedInputs={packerIntegratedInputs}");
                }
                return false;
            }

            beltInjected = belt.TryAcceptExternalHandoff(
                _simulation.CreateItem(scratchFactory.InteriorSite, BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate, FactoryCargoForm.InteriorFeed),
                belt.Cell + Vector2I.Left,
                _simulation);
            if (!beltInjected)
            {
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print("MOBILE_FACTORY_PACKER_BELT_PORT_SMOKE inject_failed=true");
                }
                return false;
            }

            var remaining = 4.0f;
            while (remaining > 0.0f)
            {
                beltDrained |= belt.TransitItemCount == 0;
                packerAcceptedInput |= packer.HasProcessingBundle || packer.HasPackedBundleBuffered;

                if (!packerAcceptedInput)
                {
                    foreach (var line in packer.GetInspectionLines())
                    {
                        if (line.Contains("累计装箱：", global::System.StringComparison.Ordinal)
                            && !line.EndsWith("/0", global::System.StringComparison.Ordinal))
                        {
                            packerAcceptedInput = true;
                            break;
                        }
                    }
                }

                if (beltDrained && packerAcceptedInput)
                {
                    break;
                }

                await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
                remaining -= 0.2f;
            }

            if ((!beltDrained || !packerAcceptedInput) && HasFocusedSmokeTestFlag())
            {
                GD.Print(
                    $"MOBILE_FACTORY_PACKER_BELT_PORT_SMOKE " +
                    $"configured={configured} " +
                    $"recipeActive={recipeActive} " +
                    $"integratedInputs={packerIntegratedInputs} " +
                    $"beltInjected={beltInjected} " +
                    $"beltTransit={belt.TransitItemCount} " +
                    $"packerProcessing={packer.HasProcessingBundle} " +
                    $"packerBuffered={packer.HasPackedBundleBuffered} " +
                    $"inspection={string.Join(" || ", packer.GetInspectionLines())}");
            }

            return configured
                && recipeActive
                && packerIntegratedInputs
                && beltInjected
                && beltDrained
                && packerAcceptedInput;
        }
        finally
        {
            var cellsToRemove = new List<Vector2I>();
            foreach (var structure in scratchFactory.InteriorSite.GetStructures())
            {
                cellsToRemove.Add(structure.Cell);
            }

            for (var index = 0; index < cellsToRemove.Count; index++)
            {
                removedAll &= scratchFactory.RemoveInteriorStructure(cellsToRemove[index]);
            }

            scratchRoot.QueueFree();

            if (!removedAll && HasFocusedSmokeTestFlag())
            {
                GD.Print("MOBILE_FACTORY_PACKER_BELT_PORT_SMOKE cleanup_failed=true");
            }
        }
    }

    private static bool RunBundleTemplateRulesSmoke()
    {
        var fixedTemplate = FactoryBundleCatalog.Get("packed-gear-compact");
        var categoryTemplate = FactoryBundleCatalog.Get("packed-ore-mixed-standard");
        var mixedTemplate = FactoryBundleCatalog.Get("packed-frontline-sustainment-wide");
        var fixedWorldTemplate = new FactoryItem(
            -302,
            BuildPrototypeKind.MiningDrill,
            FactoryItemKind.IronOre,
            FactoryCargoForm.WorldBulk,
            "bulk-iron-ore-standard");
        var mixedWorldTemplate = new FactoryItem(
            -303,
            BuildPrototypeKind.CargoPacker,
            FactoryItemKind.GenericCargo,
            FactoryCargoForm.WorldPacked,
            "packed-frontline-sustainment-wide");
        var autoPackInput = new FactoryItem(
            -304,
            BuildPrototypeKind.Smelter,
            FactoryItemKind.IronPlate,
            FactoryCargoForm.InteriorFeed);

        var fixedAcceptsGear = FactoryBundleCatalog.CanAcceptIntoTemplate(fixedTemplate, FactoryItemKind.Gear, new Dictionary<FactoryItemKind, int>(), out _);
        var fixedAcceptsFinalGear = FactoryBundleCatalog.CanAcceptIntoTemplate(
            fixedTemplate,
            FactoryItemKind.Gear,
            new Dictionary<FactoryItemKind, int> { [FactoryItemKind.Gear] = 3 },
            out _);
        var fixedRejectsWrongItem = !FactoryBundleCatalog.CanAcceptIntoTemplate(fixedTemplate, FactoryItemKind.IronPlate, new Dictionary<FactoryItemKind, int>(), out _);
        var fixedRejectsOverflowGear = !FactoryBundleCatalog.CanAcceptIntoTemplate(
            fixedTemplate,
            FactoryItemKind.Gear,
            new Dictionary<FactoryItemKind, int> { [FactoryItemKind.Gear] = 4 },
            out _);

        var categoryCounts = new Dictionary<FactoryItemKind, int>
        {
            [FactoryItemKind.IronOre] = 3,
            [FactoryItemKind.CopperOre] = 3
        };
        var categoryRejectsSupport = !FactoryBundleCatalog.CanAcceptIntoTemplate(categoryTemplate, FactoryItemKind.RepairKit, categoryCounts, out _);
        var categorySatisfied = FactoryBundleCatalog.IsSatisfied(categoryTemplate, categoryCounts, out _);

        var mixedCounts = new Dictionary<FactoryItemKind, int>
        {
            [FactoryItemKind.AmmoMagazine] = 4,
            [FactoryItemKind.HighVelocityAmmo] = 2,
            [FactoryItemKind.RepairKit] = 2
        };
        var mixedRejectsFreeMix = !FactoryBundleCatalog.CanAcceptIntoTemplate(mixedTemplate, FactoryItemKind.Gear, mixedCounts, out _);
        var mixedSatisfied = FactoryBundleCatalog.IsSatisfied(mixedTemplate, mixedCounts, out _);

        var unpackManifest = FactoryBundleCatalog.ExpandManifest(new FactoryItem(
            -301,
            BuildPrototypeKind.MiningDrill,
            FactoryItemKind.IronOre,
            FactoryCargoForm.WorldBulk,
            "bulk-iron-ore-standard"));
        var resolvesOneToOneWorldTemplate =
            FactoryBundleCatalog.TryResolveOneToOneWorldTemplate(fixedWorldTemplate, out var resolvedWorldTemplate)
            && resolvedWorldTemplate is not null
            && resolvedWorldTemplate.Id == "bulk-iron-ore-standard";
        var rejectsMixedWorldTemplate = !FactoryBundleCatalog.TryResolveOneToOneWorldTemplate(mixedWorldTemplate, out _);
        var resolvesAutoPackTemplate =
            FactoryBundleCatalog.TryResolveAutoPackTemplate(autoPackInput, out var resolvedAutoPackTemplate)
            && resolvedAutoPackTemplate is not null
            && resolvedAutoPackTemplate.Id == "packed-iron-plate-standard";
        var converterSelectableTemplates = FactoryBundleCatalog.GetConverterSelectableTemplates(
            FactoryCargoForm.WorldBulk,
            FactoryCargoForm.WorldPacked);
        var converterSelectableTemplatesOnlySingle = converterSelectableTemplates.Count > 0;
        var containsBulkIronOreStandard = false;
        var containsPackedIronPlateStandard = false;
        var excludesMixedTemplates = true;
        for (var index = 0; index < converterSelectableTemplates.Count; index++)
        {
            var template = converterSelectableTemplates[index];
            converterSelectableTemplatesOnlySingle &= FactoryBundleCatalog.IsSingleItemTemplate(template);
            containsBulkIronOreStandard |= template.Id == "bulk-iron-ore-standard";
            containsPackedIronPlateStandard |= template.Id == "packed-iron-plate-standard";
            excludesMixedTemplates &= template.Id != "packed-ore-mixed-standard"
                && template.Id != "packed-frontline-sustainment-wide";
        }

        return fixedAcceptsGear
            && fixedAcceptsFinalGear
            && fixedRejectsWrongItem
            && fixedRejectsOverflowGear
            && categoryRejectsSupport
            && categorySatisfied
            && mixedRejectsFreeMix
            && mixedSatisfied
            && unpackManifest.Count == 6
            && resolvesOneToOneWorldTemplate
            && rejectsMixedWorldTemplate
            && resolvesAutoPackTemplate
            && converterSelectableTemplatesOnlySingle
            && containsBulkIronOreStandard
            && containsPackedIronPlateStandard
            && excludesMixedTemplates;
    }

    private async Task<bool> RunDebugWorldSupportSmoke()
    {
        if (_grid is null || _simulation is null || _playerController is null)
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
        var authoredWorldDebugFree = !ContainsDebugStructure(_grid.GetStructures());
        var playerStarterInventoryReady = PlayerInventoryContainsDebugKit();

        var sourceCell = new Vector2I(-40, 36);
        var facing = FacingDirection.East;
        var sourceOutputCell = FactoryStructureFactory.GetFootprint(BuildPrototypeKind.DebugPartSource).ResolveOutputCell(sourceCell, facing);
        var generatorCell = new Vector2I(-40, 33);
        if (!_grid.CanPlaceStructure(BuildPrototypeKind.DebugPartSource, sourceCell, facing, out _)
            || !_grid.CanPlaceStructure(BuildPrototypeKind.Sink, sourceOutputCell, facing, out _)
            || !_grid.CanPlaceStructure(BuildPrototypeKind.DebugPowerGenerator, generatorCell, facing, out _))
        {
            return false;
        }

        var source = PlaceWorldStructure(BuildPrototypeKind.DebugPartSource, sourceCell, facing) as DebugPartSourceStructure;
        var sink = PlaceWorldStructure(BuildPrototypeKind.Sink, sourceOutputCell, facing) as SinkStructure;
        var generator = PlaceWorldStructure(BuildPrototypeKind.DebugPowerGenerator, generatorCell, facing) as DebugPowerGeneratorStructure;
        if (source is null || sink is null || generator is null)
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(2.5f), SceneTreeTimer.SignalName.Timeout);
        var sourceDelivered = sink.DeliveredTotal > 0;
        var generatorStable = Mathf.IsEqualApprox(generator.GetAvailablePower(_simulation), generator.NominalPowerSupply);

        await ToSignal(GetTree().CreateTimer(0.8f), SceneTreeTimer.SignalName.Timeout);
        var generatorStillStable = Mathf.IsEqualApprox(generator.GetAvailablePower(_simulation), generator.NominalPowerSupply);
        return catalogReady && authoredWorldDebugFree && playerStarterInventoryReady && sourceDelivered && generatorStable && generatorStillStable;
    }

    private Vector2I? FindPlayerWorldBuildSmokeCell(BuildPrototypeKind kind, FacingDirection facing)
    {
        if (_grid is null)
        {
            return null;
        }

        var origin = new Vector2I(GetWorldMinCell() + 2, GetWorldMaxCell() - 2);
        for (var radius = 0; radius <= 8; radius++)
        {
            for (var y = origin.Y - radius; y <= origin.Y + radius; y++)
            {
                for (var x = origin.X - radius; x <= origin.X + radius; x++)
                {
                    var candidate = new Vector2I(x, y);
                    if (_grid.IsInBounds(candidate) && _grid.CanPlaceStructure(kind, candidate, facing, out _))
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }

    private bool TryArmPlayerWorldBuildForSmoke(out BuildPrototypeKind kind)
    {
        kind = default;
        if (_playerController is null)
        {
            return false;
        }

        for (var preferredPass = 0; preferredPass < 2; preferredPass++)
        {
            for (var slotIndex = 0; slotIndex < FactoryPlayerController.HotbarSlotCount; slotIndex++)
            {
                var item = _playerController.GetHotbarItem(slotIndex);
                if (!FactoryPresentation.TryGetPlaceableStructureKind(item, out var candidateKind))
                {
                    continue;
                }

                if (preferredPass == 0 && candidateKind != BuildPrototypeKind.Belt)
                {
                    continue;
                }

                HandlePlayerHotbarPressed(slotIndex);
                if (!_playerPlacementState.PlacementArmed)
                {
                    HandlePlayerHotbarPressed(slotIndex);
                }

                if (_playerPlacementState.PlacementArmed && TryResolveSelectedPlayerPlaceable(out kind))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool RunInteriorMultiCellSmoke()
    {
        if (_mobileFactory is null)
        {
            return false;
        }

        var secondaryCell = FocusedDepotAnchorCell + Vector2I.One;
        var anchorCell = FocusedDepotAnchorCell;
        var edgeAnchor = new Vector2I(7, 7);
        var unpackerSpansMultipleCells =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(3, 3), out var unpackerFootprintStructure)
            && unpackerFootprintStructure is CargoUnpackerStructure;
        var packerSpansMultipleCells =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(6, 3), out var packerFootprintStructure)
            && packerFootprintStructure is CargoPackerStructure;
        var inputPortSpansMultipleCells =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(1, 2), out var inputPortSecondaryStructure)
            && inputPortSecondaryStructure is MobileFactoryInputPortStructure;
        var outputPortSpansMultipleCells =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(7, 2), out var outputPortUpperStructure)
            && outputPortUpperStructure is MobileFactoryOutputPortStructure
            && _mobileFactory.TryGetInteriorStructure(new Vector2I(7, 0), out var outputPortLowerStructure)
            && outputPortLowerStructure is MobileFactoryOutputPortStructure;
        var standardUnpackerFootprint = new HashSet<Vector2I>(
            FactoryStructureFactory
                .GetFootprint(BuildPrototypeKind.CargoUnpacker, configuration: null, mapRecipeId: "bulk-iron-ore-standard")
                .ResolveOccupiedCells(new Vector2I(2, 2), FacingDirection.East));
        var widePackerFootprint = new HashSet<Vector2I>(
            FactoryStructureFactory
                .GetFootprint(BuildPrototypeKind.CargoPacker, configuration: null, mapRecipeId: "packed-frontline-sustainment-wide")
                .ResolveOccupiedCells(new Vector2I(2, 2), FacingDirection.East));
        var sizeTierFootprintsVerified =
            standardUnpackerFootprint.SetEquals(new[] { new Vector2I(2, 2), new Vector2I(3, 2), new Vector2I(2, 3), new Vector2I(3, 3) })
            && widePackerFootprint.SetEquals(new[] { new Vector2I(2, 2), new Vector2I(3, 2), new Vector2I(2, 3), new Vector2I(3, 3) });
        var resolvesFromSecondaryCell = _mobileFactory.TryGetInteriorStructure(secondaryCell, out var structure) && structure is LargeStorageDepotStructure;
        var blockedAtEdge = !_mobileFactory.CanPlaceInterior(BuildPrototypeKind.LargeStorageDepot, edgeAnchor, FacingDirection.East);
        var removedFromSecondaryCell = _mobileFactory.RemoveInteriorStructure(secondaryCell);
        var cleared = !_mobileFactory.TryGetInteriorStructure(anchorCell, out _)
            && !_mobileFactory.TryGetInteriorStructure(secondaryCell, out _);
        var heavyTurretPlaced = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.HeavyGunTurret, anchorCell, FacingDirection.East);
        var heavyTurretResolves = _mobileFactory.TryGetInteriorStructure(anchorCell + Vector2I.Right, out var heavyTurretStructure)
            && heavyTurretStructure is HeavyGunTurretStructure;
        var heavyTurretRemoved = _mobileFactory.RemoveInteriorStructure(anchorCell + Vector2I.Right);
        var replaced = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.LargeStorageDepot, anchorCell, FacingDirection.East);
        return unpackerSpansMultipleCells
            && packerSpansMultipleCells
            && inputPortSpansMultipleCells
            && outputPortSpansMultipleCells
            && sizeTierFootprintsVerified
            && resolvesFromSecondaryCell
            && blockedAtEdge
            && removedFromSecondaryCell
            && cleared
            && heavyTurretPlaced
            && heavyTurretResolves
            && heavyTurretRemoved
            && replaced;
    }

    private async Task<bool> RunWorkspaceNavigationSmoke()
    {
        if (_hud is null)
        {
            return false;
        }

        var workspaceIds = _hud.GetWorkspaceIds();
        var required = UseLargeTestScenario
            ? new[] { OverviewWorkspaceId, BuildTestWorkspaceId, BlueprintWorkspaceId, DiagnosticsWorkspaceId, SavesWorkspaceId, DetailsWorkspaceId }
            : new[] { CommandWorkspaceId, WorldBuildWorkspaceId, EditorWorkspaceId, TestingWorkspaceId, BlueprintWorkspaceId, SavesWorkspaceId, DetailsWorkspaceId };
        if (!FactoryDemoSmokeSupport.ContainsAllWorkspaces(workspaceIds, required))
        {
            return false;
        }

        var editorWasOpen = _editorOpen;
        _hud.SelectWorkspace(BlueprintWorkspaceId);
        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
        var blueprintReady = _hud.ActiveWorkspaceId == BlueprintWorkspaceId && _hud.IsWorkspaceVisible(BlueprintWorkspaceId) && _editorOpen == editorWasOpen;

        var buildWorkspaceId = GetHudBuildWorkspaceId();
        _hud.SelectWorkspace(buildWorkspaceId);
        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
        var buildReady = _hud.ActiveWorkspaceId == buildWorkspaceId && _hud.IsWorkspaceVisible(buildWorkspaceId) && _editorOpen == editorWasOpen;

        var editorReady = true;
        if (!UseLargeTestScenario)
        {
            _hud.SelectWorkspace(EditorWorkspaceId);
            await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
            editorReady = _hud.ActiveWorkspaceId == EditorWorkspaceId && _hud.IsWorkspaceVisible(EditorWorkspaceId) && _editorOpen == editorWasOpen;
        }

        var testingReady = true;
        if (!UseLargeTestScenario)
        {
            _hud.SelectWorkspace(TestingWorkspaceId);
            await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
            testingReady = _hud.ActiveWorkspaceId == TestingWorkspaceId && _hud.IsWorkspaceVisible(TestingWorkspaceId) && _editorOpen == editorWasOpen;
        }

        _hud.SelectWorkspace(DetailsWorkspaceId);
        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
        var detailsReady = _hud.ActiveWorkspaceId == DetailsWorkspaceId && _hud.IsWorkspaceVisible(DetailsWorkspaceId) && _editorOpen == editorWasOpen;

        _hud.SelectWorkspace(SavesWorkspaceId);
        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
        var savesReady = _hud.ActiveWorkspaceId == SavesWorkspaceId && _hud.IsWorkspaceVisible(SavesWorkspaceId) && _editorOpen == editorWasOpen;

        var workspaceBeforeCollapse = _hud.ActiveWorkspaceId;
        var expandedOverviewWidth = _hud.OverviewVisibleWidth;
        var chromeEmbedded = _hud.IsWorkspaceChromeEmbeddedInOverview;
        var editorPanelBuildFocused = _hud.IsEditorOperationPanelBuildFocused;
        _hud.SetOverviewCollapsed(true);
        await WaitForCondition(() => _hud.IsOverviewCollapsed && _hud.OverviewVisibleWidth < expandedOverviewWidth, 0.6f);
        var collapsedOverviewWidth = _hud.OverviewVisibleWidth;
        var collapseReady = _hud.IsOverviewCollapsed
            && collapsedOverviewWidth < expandedOverviewWidth
            && _hud.ActiveWorkspaceId == workspaceBeforeCollapse
            && _hud.OverviewCollapseButtonText.Contains("展开", global::System.StringComparison.Ordinal);
        _hud.SetOverviewCollapsed(false);
        await WaitForCondition(() => !_hud.IsOverviewCollapsed && _hud.OverviewVisibleWidth >= expandedOverviewWidth - 4.0f, 0.6f);
        var reopenOverviewReady = !_hud.IsOverviewCollapsed
            && _hud.ActiveWorkspaceId == workspaceBeforeCollapse
            && _hud.OverviewCollapseButtonText.Contains("收起", global::System.StringComparison.Ordinal);

        var worldWorkspaceId = UseLargeTestScenario ? DiagnosticsWorkspaceId : CommandWorkspaceId;
        _hud.SelectWorkspace(worldWorkspaceId);
        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
        var worldReady = _hud.ActiveWorkspaceId == worldWorkspaceId && _hud.IsWorkspaceVisible(worldWorkspaceId) && _editorOpen == editorWasOpen;

        if (UseLargeTestScenario)
        {
            _hud.SelectWorkspace(OverviewWorkspaceId);
            await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
            worldReady &= _hud.ActiveWorkspaceId == OverviewWorkspaceId && _hud.IsWorkspaceVisible(OverviewWorkspaceId);
        }

        SetEditorOpenState(false);
        await WaitForCondition(() => !_hud.IsEditorVisible, 0.4f);
        var closePreservesWorkspace = _hud.ActiveWorkspaceId == (UseLargeTestScenario ? OverviewWorkspaceId : worldWorkspaceId) || _hud.ActiveWorkspaceId == worldWorkspaceId;
        closePreservesWorkspace &= !_hud.IsEditorVisible;

        SetEditorOpenState(true);
        await WaitForCondition(() => _hud.IsEditorVisible, 0.4f);
        var reopenPreservesWorkspace = _hud.ActiveWorkspaceId == (UseLargeTestScenario ? OverviewWorkspaceId : worldWorkspaceId) || _hud.ActiveWorkspaceId == worldWorkspaceId;
        reopenPreservesWorkspace &= _hud.IsEditorVisible;

        if (!editorWasOpen)
        {
            SetEditorOpenState(false);
            await WaitForCondition(() => !_hud.IsEditorVisible, 0.4f);
        }

        return blueprintReady
            && buildReady
            && editorReady
            && testingReady
            && detailsReady
            && savesReady
            && chromeEmbedded
            && editorPanelBuildFocused
            && collapseReady
            && reopenOverviewReady
            && worldReady
            && closePreservesWorkspace
            && reopenPreservesWorkspace;
    }

    private async Task<bool> RunDebugInteriorSupportSmoke()
    {
        if (_mobileFactory is null || _simulation is null)
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
            FactoryIndustrialStandards.GetBuildCatalog(FactorySiteKind.Interior),
            "调试舱段",
            expectedKinds);
        var authoredInteriorDebugFree = !ContainsDebugStructure(_mobileFactory.InteriorSite.GetStructures());

        if (!TryFindInteriorPlacementWithOutput(BuildPrototypeKind.DebugCombatSource, FacingDirection.East, out var sourceCell, out var sinkCell)
            || !TryFindInteriorPlacement(BuildPrototypeKind.DebugPowerGenerator, FacingDirection.East, out var generatorCell))
        {
            return false;
        }

        var sourcePlaced = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.DebugCombatSource, sourceCell, FacingDirection.East);
        var sinkPlaced = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.Sink, sinkCell, FacingDirection.East);
        var generatorPlaced = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.DebugPowerGenerator, generatorCell, FacingDirection.East);
        if (!sourcePlaced
            || !sinkPlaced
            || !generatorPlaced
            || !_mobileFactory.TryGetInteriorStructure(sourceCell, out var sourceStructure)
            || sourceStructure is not DebugCombatSourceStructure
            || !_mobileFactory.TryGetInteriorStructure(sinkCell, out var sinkStructure)
            || sinkStructure is not SinkStructure sink
            || !_mobileFactory.TryGetInteriorStructure(generatorCell, out var generatorStructure)
            || generatorStructure is not DebugPowerGeneratorStructure generator)
        {
            return false;
        }

        var debugRecipeConfigured = sourceStructure.TrySetDetailRecipe("debug-repair-kit-source");
        var sourceRecipeActive = sourceStructure.GetDetailModel().RecipeSection?.ActiveRecipeId == "debug-repair-kit-source";

        await ToSignal(GetTree().CreateTimer(2.5f), SceneTreeTimer.SignalName.Timeout);
        var sourceDelivered = sink.DeliveredTotal > 0;
        var generatorStable = Mathf.IsEqualApprox(generator.GetAvailablePower(_simulation), generator.NominalPowerSupply);

        await ToSignal(GetTree().CreateTimer(0.8f), SceneTreeTimer.SignalName.Timeout);
        var generatorStillStable = Mathf.IsEqualApprox(generator.GetAvailablePower(_simulation), generator.NominalPowerSupply);
        var removedGenerator = _mobileFactory.RemoveInteriorStructure(generatorCell);
        var removedSink = _mobileFactory.RemoveInteriorStructure(sinkCell);
        var removedSource = _mobileFactory.RemoveInteriorStructure(sourceCell);
        return catalogReady
            && authoredInteriorDebugFree
            && debugRecipeConfigured
            && sourceRecipeActive
            && sourceDelivered
            && generatorStable
            && generatorStillStable
            && removedGenerator
            && removedSink
            && removedSource;
    }

    private static bool HasWorkspace(string workspaceId, IReadOnlyList<string> workspaceIds)
    {
        return FactoryDemoSmokeSupport.HasWorkspace(workspaceIds, workspaceId);
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

    private static bool ContainsDebugStructure(IEnumerable<FactoryStructure> structures)
    {
        foreach (var structure in structures)
        {
            if (FactoryIndustrialStandards.IsDebugStructure(structure.Kind))
            {
                return true;
            }
        }

        return false;
    }

    private bool PlayerInventoryContainsDebugKit()
    {
        if (_playerController is null)
        {
            return false;
        }

        var snapshot = _playerController.BackpackInventory.Snapshot();
        for (var index = 0; index < snapshot.Length; index++)
        {
            if (!snapshot[index].HasItem
                || snapshot[index].Item is not FactoryItem item
                || !FactoryPresentation.TryGetPlaceableStructureKind(item, out var kind)
                || !FactoryIndustrialStandards.IsDebugStructure(kind))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool TryFindInteriorPlacement(BuildPrototypeKind kind, FacingDirection facing, out Vector2I cell)
    {
        cell = Vector2I.Zero;
        if (_mobileFactory is null)
        {
            return false;
        }

        for (var y = _mobileFactory.Profile.InteriorHeight - 1; y >= 0; y--)
        {
            for (var x = _mobileFactory.Profile.InteriorWidth - 1; x >= 0; x--)
            {
                var candidate = new Vector2I(x, y);
                if (!_mobileFactory.CanPlaceInterior(kind, candidate, facing))
                {
                    continue;
                }

                cell = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryFindInteriorPlacementWithOutput(BuildPrototypeKind sourceKind, FacingDirection facing, out Vector2I sourceCell, out Vector2I sinkCell)
    {
        sourceCell = Vector2I.Zero;
        sinkCell = Vector2I.Zero;
        if (_mobileFactory is null)
        {
            return false;
        }

        var sourceFootprint = FactoryStructureFactory.GetFootprint(sourceKind);
        for (var y = _mobileFactory.Profile.InteriorHeight - 1; y >= 0; y--)
        {
            for (var x = _mobileFactory.Profile.InteriorWidth - 1; x >= 0; x--)
            {
                var candidate = new Vector2I(x, y);
                if (!_mobileFactory.CanPlaceInterior(sourceKind, candidate, facing))
                {
                    continue;
                }

                var outputCell = sourceFootprint.ResolveOutputCell(candidate, facing);
                if (!_mobileFactory.CanPlaceInterior(BuildPrototypeKind.Sink, outputCell, facing))
                {
                    continue;
                }

                sourceCell = candidate;
                sinkCell = outputCell;
                return true;
            }
        }

        return false;
    }

    private async void RunMiningPortSmokeChecks()
    {
        if (_grid is null || _mobileFactory is null || _structureRoot is null || _hud is null || _simulation is null || _sinkA is null)
        {
            GD.PushError("MOBILE_FACTORY_MINING_PORT_SMOKE_FAILED missing grid, factory, hud, simulation, sink, or structure root.");
            GetTree().Quit(1);
            return;
        }

        await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);

        var defaultOutputDeliveredBaseline = GetScenarioDeliveryTotal();
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = AnchorA;
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, AnchorA, FacingDirection.East);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State != MobileFactoryLifecycleState.AutoDeploying, MiningPortSmokeDeployTimeoutSeconds);
        var baselineOutputDeploy = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        await ToSignal(GetTree().CreateTimer(1.6f), SceneTreeTimer.SignalName.Timeout);
        var baselineOutputConnected = _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.OutputPort);
        PrimeFocusedOutputPorts(_mobileFactory);
        await ToSignal(GetTree().CreateTimer(4.2f), SceneTreeTimer.SignalName.Timeout);
        var baselineOutputDelivered = GetScenarioDeliveryTotal() > defaultOutputDeliveredBaseline;
        var baselineOutputRecall = _mobileFactory.ReturnToTransitMode();
        if (baselineOutputRecall)
        {
            await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 1.2f);
        }
        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);

        var swapped = ReplaceFocusedInputAttachmentWithMiningPort();
        var anchorAValid = swapped && _mobileFactory.EvaluateDeployment(_grid, MiningAnchorA, FacingDirection.East).State == MobileFactoryDeployState.Valid;
        var anchorBValid = swapped && _mobileFactory.EvaluateDeployment(_grid, MiningAnchorB, FacingDirection.East).State == MobileFactoryDeployState.Valid;
        var warningAnchor = Vector2I.Zero;
        var warningAnchorFound = swapped && TryFindMiningWarningAnchor(FacingDirection.East, out warningAnchor);
        var warningPreviewTone = false;
        var warningArrowVisible = false;
        var warningMiningPreviewVisible = false;
        var deployedAtWarningAnchor = false;
        var connectedAtWarningAnchor = false;
        var payloadVisibleAtWarningAnchor = false;
        var deliveredAtWarningAnchor = false;
        var recalledFromWarningAnchor = false;
        var payloadRetractedAfterWarningAnchor = false;

        var miningPort = swapped ? GetMiningInputPort() : null;
        var initialBuiltStakeCount = miningPort?.BuiltStakeCount ?? 0;
        var deployedStakeCountAtAnchorA = 0;
        var eligibleStakeCountAtAnchorA = 0;
        var stakeDestroyedAtAnchorA = false;
        var builtStakeCountAfterDamage = 0;
        var partialStakeDeployAtAnchorB = false;
        var detailVisibleForRebuild = false;
        var rebuildActionWorked = false;
        var fullStakeDeployAfterRebuild = false;
        var miningStayedConnectedAfterOutputRemoval = false;
        var otherOutputsStayedConnectedAfterOutputRemoval = false;

        if (warningAnchorFound)
        {
            var deliveredBeforeWarningAnchor = GetScenarioDeliveryTotal();
            SetControlMode(MobileFactoryControlMode.DeployPreview);
            _selectedDeployFacing = FacingDirection.East;
            _hoveredAnchor = warningAnchor;
            _hasHoveredAnchor = true;
            _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, warningAnchor, FacingDirection.East);
            _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
            UpdateWorldPreview();
            UpdateWorldStatusMessage(0.0);
            warningPreviewTone = _worldStatusTone == FactoryStatusTone.Warning;
            warningArrowVisible = _worldPreviewFacingArrow is not null
                && _worldPreviewFacingArrow.Visible
                && _worldPreviewFacingArrow.GetChildCount() >= 3;
            var expectedWarningPreviewCount = 0;
            if (_currentDeployEvaluation is not null)
            {
                for (var attachmentIndex = 0; attachmentIndex < _currentDeployEvaluation.AttachmentEvaluations.Count; attachmentIndex++)
                {
                    var attachmentEvaluation = _currentDeployEvaluation.AttachmentEvaluations[attachmentIndex];
                    if (attachmentEvaluation.Attachment.Kind == BuildPrototypeKind.MiningInputPort)
                    {
                        expectedWarningPreviewCount += attachmentEvaluation.PreviewWorldCells.Count;
                    }
                }
            }

            warningMiningPreviewVisible = expectedWarningPreviewCount > 0
                && CountVisibleWorldPreviewMiningMeshes() == expectedWarningPreviewCount;

            ConfirmDeployPreview();
            await WaitForCondition(() => _mobileFactory.State != MobileFactoryLifecycleState.AutoDeploying, MiningPortSmokeDeployTimeoutSeconds);

            deployedAtWarningAnchor = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
            await ToSignal(GetTree().CreateTimer(0.9f), SceneTreeTimer.SignalName.Timeout);
            connectedAtWarningAnchor = _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.MiningInputPort);
            payloadVisibleAtWarningAnchor = CountMiningStakeStructures(_structureRoot) > 0;
            await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
            deliveredAtWarningAnchor = GetScenarioDeliveryTotal() > deliveredBeforeWarningAnchor;

            recalledFromWarningAnchor = _mobileFactory.ReturnToTransitMode();
            if (recalledFromWarningAnchor)
            {
                await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 1.2f);
            }
            await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
            payloadRetractedAfterWarningAnchor = CountMiningStakeStructures(_structureRoot) == 0;
        }

        var deliveredBeforeAnchorA = GetScenarioDeliveryTotal();

        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = MiningAnchorA;
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, MiningAnchorA, FacingDirection.East);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State != MobileFactoryLifecycleState.AutoDeploying, MiningPortSmokeDeployTimeoutSeconds);

        var deployedAtAnchorA = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        await WaitForCondition(() =>
        {
            var port = GetMiningInputPort();
            return port is not null
                && port.EligibleStakeCount > 0
                && port.DeployingStakeCount == 0
                && port.DeployedStakeCount == port.EligibleStakeCount;
        }, 4.0f);
        var connectedAtAnchorA = _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.MiningInputPort);
        miningPort = GetMiningInputPort();
        deployedStakeCountAtAnchorA = miningPort?.DeployedStakeCount ?? 0;
        eligibleStakeCountAtAnchorA = miningPort?.EligibleStakeCount ?? 0;
        var payloadVisibleAtAnchorA = CountMiningStakeStructures(_structureRoot) == deployedStakeCountAtAnchorA && deployedStakeCountAtAnchorA > 0;
        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
        PrimeFocusedOutputPorts(_mobileFactory);
        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
        var deliveredAtAnchorA = GetScenarioDeliveryTotal() > deliveredBeforeAnchorA;

        if (deployedAtAnchorA && miningPort is not null && payloadVisibleAtAnchorA)
        {
            var stakeBeforeDamage = CountMiningStakeStructures(_structureRoot);
            if (FindFirstMiningStakeStructure(_structureRoot) is MobileFactoryMiningStakeStructure stake)
            {
                stake.ApplyDamage(stake.MaxHealth, _simulation!);
                await WaitForCondition(() => CountMiningStakeStructures(_structureRoot) == Mathf.Max(0, stakeBeforeDamage - 1), 2.0f);
                miningPort = GetMiningInputPort();
                builtStakeCountAfterDamage = miningPort?.BuiltStakeCount ?? 0;
                stakeDestroyedAtAnchorA = CountMiningStakeStructures(_structureRoot) == Mathf.Max(0, stakeBeforeDamage - 1)
                    && builtStakeCountAfterDamage == Mathf.Max(0, initialBuiltStakeCount - 1);
            }
        }

        var recalledFromAnchorA = _mobileFactory.ReturnToTransitMode();
        if (recalledFromAnchorA)
        {
            await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 1.2f);
        }
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        var payloadRetractedAfterAnchorA = CountMiningStakeStructures(_structureRoot) == 0;

        var deliveredBeforeAnchorB = GetScenarioDeliveryTotal();
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = MiningAnchorB;
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, MiningAnchorB, FacingDirection.East);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State != MobileFactoryLifecycleState.AutoDeploying, MiningPortSmokeDeployTimeoutSeconds);

        var deployedAtAnchorB = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        await WaitForCondition(() =>
        {
            var port = GetMiningInputPort();
            return port is not null
                && port.DeployingStakeCount == 0
                && port.DeployedStakeCount > 0;
        }, 4.0f);
        var connectedAtAnchorB = _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.MiningInputPort);
        miningPort = GetMiningInputPort();
        var payloadVisibleAtAnchorB = CountMiningStakeStructures(_structureRoot) > 0;
        partialStakeDeployAtAnchorB = miningPort is not null
            && miningPort.DeployedStakeCount > 0
            && miningPort.DeployedStakeCount < miningPort.EligibleStakeCount;
        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
        PrimeFocusedOutputPorts(_mobileFactory);
        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
        var deliveredAtAnchorB = GetScenarioDeliveryTotal() > deliveredBeforeAnchorB;

        var recalledFromAnchorB = _mobileFactory.ReturnToTransitMode();
        if (recalledFromAnchorB)
        {
            await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 1.2f);
        }
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        var payloadRetractedAfterAnchorB = CountMiningStakeStructures(_structureRoot) == 0;

        miningPort = GetMiningInputPort();
        if (miningPort is not null)
        {
            _selectedInteriorStructure = miningPort;
            UpdateHud();
            detailVisibleForRebuild = _hud is not null
                && _hud.IsDetailVisible
                && _hud.DetailTitleText.Contains("采矿输入端口", global::System.StringComparison.Ordinal);
            HandleEditorDetailActionRequested("build-one-stake");
            miningPort = GetMiningInputPort();
            rebuildActionWorked = miningPort is not null && miningPort.BuiltStakeCount == initialBuiltStakeCount;
        }

        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = MiningAnchorB;
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, MiningAnchorB, FacingDirection.East);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State != MobileFactoryLifecycleState.AutoDeploying, MiningPortSmokeDeployTimeoutSeconds);
        if (_mobileFactory.State == MobileFactoryLifecycleState.Deployed)
        {
            await WaitForCondition(() =>
            {
                var port = GetMiningInputPort();
                return port is not null
                    && port.EligibleStakeCount > 0
                    && port.DeployingStakeCount == 0
                    && port.DeployedStakeCount == port.EligibleStakeCount;
            }, 4.0f);
            miningPort = GetMiningInputPort();
            fullStakeDeployAfterRebuild = miningPort is not null
                && miningPort.DeployedStakeCount == miningPort.EligibleStakeCount
                && miningPort.DeployedStakeCount > 0;

            var connectedOutputsBeforeRemoval = CountConnectedAttachments(BuildPrototypeKind.OutputPort);
            var removedAuxOutput = _mobileFactory.RemoveInteriorStructure(new Vector2I(7, 3));
            await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
            miningPort = GetMiningInputPort();
            miningStayedConnectedAfterOutputRemoval = removedAuxOutput
                && _mobileFactory.State == MobileFactoryLifecycleState.Deployed
                && _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.MiningInputPort)
                && miningPort is not null
                && miningPort.DeployedStakeCount > 0
                && CountMiningStakeStructures(_structureRoot) > 0;
            otherOutputsStayedConnectedAfterOutputRemoval = removedAuxOutput
                && connectedOutputsBeforeRemoval > 1
                && CountConnectedAttachments(BuildPrototypeKind.OutputPort) >= 1;
        }

        if (!swapped
            || !baselineOutputDeploy
            || !baselineOutputConnected
            || !baselineOutputDelivered
            || !baselineOutputRecall
            || !anchorAValid
            || !anchorBValid
            || !warningAnchorFound
            || !warningPreviewTone
            || !warningArrowVisible
            || !warningMiningPreviewVisible
            || !deployedAtWarningAnchor
            || connectedAtWarningAnchor
            || payloadVisibleAtWarningAnchor
            || deliveredAtWarningAnchor
            || !recalledFromWarningAnchor
            || !payloadRetractedAfterWarningAnchor
            || !deployedAtAnchorA
            || !connectedAtAnchorA
            || !payloadVisibleAtAnchorA
            || deployedStakeCountAtAnchorA != eligibleStakeCountAtAnchorA
            || !stakeDestroyedAtAnchorA
            || !recalledFromAnchorA
            || !payloadRetractedAfterAnchorA
            || !deployedAtAnchorB
            || !connectedAtAnchorB
            || !payloadVisibleAtAnchorB
            || !partialStakeDeployAtAnchorB
            || !recalledFromAnchorB
            || !payloadRetractedAfterAnchorB
            || !detailVisibleForRebuild
            || !rebuildActionWorked
            || !fullStakeDeployAfterRebuild
            || !miningStayedConnectedAfterOutputRemoval)
        {
            GD.PushError($"MOBILE_FACTORY_MINING_PORT_SMOKE_FAILED baselineOutputDeploy={baselineOutputDeploy} baselineOutputConnected={baselineOutputConnected} baselineOutputDelivered={baselineOutputDelivered} baselineOutputRecall={baselineOutputRecall} swapped={swapped} anchorAValid={anchorAValid} anchorBValid={anchorBValid} warningAnchorFound={warningAnchorFound} warningPreviewTone={warningPreviewTone} warningArrowVisible={warningArrowVisible} warningMiningPreviewVisible={warningMiningPreviewVisible} deployedAtWarningAnchor={deployedAtWarningAnchor} connectedAtWarningAnchor={connectedAtWarningAnchor} payloadVisibleAtWarningAnchor={payloadVisibleAtWarningAnchor} deliveredAtWarningAnchor={deliveredAtWarningAnchor} recalledFromWarningAnchor={recalledFromWarningAnchor} payloadRetractedAfterWarningAnchor={payloadRetractedAfterWarningAnchor} deployedAtAnchorA={deployedAtAnchorA} connectedAtAnchorA={connectedAtAnchorA} payloadVisibleAtAnchorA={payloadVisibleAtAnchorA} deliveredAtAnchorA={deliveredAtAnchorA} deployedStakeCountAtAnchorA={deployedStakeCountAtAnchorA} eligibleStakeCountAtAnchorA={eligibleStakeCountAtAnchorA} stakeDestroyedAtAnchorA={stakeDestroyedAtAnchorA} builtStakeCountAfterDamage={builtStakeCountAfterDamage} recalledFromAnchorA={recalledFromAnchorA} payloadRetractedAfterAnchorA={payloadRetractedAfterAnchorA} deployedAtAnchorB={deployedAtAnchorB} connectedAtAnchorB={connectedAtAnchorB} payloadVisibleAtAnchorB={payloadVisibleAtAnchorB} deliveredAtAnchorB={deliveredAtAnchorB} partialStakeDeployAtAnchorB={partialStakeDeployAtAnchorB} recalledFromAnchorB={recalledFromAnchorB} payloadRetractedAfterAnchorB={payloadRetractedAfterAnchorB} detailVisibleForRebuild={detailVisibleForRebuild} rebuildActionWorked={rebuildActionWorked} fullStakeDeployAfterRebuild={fullStakeDeployAfterRebuild} miningStayedConnectedAfterOutputRemoval={miningStayedConnectedAfterOutputRemoval} otherOutputsStayedConnectedAfterOutputRemoval={otherOutputsStayedConnectedAfterOutputRemoval}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_MINING_PORT_SMOKE_OK warningAnchor={warningAnchorFound} anchorADelivered={deliveredAtAnchorA} anchorBDelivered={deliveredAtAnchorB} rebuilt={rebuildActionWorked} totalDelivered={GetScenarioDeliveryTotal()}");
        GetTree().Quit();
    }

    private async Task<bool> RunEditorDetailSmoke()
    {
        if (_mobileFactory is null || _hud is null || _simulation is null)
        {
            return false;
        }

        var editorWasOpen = _editorOpen;
        if (!editorWasOpen)
        {
            SetEditorOpenState(true);
            await WaitForCondition(() => _hud.IsEditorVisible, 0.4f);
        }

        if (!_mobileFactory.TryGetInteriorStructure(FocusedAssemblerCell, out var recipeAssemblerStructure)
            || recipeAssemblerStructure is not AssemblerStructure recipeAssembler
            || !_mobileFactory.TryGetInteriorStructure(FocusedIronBufferCell, out var storageStructure)
            || storageStructure is not StorageStructure storage
            || !_mobileFactory.TryGetInteriorStructure(FocusedAmmoAssemblerCell, out var ammoAssemblerStructure)
            || ammoAssemblerStructure is not AmmoAssemblerStructure ammoAssembler
            || !_mobileFactory.TryGetInteriorStructure(FocusedTurretCell, out var turretStructure)
            || turretStructure is not GunTurretStructure turret)
        {
            return false;
        }

        recipeAssembler.TrySetDetailRecipe("gear");
        ammoAssembler.TrySetDetailRecipe("standard-ammo");

        await ToSignal(GetTree().CreateTimer(2.4f), SceneTreeTimer.SignalName.Timeout);

        recipeAssembler.TryReceiveProvidedItem(
            _simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate),
            GetPrimaryInputCell(recipeAssembler),
            _simulation);
        recipeAssembler.TryReceiveProvidedItem(
            _simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate),
            GetPrimaryInputCell(recipeAssembler),
            _simulation);
        storage.TryReceiveProvidedItem(
            _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
            storage.Cell + Vector2I.Left,
            _simulation);
        turret.TryReceiveProvidedItem(
            _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.AmmoMagazine),
            turret.Cell + Vector2I.Left,
            _simulation);

        _selectedInteriorStructure = storage;
        UpdateHud();
        var storageDetailVisible = _hud.IsDetailVisible && _hud.DetailTitleText.Contains("仓储", global::System.StringComparison.Ordinal);

        var storageDetail = storage.GetDetailModel();
        if (storageDetail.InventorySections.Count == 0)
        {
            return false;
        }

        var stackLimit = FactoryItemCatalog.GetMaxStackSize(FactoryItemKind.GenericCargo);
        for (var index = 0; index < stackLimit + 2; index++)
        {
            var seededCargo = _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo);
            if (!storage.TryReceiveProvidedItem(seededCargo, storage.Cell + Vector2I.Left, _simulation))
            {
                return false;
            }
        }

        storageDetail = storage.GetDetailModel();

        var mergeSourceSlot = new Vector2I(-1, -1);
        var mergeTargetSlot = new Vector2I(-1, -1);
        var emptySlot = new Vector2I(-1, -1);
        var targetStackBeforeMove = 0;
        var totalStackCountBeforeMove = 0;
        var stackCountsVisible = false;
        for (var index = 0; index < storageDetail.InventorySections[0].Slots.Count; index++)
        {
            var slot = storageDetail.InventorySections[0].Slots[index];
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
            for (var index = 0; index < storageDetail.InventorySections[0].Slots.Count; index++)
            {
                var slot = storageDetail.InventorySections[0].Slots[index];
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
        var inventoryMoveWorked = mergeSourceSlot.X >= 0
            && mergeTargetSlot.X >= 0
            && storage.TryMoveDetailInventoryItem("storage-buffer", mergeSourceSlot, mergeTargetSlot);

        var movedDetail = storage.GetDetailModel();
        var targetStackAfterMove = 0;
        var totalStackCountAfterMove = 0;
        var splitSourceSlot = new Vector2I(-1, -1);
        var splitSourceCountBefore = 0;
        for (var index = 0; index < movedDetail.InventorySections[0].Slots.Count; index++)
        {
            var slot = movedDetail.InventorySections[0].Slots[index];
            totalStackCountAfterMove += slot.StackCount;
            if (slot.Position == mergeTargetSlot)
            {
                targetStackAfterMove = slot.StackCount;
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

        await ToSignal(GetTree().CreateTimer(1.6f), SceneTreeTimer.SignalName.Timeout);

        _selectedInteriorStructure = recipeAssembler;
        UpdateHud();

        _selectedInteriorStructure = turret;
        UpdateHud();

        var editorVisible = _hud.IsEditorVisible;
        if (!editorWasOpen)
        {
            SetEditorOpenState(false);
            await WaitForCondition(() => !_hud.IsEditorVisible, 0.4f);
        }

        return storageDetailVisible
            && stackCountsVisible
            && emptyDragRejected
            && inventoryMoveWorked
            && targetStackAfterMove > targetStackBeforeMove
            && totalStackCountAfterMove == totalStackCountBeforeMove
            && splitMoveWorked
            && splitTargetCount > 0
            && splitTargetCount < splitSourceCountBefore
            && splitSourceCountAfter > 0
            && splitTotalStackCount == totalStackCountBeforeMove
            && editorVisible;
    }

    private async Task<bool> RunWorldDetailSmoke()
    {
        if (_grid is null || _hud is null)
        {
            return false;
        }

        FactoryStructure? worldStructure = null;
        foreach (var structure in _grid.GetStructures())
        {
            if (GodotObject.IsInstanceValid(structure) && structure.IsInsideTree())
            {
                worldStructure = structure;
                break;
            }
        }

        var deposits = _grid.GetResourceDeposits();
        var deposit = deposits.Count > 0 ? deposits[0] : null;
        if (worldStructure is null || deposit is null)
        {
            return false;
        }

        if (_mobileFactory is not null
            && _mobileFactory.TryGetInteriorStructure(FocusedIronBufferCell, out var interiorStructure))
        {
            _selectedInteriorStructure = interiorStructure;
            UpdateHud();
        }

        _selectedWorldResourceDeposit = null;
        _selectedWorldStructure = worldStructure;
        UpdateHud();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var structureDetailVisible = _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains(worldStructure.DisplayName, global::System.StringComparison.Ordinal);

        _selectedWorldStructure = null;
        _selectedWorldResourceDeposit = deposit;
        UpdateHud();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var depositDetailVisible = _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains(deposit.DisplayName, global::System.StringComparison.Ordinal);

        _selectedWorldStructure = null;
        _selectedWorldResourceDeposit = null;
        _selectedInteriorStructure = null;
        UpdateHud();
        return structureDetailVisible && depositDetailVisible;
    }

    private async Task<bool> RunInteriorBlueprintSmoke()
    {
        if (_mobileFactory is null || _interiorBlueprintSite is null || _simulation is null)
        {
            if (HasFocusedSmokeTestFlag())
            {
                GD.Print("MOBILE_FACTORY_BLUEPRINT_SMOKE missingDependencies");
            }
            return false;
        }

        if (!_mobileFactory.TryGetInteriorStructure(FocusedAssemblerCell, out var assemblerStructure)
            || assemblerStructure is not AssemblerStructure assembler
            || !_mobileFactory.TryGetInteriorStructure(FocusedAmmoAssemblerCell, out var ammoAssemblerStructure)
            || ammoAssemblerStructure is not AmmoAssemblerStructure ammoAssembler)
        {
            if (HasFocusedSmokeTestFlag())
            {
                GD.Print("MOBILE_FACTORY_BLUEPRINT_SMOKE missingMachines");
            }
            return false;
        }

        var assemblerConfigured = HasBlueprintRecipe(assembler, "gear") || assembler.TrySetDetailRecipe("gear");
        var ammoAssemblerConfigured = HasBlueprintRecipe(ammoAssembler, "standard-ammo") || ammoAssembler.TrySetDetailRecipe("standard-ammo");
        if (!assemblerConfigured || !ammoAssemblerConfigured)
        {
            if (HasFocusedSmokeTestFlag())
            {
                GD.Print($"MOBILE_FACTORY_BLUEPRINT_SMOKE recipeSetupFailed assemblerConfigured={assemblerConfigured} ammoAssemblerConfigured={ammoAssemblerConfigured}");
            }
            return false;
        }

        var blueprintCaptureRect = new Rect2I(
            Mathf.Min(FocusedAssemblerCell.X, FocusedAmmoAssemblerCell.X),
            Mathf.Min(FocusedAssemblerCell.Y, FocusedAmmoAssemblerCell.Y),
            4,
            5);
        var captured = FactoryBlueprintCaptureService.CaptureSelection(
            _interiorBlueprintSite,
            blueprintCaptureRect,
            "Smoke Interior Blueprint");
        if (captured is null || captured.StructureCount < 2)
        {
            if (HasFocusedSmokeTestFlag())
            {
                GD.Print($"MOBILE_FACTORY_BLUEPRINT_SMOKE captureFailed captured={(captured is null ? -1 : captured.StructureCount)}");
            }
            return false;
        }

        var savedRecord = new FactoryBlueprintRecord(
            captured.Id,
            "Smoke Interior Blueprint",
            captured.SourceSiteKind,
            captured.SuggestedAnchorCell,
            captured.BoundsSize,
            captured.Entries,
            captured.RequiredAttachments);
        savedRecord = FactoryBlueprintWorkflowBridge.SavePendingCapture(savedRecord, savedRecord.DisplayName);

        var storedBlueprint = FactoryBlueprintLibrary.FindById(savedRecord.Id);
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        if (storedBlueprint is null || activeBlueprint?.Id != savedRecord.Id)
        {
            if (HasFocusedSmokeTestFlag())
            {
                GD.Print($"MOBILE_FACTORY_BLUEPRINT_SMOKE libraryFailed stored={storedBlueprint is not null} active={activeBlueprint?.Id == savedRecord.Id}");
            }
            return false;
        }

        var oversizeRecord = new FactoryBlueprintRecord(
            $"{savedRecord.Id}-oversize",
            "Oversize Smoke Interior Blueprint",
            savedRecord.SourceSiteKind,
            savedRecord.SuggestedAnchorCell,
            new Vector2I(_mobileFactory.Profile.InteriorWidth + 1, savedRecord.BoundsSize.Y),
            savedRecord.Entries,
            savedRecord.RequiredAttachments);
        var defaultAnchor = _interiorBlueprintSite.GetDefaultApplyAnchor(savedRecord);
        var invalidBoundsPlan = FactoryBlueprintPlanner.CreatePlan(oversizeRecord, _interiorBlueprintSite, defaultAnchor);
        var boundsRejected = !invalidBoundsPlan.IsValid
            && invalidBoundsPlan.GetIssueSummary().Contains("尺寸", global::System.StringComparison.Ordinal);

        var invalidPlan = FactoryBlueprintPlanner.CreatePlan(savedRecord, _interiorBlueprintSite, defaultAnchor);
        var overlapRejected = !invalidPlan.IsValid;

        for (var y = blueprintCaptureRect.Position.Y; y < blueprintCaptureRect.End.Y; y++)
        {
            for (var x = blueprintCaptureRect.Position.X; x < blueprintCaptureRect.End.X; x++)
            {
                _mobileFactory.TryGetInteriorStructure(new Vector2I(x, y), out var existingStructure);
                if (existingStructure is not null)
                {
                    _mobileFactory.RemoveInteriorStructure(new Vector2I(x, y));
                }
            }
        }

        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

        var validPlan = FactoryBlueprintPlanner.CreatePlan(savedRecord, _interiorBlueprintSite, defaultAnchor);
        var committed = validPlan.IsValid && FactoryBlueprintPlanner.CommitPlan(validPlan, _interiorBlueprintSite);
        if (!validPlan.IsValid || !committed)
        {
            if (HasFocusedSmokeTestFlag())
            {
                GD.Print(
                    $"MOBILE_FACTORY_BLUEPRINT_SMOKE applyFailed " +
                    $"planValid={validPlan.IsValid} " +
                    $"committed={committed} " +
                    $"issues={validPlan.GetIssueSummary()}");
            }
            return false;
        }

        await ToSignal(GetTree().CreateTimer(2.5f), SceneTreeTimer.SignalName.Timeout);

        var restoredEntries = FactoryDemoSmokeSupport.CountMatchingPlacedEntries(
            validPlan.Entries,
            cell => _mobileFactory.TryGetInteriorStructure(cell, out var structure) ? structure : null);

        var assemblerRecipeRestored =
            _mobileFactory.TryGetInteriorStructure(FocusedAssemblerCell, out var restoredAssemblerStructure)
            && restoredAssemblerStructure is AssemblerStructure restoredAssembler
            && restoredAssembler.CaptureBlueprintConfiguration().TryGetValue("recipe_id", out var restoredAssemblerRecipe)
            && restoredAssemblerRecipe == "gear";
        var ammoRecipeRestored =
            _mobileFactory.TryGetInteriorStructure(FocusedAmmoAssemblerCell, out var restoredAmmoAssemblerStructure)
            && restoredAmmoAssemblerStructure is AmmoAssemblerStructure restoredAmmoAssembler
            && restoredAmmoAssembler.CaptureBlueprintConfiguration().TryGetValue("recipe_id", out var restoredAmmoRecipe)
            && restoredAmmoRecipe == "standard-ammo";
        var turretPrimed = false;
        if (_mobileFactory.TryGetInteriorStructure(FocusedTurretCell, out var restoredTurretStructure)
            && restoredTurretStructure is GunTurretStructure restoredTurret)
        {
            if (restoredTurret.BufferedAmmo <= 0)
            {
                restoredTurret.TryReceiveProvidedItem(
                    _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.AmmoMagazine),
                    restoredTurret.Cell + Vector2I.Left,
                    _simulation);
            }

            turretPrimed = restoredTurret.BufferedAmmo > 0;
        }

        var verified = boundsRejected
            && overlapRejected
            && restoredEntries == savedRecord.StructureCount
            && assemblerRecipeRestored
            && ammoRecipeRestored
            && turretPrimed;

        if (!verified && HasFocusedSmokeTestFlag())
        {
            GD.Print(
                $"MOBILE_FACTORY_BLUEPRINT_SMOKE " +
                $"captured={captured.StructureCount} " +
                $"stored={storedBlueprint is not null} " +
                $"active={activeBlueprint?.Id == savedRecord.Id} " +
                $"boundsRejected={boundsRejected} " +
                $"overlapRejected={overlapRejected} " +
                $"validPlan={validPlan.IsValid} " +
                $"restoredEntries={restoredEntries} " +
                $"expectedEntries={savedRecord.StructureCount} " +
                $"assemblerRecipeRestored={assemblerRecipeRestored} " +
                $"ammoRecipeRestored={ammoRecipeRestored} " +
                $"turretPrimed={turretPrimed} " +
                $"planIssues={validPlan.GetIssueSummary()}");
        }

        return verified;
    }

    private bool ClearInteriorStructuresForBlueprintSmoke()
    {
        if (_mobileFactory is null)
        {
            return false;
        }

        var cells = new List<Vector2I>();
        foreach (var structure in _mobileFactory.InteriorSite.GetStructures())
        {
            cells.Add(structure.Cell);
        }

        for (var index = 0; index < cells.Count; index++)
        {
            if (!_mobileFactory.RemoveInteriorStructure(cells[index]))
            {
                return false;
            }
        }

        return CountEditableInteriorStructures() == 0;
    }

    private bool ReplaceFocusedInputAttachmentWithMiningPort()
    {
        if (_mobileFactory is null)
        {
            return false;
        }

        var attachmentCell = new Vector2I(0, 3);
        if (_mobileFactory.TryGetInteriorStructure(attachmentCell, out var existingStructure)
            && existingStructure is MobileFactoryMiningInputPortStructure)
        {
            return true;
        }

        if (existingStructure is null || !_mobileFactory.RemoveInteriorStructure(attachmentCell))
        {
            return false;
        }

        return _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.MiningInputPort, attachmentCell, FacingDirection.West)
            && _mobileFactory.TryGetInteriorStructure(attachmentCell, out var miningStructure)
            && miningStructure is MobileFactoryMiningInputPortStructure;
    }

    private bool TryFindMiningBlockedAnchor(FacingDirection facing, out Vector2I anchor)
    {
        anchor = Vector2I.Zero;
        if (_mobileFactory is null || _grid is null)
        {
            return false;
        }

        for (var y = GetWorldMinCell(); y <= GetWorldMaxCell(); y++)
        {
            for (var x = GetWorldMinCell(); x <= GetWorldMaxCell(); x++)
            {
                var candidate = new Vector2I(x, y);
                var evaluation = _mobileFactory.EvaluateDeployment(_grid, candidate, facing);
                if (evaluation.State != MobileFactoryDeployState.Blocked)
                {
                    continue;
                }

                anchor = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryFindMiningWarningAnchor(FacingDirection facing, out Vector2I anchor)
    {
        anchor = Vector2I.Zero;
        if (_mobileFactory is null || _grid is null)
        {
            return false;
        }

        for (var y = GetWorldMinCell(); y <= GetWorldMaxCell(); y++)
        {
            for (var x = GetWorldMinCell(); x <= GetWorldMaxCell(); x++)
            {
                var candidate = new Vector2I(x, y);
                var evaluation = _mobileFactory.EvaluateDeployment(_grid, candidate, facing);
                if (evaluation.State != MobileFactoryDeployState.Warning)
                {
                    continue;
                }

                anchor = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryFindMiningValidAnchor(FacingDirection facing, out Vector2I anchor)
    {
        anchor = Vector2I.Zero;
        if (_mobileFactory is null || _grid is null)
        {
            return false;
        }

        for (var y = GetWorldMinCell(); y <= GetWorldMaxCell(); y++)
        {
            for (var x = GetWorldMinCell(); x <= GetWorldMaxCell(); x++)
            {
                var candidate = new Vector2I(x, y);
                var evaluation = _mobileFactory.EvaluateDeployment(_grid, candidate, facing);
                if (evaluation.State != MobileFactoryDeployState.Valid)
                {
                    continue;
                }

                anchor = candidate;
                return true;
            }
        }

        return false;
    }

    private static int CountNamedNodes(Node root, string nodeName)
    {
        var count = root.Name == nodeName ? 1 : 0;
        foreach (var child in root.GetChildren())
        {
            if (child is Node childNode)
            {
                count += CountNamedNodes(childNode, nodeName);
            }
        }

        return count;
    }

    private static int CountVisibleNodesWithPrefix(Node root, string namePrefix)
    {
        var count = root is Node3D node3D && node3D.Visible && root.Name.ToString().StartsWith(namePrefix, global::System.StringComparison.Ordinal)
            ? 1
            : 0;
        foreach (var child in root.GetChildren())
        {
            if (child is Node childNode)
            {
                count += CountVisibleNodesWithPrefix(childNode, namePrefix);
            }
        }

        return count;
    }

    private static int CountMiningStakeStructures(Node root)
    {
        var count = root is MobileFactoryMiningStakeStructure ? 1 : 0;
        foreach (var child in root.GetChildren())
        {
            if (child is Node childNode)
            {
                count += CountMiningStakeStructures(childNode);
            }
        }

        return count;
    }

    private static MobileFactoryMiningStakeStructure? FindFirstMiningStakeStructure(Node root)
    {
        if (root is MobileFactoryMiningStakeStructure miningStake)
        {
            return miningStake;
        }

        foreach (var child in root.GetChildren())
        {
            if (child is Node childNode)
            {
                var nestedStake = FindFirstMiningStakeStructure(childNode);
                if (nestedStake is not null)
                {
                    return nestedStake;
                }
            }
        }

        return null;
    }
    private MobileFactoryMiningInputPortStructure? GetMiningInputPort()
    {
        if (_mobileFactory is null)
        {
            return null;
        }

        return _mobileFactory.TryGetInteriorStructure(new Vector2I(0, 3), out var miningStructure)
            ? miningStructure as MobileFactoryMiningInputPortStructure
            : null;
    }

    private int CountVisibleWorldPreviewMiningMeshes()
    {
        var count = 0;
        for (var index = 0; index < _worldPreviewMiningMeshes.Count; index++)
        {
            if (_worldPreviewMiningMeshes[index].Visible)
            {
                count++;
            }
        }

        return count;
    }

    private async void RunLargeScenarioSmokeChecks()
    {
        if (_grid is null || _mobileFactory is null || _hud is null || _cameraRig is null || _simulation is null || _backgroundFactories.Count < 3 || _scenarioSinks.Count < 4)
        {
            GD.PushError("MOBILE_FACTORY_LARGE_SMOKE_FAILED missing grid, player factory, hud, camera, background actors, or scenario sinks.");
            GetTree().Quit(1);
            return;
        }

        await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);

        var allFactories = new List<MobileFactoryInstance> { _mobileFactory };
        allFactories.AddRange(_backgroundFactories);
        var deployedCount = 0;
        var inTransitCount = 0;
        var profileIds = new HashSet<string>();
        var presetIds = new HashSet<string>();
        foreach (var factory in allFactories)
        {
            profileIds.Add(factory.Profile.Id);
            presetIds.Add(factory.InteriorPreset.Id);
            if (factory.State == MobileFactoryLifecycleState.Deployed)
            {
                deployedCount++;
            }
            else if (factory.State == MobileFactoryLifecycleState.InTransit)
            {
                inTransitCount++;
            }
        }

        var mixedStates = deployedCount >= 2 && inTransitCount >= 1;
        var variedProfiles = profileIds.Count >= 3;
        var variedPresets = presetIds.Count >= 3;

        var backgroundStartPositions = new List<Vector3>();
        foreach (var factory in _backgroundFactories)
        {
            backgroundStartPositions.Add(factory.WorldFocusPoint);
        }
        var initialDelivered = GetScenarioDeliveryTotal();

        var playerStartPosition = _mobileFactory.WorldFocusPoint;
        _mobileFactory.ApplyTransitInput(_grid, 1.0f, -1.0f, 0.5);
        var playerMoved = _mobileFactory.WorldFocusPoint.DistanceTo(playerStartPosition) > 0.05f;
        var workspaceNavigationVerified = await RunWorkspaceNavigationSmoke();

        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        var editorVisible = _hud.IsEditorVisible;
        SetEditorOpenState(false);
        var turretShotsBaseline = CountMobileTurretShots();

        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = new Vector2I(-12, 3);
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, _hoveredAnchor, FacingDirection.East);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.Deployed, 5.0f);
        var playerDeployed = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);

        for (var index = 0; index < allFactories.Count; index++)
        {
            if (allFactories[index].State == MobileFactoryLifecycleState.Deployed)
            {
                PrimeScenarioOutputPorts(allFactories[index]);
            }
        }

        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
        var backgroundMoved = false;
        for (var i = 0; i < _backgroundFactories.Count; i++)
        {
            if (_backgroundFactories[i].WorldFocusPoint.DistanceTo(backgroundStartPositions[i]) > 0.10f)
            {
                backgroundMoved = true;
                break;
            }
        }
        var deliveredDuringRun = GetScenarioDeliveryTotal() > initialDelivered;
        var inputDeliveredDuringRun = deliveredDuringRun;
        var playerTurretTrackedThreats = CountMobileTurretShots() > turretShotsBaseline;
        var heavyEnemyCount = CountActiveHeavyWorldEnemies();
        var worldCombatActive = (_simulation.ActiveEnemyCount > 0 || _simulation.DefeatedEnemyCount > 0) && heavyEnemyCount > 0;

        var anyConnectedBridge = false;
        foreach (var factory in allFactories)
        {
            if (factory.HasConnectedAttachment(BuildPrototypeKind.OutputPort))
            {
                anyConnectedBridge = true;
                break;
            }
        }

        if (!mixedStates || !variedProfiles || !variedPresets || !playerMoved || !workspaceNavigationVerified || !editorVisible || !playerDeployed || !backgroundMoved || !anyConnectedBridge || !playerTurretTrackedThreats || !worldCombatActive)
        {
            GD.PushError($"MOBILE_FACTORY_LARGE_SMOKE_FAILED mixedStates={mixedStates} variedProfiles={variedProfiles} variedPresets={variedPresets} playerMoved={playerMoved} workspaceNavigation={workspaceNavigationVerified} editorVisible={editorVisible} playerDeployed={playerDeployed} backgroundMoved={backgroundMoved} deliveredDuringRun={deliveredDuringRun} inputDeliveredDuringRun={inputDeliveredDuringRun} anyConnectedBridge={anyConnectedBridge} deployedCount={deployedCount} inTransitCount={inTransitCount} scenarioDelivered={GetScenarioDeliveryTotal()} mobileTurretShots={CountMobileTurretShots()} heavyEnemyCount={heavyEnemyCount} activeEnemies={_simulation.ActiveEnemyCount} defeatedEnemies={_simulation.DefeatedEnemyCount}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_LARGE_SMOKE_OK deployed={deployedCount} inTransit={inTransitCount} workspaceNavigation={workspaceNavigationVerified} delivered={GetScenarioDeliveryTotal()} heavyEnemies={heavyEnemyCount} mobileTurretShots={CountMobileTurretShots()}");
        GetTree().Quit();
    }

    private static Vector2I FirstCell(IEnumerable<Vector2I> cells)
    {
        foreach (var cell in cells)
        {
            return cell;
        }

        return Vector2I.Zero;
    }

    private async System.Threading.Tasks.Task WaitForCondition(global::System.Func<bool> predicate, float timeoutSeconds)
    {
        var remaining = timeoutSeconds;
        while (remaining > 0.0f)
        {
            if (predicate())
            {
                return;
            }

            await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
            remaining -= 0.1f;
        }
    }
}
