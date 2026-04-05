using Godot;

public static class FactoryPreviewVisuals
{
    public static Node3D CreateFacingArrow(string name, float cellSize, float liftY)
    {
        var root = new Node3D
        {
            Name = name,
            Visible = false
        };

        root.AddChild(CreateArrowPart(
            "ArrowTip",
            new Vector3(cellSize * 0.16f, 0.08f, cellSize * 0.12f),
            new Vector3(cellSize * 0.28f, liftY, 0.0f)));
        root.AddChild(CreateArrowPart(
            "ArrowWingNorth",
            new Vector3(cellSize * 0.24f, 0.06f, cellSize * 0.08f),
            new Vector3(cellSize * 0.10f, liftY, -cellSize * 0.10f),
            new Vector3(0.0f, -0.58f, 0.0f)));
        root.AddChild(CreateArrowPart(
            "ArrowWingSouth",
            new Vector3(cellSize * 0.24f, 0.06f, cellSize * 0.08f),
            new Vector3(cellSize * 0.10f, liftY, cellSize * 0.10f),
            new Vector3(0.0f, 0.58f, 0.0f)));
        return root;
    }

    public static void ApplyArrowColor(Node3D arrowRoot, Color color)
    {
        foreach (var child in arrowRoot.GetChildren())
        {
            if (child is MeshInstance3D meshInstance)
            {
                meshInstance.MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = color,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    Roughness = 0.4f
                };
            }
        }
    }

    private static MeshInstance3D CreateArrowPart(string name, Vector3 size, Vector3 position, Vector3? rotation = null)
    {
        return new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = position,
            Rotation = rotation ?? Vector3.Zero
        };
    }
}
