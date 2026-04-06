using Godot;
using System;
using System.Collections.Generic;

public partial class FactoryStructureDetailWindow : PanelContainer
{
    private const double InventoryDragHoldSeconds = 0.18;
    private const float DetailWindowMinWidth = 220.0f;
    private const float DetailWindowViewportMargin = 24.0f;
    private const float DetailWindowHorizontalChrome = 40.0f;
    private const float InventoryGridGap = 4.0f;
    private const float InventorySlotPreferredSize = 58.0f;
    private const float InventorySlotMinSize = 44.0f;
    private static readonly Vector2 DragPreviewOffset = new(18.0f, 18.0f);
    private static void TraceLog(string message) => GD.Print($"[FactoryStructureDetailWindow] {message}");

    private sealed class InventorySlotWidget
    {
        public required PanelContainer Panel { get; init; }
        public required string InventoryId { get; init; }
        public required Vector2I SlotPosition { get; init; }
        public required bool HasItem { get; init; }
        public required FactoryItemKind? ItemKind { get; init; }
        public required string ItemLabelText { get; init; }
        public required int StackCount { get; init; }
        public required int MaxStackSize { get; init; }
        public required TextureRect Icon { get; init; }
        public required Label Label { get; init; }
        public required Label StackLabel { get; init; }
        public required Label SubLabel { get; init; }
        public required Color AccentColor { get; init; }

        public bool CanAcceptFrom(InventorySlotWidget source)
        {
            if (InventoryId == source.InventoryId && SlotPosition == source.SlotPosition)
            {
                return false;
            }

            if (!HasItem)
            {
                return true;
            }

            return ItemKind.HasValue
                && source.ItemKind.HasValue
                && ItemKind == source.ItemKind
                && StackCount < MaxStackSize;
        }
    }

    private sealed class InventoryGridLayout
    {
        public required GridContainer Grid { get; init; }
        public required int Columns { get; init; }
        public required List<PanelContainer> SlotPanels { get; init; }
    }

    private readonly struct InventorySlotReference
    {
        public InventorySlotReference(string inventoryId, Vector2I slotPosition, FactoryItemKind? itemKind)
        {
            InventoryId = inventoryId;
            SlotPosition = slotPosition;
            ItemKind = itemKind;
        }

        public string InventoryId { get; }
        public Vector2I SlotPosition { get; }
        public FactoryItemKind? ItemKind { get; }
    }

    private readonly struct InventoryDragPayload
    {
        public InventoryDragPayload(
            string inventoryId,
            Vector2I slotPosition,
            FactoryItemKind? itemKind,
            string itemLabelText,
            int stackCount,
            int maxStackSize,
            Texture2D? iconTexture,
            Color accentColor)
        {
            InventoryId = inventoryId;
            SlotPosition = slotPosition;
            ItemKind = itemKind;
            ItemLabelText = itemLabelText ?? string.Empty;
            StackCount = stackCount;
            MaxStackSize = maxStackSize;
            IconTexture = iconTexture;
            AccentColor = accentColor;
        }

        public string InventoryId { get; }
        public Vector2I SlotPosition { get; }
        public FactoryItemKind? ItemKind { get; }
        public string ItemLabelText { get; }
        public int StackCount { get; }
        public int MaxStackSize { get; }
        public Texture2D? IconTexture { get; }
        public Color AccentColor { get; }
    }

    private static readonly List<FactoryStructureDetailWindow> s_instances = new();
    private static InventoryDragPayload? s_dragSourcePayload;
    private static Action<string, Vector2I, Vector2I, bool>? s_sharedMoveHandler;
    private static Action<string, Vector2I, string, Vector2I, bool>? s_sharedTransferHandler;

    private PanelContainer? _titleBar;
    private Label? _titleLabel;
    private Label? _subtitleLabel;
    private Label? _summaryLabel;
    private ScrollContainer? _scroll;
    private MarginContainer? _bodyMargin;
    private VBoxContainer? _body;
    private VBoxContainer? _inventorySections;
    private VBoxContainer? _recipeSections;
    private VBoxContainer? _actionSections;
    private PanelContainer? _recipePickerPanel;
    private VBoxContainer? _recipePickerList;
    private Label? _dragStateLabel;
    private PanelContainer? _dragPreview;
    private TextureRect? _dragPreviewIcon;
    private Label? _dragPreviewTitle;
    private Label? _dragPreviewCount;
    private readonly List<InventorySlotWidget> _slotWidgets = new();
    private readonly List<InventoryGridLayout> _inventoryLayouts = new();
    private Rect2 _dragBounds = new(Vector2.Zero, new Vector2(900.0f, 600.0f));
    private bool _draggingWindow;
    private bool _windowMovedByUser;
    private Vector2 _windowDragOffset;
    private InventorySlotWidget? _pendingDragSourceSlot;
    private double _pendingDragElapsed;
    private InventorySlotWidget? _dragSourceSlot;
    private InventorySlotWidget? _hoveredSlot;
    private string? _modelSignature;
    private bool _leftMouseHeld;
    private int _inventoryLayoutRevision;
    private int _lastSyncedInventoryLayoutRevision = -1;
    private float _lastSyncedWindowWidth = -1.0f;
    private float _lastSyncedBodyWidth = -1.0f;

    public event Action<string, Vector2I, Vector2I, bool>? InventoryMoveRequested;
    public event Action<string, Vector2I, string, Vector2I, bool>? InventoryTransferRequested;
    public event Action<string, Vector2I>? InventorySlotActivated;
    public event Action<string>? RecipeSelected;
    public event Action<string>? DetailActionRequested;
    public event Action? CloseRequested;
    public bool IsShowing => Visible;
    public string CurrentTitleText => _titleLabel?.Text ?? string.Empty;
    public bool HasActiveInventoryInteraction => _dragSourceSlot is not null || HasSharedInventoryDrag;
    public static bool HasSharedInventoryDrag => s_dragSourcePayload.HasValue;

    public override void _Ready()
    {
        Name = "FactoryStructureDetailWindow";
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(DetailWindowMinWidth, 560.0f);
        Size = CustomMinimumSize;
        AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("0F172A"), new Color("60A5FA"), 2));

        var root = new VBoxContainer();
        root.MouseFilter = MouseFilterEnum.Stop;
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        var titleBar = new PanelContainer();
        titleBar.MouseFilter = MouseFilterEnum.Stop;
        titleBar.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("13253A"), new Color("93C5FD"), 1));
        titleBar.GuiInput += HandleTitleBarGuiInput;
        root.AddChild(titleBar);
        _titleBar = titleBar;

        var titleMargin = new MarginContainer();
        titleMargin.AddThemeConstantOverride("margin_left", 10);
        titleMargin.AddThemeConstantOverride("margin_top", 8);
        titleMargin.AddThemeConstantOverride("margin_right", 8);
        titleMargin.AddThemeConstantOverride("margin_bottom", 8);
        titleBar.AddChild(titleMargin);

        var titleRow = new HBoxContainer();
        titleRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleRow.AddThemeConstantOverride("separation", 8);
        titleMargin.AddChild(titleRow);

        var titleColumn = new VBoxContainer();
        titleColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleColumn.AddThemeConstantOverride("separation", 2);
        titleRow.AddChild(titleColumn);

        _titleLabel = CreateTextLabel(16, Colors.White);
        _subtitleLabel = CreateTextLabel(11, new Color("A5C8E1"));
        titleColumn.AddChild(_titleLabel);
        titleColumn.AddChild(_subtitleLabel);

        var closeButton = new Button
        {
            Text = "关闭",
            CustomMinimumSize = new Vector2(56.0f, 28.0f)
        };
        closeButton.Pressed += () =>
        {
            HideWindow();
            CloseRequested?.Invoke();
        };
        titleRow.AddChild(closeButton);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        scroll.ClipContents = true;
        scroll.CustomMinimumSize = new Vector2(480.0f, 0.0f);
        root.AddChild(scroll);
        _scroll = scroll;

        var bodyMargin = new MarginContainer();
        bodyMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bodyMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        bodyMargin.CustomMinimumSize = new Vector2(480.0f, 0.0f);
        bodyMargin.AddThemeConstantOverride("margin_left", 10);
        bodyMargin.AddThemeConstantOverride("margin_top", 8);
        bodyMargin.AddThemeConstantOverride("margin_right", 10);
        bodyMargin.AddThemeConstantOverride("margin_bottom", 10);
        scroll.AddChild(bodyMargin);
        _bodyMargin = bodyMargin;

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.CustomMinimumSize = new Vector2(450.0f, 0.0f);
        body.AddThemeConstantOverride("separation", 10);
        bodyMargin.AddChild(body);
        _body = body;

        _summaryLabel = CreateTextLabel(12, new Color("D7E6F2"));
        body.AddChild(_summaryLabel);

        _recipeSections = new VBoxContainer();
        _recipeSections.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _recipeSections.AddThemeConstantOverride("separation", 6);
        body.AddChild(_recipeSections);

        _inventorySections = new VBoxContainer();
        _inventorySections.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _inventorySections.AddThemeConstantOverride("separation", 8);
        body.AddChild(_inventorySections);

        _actionSections = new VBoxContainer();
        _actionSections.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _actionSections.AddThemeConstantOverride("separation", 6);
        body.AddChild(_actionSections);

        _dragStateLabel = CreateTextLabel(11, new Color("FDE68A"));
        body.AddChild(_dragStateLabel);

        var dragPreview = new PanelContainer
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            TopLevel = true,
            CustomMinimumSize = new Vector2(170.0f, 52.0f),
            ZIndex = 128
        };
        dragPreview.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.03f, 0.07f, 0.11f, 0.92f), new Color("38BDF8"), 2));
        AddChild(dragPreview);
        _dragPreview = dragPreview;

        var dragPreviewMargin = new MarginContainer();
        dragPreviewMargin.AddThemeConstantOverride("margin_left", 8);
        dragPreviewMargin.AddThemeConstantOverride("margin_top", 6);
        dragPreviewMargin.AddThemeConstantOverride("margin_right", 8);
        dragPreviewMargin.AddThemeConstantOverride("margin_bottom", 6);
        dragPreview.AddChild(dragPreviewMargin);

        var dragPreviewRow = new HBoxContainer();
        dragPreviewRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        dragPreviewRow.AddThemeConstantOverride("separation", 8);
        dragPreviewMargin.AddChild(dragPreviewRow);

        var dragPreviewIcon = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(28.0f, 28.0f),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        dragPreviewRow.AddChild(dragPreviewIcon);
        _dragPreviewIcon = dragPreviewIcon;

        var dragPreviewText = new VBoxContainer();
        dragPreviewText.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        dragPreviewText.AddThemeConstantOverride("separation", 2);
        dragPreviewRow.AddChild(dragPreviewText);

        _dragPreviewTitle = CreatePreviewTextLabel(11, Colors.White);
        _dragPreviewCount = CreatePreviewTextLabel(10, new Color("FDE68A"));
        dragPreviewText.AddChild(_dragPreviewTitle);
        dragPreviewText.AddChild(_dragPreviewCount);

        if (!s_instances.Contains(this))
        {
            s_instances.Add(this);
        }

        UpdateDragStateLabel();
    }

    public override void _ExitTree()
    {
        s_instances.Remove(this);
        if (_dragSourceSlot is not null || _pendingDragSourceSlot is not null)
        {
            CancelSharedInventoryDrag();
        }
    }

    public override void _Process(double delta)
    {
        UpdateHoveredSlotFromPointer();

        if (_pendingDragSourceSlot is not null)
        {
            if (!_leftMouseHeld)
            {
                ClearPendingDrag();
            }
            else
            {
                _pendingDragElapsed += delta;
                if (_pendingDragElapsed >= InventoryDragHoldSeconds)
                {
                    BeginInventoryDrag(_pendingDragSourceSlot);
                }
            }
        }

        if (_dragSourceSlot is not null)
        {
            UpdateDragPreview();
            if (!_leftMouseHeld)
            {
                FinalizeInventoryDrag();
            }
        }

        if (Visible)
        {
            SyncContentWidth();
            ClampToBounds();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        _leftMouseHeld = mouseButton.Pressed;
        if (!mouseButton.Pressed && HasSharedInventoryDrag && _dragSourceSlot is null && _pendingDragSourceSlot is null)
        {
            var pointer = GetViewport().GetMousePosition();
            var completed = TryCompleteSharedInventoryDropAtPointer(pointer, splitStack: IsSplitModifierHeld());
            TraceLog($"release with shared drag active and external source pointer={pointer} completed={completed}");
        }
    }

    public void SetDragBounds(Rect2 bounds)
    {
        _dragBounds = bounds;
        ClampToBounds();
    }

    public void ShowDetails(FactoryStructureDetailModel model, Vector2 defaultPosition)
    {
        var signature = model.BuildSignature();
        if (Size.X < CustomMinimumSize.X || Size.Y < CustomMinimumSize.Y)
        {
            Size = new Vector2(
                Mathf.Max(Size.X, CustomMinimumSize.X),
                Mathf.Max(Size.Y, CustomMinimumSize.Y));
        }

        if (!Visible)
        {
            Visible = true;
            if (!_windowMovedByUser)
            {
                Position = defaultPosition;
            }
        }

        MoveToFront();

        if (_modelSignature == signature)
        {
            ClampToBounds();
            return;
        }

        _modelSignature = signature;
        RebuildContent(model);
        SyncContentWidth();
        ClampToBounds();
    }

    public void HideWindow()
    {
        Visible = false;
        _modelSignature = null;
        _leftMouseHeld = false;
        CancelInventoryDrag();
        _hoveredSlot = null;
        _recipePickerPanel = null;
        _recipePickerList = null;
        UpdateDragStateLabel();
    }

    public bool BlocksInput(Control? control)
    {
        if (HasActiveInventoryInteraction)
        {
            return true;
        }

        if (control is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current == this)
            {
                return true;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        if (!Visible)
        {
            return false;
        }

        if (GetGlobalRect().HasPoint(screenPoint))
        {
            return true;
        }

        return _dragPreview?.Visible == true && _dragPreview.GetGlobalRect().HasPoint(screenPoint);
    }

    public static bool CanAcceptSharedInventoryDrop(
        string inventoryId,
        Vector2I slotPosition,
        FactoryItemKind? itemKind,
        int stackCount,
        int maxStackSize,
        string? itemLabelText)
    {
        return s_dragSourcePayload.HasValue
            && CanAcceptPayload(s_dragSourcePayload.Value, inventoryId, slotPosition, itemKind, stackCount, maxStackSize, itemLabelText);
    }

    public static bool TryCompleteSharedInventoryDrop(
        string inventoryId,
        Vector2I slotPosition,
        FactoryItemKind? itemKind,
        int stackCount,
        int maxStackSize,
        string? itemLabelText,
        bool splitStack)
    {
        if (!s_dragSourcePayload.HasValue
            || !CanAcceptPayload(s_dragSourcePayload.Value, inventoryId, slotPosition, itemKind, stackCount, maxStackSize, itemLabelText))
        {
            TraceLog($"shared drop rejected target={inventoryId}@{slotPosition} item={itemKind?.ToString() ?? "empty"} stack={stackCount}/{maxStackSize}");
            return false;
        }

        var source = s_dragSourcePayload.Value;
        TraceLog($"shared drop accepted source={source.InventoryId}@{source.SlotPosition} -> target={inventoryId}@{slotPosition} split={splitStack}");
        if (source.InventoryId == inventoryId)
        {
            s_sharedMoveHandler?.Invoke(inventoryId, source.SlotPosition, slotPosition, splitStack);
        }
        else
        {
            s_sharedTransferHandler?.Invoke(source.InventoryId, source.SlotPosition, inventoryId, slotPosition, splitStack);
        }

        CancelSharedInventoryDrag();
        return true;
    }

    public static bool TryCompleteSharedInventoryDropAtPointer(Vector2 pointer, bool splitStack)
    {
        var target = FindSlotUnderPointer(pointer);
        if (target is null)
        {
            TraceLog($"shared drop at pointer={pointer} found no inventory slot target");
            return false;
        }

        TraceLog($"shared drop at pointer={pointer} resolved target={target.InventoryId}@{target.SlotPosition}");
        return TryCompleteSharedInventoryDrop(
            target.InventoryId,
            target.SlotPosition,
            target.ItemKind,
            target.StackCount,
            target.MaxStackSize,
            target.ItemLabelText,
            splitStack);
    }

    public static void BeginSharedInventoryDrag(
        string inventoryId,
        Vector2I slotPosition,
        FactoryItemKind? itemKind,
        string itemLabelText,
        int stackCount,
        int maxStackSize,
        Texture2D? iconTexture,
        Color accentColor,
        Action<string, Vector2I, Vector2I, bool>? moveHandler,
        Action<string, Vector2I, string, Vector2I, bool>? transferHandler)
    {
        CancelSharedInventoryDrag();
        s_dragSourcePayload = new InventoryDragPayload(
            inventoryId,
            slotPosition,
            itemKind,
            itemLabelText,
            stackCount,
            maxStackSize,
            iconTexture,
            accentColor);
        s_sharedMoveHandler = moveHandler;
        s_sharedTransferHandler = transferHandler;
        TraceLog($"begin shared drag source={inventoryId}@{slotPosition} item={itemKind?.ToString() ?? "empty"} label={itemLabelText} count={stackCount}/{maxStackSize}");
    }

    public static void CancelSharedInventoryDrag()
    {
        if (s_dragSourcePayload.HasValue)
        {
            var source = s_dragSourcePayload.Value;
            TraceLog($"cancel shared drag source={source.InventoryId}@{source.SlotPosition}");
        }

        s_dragSourcePayload = null;
        s_sharedMoveHandler = null;
        s_sharedTransferHandler = null;
        for (var index = 0; index < s_instances.Count; index++)
        {
            if (GodotObject.IsInstanceValid(s_instances[index]))
            {
                s_instances[index].HandleSharedDragCancelled();
            }
        }
    }

    private void RebuildContent(FactoryStructureDetailModel model)
    {
        if (_titleLabel is null || _subtitleLabel is null || _summaryLabel is null || _inventorySections is null || _recipeSections is null)
        {
            return;
        }

        _titleLabel.Text = model.Title;
        _subtitleLabel.Text = model.Subtitle;
        _summaryLabel.Text = string.Join("\n", model.SummaryLines);

        RebuildInventorySections(model.InventorySections);
        RebuildRecipeSection(model.RecipeSection);
        RebuildActionSection(model.Actions);
        UpdateDragStateLabel();
    }

    private void RebuildInventorySections(IReadOnlyList<FactoryInventorySectionModel> sections)
    {
        if (_inventorySections is null)
        {
            return;
        }

        var pendingDragReference = CaptureSlotReference(_pendingDragSourceSlot);
        var dragReference = CaptureSlotReference(_dragSourceSlot);
        var hoveredReference = CaptureSlotReference(_hoveredSlot);
        var pendingDragElapsed = _pendingDragElapsed;

        foreach (var child in _inventorySections.GetChildren())
        {
            child.QueueFree();
        }

        _slotWidgets.Clear();
        _inventoryLayouts.Clear();
        _hoveredSlot = null;

        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            var title = CreateTextLabel(13, new Color("FDE68A"));
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            title.Text = section.Title;
            _inventorySections.AddChild(title);

            var grid = new GridContainer();
            grid.Columns = Mathf.Max(1, section.GridSize.X);
            grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            grid.AddThemeConstantOverride("h_separation", (int)InventoryGridGap);
            grid.AddThemeConstantOverride("v_separation", (int)InventoryGridGap);
            _inventorySections.AddChild(grid);

            var layout = new InventoryGridLayout
            {
                Grid = grid,
                Columns = grid.Columns,
                SlotPanels = new List<PanelContainer>()
            };
            _inventoryLayouts.Add(layout);

            for (var slotIndex = 0; slotIndex < section.Slots.Count; slotIndex++)
            {
                var slot = section.Slots[slotIndex];
                var slotPanel = new PanelContainer();
                slotPanel.MouseFilter = MouseFilterEnum.Stop;
                slotPanel.CustomMinimumSize = new Vector2(InventorySlotPreferredSize, InventorySlotPreferredSize);
                grid.AddChild(slotPanel);
                layout.SlotPanels.Add(slotPanel);

                var margin = new MarginContainer();
                margin.MouseFilter = MouseFilterEnum.Ignore;
                margin.AddThemeConstantOverride("margin_left", 4);
                margin.AddThemeConstantOverride("margin_top", 4);
                margin.AddThemeConstantOverride("margin_right", 4);
                margin.AddThemeConstantOverride("margin_bottom", 4);
                slotPanel.AddChild(margin);

                var body = new VBoxContainer();
                body.MouseFilter = MouseFilterEnum.Ignore;
                body.AddThemeConstantOverride("separation", 1);
                margin.AddChild(body);

                var iconFrame = new PanelContainer();
                iconFrame.MouseFilter = MouseFilterEnum.Ignore;
                iconFrame.CustomMinimumSize = new Vector2(24.0f, 24.0f);
                iconFrame.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                body.AddChild(iconFrame);

                var iconMargin = new MarginContainer();
                iconMargin.MouseFilter = MouseFilterEnum.Ignore;
                iconMargin.SetAnchorsPreset(LayoutPreset.FullRect);
                iconMargin.AddThemeConstantOverride("margin_left", 2);
                iconMargin.AddThemeConstantOverride("margin_top", 2);
                iconMargin.AddThemeConstantOverride("margin_right", 2);
                iconMargin.AddThemeConstantOverride("margin_bottom", 2);
                iconFrame.AddChild(iconMargin);

                var iconRect = new TextureRect();
                iconRect.MouseFilter = MouseFilterEnum.Ignore;
                iconRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
                iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                iconRect.CustomMinimumSize = new Vector2(18.0f, 18.0f);
                iconRect.Texture = slot.IconTexture;
                iconRect.Visible = slot.IconTexture is not null;
                iconMargin.AddChild(iconRect);

                var itemLabel = CreateTextLabel(9, slot.HasItem ? Colors.White : new Color("7B8DA1"));
                itemLabel.AutowrapMode = TextServer.AutowrapMode.Off;
                itemLabel.ClipText = true;
                itemLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
                itemLabel.Text = slot.HasItem ? slot.ItemLabel ?? string.Empty : "空槽位";
                body.AddChild(itemLabel);

                var stackLabel = CreateTextLabel(9, slot.HasItem ? new Color("FDE68A") : new Color("64748B"));
                stackLabel.Text = slot.HasItem ? $"{slot.StackCount}/{slot.MaxStackSize}" : "--";
                body.AddChild(stackLabel);

                var posLabel = CreateTextLabel(1, new Color("9FB6C9"));
                posLabel.Visible = false;

                slotPanel.TooltipText = slot.ItemDescription ?? string.Empty;

                var widget = new InventorySlotWidget
                {
                    Panel = slotPanel,
                    InventoryId = section.InventoryId,
                    SlotPosition = slot.Position,
                    HasItem = slot.HasItem,
                    ItemKind = slot.ItemKind,
                    ItemLabelText = slot.ItemLabel ?? string.Empty,
                    StackCount = slot.StackCount,
                    MaxStackSize = slot.MaxStackSize,
                    Icon = iconRect,
                    Label = itemLabel,
                    StackLabel = stackLabel,
                    SubLabel = posLabel,
                    AccentColor = slot.AccentColor
                };

                slotPanel.GuiInput += @event => HandleSlotGuiInput(widget, section.AllowItemMove, @event);
                _slotWidgets.Add(widget);
            }
        }

        _inventoryLayoutRevision++;
        RestoreInventoryInteractionState(pendingDragReference, dragReference, hoveredReference, pendingDragElapsed);
        RefreshSlotVisuals();
    }

    private void RebuildRecipeSection(FactoryRecipeSectionModel? section)
    {
        if (_recipeSections is null)
        {
            return;
        }

        foreach (var child in _recipeSections.GetChildren())
        {
            child.QueueFree();
        }

        if (section is null)
        {
            _recipePickerPanel = null;
            _recipePickerList = null;
            return;
        }

        var title = CreateTextLabel(13, new Color("FDE68A"));
        title.Text = section.Title;
        _recipeSections.AddChild(title);

        if (!string.IsNullOrWhiteSpace(section.Description))
        {
            var description = CreateTextLabel(11, new Color("A5C8E1"));
            description.Text = section.Description;
            _recipeSections.AddChild(description);
        }

        FactoryRecipeOptionModel? activeOption = null;
        for (var index = 0; index < section.Options.Count; index++)
        {
            var option = section.Options[index];
            if (option.IsActive)
            {
                activeOption = option;
                break;
            }
        }

        activeOption ??= section.Options.Count > 0 ? section.Options[0] : null;
        if (activeOption is null)
        {
            _recipePickerPanel = null;
            _recipePickerList = null;
            return;
        }

        var triggerRow = new HBoxContainer();
        triggerRow.AddThemeConstantOverride("separation", 8);
        _recipeSections.AddChild(triggerRow);

        var triggerCard = CreateRecipeCard(activeOption, isCompact: true, isActive: true);
        triggerCard.TooltipText = $"当前配方：{activeOption.DisplayName}\n点击打开配方列表";
        triggerCard.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton mouseButton
                && mouseButton.ButtonIndex == MouseButton.Left
                && mouseButton.Pressed
                && _recipePickerPanel is not null)
            {
                _recipePickerPanel.Visible = !_recipePickerPanel.Visible;
                MoveToFront();
            }
        };
        triggerRow.AddChild(triggerCard);

        var activeRecipeText = new VBoxContainer();
        activeRecipeText.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        activeRecipeText.AddThemeConstantOverride("separation", 2);
        triggerRow.AddChild(activeRecipeText);

        var activeTitle = CreateTextLabel(12, Colors.White);
        activeTitle.Text = $"当前配方：{activeOption.DisplayName}";
        activeRecipeText.AddChild(activeTitle);

        var activeSummary = CreateTextLabel(10, new Color("A5C8E1"));
        activeSummary.Text = activeOption.Summary;
        activeRecipeText.AddChild(activeSummary);

        var activeHint = CreateTextLabel(10, new Color("93C5FD"));
        activeHint.Text = "点击左侧图标打开配方面板";
        activeRecipeText.AddChild(activeHint);

        var pickerPanel = new PanelContainer();
        pickerPanel.Visible = false;
        pickerPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("10243A"), new Color("4DA8DA"), 1));
        _recipeSections.AddChild(pickerPanel);
        _recipePickerPanel = pickerPanel;

        var pickerMargin = new MarginContainer();
        pickerMargin.AddThemeConstantOverride("margin_left", 8);
        pickerMargin.AddThemeConstantOverride("margin_top", 8);
        pickerMargin.AddThemeConstantOverride("margin_right", 8);
        pickerMargin.AddThemeConstantOverride("margin_bottom", 8);
        pickerPanel.AddChild(pickerMargin);

        var pickerBody = new VBoxContainer();
        pickerBody.AddThemeConstantOverride("separation", 6);
        pickerMargin.AddChild(pickerBody);

        var pickerHeader = new HBoxContainer();
        pickerHeader.AddThemeConstantOverride("separation", 6);
        pickerBody.AddChild(pickerHeader);

        var pickerTitle = CreateTextLabel(12, new Color("FDE68A"));
        pickerTitle.Text = $"{section.Title} 列表";
        pickerTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        pickerHeader.AddChild(pickerTitle);

        var closeButton = new Button
        {
            Text = "收起",
            CustomMinimumSize = new Vector2(56.0f, 26.0f)
        };
        closeButton.Pressed += () =>
        {
            if (_recipePickerPanel is not null)
            {
                _recipePickerPanel.Visible = false;
            }
        };
        pickerHeader.AddChild(closeButton);

        if (!string.IsNullOrWhiteSpace(section.Description))
        {
            var pickerDescription = CreateTextLabel(10, new Color("9FB6C9"));
            pickerDescription.Text = section.Description;
            pickerBody.AddChild(pickerDescription);
        }

        var pickerList = new VBoxContainer();
        pickerList.AddThemeConstantOverride("separation", 6);
        pickerBody.AddChild(pickerList);
        _recipePickerList = pickerList;

        for (var index = 0; index < section.Options.Count; index++)
        {
            var option = section.Options[index];
            var optionCard = CreateRecipeCard(option, isCompact: false, isActive: option.IsActive);
            optionCard.TooltipText = option.Summary;
            optionCard.GuiInput += @event =>
            {
                if (@event is InputEventMouseButton mouseButton
                    && mouseButton.ButtonIndex == MouseButton.Left
                    && mouseButton.Pressed)
                {
                    if (_recipePickerPanel is not null)
                    {
                        _recipePickerPanel.Visible = false;
                    }

                    RecipeSelected?.Invoke(option.RecipeId);
                }
            };
            pickerList.AddChild(optionCard);
        }
    }

    private void RebuildActionSection(IReadOnlyList<FactoryDetailActionModel> actions)
    {
        if (_actionSections is null)
        {
            return;
        }

        foreach (var child in _actionSections.GetChildren())
        {
            child.QueueFree();
        }

        if (actions.Count == 0)
        {
            return;
        }

        var title = CreateTextLabel(13, new Color("FDE68A"));
        title.Text = "操作";
        _actionSections.AddChild(title);

        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            var button = new Button
            {
                Text = action.Label,
                Disabled = !action.IsEnabled,
                CustomMinimumSize = new Vector2(0.0f, 34.0f)
            };
            button.Alignment = HorizontalAlignment.Left;
            button.Pressed += () => DetailActionRequested?.Invoke(action.ActionId);
            _actionSections.AddChild(button);

            if (!string.IsNullOrWhiteSpace(action.Description))
            {
                var description = CreateTextLabel(10, new Color("9FB6C9"));
                description.Text = action.Description;
                _actionSections.AddChild(description);
            }
        }
    }

    private PanelContainer CreateRecipeCard(FactoryRecipeOptionModel option, bool isCompact, bool isActive)
    {
        var panel = new PanelContainer();
        panel.MouseFilter = MouseFilterEnum.Stop;
        panel.CustomMinimumSize = isCompact ? new Vector2(54.0f, 54.0f) : new Vector2(0.0f, 66.0f);
        panel.SizeFlagsHorizontal = isCompact ? SizeFlags.ShrinkBegin : SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            "panel",
            CreatePanelStyle(
                isActive ? new Color("13253A") : new Color("0F172A"),
                isActive ? option.AccentColor.Lightened(0.12f) : new Color("475569"),
                isActive ? 2 : 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        if (isCompact)
        {
            margin.AddChild(CreateIconRect(option.IconTexture, option.AccentColor, new Vector2(38.0f, 38.0f)));
            return panel;
        }

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        margin.AddChild(row);

        row.AddChild(CreateIconRect(option.IconTexture, option.AccentColor, new Vector2(42.0f, 42.0f)));

        var textColumn = new VBoxContainer();
        textColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        textColumn.AddThemeConstantOverride("separation", 2);
        row.AddChild(textColumn);

        var title = CreateTextLabel(11, isActive ? Colors.White : new Color("D7E6F2"));
        title.Text = option.DisplayName;
        textColumn.AddChild(title);

        var summary = CreateTextLabel(10, new Color("9FB6C9"));
        summary.Text = option.Summary;
        textColumn.AddChild(summary);

        if (isActive)
        {
            var badge = CreateTextLabel(10, option.AccentColor.Lightened(0.2f));
            badge.Text = "当前配方";
            textColumn.AddChild(badge);
        }

        return panel;
    }

    private static Control CreateIconRect(Texture2D? texture, Color accentColor, Vector2 size)
    {
        var host = new CenterContainer();
        host.MouseFilter = MouseFilterEnum.Ignore;
        host.CustomMinimumSize = size;
        host.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        host.SizeFlagsVertical = SizeFlags.ShrinkCenter;

        var iconRect = new TextureRect();
        iconRect.MouseFilter = MouseFilterEnum.Ignore;
        iconRect.CustomMinimumSize = size;
        iconRect.Texture = texture;
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconRect.Modulate = texture is null ? accentColor : Colors.White;
        host.AddChild(iconRect);
        return host;
    }

    private void HandleTitleBarGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                _draggingWindow = true;
                _windowDragOffset = mouseButton.Position;
                MoveToFront();
            }
            else
            {
                _draggingWindow = false;
                ClampToBounds();
            }

            return;
        }

        if (@event is InputEventMouseMotion motion && _draggingWindow)
        {
            Position += motion.Relative;
            _windowMovedByUser = true;
            ClampToBounds();
        }
    }

    private void HandleSlotGuiInput(InventorySlotWidget widget, bool allowMove, InputEvent @event)
    {
        if (!allowMove || @event is not InputEventMouseButton mouseButton || mouseButton.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        if (mouseButton.Pressed)
        {
            if (!widget.HasItem)
            {
                TraceLog($"slot press ignored empty slot target={widget.InventoryId}@{widget.SlotPosition}");
                return;
            }

            _pendingDragSourceSlot = widget;
            _pendingDragElapsed = 0.0;
            _hoveredSlot = widget;
            TraceLog($"slot press pending drag source={widget.InventoryId}@{widget.SlotPosition} item={widget.ItemKind?.ToString() ?? "empty"} count={widget.StackCount}/{widget.MaxStackSize}");
            RefreshSlotVisuals();
            UpdateDragStateLabel();
            CallDeferred(nameof(EmitInventorySlotActivatedDeferred), widget.InventoryId, widget.SlotPosition);
            return;
        }
    }

    private void EmitInventorySlotActivatedDeferred(string inventoryId, Vector2I slotPosition)
    {
        InventorySlotActivated?.Invoke(inventoryId, slotPosition);
    }

    private void RefreshSlotVisuals()
    {
        var activeDragPayload = GetActiveDragPayload();
        for (var index = 0; index < _slotWidgets.Count; index++)
        {
            var widget = _slotWidgets[index];
            var borderColor = widget.HasItem ? widget.AccentColor : new Color("475569");
            var backgroundColor = widget.HasItem ? new Color("172236") : new Color("0F172A");
            var borderWidth = 1;

            if (_dragSourceSlot is not null && widget == _dragSourceSlot)
            {
                borderColor = new Color("38BDF8");
                backgroundColor = new Color("10243C");
                borderWidth = 2;
            }
            else if (_pendingDragSourceSlot is not null && widget == _pendingDragSourceSlot)
            {
                borderColor = new Color("7DD3FC");
                backgroundColor = new Color("12253A");
                borderWidth = 2;
            }
            else if (activeDragPayload.HasValue
                && _hoveredSlot == widget
                && CanAcceptPayload(activeDragPayload.Value, widget))
            {
                borderColor = new Color("4ADE80");
                backgroundColor = new Color("122A23");
                borderWidth = 2;
            }
            else if (_hoveredSlot == widget)
            {
                borderColor = new Color("93C5FD");
            }

            widget.Panel.AddThemeStyleboxOverride("panel", CreateInventorySlotStyle(backgroundColor, borderColor, borderWidth));
            widget.Label.Modulate = widget.HasItem ? Colors.White : new Color("7B8DA1");
            widget.StackLabel.Modulate = widget.HasItem ? new Color("FDE68A") : new Color("64748B");
            widget.SubLabel.Modulate = new Color("9FB6C9");
            widget.Icon.Modulate = widget.HasItem ? Colors.White : new Color(0.55f, 0.62f, 0.70f, 0.72f);
        }
    }

    private void UpdateDragStateLabel()
    {
        if (_dragStateLabel is null)
        {
            return;
        }

        if (_pendingDragSourceSlot is not null)
        {
            var splitText = IsSplitModifierHeld() ? "当前为半堆拖拽" : "当前为整堆拖拽";
            _dragStateLabel.Text = $"长按左键开始拖拽；按住 Ctrl 可拖出一半。源槽位 ({_pendingDragSourceSlot.SlotPosition.X}, {_pendingDragSourceSlot.SlotPosition.Y})，{splitText}";
            return;
        }

        if (_dragSourceSlot is null)
        {
            if (HasSharedInventoryDrag && _hoveredSlot is not null && s_dragSourcePayload.HasValue)
            {
                var action = CanAcceptPayload(s_dragSourcePayload.Value, _hoveredSlot)
                    ? _hoveredSlot.HasItem
                        ? "可并入当前堆叠"
                        : "可放入当前槽位"
                    : "当前槽位不接受这次拖拽";
                _dragStateLabel.Text = $"跨面板拖拽中：目标槽位 ({_hoveredSlot.SlotPosition.X}, {_hoveredSlot.SlotPosition.Y})，{action}。";
                return;
            }

            _dragStateLabel.Text = "拖动窗口标题可移动位置；长按左键拖动物品到空槽位或同类未满堆叠，按住 Ctrl 可拖出一半。";
            return;
        }

        if (_hoveredSlot is not null)
        {
            var action = CanAcceptPayload(CreateDragPayload(_dragSourceSlot), _hoveredSlot)
                ? _hoveredSlot.HasItem
                    ? "并入目标堆叠"
                    : "移动到空槽位"
                : "目标无效";
            var splitText = IsSplitModifierHeld() ? $"半堆 {GetRequestedMoveCount(_dragSourceSlot)}" : $"整堆 {GetRequestedMoveCount(_dragSourceSlot)}";
            _dragStateLabel.Text = $"正在拖动物品：从 ({_dragSourceSlot.SlotPosition.X}, {_dragSourceSlot.SlotPosition.Y}) 移向 ({_hoveredSlot.SlotPosition.X}, {_hoveredSlot.SlotPosition.Y})，{action}，{splitText}";
            return;
        }

        _dragStateLabel.Text = $"正在拖动物品：源槽位 ({_dragSourceSlot.SlotPosition.X}, {_dragSourceSlot.SlotPosition.Y})，数量 {GetRequestedMoveCount(_dragSourceSlot)}";
    }

    private void BeginInventoryDrag(InventorySlotWidget source)
    {
        _pendingDragSourceSlot = null;
        _pendingDragElapsed = 0.0;
        TraceLog($"begin inventory drag source={source.InventoryId}@{source.SlotPosition} item={source.ItemKind?.ToString() ?? "empty"} count={source.StackCount}/{source.MaxStackSize}");
        BeginSharedInventoryDrag(
            source.InventoryId,
            source.SlotPosition,
            source.ItemKind,
            source.ItemLabelText,
            source.StackCount,
            source.MaxStackSize,
            source.Icon.Texture,
            source.AccentColor,
            (inventoryId, fromSlot, toSlot, splitStack) => InventoryMoveRequested?.Invoke(inventoryId, fromSlot, toSlot, splitStack),
            (fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack) => InventoryTransferRequested?.Invoke(fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack));
        _dragSourceSlot = source;
        MoveToFront();
        RefreshSlotVisuals();
        UpdateDragPreview();
        UpdateDragStateLabel();
    }

    private void FinalizeInventoryDrag()
    {
        if (_dragSourceSlot is not null
            && s_dragSourcePayload.HasValue)
        {
            var target = FindSlotUnderPointer(GetGlobalMousePosition());
            TraceLog($"finalize inventory drag source={_dragSourceSlot.InventoryId}@{_dragSourceSlot.SlotPosition} target={(target is null ? "none" : $"{target.InventoryId}@{target.SlotPosition}")}");
            if (target is not null && CanAcceptPayload(s_dragSourcePayload.Value, target))
            {
                if (_dragSourceSlot.InventoryId == target.InventoryId)
                {
                    TraceLog("finalize inventory drag -> same inventory move");
                    InventoryMoveRequested?.Invoke(
                        target.InventoryId,
                        _dragSourceSlot.SlotPosition,
                        target.SlotPosition,
                        IsSplitModifierHeld());
                }
                else
                {
                    TraceLog("finalize inventory drag -> cross inventory transfer");
                    InventoryTransferRequested?.Invoke(
                        _dragSourceSlot.InventoryId,
                        _dragSourceSlot.SlotPosition,
                        target.InventoryId,
                        target.SlotPosition,
                        IsSplitModifierHeld());
                }
            }
        }

        CancelInventoryDrag();
    }

    private void ClearPendingDrag()
    {
        if (_pendingDragSourceSlot is not null)
        {
            TraceLog($"clear pending drag source={_pendingDragSourceSlot.InventoryId}@{_pendingDragSourceSlot.SlotPosition}");
        }

        _pendingDragSourceSlot = null;
        _pendingDragElapsed = 0.0;
        UpdateHoveredSlotFromPointer();
        RefreshSlotVisuals();
        UpdateDragStateLabel();
    }

    private void CancelInventoryDrag()
    {
        var hadLocalDragSource = _dragSourceSlot is not null;
        _pendingDragSourceSlot = null;
        _pendingDragElapsed = 0.0;
        _dragSourceSlot = null;
        if (_dragPreview is not null)
        {
            _dragPreview.Visible = false;
        }

        UpdateHoveredSlotFromPointer();
        RefreshSlotVisuals();
        UpdateDragStateLabel();

        if (hadLocalDragSource)
        {
            CancelSharedInventoryDrag();
        }
    }

    private void UpdateDragPreview()
    {
        if (_dragPreview is null || _dragPreviewIcon is null || _dragPreviewTitle is null || _dragPreviewCount is null || _dragSourceSlot is null)
        {
            return;
        }

        _dragPreview.Visible = true;
        _dragPreview.Position = GetGlobalMousePosition() + DragPreviewOffset;
        _dragPreviewIcon.Texture = _dragSourceSlot.Icon.Texture;
        _dragPreviewIcon.Modulate = _dragSourceSlot.Icon.Texture is null ? _dragSourceSlot.AccentColor : Colors.White;
        _dragPreviewTitle.Text = _dragSourceSlot.Label.Text;
        _dragPreviewCount.Text = IsSplitModifierHeld()
            ? $"分半拖拽 {GetRequestedMoveCount(_dragSourceSlot)}"
            : $"整堆拖拽 {GetRequestedMoveCount(_dragSourceSlot)}";
    }

    private void UpdateHoveredSlotFromPointer()
    {
        var hoveredSlot = GetSlotUnderPointer();
        if (_hoveredSlot == hoveredSlot)
        {
            return;
        }

        _hoveredSlot = hoveredSlot;
        RefreshSlotVisuals();
        UpdateDragStateLabel();
    }

    private InventorySlotWidget? GetSlotUnderPointer()
    {
        return GetSlotUnderPointer(GetGlobalMousePosition());
    }

    private InventorySlotWidget? GetSlotUnderPointer(Vector2 pointer)
    {
        if (!Visible)
        {
            return null;
        }

        for (var index = 0; index < _slotWidgets.Count; index++)
        {
            var widget = _slotWidgets[index];
            if (widget.Panel.GetGlobalRect().HasPoint(pointer))
            {
                return widget;
            }
        }

        return null;
    }

    private InventorySlotReference? CaptureSlotReference(InventorySlotWidget? widget)
    {
        if (widget is null)
        {
            return null;
        }

        return new InventorySlotReference(widget.InventoryId, widget.SlotPosition, widget.ItemKind);
    }

    private InventorySlotWidget? ResolveSlotReference(InventorySlotReference? slotReference, bool requireMatchingItem)
    {
        if (slotReference is null)
        {
            return null;
        }

        for (var index = 0; index < _slotWidgets.Count; index++)
        {
            var widget = _slotWidgets[index];
            if (widget.InventoryId != slotReference.Value.InventoryId || widget.SlotPosition != slotReference.Value.SlotPosition)
            {
                continue;
            }

            if (!requireMatchingItem)
            {
                return widget;
            }

            if (!widget.HasItem || widget.ItemKind != slotReference.Value.ItemKind)
            {
                return null;
            }

            return widget;
        }

        return null;
    }

    private void RestoreInventoryInteractionState(
        InventorySlotReference? pendingDragReference,
        InventorySlotReference? dragReference,
        InventorySlotReference? hoveredReference,
        double pendingDragElapsed)
    {
        _pendingDragSourceSlot = ResolveSlotReference(pendingDragReference, requireMatchingItem: true);
        _dragSourceSlot = ResolveSlotReference(dragReference, requireMatchingItem: true);
        _hoveredSlot = ResolveSlotReference(hoveredReference, requireMatchingItem: false) ?? GetSlotUnderPointer();
        _pendingDragElapsed = _pendingDragSourceSlot is null ? 0.0 : pendingDragElapsed;

        if (_dragSourceSlot is null && _dragPreview is not null)
        {
            _dragPreview.Visible = false;
        }

        if (_dragSourceSlot is null && dragReference is not null)
        {
            _pendingDragSourceSlot = null;
            _pendingDragElapsed = 0.0;
        }

        UpdateDragStateLabel();
    }

    private static bool IsSplitModifierHeld()
    {
        return Input.IsKeyPressed(Key.Ctrl);
    }

    private InventoryDragPayload? GetActiveDragPayload()
    {
        if (_dragSourceSlot is not null)
        {
            return CreateDragPayload(_dragSourceSlot);
        }

        return s_dragSourcePayload;
    }

    private static InventoryDragPayload CreateDragPayload(InventorySlotWidget widget)
    {
        return new InventoryDragPayload(
            widget.InventoryId,
            widget.SlotPosition,
            widget.ItemKind,
            widget.ItemLabelText,
            widget.StackCount,
            widget.MaxStackSize,
            widget.Icon.Texture,
            widget.AccentColor);
    }

    private static bool CanAcceptPayload(InventoryDragPayload source, InventorySlotWidget target)
    {
        return CanAcceptPayload(
            source,
            target.InventoryId,
            target.SlotPosition,
            target.ItemKind,
            target.StackCount,
            target.MaxStackSize,
            target.ItemLabelText);
    }

    private static bool CanAcceptPayload(
        InventoryDragPayload source,
        string inventoryId,
        Vector2I slotPosition,
        FactoryItemKind? itemKind,
        int stackCount,
        int maxStackSize,
        string? itemLabelText)
    {
        if (source.InventoryId == inventoryId && source.SlotPosition == slotPosition)
        {
            return false;
        }

        if (!itemKind.HasValue || stackCount <= 0 || maxStackSize <= 0)
        {
            return true;
        }

        if (!source.ItemKind.HasValue)
        {
            return false;
        }

        var canMerge = itemKind.Value == source.ItemKind.Value
            && stackCount < maxStackSize
            && (itemKind.Value != FactoryItemKind.BuildingKit
                || string.Equals(itemLabelText ?? string.Empty, source.ItemLabelText, StringComparison.Ordinal));
        if (canMerge)
        {
            return true;
        }

        return !IsSplitModifierHeld() && source.StackCount > 0 && stackCount > 0;
    }

    private static InventorySlotWidget? FindSlotUnderPointer(Vector2 pointer)
    {
        for (var index = s_instances.Count - 1; index >= 0; index--)
        {
            var window = s_instances[index];
            if (!GodotObject.IsInstanceValid(window))
            {
                continue;
            }

            var slot = window.GetSlotUnderPointer(pointer);
            if (slot is not null)
            {
                return slot;
            }
        }

        return null;
    }

    private static int GetRequestedMoveCount(InventorySlotWidget widget)
    {
        if (!widget.HasItem)
        {
            return 0;
        }

        if (!IsSplitModifierHeld() || widget.StackCount <= 1)
        {
            return widget.StackCount;
        }

        return Mathf.Max(1, Mathf.CeilToInt(widget.StackCount * 0.5f));
    }

    private void HandleSharedDragCancelled()
    {
        if (_dragSourceSlot is not null || _pendingDragSourceSlot is not null)
        {
            TraceLog($"handle shared drag cancelled dragSource={_dragSourceSlot?.InventoryId}@{_dragSourceSlot?.SlotPosition} pending={_pendingDragSourceSlot?.InventoryId}@{_pendingDragSourceSlot?.SlotPosition}");
        }

        _pendingDragSourceSlot = null;
        _pendingDragElapsed = 0.0;
        _dragSourceSlot = null;
        if (_dragPreview is not null)
        {
            _dragPreview.Visible = false;
        }

        UpdateHoveredSlotFromPointer();
        RefreshSlotVisuals();
        UpdateDragStateLabel();
    }

    private void ClampToBounds()
    {
        if (!Visible)
        {
            return;
        }

        var maxX = Mathf.Max(_dragBounds.Position.X, _dragBounds.Position.X + _dragBounds.Size.X - Size.X);
        var maxY = Mathf.Max(_dragBounds.Position.Y, _dragBounds.Position.Y + _dragBounds.Size.Y - Size.Y);
        Position = new Vector2(
            Mathf.Clamp(Position.X, _dragBounds.Position.X, maxX),
            Mathf.Clamp(Position.Y, _dragBounds.Position.Y, maxY));
    }

    private void SyncContentWidth()
    {
        if (_scroll is null || _bodyMargin is null || _body is null)
        {
            return;
        }

        var viewportWidth = GetViewport().GetVisibleRect().Size.X;
        var maxWindowWidth = Mathf.Max(180.0f, viewportWidth - DetailWindowViewportMargin);
        var minWindowWidth = Mathf.Min(DetailWindowMinWidth, maxWindowWidth);

        var maxColumns = 0;
        for (var index = 0; index < _inventoryLayouts.Count; index++)
        {
            maxColumns = Mathf.Max(maxColumns, _inventoryLayouts[index].Columns);
        }

        var maxBodyWidth = Mathf.Max(140.0f, maxWindowWidth - DetailWindowHorizontalChrome);
        var resolvedSlotSize = InventorySlotPreferredSize;
        if (maxColumns > 0)
        {
            var maxBodyWidthForSlots = maxBodyWidth - (Mathf.Max(0, maxColumns - 1) * InventoryGridGap);
            resolvedSlotSize = Mathf.Clamp(maxBodyWidthForSlots / maxColumns, InventorySlotMinSize, InventorySlotPreferredSize);
        }

        var bodyWidth = maxColumns > 0
            ? (maxColumns * resolvedSlotSize) + (Mathf.Max(0, maxColumns - 1) * InventoryGridGap)
            : Mathf.Max(140.0f, DetailWindowMinWidth - DetailWindowHorizontalChrome);
        var preferredWindowWidth = Mathf.Max(DetailWindowMinWidth, bodyWidth + DetailWindowHorizontalChrome);
        var resolvedWindowWidth = Mathf.Clamp(preferredWindowWidth, minWindowWidth, maxWindowWidth);
        var availableWidth = Mathf.Max(160.0f, resolvedWindowWidth - 20.0f);

        if (_inventoryLayoutRevision == _lastSyncedInventoryLayoutRevision
            && Mathf.IsEqualApprox(resolvedWindowWidth, _lastSyncedWindowWidth)
            && Mathf.IsEqualApprox(bodyWidth, _lastSyncedBodyWidth))
        {
            return;
        }

        if (!Mathf.IsEqualApprox(Size.X, resolvedWindowWidth))
        {
            Size = new Vector2(resolvedWindowWidth, Size.Y);
        }

        _scroll.CustomMinimumSize = new Vector2(availableWidth, 0.0f);
        _bodyMargin.CustomMinimumSize = new Vector2(availableWidth, 0.0f);
        _body.CustomMinimumSize = new Vector2(bodyWidth, 0.0f);

        for (var layoutIndex = 0; layoutIndex < _inventoryLayouts.Count; layoutIndex++)
        {
            var layout = _inventoryLayouts[layoutIndex];
            var columns = Mathf.Max(1, layout.Columns);
            var gridWidth = (columns * resolvedSlotSize) + (Mathf.Max(0, columns - 1) * InventoryGridGap);
            layout.Grid.CustomMinimumSize = new Vector2(gridWidth, 0.0f);

            for (var slotIndex = 0; slotIndex < layout.SlotPanels.Count; slotIndex++)
            {
                layout.SlotPanels[slotIndex].CustomMinimumSize = new Vector2(resolvedSlotSize, resolvedSlotSize);
            }
        }

        _lastSyncedInventoryLayoutRevision = _inventoryLayoutRevision;
        _lastSyncedWindowWidth = resolvedWindowWidth;
        _lastSyncedBodyWidth = bodyWidth;
    }

    private static Label CreateTextLabel(int fontSize, Color color)
    {
        var label = new Label();
        label.MouseFilter = MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Modulate = color;
        return label;
    }

    private static Label CreatePreviewTextLabel(int fontSize, Color color)
    {
        var label = CreateTextLabel(fontSize, color);
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.ClipText = true;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return label;
    }

    private static StyleBoxFlat CreatePanelStyle(Color backgroundColor, Color borderColor, int borderWidth)
    {
        return new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = borderColor,
            BorderWidthBottom = borderWidth,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginBottom = 2,
            ContentMarginLeft = 2,
            ContentMarginRight = 2,
            ContentMarginTop = 2
        };
    }

    private static StyleBoxFlat CreateInventorySlotStyle(Color backgroundColor, Color borderColor, int borderWidth)
    {
        return new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = borderColor,
            BorderWidthBottom = borderWidth,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            ContentMarginBottom = 1,
            ContentMarginLeft = 1,
            ContentMarginRight = 1,
            ContentMarginTop = 1
        };
    }
}
