using Godot;
using System.Collections.Generic;

public sealed class MobileFactoryInstance
{
    private readonly struct DeployTarget
    {
        public DeployTarget(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
        {
            WorldGrid = worldGrid;
            AnchorCell = anchorCell;
            Facing = facing;
        }

        public GridManager WorldGrid { get; }
        public Vector2I AnchorCell { get; }
        public FacingDirection Facing { get; }
    }

    private const int InteriorRenderLayer = 1;
    private const int HullRenderLayer = 2;
    private const float TransitMoveSpeed = 5.4f;
    private const float TransitTurnSpeed = 2.5f;
    private const float AutoDeployMoveSpeed = 6.0f;
    private const float AutoDeployTurnSpeed = 4.2f;
    private const float AutoDeployArrivalDistance = 0.08f;
    private const float AutoDeployArrivalAngle = 0.04f;
    private const float RecallDurationSeconds = 0.22f;
    private const float AutopilotArrivalDistance = 0.14f;
    private const float AutopilotArrivalAngle = 0.10f;
    private const float AttachmentConnectorStartOffset = 0.45f;
    private const float AttachmentConnectorWorldInset = 0.10f;
    private const string AttachmentIdMetaKey = "attachment_id";
    private const string AttachmentFullLengthMetaKey = "full_length";
    private const string AttachmentMouthExtensionMetaKey = "mouth_extension";
    private const string AttachmentVisualProgressMetaKey = "visual_progress";
    private const string AttachmentVisualTargetMetaKey = "visual_target";
    private const string AttachmentRemoveWhenHiddenMetaKey = "remove_when_hidden";

    private readonly SimulationController _simulation;
    private readonly Node3D _structureRoot;
    private readonly Node3D _hullRoot;
    private readonly Node3D _worldChildStructureRoot;
    private readonly Node3D _worldAttachmentVisualRoot;
    private readonly List<MobileFactoryBoundaryAttachmentStructure> _attachments = new();
    private readonly Vector3 _interiorFloorLocalOffset;
    private GridManager? _deployedGrid;
    private DeployTarget? _pendingDeployTarget;
    private Vector3 _pendingTransitPosition;
    private float _pendingTransitHeadingRadians;
    private float _currentHeadingRadians;
    private float _recallTimer;
    private string? _pendingStatusMessage;

    public MobileFactoryInstance(
        string factoryId,
        Node3D structureRoot,
        SimulationController simulation,
        MobileFactoryProfile? profile = null,
        MobileFactoryInteriorPreset? interiorPreset = null)
    {
        FactoryId = factoryId;
        Profile = profile ?? MobileFactoryScenarioLibrary.CreateFocusedDemoProfile();
        InteriorPreset = interiorPreset ?? MobileFactoryScenarioLibrary.CreateFocusedDemoPreset();
        _simulation = simulation;
        _structureRoot = structureRoot;
        ReservationOwnerId = $"mobile:{factoryId}";
        InteriorSite = new MobileFactorySite(
            $"mobile-site:{factoryId}",
            Profile.InteriorMinCell,
            Profile.InteriorMaxCell,
            Profile.InteriorCellSize,
            this);
        _interiorFloorLocalOffset = CalculateInteriorFloorLocalOffset(Profile);

        _hullRoot = CreateHullRoot(Profile, _interiorFloorLocalOffset);
        _structureRoot.AddChild(_hullRoot);
        _worldChildStructureRoot = new Node3D
        {
            Name = "MobileFactoryWorldChildStructures"
        };
        _structureRoot.AddChild(_worldChildStructureRoot);
        _worldAttachmentVisualRoot = new Node3D
        {
            Name = "MobileFactoryWorldAttachments",
            Visible = false
        };
        _structureRoot.AddChild(_worldAttachmentVisualRoot);

        ApplyInteriorPreset(InteriorPreset);
        EnsureDefaultAttachments();

        DeploymentFacing = FacingDirection.East;
        _currentHeadingRadians = FactoryDirection.ToYRotationRadians(FacingDirection.East);
        MoveToTransitParking();
        PushStatus("移动工厂待命中，使用 WASD 操作本体，按 G 进入部署模式。");
        _simulation.RebuildTopology();
    }

    public string FactoryId { get; }
    public string ReservationOwnerId { get; }
    public MobileFactoryProfile Profile { get; }
    public MobileFactoryInteriorPreset InteriorPreset { get; }
    public MobileFactorySite InteriorSite { get; }
    public MobileFactoryLifecycleState State { get; private set; } = MobileFactoryLifecycleState.InTransit;
    public Vector2I? AnchorCell { get; private set; }
    public FacingDirection DeploymentFacing { get; private set; }
    public FacingDirection TransitFacing => FactoryDirection.FromAngleRadians(_currentHeadingRadians);
    public Vector2I InteriorMinCell => InteriorSite.MinCell;
    public Vector2I InteriorMaxCell => InteriorSite.MaxCell;
    public Vector3 WorldFocusPoint => _hullRoot.GlobalPosition;
    public bool IsBusy => State == MobileFactoryLifecycleState.AutoDeploying || State == MobileFactoryLifecycleState.Recalling;
    public Vector2I? PendingDeployAnchor => _pendingDeployTarget?.AnchorCell;
    public FacingDirection? PendingDeployFacing => _pendingDeployTarget?.Facing;
    public IEnumerable<MobileFactoryBoundaryAttachmentStructure> BoundaryAttachments => _attachments;

    public IEnumerable<Vector2I> GetFootprintCells(Vector2I anchorCell)
    {
        return GetFootprintCells(anchorCell, DeploymentFacing);
    }

    public IEnumerable<Vector2I> GetFootprintCells(Vector2I anchorCell, FacingDirection facing)
    {
        foreach (var offset in Profile.FootprintOffsetsEast)
        {
            yield return anchorCell + FactoryDirection.RotateOffset(offset, facing);
        }
    }

    public IEnumerable<Vector2I> GetPortCells(Vector2I anchorCell)
    {
        return GetPortCells(anchorCell, DeploymentFacing);
    }

    public IEnumerable<Vector2I> GetPortCells(Vector2I anchorCell, FacingDirection facing)
    {
        var seen = new HashSet<Vector2I>();
        foreach (var projection in GetAttachmentProjections(anchorCell, facing))
        {
            for (var i = 0; i < projection.WorldCells.Count; i++)
            {
                if (seen.Add(projection.WorldCells[i]))
                {
                    yield return projection.WorldCells[i];
                }
            }
        }
    }

    public IEnumerable<MobileFactoryAttachmentProjection> GetAttachmentProjections(Vector2I anchorCell, FacingDirection facing)
    {
        for (var i = 0; i < _attachments.Count; i++)
        {
            var attachment = _attachments[i];
            if (!Profile.TryGetAttachmentMount(attachment.Cell, attachment.Facing, attachment.Kind, out var mount) || mount is null)
            {
                continue;
            }

            yield return MobileFactoryBoundaryAttachmentGeometry.CreateProjection(attachment, mount, anchorCell, facing);
        }
    }

    public MobileFactoryDeploymentEvaluation EvaluateDeployment(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        return EvaluateDeployment(worldGrid, anchorCell, facing, allowWhenAlreadyDeployed: false);
    }

    private MobileFactoryDeploymentEvaluation EvaluateDeployment(
        GridManager worldGrid,
        Vector2I anchorCell,
        FacingDirection facing,
        bool allowWhenAlreadyDeployed)
    {
        var footprintCells = new List<Vector2I>(GetFootprintCells(anchorCell, facing));
        if (!allowWhenAlreadyDeployed
            && (State == MobileFactoryLifecycleState.Deployed || State == MobileFactoryLifecycleState.Recalling))
        {
            return new MobileFactoryDeploymentEvaluation(
                MobileFactoryDeployState.Blocked,
                footprintCells,
                new List<MobileFactoryAttachmentDeploymentEvaluation>(),
                "移动工厂当前必须先解除部署，才能重新选择落点。");
        }

        if (!worldGrid.CanReserveAll(footprintCells, ReservationOwnerId))
        {
            return new MobileFactoryDeploymentEvaluation(
                MobileFactoryDeployState.Blocked,
                footprintCells,
                new List<MobileFactoryAttachmentDeploymentEvaluation>(),
                "移动工厂主体占地越界或与现有占用冲突。");
        }

        var attachmentEvaluations = new List<MobileFactoryAttachmentDeploymentEvaluation>(_attachments.Count);
        var state = MobileFactoryDeployState.Valid;
        var reason = string.Empty;
        foreach (var projection in GetAttachmentProjections(anchorCell, facing))
        {
            var evaluation = projection.Attachment.EvaluateDeployment(worldGrid, projection);
            var activationReservedCells = evaluation.Attachment.GetActivationReservedWorldCells(evaluation);
            if (evaluation.CanDeploy && !worldGrid.CanReserveAll(activationReservedCells, ReservationOwnerId))
            {
                evaluation = new MobileFactoryAttachmentDeploymentEvaluation(
                    projection.Attachment,
                    projection,
                    MobileFactoryAttachmentDeployState.Blocked,
                    evaluation.PreviewWorldCells,
                    new List<Vector2I>(),
                    evaluation.ActiveWorldCells,
                    "边界 attachment 的世界侧投影越界或与现有占用冲突。");
            }

            attachmentEvaluations.Add(evaluation);
            if (evaluation.State == MobileFactoryAttachmentDeployState.Blocked)
            {
                state = MobileFactoryDeployState.Blocked;
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = evaluation.Reason;
                }
            }
            else if (evaluation.State == MobileFactoryAttachmentDeployState.Optional && state != MobileFactoryDeployState.Blocked)
            {
                state = MobileFactoryDeployState.Warning;
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = evaluation.Reason;
                }
            }
        }

        return new MobileFactoryDeploymentEvaluation(state, footprintCells, attachmentEvaluations, reason);
    }

    public bool CanDeployAt(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        return EvaluateDeployment(worldGrid, anchorCell, facing).CanDeploy;
    }

    public bool CanDeployAt(GridManager worldGrid, Vector2I anchorCell)
    {
        return CanDeployAt(worldGrid, anchorCell, DeploymentFacing);
    }

    public void ApplyTransitInput(GridManager? worldGrid, float throttle, float turn, double delta)
    {
        if (State != MobileFactoryLifecycleState.InTransit)
        {
            return;
        }

        if (Mathf.Abs(turn) > 0.01f)
        {
            _currentHeadingRadians = NormalizeAngle(_currentHeadingRadians + turn * TransitTurnSpeed * (float)delta);
        }

        var hullCenter = _hullRoot.Position;
        if (Mathf.Abs(throttle) > 0.01f)
        {
            var forward = FactoryDirection.ToWorldForward(_currentHeadingRadians);
            hullCenter += forward * (throttle * TransitMoveSpeed * (float)delta);
            hullCenter = ClampHullCenter(worldGrid, hullCenter, GetHullRadiusPadding());
        }

        ApplyHullTransform(hullCenter, _currentHeadingRadians);
    }

    public bool UpdateTransitAutopilot(GridManager? worldGrid, Vector3 targetPosition, FacingDirection targetFacing, double delta)
    {
        if (State != MobileFactoryLifecycleState.InTransit)
        {
            return false;
        }

        var hullCenter = _hullRoot.Position;
        var toTarget = targetPosition - hullCenter;
        var planarDelta = new Vector2(toTarget.X, toTarget.Z);
        var planarDistance = planarDelta.Length();
        var desiredHeading = planarDistance > 0.01f
            ? Mathf.Atan2(-toTarget.Z, toTarget.X)
            : FactoryDirection.ToYRotationRadians(targetFacing);

        _currentHeadingRadians = MoveAngleTowards(_currentHeadingRadians, desiredHeading, TransitTurnSpeed * (float)delta);

        if (planarDistance > AutopilotArrivalDistance)
        {
            var moveDistance = Mathf.Min(TransitMoveSpeed * 0.8f * (float)delta, planarDistance);
            var moveDirection = new Vector3(toTarget.X / planarDistance, 0.0f, toTarget.Z / planarDistance);
            hullCenter += moveDirection * moveDistance;
            hullCenter = ClampHullCenter(worldGrid, hullCenter, GetHullRadiusPadding());
        }
        else
        {
            _currentHeadingRadians = MoveAngleTowards(
                _currentHeadingRadians,
                FactoryDirection.ToYRotationRadians(targetFacing),
                TransitTurnSpeed * (float)delta);
        }

        ApplyHullTransform(hullCenter, _currentHeadingRadians);

        return hullCenter.DistanceTo(targetPosition) <= AutopilotArrivalDistance
            && Mathf.Abs(NormalizeAngle(FactoryDirection.ToYRotationRadians(targetFacing) - _currentHeadingRadians)) <= AutopilotArrivalAngle;
    }

    public void SetTransitPose(Vector3 worldPosition, FacingDirection facing)
    {
        ReleaseDeploymentReservations();
        ClearAttachmentBindings();

        _pendingDeployTarget = null;
        AnchorCell = null;
        State = MobileFactoryLifecycleState.InTransit;
        DeploymentFacing = facing;
        _currentHeadingRadians = FactoryDirection.ToYRotationRadians(facing);
        ApplyHullTransform(worldPosition, _currentHeadingRadians);
        InteriorSite.SetRuntimeState(true, true);
        _simulation.RebuildTopology();
    }

    public bool TryStartAutoDeploy(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        if (!CanDeployAt(worldGrid, anchorCell, facing))
        {
            return false;
        }

        _pendingDeployTarget = new DeployTarget(worldGrid, anchorCell, facing);
        State = MobileFactoryLifecycleState.AutoDeploying;
        PushStatus($"部署命令已确认，正在朝 ({anchorCell.X}, {anchorCell.Y}) 行进；抵达后会转向 {FactoryDirection.ToLabel(facing)} 并展开。");
        return true;
    }

    public bool CancelAutoDeploy()
    {
        if (State != MobileFactoryLifecycleState.AutoDeploying)
        {
            return false;
        }

        _pendingDeployTarget = null;
        State = MobileFactoryLifecycleState.InTransit;
        PushStatus("已取消自动部署，返回工厂控制模式。");
        return true;
    }

    public bool TryDeploy(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        var evaluation = EvaluateDeployment(worldGrid, anchorCell, facing);
        if (!evaluation.CanDeploy)
        {
            return false;
        }

        FinalizeDeployment(worldGrid, anchorCell, facing, evaluation);
        PushStatus(evaluation.HasWarnings
            ? $"已部署到 ({anchorCell.X}, {anchorCell.Y})，朝向 {FactoryDirection.ToLabel(facing)}；采矿输入端口未接入矿区，将保持待机。"
            : $"已部署到 ({anchorCell.X}, {anchorCell.Y})，朝向 {FactoryDirection.ToLabel(facing)}。");
        return true;
    }

    public bool TryDeploy(GridManager worldGrid, Vector2I anchorCell)
    {
        return TryDeploy(worldGrid, anchorCell, DeploymentFacing);
    }

    public bool ReturnToTransitMode()
    {
        if (State != MobileFactoryLifecycleState.Deployed || _deployedGrid is null)
        {
            return false;
        }

        var recallDuration = GetActiveAttachmentRetractionDurationSeconds();
        _pendingTransitPosition = _hullRoot.Position;
        _pendingTransitHeadingRadians = _currentHeadingRadians;
        ReleaseDeploymentReservations();
        DisconnectAttachmentBindings();
        BeginAttachmentRetraction();
        AnchorCell = null;
        InteriorSite.SetRuntimeState(true, true);
        State = MobileFactoryLifecycleState.Recalling;
        _recallTimer = recallDuration;
        PushStatus("移动工厂正在收拢边界 attachment，准备切回移动态；未激活边界会阻塞等待重新部署。");
        _simulation.RebuildTopology();
        return true;
    }

    public bool Recall()
    {
        return ReturnToTransitMode();
    }

    public void SetCombatOverlayScale(float combatOverlayScale)
    {
        InteriorSite.SetCombatOverlayScale(combatOverlayScale);
    }

    public void UpdateRuntime(double delta)
    {
        UpdateAttachmentVisualAnimation((float)delta);

        switch (State)
        {
            case MobileFactoryLifecycleState.AutoDeploying:
                UpdateAutoDeploy((float)delta);
                break;
            case MobileFactoryLifecycleState.Recalling:
                UpdateRecall((float)delta);
                break;
        }
    }

    public string? ConsumeStatusMessage()
    {
        var message = _pendingStatusMessage;
        _pendingStatusMessage = null;
        return message;
    }

    public bool CanPlaceInterior(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(kind))
        {
            return CanPlaceAttachment(kind, cell, facing);
        }

        return InteriorSite.CanPlace(cell);
    }

    public bool TryGetInteriorStructure(Vector2I cell, out FactoryStructure? structure)
    {
        return InteriorSite.TryGetStructure(cell, out structure);
    }

    public bool IsAttachmentCell(Vector2I cell)
    {
        return InteriorSite.TryGetStructure(cell, out var structure) && structure is MobileFactoryBoundaryAttachmentStructure;
    }

    public bool TryGetAttachmentPreview(
        BuildPrototypeKind kind,
        Vector2I cell,
        FacingDirection facing,
        out List<Vector2I> interiorCells,
        out List<Vector2I> boundaryCells,
        out List<Vector2I> exteriorCells,
        out string message)
    {
        interiorCells = new List<Vector2I>();
        boundaryCells = new List<Vector2I>();
        exteriorCells = new List<Vector2I>();

        if (!MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(kind))
        {
            message = "当前选中的不是边界 attachment。";
            return false;
        }

        var definition = MobileFactoryBoundaryAttachmentCatalog.Get(kind);
        interiorCells = MobileFactoryBoundaryAttachmentGeometry.GetInteriorCells(definition, cell, facing);
        boundaryCells = MobileFactoryBoundaryAttachmentGeometry.GetBoundaryCells(definition, cell, facing);
        exteriorCells = MobileFactoryBoundaryAttachmentGeometry.GetExteriorStencilCells(definition, cell, facing);

        if (!Profile.TryGetAttachmentMount(cell, facing, kind, out var mount) || mount is null)
        {
            message = $"格 ({cell.X}, {cell.Y}) 不是该 attachment 的合法边界挂点，或当前朝向与挂点不匹配。";
            return false;
        }

        if (!CanPlaceAttachment(kind, cell, facing))
        {
            message = $"格 ({cell.X}, {cell.Y}) 的边界挂点已被占用，或当前部署状态下无法激活该 attachment。";
            return false;
        }

        message = $"{MobileFactoryBoundaryAttachmentCatalog.Get(kind).DisplayName} 可挂接在 {mount.Id}，其外侧投影会随部署朝向旋转。";
        return true;
    }

    public bool PlaceInteriorStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (!CanPlaceInterior(kind, cell, facing))
        {
            return false;
        }

        var structure = FactoryStructureFactory.Create(kind, new FactoryStructurePlacement(InteriorSite, cell, facing));
        RegisterInteriorStructure(structure);
        if (structure is MobileFactoryBoundaryAttachmentStructure attachment)
        {
            _attachments.Add(attachment);
        }

        RefreshRuntimeAfterInteriorChange();
        return true;
    }

    public bool RemoveInteriorStructure(Vector2I cell)
    {
        if (!InteriorSite.TryGetStructure(cell, out var structure) || structure is null)
        {
            return false;
        }

        InteriorSite.RemoveStructure(structure);
        _simulation.UnregisterStructure(structure);
        DetachAttachmentIfNeeded(structure);
        structure.QueueFree();
        RefreshRuntimeAfterInteriorChange();
        return true;
    }

    public void HandleDestroyedInteriorStructure(FactoryStructure structure, bool rebuildTopology = true)
    {
        DetachAttachmentIfNeeded(structure);
        RefreshRuntimeAfterInteriorChange(rebuildTopology);
    }

    public Vector3 GetEditorFocusWorldCenter()
    {
        var minWorld = InteriorSite.CellToWorld(InteriorMinCell);
        var maxWorld = InteriorSite.CellToWorld(InteriorMaxCell);
        return new Vector3(
            (minWorld.X + maxWorld.X) * 0.5f,
            InteriorSite.WorldOrigin.Y,
            (minWorld.Z + maxWorld.Z) * 0.5f);
    }

    public float GetSuggestedEditorCameraSize()
    {
        var maxDimension = Mathf.Max(Profile.InteriorWidth, Profile.InteriorHeight) * Profile.InteriorCellSize;
        return Mathf.Max(3.9f, maxDimension * 0.95f);
    }

    public string GetPortStatusLabel()
    {
        if (_attachments.Count == 0)
        {
            return "边界 attachment：当前未安装。";
        }

        var lines = new List<string>(_attachments.Count);
        for (var i = 0; i < _attachments.Count; i++)
        {
            var attachment = _attachments[i];
            var stateLabel = attachment.ConnectionStateLabel;
            var displayLabel = attachment.AttachmentDefinition.DisplayName;

            if (attachment.IsConnectedToWorld)
            {
                var portCell = attachment.WorldPortCell;
                var footprintSuffix = attachment.Projection is { } projection
                    && attachment.BoundWorldSite is GridManager boundWorld
                    && attachment.GetReservedWorldCells(boundWorld, projection).Count > 1
                    ? $"，占地 {attachment.GetReservedWorldCells(boundWorld, projection).Count} 格"
                    : string.Empty;
                var miningSuffix = attachment is MobileFactoryMiningInputPortStructure miningPort
                    ? $"，采矿桩 {miningPort.DeployedStakeCount}/{Mathf.Max(miningPort.EligibleStakeCount, 1)}"
                    : string.Empty;
                lines.Add($"{displayLabel} {i + 1}：朝{FactoryDirection.ToLabel(attachment.WorldFacing)}，{stateLabel} ({portCell.X}, {portCell.Y}){footprintSuffix}{miningSuffix}");
            }
            else if (State == MobileFactoryLifecycleState.Deployed && attachment.DeploymentProjection is { } deployedProjection)
            {
                var miningSuffix = attachment is MobileFactoryMiningInputPortStructure miningPort
                    ? $"，采矿桩 {miningPort.DeployedStakeCount}/{Mathf.Max(miningPort.EligibleStakeCount, 1)}，库存 {miningPort.BuiltStakeCount}/{miningPort.MaxStakeCapacity}"
                    : string.Empty;
                lines.Add($"{displayLabel} {i + 1}：目标朝{FactoryDirection.ToLabel(deployedProjection.WorldFacing)}，{stateLabel} ({deployedProjection.WorldPortCell.X}, {deployedProjection.WorldPortCell.Y}){miningSuffix}");
            }
            else if (State == MobileFactoryLifecycleState.AutoDeploying && AnchorCell is null && _pendingDeployTarget is DeployTarget target)
            {
                var projection = TryGetAttachmentProjection(attachment, target.AnchorCell, target.Facing);
                if (projection is not null)
                {
                    var evaluation = attachment.EvaluateDeployment(target.WorldGrid, projection);
                    var pendingState = evaluation.State switch
                    {
                        MobileFactoryAttachmentDeployState.Connected => "准备连接",
                        MobileFactoryAttachmentDeployState.Optional => "部署后待机",
                        _ => "目标无效"
                    };
                    lines.Add($"{displayLabel} {i + 1}：目标朝{FactoryDirection.ToLabel(projection.WorldFacing)}，{pendingState} ({projection.WorldPortCell.X}, {projection.WorldPortCell.Y})");
                }
            }
            else
            {
                lines.Add($"{displayLabel} {i + 1}：朝{FactoryDirection.ToLabel(attachment.Facing)}，当前{stateLabel}");
            }
        }

        return string.Join("\n", lines);
    }

    public bool HasConnectedAttachment(BuildPrototypeKind? kind = null)
    {
        for (var i = 0; i < _attachments.Count; i++)
        {
            if ((kind is null || _attachments[i].Kind == kind.Value) && _attachments[i].IsConnectedToWorld)
            {
                return true;
            }
        }

        return false;
    }

    public int CountAttachmentTransitItems(BuildPrototypeKind kind, bool onlyDisconnected = false)
    {
        var total = 0;
        for (var i = 0; i < _attachments.Count; i++)
        {
            if (_attachments[i].Kind != kind)
            {
                continue;
            }

            if (onlyDisconnected && _attachments[i].IsConnectedToWorld)
            {
                continue;
            }

            total += _attachments[i].TransitItemCount;
        }

        return total;
    }

    private void ApplyInteriorPreset(MobileFactoryInteriorPreset preset)
    {
        foreach (var placement in preset.Placements)
        {
            if (!InteriorSite.CanPlace(placement.Cell))
            {
                continue;
            }

            RegisterInteriorStructure(FactoryStructureFactory.Create(
                placement.Kind,
                new FactoryStructurePlacement(InteriorSite, placement.Cell, placement.Facing)));
        }

        foreach (var attachmentPlacement in preset.AttachmentPlacements)
        {
            if (CanPlaceAttachment(attachmentPlacement.Kind, attachmentPlacement.Cell, attachmentPlacement.Facing))
            {
                var structure = FactoryStructureFactory.Create(
                    attachmentPlacement.Kind,
                    new FactoryStructurePlacement(InteriorSite, attachmentPlacement.Cell, attachmentPlacement.Facing));
                RegisterInteriorStructure(structure);
                if (structure is MobileFactoryBoundaryAttachmentStructure attachment)
                {
                    _attachments.Add(attachment);
                }
            }
        }
    }

    private void EnsureDefaultAttachments()
    {
        if (_attachments.Count > 0)
        {
            return;
        }

        for (var i = 0; i < Profile.AttachmentMounts.Count; i++)
        {
            var mount = Profile.AttachmentMounts[i];
            if (!mount.Allows(BuildPrototypeKind.OutputPort) || !InteriorSite.CanPlace(mount.Cell))
            {
                continue;
            }

            var structure = FactoryStructureFactory.Create(
                BuildPrototypeKind.OutputPort,
                new FactoryStructurePlacement(InteriorSite, mount.Cell, mount.Facing));
            RegisterInteriorStructure(structure);
            if (structure is MobileFactoryBoundaryAttachmentStructure attachment)
            {
                _attachments.Add(attachment);
            }
            break;
        }
    }

    private void UpdateAutoDeploy(float delta)
    {
        if (_pendingDeployTarget is not DeployTarget target)
        {
            State = MobileFactoryLifecycleState.InTransit;
            return;
        }

        var targetCenter = GetFootprintCenterWorld(target.WorldGrid, target.AnchorCell, target.Facing);
        var deployHeading = FactoryDirection.ToYRotationRadians(target.Facing);
        var currentCenter = _hullRoot.Position;
        var toTarget = targetCenter - currentCenter;
        var planarDistance = new Vector2(toTarget.X, toTarget.Z).Length();
        var isApproachingTarget = planarDistance > AutoDeployArrivalDistance;
        var desiredHeading = deployHeading;

        if (isApproachingTarget)
        {
            var maxMove = AutoDeployMoveSpeed * delta;
            var move = Mathf.Min(maxMove, planarDistance);
            var moveDir = new Vector3(toTarget.X / planarDistance, 0.0f, toTarget.Z / planarDistance);
            desiredHeading = Mathf.Atan2(-moveDir.Z, moveDir.X);
            currentCenter += moveDir * move;
            currentCenter = ClampHullCenter(target.WorldGrid, currentCenter, GetHullRadiusPadding());
        }
        else
        {
            currentCenter = targetCenter;
        }

        _currentHeadingRadians = MoveAngleTowards(_currentHeadingRadians, desiredHeading, AutoDeployTurnSpeed * delta);
        ApplyHullTransform(currentCenter, _currentHeadingRadians);

        if (currentCenter.DistanceTo(targetCenter) > AutoDeployArrivalDistance || Mathf.Abs(NormalizeAngle(deployHeading - _currentHeadingRadians)) > AutoDeployArrivalAngle)
        {
            return;
        }

        var evaluation = EvaluateDeployment(target.WorldGrid, target.AnchorCell, target.Facing);
        if (!evaluation.CanDeploy)
        {
            _pendingDeployTarget = null;
            State = MobileFactoryLifecycleState.InTransit;
            PushStatus("自动部署在最终校验时失败，目标区域已无效。");
            return;
        }

        FinalizeDeployment(target.WorldGrid, target.AnchorCell, target.Facing, evaluation);
        _pendingDeployTarget = null;
        PushStatus(evaluation.HasWarnings
            ? $"自动部署完成，已在 ({target.AnchorCell.X}, {target.AnchorCell.Y}) 朝 {FactoryDirection.ToLabel(target.Facing)} 展开；采矿输入端口当前未接入矿区。"
            : $"自动部署完成，已在 ({target.AnchorCell.X}, {target.AnchorCell.Y}) 朝 {FactoryDirection.ToLabel(target.Facing)} 展开。");
    }

    private void UpdateRecall(float delta)
    {
        _recallTimer -= delta;
        if (_recallTimer > 0.0f)
        {
            return;
        }

        ApplyHullTransform(_pendingTransitPosition, _pendingTransitHeadingRadians);
        InteriorSite.SetRuntimeState(true, true);
        State = MobileFactoryLifecycleState.InTransit;
        PushStatus("移动工厂已切回移动态，可继续机动或重新部署；未连接边界 attachment 会如实阻塞等待重连。");
    }

    private void FinalizeDeployment(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing, MobileFactoryDeploymentEvaluation evaluation)
    {
        _deployedGrid = worldGrid;
        AnchorCell = anchorCell;
        DeploymentFacing = facing;
        State = MobileFactoryLifecycleState.Deployed;

        worldGrid.ReserveCells(GetFootprintCells(anchorCell, facing), ReservationOwnerId, GridReservationKind.MobileFootprint);

        var footprintCenter = GetFootprintCenterWorld(worldGrid, anchorCell, facing);
        _currentHeadingRadians = FactoryDirection.ToYRotationRadians(facing);
        ApplyHullTransform(footprintCenter, _currentHeadingRadians);
        ActivateAttachmentsForDeployment(worldGrid, evaluation);
        InteriorSite.SetRuntimeState(true, true);
        _simulation.RebuildTopology();
    }

    private void ActivateAttachmentsForDeployment(GridManager worldGrid, MobileFactoryDeploymentEvaluation evaluation)
    {
        var activeEvaluations = new Dictionary<ulong, MobileFactoryAttachmentDeploymentEvaluation>();
        for (var index = 0; index < evaluation.AttachmentEvaluations.Count; index++)
        {
            var attachmentEvaluation = evaluation.AttachmentEvaluations[index];
            if (attachmentEvaluation.CanDeploy)
            {
                activeEvaluations[attachmentEvaluation.Attachment.GetInstanceId()] = attachmentEvaluation;
            }
        }

        for (var index = 0; index < _attachments.Count; index++)
        {
            var attachment = _attachments[index];
            if (!activeEvaluations.ContainsKey(attachment.GetInstanceId()))
            {
                ClearSingleAttachmentBinding(attachment, clearDeploymentContext: true);
            }
        }

        for (var index = 0; index < evaluation.AttachmentEvaluations.Count; index++)
        {
            var attachmentEvaluation = evaluation.AttachmentEvaluations[index];
            if (!attachmentEvaluation.CanDeploy)
            {
                continue;
            }

            attachmentEvaluation.Attachment.RecordDeploymentContext(worldGrid, attachmentEvaluation.Projection);
            var activationReservedCells = attachmentEvaluation.Attachment.GetActivationReservedWorldCells(attachmentEvaluation);
            if (activationReservedCells.Count > 0)
            {
                worldGrid.ReserveCells(activationReservedCells, ReservationOwnerId, GridReservationKind.MobilePort, attachmentEvaluation.Attachment);
            }

            var shouldBindToWorld = attachmentEvaluation.ActiveWorldCells.Count > 0 || attachmentEvaluation.State == MobileFactoryAttachmentDeployState.Connected;
            if (shouldBindToWorld)
            {
                attachmentEvaluation.Attachment.BindToWorld(worldGrid, attachmentEvaluation.Projection);
            }
            else
            {
                attachmentEvaluation.Attachment.ClearBinding();
            }

            attachmentEvaluation.Attachment.OnDeploymentActivated(
                _worldChildStructureRoot,
                _simulation,
                worldGrid,
                attachmentEvaluation);
        }

        RebuildWorldAttachmentVisuals(worldGrid, AnchorCell ?? Vector2I.Zero, DeploymentFacing);
    }

    private void RegisterInteriorStructure(FactoryStructure structure)
    {
        _structureRoot.AddChild(structure);
        InteriorSite.AddStructure(structure);
        _simulation.RegisterStructure(structure);
    }

    private void RefreshRuntimeAfterInteriorChange(bool rebuildTopology = true)
    {
        if (State == MobileFactoryLifecycleState.Deployed && _deployedGrid is not null && AnchorCell is Vector2I anchorCell)
        {
            var deployedGrid = _deployedGrid;
            ReleaseDeploymentReservations();
            _deployedGrid = deployedGrid;
            deployedGrid.ReserveCells(GetFootprintCells(anchorCell, DeploymentFacing), ReservationOwnerId, GridReservationKind.MobileFootprint);
            ActivateAttachmentsForDeployment(
                deployedGrid,
                EvaluateDeployment(deployedGrid, anchorCell, DeploymentFacing, allowWhenAlreadyDeployed: true));
        }
        else
        {
            DisconnectAttachmentBindings();
            ClearAttachmentBindings();
        }

        if (rebuildTopology)
        {
            _simulation.RebuildTopology();
        }
    }

    private void DetachAttachmentIfNeeded(FactoryStructure structure)
    {
        if (structure is not MobileFactoryBoundaryAttachmentStructure attachment)
        {
            return;
        }

        _attachments.Remove(attachment);
        attachment.OnDeploymentCleared(_worldChildStructureRoot, _simulation);
        attachment.ClearBinding();
        attachment.ClearDeploymentContext();
    }

    private void MoveToTransitParking()
    {
        ApplyHullTransform(Profile.TransitParkingCenter, _currentHeadingRadians);
        _worldAttachmentVisualRoot.Visible = false;
        InteriorSite.SetRuntimeState(true, true);
    }

    private void ApplyHullTransform(Vector3 hullCenter, float headingRadians)
    {
        _hullRoot.Visible = true;
        _hullRoot.Position = hullCenter;
        _hullRoot.Rotation = new Vector3(0.0f, headingRadians, 0.0f);
        var rotatedFloorOffset = _interiorFloorLocalOffset.Rotated(Vector3.Up, headingRadians);
        InteriorSite.SetWorldTransform(hullCenter + rotatedFloorOffset, headingRadians);
    }

    private Vector3 GetFootprintCenterWorld(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        var sum = Vector3.Zero;
        var count = 0;

        foreach (var cell in GetFootprintCells(anchorCell, facing))
        {
            sum += worldGrid.CellToWorld(cell);
            count++;
        }

        return count > 0 ? sum / count : worldGrid.CellToWorld(anchorCell);
    }

    private static Vector3 CalculateInteriorFloorLocalOffset(MobileFactoryProfile profile)
    {
        var centerX = (profile.InteriorMinCell.X + profile.InteriorMaxCell.X) * 0.5f * profile.InteriorCellSize;
        var centerZ = (profile.InteriorMinCell.Y + profile.InteriorMaxCell.Y) * 0.5f * profile.InteriorCellSize;
        return new Vector3(-centerX, profile.InteriorFloorHeight, -centerZ);
    }

    private static Node3D CreateHullRoot(MobileFactoryProfile profile, Vector3 interiorFloorLocalOffset)
    {
        var root = new Node3D
        {
            Name = "MobileFactoryHull",
            Visible = true
        };

        var platformSize = GetInteriorPlatformSize(profile);
        root.AddChild(CreateHullMesh(
            "Platform",
            platformSize,
            profile.HullColor,
            new Vector3(0.0f, 0.18f, 0.0f),
            visibleInInterior: true,
            visibleInWorld: true));
        root.AddChild(CreateInteriorGridLines(profile, interiorFloorLocalOffset));

        for (var i = 0; i < profile.AttachmentMounts.Count; i++)
        {
            var mount = profile.AttachmentMounts[i];
            root.AddChild(CreateAttachmentMountMarker(profile, interiorFloorLocalOffset, mount));
        }

        return root;
    }

    private static MeshInstance3D CreateHullMesh(string name, Vector3 size, Color color, Vector3 localPosition, bool visibleInInterior, bool visibleInWorld)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = localPosition,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.85f
            }
        };

        mesh.SetLayerMaskValue(InteriorRenderLayer, visibleInInterior);
        mesh.SetLayerMaskValue(HullRenderLayer, visibleInWorld);
        return mesh;
    }

    private static Node3D CreateInteriorGridLines(MobileFactoryProfile profile, Vector3 interiorFloorLocalOffset)
    {
        var gridRoot = new Node3D
        {
            Name = "InteriorGridLines"
        };
        var lineThickness = Mathf.Max(0.018f, profile.InteriorCellSize * 0.035f);
        var lineHeight = 0.014f;
        var lineY = profile.InteriorFloorHeight + (lineHeight * 0.5f) + 0.006f;
        var minX = interiorFloorLocalOffset.X + ((profile.InteriorMinCell.X - 0.5f) * profile.InteriorCellSize);
        var maxX = interiorFloorLocalOffset.X + ((profile.InteriorMaxCell.X + 0.5f) * profile.InteriorCellSize);
        var minZ = interiorFloorLocalOffset.Z + ((profile.InteriorMinCell.Y - 0.5f) * profile.InteriorCellSize);
        var maxZ = interiorFloorLocalOffset.Z + ((profile.InteriorMaxCell.Y + 0.5f) * profile.InteriorCellSize);
        var lineLengthX = maxX - minX;
        var lineLengthZ = maxZ - minZ;
        var lineMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.35f, 0.43f, 0.53f, 0.72f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 1.0f
        };

        for (var x = profile.InteriorMinCell.X; x <= profile.InteriorMaxCell.X + 1; x++)
        {
            var vertical = new MeshInstance3D
            {
                Name = $"InteriorGridV_{x}",
                Mesh = new BoxMesh { Size = new Vector3(lineThickness, lineHeight, lineLengthZ) },
                Position = new Vector3(interiorFloorLocalOffset.X + ((x - 0.5f) * profile.InteriorCellSize), lineY, (minZ + maxZ) * 0.5f),
                MaterialOverride = lineMaterial
            };
            vertical.SetLayerMaskValue(InteriorRenderLayer, true);
            vertical.SetLayerMaskValue(HullRenderLayer, false);
            gridRoot.AddChild(vertical);
        }

        for (var y = profile.InteriorMinCell.Y; y <= profile.InteriorMaxCell.Y + 1; y++)
        {
            var horizontal = new MeshInstance3D
            {
                Name = $"InteriorGridH_{y}",
                Mesh = new BoxMesh { Size = new Vector3(lineLengthX, lineHeight, lineThickness) },
                Position = new Vector3((minX + maxX) * 0.5f, lineY, interiorFloorLocalOffset.Z + ((y - 0.5f) * profile.InteriorCellSize)),
                MaterialOverride = lineMaterial
            };
            horizontal.SetLayerMaskValue(InteriorRenderLayer, true);
            horizontal.SetLayerMaskValue(HullRenderLayer, false);
            gridRoot.AddChild(horizontal);
        }

        return gridRoot;
    }

    private static MeshInstance3D CreateAttachmentMountMarker(MobileFactoryProfile profile, Vector3 interiorFloorLocalOffset, MobileFactoryAttachmentMount mount)
    {
        var localPosition = new Vector3(
            interiorFloorLocalOffset.X + mount.Cell.X * profile.InteriorCellSize,
            0.40f,
            interiorFloorLocalOffset.Z + mount.Cell.Y * profile.InteriorCellSize);

        var marker = new MeshInstance3D
        {
            Name = $"AttachmentMount_{mount.Id}",
            Mesh = new BoxMesh { Size = new Vector3(profile.InteriorCellSize * 0.32f, 0.05f, profile.InteriorCellSize * 0.32f) },
            Position = localPosition,
            Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(mount.Facing), 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = profile.PortColor.Lightened(0.08f),
                Roughness = 0.70f
            }
        };

        marker.SetLayerMaskValue(InteriorRenderLayer, true);
        marker.SetLayerMaskValue(HullRenderLayer, false);
        return marker;
    }

    private void RebuildWorldAttachmentVisuals(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        var existingVisuals = new Dictionary<ulong, Node3D>();
        foreach (var child in _worldAttachmentVisualRoot.GetChildren())
        {
            if (child is Node3D connectorRoot && connectorRoot.HasMeta(AttachmentIdMetaKey))
            {
                existingVisuals[connectorRoot.GetMeta(AttachmentIdMetaKey).AsUInt64()] = connectorRoot;
            }
        }

        foreach (var projection in GetAttachmentProjections(anchorCell, facing))
        {
            if (!projection.Attachment.IsConnectedToWorld)
            {
                continue;
            }

            var attachmentId = projection.Attachment.GetInstanceId();
            if (existingVisuals.Remove(attachmentId, out var connectorRoot))
            {
                ConfigureWorldAttachmentVisual(connectorRoot, worldGrid, projection);
                connectorRoot.SetMeta(AttachmentVisualTargetMetaKey, 1.0f);
                connectorRoot.SetMeta(AttachmentRemoveWhenHiddenMetaKey, false);
            }
            else
            {
                var newConnectorRoot = CreateWorldAttachmentVisual(worldGrid, projection, 0.0f);
                _worldAttachmentVisualRoot.AddChild(newConnectorRoot);
                ConfigureWorldAttachmentVisual(newConnectorRoot, worldGrid, projection);
                ApplySingleAttachmentVisualProgress(newConnectorRoot, EaseAttachmentVisual(0.0f));
            }
        }

        foreach (var staleVisual in existingVisuals.Values)
        {
            staleVisual.SetMeta(AttachmentVisualTargetMetaKey, 0.0f);
            staleVisual.SetMeta(AttachmentRemoveWhenHiddenMetaKey, true);
        }

        _worldAttachmentVisualRoot.Visible = _worldAttachmentVisualRoot.GetChildCount() > 0;
        ApplyAttachmentVisualProgress();
    }

    private Node3D CreateWorldAttachmentVisual(GridManager worldGrid, MobileFactoryAttachmentProjection projection, float initialProgress)
    {
        var root = new Node3D
        {
            Name = GetWorldAttachmentVisualName(projection.Attachment)
        };
        root.SetMeta(AttachmentIdMetaKey, projection.Attachment.GetInstanceId());
        root.SetMeta(AttachmentVisualProgressMetaKey, initialProgress);
        root.SetMeta(AttachmentVisualTargetMetaKey, 1.0f);
        root.SetMeta(AttachmentRemoveWhenHiddenMetaKey, false);

        var connector = new MeshInstance3D
        {
            Name = "ConnectorStem",
            Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.20f, 0.001f) },
            Position = new Vector3(0.0f, 0.18f, 0.0005f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = projection.Attachment.AttachmentDefinition.ConnectorColor,
                Roughness = 0.70f
            }
        };
        root.AddChild(connector);

        var endpoint = new MeshInstance3D
        {
            Name = "ConnectorEndpoint",
            Mesh = new BoxMesh { Size = new Vector3(0.46f, 0.20f, 0.62f) },
            Position = new Vector3(0.0f, 0.12f, 0.0f),
            Rotation = Vector3.Zero,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = projection.Attachment.AttachmentDefinition.Tint,
                Roughness = 0.68f
            }
        };
        root.AddChild(endpoint);

        var mouth = new MeshInstance3D
        {
            Name = "ConnectorMouth",
            Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.14f, 0.42f) },
            Position = new Vector3(0.0f, 0.22f, 0.0f),
            Rotation = Vector3.Zero,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = projection.Attachment.AttachmentDefinition.ConnectorColor.Lightened(0.12f),
                Roughness = 0.55f
            }
        };
        root.AddChild(mouth);

        projection.Attachment.BuildWorldPayload(root, worldGrid, projection);

        return root;
    }

    private static string GetWorldAttachmentVisualName(MobileFactoryBoundaryAttachmentStructure attachment)
    {
        return $"Attachment_{attachment.GetInstanceId()}_WorldConnector";
    }

    private static void ConfigureWorldAttachmentVisual(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        var start = GetAttachmentConnectorStartWorld(projection.Attachment);
        var end = projection.Attachment.GetWorldConnectorEndWorld(worldGrid, projection);
        var connectorVector = end - start;
        var connectorLength = new Vector2(connectorVector.X, connectorVector.Z).Length();
        var connectorYaw = Mathf.Atan2(connectorVector.X, connectorVector.Z);

        root.Name = GetWorldAttachmentVisualName(projection.Attachment);
        root.Position = start;
        root.Rotation = new Vector3(0.0f, connectorYaw, 0.0f);
        root.SetMeta(AttachmentFullLengthMetaKey, connectorLength);
        root.SetMeta(AttachmentMouthExtensionMetaKey, 0.14f);

        var endpointYaw = FactoryDirection.ToYRotationRadians(projection.WorldFacing) - connectorYaw;
        if (root.GetNodeOrNull<MeshInstance3D>("ConnectorEndpoint") is MeshInstance3D endpoint)
        {
            endpoint.Rotation = new Vector3(0.0f, endpointYaw, 0.0f);
        }

        if (root.GetNodeOrNull<MeshInstance3D>("ConnectorMouth") is MeshInstance3D mouth)
        {
            mouth.Rotation = new Vector3(0.0f, endpointYaw, 0.0f);
        }

        projection.Attachment.ConfigureWorldPayload(root, worldGrid, projection);
    }

    private static Vector3 GetAttachmentConnectorStartWorld(MobileFactoryBoundaryAttachmentStructure attachment)
    {
        return attachment.ToGlobal(new Vector3(attachment.Site.CellSize * AttachmentConnectorStartOffset, 0.0f, 0.0f));
    }

    private static Vector3 GetInteriorPlatformSize(MobileFactoryProfile profile)
    {
        var width = profile.InteriorWidth * profile.InteriorCellSize + profile.InteriorPlatformBorder;
        var depth = profile.InteriorHeight * profile.InteriorCellSize + profile.InteriorPlatformBorder;
        return new Vector3(width, 0.35f, depth);
    }

    private MobileFactoryAttachmentProjection? TryGetAttachmentProjection(MobileFactoryBoundaryAttachmentStructure attachment, Vector2I anchorCell, FacingDirection facing)
    {
        foreach (var projection in GetAttachmentProjections(anchorCell, facing))
        {
            if (projection.Attachment == attachment)
            {
                return projection;
            }
        }

        return null;
    }

    private void ReleaseDeploymentReservations()
    {
        if (_deployedGrid is null)
        {
            return;
        }

        _deployedGrid.ReleaseOwner(ReservationOwnerId);
        _deployedGrid = null;
    }

    private void DisconnectAttachmentBindings()
    {
        for (var i = 0; i < _attachments.Count; i++)
        {
            ClearSingleAttachmentBinding(_attachments[i], clearDeploymentContext: true);
        }
    }

    private void ClearAttachmentBindings()
    {
        foreach (var child in _worldAttachmentVisualRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        _worldAttachmentVisualRoot.Visible = false;
        foreach (var child in _worldChildStructureRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
    }

    private void ClearSingleAttachmentBinding(MobileFactoryBoundaryAttachmentStructure attachment, bool clearDeploymentContext)
    {
        attachment.OnDeploymentCleared(_worldChildStructureRoot, _simulation);
        attachment.ClearBinding();
        if (clearDeploymentContext)
        {
            attachment.ClearDeploymentContext();
        }
    }

    private void BeginAttachmentRetraction()
    {
        if (_worldAttachmentVisualRoot.GetChildCount() == 0)
        {
            _worldAttachmentVisualRoot.Visible = false;
            return;
        }

        foreach (var child in _worldAttachmentVisualRoot.GetChildren())
        {
            if (child is Node3D connectorRoot)
            {
                connectorRoot.SetMeta(AttachmentVisualTargetMetaKey, 0.0f);
                connectorRoot.SetMeta(AttachmentRemoveWhenHiddenMetaKey, true);
            }
        }

        _worldAttachmentVisualRoot.Visible = true;
        ApplyAttachmentVisualProgress();
    }

    private void UpdateAttachmentVisualAnimation(float delta)
    {
        ApplyAttachmentVisualProgress(delta);
    }

    private void ApplyAttachmentVisualProgress(float delta = 0.0f)
    {
        if (_worldAttachmentVisualRoot.GetChildCount() == 0)
        {
            _worldAttachmentVisualRoot.Visible = false;
            return;
        }

        var hasVisibleConnector = false;
        var connectorsToRemove = new List<Node3D>();

        foreach (var child in _worldAttachmentVisualRoot.GetChildren())
        {
            if (child is Node3D connectorRoot)
            {
                var progress = connectorRoot.GetMeta(AttachmentVisualProgressMetaKey, 1.0f).AsSingle();
                var target = connectorRoot.GetMeta(AttachmentVisualTargetMetaKey, 1.0f).AsSingle();
                if (delta > 0.0f && !Mathf.IsEqualApprox(progress, target))
                {
                    var animationDuration = GetAttachmentVisualAnimationDuration(connectorRoot);
                    progress = Mathf.MoveToward(progress, target, delta / animationDuration);
                    connectorRoot.SetMeta(AttachmentVisualProgressMetaKey, progress);
                }

                var eased = EaseAttachmentVisual(progress);
                ApplySingleAttachmentVisualProgress(connectorRoot, eased);

                var removeWhenHidden = connectorRoot.GetMeta(AttachmentRemoveWhenHiddenMetaKey, false).AsBool();
                if (removeWhenHidden && progress <= 0.001f)
                {
                    connectorsToRemove.Add(connectorRoot);
                    continue;
                }

                hasVisibleConnector |= eased > 0.001f || target > 0.001f;
            }
        }

        foreach (var connectorRoot in connectorsToRemove)
        {
            connectorRoot.QueueFree();
        }

        _worldAttachmentVisualRoot.Visible = hasVisibleConnector || _worldAttachmentVisualRoot.GetChildCount() > connectorsToRemove.Count;
    }

    private static float EaseAttachmentVisual(float t)
    {
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }

    private float GetAttachmentVisualAnimationDuration(Node3D connectorRoot)
    {
        var attachmentId = connectorRoot.GetMeta(AttachmentIdMetaKey, 0UL).AsUInt64();
        for (var index = 0; index < _attachments.Count; index++)
        {
            if (_attachments[index].GetInstanceId() == attachmentId)
            {
                return Mathf.Max(0.05f, _attachments[index].WorldVisualAnimationDurationSeconds);
            }
        }

        return 0.26f;
    }

    private float GetActiveAttachmentRetractionDurationSeconds()
    {
        var duration = RecallDurationSeconds;
        for (var index = 0; index < _attachments.Count; index++)
        {
            if (_attachments[index].IsConnectedToWorld)
            {
                duration = Mathf.Max(duration, _attachments[index].WorldVisualAnimationDurationSeconds);
            }
        }

        return duration;
    }

    private void ApplySingleAttachmentVisualProgress(Node3D connectorRoot, float eased)
    {
        var fullLength = connectorRoot.GetMeta(AttachmentFullLengthMetaKey, 0.0f).AsSingle();
        var mouthExtension = connectorRoot.GetMeta(AttachmentMouthExtensionMetaKey, 0.14f).AsSingle();
        var currentLength = Mathf.Max(0.001f, fullLength * eased);

        var stem = connectorRoot.GetNodeOrNull<MeshInstance3D>("ConnectorStem");
        if (stem is not null)
        {
            stem.Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.20f, currentLength) };
            stem.Position = new Vector3(0.0f, 0.18f, currentLength * 0.5f);
            stem.Visible = eased > 0.001f;
        }

        var endpoint = connectorRoot.GetNodeOrNull<MeshInstance3D>("ConnectorEndpoint");
        if (endpoint is not null)
        {
            endpoint.Position = new Vector3(0.0f, 0.12f, fullLength * eased);
            endpoint.Visible = eased > 0.001f;
        }

        var mouth = connectorRoot.GetNodeOrNull<MeshInstance3D>("ConnectorMouth");
        if (mouth is not null)
        {
            mouth.Position = new Vector3(0.0f, 0.22f, (fullLength + mouthExtension) * eased);
            mouth.Visible = eased > 0.001f;
        }

        var attachmentId = connectorRoot.GetMeta(AttachmentIdMetaKey, 0UL).AsUInt64();
        var progress = connectorRoot.GetMeta(AttachmentVisualProgressMetaKey, 1.0f).AsSingle();
        var target = connectorRoot.GetMeta(AttachmentVisualTargetMetaKey, 1.0f).AsSingle();
        for (var index = 0; index < _attachments.Count; index++)
        {
            if (_attachments[index].GetInstanceId() == attachmentId)
            {
                _attachments[index].UpdateWorldVisualReadiness(progress, target);
                _attachments[index].ApplyWorldPayloadVisualProgress(connectorRoot, eased);
                return;
            }
        }

        if (connectorRoot.GetNodeOrNull<Node3D>("WorldPayloadRoot") is Node3D payloadRoot)
        {
            var targetPosition = payloadRoot.GetMeta("payload_target_position", Vector3.Zero).AsVector3();
            payloadRoot.Position = targetPosition * eased;
            payloadRoot.Scale = Vector3.One * Mathf.Max(0.001f, eased);
            payloadRoot.Visible = eased > 0.001f;
        }
    }

    private bool CanPlaceAttachment(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (!Profile.TryGetAttachmentMount(cell, facing, kind, out var mount) || mount is null)
        {
            return false;
        }

        var definition = MobileFactoryBoundaryAttachmentCatalog.Get(kind);
        var interiorCells = MobileFactoryBoundaryAttachmentGeometry.GetInteriorCells(definition, cell, facing);
        for (var i = 0; i < interiorCells.Count; i++)
        {
            if (!InteriorSite.IsInBounds(interiorCells[i]) || !InteriorSite.CanPlace(interiorCells[i]))
            {
                return false;
            }
        }

        if (State == MobileFactoryLifecycleState.Deployed && _deployedGrid is not null && AnchorCell is Vector2I anchorCell)
        {
            var previewStructure = FactoryStructureFactory.Create(kind, new FactoryStructurePlacement(InteriorSite, cell, facing)) as MobileFactoryBoundaryAttachmentStructure;
            if (previewStructure is null)
            {
                return false;
            }

            var projection = MobileFactoryBoundaryAttachmentGeometry.CreateProjection(previewStructure, mount, anchorCell, DeploymentFacing);
            var evaluation = previewStructure.EvaluateDeployment(_deployedGrid, projection);
            previewStructure.QueueFree();
            return evaluation.CanDeploy
                && _deployedGrid.CanReserveAll(evaluation.ReservedWorldCells, ReservationOwnerId);
        }

        return true;
    }

    private float GetHullRadiusPadding()
    {
        var platformSize = GetInteriorPlatformSize(Profile);
        return Mathf.Max(platformSize.X, platformSize.Z) * 0.45f;
    }

    private static Vector3 ClampHullCenter(GridManager? worldGrid, Vector3 hullCenter, float padding)
    {
        if (worldGrid is null)
        {
            return hullCenter;
        }

        var worldMin = worldGrid.GetWorldMin();
        var worldMax = worldGrid.GetWorldMax();
        return new Vector3(
            Mathf.Clamp(hullCenter.X, worldMin.X + padding, worldMax.X - padding),
            hullCenter.Y,
            Mathf.Clamp(hullCenter.Z, worldMin.Y + padding, worldMax.Y - padding));
    }

    private static float NormalizeAngle(float angleRadians)
    {
        while (angleRadians > Mathf.Pi)
        {
            angleRadians -= Mathf.Tau;
        }

        while (angleRadians < -Mathf.Pi)
        {
            angleRadians += Mathf.Tau;
        }

        return angleRadians;
    }

    private static float MoveAngleTowards(float current, float target, float maxDelta)
    {
        var delta = NormalizeAngle(target - current);
        if (Mathf.Abs(delta) <= maxDelta)
        {
            return NormalizeAngle(target);
        }

        return NormalizeAngle(current + Mathf.Sign(delta) * maxDelta);
    }

    private void PushStatus(string message)
    {
        _pendingStatusMessage = message;
    }
}
