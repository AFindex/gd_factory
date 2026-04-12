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
        var blueprintWorkflowInTransit = await RunInteriorBlueprintSmoke();
        var multiCellInteriorVerified = RunInteriorMultiCellSmoke();
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
        var cabinPresentationVerified =
            _mobileFactory.TryGetInteriorStructure(FocusedSmelterCell, out var smelterStructure)
            && smelterStructure is SmelterStructure
            && CountNamedNodes(smelterStructure, "CabinLabelPlate") > 0
            && CountNamedNodes(smelterStructure, "SmelterCabinShell") > 0
            && _mobileFactory.TryGetInteriorStructure(FocusedIronBufferCell, out var bufferStructure)
            && bufferStructure is StorageStructure
            && CountNamedNodes(bufferStructure, "StorageCabinetShell") > 0
            && escortTurret is not null
            && CountNamedNodes(escortTurret, "Well") > 0;
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
                    && CountNamedNodes(outputAttachment, "HullMouth") > 0;
            }
            else if (attachment is MobileFactoryInputPortStructure inputAttachment)
            {
                boundaryInterfaceVerified |= CountNamedNodes(inputAttachment, "InputReceiver") > 0
                    && CountNamedNodes(inputAttachment, "HullMouth") > 0;
            }
        }
        var miniatureSyncedDeployed = placedStructure is not null
            && placedStructure.GlobalPosition.DistanceTo(_mobileFactory.InteriorSite.CellToWorld(new Vector2I(2, 0))) < 0.05f;
        PrimeFocusedOutputPorts(_mobileFactory);
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

        if (!startsInPlayerMode || !playerHudReady || !playerMoved || !cameraFollowedPlayer || !playerWorldPlacementWorked || !commandActive || !cameraLockedInCommand || !returnedToPlayerFromCommand || !observerActive || !returnedToPlayerFromObserver || !deployPreviewEntered || !returnedToPlayerFromDeploy || !editRestoresPlayerMode || !editRestoresFactoryMode || !editRestoresObserverMode || !editRestoresDeployMode || !interiorRunsInTransit || !movedInTransit || !openedInTransit || !workspaceNavigationVerified || !operationPanelHover || !editorViewportHover || !worldHover || !detailWindowInTransit || !blueprintWorkflowInTransit || !multiCellInteriorVerified || !placedInterior || !interiorPlacedExists || !placedInteriorSink || !interiorSinkExists || !placedSplitterPresentationVerified || !miniatureSyncedInTransit || !inputBlockedInTransit || !blockedDeploy || !edgeBlockedDeploy || !facingAwareCells || !mapFormatVerified || !contextualRotateWorks || !previewArrowTracksFacing || !firstDeploy || !moveRejectedWhileDeployed || !openedWhileDeployed || !portConnected || !portOverlayConnected || !boundaryInterfaceVerified || !miniatureSyncedDeployed || !turretTrackedThreats || !mobileCombatActive || !recalled || !stayedInPlaceAfterReturn || !reservationsReleased || !secondDeploy)
        {
            GD.PushError($"MOBILE_FACTORY_SMOKE_FAILED startsPlayer={startsInPlayerMode} playerHudReady={playerHudReady} playerMoved={playerMoved} cameraFollowedPlayer={cameraFollowedPlayer} playerWorldPlacementWorked={playerWorldPlacementWorked} commandActive={commandActive} cameraLocked={cameraLockedInCommand} returnedPlayerFromCommand={returnedToPlayerFromCommand} observerActive={observerActive} observerCamera={observerCameraActive} returnedPlayerFromObserver={returnedToPlayerFromObserver} deployPreviewEntered={deployPreviewEntered} returnedPlayerFromDeploy={returnedToPlayerFromDeploy} editRestoresPlayer={editRestoresPlayerMode} editRestoresFactory={editRestoresFactoryMode} editRestoresObserver={editRestoresObserverMode} editRestoresDeploy={editRestoresDeployMode} interiorTransit={interiorRunsInTransit} movedInTransit={movedInTransit} openedTransit={openedInTransit} workspaceNavigation={workspaceNavigationVerified} operationHover={operationPanelHover} viewportHover={editorViewportHover} worldHover={worldHover} detailWindow={detailWindowInTransit} blueprintWorkflow={blueprintWorkflowInTransit} multiCellInterior={multiCellInteriorVerified} placedInterior={placedInterior} interiorPlacedExists={interiorPlacedExists} placedSink={placedInteriorSink} sinkExists={interiorSinkExists} cabinPresentation={cabinPresentationVerified} splitterPresentation={placedSplitterPresentationVerified} miniatureTransit={miniatureSyncedInTransit} inputBlockedInTransit={inputBlockedInTransit} blocked={blockedDeploy} edgeBlocked={edgeBlockedDeploy} facingAware={facingAwareCells} mapFormat={mapFormatVerified} contextualRotateWorks={contextualRotateWorks} previewArrowTracksFacing={previewArrowTracksFacing} firstDeploy={firstDeploy} moveRejected={moveRejectedWhileDeployed} openedDeployed={openedWhileDeployed} portConnected={portConnected} portOverlay={portOverlayConnected} boundaryInterface={boundaryInterfaceVerified} miniatureDeployed={miniatureSyncedDeployed} firstDelivered={firstDelivered} inputAttachmentTransit={inputAttachmentTransit} inputDeliveredWhileDeployed={inputDeliveredWhileDeployed} turretShots={(escortTurret?.ShotsFired ?? -1)} mobileCombatActive={mobileCombatActive} recalled={recalled} blockedOutputActive={blockedOutputActive} stayedInPlaceAfterReturn={stayedInPlaceAfterReturn} released={reservationsReleased} secondDeploy={secondDeploy} secondDelivered={secondDelivered}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_SMOKE_OK playerMoved={playerMoved} commandActive={commandActive} observerActive={observerActive} mapFormat={mapFormatVerified} firstDelivered={firstDelivered} secondDelivered={secondDelivered} workspaceNavigation={workspaceNavigationVerified} detailWindow={detailWindowInTransit} blueprintWorkflow={blueprintWorkflowInTransit} multiCellInterior={multiCellInteriorVerified} turretShots={(escortTurret?.ShotsFired ?? -1)} combatKills={_simulation.DefeatedEnemyCount}");
        GetTree().Quit();
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
                if (!_playerInteriorPlacementArmed)
                {
                    HandlePlayerHotbarPressed(slotIndex);
                }

                if (_playerInteriorPlacementArmed && TryResolveSelectedPlayerPlaceable(out kind))
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
        return resolvesFromSecondaryCell
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
            : new[] { CommandWorkspaceId, EditorWorkspaceId, TestingWorkspaceId, BlueprintWorkspaceId, SavesWorkspaceId, DetailsWorkspaceId };
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

        return blueprintReady && buildReady && testingReady && detailsReady && savesReady && worldReady && closePreservesWorkspace && reopenPreservesWorkspace;
    }

    private static bool HasWorkspace(string workspaceId, IReadOnlyList<string> workspaceIds)
    {
        return FactoryDemoSmokeSupport.HasWorkspace(workspaceIds, workspaceId);
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
            var removedAuxOutput = _mobileFactory.RemoveInteriorStructure(new Vector2I(7, 4));
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

        var captured = FactoryBlueprintCaptureService.CaptureSelection(
            _interiorBlueprintSite,
            new Rect2I(FocusedAssemblerCell.X, FocusedAssemblerCell.Y, 3, 5),
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

        for (var y = FocusedAssemblerCell.Y; y < FocusedAssemblerCell.Y + 5; y++)
        {
            for (var x = FocusedAssemblerCell.X; x < FocusedAssemblerCell.X + 3; x++)
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
