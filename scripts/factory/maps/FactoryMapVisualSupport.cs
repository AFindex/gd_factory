using Godot;

public static class FactoryMapVisualSupport
{
    public static void RebuildResourceOverlay(
        Node3D? overlayRoot,
        GridManager? grid,
        string tileNamePrefix,
        float tileFootprintScale,
        float tileHeight,
        float tileYOffset,
        float tileRoughness,
        string chipNamePrefix,
        float chipFootprintScale,
        float chipHeight,
        float chipYOffset,
        float chipLightenAmount,
        float chipRoughness)
    {
        if (overlayRoot is null || grid is null)
        {
            return;
        }

        foreach (var child in overlayRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        var deposits = grid.GetResourceDeposits();
        for (var depositIndex = 0; depositIndex < deposits.Count; depositIndex++)
        {
            var deposit = deposits[depositIndex];
            for (var cellIndex = 0; cellIndex < deposit.Cells.Count; cellIndex++)
            {
                var cell = deposit.Cells[cellIndex];
                overlayRoot.AddChild(CreateTileMesh(
                    tileNamePrefix,
                    deposit.Id,
                    cell,
                    grid,
                    deposit.Tint,
                    tileFootprintScale,
                    tileHeight,
                    tileYOffset,
                    tileRoughness));
                overlayRoot.AddChild(CreateChipMesh(
                    chipNamePrefix,
                    deposit.Id,
                    cell,
                    grid,
                    deposit.Tint,
                    chipFootprintScale,
                    chipHeight,
                    chipYOffset,
                    chipLightenAmount,
                    chipRoughness));
            }
        }
    }

    private static MeshInstance3D CreateTileMesh(
        string namePrefix,
        string depositId,
        Vector2I cell,
        GridManager grid,
        Color tint,
        float footprintScale,
        float height,
        float yOffset,
        float roughness)
    {
        return new MeshInstance3D
        {
            Name = $"{namePrefix}{depositId}_{cell.X}_{cell.Y}",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * footprintScale, height, FactoryConstants.CellSize * footprintScale) },
            Position = grid.CellToWorld(cell) + new Vector3(0.0f, yOffset, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = tint,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                Roughness = roughness
            }
        };
    }

    private static MeshInstance3D CreateChipMesh(
        string namePrefix,
        string depositId,
        Vector2I cell,
        GridManager grid,
        Color tint,
        float footprintScale,
        float height,
        float yOffset,
        float lightenAmount,
        float roughness)
    {
        return new MeshInstance3D
        {
            Name = $"{namePrefix}{depositId}_{cell.X}_{cell.Y}",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * footprintScale, height, FactoryConstants.CellSize * footprintScale) },
            Position = grid.CellToWorld(cell) + new Vector3(0.0f, yOffset, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = tint.Lightened(lightenAmount),
                Roughness = roughness
            }
        };
    }
}
