using Godot;
using NetFactory;
using NetFactory.Models;

public static class InteriorAccentDescriptor
{
    public static void BuildAccents(IModelBuilder builder, FactorySiteKind siteKind, FactoryStructure structure)
    {
        if (siteKind != FactorySiteKind.Interior)
        {
            return;
        }

        var cellSize = builder.CellSize;
        var previewSize = structure.ResolvedFootprint.GetPreviewSize(cellSize, structure.Facing);
        var deckWidth = Mathf.Max(cellSize * 0.96f, previewSize.X + (cellSize * 0.14f));
        var deckDepth = Mathf.Max(cellSize * 0.96f, previewSize.Y + (cellSize * 0.14f));
        var style = structure.ResolveInteriorVisualStyle();

        builder.AddBox("MaintenanceDeck",
            new Vector3(deckWidth, 0.07f, deckDepth),
            style.DeckColor,
            new Vector3(0.0f, 0.035f, 0.0f));
        builder.AddBox("MaintenanceTrim",
            new Vector3(deckWidth * 0.92f, 0.02f, deckDepth * 0.92f),
            style.TrimColor,
            new Vector3(0.0f, 0.075f, 0.0f));
        builder.AddLabelPlate("CabinLabel",
            structure.GetInteriorPresentationLabel(),
            style.LabelColor,
            new Vector3(0.0f, 0.11f, -deckDepth * 0.28f),
            cellSize,
            Mathf.Clamp(previewSize.X / cellSize, 1.0f, 3.0f));

        if (style.UsesHardpointRing)
        {
            builder.AddBox("HardpointRingOuter",
                new Vector3(deckWidth * 0.88f, 0.02f, deckDepth * 0.88f),
                style.AccentColor.Darkened(0.24f),
                new Vector3(0.0f, 0.094f, 0.0f));
            builder.AddBox("HardpointRingInner",
                new Vector3(deckWidth * 0.54f, 0.025f, deckDepth * 0.54f),
                style.AccentColor,
                new Vector3(0.0f, 0.108f, 0.0f));
        }

        if (style.UsesChannel)
        {
            builder.AddBox("CargoChannel",
                new Vector3(deckWidth * 0.88f, 0.03f, Mathf.Max(cellSize * 0.16f, deckDepth * 0.24f)),
                style.AccentColor.Darkened(0.18f),
                new Vector3(0.0f, 0.095f, 0.0f));
            builder.AddBox("CargoChannelStripe",
                new Vector3(deckWidth * 0.64f, 0.01f, Mathf.Max(cellSize * 0.05f, deckDepth * 0.08f)),
                style.AccentColor.Lightened(0.24f),
                new Vector3(0.0f, 0.118f, 0.0f));
            builder.AddBox("CargoChannelLeftRail",
                new Vector3(deckWidth * 0.86f, 0.03f, Mathf.Max(cellSize * 0.03f, deckDepth * 0.05f)),
                style.LabelColor,
                new Vector3(0.0f, 0.108f, -Mathf.Max(cellSize * 0.14f, deckDepth * 0.12f)));
            builder.AddBox("CargoChannelRightRail",
                new Vector3(deckWidth * 0.86f, 0.03f, Mathf.Max(cellSize * 0.03f, deckDepth * 0.05f)),
                style.LabelColor,
                new Vector3(0.0f, 0.108f, Mathf.Max(cellSize * 0.14f, deckDepth * 0.12f)));
        }

        if (style.UsesServiceWalkway)
        {
            builder.AddBox("MaintenanceWalkway",
                new Vector3(deckWidth * 0.86f, 0.02f, Mathf.Max(cellSize * 0.22f, deckDepth * 0.24f)),
                style.TrimColor.Lightened(0.08f),
                new Vector3(0.0f, 0.085f, (deckDepth * 0.5f) - Mathf.Max(cellSize * 0.12f, deckDepth * 0.12f)));
        }

        builder.AddBox("AccessPanel",
            new Vector3(Mathf.Max(cellSize * 0.18f, deckWidth * 0.14f), 0.10f, Mathf.Max(cellSize * 0.20f, deckDepth * 0.18f)),
            style.LabelColor,
            new Vector3((deckWidth * 0.5f) - Mathf.Max(cellSize * 0.14f, deckWidth * 0.10f), 0.13f, 0.0f));
        builder.AddIndicatorLight("StatusLamp",
            style.AccentColor.Lightened(0.28f),
            new Vector3((-deckWidth * 0.5f) + Mathf.Max(cellSize * 0.12f, deckWidth * 0.10f), 0.14f, 0.0f),
            cellSize * 0.06f);
    }
}
