using Godot;
using NetFactory.Models;
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
                    FactoryResourceKind.StoneOre => new Color("D6D3D1"),
                    FactoryResourceKind.SulfurOre => new Color("FDE047"),
                    FactoryResourceKind.QuartzOre => new Color("67E8F9"),
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
        var builder = new DefaultModelBuilder(this, CellSize);
        MiningDrillModelDescriptor.BuildModel(builder, SiteKind, GetInteriorVisualRole());
        _drum = builder.Root.FindChild("Drum", true, false) as MeshInstance3D;
        _statusBeacon = builder.Root.FindChild("Beacon", true, false) as MeshInstance3D;
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
            FactoryResourceKind.CopperOre => "copper-ore-extraction",
            FactoryResourceKind.StoneOre => "stone-ore-extraction",
            FactoryResourceKind.SulfurOre => "sulfur-ore-extraction",
            _ => "quartz-ore-extraction"
        });
    }
}
