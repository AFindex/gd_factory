using Godot;
using System.Collections.Generic;

public partial class MiningDrillStructure : FactoryRecipeMachineStructure
{
    private MeshInstance3D? _statusBeacon;
    private MeshInstance3D? _drum;
    private FactoryResourceKind? _resourceKind;
    private string _depositName = "未绑定矿区";

    public MiningDrillStructure()
        : base(1, 1, 1, 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.MiningDrill;
    public override string Description => "必须覆盖可开采矿点，通电后持续产出原矿或煤。";
    public FactoryResourceKind? ResourceKind => _resourceKind;

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => FactoryRecipeCatalog.MiningDrillRecipes;
    protected override string DetailSubtitle => "矿区绑定、供电与输出缓存";
    protected override string OutputSectionTitle => "采出缓存";
    protected override string OutputInventoryId => "drill-output";
    protected override string RecipeSectionTitle => "矿种";
    protected override string RecipeSectionDescription => "采矿机根据当前覆盖矿种自动切换。";
    protected override int MachinePowerRangeCells => 3;
    protected override bool SupportsRecipeSelection => false;
    protected override bool CanRunRecipe(SimulationController simulation)
    {
        SyncDepositBinding();
        return _resourceKind.HasValue;
    }

    public override void RefreshPlacement()
    {
        base.RefreshPlacement();
        SyncDepositBinding();
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"矿区：{_depositName}";
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_statusBeacon is not null)
        {
            var targetScale = HasBufferedOutput
                ? new Vector3(1.05f, 1.18f, 1.05f)
                : Vector3.One;
            _statusBeacon.Scale = _statusBeacon.Scale.Lerp(targetScale, tickAlpha * 0.45f);
            if (_statusBeacon.MaterialOverride is StandardMaterial3D beaconMaterial)
            {
                beaconMaterial.AlbedoColor = _resourceKind switch
                {
                    FactoryResourceKind.Coal => new Color("FBBF24"),
                    FactoryResourceKind.IronOre => new Color("93C5FD"),
                    FactoryResourceKind.CopperOre => new Color("FB923C"),
                    _ => new Color("FCA5A5")
                };
            }
        }

        if (_drum is not null)
        {
            var spin = (CurrentPowerStatus == FactoryPowerStatus.Powered ? 0.045f : CurrentPowerStatus == FactoryPowerStatus.Underpowered ? 0.02f : 0.0f) * tickAlpha * 60.0f;
            _drum.Rotation += new Vector3(0.0f, spin, 0.0f);
        }
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.92f, 0.18f, CellSize * 0.92f), new Color("334155"), new Vector3(0.0f, 0.09f, 0.0f));
        CreateBox("Chassis", new Vector3(CellSize * 0.72f, 0.58f, CellSize * 0.72f), new Color("475569"), new Vector3(0.0f, 0.47f, 0.0f));
        _drum = CreateBox("Drum", new Vector3(CellSize * 0.44f, 0.44f, CellSize * 0.44f), new Color("94A3B8"), new Vector3(-0.16f, 0.86f, 0.0f));
        CreateBox("Arm", new Vector3(CellSize * 0.52f, 0.12f, CellSize * 0.12f), new Color("CBD5E1"), new Vector3(CellSize * 0.10f, 0.92f, 0.0f));
        _statusBeacon = CreateBox("Beacon", new Vector3(CellSize * 0.18f, 0.18f, CellSize * 0.18f), new Color("FBBF24"), new Vector3(CellSize * 0.28f, 1.08f, 0.0f));
    }

    private void SyncDepositBinding()
    {
        if (Site is not GridManager grid || !grid.TryGetResourceDeposit(Cell, out var deposit) || deposit is null)
        {
            _resourceKind = null;
            _depositName = "未绑定矿区";
            return;
        }

        _resourceKind = deposit.ResourceKind;
        _depositName = deposit.DisplayName;
        SetActiveRecipeById(deposit.ResourceKind switch
        {
            FactoryResourceKind.Coal => "coal-extraction",
            FactoryResourceKind.IronOre => "iron-ore-extraction",
            _ => "copper-ore-extraction"
        });
    }
}
