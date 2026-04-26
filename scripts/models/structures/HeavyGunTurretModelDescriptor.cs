using Godot;
using NetFactory.Models;

public static class HeavyGunTurretModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        builder.AddDisc("RangeIndicator",
            FactoryConstants.HeavyGunTurretRange,
            0.03f,
            new Color(0.96f, 0.88f, 0.70f, 0.16f),
            new Vector3(0.0f, 0.02f, 0.0f));

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("Base", new Vector3(cs * 1.84f, 0.18f, cs * 1.84f), new Color("111827"), new Vector3(0.0f, 0.09f, 0.0f));
            builder.AddBox("Well", new Vector3(cs * 1.24f, 0.24f, cs * 1.24f), new Color("1E293B"), new Vector3(0.0f, 0.20f, 0.0f));

            var headPivot = builder.AddPivotNode("HeadPivot", new Vector3(0.0f, 0.54f, 0.0f));

            builder.AddArmBox(headPivot, "TurretBody", new Vector3(cs * 0.88f, 0.46f, cs * 0.74f), new Color("64748B"), new Vector3(0.0f, 0.20f, 0.0f));
            builder.AddArmBox(headPivot, "Barrel", new Vector3(cs * 0.96f, 0.18f, 0.22f), new Color("CBD5E1"), new Vector3(cs * 0.42f, 0.20f, 0.0f));
            builder.AddArmBox(headPivot, "CounterWeight", new Vector3(cs * 0.22f, 0.24f, cs * 0.42f), new Color("475569"), new Vector3(-cs * 0.36f, 0.18f, 0.0f));

            var muzzlePoint = builder.AddPivotNode(headPivot, "MuzzlePoint", new Vector3(cs * 0.86f, 0.20f, 0.0f));
            var muzzleFlash = builder.AddArmBox(muzzlePoint, "MuzzleFlash", new Vector3(0.22f, 0.22f, 0.22f), new Color("FDE68A"), Vector3.Zero);
            muzzleFlash.Visible = false;

            builder.AddBox("AmmoIndicator", new Vector3(cs * 0.20f, 0.24f, cs * 0.20f), new Color("F59E0B"), new Vector3(-cs * 0.52f, 0.90f, 0.0f));
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 1.84f, 0.28f, cs * 1.84f), new Color("111827"), new Vector3(0.0f, 0.14f, 0.0f));
        builder.AddBox("Plinth", new Vector3(cs * 1.34f, 0.38f, cs * 1.34f), new Color("334155"), new Vector3(0.0f, 0.34f, 0.0f));

        var headPivotW = builder.AddPivotNode("HeadPivot", new Vector3(0.0f, 0.74f, 0.0f));

        builder.AddArmBox(headPivotW, "TurretBody", new Vector3(cs * 0.96f, 0.56f, cs * 0.82f), new Color("64748B"), new Vector3(0.0f, 0.22f, 0.0f));
        builder.AddArmBox(headPivotW, "Barrel", new Vector3(cs * 1.08f, 0.22f, 0.28f), new Color("CBD5E1"), new Vector3(cs * 0.46f, 0.22f, 0.0f));
        builder.AddArmBox(headPivotW, "CounterWeight", new Vector3(cs * 0.28f, 0.30f, 0.48f), new Color("475569"), new Vector3(-cs * 0.42f, 0.18f, 0.0f));

        var muzzlePointW = builder.AddPivotNode(headPivotW, "MuzzlePoint", new Vector3(cs * 0.96f, 0.22f, 0.0f));
        var muzzleFlashW = builder.AddArmBox(muzzlePointW, "MuzzleFlash", new Vector3(0.24f, 0.24f, 0.24f), new Color("FDE68A"), Vector3.Zero);
        muzzleFlashW.Visible = false;

        builder.AddBox("AmmoIndicator", new Vector3(cs * 0.24f, 0.28f, cs * 0.24f), new Color("F59E0B"), new Vector3(-cs * 0.62f, 1.18f, 0.0f));
    }
}
