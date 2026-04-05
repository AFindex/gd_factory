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
    public FactoryInventorySlotState(Vector2I position, FactoryItem? item, int stackCount = 0, int maxStackSize = 0)
    {
        Position = position;
        Item = item;
        StackCount = item is null ? 0 : Mathf.Max(1, stackCount);
        MaxStackSize = item is null ? 0 : Mathf.Max(StackCount, maxStackSize);
    }

    public Vector2I Position { get; }
    public FactoryItem? Item { get; }
    public int StackCount { get; }
    public int MaxStackSize { get; }
    public bool HasItem => Item is not null;
    public bool IsFullStack => HasItem && StackCount >= MaxStackSize;
}

internal sealed class FactoryInventoryItemStack
{
    private readonly List<FactoryItem> _items = new();

    public FactoryInventoryItemStack(FactoryItem firstItem)
    {
        _items.Add(firstItem);
    }

    public FactoryItemKind ItemKind => _items[0].ItemKind;
    public int Count => _items.Count;
    public int MaxStackSize => FactoryItemCatalog.GetMaxStackSize(ItemKind);
    public int SpaceLeft => Mathf.Max(0, MaxStackSize - Count);
    public bool IsEmpty => _items.Count == 0;
    public bool IsFull => _items.Count >= MaxStackSize;
    public FactoryItem PeekFirst() => _items[0];

    public bool CanAccept(FactoryItem item)
    {
        return item.ItemKind == ItemKind && !IsFull;
    }

    public bool TryAddItem(FactoryItem item)
    {
        if (!CanAccept(item))
        {
            return false;
        }

        _items.Add(item);
        return true;
    }

    public bool TryTakeFirst(out FactoryItem? item)
    {
        if (_items.Count == 0)
        {
            item = null;
            return false;
        }

        item = _items[0];
        _items.RemoveAt(0);
        return true;
    }

    public int MergeFrom(FactoryInventoryItemStack source)
    {
        if (source.ItemKind != ItemKind || IsFull)
        {
            return 0;
        }

        var moved = 0;
        while (!IsFull && source.TryTakeFirst(out var item) && item is not null)
        {
            _items.Add(item);
            moved++;
        }

        return moved;
    }

    public List<FactoryItem> TakeUpTo(int count)
    {
        var result = new List<FactoryItem>(Mathf.Max(0, count));
        while (result.Count < count && TryTakeFirst(out var item) && item is not null)
        {
            result.Add(item);
        }

        return result;
    }

    public static FactoryInventoryItemStack? CreateFromItems(IReadOnlyList<FactoryItem> items)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var stack = new FactoryInventoryItemStack(items[0]);
        for (var index = 1; index < items.Count; index++)
        {
            stack.TryAddItem(items[index]);
        }

        return stack;
    }
}

public sealed class FactorySlottedItemInventory
{
    private readonly Dictionary<Vector2I, FactoryInventoryItemStack> _items = new();
    private readonly List<Vector2I> _slotOrder = new();
    private int _itemCount;

    private sealed class SimulatedSlotState
    {
        public FactoryItemKind? ItemKind { get; set; }
        public int Count { get; set; }
        public int MaxStackSize { get; set; }
        public bool IsEmpty => !ItemKind.HasValue;
    }

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
    public int Count => _itemCount;
    public int OccupiedSlotCount => _items.Count;
    public int EmptySlotCount => Capacity - OccupiedSlotCount;
    public bool IsFull => EmptySlotCount == 0;
    public bool IsEmpty => _itemCount == 0;
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
        return _items.TryGetValue(slot, out var stack) ? stack.PeekFirst() : null;
    }

    public bool CanAcceptItem(FactoryItem item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out var existingStack) && existingStack.CanAccept(item))
            {
                return true;
            }
        }

        return EmptySlotCount > 0;
    }

    public bool TryAddItem(FactoryItem item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out var stack) && stack.TryAddItem(item))
            {
                _itemCount++;
                return true;
            }
        }

        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.ContainsKey(slot))
            {
                continue;
            }

            _items[slot] = new FactoryInventoryItemStack(item);
            _itemCount++;
            return true;
        }

        return false;
    }

    public bool TryMoveItem(Vector2I fromSlot, Vector2I toSlot, bool splitStack = false)
    {
        if (!IsValidSlot(fromSlot)
            || !IsValidSlot(toSlot)
            || fromSlot == toSlot
            || !_items.TryGetValue(fromSlot, out var fromStack))
        {
            return false;
        }

        var requestedCount = splitStack && fromStack.Count > 1
            ? Mathf.Max(1, Mathf.CeilToInt(fromStack.Count * 0.5f))
            : fromStack.Count;

        if (!_items.TryGetValue(toSlot, out var toStack))
        {
            if (requestedCount >= fromStack.Count)
            {
                _items.Remove(fromSlot);
                _items[toSlot] = fromStack;
                return true;
            }

            var movedItems = fromStack.TakeUpTo(requestedCount);
            var movedStack = FactoryInventoryItemStack.CreateFromItems(movedItems);
            if (movedStack is null)
            {
                return false;
            }

            _items[toSlot] = movedStack;
            if (fromStack.IsEmpty)
            {
                _items.Remove(fromSlot);
            }

            return true;
        }

        if (toStack.ItemKind != fromStack.ItemKind || toStack.IsFull)
        {
            return false;
        }

        var moved = 0;
        var maxToMove = Mathf.Min(requestedCount, toStack.SpaceLeft);
        var movedItemsToStack = fromStack.TakeUpTo(maxToMove);
        for (var index = 0; index < movedItemsToStack.Count; index++)
        {
            if (toStack.TryAddItem(movedItemsToStack[index]))
            {
                moved++;
            }
        }

        if (fromStack.IsEmpty)
        {
            _items.Remove(fromSlot);
        }

        return moved > 0;
    }

    public bool TryPeekFirst(out FactoryItem? item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out var stack))
            {
                item = stack.PeekFirst();
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
            if (_items.TryGetValue(slot, out var stack) && stack.TryTakeFirst(out item))
            {
                _itemCount--;
                if (stack.IsEmpty)
                {
                    _items.Remove(slot);
                }

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
            if (_items.TryGetValue(slot, out var stack) && stack.ItemKind == itemKind)
            {
                item = stack.PeekFirst();
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
            if (_items.TryGetValue(slot, out var stack) && stack.ItemKind == itemKind && stack.TryTakeFirst(out item))
            {
                _itemCount--;
                if (stack.IsEmpty)
                {
                    _items.Remove(slot);
                }

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
            if (_items.TryGetValue(slot, out var stack))
            {
                snapshot[index] = new FactoryInventorySlotState(slot, stack.PeekFirst(), stack.Count, stack.MaxStackSize);
            }
            else
            {
                snapshot[index] = new FactoryInventorySlotState(slot, null);
            }
        }

        return snapshot;
    }

    public int CountByKind(FactoryItemKind itemKind)
    {
        var total = 0;
        foreach (var stack in _items.Values)
        {
            if (stack.ItemKind == itemKind)
            {
                total += stack.Count;
            }
        }

        return total;
    }

    public bool CanFitItems(IEnumerable<FactoryItemKind> itemKinds)
    {
        var simulation = new SimulatedSlotState[_slotOrder.Count];
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            simulation[index] = new SimulatedSlotState();
            if (_items.TryGetValue(slot, out var stack))
            {
                simulation[index].ItemKind = stack.ItemKind;
                simulation[index].Count = stack.Count;
                simulation[index].MaxStackSize = stack.MaxStackSize;
            }
        }

        foreach (var itemKind in itemKinds)
        {
            var placed = false;
            for (var index = 0; index < simulation.Length; index++)
            {
                var slot = simulation[index];
                if (slot.ItemKind == itemKind && slot.Count < slot.MaxStackSize)
                {
                    slot.Count++;
                    placed = true;
                    break;
                }
            }

            if (placed)
            {
                continue;
            }

            for (var index = 0; index < simulation.Length; index++)
            {
                var slot = simulation[index];
                if (!slot.IsEmpty)
                {
                    continue;
                }

                slot.ItemKind = itemKind;
                slot.Count = 1;
                slot.MaxStackSize = FactoryItemCatalog.GetMaxStackSize(itemKind);
                placed = true;
                break;
            }

            if (!placed)
            {
                return false;
            }
        }

        return true;
    }
}
