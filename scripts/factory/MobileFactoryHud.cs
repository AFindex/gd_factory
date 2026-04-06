using Godot;
using System;
using System.Collections.Generic;

public partial class MobileFactoryHud : CanvasLayer
{
    private const string CommandWorkspaceId = "command";
    private const string EditorWorkspaceId = "editor";
    private const string BlueprintWorkspaceId = "blueprints";
    private const string DetailsWorkspaceId = "details";
    private const string OverviewWorkspaceId = "overview";
    private const string BuildTestWorkspaceId = "build-test";
    private const string DiagnosticsWorkspaceId = "diagnostics";
    private const float EditorSidebarWidth = 292.0f;
    private const int CompactTabFontSize = 10;
    private static readonly (string Title, BuildPrototypeKind[] Kinds)[] EditorPaletteCategories =
    {
        ("物流", new[]
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
        ("生产电力", new[]
        {
            BuildPrototypeKind.Generator,
            BuildPrototypeKind.PowerPole,
            BuildPrototypeKind.Smelter,
            BuildPrototypeKind.Assembler
        }),
        ("防御", new[]
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
        }),
        ("边界接口", new[]
        {
            BuildPrototypeKind.OutputPort,
            BuildPrototypeKind.InputPort,
            BuildPrototypeKind.MiningInputPort
        })
    };

    private static readonly Color EditorFocusColor = new("7DD3FC");
    private static readonly Color WorldFocusColor = new("FDE68A");

    private readonly Dictionary<BuildPrototypeKind, Button> _paletteButtons = new();
    private readonly Dictionary<string, Control> _worldWorkspacePanels = new();
    private readonly Dictionary<string, Control> _editorWorkspacePanels = new();

    private PanelContainer? _topChromePanel;
    private FactoryWorkspaceChrome? _workspaceChrome;
    private PanelContainer? _worldFocusFrame;
    private Control? _overlayRoot;
    private PanelContainer? _infoPanel;
    private PanelContainer? _editorPanel;
    private TextureRect? _editorViewportRect;
    private SubViewport? _editorViewport;
    private Label? _modeLabel;
    private Label? _stateLabel;
    private Label? _hoverLabel;
    private Label? _previewLabel;
    private Label? _deliveryLabel;
    private Label? _diagnosticsDeliveryLabel;
    private Label? _hintLabel;
    private Label? _diagnosticsHintLabel;
    private Label? _editorModeLabel;
    private Label? _selectionLabel;
    private Label? _selectionTargetLabel;
    private Label? _editorPreviewLabel;
    private Label? _portStatusLabel;
    private Label? _combatLabel;
    private Label? _diagnosticsCombatLabel;
    private Label? _focusLabel;
    private Label? _diagnosticsFocusLabel;
    private Label? _factoryDetailLabel;
    private Label? _worldWorkspaceHintLabel;
    private Label? _editorWorkspaceHintLabel;
    private PanelContainer? _inspectionPanel;
    private Label? _inspectionTitleLabel;
    private Label? _inspectionBodyLabel;
    private FactoryStructureDetailWindow? _detailWindow;
    private FactoryBlueprintPanel? _blueprintPanel;
    private Button? _factoryCommandButton;
    private Button? _observerButton;
    private Button? _deployButton;
    private float _editorProgress;
    private bool _editorOpen;
    private bool _editorFocused;

    public bool UseLargeScenarioWorkspaces { get; set; }
    public SubViewport EditorViewport => _editorViewport!;
    public bool IsEditorVisible => _editorProgress > 0.01f;
    public string PortStatusText => _portStatusLabel?.Text ?? string.Empty;
    public bool IsDetailVisible => _detailWindow?.IsShowing ?? false;
    public string DetailTitleText => _detailWindow?.CurrentTitleText ?? string.Empty;
    public string ActiveWorkspaceId => _workspaceChrome?.ActiveWorkspaceId ?? string.Empty;

    public event Action<BuildPrototypeKind>? EditorPaletteSelected;
    public event Action<int>? EditorRotateRequested;
    public event Action? FactoryCommandModeToggleRequested;
    public event Action? ObserverModeToggleRequested;
    public event Action? DeployModeToggleRequested;
    public event Action<string, Vector2I, Vector2I, bool>? EditorDetailInventoryMoveRequested;
    public event Action<string>? EditorDetailRecipeSelected;
    public event Action<string>? EditorDetailActionRequested;
    public event Action? EditorDetailClosed;
    public event Action? BlueprintCaptureSelectionRequested;
    public event Action? BlueprintCaptureFullRequested;
    public event Action<string>? BlueprintSaveRequested;
    public event Action<string>? BlueprintSelected;
    public event Action? BlueprintApplyRequested;
    public event Action? BlueprintConfirmRequested;
    public event Action<string>? BlueprintDeleteRequested;
    public event Action? BlueprintCancelRequested;
    public event Action<string>? WorkspaceSelected;

    public override void _Ready()
    {
        Name = "MobileFactoryHud";
        var overlayRoot = new Control();
        overlayRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlayRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(overlayRoot);
        _overlayRoot = overlayRoot;
        BuildTopChrome();
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

    public IReadOnlyList<string> GetWorkspaceIds() => _workspaceChrome?.GetWorkspaceIds() ?? Array.Empty<string>();

    public bool IsWorkspaceVisible(string workspaceId)
    {
        return (_worldWorkspacePanels.TryGetValue(workspaceId, out var worldPanel) && worldPanel.Visible)
            || (_editorWorkspacePanels.TryGetValue(workspaceId, out var editorPanel) && editorPanel.Visible);
    }

    public void SelectWorkspace(string workspaceId) => _workspaceChrome?.SetActiveWorkspace(workspaceId);

    public void SetEditorOpen(bool isOpen) => _editorOpen = isOpen;

    public bool IsPointerOverEditor(Vector2 mousePosition)
        => _editorPanel is not null && _editorProgress > 0.01f && _editorPanel.GetGlobalRect().HasPoint(mousePosition);

    public bool IsPointerOverEditorViewport(Vector2 mousePosition)
        => _editorViewportRect is not null && _editorProgress > 0.01f && _editorViewportRect.GetGlobalRect().HasPoint(mousePosition);

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
                MobileFactoryControlMode.Player => "玩家控制",
                MobileFactoryControlMode.FactoryCommand => "工厂控制",
                MobileFactoryControlMode.DeployPreview => "部署预览",
                MobileFactoryControlMode.Observer => "观察模式",
                _ => "工厂控制"
            };
            _modeLabel.Text = $"当前模式：{modeText} | 行进朝向：{FactoryDirection.ToLabel(transitFacing)} | 部署朝向：{FactoryDirection.ToLabel(deployFacing)}";
            _modeLabel.Modulate = controlMode switch
            {
                MobileFactoryControlMode.Player => new Color("A7F3A0"),
                MobileFactoryControlMode.Observer => new Color("7DD3FC"),
                _ => new Color("FDE68A")
            };
        }

        if (_factoryCommandButton is not null)
        {
            _factoryCommandButton.Text = controlMode == MobileFactoryControlMode.FactoryCommand
                ? "返回玩家控制 (C)"
                : "进入工厂控制 (C)";
            _factoryCommandButton.ButtonPressed = controlMode == MobileFactoryControlMode.FactoryCommand;
            _factoryCommandButton.Disabled = lifecycleState == MobileFactoryLifecycleState.Recalling;
        }

        if (_observerButton is not null)
        {
            _observerButton.Text = controlMode == MobileFactoryControlMode.Observer ? "返回玩家控制 (Tab)" : "进入观察模式 (Tab)";
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
        if (_hoverLabel is not null)
        {
            _hoverLabel.Text = hasHover ? $"当前锚点：({anchorCell.X}, {anchorCell.Y})" : "当前锚点：未选择";
        }
    }

    public void SetPreviewStatus(FactoryStatusTone tone, string text)
    {
        if (_previewLabel is not null)
        {
            _previewLabel.Text = $"世界提示：{text}";
            _previewLabel.Modulate = tone switch
            {
                FactoryStatusTone.Positive => new Color("A7F3A0"),
                FactoryStatusTone.Warning => new Color("FDE68A"),
                _ => new Color("FFB4A2")
            };
        }
    }

    public void SetDeliveryStats(int sinkA, int sinkB)
    {
        if (_deliveryLabel is not null)
        {
            _deliveryLabel.Text = $"演示回收站：A 线路累计 {sinkA} | B 线路累计 {sinkB}";
        }

        if (_diagnosticsDeliveryLabel is not null)
        {
            _diagnosticsDeliveryLabel.Text = $"演示回收站：A 线路累计 {sinkA} | B 线路累计 {sinkB}";
        }
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
        }
        else if (interactionMode == FactoryInteractionMode.Delete)
        {
            _selectionLabel.Text = "内部模式：删除 | X 切换，右键退出，Shift 可框选删除";
            RefreshPaletteButtons(null);
        }
        else
        {
            _selectionLabel.Text = "内部模式：交互 | 点击建筑查看状态";
            RefreshPaletteButtons(null);
        }
    }

    public void SetEditorPreview(bool isValid, string text)
    {
        if (_editorPreviewLabel is not null)
        {
            _editorPreviewLabel.Text = $"内部预览：{text}";
            _editorPreviewLabel.Modulate = isValid ? new Color("A7F3A0") : new Color("FFB4A2");
        }
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

        if (_diagnosticsCombatLabel is not null)
        {
            _diagnosticsCombatLabel.Text = $"世界威胁：敌人 {activeEnemies} | 击杀 {kills} | 损失建筑 {structuresLost}";
            _diagnosticsCombatLabel.Modulate = activeEnemies > 0 ? new Color("FCA5A5") : new Color("FDE68A");
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
        if (_inspectionPanel is not null && _inspectionTitleLabel is not null && _inspectionBodyLabel is not null)
        {
            var isVisible = !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body);
            _inspectionPanel.Visible = isVisible;
            _inspectionTitleLabel.Text = title ?? string.Empty;
            _inspectionBodyLabel.Text = body ?? string.Empty;
        }
    }

    public void SetEditorFocusHint(bool overEditor)
    {
        if (_focusLabel is not null)
        {
            _focusLabel.Text = overEditor ? "鼠标焦点：内部编辑区" : "鼠标焦点：大世界";
        }

        if (_diagnosticsFocusLabel is not null)
        {
            _diagnosticsFocusLabel.Text = overEditor ? "鼠标焦点：内部编辑区" : "鼠标焦点：大世界";
        }
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

        if (_diagnosticsHintLabel is not null)
        {
            _diagnosticsHintLabel.Text = text;
        }
    }

    public void SetFactoryDetails(string text)
    {
        if (_factoryDetailLabel is not null)
        {
            _factoryDetailLabel.Text = text;
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
        }
        else
        {
            _detailWindow.ShowDetails(model, _editorPanel.Position + new Vector2(28.0f, 24.0f));
        }
    }

    public void SetBlueprintState(FactoryBlueprintPanelState state)
    {
        _blueprintPanel?.SetState(state);
        if (state.PendingCaptureId is not null || state.CanConfirmApply || state.ModeText.Contains("框选", StringComparison.Ordinal) || state.ModeText.Contains("应用预览", StringComparison.Ordinal))
        {
            SetActiveWorkspace(BlueprintWorkspaceId, emitSignal: false);
        }
    }

    public bool BlocksInput(Control? control)
    {
        if (_detailWindow?.BlocksInput(control) ?? false)
        {
            return true;
        }

        if (_blueprintPanel?.BlocksInput(control) ?? false)
        {
            return true;
        }

        return BlocksInteractiveInput(control, _topChromePanel)
            || BlocksInteractiveInput(control, _infoPanel)
            || BlocksInteractiveInput(control, _editorPanel);
    }
}
