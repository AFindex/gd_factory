using Godot;
using NetFactory.Models;

public static class DebugSourceModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddInteriorModuleShell(builder.Root, "DebugSource", new Vector3(cs * 0.64f, 0.78f, cs * 0.64f), new Color("0C4A6E"), new Color("38BDF8"), new Vector3(0.0f, 0.42f, 0.0f));

            var spinnerRig = builder.AddPivotNode("DebugSpinnerRig", new Vector3(0.0f, 0.84f, 0.0f));
            builder.AddBox(spinnerRig, "DebugSpinnerNorth", new Vector3(cs * 0.10f, 0.14f, cs * 0.42f), new Color("0EA5E9"), Vector3.Zero);
            builder.AddBox(spinnerRig, "DebugSpinnerEast", new Vector3(cs * 0.42f, 0.14f, cs * 0.10f), new Color("7DD3FC"), Vector3.Zero);

            builder.AddInteriorTray(builder.Root, "DebugSourceTray", new Vector3(cs * 0.52f, 0.12f, cs * 0.38f), new Color("0E7490"), new Color("22D3EE"), new Vector3(0.0f, 0.20f, 0.0f));
            builder.AddBox("DebugStatusLamp", new Vector3(cs * 0.12f, cs * 0.12f, cs * 0.12f), new Color("67E8F9"), new Vector3(0.0f, 1.04f, 0.0f));
            return;
        }

        builder.AddBox("DebugFooting", new Vector3(cs * 0.86f, 0.20f, cs * 0.86f), new Color("0C4A6E"), new Vector3(0.0f, 0.10f, 0.0f));
        builder.AddBox("DebugCrate", new Vector3(cs * 0.64f, 0.92f, cs * 0.64f), new Color("0EA5E9"), new Vector3(0.0f, 0.56f, 0.0f));

        var worldSpinnerRig = builder.AddPivotNode("DebugSpinnerRig", new Vector3(0.0f, 1.08f, 0.0f));
        builder.AddBox(worldSpinnerRig, "DebugSpinnerNorth", new Vector3(cs * 0.12f, 0.14f, cs * 0.52f), new Color("7DD3FC"), Vector3.Zero);
        builder.AddBox(worldSpinnerRig, "DebugSpinnerEast", new Vector3(cs * 0.52f, 0.14f, cs * 0.12f), new Color("0C4A6E"), Vector3.Zero);

        builder.AddBox("DebugOutlet", new Vector3(cs * 0.22f, 0.28f, cs * 0.22f), new Color("67E8F9"), new Vector3(cs * 0.34f, 0.66f, 0.0f));
        builder.AddBox("DebugStatusLamp", new Vector3(cs * 0.14f, cs * 0.14f, cs * 0.14f), new Color("67E8F9"), new Vector3(0.0f, 1.38f, 0.0f));
    }
}
