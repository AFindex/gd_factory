using Godot;
using System.Collections.Generic;

public sealed class MobileFactoryInstance
{
    private const int InteriorRenderLayer = 1;
    private const int HullRenderLayer = 2;
    private static readonly Vector2I InteriorMinBounds = new(0, 0);
    private static readonly Vector2I InteriorMaxBounds = new(4, 4);
    private const float InteriorCellSize = 0.72f;
    private const float InteriorFloorHeight = 0.36f;
    private const float InteriorPlatformBorder = 0.18f;
    private static readonly Vector3 InteriorFloorLocalOffset = new(
        -((InteriorMaxBounds.X - InteriorMinBounds.X) * InteriorCellSize) * 0.5f,
        InteriorFloorHeight,
        -((InteriorMaxBounds.Y - InteriorMinBounds.Y) * InteriorCellSize) * 0.5f);

    private static readonly Vector2I[] FootprintOffsets =
    {
        new(0, 0),
        new(1, 0),
        new(0, 1),
        new(1, 1)
    };

    private static readonly Vector2I[] PortOffsets =
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

        MoveToTransitParking();
        _simulation.RebuildTopology();
    }

    public string FactoryId { get; }
    public string ReservationOwnerId { get; }
    public MobileFactorySite InteriorSite { get; }
    public MobileFactoryLifecycleState State { get; private set; } = MobileFactoryLifecycleState.InTransit;
    public Vector2I? AnchorCell { get; private set; }
    public MobileFactoryPortBridge OutputBridge => _outputBridge;
    public Vector2I InteriorMinCell => InteriorSite.MinCell;
    public Vector2I InteriorMaxCell => InteriorSite.MaxCell;
    public Vector3 WorldFocusPoint => _hullRoot.GlobalPosition;

    public IEnumerable<Vector2I> GetFootprintCells(Vector2I anchorCell)
    {
        foreach (var offset in FootprintOffsets)
        {
            yield return anchorCell + offset;
        }
    }

    public IEnumerable<Vector2I> GetPortCells(Vector2I anchorCell)
    {
        foreach (var offset in PortOffsets)
        {
            yield return anchorCell + offset;
        }
    }

    public bool CanDeployAt(GridManager worldGrid, Vector2I anchorCell)
    {
        if (State == MobileFactoryLifecycleState.Deployed)
        {
            return false;
        }

        return worldGrid.CanReserveAll(GetFootprintCells(anchorCell), ReservationOwnerId)
            && worldGrid.CanReserveAll(GetPortCells(anchorCell), ReservationOwnerId);
    }

    public bool TryDeploy(GridManager worldGrid, Vector2I anchorCell)
    {
        if (!CanDeployAt(worldGrid, anchorCell))
        {
            return false;
        }

        _deployedGrid = worldGrid;
        AnchorCell = anchorCell;
        worldGrid.ReserveCells(GetFootprintCells(anchorCell), ReservationOwnerId, GridReservationKind.MobileFootprint);
        worldGrid.ReserveCells(GetPortCells(anchorCell), ReservationOwnerId, GridReservationKind.MobilePort);

        var footprintCenter = GetFootprintCenterWorld(worldGrid, anchorCell);
        PositionHullAndInterior(footprintCenter);
        UpdateWorldPortVisual(worldGrid, anchorCell, true);
        InteriorSite.SetRuntimeState(true, true);
        _outputBridge.BindToWorld(worldGrid, GetPrimaryPortCell(anchorCell), FacingDirection.East);
        State = MobileFactoryLifecycleState.Deployed;
        _simulation.RebuildTopology();
        return true;
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
        MoveToTransitParking();
        State = MobileFactoryLifecycleState.InTransit;
        _simulation.RebuildTopology();
        return true;
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
        return State == MobileFactoryLifecycleState.Deployed && OutputBridge.IsConnectedToWorld && AnchorCell is not null
            ? $"输出端口：朝东，已连接到世界线路 ({GetPrimaryPortCell(AnchorCell.Value).X}, {GetPrimaryPortCell(AnchorCell.Value).Y})"
            : "输出端口：朝东，当前未连接世界线路";
    }

    private void RegisterInteriorStructure(FactoryStructure structure)
    {
        _structureRoot.AddChild(structure);
        InteriorSite.AddStructure(structure);
        _simulation.RegisterStructure(structure);
    }

    private void MoveToTransitParking()
    {
        PositionHullAndInterior(TransitParkingCenter);
        _worldPortRoot.Visible = false;
        InteriorSite.SetRuntimeState(true, false);
    }

    private void PositionHullAndInterior(Vector3 hullCenter)
    {
        _hullRoot.Visible = true;
        _hullRoot.Position = hullCenter;
        InteriorSite.SetWorldOrigin(hullCenter + InteriorFloorLocalOffset);
    }

    private static Vector3 GetFootprintCenterWorld(GridManager worldGrid, Vector2I anchorCell)
    {
        return worldGrid.CellToWorld(anchorCell) + new Vector3(worldGrid.CellSize * 0.5f, 0.0f, worldGrid.CellSize * 0.5f);
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

    private void UpdateWorldPortVisual(GridManager worldGrid, Vector2I anchorCell, bool visible)
    {
        _worldPortRoot.Visible = visible;
        if (!visible)
        {
            return;
        }

        var portCell = GetPrimaryPortCell(anchorCell);
        _worldPortRoot.Position = worldGrid.CellToWorld(portCell);
        _worldPortRoot.Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(FacingDirection.East), 0.0f);
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

    private static Vector2I GetPrimaryPortCell(Vector2I anchorCell)
    {
        return anchorCell + PortOffsets[0];
    }
}
