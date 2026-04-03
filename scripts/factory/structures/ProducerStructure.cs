using Godot;
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
        CreateBox("Base", new Vector3(CellSize * 0.9f, 0.8f, CellSize * 0.9f), new Color("6D8B74"), new Vector3(0.0f, 0.4f, 0.0f));
        CreateBox("Tower", new Vector3(CellSize * 0.45f, 1.4f, CellSize * 0.45f), new Color("9DC08B"), new Vector3(-0.15f, 1.1f, 0.0f));
        _indicator = CreateBox("Outlet", new Vector3(CellSize * 0.35f, 0.2f, CellSize * 0.35f), new Color("D7FFC2"), new Vector3(CellSize * 0.45f, 0.75f, 0.0f));
    }
}
