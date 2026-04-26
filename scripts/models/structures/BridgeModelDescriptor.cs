using Godot;
using NetFactory.Models;

public static class BridgeModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("BridgeDeck", new Vector3(cs * 0.94f, 0.10f, cs * 0.94f), new Color("1E293B"), new Vector3(0.0f, 0.10f, 0.0f));
            builder.AddInteriorTray(builder.Root, "BridgeLower", new Vector3(cs * 0.88f, 0.08f, cs * 0.18f), new Color("0EA5E9"), new Color("BAE6FD"), new Vector3(0.0f, 0.16f, 0.0f));
            builder.AddInteriorTray(builder.Root, "BridgeUpper", new Vector3(cs * 0.18f, 0.08f, cs * 0.88f), new Color("F59E0B"), new Color("FDE68A"), new Vector3(0.0f, 0.28f, 0.0f));
            builder.AddBox("BridgeSpacer", new Vector3(cs * 0.22f, 0.12f, cs * 0.22f), new Color("475569"), new Vector3(0.0f, 0.22f, 0.0f));
            builder.AddInteriorIndicatorLight(builder.Root, "BridgeLamp", new Color("E2E8F0"), new Vector3(0.0f, 0.40f, 0.0f), cs * 0.07f);
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 0.92f, 0.16f, cs * 0.92f), new Color("475569"), new Vector3(0.0f, 0.08f, 0.0f));
        builder.AddBox("EastWest", new Vector3(cs * 0.95f, 0.10f, cs * 0.20f), new Color("F59E0B"), new Vector3(0.0f, 0.38f, 0.0f));
        builder.AddBox("NorthSouth", new Vector3(cs * 0.20f, 0.10f, cs * 0.95f), new Color("38BDF8"), new Vector3(0.0f, 0.22f, 0.0f));
    }
}
