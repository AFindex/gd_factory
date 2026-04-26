using Godot;
using NetFactory.Models;

public static class CargoConversionModelDescriptor
{
    public static void BuildUnpackerModel(IModelBuilder builder, FactorySiteKind siteKind, float footprintPreviewX, float footprintPreviewY)
    {
        var cs = builder.CellSize;
        var root = builder.Root;

        if (siteKind == FactorySiteKind.Interior)
        {
            var deckWidth = Mathf.Max(cs * 1.56f, footprintPreviewX * 0.92f);
            var deckDepth = Mathf.Max(cs * 1.58f, footprintPreviewY * 0.92f);
            var intakeRailHeight = 0.38f;

            CreateOpenHeavyChamber(builder,
                "UnpackerChamber",
                new Vector3(deckWidth, 0.14f, deckDepth),
                frameHeight: 0.86f,
                chamberDepth: deckDepth,
                frameColor: new Color("12324A"),
                accentColor: new Color("7DD3FC"));

            builder.AddBox("UnpackerMouthFrame", new Vector3(Mathf.Max(cs * 0.18f, deckWidth * 0.12f), 0.48f, deckDepth * 0.74f), new Color("C7EAFE"), new Vector3(-deckWidth * 0.38f, 0.30f, 0.0f));
            builder.AddBox("UnpackerCradle", new Vector3(deckWidth * 0.66f, 0.12f, deckDepth * 0.52f), new Color("0EA5E9"), new Vector3(0.0f, 0.18f, 0.0f));
            builder.AddBox("UnpackerGuideCenter", new Vector3(deckWidth * 0.62f, 0.06f, cs * 0.10f), new Color("DBEAFE"), new Vector3(0.0f, 0.26f, 0.0f));
            builder.AddBox("UnpackerInfeedRail", new Vector3(deckWidth * 0.48f, 0.05f, cs * 0.08f), new Color("E0F2FE"), new Vector3(-deckWidth * 0.22f, intakeRailHeight - 0.08f, 0.0f));
            builder.AddInteriorTray(root, "UnpackerOutfeed", new Vector3(deckWidth * 0.34f, 0.10f, cs * 0.22f), new Color("0B5A88"), new Color("DBEAFE"), new Vector3(deckWidth * 0.36f, 0.16f, 0.0f));
            builder.AddBox("UnpackerClampNorth", new Vector3(deckWidth * 0.48f, 0.06f, cs * 0.10f), new Color("E0F2FE"), new Vector3(0.0f, 0.60f, -cs * 0.26f));
            builder.AddBox("UnpackerClampSouth", new Vector3(deckWidth * 0.48f, 0.06f, cs * 0.10f), new Color("E0F2FE"), new Vector3(0.0f, 0.60f, cs * 0.26f));

            var unpackerLamp = builder.AddBox("UnpackerLamp", new Vector3(cs * 0.09f, cs * 0.09f, cs * 0.09f), new Color("7DD3FC"), new Vector3(-deckWidth * 0.34f, 0.64f, 0.0f));
            if (unpackerLamp.MaterialOverride is StandardMaterial3D statusLampMaterial)
            {
                statusLampMaterial.Roughness = 0.18f;
                statusLampMaterial.EmissionEnabled = true;
                statusLampMaterial.Emission = new Color("7DD3FC");
                statusLampMaterial.EmissionEnergyMultiplier = 1.2f;
            }

            builder.AddInteriorLabelPlate(root, "UnpackerTier", "重载", new Color("7DD3FC"), new Vector3(-deckWidth * 0.10f, 0.16f, -deckDepth * 0.32f), 1.1f);

            var progressBackground = builder.AddBox(
                "UnpackerProgressBackground",
                new Vector3(cs * 0.62f, 0.03f, 0.08f),
                new Color(0.04f, 0.07f, 0.12f, 0.82f),
                new Vector3(0.0f, 1.04f, 0.0f));
            if (progressBackground.MaterialOverride is StandardMaterial3D progressBackgroundMaterial)
            {
                progressBackgroundMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                progressBackgroundMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            }
            progressBackground.Visible = false;

            var progressFill = builder.AddBox(
                "UnpackerProgressFill",
                new Vector3(cs * 0.58f, 0.02f, 0.06f),
                new Color("7DD3FC"),
                new Vector3(0.0f, 1.04f, 0.0f));
            if (progressFill.MaterialOverride is StandardMaterial3D progressFillMaterial)
            {
                progressFillMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                progressFillMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                progressFillMaterial.EmissionEnabled = true;
                progressFillMaterial.Emission = new Color("7DD3FC");
            }
            progressFill.Visible = false;

            builder.AddPivotNode("StagingPayloadAnchor", new Vector3(-deckWidth * 0.38f, intakeRailHeight, 0.0f));
            builder.AddPivotNode("ProcessingPayloadAnchor", new Vector3(0.0f, intakeRailHeight, 0.0f));
            builder.AddPivotNode("DispatchPayloadAnchor", new Vector3(deckWidth * 0.38f, 0.24f, 0.0f));
            return;
        }

        builder.AddBox("Deck", new Vector3(cs * 1.2f, 0.16f, cs * 1.2f), new Color("164E63"), new Vector3(0.0f, 0.08f, 0.0f));
        builder.AddBox("Chamber", new Vector3(cs * 0.86f, 0.58f, cs * 0.86f), new Color("38BDF8"), new Vector3(0.0f, 0.30f, 0.0f));
        builder.AddBox("FeedRail", new Vector3(cs * 1.24f, 0.08f, cs * 0.18f), new Color("BAE6FD"), new Vector3(0.0f, 0.22f, 0.0f));
    }

    public static void BuildPackerModel(IModelBuilder builder, FactorySiteKind siteKind, float footprintPreviewX, float footprintPreviewY, System.Collections.Generic.IReadOnlyList<Vector2I> inputOffsetsEast)
    {
        var cs = builder.CellSize;
        var root = builder.Root;

        if (siteKind == FactorySiteKind.Interior)
        {
            var deckWidth = Mathf.Max(cs * 1.56f, footprintPreviewX * 0.92f);
            var deckDepth = Mathf.Max(cs * 1.52f, footprintPreviewY * 0.92f);
            var intakeEdgeX = -deckWidth * 0.38f;

            CreateOpenHeavyChamber(builder,
                "PackerChamber",
                new Vector3(deckWidth, 0.14f, deckDepth),
                frameHeight: 0.84f,
                chamberDepth: deckDepth,
                frameColor: new Color("6A240B"),
                accentColor: new Color("FDBA74"));

            for (var inputIndex = 0; inputIndex < inputOffsetsEast.Count; inputIndex++)
            {
                var inputOffset = inputOffsetsEast[inputIndex];
                var portCenter = new Vector3(inputOffset.X * cs, 0.0f, inputOffset.Y * cs);
                var laneCenterX = (portCenter.X + intakeEdgeX) * 0.5f;
                var laneWidth = Mathf.Max(cs * 1.10f, Mathf.Abs(portCenter.X - intakeEdgeX) + cs * 0.34f);
                builder.AddInteriorTray(
                    root,
                    $"PackerInputLane{inputIndex}",
                    new Vector3(laneWidth, 0.10f, cs * 0.22f),
                    new Color("B94A13"),
                    new Color("FED7AA"),
                    new Vector3(laneCenterX, 0.16f, portCenter.Z));
                builder.AddBox(
                    $"PackerInputSocket{inputIndex}",
                    new Vector3(cs * 0.26f, 0.12f, cs * 0.28f),
                    new Color("FDBA74"),
                    new Vector3(portCenter.X, 0.18f, portCenter.Z));
            }

            builder.AddBox("PackerCompressionDeck", new Vector3(deckWidth * 0.66f, 0.12f, deckDepth * 0.54f), new Color("C2410C"), new Vector3(0.0f, 0.18f, 0.0f));
            builder.AddBox("PackerClampNorth", new Vector3(deckWidth * 0.62f, 0.08f, cs * 0.08f), new Color("FED7AA"), new Vector3(0.0f, 0.42f, -cs * 0.30f));
            builder.AddBox("PackerClampSouth", new Vector3(deckWidth * 0.62f, 0.08f, cs * 0.08f), new Color("FED7AA"), new Vector3(0.0f, 0.42f, cs * 0.30f));
            builder.AddBox("PackerRamColumnWest", new Vector3(cs * 0.08f, 0.54f, cs * 0.10f), new Color("FCD7AA"), new Vector3(-cs * 0.18f, 0.44f, 0.0f));
            builder.AddBox("PackerRamColumnEast", new Vector3(cs * 0.08f, 0.54f, cs * 0.10f), new Color("FCD7AA"), new Vector3(cs * 0.18f, 0.44f, 0.0f));
            builder.AddBox("PackerCompressionRam", new Vector3(cs * 0.22f, 0.14f, cs * 0.22f), new Color("FFE4C2"), new Vector3(0.0f, 0.70f, 0.0f));
            builder.AddBox("PackerExportCradle", new Vector3(deckWidth * 0.48f, 0.12f, deckDepth * 0.40f), new Color("FB923C"), new Vector3(deckWidth * 0.36f, 0.18f, 0.0f));
            builder.AddBox("PackerGuideCenter", new Vector3(deckWidth * 0.62f, 0.06f, cs * 0.10f), new Color("FED7AA"), new Vector3(0.0f, 0.26f, 0.0f));
            builder.AddInteriorIndicatorLight(root, "PackerLamp", new Color("FB923C"), new Vector3(deckWidth * 0.34f, 0.64f, 0.0f), cs * 0.09f);
            builder.AddInteriorLabelPlate(root, "PackerTier", "压装", new Color("FB923C"), new Vector3(deckWidth * 0.10f, 0.16f, -deckDepth * 0.32f), 1.1f);

            builder.AddPivotNode("StagingPayloadAnchor", new Vector3(-deckWidth * 0.38f, 0.22f, 0.0f));
            builder.AddPivotNode("ProcessingPayloadAnchor", new Vector3(0.0f, 0.28f, 0.0f));
            builder.AddPivotNode("DispatchPayloadAnchor", new Vector3(deckWidth * 0.38f, 0.24f, 0.0f));
            return;
        }

        builder.AddBox("Deck", new Vector3(cs * 1.2f, 0.16f, cs * 1.2f), new Color("7C2D12"), new Vector3(0.0f, 0.08f, 0.0f));
        builder.AddBox("Compressor", new Vector3(cs * 0.86f, 0.58f, cs * 0.86f), new Color("F97316"), new Vector3(0.0f, 0.30f, 0.0f));
    }

    public static void BuildTransferBufferModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;
        var root = builder.Root;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox("BufferDeck", new Vector3(cs * 1.24f, 0.12f, cs * 0.98f), new Color("062827"), new Vector3(0.0f, 0.06f, 0.0f));
            builder.AddBox("BufferCradle", new Vector3(cs * 0.92f, 0.14f, cs * 0.68f), new Color("0F766E"), new Vector3(0.0f, 0.18f, 0.0f));
            builder.AddBox("BufferGuideNorth", new Vector3(cs * 0.82f, 0.08f, cs * 0.06f), new Color("99F6E4"), new Vector3(0.0f, 0.28f, -cs * 0.30f));
            builder.AddBox("BufferGuideSouth", new Vector3(cs * 0.82f, 0.08f, cs * 0.06f), new Color("99F6E4"), new Vector3(0.0f, 0.28f, cs * 0.30f));
            builder.AddBox("BufferRackBack", new Vector3(cs * 0.12f, 0.52f, cs * 0.74f), new Color("134E4A"), new Vector3(-cs * 0.42f, 0.34f, 0.0f));
            builder.AddInteriorTray(root, "BufferOutfeed", new Vector3(cs * 0.44f, 0.08f, cs * 0.16f), new Color("115E59"), new Color("CCFBF1"), new Vector3(cs * 0.44f, 0.14f, 0.0f));
            builder.AddInteriorIndicatorLight(root, "BufferLamp", new Color("5EEAD4"), new Vector3(-cs * 0.50f, 0.50f, 0.0f), cs * 0.08f);
            builder.AddPivotNode("BufferPayloadAnchor", new Vector3(0.0f, 0.26f, 0.0f));
            return;
        }

        builder.AddBox("Trench", new Vector3(cs * 0.84f, 0.12f, cs * 0.84f), new Color("0F766E"), new Vector3(0.0f, 0.06f, 0.0f));
        builder.AddBox("Tray", new Vector3(cs * 0.56f, 0.14f, cs * 0.56f), new Color("14B8A6"), new Vector3(0.0f, 0.18f, 0.0f));
    }

    private static void CreateOpenHeavyChamber(
        IModelBuilder builder,
        string prefix,
        Vector3 baseSize,
        float frameHeight,
        float chamberDepth,
        Color frameColor,
        Color accentColor)
    {
        var cs = builder.CellSize;
        var frameSpan = Mathf.Max(cs * 0.68f, baseSize.X * 0.58f);
        var railSpan = Mathf.Max(cs * 0.62f, baseSize.X * 0.76f);
        var braceSpan = Mathf.Max(cs * 0.72f, baseSize.X * 0.84f);

        builder.AddBox($"{prefix}BaseSkid", baseSize, frameColor.Darkened(0.24f), new Vector3(0.0f, baseSize.Y * 0.5f, 0.0f));
        builder.AddBox(
            $"{prefix}DeckPlate",
            new Vector3(baseSize.X * 0.72f, Mathf.Max(cs * 0.04f, baseSize.Y * 0.42f), chamberDepth * 0.38f),
            frameColor.Lightened(0.08f),
            new Vector3(0.0f, baseSize.Y + 0.02f, 0.0f));
        builder.AddBox(
            $"{prefix}FrameWest",
            new Vector3(cs * 0.10f, frameHeight, chamberDepth * 0.78f),
            frameColor,
            new Vector3(-frameSpan * 0.5f, frameHeight * 0.5f, 0.0f));
        builder.AddBox(
            $"{prefix}FrameEast",
            new Vector3(cs * 0.10f, frameHeight, chamberDepth * 0.78f),
            frameColor,
            new Vector3(frameSpan * 0.5f, frameHeight * 0.5f, 0.0f));
        builder.AddBox(
            $"{prefix}ClampRailNorth",
            new Vector3(railSpan, cs * 0.04f, cs * 0.08f),
            accentColor.Lightened(0.06f),
            new Vector3(0.0f, frameHeight * 0.78f, -chamberDepth * 0.28f));
        builder.AddBox(
            $"{prefix}ClampRailSouth",
            new Vector3(railSpan, cs * 0.04f, cs * 0.08f),
            accentColor.Lightened(0.06f),
            new Vector3(0.0f, frameHeight * 0.78f, chamberDepth * 0.28f));
        builder.AddBox(
            $"{prefix}RearBrace",
            new Vector3(braceSpan, cs * 0.06f, cs * 0.08f),
            accentColor,
            new Vector3(0.0f, frameHeight * 0.58f, -chamberDepth * 0.36f));
        builder.AddBox(
            $"{prefix}FrontBrace",
            new Vector3(braceSpan, cs * 0.05f, cs * 0.06f),
            accentColor.Darkened(0.06f),
            new Vector3(0.0f, frameHeight * 0.28f, chamberDepth * 0.34f));
    }
}
