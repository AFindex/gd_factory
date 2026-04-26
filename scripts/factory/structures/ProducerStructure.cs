using Godot;
using NetFactory.Models;
using System.Collections.Generic;

public partial class ProducerStructure : FactoryRecipeMachineStructure
{
    private MeshInstance3D? _indicator;

    public ProducerStructure()
        : base(1, 1, 1, 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Producer;
    public override string Description => "兼容型占位生产器，仅用于旧回归线与 debug 产物流。";

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => FactoryRecipeCatalog.ProducerRecipes;
    protected override string DetailSubtitle => "兼容型输出缓存与配方";
    protected override string OutputSectionTitle => "兼容输出缓存";
    protected override string OutputInventoryId => "producer-output";
    protected override string RecipeSectionTitle => "兼容配方";
    protected override string RecipeSectionDescription => "切换 legacy 生产器输出类型。";
    protected override int MachinePowerRangeCells => 0;

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_indicator is not null)
        {
            var targetScale = HasBufferedOutput
                ? new Vector3(1.15f, 1.15f, 1.15f)
                : Vector3.One;
            _indicator.Scale = _indicator.Scale.Lerp(targetScale, tickAlpha * 0.5f);
        }
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        ProducerModelDescriptor.BuildModel(builder, SiteKind, GetInteriorVisualRole());
        _indicator = builder.Root.FindChild("Outlet", true, false) as MeshInstance3D;
    }
}
