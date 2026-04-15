using Godot;

public partial class BeltStructure : FlowTransportStructure, IFactoryTopologyAware
{
    private const float InteriorTrackWidthRatio = (2.0f / 3.0f) * 0.70f;
    private const float InteriorTrackArmLengthRatio = 0.56f;
    private const float InteriorTrackCenterRunRatio = 0.40f;
    private const float InteriorTrackCapRunRatio = 0.28f;

    private FacingDirection _inputFacing;
    private MeshInstance3D? _centerMesh;
    private MeshInstance3D? _inputArmMesh;
    private MeshInstance3D? _outputArmMesh;
    private MeshInstance3D? _arrowMesh;
    private MeshInstance3D? _capMesh;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Belt;

    public override string Description => "支持直线与拐弯的传送带，可连续堆积并逐段传递物品。";

    public override void RefreshPlacement()
    {
        Position = Site.CellToWorld(Cell);
        Rotation = new Vector3(0.0f, Site.WorldRotationRadians, 0.0f);
        Visible = Site.IsVisible;
        RebuildTrackVisuals();
    }

    public void RefreshTopology()
    {
        _inputFacing = FactoryTransportTopology.DetermineBeltPrimaryInputFacing(Site, Cell, Facing);
        Rotation = new Vector3(0.0f, Site.WorldRotationRadians, 0.0f);
        RebuildTrackVisuals();
    }

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return FactoryTransportTopology.BeltCanReceiveFrom(Site, Cell, Facing, sourceCell);
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return GetOutputCell() == targetCell;
    }

    public bool CanAcceptExternalHandoff(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveExternalHandoffFrom(sourceCell))
        {
            return false;
        }

        if (!TryResolveTargetCell(item, sourceCell, simulation, out var targetCell))
        {
            return false;
        }

        return CanAcceptTransitSpawn(item, sourceCell, targetCell);
    }

    public bool TryAcceptExternalHandoff(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveExternalHandoffFrom(sourceCell))
        {
            return false;
        }

        if (!TryResolveTargetCell(item, sourceCell, simulation, out var targetCell))
        {
            return false;
        }

        return TrySpawnTransitItem(item, sourceCell, targetCell, "flow_receive_external_handoff");
    }

    public void Reorient(FacingDirection facing)
    {
        if (Facing == facing)
        {
            return;
        }

        Facing = facing;
        RefreshTopology();
    }

    public new Vector2I GetInputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(_inputFacing);
    }

    protected override bool CanProvideTo(Vector2I requesterCell)
    {
        return IsOrthogonallyAdjacent(Cell, requesterCell);
    }

    protected override bool CanRequesterTakeState(TransitItemState state, Vector2I requesterCell)
    {
        return IsOrthogonallyAdjacent(Cell, requesterCell);
    }

    protected override bool CanReceiveProvidedFrom(Vector2I sourceCell)
    {
        return FactoryTransportTopology.BeltCanReceiveFrom(Site, Cell, Facing, sourceCell);
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            _centerMesh = CreateBox(
                "CabinChannelCore",
                new Vector3(CellSize * InteriorTrackCenterRunRatio, 0.08f, CellSize * InteriorTrackWidthRatio),
                new Color("0F172A"),
                new Vector3(0.0f, 0.12f, 0.0f));
            _inputArmMesh = CreateBox(
                "CabinInputTray",
                new Vector3(CellSize * InteriorTrackArmLengthRatio, 0.10f, CellSize * InteriorTrackWidthRatio),
                new Color("1D4ED8"),
                Vector3.Zero);
            _outputArmMesh = CreateBox(
                "CabinOutputTray",
                new Vector3(CellSize * InteriorTrackArmLengthRatio, 0.10f, CellSize * InteriorTrackWidthRatio),
                new Color("2563EB"),
                Vector3.Zero);
            _arrowMesh = CreateBox(
                "CabinDirectionStrip",
                new Vector3(CellSize * 0.20f, 0.03f, CellSize * 0.18f),
                new Color("BAE6FD"),
                new Vector3(0.26f * CellSize, 0.18f, 0.0f));
            _capMesh = CreateBox(
                "CabinTrayCap",
                new Vector3(CellSize * InteriorTrackCapRunRatio, 0.05f, CellSize * InteriorTrackCapRunRatio),
                new Color("CBD5E1"),
                new Vector3(0.0f, 0.18f, 0.0f));
            RefreshTopology();
            return;
        }

        _centerMesh = CreateColoredBox(
            "Center",
            new Vector3(CellSize * 0.42f, 0.12f, CellSize * 0.42f),
            new Color("4B5563"),
            new Vector3(0.0f, 0.08f, 0.0f));

        _inputArmMesh = CreateColoredBox(
            "InputArm",
            new Vector3(CellSize * 0.55f, 0.12f, CellSize * 0.22f),
            new Color("4B5563"),
            Vector3.Zero);

        _outputArmMesh = CreateColoredBox(
            "OutputArm",
            new Vector3(CellSize * 0.55f, 0.12f, CellSize * 0.22f),
            new Color("4B5563"),
            Vector3.Zero);

        _arrowMesh = CreateColoredBox(
            "Arrow",
            new Vector3(CellSize * 0.22f, 0.05f, CellSize * 0.18f),
            new Color("7DD3FC"),
            new Vector3(0.26f * CellSize, 0.16f, 0.0f));

        RefreshTopology();
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();
        return FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }

    protected override void BuildSitePresentationAccents()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            return;
        }

        base.BuildSitePresentationAccents();
    }

    private void RebuildTrackVisuals()
    {
        if (_centerMesh is null || _inputArmMesh is null || _outputArmMesh is null || _arrowMesh is null)
        {
            return;
        }

        var inputLocal = ToDirectionVector(FactoryDirection.ToCellOffset(_inputFacing));
        var outputLocal = ToDirectionVector(FactoryDirection.ToCellOffset(Facing));

        ConfigureCenter(_centerMesh, inputLocal, outputLocal);
        if (_capMesh is not null)
        {
            ConfigureCap(_capMesh, inputLocal, outputLocal);
        }
        ConfigureArm(_inputArmMesh, inputLocal);
        ConfigureArm(_outputArmMesh, outputLocal);
        ConfigureArrow(_arrowMesh, outputLocal);
    }

    private bool CanReceiveExternalHandoffFrom(Vector2I sourceCell)
    {
        return IsOrthogonallyAdjacentCell(Cell, sourceCell)
            && sourceCell != GetOutputCell();
    }

    private static bool IsOrthogonallyAdjacentCell(Vector2I a, Vector2I b)
    {
        var delta = a - b;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }

    private void ConfigureCenter(MeshInstance3D mesh, Vector2 inputDirection, Vector2 outputDirection)
    {
        if (SiteKind != FactorySiteKind.Interior)
        {
            mesh.Position = new Vector3(0.0f, 0.08f, 0.0f);
            return;
        }

        var spansHorizontal = Mathf.Abs(inputDirection.X) > 0.1f || Mathf.Abs(outputDirection.X) > 0.1f;
        var spansVertical = Mathf.Abs(inputDirection.Y) > 0.1f || Mathf.Abs(outputDirection.Y) > 0.1f;
        var isCorner = spansHorizontal && spansVertical;
        var trackWidth = CellSize * InteriorTrackWidthRatio;
        var straightRun = CellSize * InteriorTrackCenterRunRatio;
        var width = isCorner
            ? trackWidth
            : spansHorizontal
                ? straightRun
                : trackWidth;
        var depth = isCorner
            ? trackWidth
            : spansVertical
                ? straightRun
                : trackWidth;

        mesh.Mesh = new BoxMesh
        {
            Size = new Vector3(width, 0.08f, depth)
        };
        mesh.Position = new Vector3(0.0f, 0.12f, 0.0f);
    }

    private void ConfigureCap(MeshInstance3D mesh, Vector2 inputDirection, Vector2 outputDirection)
    {
        if (SiteKind != FactorySiteKind.Interior)
        {
            mesh.Position = new Vector3(0.0f, 0.18f, 0.0f);
            return;
        }

        var spansHorizontal = Mathf.Abs(inputDirection.X) > 0.1f || Mathf.Abs(outputDirection.X) > 0.1f;
        var spansVertical = Mathf.Abs(inputDirection.Y) > 0.1f || Mathf.Abs(outputDirection.Y) > 0.1f;
        var isCorner = spansHorizontal && spansVertical;
        var trackCapRun = CellSize * InteriorTrackCapRunRatio;
        var width = isCorner
            ? trackCapRun
            : spansHorizontal
                ? CellSize * 0.42f
                : trackCapRun;
        var depth = isCorner
            ? trackCapRun
            : spansVertical
                ? CellSize * 0.42f
                : trackCapRun;

        mesh.Mesh = new BoxMesh
        {
            Size = new Vector3(width, 0.05f, depth)
        };
        mesh.Position = new Vector3(0.0f, 0.18f, 0.0f);
    }

    private void ConfigureArm(MeshInstance3D mesh, Vector2 direction)
    {
        var alongX = Mathf.Abs(direction.X) > 0.1f;
        if (SiteKind == FactorySiteKind.Interior)
        {
            var trackWidth = CellSize * InteriorTrackWidthRatio;
            mesh.Mesh = new BoxMesh
            {
                Size = alongX
                    ? new Vector3(CellSize * InteriorTrackArmLengthRatio, 0.10f, trackWidth)
                    : new Vector3(trackWidth, 0.10f, CellSize * InteriorTrackArmLengthRatio)
            };

            mesh.Position = new Vector3(direction.X * CellSize * 0.24f, 0.08f, direction.Y * CellSize * 0.24f);
            return;
        }

        mesh.Mesh = new BoxMesh
        {
            Size = alongX
                ? new Vector3(CellSize * 0.55f, 0.12f, CellSize * 0.22f)
                : new Vector3(CellSize * 0.22f, 0.12f, CellSize * 0.55f)
        };

        mesh.Position = new Vector3(direction.X * CellSize * 0.24f, 0.08f, direction.Y * CellSize * 0.24f);
    }

    private void ConfigureArrow(MeshInstance3D mesh, Vector2 direction)
    {
        var alongX = Mathf.Abs(direction.X) > 0.1f;
        if (SiteKind == FactorySiteKind.Interior)
        {
            mesh.Mesh = new BoxMesh
            {
                Size = alongX
                    ? new Vector3(CellSize * 0.22f, 0.03f, CellSize * 0.22f)
                    : new Vector3(CellSize * 0.22f, 0.03f, CellSize * 0.22f)
            };

            mesh.Position = new Vector3(direction.X * CellSize * 0.30f, 0.16f, direction.Y * CellSize * 0.30f);
            return;
        }

        mesh.Mesh = new BoxMesh
        {
            Size = alongX
                ? new Vector3(CellSize * 0.20f, 0.05f, CellSize * 0.14f)
                : new Vector3(CellSize * 0.14f, 0.05f, CellSize * 0.20f)
        };

        mesh.Position = new Vector3(direction.X * CellSize * 0.32f, 0.16f, direction.Y * CellSize * 0.32f);
    }
}
