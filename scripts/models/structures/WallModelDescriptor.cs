using Godot;
using NetFactory.Models;

public static class WallModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("Base", new Vector3(cs * 0.94f, 0.32f, cs * 0.94f), new Color("374151"), new Vector3(0.0f, 0.16f, 0.0f));
            builder.AddBox("WallBody", new Vector3(cs * 0.78f, 1.26f, cs * 0.42f), new Color("9CA3AF"), new Vector3(0.0f, 0.82f, 0.0f));
            builder.AddBox("TopCap", new Vector3(cs * 0.88f, 0.14f, cs * 0.52f), new Color("E5E7EB"), new Vector3(0.0f, 1.48f, 0.0f));
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 0.94f, 0.32f, cs * 0.94f), new Color("374151"), new Vector3(0.0f, 0.16f, 0.0f));
        builder.AddBox("WallBody", new Vector3(cs * 0.78f, 1.26f, cs * 0.42f), new Color("9CA3AF"), new Vector3(0.0f, 0.82f, 0.0f));
        builder.AddBox("TopCap", new Vector3(cs * 0.88f, 0.14f, cs * 0.52f), new Color("E5E7EB"), new Vector3(0.0f, 1.48f, 0.0f));
    }
}
