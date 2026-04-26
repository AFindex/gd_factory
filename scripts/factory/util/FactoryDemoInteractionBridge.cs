using Godot;

public delegate bool FactoryInventoryEndpointResolver(string inventoryId, out FactoryInventoryTransferEndpoint endpoint);

public sealed class FactoryPlayerInventorySelectionState
{
    public string? InventoryId { get; set; }
    public Vector2I Slot { get; set; }
    public bool HasSlot { get; set; }
    public bool PlacementArmed { get; set; }
}

public static class FactoryDemoInteractionBridge
{
    public static bool TryResolveInventoryEndpoint(
        FactoryPlayerController? playerController,
        FactoryStructure? selectedStructure,
        string inventoryId,
        out FactoryInventoryTransferEndpoint endpoint)
    {
        if (playerController?.TryResolveInventoryEndpoint(inventoryId, out endpoint) == true)
        {
            return true;
        }

        if (selectedStructure is IFactoryInventoryEndpointProvider endpointProvider
            && endpointProvider.TryResolveInventoryEndpoint(inventoryId, out endpoint))
        {
            return true;
        }

        endpoint = default;
        return false;
    }

    public static bool TryMoveInventoryItem(
        FactoryInventoryEndpointResolver resolver,
        string inventoryId,
        Vector2I fromSlot,
        Vector2I toSlot,
        bool splitStack)
    {
        return resolver(inventoryId, out var endpoint)
            && endpoint.Inventory.TryMoveItem(fromSlot, toSlot, splitStack);
    }

    public static bool TryTransferInventoryItem(
        FactoryInventoryEndpointResolver resolver,
        string fromInventoryId,
        Vector2I fromSlot,
        string toInventoryId,
        Vector2I toSlot,
        bool splitStack)
    {
        if (!resolver(fromInventoryId, out var fromEndpoint)
            || !resolver(toInventoryId, out var toEndpoint))
        {
            return false;
        }

        return fromEndpoint.Inventory.TryMoveItemTo(
            toEndpoint.Inventory,
            fromSlot,
            toSlot,
            splitStack,
            toEndpoint.CanInsert,
            fromEndpoint.CanInsert);
    }

    public static void ActivatePlayerInventorySlot(
        FactoryPlayerController? playerController,
        FactoryInventoryEndpointResolver resolver,
        FactoryPlayerInventorySelectionState state,
        string inventoryId,
        Vector2I slot,
        System.Action<int> handleHotbarPressed)
    {
        state.InventoryId = inventoryId;
        state.Slot = slot;
        state.HasSlot = true;

        if (inventoryId == FactoryPlayerController.BackpackInventoryId && slot.Y == 0)
        {
            handleHotbarPressed(slot.X);
            return;
        }

        playerController?.DisarmHotbarPlacement();
        state.PlacementArmed = inventoryId == FactoryPlayerController.BackpackInventoryId
            && ResolveSelectedPlayerItem(playerController, resolver, state) is FactoryItem item
            && FactoryPresentation.IsPlaceableStructureItem(item);
    }

    public static FactoryItem? ResolveSelectedPlayerItem(
        FactoryPlayerController? playerController,
        FactoryInventoryEndpointResolver resolver,
        FactoryPlayerInventorySelectionState state)
    {
        if (!state.HasSlot || string.IsNullOrWhiteSpace(state.InventoryId))
        {
            return playerController?.GetActiveHotbarItem();
        }

        return resolver(state.InventoryId!, out var endpoint)
            ? endpoint.Inventory.GetItemOrDefault(state.Slot)
            : playerController?.GetActiveHotbarItem();
    }

    public static bool TryConsumeSelectedPlaceable(
        FactoryPlayerController? playerController,
        FactoryInventoryEndpointResolver resolver,
        FactoryPlayerInventorySelectionState state)
    {
        if (!state.HasSlot
            || string.IsNullOrWhiteSpace(state.InventoryId)
            || !resolver(state.InventoryId!, out var endpoint))
        {
            state.PlacementArmed = false;
            return false;
        }

        var consumed = endpoint.Inventory.TryTakeFromSlot(state.Slot, out _);
        playerController?.RefreshActiveSlotState();

        var remainingItem = endpoint.Inventory.GetItemOrDefault(state.Slot);
        state.PlacementArmed = remainingItem is not null && FactoryPresentation.IsPlaceableStructureItem(remainingItem);
        if (!state.PlacementArmed && state.Slot.Y == 0)
        {
            playerController?.DisarmHotbarPlacement();
        }

        return consumed;
    }

    public static bool TryMoveDetailInventoryItem(
        FactoryStructure? selectedStructure,
        string inventoryId,
        Vector2I fromSlot,
        Vector2I toSlot,
        bool splitStack)
    {
        return selectedStructure is IFactoryStructureDetailProvider detailProvider
            && detailProvider.TryMoveDetailInventoryItem(inventoryId, fromSlot, toSlot, splitStack);
    }

    public static bool TrySetDetailRecipe(FactoryStructure? selectedStructure, string recipeId)
    {
        return selectedStructure is IFactoryStructureDetailProvider detailProvider
            && detailProvider.TrySetDetailRecipe(recipeId);
    }

    public static bool TryInvokeDetailAction(FactoryStructure? selectedStructure, string actionId)
    {
        return selectedStructure is IFactoryStructureDetailProvider detailProvider
            && detailProvider.TryInvokeDetailAction(actionId);
    }

    public static bool TryGetInspection(FactoryStructure? selectedStructure, out string? title, out string? body)
    {
        if (selectedStructure is not null
            && GodotObject.IsInstanceValid(selectedStructure)
            && selectedStructure.IsInsideTree()
            && selectedStructure is IFactoryInspectable inspectable)
        {
            title = inspectable.InspectionTitle;
            body = string.Join("\n", inspectable.GetInspectionLines());
            return true;
        }

        title = null;
        body = null;
        return false;
    }

    public static FactoryStructureDetailModel? BuildLinkedDetailModel(FactoryStructure? selectedStructure)
    {
        return selectedStructure is IFactoryStructureDetailProvider detailProvider
            && GodotObject.IsInstanceValid(selectedStructure)
            && selectedStructure.IsInsideTree()
            ? detailProvider.GetDetailModel()
            : null;
    }
}
