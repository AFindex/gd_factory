using Godot;

public sealed class FactoryBaselinePlayerPlacementState
{
    private readonly FactoryPlayerInventorySelectionState _selectionState = new();

    public string? SelectedInventoryId { get; private set; }
    public Vector2I SelectedSlot { get; private set; }
    public bool HasSelectedSlot { get; private set; }
    public bool PlacementArmed { get; private set; }

    public void SetSelectedSlot(string inventoryId, Vector2I slot, bool placementArmed)
    {
        SelectedInventoryId = inventoryId;
        SelectedSlot = slot;
        HasSelectedSlot = true;
        PlacementArmed = placementArmed;
        SyncSelectionState();
    }

    public void HandleHotbarPressed(FactoryPlayerController? playerController, int index)
    {
        if (playerController is null)
        {
            return;
        }

        playerController.ToggleHotbarIndex(index);
        SelectedInventoryId = FactoryPlayerController.BackpackInventoryId;
        SelectedSlot = new Vector2I(index, 0);
        HasSelectedSlot = true;
        PlacementArmed = playerController.IsHotbarPlacementArmed;
    }

    public bool HandleInventorySlotActivated(
        FactoryPlayerController? playerController,
        FactoryInventoryEndpointResolver resolver,
        string inventoryId,
        Vector2I slot,
        System.Action<int> handleHotbarPressed)
    {
        SelectedInventoryId = inventoryId;
        SelectedSlot = slot;
        HasSelectedSlot = true;
        SyncSelectionState();
        FactoryDemoInteractionBridge.ActivatePlayerInventorySlot(
            playerController,
            resolver,
            _selectionState,
            inventoryId,
            slot,
            handleHotbarPressed);
        PlacementArmed = _selectionState.PlacementArmed;
        return inventoryId == FactoryPlayerController.BackpackInventoryId && slot.Y == 0;
    }

    public void DisarmPlacement(FactoryPlayerController? playerController)
    {
        playerController?.DisarmHotbarPlacement();
        PlacementArmed = false;
        _selectionState.PlacementArmed = false;
    }

    public FactoryItem? ResolveSelectedPlayerItem(
        FactoryPlayerController? playerController,
        FactoryInventoryEndpointResolver resolver)
    {
        SyncSelectionState();
        return FactoryDemoInteractionBridge.ResolveSelectedPlayerItem(playerController, resolver, _selectionState);
    }

    public bool TryConsumeSelectedPlaceable(
        FactoryPlayerController? playerController,
        FactoryInventoryEndpointResolver resolver)
    {
        SyncSelectionState();
        var consumed = FactoryDemoInteractionBridge.TryConsumeSelectedPlaceable(playerController, resolver, _selectionState);
        PlacementArmed = _selectionState.PlacementArmed;
        return consumed;
    }

    private void SyncSelectionState()
    {
        _selectionState.InventoryId = SelectedInventoryId;
        _selectionState.Slot = SelectedSlot;
        _selectionState.HasSlot = HasSelectedSlot;
        _selectionState.PlacementArmed = PlacementArmed;
    }
}

public sealed class FactoryBaselineHudProjection
{
    public FactoryInteractionMode InteractionMode { get; set; }
    public BuildPrototypeKind? BuildKind { get; set; }
    public string? BuildDetails { get; set; }
    public bool PreviewPositive { get; set; }
    public string PreviewMessage { get; set; } = string.Empty;
    public FacingDirection Facing { get; set; }
    public string SelectionTargetText { get; set; } = string.Empty;
    public string? InspectionTitle { get; set; }
    public string? InspectionBody { get; set; }
    public FactoryStructureDetailModel? StructureDetails { get; set; }
}

public static class FactoryBaselineHudProjectionBuilder
{
    public static FactoryBaselineHudProjection Create(
        FactoryInteractionMode interactionMode,
        BuildPrototypeKind? buildKind,
        string? buildDetails,
        bool previewPositive,
        string previewMessage,
        FacingDirection facing,
        FactoryStructure? selectedStructure,
        string selectionTargetText)
    {
        FactoryDemoInteractionBridge.TryGetInspection(selectedStructure, out var inspectionTitle, out var inspectionBody);
        return new FactoryBaselineHudProjection
        {
            InteractionMode = interactionMode,
            BuildKind = buildKind,
            BuildDetails = buildDetails,
            PreviewPositive = previewPositive,
            PreviewMessage = previewMessage,
            Facing = facing,
            SelectionTargetText = selectionTargetText,
            InspectionTitle = inspectionTitle,
            InspectionBody = inspectionBody,
            StructureDetails = FactoryDemoInteractionBridge.BuildLinkedDetailModel(selectedStructure)
        };
    }
}

public static class FactoryBaselineHudApplicator
{
    public static void ApplyToMobileWorldHud(
        FactoryHud hud,
        FactoryBaselineHudProjection projection,
        FactoryStatusTone tone)
    {
        hud.SetPreviewStatus(tone, projection.PreviewMessage);
        hud.SetWorldBuildSelection(projection.BuildKind, projection.Facing, projection.BuildDetails);
    }
}

public static class FactoryBaselineInteractionRules
{
    public static FactoryInteractionMode ResolvePlacementInteractionMode(
        FactoryInteractionMode currentMode,
        bool hasPlacementSource)
    {
        if (currentMode == FactoryInteractionMode.Delete)
        {
            return currentMode;
        }

        return hasPlacementSource
            ? FactoryInteractionMode.Build
            : FactoryInteractionMode.Interact;
    }

    public static bool BlocksWorldPointerInput(bool pointerOverUi, bool hasActiveInventoryInteraction)
    {
        return pointerOverUi || hasActiveInventoryInteraction;
    }

    public static bool AllowsWorldPointerInput(
        bool surfaceAllowsInput,
        bool pointerOverUi,
        bool hasActiveInventoryInteraction)
    {
        return surfaceAllowsInput && !BlocksWorldPointerInput(pointerOverUi, hasActiveInventoryInteraction);
    }
}
