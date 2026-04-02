using Godot;

public partial class WallStructure : FactoryStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.Wall;
    public override string Description => "高耐久的阻挡墙体，用来拖延敌人推进。";
    public override float MaxHealth => 120.0f;

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.94f, 0.32f, CellSize * 0.94f), new Color("374151"), new Vector3(0.0f, 0.16f, 0.0f));
        CreateBox("WallBody", new Vector3(CellSize * 0.78f, 1.26f, CellSize * 0.42f), new Color("9CA3AF"), new Vector3(0.0f, 0.82f, 0.0f));
        CreateBox("TopCap", new Vector3(CellSize * 0.88f, 0.14f, CellSize * 0.52f), new Color("E5E7EB"), new Vector3(0.0f, 1.48f, 0.0f));
    }
}
