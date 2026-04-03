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

public interface IFactoryCombatSystem
{
    void SimulationStep(SimulationController simulation, double stepSeconds);
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

    public int CountByKind(FactoryItemKind itemKind)
    {
        var total = 0;
        foreach (var item in _items)
        {
            if (item.ItemKind == itemKind)
            {
                total++;
            }
        }

        return total;
    }
}

public sealed class FactoryInventorySlotState
{
    public FactoryInventorySlotState(Vector2I position, FactoryItem? item)
    {
        Position = position;
        Item = item;
    }

    public Vector2I Position { get; }
    public FactoryItem? Item { get; }
    public bool HasItem => Item is not null;
}

public sealed class FactorySlottedItemInventory
{
    private readonly Dictionary<Vector2I, FactoryItem> _items = new();
    private readonly List<Vector2I> _slotOrder = new();

    public FactorySlottedItemInventory(int columns, int rows)
    {
        Columns = Mathf.Max(1, columns);
        Rows = Mathf.Max(1, rows);

        for (var y = 0; y < Rows; y++)
        {
            for (var x = 0; x < Columns; x++)
            {
                _slotOrder.Add(new Vector2I(x, y));
            }
        }
    }

    public int Columns { get; }
    public int Rows { get; }
    public int Capacity => Columns * Rows;
    public int Count => _items.Count;
    public bool IsFull => Count >= Capacity;
    public bool IsEmpty => Count == 0;
    public Vector2I GridSize => new(Columns, Rows);

    public bool IsValidSlot(Vector2I slot)
    {
        return slot.X >= 0
            && slot.X < Columns
            && slot.Y >= 0
            && slot.Y < Rows;
    }

    public bool HasItemAt(Vector2I slot)
    {
        return _items.ContainsKey(slot);
    }

    public FactoryItem? GetItemOrDefault(Vector2I slot)
    {
        return _items.TryGetValue(slot, out var item) ? item : null;
    }

    public bool TryAddItem(FactoryItem item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.ContainsKey(slot))
            {
                continue;
            }

            _items[slot] = item;
            return true;
        }

        return false;
    }

    public bool TryMoveItem(Vector2I fromSlot, Vector2I toSlot)
    {
        if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot) || fromSlot == toSlot || !_items.TryGetValue(fromSlot, out var item) || _items.ContainsKey(toSlot))
        {
            return false;
        }

        _items.Remove(fromSlot);
        _items[toSlot] = item;
        return true;
    }

    public bool TryPeekFirst(out FactoryItem? item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out item))
            {
                return true;
            }
        }

        item = null;
        return false;
    }

    public bool TryTakeFirst(out FactoryItem? item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out item))
            {
                _items.Remove(slot);
                return true;
            }
        }

        item = null;
        return false;
    }

    public bool TryPeekFirstMatching(FactoryItemKind itemKind, out FactoryItem? item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out item) && item.ItemKind == itemKind)
            {
                return true;
            }
        }

        item = null;
        return false;
    }

    public bool TryTakeFirstMatching(FactoryItemKind itemKind, out FactoryItem? item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out item) && item.ItemKind == itemKind)
            {
                _items.Remove(slot);
                return true;
            }
        }

        item = null;
        return false;
    }

    public FactoryInventorySlotState[] Snapshot()
    {
        var snapshot = new FactoryInventorySlotState[_slotOrder.Count];
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            snapshot[index] = new FactoryInventorySlotState(slot, GetItemOrDefault(slot));
        }

        return snapshot;
    }

    public int CountByKind(FactoryItemKind itemKind)
    {
        var total = 0;
        foreach (var item in _items.Values)
        {
            if (item.ItemKind == itemKind)
            {
                total++;
            }
        }

        return total;
    }
}
