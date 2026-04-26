using Godot;
using NetFactory.Models;

public static class DebugPowerModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        builder.AddDisc("PowerRange", cs * 6f, 0.03f, new Color(0.99f, 0.88f, 0.42f, 0.12f), new Vector3(0.0f, 0.02f, 0.0f));

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddInteriorModuleShell(builder.Root, "DebugPower", new Vector3(cs * 0.68f, 0.86f, cs * 0.68f), new Color("4C3B12"), new Color("FBBF24"), new Vector3(0.0f, 0.44f, 0.0f));

            var interiorRotorRig = builder.AddPivotNode("DebugPowerRotorRig", new Vector3(0.0f, 0.86f, 0.0f));
            builder.AddBox(interiorRotorRig, "RotorBladeNorth", new Vector3(cs * 0.12f, 0.12f, cs * 0.54f), new Color("FCD34D"), Vector3.Zero);
            builder.AddBox(interiorRotorRig, "RotorBladeEast", new Vector3(cs * 0.54f, 0.12f, cs * 0.12f), new Color("FDE68A"), Vector3.Zero);

            builder.AddBox("PowerCore", new Vector3(cs * 0.26f, 0.38f, cs * 0.26f), new Color("F59E0B"), new Vector3(0.0f, 0.54f, 0.0f));
            builder.AddBox("PowerLamp", new Vector3(cs * 0.14f, cs * 0.14f, cs * 0.14f), new Color("FEF3C7"), new Vector3(0.0f, 1.08f, 0.0f));
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 0.90f, 0.22f, cs * 0.90f), new Color("5B4420"), new Vector3(0.0f, 0.11f, 0.0f));
        builder.AddBox("GeneratorBody", new Vector3(cs * 0.62f, 0.92f, cs * 0.62f), new Color("D97706"), new Vector3(0.0f, 0.58f, 0.0f));

        var worldRotorRig = builder.AddPivotNode("DebugPowerRotorRig", new Vector3(0.0f, 1.18f, 0.0f));
        builder.AddBox(worldRotorRig, "RotorBladeNorth", new Vector3(cs * 0.12f, 0.12f, cs * 0.62f), new Color("FCD34D"), Vector3.Zero);
        builder.AddBox(worldRotorRig, "RotorBladeEast", new Vector3(cs * 0.62f, 0.12f, cs * 0.12f), new Color("FDE68A"), Vector3.Zero);

        builder.AddBox("GeneratorCore", new Vector3(cs * 0.28f, 0.42f, cs * 0.28f), new Color("FDBA74"), new Vector3(0.0f, 0.76f, 0.0f));
        builder.AddBox("PowerLamp", new Vector3(cs * 0.16f, cs * 0.16f, cs * 0.16f), new Color("FEF3C7"), new Vector3(0.0f, 1.52f, 0.0f));
    }
}
