using Godot;
using System.Collections.Generic;

public partial class AssemblerStructure : FactoryRecipeMachineStructure
{
    private static readonly Vector2I[] InputPortOffsetsEast =
    {
        new(-1, 0),
        new(-1, 1),
        new(1, -1)
    };

    private static readonly Vector2I[] OutputPortOffsetsEast =
    {
        new(3, 0),
        new(3, 1),
        new(1, 2)
    };

    private MeshInstance3D? _signalLamp;
    private MeshInstance3D? _armature;

    public AssemblerStructure()
        : base(3, 2, 3, 1)
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
        var baseSize = new Vector3(CellSize * 2.82f, 0.18f, CellSize * 1.82f);
        CreateBox("Base", baseSize, new Color("1F2937"), new Vector3(0.0f, 0.09f, 0.0f));
        CreateBox("Deck", new Vector3(CellSize * 2.58f, 0.12f, CellSize * 1.58f), new Color("0F172A"), new Vector3(0.0f, 0.20f, 0.0f));
        CreateBox("Body", new Vector3(CellSize * 2.22f, 0.86f, CellSize * 1.28f), new Color("334155"), new Vector3(0.0f, 0.61f, 0.0f));
        CreateBox("LeftBay", new Vector3(CellSize * 0.44f, 0.70f, CellSize * 1.12f), new Color("475569"), new Vector3(-CellSize * 0.86f, 0.54f, 0.0f));
        CreateBox("RightBay", new Vector3(CellSize * 0.44f, 0.70f, CellSize * 1.12f), new Color("475569"), new Vector3(CellSize * 0.86f, 0.54f, 0.0f));
        _armature = CreateBox("Armature", new Vector3(CellSize * 1.22f, 0.14f, CellSize * 0.16f), new Color("67E8F9"), new Vector3(0.0f, 1.02f, 0.0f));
        CreateBox("ArmColumn", new Vector3(CellSize * 0.18f, 0.48f, CellSize * 0.18f), new Color("94A3B8"), new Vector3(0.0f, 0.82f, 0.0f));
        CreateBox("ToolHead", new Vector3(CellSize * 0.26f, 0.14f, CellSize * 0.34f), new Color("38BDF8"), new Vector3(0.0f, 0.88f, CellSize * 0.18f));
        _signalLamp = CreateBox("SignalLamp", new Vector3(CellSize * 0.16f, 0.16f, CellSize * 0.16f), new Color("86EFAC"), new Vector3(CellSize * 1.02f, 1.18f, 0.0f));
        CreatePortMarkers();
    }

    private void CreatePortMarkers()
    {
        for (var index = 0; index < InputPortOffsetsEast.Length; index++)
        {
            var offset = Footprint.GetLocalCellCenterOffset(InputPortOffsetsEast[index], CellSize);
            CreateBox(
                $"InputPortMarker_{index}",
                new Vector3(CellSize * 0.18f, 0.16f, CellSize * 0.18f),
                new Color("38BDF8"),
                offset + new Vector3(0.0f, 0.16f, 0.0f));
        }

        for (var index = 0; index < OutputPortOffsetsEast.Length; index++)
        {
            var offset = Footprint.GetLocalCellCenterOffset(OutputPortOffsetsEast[index], CellSize);
            CreateBox(
                $"OutputPortMarker_{index}",
                new Vector3(CellSize * 0.18f, 0.16f, CellSize * 0.18f),
                new Color("F59E0B"),
                offset + new Vector3(0.0f, 0.16f, 0.0f));
        }
    }
}
