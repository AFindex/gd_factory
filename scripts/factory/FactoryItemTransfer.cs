using Godot;
using System.Collections.Generic;
using System.Linq;

public interface IFactoryItemProvider
{
    bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item);
    bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item);
}

public interface IFactoryFilteredItemProvider : IFactoryItemProvider
{
    bool TryPeekFilteredProvidedItem(
        Vector2I requesterCell,
        SimulationController simulation,
        FactoryItemKind? filterItemKind,
        out FactoryItem? item);

    bool TryTakeFilteredProvidedItem(
        Vector2I requesterCell,
        SimulationController simulation,
        FactoryItemKind? filterItemKind,
        out FactoryItem? item);
}

public interface IFactoryItemReceiver
{
    bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation);
    bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation);
}

public interface IFactoryHeavyBundleReceiver
{
    bool CanAcceptHeavyBundle(FactoryItem item, Vector2I sourceCell, SimulationController simulation);
    bool TryAcceptHeavyBundle(FactoryItem item, Vector2I sourceCell, SimulationController simulation);
    bool TryAcceptHeavyBundle(FactoryItem item, Vector2I sourceCell, Vector3 sourceWorldPosition, SimulationController simulation);
}

internal static class FactoryItemTransferAdapter
{
    // Temporary compatibility bridge: runtime routing now resolves a shared handoff descriptor,
    // but most provider/receiver interfaces still speak in legacy source/target cells.
    public static FactoryItemHandoffDescriptor BuildHandoff(
        FactoryStructure provider,
        Vector2I providerDispatchCell,
        FactoryStructurePortResolution receiverResolution,
        Vector2I targetCell)
    {
        return new FactoryItemHandoffDescriptor(
            provider,
            receiverResolution.Structure,
            targetCell,
            providerDispatchCell,
            receiverResolution.ResolveReceiverAcceptanceCell(targetCell),
            receiverResolution.ResolvedFromContractEdge);
    }

    public static bool CanReceiveProvidedItem(
        IFactoryItemReceiver receiver,
        FactoryItem item,
        FactoryItemHandoffDescriptor handoff,
        SimulationController simulation)
    {
        return receiver.CanReceiveProvidedItem(item, ResolveLegacyReceiverSourceCell(handoff), simulation);
    }

    public static bool TryReceiveProvidedItem(
        IFactoryItemReceiver receiver,
        FactoryItem item,
        FactoryItemHandoffDescriptor handoff,
        SimulationController simulation)
    {
        return receiver.TryReceiveProvidedItem(item, ResolveLegacyReceiverSourceCell(handoff), simulation);
    }

    public static bool CanAcceptItem(
        FactoryStructure receiver,
        FactoryItem item,
        FactoryItemHandoffDescriptor handoff,
        SimulationController simulation)
    {
        return receiver.CanAcceptItem(item, ResolveLegacyReceiverSourceCell(handoff), simulation);
    }

    public static bool TryAcceptItem(
        FactoryStructure receiver,
        FactoryItem item,
        FactoryItemHandoffDescriptor handoff,
        SimulationController simulation)
    {
        return receiver.TryAcceptItem(item, ResolveLegacyReceiverSourceCell(handoff), simulation);
    }

    private static Vector2I ResolveLegacyReceiverSourceCell(FactoryItemHandoffDescriptor handoff)
    {
        // Once receivers accept FactoryItemHandoffDescriptor directly, this downgrade step can be removed.
        return handoff.ReceiverResolvedFromContractEdge
            ? handoff.ReceiverAcceptanceCell
            : handoff.ProviderDispatchCell;
    }
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

public readonly struct FactoryInventoryTransferEndpoint
{
    public FactoryInventoryTransferEndpoint(string inventoryId, FactorySlottedItemInventory inventory, System.Func<FactoryItem, bool>? canInsert = null)
    {
        InventoryId = inventoryId;
        Inventory = inventory;
        CanInsert = canInsert;
    }

    public string InventoryId { get; }
    public FactorySlottedItemInventory Inventory { get; }
    public System.Func<FactoryItem, bool>? CanInsert { get; }

    public bool Accepts(FactoryItem item) => CanInsert?.Invoke(item) ?? true;
}

public interface IFactoryInventoryEndpointProvider
{
    bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint);
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

    public bool TryPeekFirstMatching(FactoryItemKind itemKind, out FactoryItem? item)
    {
        foreach (var queuedItem in _items)
        {
            if (queuedItem.ItemKind != itemKind)
            {
                continue;
            }

            item = queuedItem;
            return true;
        }

        item = null;
        return false;
    }

    public bool TryTakeFirstMatching(FactoryItemKind itemKind, out FactoryItem? item)
    {
        if (_items.Count == 0)
        {
            item = null;
            return false;
        }

        var remaining = new Queue<FactoryItem>(_items.Count);
        item = null;
        var removed = false;
        while (_items.Count > 0)
        {
            var nextItem = _items.Dequeue();
            if (!removed && nextItem.ItemKind == itemKind)
            {
                item = nextItem;
                removed = true;
                continue;
            }

            remaining.Enqueue(nextItem);
        }

        while (remaining.Count > 0)
        {
            _items.Enqueue(remaining.Dequeue());
        }

        return removed;
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
    public FactoryItem[] SnapshotItems() => _items.ToArray();

    public bool CanAccept(FactoryItem item)
    {
        return item.ItemKind == ItemKind
            && item.CargoForm == PeekFirst().CargoForm
            && (item.ItemKind != FactoryItemKind.BuildingKit || item.SourceKind == PeekFirst().SourceKind)
            && !IsFull;
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

        if (!toStack.CanAccept(fromStack.PeekFirst()))
        {
            if (requestedCount >= fromStack.Count)
            {
                _items[fromSlot] = toStack;
                _items[toSlot] = fromStack;
                return true;
            }

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

    public bool TryPeekSlot(Vector2I slot, out FactoryItem? item)
    {
        item = null;
        if (!IsValidSlot(slot) || !_items.TryGetValue(slot, out var stack))
        {
            return false;
        }

        item = stack.PeekFirst();
        return true;
    }

    public bool TryTakeFromSlot(Vector2I slot, out FactoryItem? item)
    {
        item = null;
        if (!IsValidSlot(slot) || !_items.TryGetValue(slot, out var stack))
        {
            return false;
        }

        if (!stack.TryTakeFirst(out item))
        {
            return false;
        }

        _itemCount--;
        if (stack.IsEmpty)
        {
            _items.Remove(slot);
        }

        return true;
    }

    public bool TryMoveItemTo(
        FactorySlottedItemInventory targetInventory,
        Vector2I fromSlot,
        Vector2I toSlot,
        bool splitStack = false,
        System.Func<FactoryItem, bool>? targetInsertRule = null,
        System.Func<FactoryItem, bool>? sourceInsertRule = null)
    {
        if (!IsValidSlot(fromSlot)
            || !targetInventory.IsValidSlot(toSlot)
            || !_items.TryGetValue(fromSlot, out var fromStack))
        {
            return false;
        }

        if (ReferenceEquals(this, targetInventory))
        {
            return TryMoveItem(fromSlot, toSlot, splitStack);
        }

        var requestedCount = splitStack && fromStack.Count > 1
            ? Mathf.Max(1, Mathf.CeilToInt(fromStack.Count * 0.5f))
            : fromStack.Count;
        var previewItem = fromStack.PeekFirst();
        if (targetInsertRule is not null && !targetInsertRule(previewItem))
        {
            return false;
        }

        if (!targetInventory._items.TryGetValue(toSlot, out var toStack))
        {
            if (requestedCount >= fromStack.Count)
            {
                if (targetInsertRule is not null)
                {
                    var itemsToValidate = fromStack.TakeUpTo(fromStack.Count);
                    var rebuiltSource = FactoryInventoryItemStack.CreateFromItems(itemsToValidate);
                    if (rebuiltSource is null)
                    {
                        return false;
                    }

                    for (var index = 0; index < itemsToValidate.Count; index++)
                    {
                        if (!targetInsertRule(itemsToValidate[index]))
                        {
                            _items[fromSlot] = rebuiltSource;
                            return false;
                        }
                    }

                    _items.Remove(fromSlot);
                    targetInventory._items[toSlot] = rebuiltSource;
                }
                else
                {
                    _items.Remove(fromSlot);
                    targetInventory._items[toSlot] = fromStack;
                }

                _itemCount -= requestedCount;
                targetInventory._itemCount += requestedCount;
                return true;
            }

            var movedItems = fromStack.TakeUpTo(requestedCount);
            for (var index = 0; index < movedItems.Count; index++)
            {
                if (targetInsertRule is not null && !targetInsertRule(movedItems[index]))
                {
                    for (var restoreIndex = movedItems.Count - 1; restoreIndex >= index; restoreIndex--)
                    {
                        fromStack.TryAddItem(movedItems[restoreIndex]);
                    }

                    return false;
                }
            }

            var movedStack = FactoryInventoryItemStack.CreateFromItems(movedItems);
            if (movedStack is null)
            {
                return false;
            }

            targetInventory._items[toSlot] = movedStack;
            if (fromStack.IsEmpty)
            {
                _items.Remove(fromSlot);
            }

            _itemCount -= movedItems.Count;
            targetInventory._itemCount += movedItems.Count;
            return movedItems.Count > 0;
        }

        if (!toStack.CanAccept(previewItem))
        {
            if (requestedCount >= fromStack.Count)
            {
                var targetPreviewItem = toStack.PeekFirst();
                if ((targetInsertRule is not null && !targetInsertRule(previewItem))
                    || (sourceInsertRule is not null && !sourceInsertRule(targetPreviewItem)))
                {
                    return false;
                }

                _items[fromSlot] = toStack;
                targetInventory._items[toSlot] = fromStack;

                _itemCount = _itemCount - fromStack.Count + toStack.Count;
                targetInventory._itemCount = targetInventory._itemCount - toStack.Count + fromStack.Count;
                return true;
            }

            return false;
        }

        var maxToMove = Mathf.Min(requestedCount, toStack.SpaceLeft);
        if (maxToMove <= 0)
        {
            return false;
        }

        var movedItemsToStack = fromStack.TakeUpTo(maxToMove);
        for (var index = 0; index < movedItemsToStack.Count; index++)
        {
            if (targetInsertRule is not null && !targetInsertRule(movedItemsToStack[index]))
            {
                for (var restoreIndex = movedItemsToStack.Count - 1; restoreIndex >= index; restoreIndex--)
                {
                    fromStack.TryAddItem(movedItemsToStack[restoreIndex]);
                }

                return false;
            }
        }

        var moved = 0;
        for (var index = 0; index < movedItemsToStack.Count; index++)
        {
            if (toStack.TryAddItem(movedItemsToStack[index]))
            {
                moved++;
            }
            else
            {
                fromStack.TryAddItem(movedItemsToStack[index]);
            }
        }

        if (fromStack.IsEmpty)
        {
            _items.Remove(fromSlot);
        }

        _itemCount -= moved;
        targetInventory._itemCount += moved;
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
        return TryPeekFirstMatching(itemKind, null, out item);
    }

    public bool TryPeekFirstMatching(FactoryItemKind itemKind, FactoryCargoForm? cargoForm, out FactoryItem? item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out var stack)
                && stack.ItemKind == itemKind
                && (!cargoForm.HasValue || stack.PeekFirst().CargoForm == cargoForm.Value))
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
        return TryTakeFirstMatching(itemKind, null, out item);
    }

    public bool TryTakeFirstMatching(FactoryItemKind itemKind, FactoryCargoForm? cargoForm, out FactoryItem? item)
    {
        for (var index = 0; index < _slotOrder.Count; index++)
        {
            var slot = _slotOrder[index];
            if (_items.TryGetValue(slot, out var stack)
                && stack.ItemKind == itemKind
                && (!cargoForm.HasValue || stack.PeekFirst().CargoForm == cargoForm.Value)
                && stack.TryTakeFirst(out item))
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

    public int CountByKind(FactoryItemKind itemKind, FactoryCargoForm? cargoForm = null)
    {
        var total = 0;
        foreach (var stack in _items.Values)
        {
            if (stack.ItemKind == itemKind
                && (!cargoForm.HasValue || stack.PeekFirst().CargoForm == cargoForm.Value))
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

    public IReadOnlyList<FactoryItem> SnapshotSlotItems(Vector2I slot)
    {
        if (!IsValidSlot(slot) || !_items.TryGetValue(slot, out var stack))
        {
            return System.Array.Empty<FactoryItem>();
        }

        return stack.SnapshotItems();
    }

    public void Clear()
    {
        _items.Clear();
        _itemCount = 0;
    }

    public bool TryRestoreStack(Vector2I slot, IReadOnlyList<FactoryItem> items)
    {
        if (!IsValidSlot(slot))
        {
            return false;
        }

        if (items.Count == 0)
        {
            if (_items.Remove(slot, out var removed))
            {
                _itemCount -= removed.Count;
            }

            return true;
        }

        if (!CanCreateStack(items))
        {
            return false;
        }

        if (_items.Remove(slot, out var existing))
        {
            _itemCount -= existing.Count;
        }

        var stack = FactoryInventoryItemStack.CreateFromItems(items);
        if (stack is null)
        {
            return false;
        }

        _items[slot] = stack;
        _itemCount += stack.Count;
        return true;
    }

    public bool TryRestoreSnapshot(IEnumerable<(Vector2I Slot, IReadOnlyList<FactoryItem> Items)> stacks)
    {
        var normalized = stacks.ToList();
        for (var index = 0; index < normalized.Count; index++)
        {
            if (!IsValidSlot(normalized[index].Slot) || !CanCreateStack(normalized[index].Items))
            {
                return false;
            }
        }

        Clear();
        for (var index = 0; index < normalized.Count; index++)
        {
            if (!TryRestoreStack(normalized[index].Slot, normalized[index].Items))
            {
                Clear();
                return false;
            }
        }

        return true;
    }

    private static bool CanCreateStack(IReadOnlyList<FactoryItem> items)
    {
        if (items.Count == 0)
        {
            return true;
        }

        var first = items[0];
        var maxStack = FactoryItemCatalog.GetMaxStackSize(first.ItemKind);
        if (items.Count > maxStack)
        {
            return false;
        }

        for (var index = 1; index < items.Count; index++)
        {
            if (items[index].ItemKind != first.ItemKind)
            {
                return false;
            }

            if (items[index].CargoForm != first.CargoForm)
            {
                return false;
            }

            if (first.ItemKind == FactoryItemKind.BuildingKit
                && items[index].SourceKind != first.SourceKind)
            {
                return false;
            }
        }

        return true;
    }
}
