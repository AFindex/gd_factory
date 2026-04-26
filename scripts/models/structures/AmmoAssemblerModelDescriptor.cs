using Godot;
using NetFactory.Models;

public static class AmmoAssemblerModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("Base", new Vector3(cs * 2.82f, 0.18f, cs * 1.82f), new Color("1F2937"), new Vector3(0.0f, 0.09f, 0.0f));
            builder.AddInteriorModuleShell(builder.Root, "AmmoCabin", new Vector3(cs * 2.18f, 0.84f, cs * 1.22f), new Color("52525B"), new Color("A1A1AA"), new Vector3(0.0f, 0.62f, 0.0f));
            builder.AddBox("FeedDrawer", new Vector3(cs * 0.34f, 0.48f, cs * 1.00f), new Color("3F3F46"), new Vector3(-cs * 0.92f, 0.48f, 0.0f));
            builder.AddBox("MagazineRack", new Vector3(cs * 0.34f, 0.48f, cs * 1.00f), new Color("F59E0B"), new Vector3(cs * 0.92f, 0.74f, 0.0f));
            builder.AddInteriorTray(builder.Root, "AmmoFeed", new Vector3(cs * 0.98f, 0.10f, cs * 0.14f), new Color("FCD34D"), new Color("FEF3C7"), new Vector3(0.0f, 0.92f, 0.0f));
            builder.AddBox("PressCore", new Vector3(cs * 0.48f, 0.44f, cs * 0.60f), new Color("D97706"), new Vector3(0.0f, 0.68f, 0.0f));
            builder.AddBox("Beacon", new Vector3(cs * 0.18f, 0.18f, cs * 0.18f), new Color("FDE68A"), new Vector3(cs * 1.00f, 1.04f, 0.0f));
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 2.82f, 0.22f, cs * 1.82f), new Color("3F3F46"), new Vector3(0.0f, 0.11f, 0.0f));
        builder.AddBox("Body", new Vector3(cs * 2.20f, 0.92f, cs * 1.22f), new Color("71717A"), new Vector3(0.0f, 0.68f, 0.0f));
        builder.AddBox("IntakeBay", new Vector3(cs * 0.40f, 0.62f, cs * 1.10f), new Color("52525B"), new Vector3(-cs * 0.92f, 0.56f, 0.0f));
        builder.AddBox("PressCore", new Vector3(cs * 0.54f, 0.58f, cs * 0.68f), new Color("D97706"), new Vector3(0.0f, 0.78f, 0.0f));
        builder.AddBox("MagazineRack", new Vector3(cs * 0.44f, 0.54f, cs * 1.18f), new Color("F59E0B"), new Vector3(cs * 0.92f, 0.88f, 0.0f));
        builder.AddBox("MagazineFeed", new Vector3(cs * 1.12f, 0.12f, cs * 0.16f), new Color("FCD34D"), new Vector3(0.0f, 1.04f, 0.0f));
        builder.AddBox("Beacon", new Vector3(cs * 0.20f, 0.20f, cs * 0.20f), new Color("FDE68A"), new Vector3(cs * 1.04f, 1.18f, 0.0f));
    }
}
