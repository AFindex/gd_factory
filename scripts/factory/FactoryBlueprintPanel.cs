using Godot;
using System;
using System.Collections.Generic;

public sealed class FactoryBlueprintPanelState
{
    public bool IsVisible { get; init; } = true;
    public string ModeText { get; init; } = "蓝图待命";
    public string ActiveBlueprintText { get; init; } = "当前蓝图：未选择";
    public string CaptureSummaryText { get; init; } = "未捕获待保存蓝图。";
    public string IssueText { get; init; } = "选择蓝图后可以进入预览和应用。";
    public string SuggestedName { get; init; } = string.Empty;
    public string? PendingCaptureId { get; init; }
    public string? ActiveBlueprintId { get; init; }
    public bool AllowSelectionCapture { get; init; } = true;
    public bool AllowFullCapture { get; init; }
    public bool CanSaveCapture { get; init; }
    public bool CanConfirmApply { get; init; }
    public IReadOnlyList<FactoryBlueprintRecord> Blueprints { get; init; } = Array.Empty<FactoryBlueprintRecord>();
}

public partial class FactoryBlueprintPanel : PanelContainer
{
    private ScrollContainer? _scrollContainer;
    private Control? _dragHandle;
    private Label? _modeLabel;
    private Label? _activeLabel;
    private Label? _captureSummaryLabel;
    private Label? _issueLabel;
    private ItemList? _blueprintList;
    private LineEdit? _nameEdit;
    private Button? _captureSelectionButton;
    private Button? _captureFullButton;
    private Button? _saveButton;
    private Button? _applyButton;
    private Button? _confirmButton;
    private Button? _deleteButton;
    private Button? _cancelButton;
    private string? _lastPendingCaptureId;
    private bool _draggingPanel;
    private bool _panelMovedByUser;
    private Rect2 _defaultRect;
    private Vector2 _dragOffset;

    public event Action? CaptureSelectionRequested;
    public event Action? CaptureFullRequested;
    public event Action<string>? BlueprintSelected;
    public event Action<string>? SaveCaptureRequested;
    public event Action? ApplyActiveRequested;
    public event Action? ConfirmApplyRequested;
    public event Action<string>? DeleteSelectedRequested;
    public event Action? CancelRequested;

    public override void _Ready()
    {
        Name = "BlueprintPanel";
        MouseFilter = Control.MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", CreatePanelStyle());
        SetProcess(true);

        var margin = new MarginContainer();
        margin.MouseFilter = Control.MouseFilterEnum.Stop;
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var root = new VBoxContainer();
        root.MouseFilter = Control.MouseFilterEnum.Stop;
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var header = new PanelContainer();
        header.MouseFilter = Control.MouseFilterEnum.Stop;
        header.AddThemeStyleboxOverride("panel", CreateHeaderStyle());
        root.AddChild(header);

        _dragHandle = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 34.0f)
        };
        _dragHandle.GuiInput += HandleDragHandleGuiInput;
        header.AddChild(_dragHandle);

        var headerMargin = new MarginContainer();
        headerMargin.SetAnchorsPreset(LayoutPreset.FullRect);
        headerMargin.AddThemeConstantOverride("margin_left", 10);
        headerMargin.AddThemeConstantOverride("margin_top", 7);
        headerMargin.AddThemeConstantOverride("margin_right", 10);
        headerMargin.AddThemeConstantOverride("margin_bottom", 7);
        _dragHandle.AddChild(headerMargin);

        var headerRow = new HBoxContainer();
        headerRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        headerRow.AddThemeConstantOverride("separation", 8);
        headerMargin.AddChild(headerRow);

        headerRow.AddChild(CreateSectionLabel("蓝图工作台", 18, Colors.White));

        var dragHint = CreateValueLabel("拖动标题栏可移动窗口", new Color("8FD3FF"));
        dragHint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        dragHint.HorizontalAlignment = HorizontalAlignment.Right;
        headerRow.AddChild(dragHint);

        _scrollContainer = new ScrollContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(220.0f, 0.0f)
        };
        root.AddChild(_scrollContainer);

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Stop;
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        body.CustomMinimumSize = new Vector2(228.0f, 0.0f);
        _scrollContainer.AddChild(body);

        _modeLabel = CreateValueLabel("蓝图待命", new Color("8FD3FF"));
        body.AddChild(_modeLabel);
        _activeLabel = CreateValueLabel("当前蓝图：未选择", new Color("FDE68A"));
        body.AddChild(_activeLabel);

        var captureRow = new HBoxContainer();
        captureRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        captureRow.AddThemeConstantOverride("separation", 6);
        body.AddChild(captureRow);

        _captureSelectionButton = CreateActionButton("框选保存");
        _captureSelectionButton.Pressed += () => CaptureSelectionRequested?.Invoke();
        captureRow.AddChild(_captureSelectionButton);

        _captureFullButton = CreateActionButton("保存当前布局");
        _captureFullButton.Pressed += () => CaptureFullRequested?.Invoke();
        captureRow.AddChild(_captureFullButton);

        _captureSummaryLabel = CreateValueLabel("未捕获待保存蓝图。", new Color("D7E6F2"));
        body.AddChild(_captureSummaryLabel);

        var saveRow = new HBoxContainer();
        saveRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        saveRow.AddThemeConstantOverride("separation", 6);
        body.AddChild(saveRow);

        _nameEdit = new LineEdit
        {
            PlaceholderText = "输入蓝图名称",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        saveRow.AddChild(_nameEdit);

        _saveButton = CreateActionButton("保存");
        _saveButton.Pressed += () => SaveCaptureRequested?.Invoke(_nameEdit?.Text?.Trim() ?? string.Empty);
        saveRow.AddChild(_saveButton);

        body.AddChild(CreateSectionLabel("蓝图库", 13, new Color("F8FAFC")));
        _blueprintList = new ItemList
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 108.0f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _blueprintList.ItemSelected += index =>
        {
            if (TryGetBlueprintId(index, out var blueprintId))
            {
                BlueprintSelected?.Invoke(blueprintId);
                RefreshSelectionActions();
            }
        };
        body.AddChild(_blueprintList);

        var actionGrid = new GridContainer();
        actionGrid.Columns = 2;
        actionGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actionGrid.AddThemeConstantOverride("h_separation", 6);
        actionGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(actionGrid);

        _applyButton = CreateActionButton("进入预览");
        _applyButton.Pressed += () => ApplyActiveRequested?.Invoke();
        actionGrid.AddChild(_applyButton);

        _deleteButton = CreateActionButton("删除");
        _deleteButton.Pressed += () =>
        {
            var blueprintId = GetSelectedBlueprintId();
            if (!string.IsNullOrWhiteSpace(blueprintId))
            {
                DeleteSelectedRequested?.Invoke(blueprintId!);
            }
        };
        actionGrid.AddChild(_deleteButton);

        _confirmButton = CreateActionButton("确认应用");
        _confirmButton.Pressed += () => ConfirmApplyRequested?.Invoke();
        actionGrid.AddChild(_confirmButton);

        _cancelButton = CreateActionButton("取消");
        _cancelButton.Pressed += () => CancelRequested?.Invoke();
        actionGrid.AddChild(_cancelButton);

        _issueLabel = CreateValueLabel("选择蓝图后可以进入预览和应用。", new Color("EED49F"));
        body.AddChild(_issueLabel);

        RefreshSelectionActions();
    }

    public override void _Process(double delta)
    {
        if (!_draggingPanel)
        {
            return;
        }

        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _draggingPanel = false;
            ClampToViewport();
            return;
        }

        Position = GetViewport().GetMousePosition() - _dragOffset;
        _panelMovedByUser = true;
        ClampToViewport();
    }

    public bool BlocksInput(Control? control)
    {
        if (control is null || !Visible)
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

    public void SetPanelRect(Rect2 rect)
    {
        _defaultRect = rect;
        Size = rect.Size;
        if (!_panelMovedByUser)
        {
            Position = rect.Position;
        }

        ClampToViewport();
    }

    public void SetState(FactoryBlueprintPanelState state)
    {
        Visible = state.IsVisible;
        if (!Visible)
        {
            return;
        }

        if (_modeLabel is not null)
        {
            _modeLabel.Text = state.ModeText;
        }

        if (_activeLabel is not null)
        {
            _activeLabel.Text = state.ActiveBlueprintText;
        }

        if (_captureSummaryLabel is not null)
        {
            _captureSummaryLabel.Text = state.CaptureSummaryText;
        }

        if (_issueLabel is not null)
        {
            _issueLabel.Text = state.IssueText;
            _issueLabel.Modulate = state.CanConfirmApply ? new Color("A7F3A0") : new Color("EED49F");
        }

        if (_captureSelectionButton is not null)
        {
            _captureSelectionButton.Visible = state.AllowSelectionCapture;
        }

        if (_captureFullButton is not null)
        {
            _captureFullButton.Visible = state.AllowFullCapture;
        }

        if (_saveButton is not null)
        {
            _saveButton.Disabled = !state.CanSaveCapture;
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Disabled = !state.CanConfirmApply;
        }

        PopulateBlueprints(state.Blueprints, state.ActiveBlueprintId);
        UpdateSuggestedName(state.PendingCaptureId, state.SuggestedName);
        RefreshSelectionActions();
    }

    private void PopulateBlueprints(IReadOnlyList<FactoryBlueprintRecord> blueprints, string? activeBlueprintId)
    {
        if (_blueprintList is null)
        {
            return;
        }

        var selectedId = GetSelectedBlueprintId();
        _blueprintList.Clear();
        for (var index = 0; index < blueprints.Count; index++)
        {
            var blueprint = blueprints[index];
            var label = blueprint.Id == activeBlueprintId
                ? $"[当前] {blueprint.DisplayName}\n{blueprint.GetSummaryText()}"
                : $"{blueprint.DisplayName}\n{blueprint.GetSummaryText()}";
            var itemIndex = _blueprintList.AddItem(label);
            _blueprintList.SetItemMetadata(itemIndex, blueprint.Id);
            if (blueprint.Id == activeBlueprintId)
            {
                _blueprintList.SetItemCustomBgColor(itemIndex, new Color(0.15f, 0.28f, 0.18f, 0.75f));
            }
        }

        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            SelectById(selectedId!);
        }
        else if (!string.IsNullOrWhiteSpace(activeBlueprintId))
        {
            SelectById(activeBlueprintId!);
        }
    }

    private void UpdateSuggestedName(string? pendingCaptureId, string suggestedName)
    {
        if (_nameEdit is null)
        {
            return;
        }

        if (_lastPendingCaptureId != pendingCaptureId)
        {
            _nameEdit.Text = pendingCaptureId is null ? string.Empty : suggestedName;
            _lastPendingCaptureId = pendingCaptureId;
            return;
        }

        if (!string.IsNullOrWhiteSpace(pendingCaptureId)
            && !_nameEdit.HasFocus()
            && string.IsNullOrWhiteSpace(_nameEdit.Text)
            && !string.IsNullOrWhiteSpace(suggestedName))
        {
            _nameEdit.Text = suggestedName;
        }
    }

    private void RefreshSelectionActions()
    {
        var hasSelection = !string.IsNullOrWhiteSpace(GetSelectedBlueprintId());
        if (_applyButton is not null)
        {
            _applyButton.Disabled = !hasSelection;
        }

        if (_deleteButton is not null)
        {
            _deleteButton.Disabled = !hasSelection;
        }
    }

    private void SelectById(string blueprintId)
    {
        if (_blueprintList is null)
        {
            return;
        }

        for (var index = 0; index < _blueprintList.ItemCount; index++)
        {
            if (TryGetBlueprintId(index, out var currentId) && currentId == blueprintId)
            {
                _blueprintList.Select(index);
                return;
            }
        }
    }

    private string? GetSelectedBlueprintId()
    {
        if (_blueprintList is null)
        {
            return null;
        }

        var selectedItems = _blueprintList.GetSelectedItems();
        if (selectedItems.Length == 0)
        {
            return null;
        }

        return TryGetBlueprintId(selectedItems[0], out var blueprintId) ? blueprintId : null;
    }

    private bool TryGetBlueprintId(long itemIndex, out string blueprintId)
    {
        blueprintId = string.Empty;
        if (_blueprintList is null || itemIndex < 0 || itemIndex >= _blueprintList.ItemCount)
        {
            return false;
        }

        var metadata = _blueprintList.GetItemMetadata((int)itemIndex);
        if (metadata.VariantType != Variant.Type.String)
        {
            return false;
        }

        blueprintId = metadata.AsString();
        return !string.IsNullOrWhiteSpace(blueprintId);
    }

    private void HandleDragHandleGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                _draggingPanel = true;
                _dragOffset = mouseButton.GlobalPosition - GlobalPosition;
                MoveToFront();
            }
            else
            {
                _draggingPanel = false;
                ClampToViewport();
            }

            AcceptEvent();
            return;
        }

        if (@event is InputEventMouseMotion && _draggingPanel)
        {
            Position = GetViewport().GetMousePosition() - _dragOffset;
            _panelMovedByUser = true;
            ClampToViewport();
            AcceptEvent();
        }
    }

    private void ClampToViewport()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var clampedX = Mathf.Clamp(Position.X, 6.0f, Mathf.Max(6.0f, viewportSize.X - Size.X - 6.0f));
        var clampedY = Mathf.Clamp(Position.Y, 6.0f, Mathf.Max(6.0f, viewportSize.Y - Size.Y - 6.0f));
        Position = new Vector2(clampedX, clampedY);
    }

    private static Button CreateActionButton(string text)
    {
        return new Button
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 30.0f)
        };
    }

    private static StyleBoxFlat CreateHeaderStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.14f, 0.20f, 0.98f),
            BorderColor = new Color("5DB5E8"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8
        };
    }

    private static Label CreateSectionLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Modulate = color;
        return label;
    }

    private static Label CreateValueLabel(string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.Modulate = color;
        return label;
    }

    private static StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.08f, 0.12f, 0.92f),
            BorderColor = new Color("4DA8DA"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10
        };
    }
}
