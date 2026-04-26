using Godot;
using NetFactory.Models;

public static class MiningDrillModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        builder.AddBox("Base", new Vector3(cs * 0.92f, 0.18f, cs * 0.92f), new Color("334155"), new Vector3(0.0f, 0.09f, 0.0f));
        builder.AddBox("Chassis", new Vector3(cs * 0.72f, 0.58f, cs * 0.72f), new Color("475569"), new Vector3(0.0f, 0.47f, 0.0f));
        builder.AddBox("Drum", new Vector3(cs * 0.44f, 0.44f, cs * 0.44f), new Color("94A3B8"), new Vector3(-0.16f, 0.86f, 0.0f));
        builder.AddBox("Arm", new Vector3(cs * 0.52f, 0.12f, cs * 0.12f), new Color("CBD5E1"), new Vector3(cs * 0.10f, 0.92f, 0.0f));
        builder.AddBox("Beacon", new Vector3(cs * 0.18f, 0.18f, cs * 0.18f), new Color("FBBF24"), new Vector3(cs * 0.28f, 1.08f, 0.0f));
    }
}
