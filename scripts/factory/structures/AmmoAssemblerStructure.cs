using Godot;
using System.Collections.Generic;
using NetFactory.Models;

public partial class AmmoAssemblerStructure : FactoryRecipeMachineStructure
{
    private MeshInstance3D? _indicator;

    public AmmoAssemblerStructure()
        : base(3, 2, 3, 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.AmmoAssembler;
    public override string Description => "消耗金属、导线与硫晶，持续组装炮塔补给弹药。";
    public override float MaxHealth => 62.0f;

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => FactoryRecipeCatalog.AmmoAssemblerRecipes;
    protected override string DetailSubtitle => "弹药缓存、供电与配方";
    protected override string OutputSectionTitle => "弹药缓存";
    protected override string OutputInventoryId => "ammo-output";
    protected override string RecipeSectionTitle => "弹药方案";
    protected override string RecipeSectionDescription => "切换当前弹药生产方案。";
    protected override int MachinePowerRangeCells => 3;

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_indicator is not null)
        {
            var targetScale = HasBufferedOutput
                ? new Vector3(1.15f, 1.15f, 1.15f)
                : Vector3.One;
            _indicator.Scale = _indicator.Scale.Lerp(targetScale, tickAlpha * 0.45f);
        }
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        AmmoAssemblerModelDescriptor.BuildModel(builder, SiteKind);

        _indicator = builder.Root.FindChild("Beacon", true, false) as MeshInstance3D;
    }
}
