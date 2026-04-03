using Godot;
using System.Collections.Generic;

public partial class AssemblerStructure : FactoryRecipeMachineStructure
{
    private MeshInstance3D? _signalLamp;
    private MeshInstance3D? _armature;

    public AssemblerStructure()
        : base(2, 2, 1, 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Assembler;
    public override string Description => "通电后消耗中间品，组装成机加工件或弹药等高阶产物。";
    public override float MaxHealth => 72.0f;

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => FactoryRecipeCatalog.AssemblerRecipes;
    protected override string DetailSubtitle => "多配方组装、供电与输入输出缓存";
    protected override string InputSectionTitle => "组装输入";
    protected override string OutputSectionTitle => "组装输出";
    protected override string InputInventoryId => "assembler-input";
    protected override string OutputInventoryId => "assembler-output";
    protected override string RecipeSectionTitle => "组装方案";
    protected override string RecipeSectionDescription => "切换当前组装机的有效配方。";
    protected override int MachinePowerRangeCells => 3;
    protected override float DispatchCooldownSeconds => 0.18f;

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_armature is not null)
        {
            var spin = (CurrentPowerStatus == FactoryPowerStatus.Powered ? 0.035f : CurrentPowerStatus == FactoryPowerStatus.Underpowered ? 0.015f : 0.0f) * tickAlpha * 60.0f;
            _armature.Rotation += new Vector3(0.0f, spin, 0.0f);
        }

        if (_signalLamp?.MaterialOverride is StandardMaterial3D material)
        {
            material.AlbedoColor = CurrentPowerStatus == FactoryPowerStatus.Powered
                ? new Color("86EFAC")
                : CurrentPowerStatus == FactoryPowerStatus.Underpowered
                    ? new Color("FDE68A")
                    : new Color("FCA5A5");
        }
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.94f, 0.18f, CellSize * 0.94f), new Color("1F2937"), new Vector3(0.0f, 0.09f, 0.0f));
        CreateBox("Body", new Vector3(CellSize * 0.80f, 0.86f, CellSize * 0.80f), new Color("334155"), new Vector3(0.0f, 0.61f, 0.0f));
        _armature = CreateBox("Armature", new Vector3(CellSize * 0.56f, 0.14f, CellSize * 0.14f), new Color("67E8F9"), new Vector3(0.0f, 1.02f, 0.0f));
        CreateBox("InputMark", new Vector3(CellSize * 0.18f, 0.08f, CellSize * 0.30f), new Color("CBD5E1"), new Vector3(-CellSize * 0.28f, 0.24f, 0.0f));
        CreateBox("OutputMark", new Vector3(CellSize * 0.18f, 0.08f, CellSize * 0.30f), new Color("A7F3D0"), new Vector3(CellSize * 0.28f, 0.24f, 0.0f));
        _signalLamp = CreateBox("SignalLamp", new Vector3(CellSize * 0.16f, 0.16f, CellSize * 0.16f), new Color("86EFAC"), new Vector3(CellSize * 0.28f, 1.18f, 0.0f));
    }
}
