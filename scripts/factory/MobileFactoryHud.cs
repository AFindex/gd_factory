using Godot;
using System;
using System.Collections.Generic;

public partial class MobileFactoryHud : CanvasLayer
{
    private static readonly BuildPrototypeKind[] EditorPalette =
    {
        BuildPrototypeKind.Producer,
        BuildPrototypeKind.Belt,
        BuildPrototypeKind.Splitter,
        BuildPrototypeKind.Merger,
        BuildPrototypeKind.Bridge,
        BuildPrototypeKind.Loader,
        BuildPrototypeKind.Unloader,
        BuildPrototypeKind.Sink,
        BuildPrototypeKind.Storage,
        BuildPrototypeKind.Inserter,
        BuildPrototypeKind.OutputPort,
        BuildPrototypeKind.InputPort
    };

    private PanelContainer? _worldFocusFrame;
    private PanelContainer? _infoPanel;
    private VBoxContainer? _infoBody;
    private PanelContainer? _editorPanel;
    private VBoxContainer? _editorBody;
    private TextureRect? _editorViewportRect;
    private SubViewport? _editorViewport;
    private Label? _modeLabel;
    private Label? _stateLabel;
    private Label? _hoverLabel;
    private Label? _previewLabel;
    private Label? _deliveryLabel;
    private Label? _hintLabel;
    private Label? _editorModeLabel;
    private Label? _selectionLabel;
    private Label? _editorPreviewLabel;
    private Label? _portStatusLabel;
    private Label? _focusLabel;
    private Button? _observerButton;
    private Button? _deployButton;
    private readonly Dictionary<BuildPrototypeKind, Button> _paletteButtons = new();
    private float _editorProgress;
    private bool _editorOpen;
    private bool _editorFocused;

    private static readonly Color EditorFocusColor = new("7DD3FC");
    private static readonly Color WorldFocusColor = new("FDE68A");

    public SubViewport EditorViewport => _editorViewport!;
    public bool IsEditorVisible => _editorProgress > 0.01f;
    public string PortStatusText => _portStatusLabel?.Text ?? string.Empty;
    public event Action<BuildPrototypeKind>? EditorPaletteSelected;
    public event Action<int>? EditorRotateRequested;
    public event Action? ObserverModeToggleRequested;
    public event Action? DeployModeToggleRequested;

    public override void _Ready()
    {
        Name = "MobileFactoryHud";

        BuildInfoPanel();
        BuildEditorPanel();
        UpdateLayout();
        GetViewport().SizeChanged += UpdateLayout;
    }

    public override void _Process(double delta)
    {
        var target = _editorOpen ? 1.0f : 0.0f;
        _editorProgress = Mathf.MoveToward(_editorProgress, target, (float)delta * 4.5f);
        UpdateLayout();
    }

    public override void _ExitTree()
    {
        if (GetViewport() is not null)
        {
            GetViewport().SizeChanged -= UpdateLayout;
        }
    }

    public void SetEditorOpen(bool isOpen)
    {
        _editorOpen = isOpen;
    }

    public bool IsPointerOverEditor(Vector2 mousePosition)
    {
        return _editorPanel is not null
            && _editorProgress > 0.01f
            && _editorPanel.GetGlobalRect().HasPoint(mousePosition);
    }

    public bool IsPointerOverEditorViewport(Vector2 mousePosition)
    {
        return _editorViewportRect is not null
            && _editorProgress > 0.01f
            && _editorViewportRect.GetGlobalRect().HasPoint(mousePosition);
    }

    public bool TryGetEditorMousePosition(Vector2 mousePosition, out Vector2 editorMousePosition)
    {
        editorMousePosition = Vector2.Zero;

        if (_editorViewportRect is null || _editorViewport is null)
        {
            return false;
        }

        var globalRect = _editorViewportRect.GetGlobalRect();
        if (!globalRect.HasPoint(mousePosition) || globalRect.Size.X <= 0.0f || globalRect.Size.Y <= 0.0f)
        {
            return false;
        }

        var localMouse = mousePosition - globalRect.Position;
        editorMousePosition = new Vector2(
            localMouse.X * _editorViewport.Size.X / globalRect.Size.X,
            localMouse.Y * _editorViewport.Size.Y / globalRect.Size.Y);
        return true;
    }

    public void SetPaneFocus(bool editorOpen, bool editorFocused)
    {
        _editorOpen = editorOpen;
        _editorFocused = editorFocused;
        RefreshFocusVisuals();
    }

    public void SetControlMode(MobileFactoryControlMode controlMode, MobileFactoryLifecycleState lifecycleState, FacingDirection transitFacing, FacingDirection deployFacing)
    {
        if (_modeLabel is not null)
        {
            var modeText = controlMode switch
            {
                MobileFactoryControlMode.FactoryCommand => "工厂控制",
                MobileFactoryControlMode.DeployPreview => "部署预览",
                MobileFactoryControlMode.Observer => "观察模式",
                _ => "工厂控制"
            };
            _modeLabel.Text = $"当前模式：{modeText} | 行进朝向：{FactoryDirection.ToLabel(transitFacing)} | 部署朝向：{FactoryDirection.ToLabel(deployFacing)}";
            _modeLabel.Modulate = controlMode == MobileFactoryControlMode.Observer ? new Color("7DD3FC") : new Color("FDE68A");
        }

        if (_observerButton is not null)
        {
            _observerButton.Text = controlMode == MobileFactoryControlMode.Observer ? "返回工厂控制 (Tab)" : "进入观察模式 (Tab)";
            _observerButton.ButtonPressed = controlMode == MobileFactoryControlMode.Observer;
            _observerButton.Disabled = lifecycleState == MobileFactoryLifecycleState.Recalling;
        }

        if (_deployButton is not null)
        {
            _deployButton.Text = controlMode == MobileFactoryControlMode.DeployPreview ? "取消部署 (G)" : "部署模式 (G)";
            _deployButton.ButtonPressed = controlMode == MobileFactoryControlMode.DeployPreview;
            _deployButton.Disabled = lifecycleState != MobileFactoryLifecycleState.InTransit;
        }
    }

    public void SetState(MobileFactoryLifecycleState state, Vector2I? anchorCell)
    {
        if (_stateLabel is null)
        {
            return;
        }

        _stateLabel.Text = state switch
        {
            MobileFactoryLifecycleState.Deployed when anchorCell is not null => $"工厂状态：已部署于 ({anchorCell.Value.X}, {anchorCell.Value.Y})",
            MobileFactoryLifecycleState.AutoDeploying => "工厂状态：自动部署中，正在进场并对齐朝向",
            MobileFactoryLifecycleState.Recalling => "工厂状态：切回移动态中，部署机构正在收拢",
            _ => "工厂状态：运输中，可自由移动或下达部署命令"
        };
    }

    public void SetHoverAnchor(Vector2I anchorCell, bool hasHover)
    {
        if (_hoverLabel is null)
        {
            return;
        }

        _hoverLabel.Text = hasHover
            ? $"当前锚点：({anchorCell.X}, {anchorCell.Y})"
            : "当前锚点：未选择";
    }

    public void SetPreviewStatus(bool isValid, string text)
    {
        if (_previewLabel is null)
        {
            return;
        }

        _previewLabel.Text = $"世界提示：{text}";
        _previewLabel.Modulate = isValid ? new Color("A7F3A0") : new Color("FFB4A2");
    }

    public void SetDeliveryStats(int sinkA, int sinkB)
    {
        if (_deliveryLabel is null)
        {
            return;
        }

        _deliveryLabel.Text = $"演示回收站：A 线路累计 {sinkA} | B 线路累计 {sinkB}";
    }

    public void SetEditorSelection(BuildPrototypeKind kind, FacingDirection facing)
    {
        if (_selectionLabel is null)
        {
            return;
        }

        _selectionLabel.Text = $"内部建造：{GetKindLabel(kind)} | 朝向 {FactoryDirection.ToLabel(facing)}";
        RefreshPaletteButtons(kind);
    }

    public void SetEditorPreview(bool isValid, string text)
    {
        if (_editorPreviewLabel is null)
        {
            return;
        }

        _editorPreviewLabel.Text = $"内部预览：{text}";
        _editorPreviewLabel.Modulate = isValid ? new Color("A7F3A0") : new Color("FFB4A2");
    }

    public void SetPortStatus(string text)
    {
        if (_portStatusLabel is not null)
        {
            _portStatusLabel.Text = text;
            _portStatusLabel.Modulate = new Color("FDE68A");
        }
    }

    public void SetEditorFocusHint(bool overEditor)
    {
        if (_focusLabel is null)
        {
            return;
        }

        _focusLabel.Text = overEditor
            ? "鼠标焦点：内部编辑区"
            : "鼠标焦点：大世界";
    }

    public void SetEditorState(bool isOpen, MobileFactoryLifecycleState lifecycleState, int structureCount)
    {
        if (_editorModeLabel is null)
        {
            return;
        }

        var stateText = lifecycleState switch
        {
            MobileFactoryLifecycleState.Deployed => "已部署",
            MobileFactoryLifecycleState.AutoDeploying => "自动部署中",
            MobileFactoryLifecycleState.Recalling => "回收中",
            _ => "运输中"
        };
        var paneText = isOpen ? "分屏编辑已展开" : "按 F 打开内部编辑";
        _editorModeLabel.Text = $"内部编辑：{paneText} | 生命周期：{stateText} | 当前内部件数：{structureCount}";
    }

    public void SetHintText(string text)
    {
        if (_hintLabel is not null)
        {
            _hintLabel.Text = text;
        }
    }

    private void BuildInfoPanel()
    {
        var worldFocusFrame = new PanelContainer();
        worldFocusFrame.MouseFilter = Control.MouseFilterEnum.Ignore;
        worldFocusFrame.Visible = false;
        AddChild(worldFocusFrame);
        _worldFocusFrame = worldFocusFrame;

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(panel);
        _infoPanel = panel;

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Pass;
        body.AddThemeConstantOverride("separation", 8);
        panel.AddChild(body);
        _infoBody = body;

        var title = CreateInfoLabel(body);
        title.Text = "Mobile Factory Demo";
        title.AddThemeFontSizeOverride("font_size", 22);

        var subtitle = CreateInfoLabel(body);
        subtitle.Text = "左侧保持世界观察，右侧管理内部编辑；移动工厂在世界里具备运输、自动部署和回收表现。";
        subtitle.Modulate = new Color("A8B8C6");

        _modeLabel = CreateInfoLabel(body);
        _stateLabel = CreateInfoLabel(body);
        _hoverLabel = CreateInfoLabel(body);
        _previewLabel = CreateInfoLabel(body);
        _deliveryLabel = CreateInfoLabel(body);
        _focusLabel = CreateInfoLabel(body);
        _hintLabel = CreateInfoLabel(body);

        var actionsRow = new HBoxContainer();
        actionsRow.MouseFilter = Control.MouseFilterEnum.Pass;
        actionsRow.AddThemeConstantOverride("separation", 8);
        body.AddChild(actionsRow);

        _observerButton = new Button
        {
            Text = "进入观察模式 (Tab)",
            ToggleMode = true,
            CustomMinimumSize = new Vector2(146.0f, 34.0f)
        };
        _observerButton.Pressed += () => ObserverModeToggleRequested?.Invoke();
        actionsRow.AddChild(_observerButton);

        _deployButton = new Button
        {
            Text = "部署模式 (G)",
            ToggleMode = true,
            CustomMinimumSize = new Vector2(126.0f, 34.0f)
        };
        _deployButton.Pressed += () => DeployModeToggleRequested?.Invoke();
        actionsRow.AddChild(_deployButton);

        _hintLabel.Text = "默认操作：W/S 前进后退 | A/D 转向 | G 部署预览 | Tab 观察模式 | R 上下文辅助 | F 内部编辑 | 1-0/-/= 切换内部建造/端口";
        _hintLabel.Modulate = new Color("EED49F");
    }

    private void BuildEditorPanel()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(panel);
        _editorPanel = panel;

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Pass;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        panel.AddChild(body);
        _editorBody = body;

        _editorModeLabel = CreateEditorLabel(body);
        _selectionLabel = CreateEditorLabel(body);
        _editorPreviewLabel = CreateEditorLabel(body);
        _portStatusLabel = CreateEditorLabel(body);
        BuildEditorToolbar(body);

        var viewport = new SubViewport();
        viewport.TransparentBg = false;
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        _editorViewport = viewport;

        var viewportRect = new TextureRect();
        viewportRect.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        viewportRect.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        viewportRect.StretchMode = TextureRect.StretchModeEnum.Scale;
        viewportRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        viewportRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportRect.Texture = viewport.GetTexture();
        body.AddChild(viewportRect);
        _editorViewportRect = viewportRect;

        AddChild(viewport);
    }

    private void UpdateLayout()
    {
        if (_worldFocusFrame is null || _infoPanel is null || _editorPanel is null || _editorViewport is null || _editorViewportRect is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var margin = new Vector2(18.0f, 18.0f);
        var worldWidth = viewportSize.X / 6.0f;
        var infoWidth = Mathf.Max(300.0f, viewportSize.X / 6.0f - margin.X * 2.0f);
        var infoHeight = Mathf.Max(280.0f, viewportSize.Y * 0.35f);

        _worldFocusFrame.Position = Vector2.Zero;
        _worldFocusFrame.Size = new Vector2(worldWidth, viewportSize.Y);
        _infoPanel.Position = margin;
        _infoPanel.Size = new Vector2(infoWidth, infoHeight);
        _infoPanel.Visible = _editorProgress < 0.01f;

        var editorWidth = viewportSize.X * 5.0f / 6.0f;
        var closedLeft = viewportSize.X + 12.0f;
        var openLeft = worldWidth;
        var left = Mathf.Lerp(closedLeft, openLeft, _editorProgress);

        _editorPanel.Position = new Vector2(left, 0.0f);
        _editorPanel.Size = new Vector2(editorWidth, viewportSize.Y);
        _editorViewportRect.CustomMinimumSize = new Vector2(
            320.0f,
            Mathf.Max(240.0f, viewportSize.Y - 220.0f));

        var viewportSize2D = new Vector2I(
            Mathf.Max(320, Mathf.RoundToInt(_editorViewportRect.Size.X)),
            Mathf.Max(240, Mathf.RoundToInt(_editorViewportRect.Size.Y)));

        if (_editorViewport.Size != viewportSize2D)
        {
            _editorViewport.Size = viewportSize2D;
        }

        RefreshFocusVisuals();
    }

    private static Label CreateInfoLabel(Container parent)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
        return label;
    }

    private static Label CreateEditorLabel(Container parent)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
        return label;
    }

    private void BuildEditorToolbar(Container parent)
    {
        var paletteGrid = new GridContainer();
        paletteGrid.Columns = 4;
        paletteGrid.AddThemeConstantOverride("h_separation", 8);
        paletteGrid.AddThemeConstantOverride("v_separation", 8);
        parent.AddChild(paletteGrid);

        foreach (var kind in EditorPalette)
        {
            var button = new Button();
            button.Text = GetKindLabel(kind);
            button.ToggleMode = true;
            button.CustomMinimumSize = new Vector2(96.0f, 34.0f);
            button.Pressed += () => EditorPaletteSelected?.Invoke(kind);
            paletteGrid.AddChild(button);
            _paletteButtons[kind] = button;
        }

        var rotateRow = new HBoxContainer();
        rotateRow.AddThemeConstantOverride("separation", 8);
        parent.AddChild(rotateRow);

        var rotateLeft = new Button();
        rotateLeft.Text = "旋左";
        rotateLeft.CustomMinimumSize = new Vector2(92.0f, 30.0f);
        rotateLeft.Pressed += () => EditorRotateRequested?.Invoke(-1);
        rotateRow.AddChild(rotateLeft);

        var rotateRight = new Button();
        rotateRight.Text = "旋右";
        rotateRight.CustomMinimumSize = new Vector2(92.0f, 30.0f);
        rotateRight.Pressed += () => EditorRotateRequested?.Invoke(1);
        rotateRow.AddChild(rotateRight);
    }

    private void RefreshFocusVisuals()
    {
        if (_worldFocusFrame is null || _editorPanel is null)
        {
            return;
        }

        var showFocus = _editorOpen && _editorProgress > 0.01f;
        _worldFocusFrame.Visible = showFocus;

        _worldFocusFrame.AddThemeStyleboxOverride("panel", CreateOutlineOnlyStyle(showFocus && !_editorFocused ? WorldFocusColor : Colors.Transparent));
        _editorPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(showFocus && _editorFocused ? EditorFocusColor : Colors.Transparent));
    }

    private static StyleBoxFlat CreateOutlineOnlyStyle(Color borderColor)
    {
        return new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = borderColor,
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 12,
            ContentMarginTop = 12,
            ContentMarginRight = 12,
            ContentMarginBottom = 12
        };
    }

    private static StyleBoxFlat CreatePanelStyle(Color borderColor)
    {
        var style = CreateOutlineOnlyStyle(borderColor);
        style.BgColor = new Color(0.05f, 0.08f, 0.12f, 0.84f);
        return style;
    }

    private void RefreshPaletteButtons(BuildPrototypeKind selectedKind)
    {
        foreach (var pair in _paletteButtons)
        {
            pair.Value.ButtonPressed = pair.Key == selectedKind;
        }
    }

    private static string GetKindLabel(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Producer => "生产器",
            BuildPrototypeKind.Belt => "传送带",
            BuildPrototypeKind.Splitter => "分流器",
            BuildPrototypeKind.Merger => "合并器",
            BuildPrototypeKind.Bridge => "跨桥",
            BuildPrototypeKind.Loader => "装载器",
            BuildPrototypeKind.Unloader => "卸载器",
            BuildPrototypeKind.Storage => "仓储",
            BuildPrototypeKind.Inserter => "机械臂",
            BuildPrototypeKind.OutputPort => "输出端口",
            BuildPrototypeKind.InputPort => "输入端口",
            BuildPrototypeKind.Sink => "回收器",
            _ => kind.ToString()
        };
    }
}
