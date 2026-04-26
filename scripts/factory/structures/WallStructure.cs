using Godot;
using NetFactory.Models;

public partial class WallStructure : FactoryStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.Wall;
    public override string Description => "高耐久的阻挡墙体，用来拖延敌人推进。";
    public override float MaxHealth => 120.0f;

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        WallModelDescriptor.BuildModel(builder, SiteKind, GetInteriorVisualRole());
    }
}
