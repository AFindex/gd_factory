using Godot;

public static class FactoryPlayerSpawner
{
    public static FactoryPlayerController SpawnPlayerInWorld(
        Node parent,
        GridManager? grid,
        SimulationController? simulation,
        FactoryCameraRig? cameraRig,
        FactoryBaselinePlayerPlacementState placementState,
        out FactoryPlayerController controller)
    {
        controller = new FactoryPlayerController();
        parent.AddChild(controller);
        controller.GlobalPosition = FindSpawnPosition(grid, controller);
        controller.EnsureStarterLoadout(simulation);
        controller.SelectHotbarIndex(0);
        controller.DisarmHotbarPlacement();
        cameraRig?.SetFollowTarget(controller, snapImmediately: true);
        if (cameraRig is not null)
        {
            cameraRig.FollowTargetEnabled = true;
        }

        placementState.SetSelectedSlot(
            FactoryPlayerController.BackpackInventoryId,
            new Vector2I(0, 0),
            controller.IsHotbarPlacementArmed);

        return controller;
    }

    public static Vector3 FindSpawnPosition(GridManager? grid, Vector3 fallback, Vector2I preferred, int maxRadius)
    {
        if (grid is null)
        {
            return fallback;
        }

        for (var radius = 0; radius <= maxRadius; radius++)
        {
            for (var y = preferred.Y - radius; y <= preferred.Y + radius; y++)
            {
                for (var x = preferred.X - radius; x <= preferred.X + radius; x++)
                {
                    var candidate = new Vector2I(x, y);
                    if (!grid.IsInBounds(candidate) || grid.TryGetStructure(candidate, out _))
                    {
                        continue;
                    }

                    var world = grid.CellToWorld(candidate);
                    return new Vector3(world.X, 0.0f, world.Z);
                }
            }
        }

        return fallback;
    }

    public static Rect2 GetPlayerMovementBounds(GridManager? grid, float fallbackLength)
    {
        if (grid is null)
        {
            return new Rect2(-fallbackLength, -fallbackLength, fallbackLength * 2.0f, fallbackLength * 2.0f);
        }

        var min = grid.GetWorldMin() + Vector2.One * 1.0f;
        var max = grid.GetWorldMax() - Vector2.One * 1.0f;
        return new Rect2(min, max - min);
    }

    private static Vector3 FindSpawnPosition(GridManager? grid, Node3D playerNode)
    {
        var fallback = playerNode.GlobalPosition;
        var preferredCell = grid?.WorldToCell(fallback) ?? Vector2I.Zero;
        return FindSpawnPosition(grid, fallback, preferredCell, 24);
    }
}
