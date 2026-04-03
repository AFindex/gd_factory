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
        BuildPrototypeKind.Wall,
        BuildPrototypeKind.AmmoAssembler,
        BuildPrototypeKind.GunTurret,
        BuildPrototypeKind.OutputPort,
        BuildPrototypeKind.InputPort
    };

    private PanelContainer? _worldFocusFrame;
    private Control? _overlayRoot;
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
    private Label? _selectionTargetLabel;
    private Label? _editorPreviewLabel;
    private Label? _portStatusLabel;
    private Label? _combatLabel;
    private PanelContainer? _inspectionPanel;
    private Label? _inspectionTitleLabel;
    private Label? _inspectionBodyLabel;
    private Label? _focusLabel;
    private FactoryStructureDetailWindow? _detailWindow;
    private Button? _observerButton;
    private Button? _deployButton;
    private readonly Dictionary<BuildPrototypeKind, Button> _paletteButtons = new();
    private float _editorProgress;
    private bool _editorOpen;
    private bool _editorFocused;

    private static readonly Color EditorFocusColor = new("7DD3FC");
    private static readonly Color WorldFocusColor = new("FDE68A");
    private const float EditorSidebarWidth = 292.0f;

    public SubViewport EditorViewport => _editorViewport!;
    public bool IsEditorVisible => _editorProgress > 0.01f;
    public string PortStatusText => _portStatusLabel?.Text ?? string.Empty;
    public bool IsDetailVisible => _detailWindow?.IsShowing ?? false;
    public string DetailTitleText => _detailWindow?.CurrentTitleText ?? string.Empty;
    public event Action<BuildPrototypeKind>? EditorPaletteSelected;
    public event Action<int>? EditorRotateRequested;
    public event Action? ObserverModeToggleRequested;
    public event Action? DeployModeToggleRequested;
    public event Action<string, Vector2I, Vector2I>? EditorDetailInventoryMoveRequested;
    public event Action<string>? EditorDetailRecipeSelected;
    public event Action? EditorDetailClosed;

    public override void _Ready()
    {
        Name = "MobileFactoryHud";

        var overlayRoot = new Control();
        overlayRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlayRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(overlayRoot);
        _overlayRoot = overlayRoot;

        BuildInfoPanel();
        BuildEditorPanel();
        if (_overlayRoot is not null)
        {
            MoveChild(_overlayRoot, GetChildCount() - 1);
        }
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

    public void SetEditorSelection(FactoryInteractionMode interactionMode, BuildPrototypeKind? kind, FacingDirection facing)
    {
        if (_selectionLabel is null)
        {
            return;
        }

        if (interactionMode == FactoryInteractionMode.Build && kind.HasValue)
        {
            _selectionLabel.Text = $"内部模式：建造 | {GetKindLabel(kind.Value)} | 朝向 {FactoryDirection.ToLabel(facing)}";
            RefreshPaletteButtons(kind.Value);
            return;
        }

        if (interactionMode == FactoryInteractionMode.Delete)
        {
            _selectionLabel.Text = "内部模式：删除 | X 切换，右键退出，Shift 可框选删除";
            RefreshPaletteButtons(null);
            return;
        }

        _selectionLabel.Text = "内部模式：交互 | 点击建筑查看状态";
        RefreshPaletteButtons(null);
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

    public void SetCombatStats(int activeEnemies, int kills, int structuresLost)
    {
        if (_combatLabel is not null)
        {
            _combatLabel.Text = $"世界威胁：敌人 {activeEnemies} | 击杀 {kills} | 损失建筑 {structuresLost}";
            _combatLabel.Modulate = activeEnemies > 0 ? new Color("FCA5A5") : new Color("FDE68A");
        }
    }

    public void SetEditorSelectionTarget(string text)
    {
        if (_selectionTargetLabel is not null)
        {
            _selectionTargetLabel.Text = $"内部选中：{text}";
        }
    }

    public void SetEditorInspection(string? title, string? body)
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

    public void SetEditorState(bool isOpen, MobileFactoryLifecycleState lifecycleState, int structureCount, FactoryInteractionMode interactionMode)
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
        var interactionText = interactionMode switch
        {
            FactoryInteractionMode.Build => "建造模式",
            FactoryInteractionMode.Delete => "删除模式",
            _ => "交互模式"
        };
        _editorModeLabel.Text = $"内部编辑：{paneText} | 生命周期：{stateText} | {interactionText} | 当前内部件数：{structureCount}";
    }

    public void SetHintText(string text)
    {
        if (_hintLabel is not null)
        {
            _hintLabel.Text = text;
        }
    }

    public void SetEditorStructureDetails(FactoryStructureDetailModel? model)
    {
        if (_detailWindow is null || _editorPanel is null)
        {
            return;
        }

        if (model is null)
        {
            _detailWindow.HideWindow();
            return;
        }

        _detailWindow.ShowDetails(model, _editorPanel.Position + new Vector2(28.0f, 24.0f));
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
        _combatLabel = CreateInfoLabel(body);
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

        _hintLabel.Text = "默认操作：W/S 前进后退 | A/D 转向 | G 部署预览 | Tab 观察模式 | R 上下文辅助 | F 内部编辑 | 1-0/-/= 快速切物流件，右侧按钮可直接选墙体/弹药组装器/机枪炮塔";
        _hintLabel.Modulate = new Color("EED49F");
    }

    private void BuildEditorPanel()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(panel);
        _editorPanel = panel;

        var chrome = new MarginContainer();
        chrome.MouseFilter = Control.MouseFilterEnum.Pass;
        chrome.AddThemeConstantOverride("margin_left", 10);
        chrome.AddThemeConstantOverride("margin_top", 10);
        chrome.AddThemeConstantOverride("margin_right", 10);
        chrome.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(chrome);

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Pass;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 0);
        chrome.AddChild(body);
        _editorBody = body;

        var row = new HBoxContainer();
        row.MouseFilter = Control.MouseFilterEnum.Pass;
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("separation", 10);
        body.AddChild(row);

        var viewportPanel = new PanelContainer();
        viewportPanel.MouseFilter = Control.MouseFilterEnum.Pass;
        viewportPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        viewportPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        viewportPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("2A4E66")));
        row.AddChild(viewportPanel);

        var viewportMargin = new MarginContainer();
        viewportMargin.MouseFilter = Control.MouseFilterEnum.Pass;
        viewportMargin.AddThemeConstantOverride("margin_left", 8);
        viewportMargin.AddThemeConstantOverride("margin_top", 8);
        viewportMargin.AddThemeConstantOverride("margin_right", 8);
        viewportMargin.AddThemeConstantOverride("margin_bottom", 8);
        viewportPanel.AddChild(viewportMargin);

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
        viewportMargin.AddChild(viewportRect);
        _editorViewportRect = viewportRect;

        var sidebarPanel = new PanelContainer();
        sidebarPanel.MouseFilter = Control.MouseFilterEnum.Pass;
        sidebarPanel.CustomMinimumSize = new Vector2(EditorSidebarWidth, 0.0f);
        sidebarPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        sidebarPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("3F6D8D")));
        row.AddChild(sidebarPanel);

        var sidebarMargin = new MarginContainer();
        sidebarMargin.MouseFilter = Control.MouseFilterEnum.Pass;
        sidebarMargin.AddThemeConstantOverride("margin_left", 10);
        sidebarMargin.AddThemeConstantOverride("margin_top", 10);
        sidebarMargin.AddThemeConstantOverride("margin_right", 10);
        sidebarMargin.AddThemeConstantOverride("margin_bottom", 10);
        sidebarPanel.AddChild(sidebarMargin);

        var sidebar = new VBoxContainer();
        sidebar.MouseFilter = Control.MouseFilterEnum.Pass;
        sidebar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sidebar.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        sidebar.AddThemeConstantOverride("separation", 6);
        sidebarMargin.AddChild(sidebar);

        var editorTitle = CreateEditorLabel(sidebar, 18, Colors.White);
        editorTitle.Text = "内部建造";

        var editorNote = CreateEditorLabel(sidebar, 12, new Color("8FD3FF"));
        editorNote.Text = "右侧管理建造与状态，左侧保留编辑视口。";

        _editorModeLabel = CreateEditorLabel(sidebar, 12, new Color("D7E6F2"));
        _selectionLabel = CreateEditorLabel(sidebar, 12, new Color("FDE68A"));
        _selectionTargetLabel = CreateEditorLabel(sidebar, 12, new Color("D7E6F2"));
        _editorPreviewLabel = CreateEditorLabel(sidebar, 12, new Color("D7E6F2"));
        _portStatusLabel = CreateEditorLabel(sidebar, 12, new Color("FDE68A"));

        _inspectionPanel = new PanelContainer
        {
            Visible = false
        };
        sidebar.AddChild(_inspectionPanel);
        var inspectionBody = new VBoxContainer();
        inspectionBody.AddThemeConstantOverride("separation", 4);
        _inspectionPanel.AddChild(inspectionBody);
        _inspectionTitleLabel = CreateEditorLabel(inspectionBody, 12, new Color("FDE68A"));
        _inspectionBodyLabel = CreateEditorLabel(inspectionBody, 11, new Color("D7E6F2"));

        BuildEditorToolbar(sidebar);

        AddChild(viewport);

        _detailWindow = new FactoryStructureDetailWindow();
        _detailWindow.InventoryMoveRequested += (inventoryId, fromSlot, toSlot) => EditorDetailInventoryMoveRequested?.Invoke(inventoryId, fromSlot, toSlot);
        _detailWindow.RecipeSelected += recipeId => EditorDetailRecipeSelected?.Invoke(recipeId);
        _detailWindow.CloseRequested += () => EditorDetailClosed?.Invoke();
        _overlayRoot?.AddChild(_detailWindow);
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
        var infoWidth = Mathf.Clamp(viewportSize.X * 0.24f, 270.0f, 340.0f);
        var infoHeight = Mathf.Clamp(viewportSize.Y * 0.31f, 220.0f, 300.0f);

        _worldFocusFrame.Position = Vector2.Zero;
        _worldFocusFrame.Size = new Vector2(worldWidth, viewportSize.Y);
        _infoPanel.Size = new Vector2(infoWidth, infoHeight);
        UpdateInfoPanelTransition(margin);

        var editorWidth = viewportSize.X * 5.0f / 6.0f;
        var closedLeft = viewportSize.X + 12.0f;
        var openLeft = worldWidth;
        var left = Mathf.Lerp(closedLeft, openLeft, _editorProgress);

        _editorPanel.Position = new Vector2(left, 0.0f);
        _editorPanel.Size = new Vector2(editorWidth, viewportSize.Y);
        _editorViewportRect.CustomMinimumSize = new Vector2(
            320.0f,
            Mathf.Max(240.0f, viewportSize.Y - 36.0f));

        var viewportSize2D = new Vector2I(
            Mathf.Max(320, Mathf.RoundToInt(_editorViewportRect.Size.X)),
            Mathf.Max(240, Mathf.RoundToInt(_editorViewportRect.Size.Y)));

        if (_editorViewport.Size != viewportSize2D)
        {
            _editorViewport.Size = viewportSize2D;
        }

        _detailWindow?.SetDragBounds(new Rect2(_editorPanel.Position, _editorPanel.Size));
        RefreshFocusVisuals();
    }

    private void UpdateInfoPanelTransition(Vector2 basePosition)
    {
        if (_infoPanel is null || _infoBody is null)
        {
            return;
        }

        var visibility = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, _editorProgress);
        var hiddenOffset = new Vector2(-54.0f, 0.0f);
        _infoPanel.Position = basePosition + hiddenOffset * (1.0f - visibility);
        _infoPanel.Modulate = new Color(1.0f, 1.0f, 1.0f, visibility);
        _infoPanel.Visible = visibility > 0.015f;

        var canInteract = visibility > 0.985f;
        _infoPanel.MouseFilter = canInteract ? Control.MouseFilterEnum.Pass : Control.MouseFilterEnum.Ignore;
        _infoBody.MouseFilter = canInteract ? Control.MouseFilterEnum.Pass : Control.MouseFilterEnum.Ignore;
    }

    private static Label CreateInfoLabel(Container parent)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
        return label;
    }

    private static Label CreateEditorLabel(Container parent, int fontSize = 13, Color? color = null)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Modulate = color ?? new Color("D7E6F2");
        parent.AddChild(label);
        return label;
    }

    private void BuildEditorToolbar(Container parent)
    {
        var paletteGrid = new GridContainer();
        paletteGrid.Columns = 3;
        paletteGrid.AddThemeConstantOverride("h_separation", 6);
        paletteGrid.AddThemeConstantOverride("v_separation", 6);
        parent.AddChild(paletteGrid);

        foreach (var kind in EditorPalette)
        {
            var button = new Button();
            button.Text = GetKindLabel(kind);
            button.ToggleMode = true;
            button.CustomMinimumSize = new Vector2(82.0f, 28.0f);
            button.AddThemeFontSizeOverride("font_size", 11);
            button.Pressed += () => EditorPaletteSelected?.Invoke(kind);
            paletteGrid.AddChild(button);
            _paletteButtons[kind] = button;
        }

        var rotateRow = new HBoxContainer();
        rotateRow.AddThemeConstantOverride("separation", 6);
        parent.AddChild(rotateRow);

        var rotateLeft = new Button();
        rotateLeft.Text = "旋左";
        rotateLeft.CustomMinimumSize = new Vector2(0.0f, 28.0f);
        rotateLeft.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rotateLeft.AddThemeFontSizeOverride("font_size", 11);
        rotateLeft.Pressed += () => EditorRotateRequested?.Invoke(-1);
        rotateRow.AddChild(rotateLeft);

        var rotateRight = new Button();
        rotateRight.Text = "旋右";
        rotateRight.CustomMinimumSize = new Vector2(0.0f, 28.0f);
        rotateRight.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rotateRight.AddThemeFontSizeOverride("font_size", 11);
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

    private void RefreshPaletteButtons(BuildPrototypeKind? selectedKind)
    {
        foreach (var pair in _paletteButtons)
        {
            pair.Value.ButtonPressed = selectedKind.HasValue && pair.Key == selectedKind.Value;
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
            BuildPrototypeKind.Wall => "墙体",
            BuildPrototypeKind.AmmoAssembler => "弹药组装器",
            BuildPrototypeKind.GunTurret => "机枪炮塔",
            BuildPrototypeKind.OutputPort => "输出端口",
            BuildPrototypeKind.InputPort => "输入端口",
            BuildPrototypeKind.Sink => "回收器",
            _ => kind.ToString()
        };
    }
}
