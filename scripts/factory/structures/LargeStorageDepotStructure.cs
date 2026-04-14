using Godot;
using System;
using System.Collections.Generic;

public partial class LargeStorageDepotStructure : FactoryStructure, IFactoryFilteredItemProvider, IFactoryItemReceiver
{
    private readonly FactorySlottedItemInventory _inventory = new(8, 6);
    private readonly List<MeshInstance3D> _fillIndicators = new();
    private double _dispatchCooldown;
    private MeshInstance3D? _statusBeacon;

    public int BufferedCount => _inventory.Count;
    public int Capacity => _inventory.Capacity;
    public int OccupiedSlotCount => _inventory.OccupiedSlotCount;
    public override string InspectionTitle => $"大型仓储 ({Cell.X}, {Cell.Y})";
    public override float MaxHealth => 110.0f;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.LargeStorageDepot;
    public override string Description => "占据 2x2 的大型缓存仓，可承接更宽的物流缓冲。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return IsAdjacentToFootprint(sourceCell);
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return IsAdjacentToFootprint(sourceCell)
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item)
            && _inventory.CanAcceptItem(item);
    }

    public override bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanReceiveProvidedItem(item, sourceCell, simulation);
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveProvidedItem(item, sourceCell, simulation))
        {
            return false;
        }

        return _inventory.TryAddItem(item);
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return TryReceiveProvidedItem(item, sourceCell, simulation);
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (!IsAdjacentToFootprint(requesterCell))
        {
            return false;
        }

        return _inventory.TryPeekFirst(out item);
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (!IsAdjacentToFootprint(requesterCell))
        {
            return false;
        }

        var removed = _inventory.TryTakeFirst(out item);
        if (removed)
        {
            _dispatchCooldown = FactoryConstants.StorageDispatchSeconds * 0.35f;
        }

        return removed;
    }

    public bool TryPeekFilteredProvidedItem(
        Vector2I requesterCell,
        SimulationController simulation,
        FactoryItemKind? filterItemKind,
        out FactoryItem? item)
    {
        item = null;
        if (!IsAdjacentToFootprint(requesterCell))
        {
            return false;
        }

        return filterItemKind.HasValue
            ? _inventory.TryPeekFirstMatching(filterItemKind.Value, out item)
            : _inventory.TryPeekFirst(out item);
    }

    public bool TryTakeFilteredProvidedItem(
        Vector2I requesterCell,
        SimulationController simulation,
        FactoryItemKind? filterItemKind,
        out FactoryItem? item)
    {
        item = null;
        if (!IsAdjacentToFootprint(requesterCell))
        {
            return false;
        }

        var removed = filterItemKind.HasValue
            ? _inventory.TryTakeFirstMatching(filterItemKind.Value, out item)
            : _inventory.TryTakeFirst(out item);
        if (removed)
        {
            _dispatchCooldown = FactoryConstants.StorageDispatchSeconds * 0.35f;
        }

        return removed;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"容量：{BufferedCount}/{Capacity} 件 | 占用槽位：{OccupiedSlotCount}/{Capacity}";
    }

    public override FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        var inventorySection = CreateInventorySection("large-storage-buffer", "大型仓储库存", _inventory, true);
        return new FactoryStructureDetailModel(
            InspectionTitle,
            "大容量缓存与转运",
            summaryLines,
            new[] { inventorySection });
    }

    public override bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack = false)
    {
        return inventoryId == "large-storage-buffer" && _inventory.TryMoveItem(fromSlot, toSlot, splitStack);
    }

    public override bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        if (inventoryId == "large-storage-buffer")
        {
            endpoint = new FactoryInventoryTransferEndpoint(inventoryId, _inventory);
            return true;
        }

        endpoint = default;
        return false;
    }

    public override IReadOnlyList<FactoryMapSeedItemEntry> CaptureMapSeedItems()
    {
        return CaptureSeedItemsFromInventory(_inventory);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _dispatchCooldown = Mathf.Max(0.0, (float)(_dispatchCooldown - stepSeconds));
        if (_dispatchCooldown > 0.0f || !_inventory.TryPeekFirst(out var item) || item is null)
        {
            return;
        }

        if (simulation.TrySendItem(this, GetOutputCell(), item))
        {
            _inventory.TryTakeFirst(out _);
            _dispatchCooldown = FactoryConstants.StorageDispatchSeconds;
            if (_statusBeacon is not null)
            {
                _statusBeacon.Scale = new Vector3(1.12f, 1.12f, 1.12f);
            }
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        var fillRatio = Capacity <= 0 ? 0.0f : (float)OccupiedSlotCount / Capacity;
        for (var index = 0; index < _fillIndicators.Count; index++)
        {
            var threshold = (index + 1.0f) / _fillIndicators.Count;
            _fillIndicators[index].Visible = fillRatio >= threshold - 0.001f;
        }

        if (_statusBeacon is not null)
        {
            var targetScale = _inventory.IsEmpty ? Vector3.One : new Vector3(1.0f, 1.10f + fillRatio * 0.14f, 1.0f);
            _statusBeacon.Scale = _statusBeacon.Scale.Lerp(targetScale, tickAlpha * 0.45f);
        }
    }

    protected override void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
        base.CaptureRuntimeState(snapshot);
        snapshot.Inventories.Add(FactoryRuntimeSnapshotValues.CaptureInventory("large-storage-buffer", _inventory));
        snapshot.State["dispatch_cooldown"] = FactoryRuntimeSnapshotValues.FormatDouble(_dispatchCooldown);
    }

    protected override void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        base.ApplyRuntimeState(snapshot, simulation);
        for (var index = 0; index < snapshot.Inventories.Count; index++)
        {
            if (snapshot.Inventories[index].InventoryId != "large-storage-buffer")
            {
                continue;
            }

            if (!FactoryRuntimeSnapshotValues.TryRestoreInventory(_inventory, snapshot.Inventories[index], simulation))
            {
                throw new InvalidOperationException($"Large depot '{GetRuntimeStructureKey()}' could not restore its buffer.");
            }
        }

        _dispatchCooldown = FactoryRuntimeSnapshotValues.TryGetDouble(snapshot.State, "dispatch_cooldown", out var cooldown)
            ? Mathf.Max(0.0, cooldown)
            : 0.0;
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 1.86f, 0.24f, CellSize * 1.86f), new Color("334155"), new Vector3(0.0f, 0.12f, 0.0f));
        CreateBox("DepotBody", new Vector3(CellSize * 1.62f, 1.02f, CellSize * 1.62f), new Color("475569"), new Vector3(0.0f, 0.76f, 0.0f));
        CreateBox("OutputStripe", new Vector3(CellSize * 0.22f, 0.12f, CellSize * 0.74f), new Color("FBBF24"), new Vector3(CellSize * 0.74f, 1.30f, 0.0f));

        for (var index = 0; index < 5; index++)
        {
            var indicator = CreateBox(
                $"Fill_{index}",
                new Vector3(CellSize * 0.16f, 0.12f, CellSize * 1.18f),
                new Color("38BDF8"),
                new Vector3(-CellSize * 0.54f + index * CellSize * 0.27f, 1.30f, 0.0f));
            indicator.Visible = false;
            _fillIndicators.Add(indicator);
        }

        _statusBeacon = CreateBox("Beacon", new Vector3(CellSize * 0.24f, 0.24f, CellSize * 0.24f), new Color("E2E8F0"), new Vector3(0.0f, 1.64f, 0.0f));
    }

    private bool IsAdjacentToFootprint(Vector2I cell)
    {
        foreach (var occupiedCell in GetOccupiedCells())
        {
            if (IsOrthogonallyAdjacent(occupiedCell, cell))
            {
                return true;
            }
        }

        return false;
    }
}
