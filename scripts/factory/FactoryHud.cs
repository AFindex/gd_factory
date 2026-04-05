using Godot;
using System;
using System.Collections.Generic;

public partial class FactoryHud : CanvasLayer
{
    private const string BuildWorkspaceId = "build";
    private const string BlueprintWorkspaceId = "blueprints";
    private const string TelemetryWorkspaceId = "telemetry";
    private const string CombatWorkspaceId = "combat";
    private const string TestingWorkspaceId = "testing";
    private const int CompactTabFontSize = 10;
    private static readonly (string Title, BuildPrototypeKind[] Kinds)[] BuildPaletteCategories =
    {
        ("物流与缓存", new[]
        {
            BuildPrototypeKind.Belt,
            BuildPrototypeKind.Splitter,
            BuildPrototypeKind.Merger,
            BuildPrototypeKind.Bridge,
            BuildPrototypeKind.Storage,
            BuildPrototypeKind.LargeStorageDepot,
            BuildPrototypeKind.Inserter,
            BuildPrototypeKind.Sink
        }),
        ("生产与电力", new[]
        {
            BuildPrototypeKind.MiningDrill,
            BuildPrototypeKind.Generator,
            BuildPrototypeKind.PowerPole,
            BuildPrototypeKind.Smelter,
            BuildPrototypeKind.Assembler
        }),
        ("防御与设施", new[]
        {
            BuildPrototypeKind.Wall,
            BuildPrototypeKind.GunTurret,
            BuildPrototypeKind.HeavyGunTurret
        }),
        ("测试建筑", new[]
        {
            BuildPrototypeKind.Loader,
            BuildPrototypeKind.Unloader,
            BuildPrototypeKind.Producer,
            BuildPrototypeKind.AmmoAssembler
        })
    };

    private readonly Dictionary<BuildPrototypeKind, Button> _selectionButtons = new();
    private readonly Dictionary<string, Control> _workspacePanels = new();

    private Control? _root;
    private PanelContainer? _chromePanel;
    private PanelContainer? _panel;
    private FactoryWorkspaceChrome? _workspaceChrome;
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
    private Label? _testingNoteLabel;
    private Label? _testingTargetLabel;
    private Label? _testingPreviewLabel;
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
    public event Action<string>? WorkspaceSelected;

    public string ProfilerText => _profilerLabel?.Text ?? string.Empty;
    public string InspectionTitleText => _inspectionTitleLabel?.Text ?? string.Empty;
    public string InspectionBodyText => _inspectionBodyLabel?.Text ?? string.Empty;
    public bool IsDetailVisible => _detailWindow?.IsShowing ?? false;
    public string DetailTitleText => _detailWindow?.CurrentTitleText ?? string.Empty;
    public string ActiveWorkspaceId => _workspaceChrome?.ActiveWorkspaceId ?? string.Empty;

    public override void _Ready()
    {
        Name = "FactoryHud";

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);
        _root = root;

        var chromePanel = new PanelContainer();
        chromePanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        chromePanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(chromePanel);
        _chromePanel = chromePanel;

        _workspaceChrome = new FactoryWorkspaceChrome();
        _workspaceChrome.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _workspaceChrome.WorkspaceSelected += HandleWorkspaceSelected;
        chromePanel.AddChild(_workspaceChrome);
        _workspaceChrome.Configure(
            string.Empty,
            string.Empty,
            BuildWorkspaceDescriptors(),
            BuildWorkspaceId);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
        root.AddChild(panel);
        _panel = panel;

        var chrome = new MarginContainer();
        chrome.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        chrome.MouseFilter = Control.MouseFilterEnum.Ignore;
        chrome.AddThemeConstantOverride("margin_left", 10);
        chrome.AddThemeConstantOverride("margin_top", 10);
        chrome.AddThemeConstantOverride("margin_right", 10);
        chrome.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(chrome);

        var workspaceHost = new PanelContainer();
        workspaceHost.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        workspaceHost.ClipContents = true;
        workspaceHost.AddThemeStyleboxOverride("panel", CreateWorkspaceBodyStyle());
        chrome.AddChild(workspaceHost);

        var workspaceMargin = new MarginContainer();
        workspaceMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        workspaceMargin.AddThemeConstantOverride("margin_left", 10);
        workspaceMargin.AddThemeConstantOverride("margin_top", 10);
        workspaceMargin.AddThemeConstantOverride("margin_right", 10);
        workspaceMargin.AddThemeConstantOverride("margin_bottom", 10);
        workspaceHost.AddChild(workspaceMargin);

        var workspaceRoot = new Control();
        workspaceRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        workspaceRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
        workspaceRoot.ClipContents = true;
        workspaceMargin.AddChild(workspaceRoot);

        workspaceRoot.AddChild(BuildBuildWorkspace());
        workspaceRoot.AddChild(BuildBlueprintWorkspace());
        workspaceRoot.AddChild(BuildTelemetryWorkspace());
        workspaceRoot.AddChild(BuildCombatWorkspace());
        workspaceRoot.AddChild(BuildTestingWorkspace());
        ApplyWorkspaceVisibility();

        _detailWindow = new FactoryStructureDetailWindow();
        _detailWindow.InventoryMoveRequested += (inventoryId, fromSlot, toSlot) => DetailInventoryMoveRequested?.Invoke(inventoryId, fromSlot, toSlot);
        _detailWindow.RecipeSelected += recipeId => DetailRecipeSelected?.Invoke(recipeId);
        _detailWindow.CloseRequested += () => DetailClosed?.Invoke();
        root.AddChild(_detailWindow);

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

    public IReadOnlyList<string> GetWorkspaceIds()
    {
        return _workspaceChrome?.GetWorkspaceIds() ?? Array.Empty<string>();
    }

    public bool IsWorkspaceVisible(string workspaceId)
    {
        return _workspacePanels.TryGetValue(workspaceId, out var panel) && panel.Visible;
    }

    public void SelectWorkspace(string workspaceId)
    {
        _workspaceChrome?.SetActiveWorkspace(workspaceId);
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

        if (_testingTargetLabel is not null)
        {
            _testingTargetLabel.Text = $"验证目标: {text}";
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

        if (_testingPreviewLabel is not null)
        {
            _testingPreviewLabel.Text = $"验证提示: {text}";
            _testingPreviewLabel.Modulate = isValid ? new Color("A7F3A0") : new Color("FFB4A2");
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

        if (_testingNoteLabel is not null)
        {
            _testingNoteLabel.Text = text;
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

        return BlocksInteractiveInput(control, _chromePanel)
            || BlocksInteractiveInput(control, _panel);
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

        if (state.PendingCaptureId is not null
            || state.CanConfirmApply
            || state.ModeText.Contains("框选", StringComparison.Ordinal)
            || state.ModeText.Contains("应用预览", StringComparison.Ordinal))
        {
            SetActiveWorkspace(BlueprintWorkspaceId, emitSignal: false);
        }
    }

    private IReadOnlyList<FactoryWorkspaceDescriptor> BuildWorkspaceDescriptors()
    {
        return new[]
        {
            new FactoryWorkspaceDescriptor(BuildWorkspaceId, "建造"),
            new FactoryWorkspaceDescriptor(BlueprintWorkspaceId, "蓝图"),
            new FactoryWorkspaceDescriptor(TelemetryWorkspaceId, "遥测"),
            new FactoryWorkspaceDescriptor(CombatWorkspaceId, "战斗"),
            new FactoryWorkspaceDescriptor(TestingWorkspaceId, "测试")
        };
    }

    private Control BuildBuildWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(BuildWorkspaceId);
        body.AddChild(CreateSectionLabel("建造工作区", 14, Colors.White));
        body.AddChild(CreateValueLabel("默认交互模式下可选中建筑，显式选择原型后才进入建造模式。", new Color("A8B8C6")));

        body.AddChild(CreateDivider());
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
        body.AddChild(CreateSectionLabel("建造面板", 12, new Color("F8FAFC")));
        body.AddChild(CreateValueLabel("按功能拆成页签；多格建筑会保留在各自所属功能分类中。", new Color("8EA4B8")));
        BuildSelectionCategories(body);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("快速观察", 12, new Color("F8FAFC")));
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

        return workspace;
    }

    private Control BuildBlueprintWorkspace()
    {
        var workspace = new Control();
        workspace.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        workspace.MouseFilter = Control.MouseFilterEnum.Ignore;
        workspace.Visible = false;

        var body = new VBoxContainer();
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        workspace.AddChild(body);

        body.AddChild(CreateSectionLabel("蓝图工作区", 14, Colors.White));
        body.AddChild(CreateValueLabel("框选保存、库浏览和应用预览都集中在这里，不再单独弹出默认常驻窗口。", new Color("A8B8C6")));

        var blueprintHost = new PanelContainer();
        blueprintHost.MouseFilter = Control.MouseFilterEnum.Ignore;
        blueprintHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        blueprintHost.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        blueprintHost.ClipContents = true;
        blueprintHost.AddThemeStyleboxOverride("panel", CreateWorkspaceBodyStyle());
        body.AddChild(blueprintHost);

        var blueprintMargin = new MarginContainer();
        blueprintMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        blueprintMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
        blueprintMargin.AddThemeConstantOverride("margin_left", 8);
        blueprintMargin.AddThemeConstantOverride("margin_top", 8);
        blueprintMargin.AddThemeConstantOverride("margin_right", 8);
        blueprintMargin.AddThemeConstantOverride("margin_bottom", 8);
        blueprintHost.AddChild(blueprintMargin);

        _blueprintPanel = new FactoryBlueprintPanel();
        _blueprintPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _blueprintPanel.SetDocked(true);
        _blueprintPanel.CaptureSelectionRequested += () => BlueprintCaptureRequested?.Invoke();
        _blueprintPanel.BlueprintSelected += blueprintId => BlueprintSelected?.Invoke(blueprintId);
        _blueprintPanel.SaveCaptureRequested += name => BlueprintSaveRequested?.Invoke(name);
        _blueprintPanel.ApplyActiveRequested += () => BlueprintApplyRequested?.Invoke();
        _blueprintPanel.ConfirmApplyRequested += () => BlueprintConfirmRequested?.Invoke();
        _blueprintPanel.DeleteSelectedRequested += blueprintId => BlueprintDeleteRequested?.Invoke(blueprintId);
        _blueprintPanel.CancelRequested += () => BlueprintCancelRequested?.Invoke();
        blueprintMargin.AddChild(_blueprintPanel);

        _workspacePanels[BlueprintWorkspaceId] = workspace;
        return workspace;
    }

    private Control BuildTelemetryWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(TelemetryWorkspaceId);
        body.AddChild(CreateSectionLabel("遥测工作区", 14, Colors.White));
        body.AddChild(CreateValueLabel("把吞吐、性能和稳定性读数收敛在一个面板里，方便观察 sandbox 当前运行状态。", new Color("A8B8C6")));

        body.AddChild(CreateDivider());
        _deliveryLabel = CreateValueLabel(string.Empty);
        _profilerLabel = CreateValueLabel(string.Empty, new Color("CFE7FF"));
        body.AddChild(_deliveryLabel);
        body.AddChild(_profilerLabel);

        body.AddChild(CreateDivider());
        _noteLabel = CreateValueLabel(string.Empty, new Color("EED49F"));
        body.AddChild(_noteLabel);

        return workspace;
    }

    private Control BuildCombatWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(CombatWorkspaceId);
        body.AddChild(CreateSectionLabel("战斗工作区", 14, Colors.White));
        body.AddChild(CreateValueLabel("把威胁与损失读数从主建造界面中分离出来，便于在需要时单独盯防。", new Color("A8B8C6")));

        body.AddChild(CreateDivider());
        _combatLabel = CreateValueLabel(string.Empty, new Color("FCA5A5"));
        body.AddChild(_combatLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateValueLabel("验证炮塔供弹、敌潮压力和防线破口时，可将视图切到这个工作区。", new Color("8EA4B8")));

        return workspace;
    }

    private Control BuildTestingWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(TestingWorkspaceId);
        body.AddChild(CreateSectionLabel("测试工作区", 14, Colors.White));
        body.AddChild(CreateValueLabel("把常见的 build/inspect/blueprint 验证路径整理成一个独立面板，而不是默认摊开在主 HUD 上。", new Color("A8B8C6")));

        var jumpRow = new HBoxContainer();
        jumpRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        jumpRow.AddThemeConstantOverride("separation", 6);
        body.AddChild(jumpRow);

        jumpRow.AddChild(CreateWorkspaceJumpButton("打开建造工作区", BuildWorkspaceId));
        jumpRow.AddChild(CreateWorkspaceJumpButton("打开蓝图工作区", BlueprintWorkspaceId));

        body.AddChild(CreateDivider());
        _testingNoteLabel = CreateValueLabel(string.Empty, new Color("EED49F"));
        _testingTargetLabel = CreateValueLabel("验证目标: 未选中建筑", new Color("D7E3EE"));
        _testingPreviewLabel = CreateValueLabel("验证提示: 等待状态更新。", new Color("D7E3EE"));
        body.AddChild(_testingNoteLabel);
        body.AddChild(_testingTargetLabel);
        body.AddChild(_testingPreviewLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateValueLabel("建议验证路径：Shift+左键框选蓝图、点击建筑查看详情、Delete/X 验证拆除与恢复。", new Color("8EA4B8")));

        return workspace;
    }

    private (ScrollContainer workspace, VBoxContainer body) CreateWorkspacePanel(string workspaceId)
    {
        var workspace = new ScrollContainer();
        workspace.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        workspace.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        workspace.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        workspace.MouseFilter = Control.MouseFilterEnum.Ignore;
        workspace.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        workspace.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        workspace.Visible = false;

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        body.CustomMinimumSize = new Vector2(240.0f, 0.0f);
        workspace.AddChild(body);

        _workspacePanels[workspaceId] = workspace;
        return (workspace, body);
    }

    private Button CreateWorkspaceJumpButton(string text, string workspaceId)
    {
        var button = new Button
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 30.0f)
        };
        button.Pressed += () => SetActiveWorkspace(workspaceId);
        return button;
    }

    private void HandleWorkspaceSelected(string workspaceId)
    {
        ApplyWorkspaceVisibility();
        WorkspaceSelected?.Invoke(workspaceId);
    }

    private void SetActiveWorkspace(string workspaceId, bool emitSignal = true)
    {
        _workspaceChrome?.SetActiveWorkspace(workspaceId, emitSignal);
        ApplyWorkspaceVisibility();
    }

    private void ApplyWorkspaceVisibility()
    {
        var activeWorkspaceId = _workspaceChrome?.ActiveWorkspaceId ?? BuildWorkspaceId;
        foreach (var pair in _workspacePanels)
        {
            pair.Value.Visible = pair.Key == activeWorkspaceId;
        }
    }

    private void UpdateLayout()
    {
        if (_root is null || _panel is null || _chromePanel is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var outerMargin = 10.0f;
        var panelWidth = Mathf.Clamp(viewportSize.X * 0.25f, 280.0f, 380.0f);
        var chromeHeight = 40.0f;
        var contentTop = chromeHeight + 6.0f;
        var contentHeight = Mathf.Max(320.0f, viewportSize.Y - contentTop - outerMargin);

        _chromePanel.Position = new Vector2(0.0f, 0.0f);
        _chromePanel.Size = new Vector2(panelWidth, chromeHeight);
        _panel.Position = new Vector2(0.0f, contentTop);
        _panel.Size = new Vector2(panelWidth, contentHeight);
        _detailWindow?.SetDragBounds(new Rect2(Vector2.Zero, viewportSize));
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

    private void BuildSelectionCategories(Container parent)
    {
        var tabs = new TabContainer();
        tabs.MouseFilter = Control.MouseFilterEnum.Ignore;
        tabs.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        tabs.AddThemeFontSizeOverride("font_size", CompactTabFontSize);
        tabs.AddThemeConstantOverride("side_margin", 2);
        parent.AddChild(tabs);

        for (var index = 0; index < BuildPaletteCategories.Length; index++)
        {
            var category = BuildPaletteCategories[index];
            var section = new VBoxContainer();
            section.Name = category.Title;
            section.MouseFilter = Control.MouseFilterEnum.Ignore;
            section.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            section.AddThemeConstantOverride("separation", 4);
            tabs.AddChild(section);

            var buttonGrid = new GridContainer();
            buttonGrid.Columns = 2;
            buttonGrid.MouseFilter = Control.MouseFilterEnum.Ignore;
            buttonGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            buttonGrid.AddThemeConstantOverride("h_separation", 6);
            buttonGrid.AddThemeConstantOverride("v_separation", 6);
            section.AddChild(buttonGrid);

            for (var kindIndex = 0; kindIndex < category.Kinds.Length; kindIndex++)
            {
                var kind = category.Kinds[kindIndex];
                CreateSelectionButton(buttonGrid, kind, GetBuildPaletteLabel(kind));
            }
        }
    }

    private static string GetBuildPaletteLabel(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Producer => "1 兼容生产器",
            BuildPrototypeKind.Belt => "2 传送带",
            BuildPrototypeKind.Sink => "3 回收站",
            BuildPrototypeKind.Splitter => "4 分流器",
            BuildPrototypeKind.Merger => "5 合并器",
            BuildPrototypeKind.Bridge => "6 跨桥",
            BuildPrototypeKind.Loader => "7 装载器",
            BuildPrototypeKind.Unloader => "8 卸载器",
            BuildPrototypeKind.Storage => "9 仓储",
            BuildPrototypeKind.Inserter => "0 机械臂",
            BuildPrototypeKind.MiningDrill => "采矿机",
            BuildPrototypeKind.Generator => "发电机",
            BuildPrototypeKind.PowerPole => "电线杆",
            BuildPrototypeKind.Smelter => "熔炉",
            BuildPrototypeKind.Assembler => "组装机",
            BuildPrototypeKind.LargeStorageDepot => "大型仓储",
            BuildPrototypeKind.Wall => "墙体",
            BuildPrototypeKind.AmmoAssembler => "弹药组装器",
            BuildPrototypeKind.GunTurret => "机枪炮塔",
            BuildPrototypeKind.HeavyGunTurret => "重型炮塔",
            _ => FactoryPresentation.GetKindLabel(kind)
        };
    }

    private static bool BlocksInteractiveInput(Control? control, Control? container)
    {
        if (control is null || container is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current == container)
            {
                return false;
            }

            if (current is BaseButton or ItemList or LineEdit)
            {
                return IsInside(current, container);
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private static bool IsInside(Control control, Control container)
    {
        var current = control;
        while (current is not null)
        {
            if (current == container)
            {
                return true;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private static Label CreateSectionLabel(string text, int fontSize, Color color)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.Text = text;
        label.Modulate = color;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
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

    private static StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.08f, 0.12f, 0.90f),
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

    private static StyleBoxFlat CreateWorkspaceBodyStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.03f, 0.05f, 0.08f, 0.72f),
            BorderColor = new Color(0.45f, 0.53f, 0.61f, 0.28f),
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
}
