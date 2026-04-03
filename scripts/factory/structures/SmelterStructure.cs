using Godot;
using System.Collections.Generic;

public partial class SmelterStructure : FactoryRecipeMachineStructure
{
    private MeshInstance3D? _chimneyGlow;

    public SmelterStructure()
        : base(2, 1, 1, 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Smelter;
    public override string Description => "消耗矿石和电力，将原矿稳定炼成可制造的板材。";
    public override float MaxHealth => 68.0f;

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => FactoryRecipeCatalog.SmelterRecipes;
    protected override string DetailSubtitle => "输入矿石、供电与冶炼缓存";
    protected override string InputSectionTitle => "矿石输入";
    protected override string OutputSectionTitle => "冶炼输出";
    protected override string InputInventoryId => "smelter-input";
    protected override string OutputInventoryId => "smelter-output";
    protected override int MachinePowerRangeCells => 3;
    protected override bool SupportsRecipeSelection => false;
    protected override float DispatchCooldownSeconds => 0.22f;

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_chimneyGlow?.MaterialOverride is StandardMaterial3D material)
        {
            material.AlbedoColor = CurrentPowerStatus == FactoryPowerStatus.Powered
                ? new Color("FB923C")
                : CurrentPowerStatus == FactoryPowerStatus.Underpowered
                    ? new Color("FDE68A")
                    : new Color("64748B");
        }
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.95f, 0.18f, CellSize * 0.95f), new Color("3F3F46"), new Vector3(0.0f, 0.09f, 0.0f));
        CreateBox("Body", new Vector3(CellSize * 0.78f, 0.82f, CellSize * 0.78f), new Color("52525B"), new Vector3(0.0f, 0.59f, 0.0f));
        CreateBox("Door", new Vector3(CellSize * 0.22f, 0.32f, CellSize * 0.10f), new Color("F59E0B"), new Vector3(CellSize * 0.30f, 0.48f, 0.0f));
        CreateBox("Chimney", new Vector3(CellSize * 0.20f, 0.62f, CellSize * 0.20f), new Color("71717A"), new Vector3(-CellSize * 0.20f, 1.08f, 0.0f));
        _chimneyGlow = CreateBox("Glow", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.12f), new Color("FB923C"), new Vector3(-CellSize * 0.20f, 1.44f, 0.0f));
    }
}
