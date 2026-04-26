using Godot;
using NetFactory;
using NetFactory.Models;

public static class CombatOverlayDescriptor
{
    public static void BuildOverlay(IModelBuilder builder, FactoryStructure structure)
    {
        var cellSize = builder.CellSize;
        var previewSize = structure.ResolvedFootprint.GetPreviewSize(cellSize, structure.Facing);

        builder.AddCombatBox("HealthBarBackground",
            new Vector3(Mathf.Max(cellSize * 0.62f, previewSize.X * 0.42f), 0.04f, cellSize * 0.08f),
            new Color(0.05f, 0.07f, 0.10f, 0.78f),
            new Vector3(0.0f, FactoryConstants.StructureHealthBarHeight, 0.0f));
        builder.AddCombatBox("HealthBarFill",
            new Vector3(Mathf.Max(cellSize * 0.60f, previewSize.X * 0.40f), 0.03f, cellSize * 0.06f),
            new Color("4ADE80"),
            new Vector3(0.0f, FactoryConstants.StructureHealthBarHeight, 0.0f));
        builder.AddCombatBox("CombatFocusRing",
            new Vector3(
                Mathf.Max(cellSize * 0.96f, previewSize.X - (cellSize * 0.12f)),
                0.02f,
                Mathf.Max(cellSize * 0.96f, previewSize.Y - (cellSize * 0.12f))),
            new Color(0.35f, 0.85f, 1.0f, 0.36f),
            new Vector3(0.0f, 0.03f, 0.0f));
    }

    public static void ApplyGhostTint(IModelBuilder builder, Color tint)
    {
        ApplyGhostTintRecursive(builder.Root, tint);
    }

    private static void ApplyGhostTintRecursive(Node node, Color tint)
    {
        if (node is MeshInstance3D meshInstance)
        {
            meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            meshInstance.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = tint,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                Roughness = 0.18f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                EmissionEnabled = true,
                Emission = tint.Lightened(0.12f)
            };
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                ApplyGhostTintRecursive(childNode, tint);
            }
        }
    }
}
