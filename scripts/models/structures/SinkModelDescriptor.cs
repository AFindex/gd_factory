using Godot;
using NetFactory.Models;

public static class SinkModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        builder.AddBox("Base", new Vector3(cs * 0.95f, 0.7f, cs * 0.95f), new Color("334155"), new Vector3(0.0f, 0.35f, 0.0f));
        builder.AddBox("Bin", new Vector3(cs * 0.72f, 1.1f, cs * 0.72f), new Color("94A3B8"), new Vector3(0.0f, 1.0f, 0.0f));
        builder.AddBox("Beacon", new Vector3(cs * 0.25f, 0.25f, cs * 0.25f), new Color("FDE68A"), new Vector3(-cs * 0.3f, 1.55f, 0.0f));
    }
}
