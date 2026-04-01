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
    private static readonly Vector2I InteriorMinBounds = new(0, 0);
    private static readonly Vector2I InteriorMaxBounds = new(4, 4);
    private const float InteriorCellSize = 0.72f;
    private const float InteriorFloorHeight = 0.36f;
    private const float InteriorPlatformBorder = 0.18f;
    private const float TransitMoveSpeed = 5.4f;
    private const float TransitTurnSpeed = 2.5f;
    private const float AutoDeployMoveSpeed = 6.0f;
    private const float AutoDeployTurnSpeed = 4.2f;
    private const float AutoDeployArrivalDistance = 0.08f;
    private const float AutoDeployArrivalAngle = 0.04f;
    private const float RecallDurationSeconds = 0.22f;
    private static readonly Vector3 InteriorFloorLocalOffset = new(
        -((InteriorMaxBounds.X - InteriorMinBounds.X) * InteriorCellSize) * 0.5f,
        InteriorFloorHeight,
        -((InteriorMaxBounds.Y - InteriorMinBounds.Y) * InteriorCellSize) * 0.5f);

    private static readonly Vector2I[] FootprintOffsetsEast =
    {
        new(0, 0),
        new(1, 0),
        new(0, 1),
        new(1, 1)
    };

    private static readonly Vector2I[] PortOffsetsEast =
    {
        new(2, 0)
    };

    private static readonly Vector3 TransitParkingCenter = new(-11.0f, 0.0f, 7.0f);

    private readonly SimulationController _simulation;
    private readonly Node3D _structureRoot;
    private readonly Node3D _hullRoot;
    private readonly Node3D _worldPortRoot;
    private readonly MobileFactoryPortBridge _outputBridge;
    private GridManager? _deployedGrid;
    private DeployTarget? _pendingDeployTarget;
    private float _currentHeadingRadians;
    private float _recallTimer;
    private string? _pendingStatusMessage;

    public MobileFactoryInstance(string factoryId, Node3D structureRoot, SimulationController simulation)
    {
        FactoryId = factoryId;
        _simulation = simulation;
        _structureRoot = structureRoot;
        ReservationOwnerId = $"mobile:{factoryId}";
        InteriorSite = new MobileFactorySite($"mobile-site:{factoryId}", InteriorMinBounds, InteriorMaxBounds, InteriorCellSize);

        _hullRoot = CreateHullRoot();
        _structureRoot.AddChild(_hullRoot);
        _worldPortRoot = CreateWorldPortRoot();
        _structureRoot.AddChild(_worldPortRoot);

        RegisterInteriorStructure(FactoryStructureFactory.Create(
            BuildPrototypeKind.Producer,
            new FactoryStructurePlacement(InteriorSite, new Vector2I(0, 2), FacingDirection.East)));
        RegisterInteriorStructure(FactoryStructureFactory.Create(
            BuildPrototypeKind.Belt,
            new FactoryStructurePlacement(InteriorSite, new Vector2I(1, 2), FacingDirection.East)));
        RegisterInteriorStructure(FactoryStructureFactory.Create(
            BuildPrototypeKind.Belt,
            new FactoryStructurePlacement(InteriorSite, new Vector2I(2, 2), FacingDirection.East)));
        RegisterInteriorStructure(FactoryStructureFactory.Create(
            BuildPrototypeKind.Belt,
            new FactoryStructurePlacement(InteriorSite, new Vector2I(3, 2), FacingDirection.East)));

        _outputBridge = new MobileFactoryPortBridge();
        _outputBridge.Configure(InteriorSite, new Vector2I(4, 2), FacingDirection.East, $"{ReservationOwnerId}:bridge");
        _structureRoot.AddChild(_outputBridge);
        InteriorSite.AddStructure(_outputBridge);
        _simulation.RegisterStructure(_outputBridge);

        DeploymentFacing = FacingDirection.East;
        MoveToTransitParking();
        PushStatus("移动工厂待命中，使用 WASD 操作本体，按 G 进入部署模式。");
        _simulation.RebuildTopology();
    }

    public string FactoryId { get; }
    public string ReservationOwnerId { get; }
    public MobileFactorySite InteriorSite { get; }
    public MobileFactoryLifecycleState State { get; private set; } = MobileFactoryLifecycleState.InTransit;
    public Vector2I? AnchorCell { get; private set; }
    public FacingDirection DeploymentFacing { get; private set; }
    public FacingDirection TransitFacing => FactoryDirection.FromAngleRadians(_currentHeadingRadians);
    public MobileFactoryPortBridge OutputBridge => _outputBridge;
    public Vector2I InteriorMinCell => InteriorSite.MinCell;
    public Vector2I InteriorMaxCell => InteriorSite.MaxCell;
    public Vector3 WorldFocusPoint => _hullRoot.GlobalPosition;
    public bool IsBusy => State == MobileFactoryLifecycleState.AutoDeploying || State == MobileFactoryLifecycleState.Recalling;
    public Vector2I? PendingDeployAnchor => _pendingDeployTarget?.AnchorCell;
    public FacingDirection? PendingDeployFacing => _pendingDeployTarget?.Facing;

    public IEnumerable<Vector2I> GetFootprintCells(Vector2I anchorCell)
    {
        return GetFootprintCells(anchorCell, DeploymentFacing);
    }

    public IEnumerable<Vector2I> GetFootprintCells(Vector2I anchorCell, FacingDirection facing)
    {
        foreach (var offset in FootprintOffsetsEast)
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
        foreach (var offset in PortOffsetsEast)
        {
            yield return anchorCell + FactoryDirection.RotateOffset(offset, facing);
        }
    }

    public bool CanDeployAt(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        if (State == MobileFactoryLifecycleState.Deployed || State == MobileFactoryLifecycleState.Recalling)
        {
            return false;
        }

        return worldGrid.CanReserveAll(GetFootprintCells(anchorCell, facing), ReservationOwnerId)
            && worldGrid.CanReserveAll(GetPortCells(anchorCell, facing), ReservationOwnerId);
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
            hullCenter = ClampHullCenter(worldGrid, hullCenter);
        }

        ApplyHullTransform(hullCenter, _currentHeadingRadians);
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

    public bool Recall()
    {
        if (State != MobileFactoryLifecycleState.Deployed || _deployedGrid is null)
        {
            return false;
        }

        _deployedGrid.ReleaseOwner(ReservationOwnerId);
        _deployedGrid = null;
        AnchorCell = null;
        _outputBridge.ClearBinding();
        _worldPortRoot.Visible = false;
        InteriorSite.SetRuntimeState(true, true);
        State = MobileFactoryLifecycleState.Recalling;
        _recallTimer = RecallDurationSeconds;
        PushStatus("移动工厂正在回收部署机构，即将返回运输位。");
        _simulation.RebuildTopology();
        return true;
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

    public bool CanPlaceInterior(Vector2I cell)
    {
        return InteriorSite.CanPlace(cell);
    }

    public bool TryGetInteriorStructure(Vector2I cell, out FactoryStructure? structure)
    {
        return InteriorSite.TryGetStructure(cell, out structure);
    }

    public bool IsProtectedInteriorCell(Vector2I cell)
    {
        return cell == _outputBridge.Cell;
    }

    public bool PlaceInteriorStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (!InteriorSite.CanPlace(cell))
        {
            return false;
        }

        var structure = FactoryStructureFactory.Create(kind, new FactoryStructurePlacement(InteriorSite, cell, facing));
        RegisterInteriorStructure(structure);
        _simulation.RebuildTopology();
        return true;
    }

    public bool RemoveInteriorStructure(Vector2I cell)
    {
        if (!InteriorSite.TryGetStructure(cell, out var structure) || structure is null || structure == _outputBridge)
        {
            return false;
        }

        InteriorSite.RemoveStructure(structure);
        _simulation.UnregisterStructure(structure);
        structure.QueueFree();
        _simulation.RebuildTopology();
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

    public string GetPortStatusLabel()
    {
        if (State == MobileFactoryLifecycleState.Deployed && OutputBridge.IsConnectedToWorld && AnchorCell is not null)
        {
            var portCell = GetPrimaryPortCell(AnchorCell.Value, DeploymentFacing);
            return $"输出端口：朝{FactoryDirection.ToLabel(DeploymentFacing)}，已连接到世界线路 ({portCell.X}, {portCell.Y})";
        }

        if (State == MobileFactoryLifecycleState.AutoDeploying && _pendingDeployTarget is DeployTarget target)
        {
            var portCell = GetPrimaryPortCell(target.AnchorCell, target.Facing);
            return $"输出端口：目标朝{FactoryDirection.ToLabel(target.Facing)}，准备连接 ({portCell.X}, {portCell.Y})";
        }

        return $"输出端口：朝{FactoryDirection.ToLabel(DeploymentFacing)}，当前未连接世界线路";
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

        MoveToTransitParking();
        State = MobileFactoryLifecycleState.InTransit;
        PushStatus("移动工厂已回到运输位，可继续机动或重新部署。");
    }

    private void FinalizeDeployment(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        _deployedGrid = worldGrid;
        AnchorCell = anchorCell;
        DeploymentFacing = facing;
        State = MobileFactoryLifecycleState.Deployed;

        worldGrid.ReserveCells(GetFootprintCells(anchorCell, facing), ReservationOwnerId, GridReservationKind.MobileFootprint);
        worldGrid.ReserveCells(GetPortCells(anchorCell, facing), ReservationOwnerId, GridReservationKind.MobilePort);

        var footprintCenter = GetFootprintCenterWorld(worldGrid, anchorCell, facing);
        _currentHeadingRadians = FactoryDirection.ToYRotationRadians(facing);
        ApplyHullTransform(footprintCenter, _currentHeadingRadians);
        UpdateWorldPortVisual(worldGrid, anchorCell, facing, true);
        InteriorSite.SetRuntimeState(true, true);
        _outputBridge.BindToWorld(worldGrid, GetPrimaryPortCell(anchorCell, facing), facing);
        _simulation.RebuildTopology();
    }

    private void RegisterInteriorStructure(FactoryStructure structure)
    {
        _structureRoot.AddChild(structure);
        InteriorSite.AddStructure(structure);
        _simulation.RegisterStructure(structure);
    }

    private void MoveToTransitParking()
    {
        ApplyHullTransform(TransitParkingCenter, _currentHeadingRadians);
        _worldPortRoot.Visible = false;
        InteriorSite.SetRuntimeState(true, true);
    }

    private void ApplyHullTransform(Vector3 hullCenter, float headingRadians)
    {
        _hullRoot.Visible = true;
        _hullRoot.Position = hullCenter;
        _hullRoot.Rotation = new Vector3(0.0f, headingRadians, 0.0f);
        var rotatedFloorOffset = InteriorFloorLocalOffset.Rotated(Vector3.Up, headingRadians);
        InteriorSite.SetWorldTransform(hullCenter + rotatedFloorOffset, headingRadians);
    }

    private static Vector3 GetFootprintCenterWorld(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing)
    {
        var sum = Vector3.Zero;
        var count = 0;

        foreach (var cell in GetRotatedCells(anchorCell, facing, FootprintOffsetsEast))
        {
            sum += worldGrid.CellToWorld(cell);
            count++;
        }

        return count > 0 ? sum / count : worldGrid.CellToWorld(anchorCell);
    }

    private static IEnumerable<Vector2I> GetRotatedCells(Vector2I anchorCell, FacingDirection facing, IEnumerable<Vector2I> offsets)
    {
        foreach (var offset in offsets)
        {
            yield return anchorCell + FactoryDirection.RotateOffset(offset, facing);
        }
    }

    private static Node3D CreateHullRoot()
    {
        var root = new Node3D
        {
            Name = "MobileFactoryHull",
            Visible = true
        };

        root.AddChild(CreateHullMesh(
            "Platform",
            GetInteriorPlatformSize(),
            new Color("1F2937"),
            new Vector3(0.0f, 0.18f, 0.0f),
            visibleInInterior: true,
            visibleInWorld: true));
        root.AddChild(CreateHullMesh(
            "DriveCab",
            new Vector3(0.62f, 0.32f, 0.52f),
            new Color("475569"),
            new Vector3(1.12f, 0.42f, 0.0f),
            visibleInInterior: false,
            visibleInWorld: true));
        root.AddChild(CreateHullMesh(
            "DriveNose",
            new Vector3(0.30f, 0.18f, 0.26f),
            new Color("F59E0B"),
            new Vector3(1.56f, 0.34f, 0.0f),
            visibleInInterior: false,
            visibleInWorld: true));
        root.AddChild(CreateHullMesh(
            "InteriorPortMarker",
            new Vector3(0.36f, 0.08f, 0.36f),
            new Color("FB923C"),
            GetInteriorPortMarkerLocalPosition(),
            visibleInInterior: true,
            visibleInWorld: false));

        return root;
    }

    private static Node3D CreateWorldPortRoot()
    {
        var root = new Node3D
        {
            Name = "MobileFactoryWorldPort",
            Visible = false
        };

        root.AddChild(CreatePortVisual(
            "PortBase",
            new Vector3(0.72f, 0.14f, 0.72f),
            new Color("7C2D12"),
            new Vector3(0.0f, 0.08f, 0.0f)));
        root.AddChild(CreatePortVisual(
            "PortGlow",
            new Vector3(0.48f, 0.18f, 0.48f),
            new Color("FB923C"),
            new Vector3(0.0f, 0.18f, 0.0f)));
        root.AddChild(CreatePortVisual(
            "PortMouth",
            new Vector3(0.26f, 0.14f, 0.38f),
            new Color("FED7AA"),
            new Vector3(0.34f, 0.18f, 0.0f)));

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

    private static MeshInstance3D CreatePortVisual(string name, Vector3 size, Color color, Vector3 localPosition)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = localPosition,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.75f
            }
        };

        mesh.SetLayerMaskValue(InteriorRenderLayer, true);
        mesh.SetLayerMaskValue(HullRenderLayer, true);
        return mesh;
    }

    private void UpdateWorldPortVisual(GridManager worldGrid, Vector2I anchorCell, FacingDirection facing, bool visible)
    {
        _worldPortRoot.Visible = visible;
        if (!visible)
        {
            return;
        }

        var portCell = GetPrimaryPortCell(anchorCell, facing);
        _worldPortRoot.Position = worldGrid.CellToWorld(portCell);
        _worldPortRoot.Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(facing), 0.0f);
    }

    private static Vector3 GetInteriorPlatformSize()
    {
        var width = (InteriorMaxBounds.X - InteriorMinBounds.X + 1) * InteriorCellSize + InteriorPlatformBorder;
        var depth = (InteriorMaxBounds.Y - InteriorMinBounds.Y + 1) * InteriorCellSize + InteriorPlatformBorder;
        return new Vector3(width, 0.35f, depth);
    }

    private static Vector3 GetInteriorPortMarkerLocalPosition()
    {
        var portCell = new Vector2I(4, 2);
        return new Vector3(
            InteriorFloorLocalOffset.X + portCell.X * InteriorCellSize,
            0.40f,
            InteriorFloorLocalOffset.Z + portCell.Y * InteriorCellSize);
    }

    private static Vector2I GetPrimaryPortCell(Vector2I anchorCell, FacingDirection facing)
    {
        return anchorCell + FactoryDirection.RotateOffset(PortOffsetsEast[0], facing);
    }

    private static Vector3 ClampHullCenter(GridManager? worldGrid, Vector3 hullCenter)
    {
        if (worldGrid is null)
        {
            return hullCenter;
        }

        var padding = worldGrid.CellSize;
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
