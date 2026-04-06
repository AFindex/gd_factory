using Godot;
using System.Collections.Generic;

public partial class StorageStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
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
        return IsOrthogonallyAdjacent(Cell, sourceCell) && _inventory.CanAcceptItem(item);
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

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.92f, 0.24f, CellSize * 0.92f), new Color("334155"), new Vector3(0.0f, 0.12f, 0.0f));
        CreateBox("CrateBody", new Vector3(CellSize * 0.78f, 0.92f, CellSize * 0.78f), new Color("64748B"), new Vector3(0.0f, 0.70f, 0.0f));
        CreateBox("OutputStripe", new Vector3(CellSize * 0.14f, 0.10f, CellSize * 0.42f), new Color("FBBF24"), new Vector3(CellSize * 0.34f, 1.20f, 0.0f));

        for (var index = 0; index < 4; index++)
        {
            var indicator = CreateBox(
                $"Fill_{index}",
                new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.54f),
                new Color("38BDF8"),
                new Vector3(-CellSize * 0.18f + index * CellSize * 0.12f, 1.20f, 0.0f));
            indicator.Visible = false;
            _fillIndicators.Add(indicator);
        }

        _statusBeacon = CreateBox("Beacon", new Vector3(CellSize * 0.18f, 0.18f, CellSize * 0.18f), new Color("E2E8F0"), new Vector3(0.0f, 1.42f, 0.0f));
    }
}
