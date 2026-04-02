using Godot;
using System.Collections.Generic;

public partial class StorageStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver, IFactoryInspectable
{
    private readonly FactoryItemBuffer _buffer = new(FactoryConstants.StorageCapacity);
    private readonly List<MeshInstance3D> _fillIndicators = new();

    private double _dispatchCooldown;
    private MeshInstance3D? _statusBeacon;

    public int BufferedCount => _buffer.Count;
    public int Capacity => _buffer.Capacity;
    public string InspectionTitle => $"仓储 ({Cell.X}, {Cell.Y})";

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
        return AcceptsFrom(sourceCell) && !_buffer.IsFull;
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

        return _buffer.TryEnqueue(item);
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return TryReceiveProvidedItem(item, sourceCell, simulation);
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;

        if (requesterCell != GetOutputCell())
        {
            return false;
        }

        return _buffer.TryPeek(out item);
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;

        if (requesterCell != GetOutputCell())
        {
            return false;
        }

        var removed = _buffer.TryDequeue(out item);
        if (removed)
        {
            _dispatchCooldown = FactoryConstants.StorageDispatchSeconds * 0.35f;
        }

        return removed;
    }

    public IEnumerable<string> GetInspectionLines()
    {
        yield return $"容量：{BufferedCount}/{Capacity}";
        yield return $"输出方向：{FactoryDirection.ToLabel(Facing)}";

        var snapshot = _buffer.Snapshot();
        if (snapshot.Length == 0)
        {
            yield return "库存为空";
            yield break;
        }

        for (var index = 0; index < snapshot.Length; index++)
        {
            var item = snapshot[index];
            yield return $"{index + 1}. {FactoryPresentation.GetKindLabel(item.SourceKind)} #{item.Id}";
        }
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _dispatchCooldown = Mathf.Max(0.0, (float)(_dispatchCooldown - stepSeconds));
        if (_dispatchCooldown > 0.0f || !_buffer.TryPeek(out var item) || item is null)
        {
            return;
        }

        if (simulation.TrySendItem(this, GetOutputCell(), item))
        {
            _buffer.TryDequeue(out _);
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
            var targetScale = _buffer.IsEmpty ? Vector3.One : new Vector3(1.0f, 1.05f + fillRatio * 0.18f, 1.0f);
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
