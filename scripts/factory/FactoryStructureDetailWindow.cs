using Godot;
using System;
using System.Collections.Generic;

public partial class FactoryStructureDetailWindow : PanelContainer
{
    private sealed class InventorySlotWidget
    {
        public required PanelContainer Panel { get; init; }
        public required string InventoryId { get; init; }
        public required Vector2I SlotPosition { get; init; }
        public required bool HasItem { get; init; }
        public required FactoryItemKind? ItemKind { get; init; }
        public required int StackCount { get; init; }
        public required int MaxStackSize { get; init; }
        public required TextureRect Icon { get; init; }
        public required Label Label { get; init; }
        public required Label StackLabel { get; init; }
        public required Label SubLabel { get; init; }
        public required Color AccentColor { get; init; }

        public bool CanAcceptFrom(InventorySlotWidget source)
        {
            if (InventoryId != source.InventoryId || SlotPosition == source.SlotPosition)
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
    private readonly List<InventorySlotWidget> _slotWidgets = new();
    private Rect2 _dragBounds = new(Vector2.Zero, new Vector2(900.0f, 600.0f));
    private bool _draggingWindow;
    private bool _windowMovedByUser;
    private Vector2 _windowDragOffset;
    private InventorySlotWidget? _dragSourceSlot;
    private InventorySlotWidget? _hoveredSlot;
    private string? _modelSignature;

    public event Action<string, Vector2I, Vector2I>? InventoryMoveRequested;
    public event Action<string>? RecipeSelected;
    public event Action<string>? DetailActionRequested;
    public event Action? CloseRequested;
    public bool IsShowing => Visible;
    public string CurrentTitleText => _titleLabel?.Text ?? string.Empty;

    public override void _Ready()
    {
        Name = "FactoryStructureDetailWindow";
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(522.0f, 280.0f);
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
        _recipeSections.AddThemeConstantOverride("separation", 6);
        body.AddChild(_recipeSections);

        _inventorySections = new VBoxContainer();
        _inventorySections.AddThemeConstantOverride("separation", 8);
        body.AddChild(_inventorySections);

        _actionSections = new VBoxContainer();
        _actionSections.AddThemeConstantOverride("separation", 6);
        body.AddChild(_actionSections);

        _dragStateLabel = CreateTextLabel(11, new Color("FDE68A"));
        body.AddChild(_dragStateLabel);
        UpdateDragStateLabel();
    }

    public override void _Process(double delta)
    {
        if (_dragSourceSlot is not null && !Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _dragSourceSlot = null;
            RefreshSlotVisuals();
            UpdateDragStateLabel();
        }

        if (Visible)
        {
            SyncContentWidth();
            ClampToBounds();
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
        _dragSourceSlot = null;
        _hoveredSlot = null;
        _recipePickerPanel = null;
        _recipePickerList = null;
        UpdateDragStateLabel();
    }

    public bool BlocksInput(Control? control)
    {
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

        foreach (var child in _inventorySections.GetChildren())
        {
            child.QueueFree();
        }

        _slotWidgets.Clear();
        _hoveredSlot = null;

        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            var title = CreateTextLabel(13, new Color("FDE68A"));
            title.Text = section.Title;
            _inventorySections.AddChild(title);

            var grid = new GridContainer();
            grid.Columns = Mathf.Max(1, section.GridSize.X);
            grid.AddThemeConstantOverride("h_separation", 6);
            grid.AddThemeConstantOverride("v_separation", 6);
            _inventorySections.AddChild(grid);

            for (var slotIndex = 0; slotIndex < section.Slots.Count; slotIndex++)
            {
                var slot = section.Slots[slotIndex];
                var slotPanel = new PanelContainer();
                slotPanel.MouseFilter = MouseFilterEnum.Stop;
                slotPanel.CustomMinimumSize = new Vector2(90.0f, 110.0f);
                grid.AddChild(slotPanel);

                var margin = new MarginContainer();
                margin.AddThemeConstantOverride("margin_left", 6);
                margin.AddThemeConstantOverride("margin_top", 6);
                margin.AddThemeConstantOverride("margin_right", 6);
                margin.AddThemeConstantOverride("margin_bottom", 6);
                slotPanel.AddChild(margin);

                var body = new VBoxContainer();
                body.AddThemeConstantOverride("separation", 2);
                margin.AddChild(body);

                var iconFrame = new PanelContainer();
                iconFrame.CustomMinimumSize = new Vector2(34.0f, 34.0f);
                iconFrame.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                body.AddChild(iconFrame);

                var iconMargin = new MarginContainer();
                iconMargin.SetAnchorsPreset(LayoutPreset.FullRect);
                iconMargin.AddThemeConstantOverride("margin_left", 4);
                iconMargin.AddThemeConstantOverride("margin_top", 4);
                iconMargin.AddThemeConstantOverride("margin_right", 4);
                iconMargin.AddThemeConstantOverride("margin_bottom", 4);
                iconFrame.AddChild(iconMargin);

                var iconRect = new TextureRect();
                iconRect.MouseFilter = MouseFilterEnum.Ignore;
                iconRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
                iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                iconRect.CustomMinimumSize = new Vector2(24.0f, 24.0f);
                iconRect.Texture = slot.IconTexture;
                iconRect.Visible = slot.IconTexture is not null;
                iconMargin.AddChild(iconRect);

                var itemLabel = CreateTextLabel(11, slot.HasItem ? Colors.White : new Color("7B8DA1"));
                itemLabel.Text = slot.HasItem ? slot.ItemLabel ?? string.Empty : "空槽位";
                body.AddChild(itemLabel);

                var stackLabel = CreateTextLabel(10, slot.HasItem ? new Color("FDE68A") : new Color("64748B"));
                stackLabel.Text = slot.HasItem ? $"x{slot.StackCount}/{slot.MaxStackSize}" : "--";
                body.AddChild(stackLabel);

                var posLabel = CreateTextLabel(10, new Color("9FB6C9"));
                posLabel.Text = $"({slot.Position.X}, {slot.Position.Y})";
                body.AddChild(posLabel);

                slotPanel.TooltipText = slot.ItemDescription ?? string.Empty;

                var widget = new InventorySlotWidget
                {
                    Panel = slotPanel,
                    InventoryId = section.InventoryId,
                    SlotPosition = slot.Position,
                    HasItem = slot.HasItem,
                    ItemKind = slot.ItemKind,
                    StackCount = slot.StackCount,
                    MaxStackSize = slot.MaxStackSize,
                    Icon = iconRect,
                    Label = itemLabel,
                    StackLabel = stackLabel,
                    SubLabel = posLabel,
                    AccentColor = slot.AccentColor
                };

                slotPanel.GuiInput += @event => HandleSlotGuiInput(widget, section.AllowItemMove, @event);
                slotPanel.MouseEntered += () =>
                {
                    _hoveredSlot = widget;
                    RefreshSlotVisuals();
                    UpdateDragStateLabel();
                };
                slotPanel.MouseExited += () =>
                {
                    if (_hoveredSlot == widget)
                    {
                        _hoveredSlot = null;
                        RefreshSlotVisuals();
                        UpdateDragStateLabel();
                    }
                };
                _slotWidgets.Add(widget);
            }
        }

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
                return;
            }

            _dragSourceSlot = widget;
            MoveToFront();
            RefreshSlotVisuals();
            UpdateDragStateLabel();
            return;
        }

        if (_dragSourceSlot is null)
        {
            return;
        }

        if (_dragSourceSlot.InventoryId == widget.InventoryId
            && widget.CanAcceptFrom(_dragSourceSlot))
        {
            InventoryMoveRequested?.Invoke(widget.InventoryId, _dragSourceSlot.SlotPosition, widget.SlotPosition);
        }

        _dragSourceSlot = null;
        RefreshSlotVisuals();
        UpdateDragStateLabel();
    }

    private void RefreshSlotVisuals()
    {
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
            else if (_dragSourceSlot is not null
                && _hoveredSlot == widget
                && widget.CanAcceptFrom(_dragSourceSlot))
            {
                borderColor = new Color("4ADE80");
                backgroundColor = new Color("122A23");
                borderWidth = 2;
            }
            else if (_hoveredSlot == widget)
            {
                borderColor = new Color("93C5FD");
            }

            widget.Panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(backgroundColor, borderColor, borderWidth));
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

        if (_dragSourceSlot is null)
        {
            _dragStateLabel.Text = "拖动窗口标题可移动位置；按住已占用槽位并释放到空槽位或同类未满堆叠可移动物品。";
            return;
        }

        if (_hoveredSlot is not null)
        {
            var action = _hoveredSlot.CanAcceptFrom(_dragSourceSlot)
                ? _hoveredSlot.HasItem
                    ? "并入目标堆叠"
                    : "移动到空槽位"
                : "目标无效";
            _dragStateLabel.Text = $"正在拖动物品：从 ({_dragSourceSlot.SlotPosition.X}, {_dragSourceSlot.SlotPosition.Y}) 移向 ({_hoveredSlot.SlotPosition.X}, {_hoveredSlot.SlotPosition.Y})，{action}";
            return;
        }

        _dragStateLabel.Text = $"正在拖动物品：源槽位 ({_dragSourceSlot.SlotPosition.X}, {_dragSourceSlot.SlotPosition.Y})";
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

        var availableWidth = Mathf.Max(260.0f, Size.X - 20.0f);
        _scroll.CustomMinimumSize = new Vector2(availableWidth, 0.0f);
        _bodyMargin.CustomMinimumSize = new Vector2(availableWidth, 0.0f);
        _body.CustomMinimumSize = new Vector2(Mathf.Max(360.0f, availableWidth - 20.0f), 0.0f);
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
}
