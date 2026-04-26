using Godot;
using NetFactory.Models;

public static class SplitterModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddInteriorModuleShell(builder.Root, "Splitter", new Vector3(cs * 0.78f, 0.34f, cs * 0.76f), new Color("312E81"), new Color("C4B5FD"), new Vector3(0.0f, 0.24f, 0.0f));
            builder.AddInteriorTray(builder.Root, "SplitterInfeed", new Vector3(cs * 0.44f, 0.08f, cs * 0.16f), new Color("4338CA"), new Color("E9D5FF"), new Vector3(-cs * 0.28f, 0.16f, 0.0f));
            builder.AddInteriorTray(builder.Root, "SplitterNorth", new Vector3(cs * 0.20f, 0.08f, cs * 0.30f), new Color("6366F1"), new Color("DDD6FE"), new Vector3(cs * 0.18f, 0.16f, -cs * 0.18f));
            builder.AddInteriorTray(builder.Root, "SplitterSouth", new Vector3(cs * 0.20f, 0.08f, cs * 0.30f), new Color("6366F1"), new Color("DDD6FE"), new Vector3(cs * 0.18f, 0.16f, cs * 0.18f));
            builder.AddInteriorIndicatorLight(builder.Root, "SplitterLamp", new Color("A5B4FC"), new Vector3(0.0f, 0.46f, 0.0f), cs * 0.08f);
            return;
        }

        builder.AddBox("Body", new Vector3(cs * 0.86f, 0.24f, cs * 0.86f), new Color("8B5CF6"), new Vector3(0.0f, 0.12f, 0.0f));
        builder.AddBox("InputStem", new Vector3(cs * 0.42f, 0.10f, cs * 0.18f), new Color("C4B5FD"), new Vector3(-cs * 0.28f, 0.2f, 0.0f));
        builder.AddBox("TopStem", new Vector3(cs * 0.22f, 0.10f, cs * 0.34f), new Color("DDD6FE"), new Vector3(cs * 0.18f, 0.2f, -cs * 0.18f));
        builder.AddBox("BottomStem", new Vector3(cs * 0.22f, 0.10f, cs * 0.34f), new Color("DDD6FE"), new Vector3(cs * 0.18f, 0.2f, cs * 0.18f));
    }
}
