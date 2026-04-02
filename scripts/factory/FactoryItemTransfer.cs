using Godot;
using System.Collections.Generic;

public interface IFactoryItemProvider
{
    bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item);
    bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item);
}

public interface IFactoryItemReceiver
{
    bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation);
    bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation);
}

public interface IFactoryInspectable
{
    string InspectionTitle { get; }
    IEnumerable<string> GetInspectionLines();
}

public sealed class FactoryItemBuffer
{
    private readonly Queue<FactoryItem> _items = new();

    public FactoryItemBuffer(int capacity)
    {
        Capacity = Mathf.Max(1, capacity);
    }

    public int Capacity { get; }
    public int Count => _items.Count;
    public bool IsFull => _items.Count >= Capacity;
    public bool IsEmpty => _items.Count == 0;

    public bool TryEnqueue(FactoryItem item)
    {
        if (IsFull)
        {
            return false;
        }

        _items.Enqueue(item);
        return true;
    }

    public bool TryPeek(out FactoryItem? item)
    {
        if (_items.Count == 0)
        {
            item = null;
            return false;
        }

        item = _items.Peek();
        return true;
    }

    public bool TryDequeue(out FactoryItem? item)
    {
        if (_items.Count == 0)
        {
            item = null;
            return false;
        }

        item = _items.Dequeue();
        return true;
    }

    public FactoryItem[] Snapshot()
    {
        return _items.ToArray();
    }
}
