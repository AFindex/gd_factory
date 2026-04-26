using Godot;
using System.Collections.Generic;
using NetFactory.Models;

public abstract partial class DebugItemSourceStructure : FactoryRecipeMachineStructure
{
    private readonly IReadOnlyList<FactoryRecipeDefinition> _recipes;
    private MeshInstance3D? _statusLamp;
    private Node3D? _spinnerRig;

    protected DebugItemSourceStructure(IReadOnlyList<FactoryRecipeDefinition> recipes)
        : base(1, 1, 4, 3)
    {
        _recipes = recipes;
    }

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => _recipes;
    protected override string DetailSubtitle => "零成本调试供料";
    protected override string OutputSectionTitle => "调试输出缓存";
    protected override string RecipeSectionTitle => "调试输出";
    protected override string RecipeSectionDescription => "切换当前零成本调试输出物品，无需上游输入。";

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return "调试模式：无成本持续供料";
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_spinnerRig is not null)
        {
            var spin = (HasActiveProcess ? 0.06f : HasBufferedOutput ? 0.02f : 0.01f) * tickAlpha * 60.0f;
            _spinnerRig.Rotation += new Vector3(0.0f, spin, 0.0f);
        }

        if (_statusLamp?.MaterialOverride is StandardMaterial3D lampMaterial)
        {
            var pulse = 0.55f + (0.45f * Mathf.Sin((float)(Time.GetTicksMsec() / 180.0)));
            lampMaterial.EmissionEnabled = true;
            lampMaterial.Emission = FactoryPresentation.GetBuildPrototypeAccentColor(Kind);
            lampMaterial.EmissionEnergyMultiplier = 0.55f + (pulse * 1.05f);
        }
    }

    protected override FactoryInteriorVisualRole GetInteriorVisualRole()
    {
        return FactoryInteriorVisualRole.ServiceModule;
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        DebugSourceModelDescriptor.BuildModel(builder, SiteKind);

        _spinnerRig = builder.Root.FindChild("DebugSpinnerRig", true, false) as Node3D;
        _statusLamp = builder.Root.FindChild("DebugStatusLamp", true, false) as MeshInstance3D;
    }
}

public partial class DebugOreSourceStructure : DebugItemSourceStructure
{
    public DebugOreSourceStructure()
        : base(FactoryRecipeCatalog.DebugOreSourceRecipes)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugOreSource;
    public override string Description => "调试专用原料源，可按所选配方无成本持续产出单种原矿或基础原料。";
}

public partial class DebugPartSourceStructure : DebugItemSourceStructure
{
    public DebugPartSourceStructure()
        : base(FactoryRecipeCatalog.DebugPartSourceRecipes)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugPartSource;
    public override string Description => "调试专用部件源，可按所选配方无成本持续产出单种板材或中间件。";
}

public partial class DebugCombatSourceStructure : DebugItemSourceStructure
{
    public DebugCombatSourceStructure()
        : base(FactoryRecipeCatalog.DebugCombatSourceRecipes)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugCombatSource;
    public override string Description => "调试专用战备源，可按所选配方无成本持续产出单种战备或维护补给。";
}

public partial class DebugPowerGeneratorStructure : FactoryStructure, IFactoryPowerProducer
{
    private Node3D? _rotorRig;
    private MeshInstance3D? _statusLamp;
    private MeshInstance3D? _powerRange;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugPowerGenerator;
    public override string Description => "调试专用永久供电机，无需燃料即可持续输出稳定电力。";
    public int PowerConnectionRangeCells => 6;
    public float NominalPowerSupply => 96.0f;

    public float GetAvailablePower(SimulationController simulation)
    {
        return NominalPowerSupply;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"发电：{NominalPowerSupply:0} kW 稳定输出";
        yield return $"供电覆盖：半径 {PowerConnectionRangeCells} 格";
        yield return "燃料：测试模式，无需补给";
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_rotorRig is not null)
        {
            _rotorRig.Rotation += new Vector3(0.0f, 0.08f * tickAlpha * 60.0f, 0.0f);
        }

        if (_statusLamp?.MaterialOverride is StandardMaterial3D lampMaterial)
        {
            var pulse = 0.7f + (0.3f * Mathf.Sin((float)(Time.GetTicksMsec() / 220.0)));
            lampMaterial.EmissionEnabled = true;
            lampMaterial.Emission = new Color("FDE68A");
            lampMaterial.EmissionEnergyMultiplier = 0.95f + pulse;
        }
    }

    public override void SetPowerRangeVisible(bool visible)
    {
        if (_powerRange is not null)
        {
            _powerRange.Visible = visible;
        }
    }

    protected override FactoryInteriorVisualRole GetInteriorVisualRole()
    {
        return FactoryInteriorVisualRole.PowerNode;
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        DebugPowerModelDescriptor.BuildModel(builder, SiteKind);

        _powerRange = builder.Root.FindChild("PowerRange", true, false) as MeshInstance3D;
        if (_powerRange is not null)
        {
            _powerRange.Visible = false;
        }

        _rotorRig = builder.Root.FindChild("DebugPowerRotorRig", true, false) as Node3D;
        _statusLamp = builder.Root.FindChild("PowerLamp", true, false) as MeshInstance3D;
    }
}
