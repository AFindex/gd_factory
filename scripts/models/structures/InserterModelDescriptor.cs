using Godot;
using NetFactory.Models;

public static class InserterModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("Base", new Vector3(cs * 0.82f, 0.12f, cs * 0.82f), new Color("0F172A"), new Vector3(0.0f, 0.06f, 0.0f));
            builder.AddInteriorTray(builder.Root, "InputTray", new Vector3(cs * 0.30f, 0.08f, cs * 0.18f), new Color("1D4ED8"), new Color("DBEAFE"), new Vector3(-cs * 0.26f, 0.12f, 0.0f));
            builder.AddInteriorTray(builder.Root, "OutputTray", new Vector3(cs * 0.30f, 0.08f, cs * 0.18f), new Color("0F766E"), new Color("CCFBF1"), new Vector3(cs * 0.26f, 0.12f, 0.0f));
            builder.AddBox("Column", new Vector3(cs * 0.16f, 0.52f, cs * 0.16f), new Color("475569"), new Vector3(0.0f, 0.38f, 0.0f));
            builder.AddBox("ServiceCap", new Vector3(cs * 0.26f, 0.10f, cs * 0.26f), new Color("CBD5E1"), new Vector3(0.0f, 0.66f, 0.0f));

            var shoulderPivot = builder.AddPivotNode("ShoulderPivot", new Vector3(0.0f, 0.68f, 0.0f));

            builder.AddArmBox(shoulderPivot, "UpperArm", new Vector3(cs * 0.28f, 0.07f, 0.09f), new Color("94A3B8"), new Vector3(cs * 0.14f, 0.0f, 0.0f));

            var elbowPivot = builder.AddPivotNode(shoulderPivot, "ElbowPivot", new Vector3(cs * 0.28f, 0.0f, 0.0f));

            builder.AddArmBox(elbowPivot, "Forearm", new Vector3(cs * 0.24f, 0.07f, 0.08f), new Color("38BDF8"), new Vector3(cs * 0.12f, 0.0f, 0.0f));

            builder.AddArmBox(elbowPivot, "Claw", new Vector3(cs * 0.10f, 0.10f, 0.18f), new Color("F8FAFC"), new Vector3(cs * 0.24f, 0.0f, 0.0f));

            builder.AddPivotNode(elbowPivot, "HeldItemAnchor", new Vector3(cs * 0.24f, cs * 0.09f, 0.0f));

            builder.AddInteriorIndicatorLight(builder.Root, "ServiceLamp", new Color("67E8F9"), new Vector3(0.0f, 0.78f, 0.0f), cs * 0.07f);
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 0.72f, 0.16f, cs * 0.72f), new Color("78350F"), new Vector3(0.0f, 0.08f, 0.0f));
        builder.AddBox("Column", new Vector3(cs * 0.18f, 0.72f, cs * 0.18f), new Color("A16207"), new Vector3(0.0f, 0.44f, 0.0f));
        builder.AddBox("InputMarker", new Vector3(cs * 0.16f, 0.05f, cs * 0.12f), new Color("FED7AA"), new Vector3(-cs * 0.28f, 0.16f, 0.0f));
        builder.AddBox("OutputMarker", new Vector3(cs * 0.16f, 0.05f, cs * 0.12f), new Color("FEF3C7"), new Vector3(cs * 0.28f, 0.16f, 0.0f));

        var shoulderPivotW = builder.AddPivotNode("ShoulderPivot", new Vector3(0.0f, 0.82f, 0.0f));

        builder.AddArmBox(shoulderPivotW, "UpperArm", new Vector3(cs * 0.34f, 0.08f, 0.10f), new Color("D97706"), new Vector3(cs * 0.17f, 0.0f, 0.0f));

        var elbowPivotW = builder.AddPivotNode(shoulderPivotW, "ElbowPivot", new Vector3(cs * 0.34f, 0.0f, 0.0f));

        builder.AddArmBox(elbowPivotW, "Forearm", new Vector3(cs * 0.30f, 0.08f, 0.09f), new Color("F59E0B"), new Vector3(cs * 0.15f, 0.0f, 0.0f));

        builder.AddArmBox(elbowPivotW, "Claw", new Vector3(cs * 0.12f, 0.12f, 0.22f), new Color("FCD34D"), new Vector3(cs * 0.30f, 0.0f, 0.0f));

        builder.AddPivotNode(elbowPivotW, "HeldItemAnchor", new Vector3(cs * 0.30f, cs * 0.11f, 0.0f));
    }
}
