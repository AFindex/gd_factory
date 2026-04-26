using Godot;

public static class FactoryPreviewVisuals
{
    private static readonly StringName PreviewColorMetaKey = new("_factory_preview_color");

    public static Node3D CreateFacingArrow(string name, float arrowLength, float liftY)
    {
        var root = new Node3D
        {
            Name = name,
            Visible = false
        };

        var shaftThickness = Mathf.Max(arrowLength * 0.14f, 0.04f);
        var shaftLength = arrowLength * 0.46f;
        var headLength = arrowLength * 0.26f;
        var headWidth = shaftThickness * 1.18f;

        root.AddChild(CreateArrowPart(
            "ArrowStem",
            new Vector3(shaftLength, 0.06f, shaftThickness),
            new Vector3(-arrowLength * 0.08f, liftY, 0.0f)));
        root.AddChild(CreateArrowPart(
            "ArrowTip",
            new Vector3(arrowLength * 0.18f, 0.07f, shaftThickness * 1.02f),
            new Vector3(arrowLength * 0.28f, liftY, 0.0f)));
        root.AddChild(CreateArrowPart(
            "ArrowHeadNorth",
            new Vector3(headLength, 0.06f, headWidth),
            new Vector3(arrowLength * 0.12f, liftY, -shaftThickness * 0.72f),
            new Vector3(0.0f, -0.58f, 0.0f)));
        root.AddChild(CreateArrowPart(
            "ArrowHeadSouth",
            new Vector3(headLength, 0.06f, headWidth),
            new Vector3(arrowLength * 0.12f, liftY, shaftThickness * 0.72f),
            new Vector3(0.0f, 0.58f, 0.0f)));
        return root;
    }

    public static void ApplyArrowColor(Node3D arrowRoot, Color color)
    {
        foreach (var child in arrowRoot.GetChildren())
        {
            if (child is MeshInstance3D meshInstance)
            {
                ApplyMeshPreviewColor(meshInstance, color);
            }
        }
    }

    public static void ApplyMeshPreviewColor(MeshInstance3D meshInstance, Color color)
    {
        if (meshInstance.HasMeta(PreviewColorMetaKey)
            && meshInstance.GetMeta(PreviewColorMetaKey).AsColor().IsEqualApprox(color))
        {
            return;
        }

        if (meshInstance.MaterialOverride is not StandardMaterial3D material)
        {
            material = new StandardMaterial3D
            {
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                Roughness = 0.4f
            };
            meshInstance.MaterialOverride = material;
        }

        material.AlbedoColor = color;
        meshInstance.SetMeta(PreviewColorMetaKey, color);
    }

    private static MeshInstance3D CreateArrowPart(string name, Vector3 size, Vector3 position, Vector3? rotation = null)
    {
        return new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = position,
            Rotation = rotation ?? Vector3.Zero,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
    }
}
