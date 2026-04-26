using Godot;
using NetFactory.Models;
using System;
using System.Collections.Generic;

public partial class StorageStructure : FactoryStructure, IFactoryFilteredItemProvider, IFactoryItemReceiver
{
    private readonly FactorySlottedItemInventory _inventory = new(8, 3);
    private readonly List<MeshInstance3D> _fillIndicators = new();

    private double _dispatchCooldown;
    private MeshInstance3D? _statusBeacon;

    public int BufferedCount => _inventory.Count;
    public int Capacity => _inventory.Capacity;
    public int OccupiedSlotCount => _inventory.OccupiedSlotCount;
    public override string InspectionTitle => $"仓储 ({Cell.X}, {Cell.Y})";
    public override float MaxHealth => 54.0f;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Storage;
    public override string Description => "缓存多个物品，并可将库存向前输出或供机械臂抓取。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return IsOrthogonallyAdjacent(Cell, sourceCell)
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

        if (!IsOrthogonallyAdjacent(Cell, requesterCell))
        {
            return false;
        }

        return _inventory.TryPeekFirst(out item);
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;

        if (!IsOrthogonallyAdjacent(Cell, requesterCell))
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

        if (!IsOrthogonallyAdjacent(Cell, requesterCell))
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

        if (!IsOrthogonallyAdjacent(Cell, requesterCell))
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

        var inventorySection = CreateInventorySection("storage-buffer", "仓储库存", _inventory, true);
        return new FactoryStructureDetailModel(
            InspectionTitle,
            "基础缓存与转运",
            summaryLines,
            new[] { inventorySection });
    }

    public override bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack = false)
    {
        return inventoryId == "storage-buffer" && _inventory.TryMoveItem(fromSlot, toSlot, splitStack);
    }

    public override bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        if (inventoryId == "storage-buffer")
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
            var targetScale = _inventory.IsEmpty ? Vector3.One : new Vector3(1.0f, 1.05f + fillRatio * 0.18f, 1.0f);
            _statusBeacon.Scale = _statusBeacon.Scale.Lerp(targetScale, tickAlpha * 0.45f);
        }
    }

    protected override void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
        base.CaptureRuntimeState(snapshot);
        snapshot.Inventories.Add(FactoryRuntimeSnapshotValues.CaptureInventory("storage-buffer", _inventory));
        snapshot.State["dispatch_cooldown"] = FactoryRuntimeSnapshotValues.FormatDouble(_dispatchCooldown);
    }

    protected override void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        base.ApplyRuntimeState(snapshot, simulation);
        for (var index = 0; index < snapshot.Inventories.Count; index++)
        {
            if (snapshot.Inventories[index].InventoryId != "storage-buffer")
            {
                continue;
            }

            if (!FactoryRuntimeSnapshotValues.TryRestoreInventory(_inventory, snapshot.Inventories[index], simulation))
            {
                throw new InvalidOperationException($"Storage '{GetRuntimeStructureKey()}' could not restore its buffer.");
            }
        }

        _dispatchCooldown = FactoryRuntimeSnapshotValues.TryGetDouble(snapshot.State, "dispatch_cooldown", out var cooldown)
            ? Mathf.Max(0.0, cooldown)
            : 0.0;
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        StorageModelDescriptor.BuildModel(builder, SiteKind, GetInteriorVisualRole());

        _fillIndicators.Clear();
        for (var index = 0; index < 4; index++)
        {
            if (builder.Root.FindChild($"Fill_{index}", true, false) is MeshInstance3D indicator)
            {
                _fillIndicators.Add(indicator);
            }
        }

        _statusBeacon = builder.Root.FindChild("Beacon", true, false) as MeshInstance3D;
    }
}
