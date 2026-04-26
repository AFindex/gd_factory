using Godot;
using NetFactory.Models;

public static class BoundaryAttachmentModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;
        var baseColor = new Color("C2410C");
        var accentColor = new Color("EA580C");
        var tipColor = new Color("F97316");

        var deckWidth = Mathf.Max(cs * 1.28f, cs * 1.28f * 0.90f);
        var deckDepth = Mathf.Max(cs * 1.86f, cs * 1.86f * 0.94f);

        builder.AddBox("BoundaryBaseSkid", new Vector3(deckWidth, 0.12f, deckDepth), baseColor, new Vector3(0.0f, 0.06f, 0.0f));
        builder.AddBox("BoundaryDeck", new Vector3(deckWidth * 0.90f, 0.08f, deckDepth * 0.90f), baseColor.Lightened(0.06f), new Vector3(0.02f * cs, 0.12f, 0.0f));
        builder.AddBox("BoundaryHandoffCradle", new Vector3(deckWidth * 0.82f, 0.10f, deckDepth * 0.58f), accentColor.Darkened(0.06f), new Vector3(0.06f * cs, 0.18f, 0.0f));
        builder.AddInteriorTray(builder.Root, "BoundaryTransferLane", new Vector3(deckWidth * 0.84f, 0.08f, cs * 0.34f), accentColor, tipColor.Lightened(0.16f), new Vector3(0.08f * cs, 0.20f, 0.0f));
        builder.AddBox("BoundaryDeckRailNorth", new Vector3(deckWidth * 0.74f, 0.10f, cs * 0.10f), tipColor.Lightened(0.12f), new Vector3(0.06f * cs, 0.24f, 0.0f));
        builder.AddBox("BoundaryPortalNorth", new Vector3(cs * 0.18f, 0.44f, cs * 0.16f), tipColor, new Vector3(deckWidth * 0.40f, 0.32f, 0.0f));
        builder.AddBox("HullMouth", new Vector3(deckWidth * 0.28f, 0.14f, deckDepth * 0.48f), tipColor.Lightened(0.04f), new Vector3(deckWidth * 0.48f, 0.24f, 0.0f));
        builder.AddBox("BoundaryScaleMarker", new Vector3(cs * 0.30f, 0.06f, cs * 0.30f), tipColor.Lightened(0.18f), new Vector3(-cs * 0.40f, 0.14f, 0.0f));
        builder.AddInteriorLabelPlate(builder.Root, "BoundaryScaleLabel", "重载", tipColor, new Vector3(-deckWidth * 0.10f, 0.12f, -deckDepth * 0.38f), 1.18f);
        builder.AddInteriorIndicatorLight(builder.Root, "Beacon", tipColor.Lightened(0.22f), new Vector3(-deckWidth * 0.30f, 0.40f, 0.0f), cs * 0.07f);
    }
}
