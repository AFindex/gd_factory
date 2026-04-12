using Godot;

public partial class MergerStructure : FlowTransportStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.Merger;
    public override string Description => "将后方、左侧和右侧三路物流汇入前方单一路径。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return FactoryTransportTopology.MergerCanReceiveFrom(Cell, Facing, sourceCell);
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateInteriorModuleShell(this, "Merger", new Vector3(CellSize * 0.78f, 0.34f, CellSize * 0.76f), new Color("134E4A"), new Color("5EEAD4"), new Vector3(0.0f, 0.24f, 0.0f));
            CreateInteriorTray(this, "MergerOutfeed", new Vector3(CellSize * 0.44f, 0.08f, CellSize * 0.16f), new Color("0F766E"), new Color("CCFBF1"), new Vector3(CellSize * 0.28f, 0.16f, 0.0f));
            CreateInteriorTray(this, "MergerRear", new Vector3(CellSize * 0.30f, 0.08f, CellSize * 0.16f), new Color("0F766E"), new Color("CCFBF1"), new Vector3(-CellSize * 0.28f, 0.16f, 0.0f));
            CreateInteriorTray(this, "MergerNorth", new Vector3(CellSize * 0.16f, 0.08f, CellSize * 0.30f), new Color("14B8A6"), new Color("CCFBF1"), new Vector3(0.0f, 0.16f, -CellSize * 0.28f));
            CreateInteriorTray(this, "MergerSouth", new Vector3(CellSize * 0.16f, 0.08f, CellSize * 0.30f), new Color("14B8A6"), new Color("CCFBF1"), new Vector3(0.0f, 0.16f, CellSize * 0.28f));
            CreateInteriorIndicatorLight(this, "MergerLamp", new Color("99F6E4"), new Vector3(0.0f, 0.46f, 0.0f), CellSize * 0.08f);
            return;
        }

        CreateColoredBox("Body", new Vector3(CellSize * 0.86f, 0.24f, CellSize * 0.86f), new Color("14B8A6"), new Vector3(0.0f, 0.12f, 0.0f));
        CreateColoredBox("OutputStem", new Vector3(CellSize * 0.42f, 0.10f, CellSize * 0.18f), new Color("99F6E4"), new Vector3(CellSize * 0.28f, 0.2f, 0.0f));
        CreateColoredBox("RearStem", new Vector3(CellSize * 0.34f, 0.10f, CellSize * 0.18f), new Color("CCFBF1"), new Vector3(-CellSize * 0.28f, 0.2f, 0.0f));
        CreateColoredBox("TopStem", new Vector3(CellSize * 0.18f, 0.10f, CellSize * 0.34f), new Color("CCFBF1"), new Vector3(0.0f, 0.2f, -CellSize * 0.28f));
        CreateColoredBox("BottomStem", new Vector3(CellSize * 0.18f, 0.10f, CellSize * 0.34f), new Color("CCFBF1"), new Vector3(0.0f, 0.2f, CellSize * 0.28f));
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();
        return FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var edgeDistance = CellSize * 0.5f;
        var input = ToDirectionVector(state.SourceCell - Cell).Rotated(FactoryDirection.ToYRotationRadians(Facing)) * edgeDistance;
        var output = ToDirectionVector(state.TargetCell - Cell).Rotated(FactoryDirection.ToYRotationRadians(Facing)) * edgeDistance;
        var oneMinus = 1.0f - progress;
        var point2D =
            oneMinus * oneMinus * input +
            2.0f * oneMinus * progress * Vector2.Zero +
            progress * progress * output;

        return new Vector3(point2D.X, ItemHeight, point2D.Y);
    }
}
