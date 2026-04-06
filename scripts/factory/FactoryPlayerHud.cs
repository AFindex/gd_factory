using Godot;
using System;
using System.Collections.Generic;

public partial class FactoryPlayerHud : CanvasLayer
{
    private const double HotbarDragHoldSeconds = 0.18;
    private static void TraceLog(string message) => GD.Print($"[FactoryPlayerHud] {message}");

    private sealed class HotbarSlotWidget
    {
        public required int Index { get; init; }
        public required PanelContainer Panel { get; init; }
        public required TextureRect Icon { get; init; }
        public required Label IndexLabel { get; init; }
        public required Label NameLabel { get; init; }
        public required Label CountLabel { get; init; }
    }

    private readonly List<HotbarSlotWidget> _hotbarSlots = new();

    private Control? _root;
    private PanelContainer? _barPanel;
    private HBoxContainer? _buttonRow;
    private Label? _statusLabel;
    private PanelContainer? _hotbarDragPreview;
    private TextureRect? _hotbarDragPreviewIcon;
    private Label? _hotbarDragPreviewTitle;
    private Label? _hotbarDragPreviewCount;
    private FactoryStructureDetailWindow? _backpackWindow;
    private FactoryStructureDetailWindow? _itemInfoWindow;
    private FactoryStructureDetailWindow? _statsWindow;

    private FactoryPlayerController? _player;
    private FactoryStructureDetailModel? _linkedStructureModel;
    private FactoryItem? _selectedItem;
    private bool _backpackVisible = false;
    private bool _itemInfoVisible = false;
    private bool _statsVisible = false;
    private int _pendingHotbarDragIndex = -1;
    private int _activeHotbarDragIndex = -1;
    private double _pendingHotbarDragElapsed;
    private bool _leftMouseHeld;
    private string? _lastBackpackWindowSignature;
    private string? _lastItemInfoWindowSignature;
    private string? _lastStatsWindowSignature;

    public event Action<int>? HotbarSlotPressed;
    public event Action<string, Vector2I, Vector2I, bool>? BackpackInventoryMoveRequested;
    public event Action<string, Vector2I, string, Vector2I, bool>? BackpackInventoryTransferRequested;
    public event Action<string, Vector2I>? BackpackSlotActivated;
    public bool HasActiveInventoryInteraction =>
        (_backpackWindow?.HasActiveInventoryInteraction ?? false)
        || (_itemInfoWindow?.HasActiveInventoryInteraction ?? false)
        || (_statsWindow?.HasActiveInventoryInteraction ?? false);

    public override void _Ready()
    {
        Name = "FactoryPlayerHud";

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);
        _root = root;

        var barPanel = new PanelContainer();
        barPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        barPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.04f, 0.08f, 0.11f, 0.94f), new Color("38BDF8"), 2, 10));
        root.AddChild(barPanel);
        _barPanel = barPanel;

        var barMargin = new MarginContainer();
        barMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        barMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
        barMargin.AddThemeConstantOverride("margin_left", 10);
        barMargin.AddThemeConstantOverride("margin_top", 8);
        barMargin.AddThemeConstantOverride("margin_right", 10);
        barMargin.AddThemeConstantOverride("margin_bottom", 8);
        barPanel.AddChild(barMargin);

        var barBody = new VBoxContainer();
        barBody.MouseFilter = Control.MouseFilterEnum.Ignore;
        barBody.AddThemeConstantOverride("separation", 8);
        barMargin.AddChild(barBody);

        _statusLabel = CreateLabel(11, new Color("D7E6F2"));
        barBody.AddChild(_statusLabel);

        var hotbarRow = new HBoxContainer();
        hotbarRow.MouseFilter = Control.MouseFilterEnum.Ignore;
        hotbarRow.AddThemeConstantOverride("separation", 6);
        barBody.AddChild(hotbarRow);

        for (var index = 0; index < 9; index++)
        {
            hotbarRow.AddChild(CreateHotbarSlot(index));
        }

        var buttonRow = new HBoxContainer();
        buttonRow.MouseFilter = Control.MouseFilterEnum.Ignore;
        buttonRow.AddThemeConstantOverride("separation", 6);
        barBody.AddChild(buttonRow);
        _buttonRow = buttonRow;

        buttonRow.AddChild(CreatePanelToggleButton("背包", () => ToggleBackpack()));
        buttonRow.AddChild(CreatePanelToggleButton("物品信息", () => ToggleItemInfo()));
        buttonRow.AddChild(CreatePanelToggleButton("个人属性", () => ToggleStats()));

        _backpackWindow = new FactoryStructureDetailWindow();
        _backpackWindow.InventoryMoveRequested += (inventoryId, fromSlot, toSlot, splitStack) => BackpackInventoryMoveRequested?.Invoke(inventoryId, fromSlot, toSlot, splitStack);
        _backpackWindow.InventoryTransferRequested += (fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack) => BackpackInventoryTransferRequested?.Invoke(fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack);
        _backpackWindow.InventorySlotActivated += (inventoryId, slot) => BackpackSlotActivated?.Invoke(inventoryId, slot);
        _backpackWindow.CloseRequested += () => _backpackVisible = false;
        root.AddChild(_backpackWindow);

        _itemInfoWindow = new FactoryStructureDetailWindow();
        _itemInfoWindow.CloseRequested += () => _itemInfoVisible = false;
        root.AddChild(_itemInfoWindow);

        _statsWindow = new FactoryStructureDetailWindow();
        _statsWindow.CloseRequested += () => _statsVisible = false;
        root.AddChild(_statsWindow);

        var hotbarDragPreview = new PanelContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TopLevel = true,
            CustomMinimumSize = new Vector2(170.0f, 52.0f),
            ZIndex = 128
        };
        hotbarDragPreview.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.03f, 0.07f, 0.11f, 0.92f), new Color("38BDF8"), 2, 8));
        root.AddChild(hotbarDragPreview);
        _hotbarDragPreview = hotbarDragPreview;

        var dragPreviewMargin = new MarginContainer();
        dragPreviewMargin.AddThemeConstantOverride("margin_left", 8);
        dragPreviewMargin.AddThemeConstantOverride("margin_top", 6);
        dragPreviewMargin.AddThemeConstantOverride("margin_right", 8);
        dragPreviewMargin.AddThemeConstantOverride("margin_bottom", 6);
        hotbarDragPreview.AddChild(dragPreviewMargin);

        var dragPreviewRow = new HBoxContainer();
        dragPreviewRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        dragPreviewRow.AddThemeConstantOverride("separation", 8);
        dragPreviewMargin.AddChild(dragPreviewRow);

        var dragPreviewIcon = new TextureRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(28.0f, 28.0f),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        dragPreviewRow.AddChild(dragPreviewIcon);
        _hotbarDragPreviewIcon = dragPreviewIcon;

        var dragPreviewText = new VBoxContainer();
        dragPreviewText.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        dragPreviewText.AddThemeConstantOverride("separation", 2);
        dragPreviewRow.AddChild(dragPreviewText);

        _hotbarDragPreviewTitle = CreateLabel(11, Colors.White);
        _hotbarDragPreviewTitle.AutowrapMode = TextServer.AutowrapMode.Off;
        _hotbarDragPreviewTitle.ClipText = true;
        _hotbarDragPreviewTitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _hotbarDragPreviewTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        dragPreviewText.AddChild(_hotbarDragPreviewTitle);

        _hotbarDragPreviewCount = CreateLabel(10, new Color("FDE68A"));
        dragPreviewText.AddChild(_hotbarDragPreviewCount);

        UpdateLayout();
        Refresh();
        GetViewport().SizeChanged += UpdateLayout;
    }

    public override void _ExitTree()
    {
        if (GetViewport() is not null)
        {
            GetViewport().SizeChanged -= UpdateLayout;
        }
    }

    public override void _Process(double delta)
    {
        if (_pendingHotbarDragIndex >= 0)
        {
            if (!_leftMouseHeld)
            {
                CompletePendingHotbarClick();
            }
            else
            {
                _pendingHotbarDragElapsed += delta;
                if (_pendingHotbarDragElapsed >= HotbarDragHoldSeconds)
                {
                    TraceLog($"pending hotbar drag matured index={_pendingHotbarDragIndex} elapsed={_pendingHotbarDragElapsed:0.000}");
                    BeginHotbarDrag(_pendingHotbarDragIndex);
                }
            }
        }

        UpdateHotbarDragPreview();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        _leftMouseHeld = mouseButton.Pressed;
        if (mouseButton.Pressed || !FactoryStructureDetailWindow.HasSharedInventoryDrag)
        {
            return;
        }

        TraceLog($"global release with shared drag active pointer={GetViewport().GetMousePosition()} activeHotbarDrag={_activeHotbarDragIndex}");
        var releasedToHotbar = TryDropSharedDragToHotbar();
        var releasedToWindowSlot = !releasedToHotbar
            && FactoryStructureDetailWindow.TryCompleteSharedInventoryDropAtPointer(
                GetViewport().GetMousePosition(),
                splitStack: Input.IsKeyPressed(Key.Ctrl));
        TraceLog($"global release resolution hotbar={releasedToHotbar} windowSlot={releasedToWindowSlot}");
        if (releasedToHotbar || releasedToWindowSlot)
        {
            ClearHotbarDragState();
            GetViewport().SetInputAsHandled();
            return;
        }

        CallDeferred(nameof(CancelSharedDragIfStillActive));
    }

    public void SetContext(FactoryPlayerController? player, FactoryStructureDetailModel? linkedStructureModel, FactoryItem? selectedItem)
    {
        var playerChanged = !ReferenceEquals(_player, player);
        _player = player;
        _linkedStructureModel = linkedStructureModel;
        _selectedItem = selectedItem;
        RefreshHotbar();

        if (playerChanged)
        {
            _lastBackpackWindowSignature = null;
            _lastItemInfoWindowSignature = null;
            _lastStatsWindowSignature = null;
        }

        RefreshWindows();
    }

    public bool BlocksWorldInput(Control? control)
    {
        return BlocksWorldInput(control, GetViewport().GetMousePosition());
    }

    public bool BlocksWorldInput(Control? control, Vector2 screenPoint)
    {
        if ((_backpackWindow?.BlocksInput(control) ?? false)
            || (_backpackWindow?.ContainsScreenPoint(screenPoint) ?? false)
            || (_itemInfoWindow?.BlocksInput(control) ?? false)
            || (_itemInfoWindow?.ContainsScreenPoint(screenPoint) ?? false)
            || (_statsWindow?.BlocksInput(control) ?? false)
            || (_statsWindow?.ContainsScreenPoint(screenPoint) ?? false))
        {
            return true;
        }

        return BlocksInteractiveInput(control, _barPanel)
            || ContainsScreenPoint(_barPanel, screenPoint);
    }

    private void ToggleBackpack()
    {
        _backpackVisible = !_backpackVisible;
        RefreshWindows();
    }

    private void ToggleItemInfo()
    {
        _itemInfoVisible = !_itemInfoVisible;
        RefreshWindows();
    }

    private void ToggleStats()
    {
        _statsVisible = !_statsVisible;
        RefreshWindows();
    }

    private Control CreateHotbarSlot(int index)
    {
        var panel = new PanelContainer();
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        panel.CustomMinimumSize = new Vector2(84.0f, 84.0f);
        panel.TooltipText = $"快捷栏 {index + 1}";
        panel.GuiInput += @event => HandleHotbarSlotGuiInput(index, @event);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.MouseFilter = Control.MouseFilterEnum.Ignore;
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.AddThemeConstantOverride("separation", 2);
        margin.AddChild(body);

        var topRow = new HBoxContainer();
        topRow.MouseFilter = Control.MouseFilterEnum.Ignore;
        topRow.AddThemeConstantOverride("separation", 4);
        body.AddChild(topRow);

        var indexLabel = CreateLabel(10, new Color("FDE68A"));
        indexLabel.Text = $"{index + 1}";
        topRow.AddChild(indexLabel);

        var countLabel = CreateLabel(10, Colors.White);
        countLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        countLabel.HorizontalAlignment = HorizontalAlignment.Right;
        topRow.AddChild(countLabel);

        var iconHost = new CenterContainer();
        iconHost.MouseFilter = Control.MouseFilterEnum.Ignore;
        iconHost.CustomMinimumSize = new Vector2(0.0f, 32.0f);
        body.AddChild(iconHost);

        var icon = new TextureRect();
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.CustomMinimumSize = new Vector2(32.0f, 32.0f);
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconHost.AddChild(icon);

        var nameLabel = CreateLabel(10, new Color("D7E6F2"));
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        body.AddChild(nameLabel);

        var widget = new HotbarSlotWidget
        {
            Index = index,
            Panel = panel,
            Icon = icon,
            IndexLabel = indexLabel,
            NameLabel = nameLabel,
            CountLabel = countLabel
        };
        _hotbarSlots.Add(widget);
        return panel;
    }

    private Button CreatePanelToggleButton(string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 30.0f)
        };
        button.Pressed += onPressed;
        return button;
    }

    private void UpdateLayout()
    {
        if (_barPanel is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var panelWidth = Mathf.Min(viewportSize.X - 20.0f, 960.0f);
        var panelHeight = 156.0f;
        _barPanel.Size = new Vector2(Mathf.Max(520.0f, panelWidth), panelHeight);
        _barPanel.Position = new Vector2((viewportSize.X - _barPanel.Size.X) * 0.5f, viewportSize.Y - panelHeight - 18.0f);

        var dragBounds = new Rect2(Vector2.Zero, viewportSize);
        _backpackWindow?.SetDragBounds(dragBounds);
        _itemInfoWindow?.SetDragBounds(dragBounds);
        _statsWindow?.SetDragBounds(dragBounds);
    }

    private void Refresh()
    {
        RefreshHotbar();
        RefreshWindows();
    }

    private void RefreshHotbar()
    {
        if (_statusLabel is null)
        {
            return;
        }

        var activePrototype = _player?.GetArmedPlaceablePrototype();
        _statusLabel.Text = _player is null
            ? "玩家未初始化。"
            : activePrototype.HasValue
                ? $"当前就绪：{FactoryPresentation.GetBuildPrototypeDisplayName(activePrototype.Value)}，左键可在有效地格放置。"
                : "当前未就绪放置物；点击快捷栏切换或开关建筑套件。";

        for (var index = 0; index < _hotbarSlots.Count; index++)
        {
            var widget = _hotbarSlots[index];
            var item = _player?.GetHotbarItem(index);
            var isSelected = _player is not null && index == _player.ActiveHotbarIndex;
            var isArmed = isSelected && (_player?.IsHotbarPlacementArmed ?? false);
            var accentColor = item is null ? new Color("475569") : FactoryPresentation.GetItemAccentColor(item);
            var borderColor = isArmed
                ? new Color("4ADE80")
                : isSelected
                    ? new Color("FDE68A")
                    : accentColor;
            var backgroundColor = isSelected ? new Color("13253A") : new Color("0F172A");
            var stackCount = CountHotbarStack(index);
            var isDropHovered = FactoryStructureDetailWindow.HasSharedInventoryDrag
                && widget.Panel.GetGlobalRect().HasPoint(GetViewport().GetMousePosition());
            var acceptsSharedDrop = isDropHovered
                && FactoryStructureDetailWindow.CanAcceptSharedInventoryDrop(
                    FactoryPlayerController.BackpackInventoryId,
                    new Vector2I(index, 0),
                    item?.ItemKind,
                    stackCount,
                    item is null ? 0 : FactoryItemCatalog.GetMaxStackSize(item.ItemKind),
                    item is null ? null : FactoryPresentation.GetItemDisplayName(item));
            if (_activeHotbarDragIndex == index)
            {
                borderColor = new Color("38BDF8");
                backgroundColor = new Color("10243C");
            }
            else if (_pendingHotbarDragIndex == index)
            {
                borderColor = new Color("7DD3FC");
                backgroundColor = new Color("12253A");
            }

            if (isDropHovered)
            {
                borderColor = acceptsSharedDrop ? new Color("4ADE80") : new Color("FCA5A5");
                backgroundColor = acceptsSharedDrop ? new Color("122A23") : new Color("2A1717");
            }

            widget.Panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(backgroundColor, borderColor, isSelected ? 2 : 1, 8));
            widget.Icon.Texture = item is null ? null : FactoryPresentation.GetItemIcon(item);
            widget.Icon.Visible = item is not null;
            widget.Icon.Modulate = item is null
                ? new Color(0.6f, 0.66f, 0.74f, 0.8f)
                : Colors.White;
            widget.NameLabel.Text = item is null ? "空槽位" : FactoryPresentation.GetItemDisplayName(item);
            widget.CountLabel.Text = item is null
                ? "--"
                : $"x{stackCount}/{FactoryItemCatalog.GetMaxStackSize(item.ItemKind)}";
            widget.Panel.TooltipText = item is null
                ? isDropHovered
                    ? acceptsSharedDrop
                        ? $"快捷栏 {index + 1} 可接收当前拖拽物品"
                        : $"快捷栏 {index + 1} 不能接收当前拖拽物品"
                    : $"快捷栏 {index + 1} 为空"
                : isDropHovered
                    ? acceptsSharedDrop
                        ? $"{FactoryPresentation.GetItemDisplayName(item)} | 松开左键可放入这个快捷栏槽位"
                        : $"{FactoryPresentation.GetItemDisplayName(item)} | 当前拖拽物品不能放入这里"
                    : $"{FactoryPresentation.GetItemDisplayName(item)} | 左键切换当前快捷栏";
        }
    }

    private int CountHotbarStack(int index)
    {
        if (_player is null)
        {
            return 0;
        }

        var state = _player.BackpackInventory.Snapshot();
        for (var slotIndex = 0; slotIndex < state.Length; slotIndex++)
        {
            if (state[slotIndex].Position == new Vector2I(index, 0))
            {
                return state[slotIndex].StackCount;
            }
        }

        return 0;
    }

    private void RefreshWindows()
    {
        if (_player is null)
        {
            _backpackWindow?.HideWindow();
            _itemInfoWindow?.HideWindow();
            _statsWindow?.HideWindow();
            _lastBackpackWindowSignature = null;
            _lastItemInfoWindowSignature = null;
            _lastStatsWindowSignature = null;
            return;
        }

        if (_backpackVisible)
        {
            var backpackSignature = _player.BuildBackpackUiSignature(_linkedStructureModel);
            if (_lastBackpackWindowSignature != backpackSignature)
            {
                _backpackWindow?.ShowDetails(
                    _player.BuildBackpackDetailModel(_linkedStructureModel),
                    new Vector2(18.0f, 160.0f));
                _lastBackpackWindowSignature = backpackSignature;
            }
        }
        else
        {
            _backpackWindow?.HideWindow();
            _lastBackpackWindowSignature = null;
        }

        if (_itemInfoVisible)
        {
            var itemInfoSignature = _player.BuildItemInfoSignature(_selectedItem ?? _player.GetActiveHotbarItem());
            if (_lastItemInfoWindowSignature != itemInfoSignature)
            {
                _itemInfoWindow?.ShowDetails(
                    _player.BuildItemInfoDetailModel(_selectedItem ?? _player.GetActiveHotbarItem()),
                    new Vector2(560.0f, 180.0f));
                _lastItemInfoWindowSignature = itemInfoSignature;
            }
        }
        else
        {
            _itemInfoWindow?.HideWindow();
            _lastItemInfoWindowSignature = null;
        }

        if (_statsVisible)
        {
            var statsSignature = _player.BuildStatsSignature();
            if (_lastStatsWindowSignature != statsSignature)
            {
                _statsWindow?.ShowDetails(
                    _player.BuildStatsDetailModel(),
                    new Vector2(880.0f, 210.0f));
                _lastStatsWindowSignature = statsSignature;
            }
        }
        else
        {
            _statsWindow?.HideWindow();
            _lastStatsWindowSignature = null;
        }
    }

    private void HandleHotbarSlotGuiInput(int index, InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        if (mouseButton.Pressed)
        {
            if (FactoryStructureDetailWindow.HasSharedInventoryDrag)
            {
                TraceLog($"ignored hotbar press index={index} because shared drag already active");
                return;
            }

            if (_player?.GetHotbarItem(index) is null)
            {
                TraceLog($"hotbar press index={index} empty slot -> toggle/select");
                HotbarSlotPressed?.Invoke(index);
                return;
            }

            _pendingHotbarDragIndex = index;
            _pendingHotbarDragElapsed = 0.0;
            TraceLog($"hotbar press index={index} item={_player.GetHotbarItem(index)?.ItemKind} count={CountHotbarStack(index)} -> pending drag");
            RefreshHotbar();
            return;
        }

        if (_activeHotbarDragIndex >= 0)
        {
            TraceLog($"hotbar release while active drag index={_activeHotbarDragIndex}");
            if (TryDropSharedDragToHotbar())
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (_pendingHotbarDragIndex == index)
        {
            TraceLog($"hotbar release index={index} before drag threshold -> click");
            CompletePendingHotbarClick();
        }
    }

    private void CompletePendingHotbarClick()
    {
        if (_pendingHotbarDragIndex < 0)
        {
            return;
        }

        var index = _pendingHotbarDragIndex;
        _pendingHotbarDragIndex = -1;
        _pendingHotbarDragElapsed = 0.0;
        TraceLog($"complete pending hotbar click index={index}");
        RefreshHotbar();
        HotbarSlotPressed?.Invoke(index);
    }

    private void BeginHotbarDrag(int index)
    {
        if (_player?.GetHotbarItem(index) is not FactoryItem item)
        {
            TraceLog($"begin hotbar drag aborted index={index} no item");
            _pendingHotbarDragIndex = -1;
            _pendingHotbarDragElapsed = 0.0;
            RefreshHotbar();
            return;
        }

        _pendingHotbarDragIndex = -1;
        _pendingHotbarDragElapsed = 0.0;
        _activeHotbarDragIndex = index;
        TraceLog($"begin hotbar drag index={index} item={item.ItemKind} source={item.SourceKind} count={CountHotbarStack(index)}");
        FactoryStructureDetailWindow.BeginSharedInventoryDrag(
            FactoryPlayerController.BackpackInventoryId,
            new Vector2I(index, 0),
            item.ItemKind,
            FactoryPresentation.GetItemDisplayName(item),
            CountHotbarStack(index),
            FactoryItemCatalog.GetMaxStackSize(item.ItemKind),
            FactoryPresentation.GetItemIcon(item),
            FactoryPresentation.GetItemAccentColor(item),
            (inventoryId, fromSlot, toSlot, splitStack) => BackpackInventoryMoveRequested?.Invoke(inventoryId, fromSlot, toSlot, splitStack),
            (fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack) => BackpackInventoryTransferRequested?.Invoke(fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack));
        RefreshHotbar();
    }

    private bool TryDropSharedDragToHotbar()
    {
        var hoveredHotbarIndex = GetHotbarIndexAtScreenPoint(GetViewport().GetMousePosition());
        if (hoveredHotbarIndex < 0)
        {
            TraceLog("try drop shared drag to hotbar -> no hovered hotbar slot");
            return false;
        }

        var hoveredItem = _player?.GetHotbarItem(hoveredHotbarIndex);
        var targetCount = CountHotbarStack(hoveredHotbarIndex);
        var targetMaxStack = hoveredItem is null ? 0 : FactoryItemCatalog.GetMaxStackSize(hoveredItem.ItemKind);
        var dropped = FactoryStructureDetailWindow.TryCompleteSharedInventoryDrop(
            FactoryPlayerController.BackpackInventoryId,
            new Vector2I(hoveredHotbarIndex, 0),
            hoveredItem?.ItemKind,
            targetCount,
            targetMaxStack,
            hoveredItem is null ? null : FactoryPresentation.GetItemDisplayName(hoveredItem),
            splitStack: Input.IsKeyPressed(Key.Ctrl));
        TraceLog($"try drop shared drag to hotbar targetIndex={hoveredHotbarIndex} targetItem={hoveredItem?.ItemKind.ToString() ?? "empty"} targetCount={targetCount} dropped={dropped}");
        if (dropped)
        {
            ClearHotbarDragState();
        }

        return dropped;
    }

    private void CancelSharedDragIfStillActive()
    {
        if (!FactoryStructureDetailWindow.HasSharedInventoryDrag)
        {
            TraceLog("cancel shared drag deferred skipped because no shared drag remains");
            _activeHotbarDragIndex = -1;
            RefreshHotbar();
            return;
        }

        TraceLog("cancel shared drag deferred -> cancelling remaining shared drag");
        FactoryStructureDetailWindow.CancelSharedInventoryDrag();
        ClearHotbarDragState();
    }

    private void UpdateHotbarDragPreview()
    {
        if (_hotbarDragPreview is null || _hotbarDragPreviewIcon is null || _hotbarDragPreviewTitle is null || _hotbarDragPreviewCount is null)
        {
            return;
        }

        if (_activeHotbarDragIndex < 0 || _player?.GetHotbarItem(_activeHotbarDragIndex) is not FactoryItem item)
        {
            _hotbarDragPreview.Visible = false;
            return;
        }

        _hotbarDragPreview.Visible = true;
        _hotbarDragPreview.Position = GetViewport().GetMousePosition() + new Vector2(18.0f, 18.0f);
        _hotbarDragPreviewIcon.Texture = FactoryPresentation.GetItemIcon(item);
        _hotbarDragPreviewIcon.Modulate = _hotbarDragPreviewIcon.Texture is null ? FactoryPresentation.GetItemAccentColor(item) : Colors.White;
        _hotbarDragPreviewTitle.Text = FactoryPresentation.GetItemDisplayName(item);
        _hotbarDragPreviewCount.Text = Input.IsKeyPressed(Key.Ctrl)
            ? $"分半拖拽 x{Mathf.Max(1, Mathf.CeilToInt(CountHotbarStack(_activeHotbarDragIndex) * 0.5f))}"
            : $"整堆拖拽 x{CountHotbarStack(_activeHotbarDragIndex)}";
    }

    private void ClearHotbarDragState()
    {
        _activeHotbarDragIndex = -1;
        _pendingHotbarDragIndex = -1;
        _pendingHotbarDragElapsed = 0.0;
        if (_hotbarDragPreview is not null)
        {
            _hotbarDragPreview.Visible = false;
        }

        RefreshHotbar();
    }

    private static bool BlocksInteractiveInput(Control? control, Control? root)
    {
        if (control is null || root is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current == root)
            {
                return true;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private int GetHotbarIndexAtScreenPoint(Vector2 screenPoint)
    {
        for (var index = 0; index < _hotbarSlots.Count; index++)
        {
            if (_hotbarSlots[index].Panel.GetGlobalRect().HasPoint(screenPoint))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool ContainsScreenPoint(Control? control, Vector2 screenPoint)
    {
        return control is not null
            && control.Visible
            && control.GetGlobalRect().HasPoint(screenPoint);
    }

    private static Label CreateLabel(int fontSize, Color color)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Modulate = color;
        return label;
    }

    private static StyleBoxFlat CreatePanelStyle(Color backgroundColor, Color borderColor, int borderWidth, int radius)
    {
        return new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = borderColor,
            BorderWidthBottom = borderWidth,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            ContentMarginBottom = 2,
            ContentMarginLeft = 2,
            ContentMarginRight = 2,
            ContentMarginTop = 2
        };
    }
}
