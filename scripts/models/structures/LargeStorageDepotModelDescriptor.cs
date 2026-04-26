using Godot;
using NetFactory.Models;

public static class LargeStorageDepotModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        builder.AddBox("Base", new Vector3(cs * 1.86f, 0.24f, cs * 1.86f), new Color("334155"), new Vector3(0.0f, 0.12f, 0.0f));
        builder.AddBox("DepotBody", new Vector3(cs * 1.62f, 1.02f, cs * 1.62f), new Color("475569"), new Vector3(0.0f, 0.76f, 0.0f));
        builder.AddBox("OutputStripe", new Vector3(cs * 0.22f, 0.12f, cs * 0.74f), new Color("FBBF24"), new Vector3(cs * 0.74f, 1.30f, 0.0f));

        for (var index = 0; index < 5; index++)
        {
            builder.AddBox(
                $"Fill_{index}",
                new Vector3(cs * 0.16f, 0.12f, cs * 1.18f),
                new Color("38BDF8"),
                new Vector3(-cs * 0.54f + index * cs * 0.27f, 1.30f, 0.0f));
        }

        builder.AddBox("Beacon", new Vector3(cs * 0.24f, 0.24f, cs * 0.24f), new Color("E2E8F0"), new Vector3(0.0f, 1.64f, 0.0f));
    }
}
