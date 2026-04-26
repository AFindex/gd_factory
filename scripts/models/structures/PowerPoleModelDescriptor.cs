using Godot;
using NetFactory.Models;

public static class PowerPoleModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            BuildInterior(builder, cs);
            return;
        }

        BuildWorld(builder, cs);
    }

    private static void BuildInterior(IModelBuilder builder, float cs)
    {
        builder.AddDisc("PowerRange",
            cs * 6,
            0.03f,
            new Color(0.99f, 0.88f, 0.42f, 0.12f),
            new Vector3(0.0f, 0.02f, 0.0f));
        builder.AddBox("Base", new Vector3(cs * 0.44f, 0.14f, cs * 0.44f), new Color("1C1917"), new Vector3(0.0f, 0.07f, 0.0f));
        builder.AddBox("BusRoot", new Vector3(cs * 0.26f, 0.24f, cs * 0.26f), new Color("57534E"), new Vector3(0.0f, 0.20f, 0.0f));

        var pole = builder.AddBox("Pole", new Vector3(cs * 0.12f, 0.92f, cs * 0.12f), new Color("FBBF24"), new Vector3(0.0f, 0.46f, 0.0f));
        pole.Scale = new Vector3(1.0f, 0.02f, 1.0f);
        pole.Position = new Vector3(0.0f, 0.03f, 0.0f);

        var crossbar = builder.AddBox("Crossbar", new Vector3(cs * 0.56f, 0.10f, cs * 0.10f), new Color("FDE68A"), new Vector3(0.0f, 0.92f, 0.0f));
        crossbar.Scale = new Vector3(0.08f, 1.0f, 1.0f);
        crossbar.Position = new Vector3(0.0f, 0.74f, 0.0f);

        builder.AddBox("BusNorth", new Vector3(cs * 0.10f, 0.10f, cs * 0.24f), new Color("FCD34D"), new Vector3(0.0f, 0.62f, -cs * 0.20f));
        builder.AddBox("BusSouth", new Vector3(cs * 0.10f, 0.10f, cs * 0.24f), new Color("FCD34D"), new Vector3(0.0f, 0.62f, cs * 0.20f));

        var lamp = builder.AddBox("Lamp", new Vector3(cs * 0.12f, 0.12f, cs * 0.12f), new Color("FEF08A"), new Vector3(0.0f, 1.08f, 0.0f));
        lamp.Scale = Vector3.One * 0.65f;
        lamp.Position = new Vector3(0.0f, 0.82f, 0.0f);
    }

    private static void BuildWorld(IModelBuilder builder, float cs)
    {
        builder.AddDisc("PowerRange",
            cs * 6,
            0.03f,
            new Color(0.99f, 0.88f, 0.42f, 0.12f),
            new Vector3(0.0f, 0.02f, 0.0f));
        builder.AddBox("Footing", new Vector3(cs * 0.32f, 0.12f, cs * 0.32f), new Color("475569"), new Vector3(0.0f, 0.06f, 0.0f));
        builder.AddBox("SupportBase", new Vector3(cs * 0.22f, 0.18f, cs * 0.22f), new Color("78716C"), new Vector3(0.0f, 0.18f, 0.0f));

        var pole = builder.AddBox("Pole", new Vector3(cs * 0.12f, 1.48f, cs * 0.12f), new Color("A16207"), new Vector3(0.0f, 0.74f, 0.0f));
        pole.Scale = new Vector3(1.0f, 0.02f, 1.0f);
        pole.Position = new Vector3(0.0f, 0.03f, 0.0f);

        builder.AddBox("BraceNorth", new Vector3(cs * 0.06f, 0.46f, cs * 0.06f), new Color("B45309"), new Vector3(0.11f, 0.42f, 0.08f));
        builder.AddBox("BraceSouth", new Vector3(cs * 0.06f, 0.46f, cs * 0.06f), new Color("B45309"), new Vector3(-0.11f, 0.42f, -0.08f));

        var crossbar = builder.AddBox("Crossbar", new Vector3(cs * 0.62f, 0.10f, cs * 0.10f), new Color("FDE68A"), new Vector3(0.0f, 1.42f, 0.0f));
        crossbar.Scale = new Vector3(0.08f, 1.0f, 1.0f);
        crossbar.Position = new Vector3(0.0f, 1.06f, 0.0f);

        var lamp = builder.AddBox("Lamp", new Vector3(cs * 0.12f, 0.12f, cs * 0.12f), new Color("FEF08A"), new Vector3(0.0f, 1.62f, 0.0f));
        lamp.Scale = Vector3.One * 0.65f;
        lamp.Position = new Vector3(0.0f, 1.14f, 0.0f);
    }
}
