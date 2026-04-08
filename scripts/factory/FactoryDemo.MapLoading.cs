using Godot;
using System.Collections.Generic;

public partial class FactoryDemo
{
    private void LoadStarterWorldMap()
    {
        if (_grid is null || _structureRoot is null || _simulation is null)
        {
            return;
        }

        var mapPath = DemoLaunchOptions.ResolveFactoryWorldMapPath();
        FactoryMapRuntimeLoader.LoadWorldMap(
            mapPath,
            _grid,
            _structureRoot,
            _simulation);
        AddTransportRenderStressSegment();
    }

    private void AddTransportRenderStressSegment()
    {
        var seededStorages = new List<(StorageStructure? Storage, FactoryItemKind ItemKind)>
        {
            (CreateSeededStressLane(new Vector2I(30, -30), 13), FactoryItemKind.IronOre),
            (CreateSeededStressLane(new Vector2I(30, -28), 13), FactoryItemKind.CopperPlate),
            (CreateSeededStressLane(new Vector2I(30, -26), 13), FactoryItemKind.AmmoMagazine)
        };

        for (var index = 0; index < seededStorages.Count; index++)
        {
            var lane = seededStorages[index];
            if (lane.Storage is null)
            {
                continue;
            }

            SeedStressStorage(lane.Storage, lane.ItemKind, 72);
        }
    }

    private StorageStructure? CreateSeededStressLane(Vector2I storageCell, int beltLength)
    {
        var storage = PlaceStructure(BuildPrototypeKind.Storage, storageCell, FacingDirection.East) as StorageStructure;
        if (storage is null)
        {
            return null;
        }

        for (var step = 1; step <= beltLength; step++)
        {
            PlaceStructure(BuildPrototypeKind.Belt, storageCell + new Vector2I(step, 0), FacingDirection.East);
        }

        PlaceStructure(BuildPrototypeKind.Sink, storageCell + new Vector2I(beltLength + 1, 0), FacingDirection.East);
        return storage;
    }

    private void SeedStressStorage(StorageStructure storage, FactoryItemKind itemKind, int count)
    {
        if (_simulation is null || !storage.TryResolveInventoryEndpoint("storage-buffer", out var endpoint))
        {
            return;
        }

        for (var index = 0; index < count; index++)
        {
            endpoint.Inventory.TryAddItem(_simulation.CreateItem(BuildPrototypeKind.Storage, itemKind));
        }
    }
}
