using Godot;
using NetFactory.Models;

public static class PortBridgeModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        builder.AddBox("Pad", new Vector3(cs * 0.72f, 0.14f, cs * 0.72f), new Color("475569"), new Vector3(0.0f, 0.08f, 0.0f));
        builder.AddBox("Chute", new Vector3(cs * 0.68f, 0.18f, cs * 0.22f), new Color("F97316"), new Vector3(0.08f * cs, 0.18f, 0.0f));
        builder.AddBox("Beacon", new Vector3(cs * 0.16f, 0.12f, cs * 0.16f), new Color("FED7AA"), new Vector3(cs * 0.28f, 0.28f, 0.0f));
    }
}
