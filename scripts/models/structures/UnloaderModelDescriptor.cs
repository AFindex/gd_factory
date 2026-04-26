using Godot;
using NetFactory.Models;

public static class UnloaderModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        builder.AddBox("Base", new Vector3(cs * 0.88f, 0.18f, cs * 0.88f), new Color("2563EB"), new Vector3(0.0f, 0.09f, 0.0f));
        builder.AddBox("BackHousing", new Vector3(cs * 0.36f, 0.34f, cs * 0.60f), new Color("1D4ED8"), new Vector3(-cs * 0.22f, 0.26f, 0.0f));
        builder.AddBox("FeedBed", new Vector3(cs * 0.56f, 0.10f, cs * 0.26f), new Color("93C5FD"), new Vector3(0.02f, 0.22f, 0.0f));
        builder.AddBox("FrontNozzle", new Vector3(cs * 0.18f, 0.18f, cs * 0.22f), new Color("DBEAFE"), new Vector3(cs * 0.34f, 0.28f, 0.0f));
        builder.AddBox("DirectionMark", new Vector3(cs * 0.18f, 0.05f, cs * 0.12f), new Color("EFF6FF"), new Vector3(cs * 0.22f, 0.40f, 0.0f));
    }
}
