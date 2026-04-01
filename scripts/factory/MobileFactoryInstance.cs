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

    private readonly SimulationController _simulation;
    private readonly Node3D _structureRoot;
    private readonly Node3D _hullRoot;
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
            Profile.InteriorCellSize);
        _interiorFloorLocalOffset = CalculateInteriorFloorLocalOffset(Profile);

        _hullRoot = CreateHullRoot(Profile, _interiorFloorLocalOffset);
        _structureRoot.AddChild(_hullRoot);
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

    public bool CanDeployAt(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        if (State == MobileFactoryLifecycleState.Deployed || State == MobileFactoryLifecycleState.Recalling)
        {
            return false;
        }

        if (!worldGrid.CanReserveAll(GetFootprintCells(anchorCell, facing), ReservationOwnerId))
        {
            return false;
        }

        foreach (var projection in GetAttachmentProjections(anchorCell, facing))
        {
            if (!worldGrid.CanReserveAll(projection.WorldCells, ReservationOwnerId))
            {
                return false;
            }
        }

        return true;
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
        PushStatus($"部署命令已确认，正在前往 ({anchorCell.X}, {anchorCell.Y}) 并对齐朝向 {FactoryDirection.ToLabel(facing)}。");
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
        if (!CanDeployAt(worldGrid, anchorCell, facing))
        {
            return false;
        }

        FinalizeDeployment(worldGrid, anchorCell, facing);
        PushStatus($"已部署到 ({anchorCell.X}, {anchorCell.Y})，朝向 {FactoryDirection.ToLabel(facing)}。");
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

        _pendingTransitPosition = _hullRoot.Position;
        _pendingTransitHeadingRadians = _currentHeadingRadians;
        ReleaseDeploymentReservations();
        ClearAttachmentBindings();
        AnchorCell = null;
        InteriorSite.SetRuntimeState(true, true);
        State = MobileFactoryLifecycleState.Recalling;
        _recallTimer = RecallDurationSeconds;
        PushStatus("移动工厂正在收拢边界 attachment，准备切回移动态；未激活边界会阻塞等待重新部署。");
        _simulation.RebuildTopology();
        return true;
    }

    public bool Recall()
    {
        return ReturnToTransitMode();
    }

    public void UpdateRuntime(double delta)
    {
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
        if (structure is MobileFactoryBoundaryAttachmentStructure attachment)
        {
            _attachments.Remove(attachment);
            attachment.ClearBinding();
        }

        structure.QueueFree();
        RefreshRuntimeAfterInteriorChange();
        return true;
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
            var channelLabel = attachment.ChannelType == MobileFactoryAttachmentChannelType.ItemOutput ? "输出" : "输入";
            var stateLabel = attachment.ConnectionStateLabel;

            if (attachment.IsConnectedToWorld)
            {
                var portCell = attachment.WorldPortCell;
                lines.Add($"{channelLabel}端口 {i + 1}：朝{FactoryDirection.ToLabel(attachment.WorldFacing)}，{stateLabel} ({portCell.X}, {portCell.Y})");
            }
            else if (State == MobileFactoryLifecycleState.AutoDeploying && AnchorCell is null && _pendingDeployTarget is DeployTarget target)
            {
                var projection = TryGetAttachmentProjection(attachment, target.AnchorCell, target.Facing);
                if (projection is not null)
                {
                    lines.Add($"{channelLabel}端口 {i + 1}：目标朝{FactoryDirection.ToLabel(projection.WorldFacing)}，准备连接 ({projection.WorldPortCell.X}, {projection.WorldPortCell.Y})");
                }
            }
            else
            {
                lines.Add($"{channelLabel}端口 {i + 1}：朝{FactoryDirection.ToLabel(attachment.Facing)}，当前{stateLabel}");
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
        var desiredHeading = FactoryDirection.ToYRotationRadians(target.Facing);
        var currentCenter = _hullRoot.Position;
        var toTarget = targetCenter - currentCenter;
        var planarDistance = new Vector2(toTarget.X, toTarget.Z).Length();

        if (planarDistance > AutoDeployArrivalDistance)
        {
            var maxMove = AutoDeployMoveSpeed * delta;
            var move = Mathf.Min(maxMove, planarDistance);
            var moveDir = new Vector3(toTarget.X / planarDistance, 0.0f, toTarget.Z / planarDistance);
            currentCenter += moveDir * move;
            currentCenter = ClampHullCenter(target.WorldGrid, currentCenter, GetHullRadiusPadding());
        }
        else
        {
            currentCenter = targetCenter;
        }

        _currentHeadingRadians = MoveAngleTowards(_currentHeadingRadians, desiredHeading, AutoDeployTurnSpeed * delta);
        ApplyHullTransform(currentCenter, _currentHeadingRadians);

        if (currentCenter.DistanceTo(targetCenter) > AutoDeployArrivalDistance || Mathf.Abs(NormalizeAngle(desiredHeading - _currentHeadingRadians)) > AutoDeployArrivalAngle)
        {
            return;
        }

        if (!CanDeployAt(target.WorldGrid, target.AnchorCell, target.Facing))
        {
            _pendingDeployTarget = null;
            State = MobileFactoryLifecycleState.InTransit;
            PushStatus("自动部署在最终校验时失败，目标区域已无效。");
            return;
        }

        FinalizeDeployment(target.WorldGrid, target.AnchorCell, target.Facing);
        _pendingDeployTarget = null;
        PushStatus($"自动部署完成，已在 ({target.AnchorCell.X}, {target.AnchorCell.Y}) 朝 {FactoryDirection.ToLabel(target.Facing)} 展开。");
    }

    private void UpdateRecall(float delta)
    {
        _recallTimer -= delta;
        if (_recallTimer > 0.0f)
        {
            return;
        }

        ApplyHullTransform(_pendingTransitPosition, _pendingTransitHeadingRadians);
        _worldAttachmentVisualRoot.Visible = false;
        InteriorSite.SetRuntimeState(true, true);
        State = MobileFactoryLifecycleState.InTransit;
        PushStatus("移动工厂已切回移动态，可继续机动或重新部署；未连接边界 attachment 会如实阻塞等待重连。");
    }

    private void FinalizeDeployment(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        _deployedGrid = worldGrid;
        AnchorCell = anchorCell;
        DeploymentFacing = facing;
        State = MobileFactoryLifecycleState.Deployed;

        worldGrid.ReserveCells(GetFootprintCells(anchorCell, facing), ReservationOwnerId, GridReservationKind.MobileFootprint);

        var footprintCenter = GetFootprintCenterWorld(worldGrid, anchorCell, facing);
        _currentHeadingRadians = FactoryDirection.ToYRotationRadians(facing);
        ApplyHullTransform(footprintCenter, _currentHeadingRadians);
        ActivateAttachmentsForDeployment(worldGrid, anchorCell, facing);
        InteriorSite.SetRuntimeState(true, true);
        _simulation.RebuildTopology();
    }

    private void ActivateAttachmentsForDeployment(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        ClearAttachmentBindings();

        foreach (var projection in GetAttachmentProjections(anchorCell, facing))
        {
            if (!worldGrid.CanReserveAll(projection.WorldCells, ReservationOwnerId))
            {
                continue;
            }

            worldGrid.ReserveCells(projection.WorldCells, ReservationOwnerId, GridReservationKind.MobilePort, projection.Attachment);
            projection.Attachment.BindToWorld(worldGrid, projection);
        }

        RebuildWorldAttachmentVisuals(worldGrid, anchorCell, facing);
    }

    private void RegisterInteriorStructure(FactoryStructure structure)
    {
        _structureRoot.AddChild(structure);
        InteriorSite.AddStructure(structure);
        _simulation.RegisterStructure(structure);
    }

    private void RefreshRuntimeAfterInteriorChange()
    {
        if (State == MobileFactoryLifecycleState.Deployed && _deployedGrid is not null && AnchorCell is Vector2I anchorCell)
        {
            ReleaseDeploymentReservations();
            _deployedGrid.ReserveCells(GetFootprintCells(anchorCell, DeploymentFacing), ReservationOwnerId, GridReservationKind.MobileFootprint);
            ActivateAttachmentsForDeployment(_deployedGrid, anchorCell, DeploymentFacing);
        }
        else
        {
            ClearAttachmentBindings();
        }

        _simulation.RebuildTopology();
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
        var cabWidth = Mathf.Clamp(platformSize.X * 0.22f, 0.58f, 0.96f);
        var cabDepth = Mathf.Clamp(platformSize.Z * 0.18f, 0.48f, 0.86f);
        var noseWidth = cabWidth * 0.5f;

        root.AddChild(CreateHullMesh(
            "Platform",
            platformSize,
            profile.HullColor,
            new Vector3(0.0f, 0.18f, 0.0f),
            visibleInInterior: true,
            visibleInWorld: true));
        root.AddChild(CreateHullMesh(
            "DriveCab",
            new Vector3(cabWidth, 0.34f, cabDepth),
            profile.CabColor,
            new Vector3(platformSize.X * 0.50f, 0.42f, 0.0f),
            visibleInInterior: false,
            visibleInWorld: true));
        root.AddChild(CreateHullMesh(
            "DriveNose",
            new Vector3(noseWidth, 0.18f, cabDepth * 0.55f),
            profile.AccentColor,
            new Vector3(platformSize.X * 0.69f, 0.34f, 0.0f),
            visibleInInterior: false,
            visibleInWorld: true));

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
        foreach (var child in _worldAttachmentVisualRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        foreach (var projection in GetAttachmentProjections(anchorCell, facing))
        {
            if (!projection.Attachment.IsConnectedToWorld)
            {
                continue;
            }

            _worldAttachmentVisualRoot.AddChild(CreateWorldAttachmentVisual(worldGrid, projection));
        }

        _worldAttachmentVisualRoot.Visible = _worldAttachmentVisualRoot.GetChildCount() > 0;
    }

    private Node3D CreateWorldAttachmentVisual(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        var root = new Node3D
        {
            Name = $"{projection.Attachment.Name}_WorldConnector"
        };

        var start = projection.Attachment.GlobalPosition + FactoryDirection.ToWorldForward(projection.Attachment.GlobalRotation.Y) * (projection.Attachment.Site.CellSize * 0.34f);
        var end = worldGrid.CellToWorld(projection.WorldPortCell);
        var connectorVector = end - start;
        var connectorLength = new Vector2(connectorVector.X, connectorVector.Z).Length();

        var connector = new MeshInstance3D
        {
            Name = "ConnectorStem",
            Mesh = new BoxMesh { Size = new Vector3(0.18f, 0.16f, Mathf.Max(0.24f, connectorLength)) },
            Position = (start + end) * 0.5f + new Vector3(0.0f, 0.18f, 0.0f),
            Rotation = new Vector3(0.0f, Mathf.Atan2(connectorVector.X, connectorVector.Z), 0.0f),
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
            Mesh = new BoxMesh { Size = new Vector3(0.56f, 0.16f, 0.56f) },
            Position = end + new Vector3(0.0f, 0.10f, 0.0f),
            Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(projection.WorldFacing), 0.0f),
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
            Mesh = new BoxMesh { Size = new Vector3(0.18f, 0.10f, 0.28f) },
            Position = end + FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(projection.WorldFacing)) * 0.14f + new Vector3(0.0f, 0.20f, 0.0f),
            Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(projection.WorldFacing), 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = projection.Attachment.AttachmentDefinition.ConnectorColor.Lightened(0.12f),
                Roughness = 0.55f
            }
        };
        root.AddChild(mouth);

        return root;
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

    private void ClearAttachmentBindings()
    {
        for (var i = 0; i < _attachments.Count; i++)
        {
            _attachments[i].ClearBinding();
        }

        foreach (var child in _worldAttachmentVisualRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        _worldAttachmentVisualRoot.Visible = false;
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
            previewStructure.QueueFree();
            return _deployedGrid.CanReserveAll(projection.WorldCells, ReservationOwnerId);
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
