using Godot;
using System.Collections.Generic;

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
        CreateBox("Base", new Vector3(CellSize * 2.82f, 0.22f, CellSize * 1.82f), new Color("3F3F46"), new Vector3(0.0f, 0.11f, 0.0f));
        CreateBox("Body", new Vector3(CellSize * 2.20f, 0.92f, CellSize * 1.22f), new Color("71717A"), new Vector3(0.0f, 0.68f, 0.0f));
        CreateBox("IntakeBay", new Vector3(CellSize * 0.40f, 0.62f, CellSize * 1.10f), new Color("52525B"), new Vector3(-CellSize * 0.92f, 0.56f, 0.0f));
        CreateBox("PressCore", new Vector3(CellSize * 0.54f, 0.58f, CellSize * 0.68f), new Color("D97706"), new Vector3(0.0f, 0.78f, 0.0f));
        CreateBox("MagazineRack", new Vector3(CellSize * 0.44f, 0.54f, CellSize * 1.18f), new Color("F59E0B"), new Vector3(CellSize * 0.92f, 0.88f, 0.0f));
        CreateBox("MagazineFeed", new Vector3(CellSize * 1.12f, 0.12f, CellSize * 0.16f), new Color("FCD34D"), new Vector3(0.0f, 1.04f, 0.0f));
        _indicator = CreateBox("Beacon", new Vector3(CellSize * 0.20f, 0.20f, CellSize * 0.20f), new Color("FDE68A"), new Vector3(CellSize * 1.04f, 1.18f, 0.0f));
    }
}
