using Godot;
using System.Collections.Generic;

public partial class AmmoAssemblerStructure : FactoryRecipeMachineStructure
{
    private MeshInstance3D? _indicator;

    public AmmoAssemblerStructure()
        : base(1, 1, 1, 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.AmmoAssembler;
    public override string Description => "兼容型弹药组装器，保留给旧防线和移动工厂回归线。";
    public override float MaxHealth => 62.0f;

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => FactoryRecipeCatalog.AmmoAssemblerRecipes;
    protected override string DetailSubtitle => "兼容型弹药缓存与配方";
    protected override string OutputSectionTitle => "弹药缓存";
    protected override string OutputInventoryId => "ammo-output";
    protected override string RecipeSectionTitle => "弹药方案";
    protected override string RecipeSectionDescription => "切换 legacy 弹药输出方案。";
    protected override int MachinePowerRangeCells => 0;

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
        CreateBox("Base", new Vector3(CellSize * 0.9f, 0.22f, CellSize * 0.9f), new Color("3F3F46"), new Vector3(0.0f, 0.11f, 0.0f));
        CreateBox("Body", new Vector3(CellSize * 0.76f, 0.92f, CellSize * 0.76f), new Color("71717A"), new Vector3(0.0f, 0.68f, 0.0f));
        CreateBox("MagazineRack", new Vector3(CellSize * 0.28f, 0.48f, CellSize * 0.58f), new Color("F59E0B"), new Vector3(-0.14f, 1.08f, 0.0f));
        _indicator = CreateBox("Beacon", new Vector3(CellSize * 0.20f, 0.20f, CellSize * 0.20f), new Color("FDE68A"), new Vector3(CellSize * 0.26f, 1.18f, 0.0f));
    }
}
