using Godot;
using System.Collections.Generic;

public partial class PowerPoleStructure : FactoryStructure, IFactoryPowerNode
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.PowerPole;
    public override string Description => "延伸电网覆盖，把发电机的供电范围传递给更远的机器。";
    public int PowerConnectionRangeCells => 6;

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"供电覆盖：半径 {PowerConnectionRangeCells} 格";
    }

    protected override void BuildVisuals()
    {
        CreateBox("Footing", new Vector3(CellSize * 0.32f, 0.12f, CellSize * 0.32f), new Color("475569"), new Vector3(0.0f, 0.06f, 0.0f));
        CreateBox("Pole", new Vector3(CellSize * 0.12f, 1.48f, CellSize * 0.12f), new Color("A16207"), new Vector3(0.0f, 0.74f, 0.0f));
        CreateBox("Crossbar", new Vector3(CellSize * 0.62f, 0.10f, CellSize * 0.10f), new Color("FDE68A"), new Vector3(0.0f, 1.42f, 0.0f));
        CreateBox("Lamp", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.12f), new Color("FEF08A"), new Vector3(0.0f, 1.62f, 0.0f));
    }
}
