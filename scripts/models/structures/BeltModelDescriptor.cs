using Godot;
using NetFactory.Models;

public static class BeltModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            const float interiorTrackWidthRatio = 0.4666667f;
            const float interiorTrackArmLengthRatio = 0.56f;
            const float interiorTrackCenterRunRatio = 0.40f;
            const float interiorTrackCapRunRatio = 0.28f;

            builder.AddBox("CabinChannelCore",
                new Vector3(cs * interiorTrackCenterRunRatio, 0.08f, cs * interiorTrackWidthRatio),
                new Color("0F172A"),
                new Vector3(0.0f, 0.12f, 0.0f));
            builder.AddBox("CabinInputTray",
                new Vector3(cs * interiorTrackArmLengthRatio, 0.10f, cs * interiorTrackWidthRatio),
                new Color("1D4ED8"),
                Vector3.Zero);
            builder.AddBox("CabinOutputTray",
                new Vector3(cs * interiorTrackArmLengthRatio, 0.10f, cs * interiorTrackWidthRatio),
                new Color("2563EB"),
                Vector3.Zero);
            builder.AddBox("CabinDirectionStrip",
                new Vector3(cs * 0.20f, 0.03f, cs * 0.18f),
                new Color("BAE6FD"),
                new Vector3(0.26f * cs, 0.18f, 0.0f));
            builder.AddBox("CabinTrayCap",
                new Vector3(cs * interiorTrackCapRunRatio, 0.05f, cs * interiorTrackCapRunRatio),
                new Color("CBD5E1"),
                new Vector3(0.0f, 0.18f, 0.0f));
            return;
        }

        builder.AddBox("Center",
            new Vector3(cs * 0.42f, 0.12f, cs * 0.42f),
            new Color("4B5563"),
            new Vector3(0.0f, 0.08f, 0.0f));
        builder.AddBox("InputArm",
            new Vector3(cs * 0.55f, 0.12f, cs * 0.22f),
            new Color("4B5563"),
            Vector3.Zero);
        builder.AddBox("OutputArm",
            new Vector3(cs * 0.55f, 0.12f, cs * 0.22f),
            new Color("4B5563"),
            Vector3.Zero);
        builder.AddBox("Arrow",
            new Vector3(cs * 0.22f, 0.05f, cs * 0.18f),
            new Color("7DD3FC"),
            new Vector3(0.26f * cs, 0.16f, 0.0f));
    }
}
