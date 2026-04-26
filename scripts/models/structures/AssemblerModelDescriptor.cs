using Godot;
using NetFactory.Models;

public static class AssemblerModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;

        if (siteKind == FactorySiteKind.Interior)
        {
            var cabinBaseSize = new Vector3(cs * 2.82f, 0.16f, cs * 1.82f);
            builder.AddBox("Base", cabinBaseSize, new Color("0F172A"), new Vector3(0.0f, 0.08f, 0.0f));
            builder.AddInteriorModuleShell(builder.Root, "AssemblerCabin", new Vector3(cs * 2.18f, 0.78f, cs * 1.28f), new Color("1E293B"), new Color("475569"), new Vector3(0.0f, 0.58f, 0.0f));
            builder.AddBox("LeftDrawer", new Vector3(cs * 0.36f, 0.54f, cs * 1.04f), new Color("334155"), new Vector3(-cs * 0.88f, 0.48f, 0.0f));
            builder.AddBox("RightDrawer", new Vector3(cs * 0.36f, 0.54f, cs * 1.04f), new Color("334155"), new Vector3(cs * 0.88f, 0.48f, 0.0f));
            builder.AddBox("Armature", new Vector3(cs * 1.08f, 0.12f, cs * 0.16f), new Color("67E8F9"), new Vector3(0.0f, 0.92f, 0.0f));
            builder.AddBox("ArmColumn", new Vector3(cs * 0.16f, 0.42f, cs * 0.16f), new Color("94A3B8"), new Vector3(0.0f, 0.72f, 0.0f));
            builder.AddBox("ToolHead", new Vector3(cs * 0.22f, 0.12f, cs * 0.28f), new Color("38BDF8"), new Vector3(0.0f, 0.82f, cs * 0.14f));
            builder.AddBox("SignalLamp", new Vector3(cs * 0.14f, 0.14f, cs * 0.14f), new Color("86EFAC"), new Vector3(cs * 0.96f, 1.04f, 0.0f));
            return;
        }

        var baseSize = new Vector3(cs * 2.82f, 0.18f, cs * 1.82f);
        builder.AddBox("Base", baseSize, new Color("1F2937"), new Vector3(0.0f, 0.09f, 0.0f));
        builder.AddBox("Deck", new Vector3(cs * 2.58f, 0.12f, cs * 1.58f), new Color("0F172A"), new Vector3(0.0f, 0.20f, 0.0f));
        builder.AddBox("Body", new Vector3(cs * 2.22f, 0.86f, cs * 1.28f), new Color("334155"), new Vector3(0.0f, 0.61f, 0.0f));
        builder.AddBox("LeftBay", new Vector3(cs * 0.44f, 0.70f, cs * 1.12f), new Color("475569"), new Vector3(-cs * 0.86f, 0.54f, 0.0f));
        builder.AddBox("RightBay", new Vector3(cs * 0.44f, 0.70f, cs * 1.12f), new Color("475569"), new Vector3(cs * 0.86f, 0.54f, 0.0f));
        builder.AddBox("Armature", new Vector3(cs * 1.22f, 0.14f, cs * 0.16f), new Color("67E8F9"), new Vector3(0.0f, 1.02f, 0.0f));
        builder.AddBox("ArmColumn", new Vector3(cs * 0.18f, 0.48f, cs * 0.18f), new Color("94A3B8"), new Vector3(0.0f, 0.82f, 0.0f));
        builder.AddBox("ToolHead", new Vector3(cs * 0.26f, 0.14f, cs * 0.34f), new Color("38BDF8"), new Vector3(0.0f, 0.88f, cs * 0.18f));
        builder.AddBox("SignalLamp", new Vector3(cs * 0.16f, 0.16f, cs * 0.16f), new Color("86EFAC"), new Vector3(cs * 1.02f, 1.18f, 0.0f));
    }
}
