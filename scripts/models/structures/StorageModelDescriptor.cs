using Godot;
using NetFactory.Models;

public static class StorageModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("Base", new Vector3(cs * 0.92f, 0.16f, cs * 0.92f), new Color("0F172A"), new Vector3(0.0f, 0.08f, 0.0f));
            builder.AddInteriorModuleShell(builder.Root, "StorageCabinet", new Vector3(cs * 0.78f, 0.90f, cs * 0.76f), new Color("334155"), new Color("94A3B8"), new Vector3(0.0f, 0.68f, 0.0f));
            builder.AddBox("OutputStripe", new Vector3(cs * 0.14f, 0.08f, cs * 0.38f), new Color("FBBF24"), new Vector3(cs * 0.30f, 1.08f, 0.0f));

            for (var index = 0; index < 4; index++)
            {
                var indicator = builder.AddBox(
                    $"Fill_{index}",
                    new Vector3(cs * 0.10f, 0.10f, cs * 0.42f),
                    new Color("67E8F9"),
                    new Vector3(-cs * 0.18f + index * cs * 0.12f, 1.08f, 0.0f));
                indicator.Visible = false;
            }

            builder.AddBox("Beacon", new Vector3(cs * 0.14f, 0.14f, cs * 0.14f), new Color("E2E8F0"), new Vector3(0.0f, 1.28f, 0.0f));
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 0.92f, 0.24f, cs * 0.92f), new Color("334155"), new Vector3(0.0f, 0.12f, 0.0f));
        builder.AddBox("CrateBody", new Vector3(cs * 0.78f, 0.92f, cs * 0.78f), new Color("64748B"), new Vector3(0.0f, 0.70f, 0.0f));
        builder.AddBox("OutputStripe", new Vector3(cs * 0.14f, 0.10f, cs * 0.42f), new Color("FBBF24"), new Vector3(cs * 0.34f, 1.20f, 0.0f));

        for (var index = 0; index < 4; index++)
        {
            var indicator = builder.AddBox(
                $"Fill_{index}",
                new Vector3(cs * 0.12f, 0.12f, cs * 0.54f),
                new Color("38BDF8"),
                new Vector3(-cs * 0.18f + index * cs * 0.12f, 1.20f, 0.0f));
            indicator.Visible = false;
        }

        builder.AddBox("Beacon", new Vector3(cs * 0.18f, 0.18f, cs * 0.18f), new Color("E2E8F0"), new Vector3(0.0f, 1.42f, 0.0f));
    }
}
