using Godot;
using NetFactory.Models;

public static class GunTurretModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        builder.AddDisc("RangeIndicator",
            FactoryConstants.GunTurretRange,
            0.03f,
            new Color(0.86f, 0.91f, 1.0f, 0.16f),
            new Vector3(0.0f, 0.02f, 0.0f));

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("Base", new Vector3(cs * 0.88f, 0.18f, cs * 0.88f), new Color("111827"), new Vector3(0.0f, 0.09f, 0.0f));
            builder.AddBox("Well", new Vector3(cs * 0.62f, 0.20f, cs * 0.62f), new Color("1F2937"), new Vector3(0.0f, 0.18f, 0.0f));

            var headPivot = builder.AddPivotNode("HeadPivot", new Vector3(0.0f, 0.42f, 0.0f));

            builder.AddArmBox(headPivot, "TurretBody", new Vector3(cs * 0.46f, 0.28f, cs * 0.46f), new Color("64748B"), new Vector3(0.0f, 0.14f, 0.0f));
            builder.AddArmBox(headPivot, "Barrel", new Vector3(cs * 0.54f, 0.12f, 0.14f), new Color("CBD5E1"), new Vector3(cs * 0.24f, 0.14f, 0.0f));
            builder.AddArmBox(headPivot, "Shield", new Vector3(cs * 0.32f, 0.10f, cs * 0.30f), new Color("94A3B8"), new Vector3(0.0f, 0.04f, 0.0f));

            var muzzlePoint = builder.AddPivotNode(headPivot, "MuzzlePoint", new Vector3(cs * 0.46f, 0.14f, 0.0f));
            var muzzleFlash = builder.AddArmBox(muzzlePoint, "MuzzleFlash", new Vector3(0.14f, 0.14f, 0.14f), new Color("FDE68A"), Vector3.Zero);
            muzzleFlash.Visible = false;

            builder.AddBox("AmmoIndicator", new Vector3(cs * 0.14f, 0.18f, cs * 0.14f), new Color("FACC15"), new Vector3(-cs * 0.22f, 0.54f, 0.0f));
            return;
        }

        builder.AddBox("Base", new Vector3(cs * 0.82f, 0.24f, cs * 0.82f), new Color("1F2937"), new Vector3(0.0f, 0.12f, 0.0f));

        var headPivotW = builder.AddPivotNode("HeadPivot", new Vector3(0.0f, 0.56f, 0.0f));

        builder.AddArmBox(headPivotW, "Pivot", new Vector3(cs * 0.32f, 0.58f, cs * 0.32f), new Color("64748B"), new Vector3(0.0f, 0.22f, 0.0f));
        builder.AddArmBox(headPivotW, "Barrel", new Vector3(cs * 0.62f, 0.18f, 0.20f), new Color("CBD5E1"), new Vector3(cs * 0.22f, 0.22f, 0.0f));
        builder.AddArmBox(headPivotW, "TopPlate", new Vector3(cs * 0.38f, 0.12f, 0.34f), new Color("94A3B8"), new Vector3(0.0f, 0.08f, 0.0f));

        var muzzlePointW = builder.AddPivotNode(headPivotW, "MuzzlePoint", new Vector3(cs * 0.53f, 0.22f, 0.0f));
        var muzzleFlashW = builder.AddArmBox(muzzlePointW, "MuzzleFlash", new Vector3(0.18f, 0.18f, 0.18f), new Color("FDE68A"), Vector3.Zero);
        muzzleFlashW.Visible = false;

        builder.AddBox("AmmoIndicator", new Vector3(cs * 0.18f, 0.22f, cs * 0.18f), new Color("FACC15"), new Vector3(-cs * 0.24f, 0.78f, 0.0f));
    }
}
