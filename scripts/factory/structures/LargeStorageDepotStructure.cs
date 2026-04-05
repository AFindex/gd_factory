using Godot;
using System.Collections.Generic;

public partial class LargeStorageDepotStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
{
    private readonly FactorySlottedItemInventory _inventory = new(5, 4);
    private readonly List<MeshInstance3D> _fillIndicators = new();
    private double _dispatchCooldown;
    private MeshInstance3D? _statusBeacon;

    public int BufferedCount => _inventory.Count;
    public int Capacity => _inventory.Capacity;
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
        return IsAdjacentToFootprint(sourceCell) && !_inventory.IsFull;
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

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"容量：{BufferedCount}/{Capacity}";
        yield return $"输出方向：{FactoryDirection.ToLabel(Facing)}";
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
            "大型缓存区与多格占地状态",
            summaryLines,
            new[] { inventorySection });
    }

    public override bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot)
    {
        return inventoryId == "large-storage-buffer" && _inventory.TryMoveItem(fromSlot, toSlot);
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
        var fillRatio = (float)BufferedCount / Capacity;
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
