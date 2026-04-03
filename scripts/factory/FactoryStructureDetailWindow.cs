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
        public required Label Label { get; init; }
        public required Label SubLabel { get; init; }
        public required Color AccentColor { get; init; }
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
    public event Action? CloseRequested;
    public bool IsShowing => Visible;
    public string CurrentTitleText => _titleLabel?.Text ?? string.Empty;

    public override void _Ready()
    {
        Name = "FactoryStructureDetailWindow";
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(348.0f, 280.0f);
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
        scroll.CustomMinimumSize = new Vector2(320.0f, 0.0f);
        root.AddChild(scroll);
        _scroll = scroll;

        var bodyMargin = new MarginContainer();
        bodyMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bodyMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        bodyMargin.CustomMinimumSize = new Vector2(320.0f, 0.0f);
        bodyMargin.AddThemeConstantOverride("margin_left", 10);
        bodyMargin.AddThemeConstantOverride("margin_top", 8);
        bodyMargin.AddThemeConstantOverride("margin_right", 10);
        bodyMargin.AddThemeConstantOverride("margin_bottom", 10);
        scroll.AddChild(bodyMargin);
        _bodyMargin = bodyMargin;

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.CustomMinimumSize = new Vector2(300.0f, 0.0f);
        body.AddThemeConstantOverride("separation", 10);
        bodyMargin.AddChild(body);
        _body = body;

        _summaryLabel = CreateTextLabel(12, new Color("D7E6F2"));
        body.AddChild(_summaryLabel);

        _inventorySections = new VBoxContainer();
        _inventorySections.AddThemeConstantOverride("separation", 8);
        body.AddChild(_inventorySections);

        _recipeSections = new VBoxContainer();
        _recipeSections.AddThemeConstantOverride("separation", 6);
        body.AddChild(_recipeSections);

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
                slotPanel.CustomMinimumSize = new Vector2(74.0f, 66.0f);
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

                var itemLabel = CreateTextLabel(11, slot.HasItem ? Colors.White : new Color("7B8DA1"));
                itemLabel.Text = slot.HasItem ? slot.ItemLabel ?? string.Empty : "空槽位";
                body.AddChild(itemLabel);

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
                    Label = itemLabel,
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

        for (var index = 0; index < section.Options.Count; index++)
        {
            var option = section.Options[index];
            var button = new Button();
            button.Text = $"{option.DisplayName} | {option.Summary}";
            button.Alignment = HorizontalAlignment.Left;
            button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            button.CustomMinimumSize = new Vector2(0.0f, 34.0f);
            button.ToggleMode = true;
            button.ButtonPressed = option.IsActive;
            button.Modulate = option.IsActive ? Colors.White : new Color("CBD5E1");
            button.Pressed += () => RecipeSelected?.Invoke(option.RecipeId);
            _recipeSections.AddChild(button);
        }
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
            && _dragSourceSlot.SlotPosition != widget.SlotPosition
            && !widget.HasItem)
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
                && _dragSourceSlot.InventoryId == widget.InventoryId
                && _hoveredSlot == widget
                && !widget.HasItem)
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
            widget.SubLabel.Modulate = new Color("9FB6C9");
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
            _dragStateLabel.Text = "拖动窗口标题可移动位置；按住已占用槽位并释放到空槽位可移动物品。";
            return;
        }

        _dragStateLabel.Text = _hoveredSlot is not null
            ? $"正在拖动物品：从 ({_dragSourceSlot.SlotPosition.X}, {_dragSourceSlot.SlotPosition.Y}) 移向 ({_hoveredSlot.SlotPosition.X}, {_hoveredSlot.SlotPosition.Y})"
            : $"正在拖动物品：源槽位 ({_dragSourceSlot.SlotPosition.X}, {_dragSourceSlot.SlotPosition.Y})";
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
        _body.CustomMinimumSize = new Vector2(Mathf.Max(240.0f, availableWidth - 20.0f), 0.0f);
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
