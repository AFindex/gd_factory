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
    public bool AllowFullCapture { get; init; }
    public bool CanSaveCapture { get; init; }
    public bool CanConfirmApply { get; init; }
    public IReadOnlyList<FactoryBlueprintRecord> Blueprints { get; init; } = Array.Empty<FactoryBlueprintRecord>();
}

public partial class FactoryBlueprintPanel : PanelContainer
{
    private PanelContainer? _headerPanel;
    private MarginContainer? _outerMargin;
    private ScrollContainer? _scrollContainer;
    private VBoxContainer? _body;
    private Control? _dragHandle;
    private Label? _dragHintLabel;
    private Label? _modeLabel;
    private Label? _activeLabel;
    private Label? _captureSummaryLabel;
    private Label? _issueLabel;
    private ItemList? _blueprintList;
    private LineEdit? _nameEdit;
    private Button? _captureFullButton;
    private Button? _saveRuntimeButton;
    private Button? _saveSourceButton;
    private Button? _applyButton;
    private Button? _confirmButton;
    private Button? _deleteButton;
    private Button? _cancelButton;
    private string? _lastPendingCaptureId;
    private bool _draggingPanel;
    private bool _panelMovedByUser;
    private bool _docked;
    private Rect2 _defaultRect;
    private Vector2 _dragOffset;

    public event Action? CaptureFullRequested;
    public event Action<string>? BlueprintSelected;
    public event Action<string>? SaveCaptureRuntimeRequested;
    public event Action<string>? SaveCaptureSourceRequested;
    public event Action? ApplyActiveRequested;
    public event Action? ConfirmApplyRequested;
    public event Action<string>? DeleteSelectedRequested;
    public event Action? CancelRequested;

    public override void _Ready()
    {
        Name = "BlueprintPanel";
        MouseFilter = Control.MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", FactoryUiTheme.CreateChromePanelStyle());
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
        _outerMargin = margin;

        var root = new VBoxContainer();
        root.MouseFilter = Control.MouseFilterEnum.Stop;
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var header = new PanelContainer();
        header.MouseFilter = Control.MouseFilterEnum.Stop;
        header.AddThemeStyleboxOverride("panel", FactoryUiTheme.CreateTitleBarStyle());
        root.AddChild(header);
        _headerPanel = header;

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

        headerRow.AddChild(CreateSectionLabel("蓝图工作台", 18, FactoryUiTheme.Text));

        var dragHint = CreateValueLabel("拖动标题栏可移动窗口", FactoryUiTheme.TextSubtle);
        dragHint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        dragHint.HorizontalAlignment = HorizontalAlignment.Right;
        headerRow.AddChild(dragHint);
        _dragHintLabel = dragHint;

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
        _body = body;

        _activeLabel = CreateValueLabel("[ACTIVE] 当前蓝图：未选择", FactoryUiTheme.Text);

        _captureFullButton = CreateActionButton("保存当前布局");
        _captureFullButton.Pressed += () => CaptureFullRequested?.Invoke();
        body.AddChild(_captureFullButton);

        var saveNameRow = new HBoxContainer();
        saveNameRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        saveNameRow.AddThemeConstantOverride("separation", 6);
        body.AddChild(saveNameRow);

        _nameEdit = new LineEdit
        {
            PlaceholderText = "输入蓝图名称",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        FactoryUiTheme.ApplyLineEditTheme(_nameEdit);
        saveNameRow.AddChild(_nameEdit);

        var saveButtonGrid = new GridContainer();
        saveButtonGrid.Columns = 2;
        saveButtonGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        saveButtonGrid.AddThemeConstantOverride("h_separation", 6);
        saveButtonGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(saveButtonGrid);

        _saveRuntimeButton = CreateActionButton("保存到运行时");
        _saveRuntimeButton.Pressed += () => SaveCaptureRuntimeRequested?.Invoke(_nameEdit?.Text?.Trim() ?? string.Empty);
        saveButtonGrid.AddChild(_saveRuntimeButton);

        _saveSourceButton = CreateActionButton("保存到工程内");
        _saveSourceButton.Pressed += () => SaveCaptureSourceRequested?.Invoke(_nameEdit?.Text?.Trim() ?? string.Empty);
        saveButtonGrid.AddChild(_saveSourceButton);

        body.AddChild(CreateSectionLabel("蓝图库", 13, FactoryUiTheme.Text));
        _blueprintList = new ItemList
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 216.0f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        FactoryUiTheme.ApplyItemListTheme(_blueprintList);
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

        RefreshSelectionActions();
        UpdatePresentationMode();
    }

    public override void _Process(double delta)
    {
        if (_docked || !_draggingPanel)
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
        if (_docked)
        {
            return;
        }

        _defaultRect = rect;
        Size = rect.Size;
        if (!_panelMovedByUser)
        {
            Position = rect.Position;
        }

        ClampToViewport();
    }

    public void SetDocked(bool docked)
    {
        _docked = docked;
        UpdatePresentationMode();
    }

    public void SetState(FactoryBlueprintPanelState state)
    {
        Visible = _docked || state.IsVisible;
        if (!Visible)
        {
            return;
        }

        if (_modeLabel is not null)
        {
            _modeLabel.Text = $"[MODE] {state.ModeText}";
        }

        if (_activeLabel is not null)
        {
            _activeLabel.Text = $"[ACTIVE] {state.ActiveBlueprintText}";
            _activeLabel.Visible = false;
        }        

        if (_captureSummaryLabel is not null)
        {
            _captureSummaryLabel.Text = $"[CAPTURE] {state.CaptureSummaryText}";
        }

        if (_issueLabel is not null)
        {
            _issueLabel.Text = $"[STATE] {state.IssueText}";
            _issueLabel.Modulate = state.CanConfirmApply ? FactoryUiTheme.StatusOk : FactoryUiTheme.TextSubtle;
        }

        if (_captureFullButton is not null)
        {
            _captureFullButton.Visible = state.AllowFullCapture;
        }

        if (_saveRuntimeButton is not null)
        {
            _saveRuntimeButton.Disabled = !state.CanSaveCapture;
        }

        if (_saveSourceButton is not null)
        {
            _saveSourceButton.Disabled = !state.CanSaveCapture;
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
                _blueprintList.SetItemCustomBgColor(itemIndex, new Color(FactoryUiTheme.SurfaceInverse, 0.22f));
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
        if (_docked)
        {
            return;
        }

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
        if (_docked)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var clampedX = Mathf.Clamp(Position.X, 6.0f, Mathf.Max(6.0f, viewportSize.X - Size.X - 6.0f));
        var clampedY = Mathf.Clamp(Position.Y, 6.0f, Mathf.Max(6.0f, viewportSize.Y - Size.Y - 6.0f));
        Position = new Vector2(clampedX, clampedY);
    }

    private void UpdatePresentationMode()
    {
        if (!IsInsideTree())
        {
            return;
        }

        _draggingPanel = false;
        if (_dragHandle is not null)
        {
            _dragHandle.Visible = !_docked;
        }

        if (_dragHintLabel is not null)
        {
            _dragHintLabel.Text = _docked ? "作为当前工作区内容显示" : "拖动标题栏可移动窗口";
        }

        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = _docked ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin;
        CustomMinimumSize = _docked ? new Vector2(0.0f, 0.0f) : new Vector2(220.0f, 0.0f);
        MouseFilter = Control.MouseFilterEnum.Stop;

        if (_headerPanel is not null)
        {
            _headerPanel.Visible = !_docked;
        }

        if (_scrollContainer is not null)
        {
            _scrollContainer.CustomMinimumSize = _docked
                ? Vector2.Zero
                : new Vector2(220.0f, 0.0f);
        }

        if (_docked)
        {
            AddThemeStyleboxOverride("panel", FactoryUiTheme.CreatePanelStyle(Colors.Transparent, Colors.Transparent, borderWidth: 0));
            if (_outerMargin is not null)
            {
                _outerMargin.AddThemeConstantOverride("margin_left", 2);
                _outerMargin.AddThemeConstantOverride("margin_top", 2);
                _outerMargin.AddThemeConstantOverride("margin_right", 2);
                _outerMargin.AddThemeConstantOverride("margin_bottom", 2);
            }

            if (_body is not null)
            {
                _body.CustomMinimumSize = Vector2.Zero;
                _body.AddThemeConstantOverride("separation", 6);
            }

            Position = Vector2.Zero;
        }
        else
        {
            AddThemeStyleboxOverride("panel", FactoryUiTheme.CreateChromePanelStyle());
            if (_outerMargin is not null)
            {
                _outerMargin.AddThemeConstantOverride("margin_left", 10);
                _outerMargin.AddThemeConstantOverride("margin_top", 10);
                _outerMargin.AddThemeConstantOverride("margin_right", 10);
                _outerMargin.AddThemeConstantOverride("margin_bottom", 10);
            }

            if (_body is not null)
            {
                _body.CustomMinimumSize = new Vector2(228.0f, 0.0f);
                _body.AddThemeConstantOverride("separation", 8);
            }

            Size = _defaultRect.Size;
            if (!_panelMovedByUser)
            {
                Position = _defaultRect.Position;
            }

            ClampToViewport();
        }
    }

    private static Button CreateActionButton(string text)
    {
        var button = new Button
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 30.0f)
        };
        FactoryUiTheme.ApplyButtonTheme(button);
        return button;
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

}
