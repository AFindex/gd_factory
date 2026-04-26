using Godot;
using NetFactory.Models;

public static class ProducerModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        builder.AddBox("Base", new Vector3(cs * 0.9f, 0.8f, cs * 0.9f), new Color("6D8B74"), new Vector3(0.0f, 0.4f, 0.0f));
        builder.AddBox("Tower", new Vector3(cs * 0.45f, 1.4f, cs * 0.45f), new Color("9DC08B"), new Vector3(-0.15f, 1.1f, 0.0f));
        builder.AddBox("Outlet", new Vector3(cs * 0.35f, 0.2f, cs * 0.35f), new Color("D7FFC2"), new Vector3(cs * 0.45f, 0.75f, 0.0f));
    }
}
