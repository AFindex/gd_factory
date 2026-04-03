using Godot;
using System;
using System.Collections.Generic;

public partial class FactoryHud : CanvasLayer
{
    private readonly Dictionary<BuildPrototypeKind, Button> _selectionButtons = new();

    private Control? _root;
    private PanelContainer? _panel;
    private MarginContainer? _chrome;
    private VBoxContainer? _body;
    private Label? _modeLabel;
    private Label? _selectedLabel;
    private Label? _selectionTargetLabel;
    private Label? _hoverLabel;
    private Label? _previewLabel;
    private Label? _rotationLabel;
    private Label? _deliveryLabel;
    private Label? _noteLabel;
    private Label? _profilerLabel;
    private Label? _combatLabel;
    private PanelContainer? _inspectionPanel;
    private Label? _inspectionTitleLabel;
    private Label? _inspectionBodyLabel;
    private FactoryStructureDetailWindow? _detailWindow;
    private FactoryBlueprintPanel? _blueprintPanel;

    public event Action<BuildPrototypeKind?>? SelectionChanged;
    public event Action<string, Vector2I, Vector2I>? DetailInventoryMoveRequested;
    public event Action<string>? DetailRecipeSelected;
    public event Action? DetailClosed;
    public event Action? BlueprintCaptureRequested;
    public event Action<string>? BlueprintSaveRequested;
    public event Action<string>? BlueprintSelected;
    public event Action? BlueprintApplyRequested;
    public event Action? BlueprintConfirmRequested;
    public event Action<string>? BlueprintDeleteRequested;
    public event Action? BlueprintCancelRequested;

    public string ProfilerText => _profilerLabel?.Text ?? string.Empty;
    public string InspectionTitleText => _inspectionTitleLabel?.Text ?? string.Empty;
    public string InspectionBodyText => _inspectionBodyLabel?.Text ?? string.Empty;
    public bool IsDetailVisible => _detailWindow?.IsShowing ?? false;
    public string DetailTitleText => _detailWindow?.CurrentTitleText ?? string.Empty;

    public override void _Ready()
    {
        Name = "FactoryHud";

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);
        _root = root;

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(panel);
        _panel = panel;

        var chrome = new MarginContainer();
        chrome.MouseFilter = Control.MouseFilterEnum.Ignore;
        chrome.AddThemeConstantOverride("margin_left", 12);
        chrome.AddThemeConstantOverride("margin_top", 10);
        chrome.AddThemeConstantOverride("margin_right", 12);
        chrome.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(chrome);
        _chrome = chrome;

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.AddThemeConstantOverride("separation", 6);
        chrome.AddChild(body);
        _body = body;

        body.AddChild(CreateSectionLabel("Net Factory Logistics Demo", 18, Colors.White));
        body.AddChild(CreateValueLabel("默认交互模式下可选中建筑，显式选择原型后才进入建造模式。", new Color("A8B8C6")));

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("建造面板", 12, new Color("F8FAFC")));

        var buttonGrid = new GridContainer();
        buttonGrid.Columns = 2;
        buttonGrid.MouseFilter = Control.MouseFilterEnum.Ignore;
        buttonGrid.AddThemeConstantOverride("h_separation", 6);
        buttonGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(buttonGrid);

        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Producer, "1 兼容生产器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.MiningDrill, "采矿机");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Generator, "发电机");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.PowerPole, "电线杆");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Smelter, "熔炉");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Assembler, "组装机");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Belt, "2 传送带");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Sink, "3 回收站");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Splitter, "4 分流器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Merger, "5 合并器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Bridge, "6 跨桥");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Loader, "7 装载器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Unloader, "8 卸载器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Storage, "9 仓储");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Inserter, "0 机械臂");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Wall, "墙体");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.AmmoAssembler, "弹药组装器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.GunTurret, "机枪炮塔");

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("状态", 12, new Color("F8FAFC")));
        _modeLabel = CreateValueLabel(string.Empty);
        _selectedLabel = CreateValueLabel(string.Empty);
        _selectionTargetLabel = CreateValueLabel(string.Empty);
        _rotationLabel = CreateValueLabel(string.Empty);
        _hoverLabel = CreateValueLabel(string.Empty);
        _previewLabel = CreateValueLabel(string.Empty);
        body.AddChild(_modeLabel);
        body.AddChild(_selectedLabel);
        body.AddChild(_selectionTargetLabel);
        body.AddChild(_rotationLabel);
        body.AddChild(_hoverLabel);
        body.AddChild(_previewLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("吞吐", 12, new Color("F8FAFC")));
        _deliveryLabel = CreateValueLabel(string.Empty);
        _noteLabel = CreateValueLabel(string.Empty, new Color("EED49F"));
        body.AddChild(_deliveryLabel);
        body.AddChild(_noteLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("建筑面板", 12, new Color("F8FAFC")));
        var inspectionPanel = new PanelContainer();
        inspectionPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        inspectionPanel.Visible = false;
        body.AddChild(inspectionPanel);
        _inspectionPanel = inspectionPanel;

        var inspectionBody = new VBoxContainer();
        inspectionBody.MouseFilter = Control.MouseFilterEnum.Ignore;
        inspectionBody.AddThemeConstantOverride("separation", 4);
        inspectionPanel.AddChild(inspectionBody);

        _inspectionTitleLabel = CreateValueLabel(string.Empty, new Color("FDE68A"));
        _inspectionBodyLabel = CreateValueLabel(string.Empty, new Color("D7E3EE"));
        inspectionBody.AddChild(_inspectionTitleLabel);
        inspectionBody.AddChild(_inspectionBodyLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("Combat", 12, new Color("F8FAFC")));
        _combatLabel = CreateValueLabel(string.Empty, new Color("FCA5A5"));
        body.AddChild(_combatLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("Profiler", 12, new Color("F8FAFC")));
        _profilerLabel = CreateValueLabel(string.Empty, new Color("CFE7FF"));
        body.AddChild(_profilerLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateValueLabel("镜头 WASD/方向键 | 缩放 滚轮 | 朝向 Q/E | 数字键或面板按钮进建造 | X 删除模式 | Esc 返回交互 | 交互模式左键选中 / Shift+左键框选蓝图 | 建造模式左键放置 / 右键退出建造 / Delete 拆除", new Color("8EA4B8")));

        _detailWindow = new FactoryStructureDetailWindow();
        _detailWindow.InventoryMoveRequested += (inventoryId, fromSlot, toSlot) => DetailInventoryMoveRequested?.Invoke(inventoryId, fromSlot, toSlot);
        _detailWindow.RecipeSelected += recipeId => DetailRecipeSelected?.Invoke(recipeId);
        _detailWindow.CloseRequested += () => DetailClosed?.Invoke();
        root.AddChild(_detailWindow);

        _blueprintPanel = new FactoryBlueprintPanel();
        _blueprintPanel.CaptureSelectionRequested += () => BlueprintCaptureRequested?.Invoke();
        _blueprintPanel.BlueprintSelected += blueprintId => BlueprintSelected?.Invoke(blueprintId);
        _blueprintPanel.SaveCaptureRequested += name => BlueprintSaveRequested?.Invoke(name);
        _blueprintPanel.ApplyActiveRequested += () => BlueprintApplyRequested?.Invoke();
        _blueprintPanel.ConfirmApplyRequested += () => BlueprintConfirmRequested?.Invoke();
        _blueprintPanel.DeleteSelectedRequested += blueprintId => BlueprintDeleteRequested?.Invoke(blueprintId);
        _blueprintPanel.CancelRequested += () => BlueprintCancelRequested?.Invoke();
        root.AddChild(_blueprintPanel);

        SetMode(FactoryInteractionMode.Interact);
        SetBuildSelection(null, null);
        SetSelectionTarget("未选中建筑");
        SetHoverCell(Vector2I.Zero, false);
        SetPreviewStatus(false, "交互模式：点击建筑查看；按数字键选择建筑后进入建造。");
        SetRotation(FacingDirection.East);
        SetSinkStats(0, 0, 0);
        SetProfilerStats(0, 0.0, 0, 0, 0.0, 0.0, 0.0);
        SetCombatStats(0, 0, 0);
        SetNote("默认场景包含采矿、电力与制造主循环，同时保留一部分 legacy 回归线用于 smoke 和蓝图验证。");
        SetInspection(null, null);
        UpdateLayout();
        GetViewport().SizeChanged += UpdateLayout;
    }

    public override void _ExitTree()
    {
        if (GetViewport() is not null)
        {
            GetViewport().SizeChanged -= UpdateLayout;
        }
    }

    public void SetMode(FactoryInteractionMode mode)
    {
        if (_modeLabel is null)
        {
            return;
        }

        _modeLabel.Text = mode switch
        {
            FactoryInteractionMode.Build => "当前模式: 建造模式",
            FactoryInteractionMode.Delete => "当前模式: 删除模式",
            _ => "当前模式: 交互模式"
        };
        _modeLabel.Modulate = mode switch
        {
            FactoryInteractionMode.Build => new Color("A7F3A0"),
            FactoryInteractionMode.Delete => new Color("FCA5A5"),
            _ => new Color("FDE68A")
        };
    }

    public void SetBuildSelection(BuildPrototypeKind? kind, string? details)
    {
        foreach (var pair in _selectionButtons)
        {
            pair.Value.ButtonPressed = kind.HasValue && pair.Key == kind.Value;
            pair.Value.Modulate = pair.Value.ButtonPressed ? Colors.White : new Color(0.72f, 0.78f, 0.86f);
        }

        if (_selectedLabel is null)
        {
            return;
        }

        if (!kind.HasValue)
        {
            _selectedLabel.Text = "当前建造: 未选择";
            _selectedLabel.TooltipText = "交互模式下点击建筑查看详情。";
            return;
        }

        var detailText = details ?? string.Empty;
        _selectedLabel.Text = $"当前建造: {FactoryPresentation.GetKindLabel(kind.Value)}\n{detailText}";
        _selectedLabel.TooltipText = detailText;
    }

    public void SetSelectionTarget(string text)
    {
        if (_selectionTargetLabel is not null)
        {
            _selectionTargetLabel.Text = $"当前选中: {text}";
        }
    }

    public void SetHoverCell(Vector2I cell, bool hasHover)
    {
        if (_hoverLabel is not null)
        {
            _hoverLabel.Text = hasHover ? $"格子: ({cell.X}, {cell.Y})" : "格子: 超出可建造区域";
        }
    }

    public void SetPreviewStatus(bool isValid, string text)
    {
        if (_previewLabel is not null)
        {
            _previewLabel.Text = $"提示: {text}";
            _previewLabel.Modulate = isValid ? new Color("A7F3A0") : new Color("FFB4A2");
        }
    }

    public void SetRotation(FacingDirection facing)
    {
        if (_rotationLabel is not null)
        {
            _rotationLabel.Text = $"朝向: {FactoryDirection.ToLabel(facing)}";
        }
    }

    public void SetSinkStats(int deliveredTotal, int deliveredRate, int sinkCount)
    {
        if (_deliveryLabel is not null)
        {
            _deliveryLabel.Text = $"活跃回收端: {sinkCount} | 累计: {deliveredTotal} | 最近: {deliveredRate}/秒";
        }
    }

    public void SetProfilerStats(int fps, double frameMilliseconds, int structureCount, int transitItemCount, double simulationMilliseconds, double visualMilliseconds, double topologyMilliseconds)
    {
        if (_profilerLabel is null)
        {
            return;
        }

        _profilerLabel.Text =
            $"FPS {fps} | 帧 {frameMilliseconds:0.0} ms\n" +
            $"结构 {structureCount} | 在途 {transitItemCount}\n" +
            $"热点 sim {simulationMilliseconds:0.00} ms | visual {visualMilliseconds:0.00} ms\n" +
            $"拓扑重建 {topologyMilliseconds:0.00} ms";
    }

    public void SetCombatStats(int activeEnemies, int kills, int structuresLost)
    {
        if (_combatLabel is not null)
        {
            _combatLabel.Text = $"敌人 {activeEnemies} | 击杀 {kills} | 损失建筑 {structuresLost}";
        }
    }

    public void SetNote(string text)
    {
        if (_noteLabel is not null)
        {
            _noteLabel.Text = text;
        }
    }

    public void SetInspection(string? title, string? body)
    {
        if (_inspectionPanel is null || _inspectionTitleLabel is null || _inspectionBodyLabel is null)
        {
            return;
        }

        var isVisible = !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body);
        _inspectionPanel.Visible = isVisible;
        _inspectionTitleLabel.Text = title ?? string.Empty;
        _inspectionBodyLabel.Text = body ?? string.Empty;
    }

    public bool BlocksWorldInput(Control? control)
    {
        if (_detailWindow?.BlocksInput(control) ?? false)
        {
            return true;
        }

        if (_blueprintPanel?.BlocksInput(control) ?? false)
        {
            return true;
        }

        if (control is null || _panel is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current is BaseButton && IsInsidePanel(current))
            {
                return true;
            }

            if (current == _panel)
            {
                return false;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    public void SetStructureDetails(FactoryStructureDetailModel? model)
    {
        if (_detailWindow is null || _panel is null)
        {
            return;
        }

        if (model is null)
        {
            _detailWindow.HideWindow();
            return;
        }

        var defaultPosition = new Vector2(_panel.Position.X + _panel.Size.X + 18.0f, 18.0f);
        _detailWindow.ShowDetails(model, defaultPosition);
    }

    public void SetBlueprintState(FactoryBlueprintPanelState state)
    {
        _blueprintPanel?.SetState(state);
    }

    private bool IsInsidePanel(Control control)
    {
        if (_panel is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current == _panel)
            {
                return true;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private void UpdateLayout()
    {
        if (_root is null || _panel is null || _chrome is null || _body is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var outerMargin = 14.0f;
        var panelWidth = Mathf.Clamp(viewportSize.X * 0.23f, 240.0f, 360.0f);
        var availableHeight = Mathf.Max(260.0f, viewportSize.Y - outerMargin * 2.0f);
        var targetHeight = Mathf.Clamp(viewportSize.Y * 0.68f, 340.0f, availableHeight);
        var innerWidth = Mathf.Max(200.0f, panelWidth - 24.0f);

        _panel.Position = new Vector2(outerMargin, outerMargin);
        _panel.Size = new Vector2(panelWidth, targetHeight);
        _chrome.Size = _panel.Size;
        _body.CustomMinimumSize = new Vector2(innerWidth, 0.0f);
        _detailWindow?.SetDragBounds(new Rect2(Vector2.Zero, viewportSize));
        var blueprintWidth = Mathf.Clamp(viewportSize.X * 0.24f, 260.0f, 340.0f);
        var blueprintHeight = Mathf.Clamp(viewportSize.Y * 0.76f, 440.0f, viewportSize.Y - outerMargin * 2.0f);
        _blueprintPanel?.SetPanelRect(new Rect2(
            new Vector2(_panel.Position.X + _panel.Size.X + 18.0f, outerMargin),
            new Vector2(blueprintWidth, blueprintHeight)));
    }

    private void CreateSelectionButton(Container parent, BuildPrototypeKind kind, string text)
    {
        var localKind = kind;
        var button = new Button();
        button.Text = text;
        button.ToggleMode = true;
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.CustomMinimumSize = new Vector2(0.0f, 30.0f);
        button.AddThemeFontSizeOverride("font_size", 12);
        button.Pressed += () =>
        {
            BuildPrototypeKind? nextKind = button.ButtonPressed ? localKind : null;
            SelectionChanged?.Invoke(nextKind);
        };
        parent.AddChild(button);
        _selectionButtons[kind] = button;
    }

    private static Label CreateSectionLabel(string text, int fontSize, Color color)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.Text = text;
        label.Modulate = color;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static Label CreateValueLabel(string text, Color? color = null)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.Text = text;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.Modulate = color ?? new Color("D7E3EE");
        label.AddThemeFontSizeOverride("font_size", 12);
        return label;
    }

    private static ColorRect CreateDivider()
    {
        return new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0.0f, 1.0f),
            Color = new Color(0.45f, 0.53f, 0.61f, 0.28f)
        };
    }
}
