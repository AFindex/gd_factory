using Godot;
using NetFactory.Models;

public static class MergerModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddInteriorModuleShell(builder.Root, "Merger", new Vector3(cs * 0.78f, 0.34f, cs * 0.76f), new Color("134E4A"), new Color("5EEAD4"), new Vector3(0.0f, 0.24f, 0.0f));
            builder.AddInteriorTray(builder.Root, "MergerOutfeed", new Vector3(cs * 0.44f, 0.08f, cs * 0.16f), new Color("0F766E"), new Color("CCFBF1"), new Vector3(cs * 0.28f, 0.16f, 0.0f));
            builder.AddInteriorTray(builder.Root, "MergerRear", new Vector3(cs * 0.30f, 0.08f, cs * 0.16f), new Color("0F766E"), new Color("CCFBF1"), new Vector3(-cs * 0.28f, 0.16f, 0.0f));
            builder.AddInteriorTray(builder.Root, "MergerNorth", new Vector3(cs * 0.16f, 0.08f, cs * 0.30f), new Color("14B8A6"), new Color("CCFBF1"), new Vector3(0.0f, 0.16f, -cs * 0.28f));
            builder.AddInteriorTray(builder.Root, "MergerSouth", new Vector3(cs * 0.16f, 0.08f, cs * 0.30f), new Color("14B8A6"), new Color("CCFBF1"), new Vector3(0.0f, 0.16f, cs * 0.28f));
            builder.AddInteriorIndicatorLight(builder.Root, "MergerLamp", new Color("99F6E4"), new Vector3(0.0f, 0.46f, 0.0f), cs * 0.08f);
            return;
        }

        builder.AddBox("Body", new Vector3(cs * 0.86f, 0.24f, cs * 0.86f), new Color("14B8A6"), new Vector3(0.0f, 0.12f, 0.0f));
        builder.AddBox("OutputStem", new Vector3(cs * 0.42f, 0.10f, cs * 0.18f), new Color("99F6E4"), new Vector3(cs * 0.28f, 0.2f, 0.0f));
        builder.AddBox("RearStem", new Vector3(cs * 0.34f, 0.10f, cs * 0.18f), new Color("CCFBF1"), new Vector3(-cs * 0.28f, 0.2f, 0.0f));
        builder.AddBox("TopStem", new Vector3(cs * 0.18f, 0.10f, cs * 0.34f), new Color("CCFBF1"), new Vector3(0.0f, 0.2f, -cs * 0.28f));
        builder.AddBox("BottomStem", new Vector3(cs * 0.18f, 0.10f, cs * 0.34f), new Color("CCFBF1"), new Vector3(0.0f, 0.2f, cs * 0.28f));
    }
}
