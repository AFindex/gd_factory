using Godot;
using System;
using System.Collections.Generic;

public partial class MobileFactoryHud : CanvasLayer
{
    private enum DetailSurfaceContext
    {
        None,
        World,
        Editor
    }

    private const string CommandWorkspaceId = "command";
    private const string WorldBuildWorkspaceId = "world-build";
    private const string EditorWorkspaceId = "editor";
    private const string TestingWorkspaceId = "testing";
    private const string BlueprintWorkspaceId = "blueprints";
    private const string DetailsWorkspaceId = "details";
    private const string OverviewWorkspaceId = "overview";
    private const string BuildTestWorkspaceId = "build-test";
    private const string DiagnosticsWorkspaceId = "diagnostics";
    private const string SavesWorkspaceId = "saves";
    private const float EditorSidebarWidth = 292.0f;
    private const int CompactTabFontSize = 10;

    private static readonly Color EditorFocusColor = FactoryUiTheme.BorderStrong;
    private static readonly Color WorldFocusColor = FactoryUiTheme.Border;

    private readonly Dictionary<BuildPrototypeKind, Button> _paletteButtons = new();
    private readonly Dictionary<BuildPrototypeKind, Button> _worldBuildButtons = new();
    private readonly Dictionary<string, Control> _worldWorkspacePanels = new();
    private readonly Dictionary<string, Control> _editorWorkspacePanels = new();

    private PanelContainer? _topChromePanel;
    private Control? _overviewHeaderChromeHost;
    private Button? _overviewCollapseButton;
    private FactoryWorkspaceChrome? _workspaceChrome;
    private PanelContainer? _worldFocusFrame;
    private StyleBoxFlat? _worldFocusFrameStyle;
    private Control? _overlayRoot;
    private PanelContainer? _infoPanel;
    private PanelContainer? _editorViewportPanel;
    private StyleBoxFlat? _editorViewportPanelStyle;
    private PanelContainer? _editorPanel;
    private StyleBoxFlat? _editorPanelStyle;
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
    private Label? _worldBuildSelectionLabel;
    private Label? _worldBuildRotationLabel;
    private Label? _worldSelectionTargetLabel;
    private PanelContainer? _worldInspectionPanel;
    private Label? _worldInspectionTitleLabel;
    private Label? _worldInspectionBodyLabel;
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
    private Label? _saveStatusLabel;
    private Label? _saveLibraryStatusLabel;
    private VBoxContainer? _saveLibraryList;
    private LineEdit? _saveWorkspaceSlotEdit;
    private ConfirmationDialog? _overwriteSaveDialog;
    private Label? _testingEditorStateLabel;
    private Label? _testingSelectionTargetLabel;
    private Label? _testingPreviewLabel;
    private Label? _testingPortStatusLabel;
    private Label? _testingHintLabel;
    private Label? _testingPersistenceLabel;
    private PanelContainer? _inspectionPanel;
    private Label? _inspectionTitleLabel;
    private Label? _inspectionBodyLabel;
    private FactoryStructureDetailWindow? _detailWindow;
    private FactoryBlueprintPanel? _blueprintPanel;
    private Button? _factoryCommandButton;
    private Button? _observerButton;
    private Button? _deployButton;
    private Button? _editModeButton;
    private Button? _editorBuildModeButton;
    private Button? _editorInteractionModeButton;
    private Button? _editorDeleteModeButton;
    private float _editorProgress;
    private float _overviewCollapseProgress;
    private bool _editorOpen;
    private bool _editorViewportFocused;
    private bool _editorOperationFocused;
    private bool _overviewCollapsed;
    private FactoryInteractionMode _editorInteractionMode = FactoryInteractionMode.Interact;
    private DetailSurfaceContext _detailSurfaceContext;
    private Action? _pendingOverwriteSaveAction;

    public bool UseLargeScenarioWorkspaces { get; set; }
    public SubViewport EditorViewport => _editorViewport!;
    public bool IsEditorVisible => _editorOpen;
    public string PortStatusText => _portStatusLabel?.Text ?? _testingPortStatusLabel?.Text ?? string.Empty;
    public bool IsDetailVisible => _detailWindow?.IsShowing ?? false;
    public bool HasActiveInventoryInteraction => _detailWindow?.HasActiveInventoryInteraction ?? false;
    public string DetailTitleText => _detailWindow?.CurrentTitleText ?? string.Empty;
    public string ActiveWorkspaceId => _workspaceChrome?.ActiveWorkspaceId ?? string.Empty;
    public bool IsOverviewCollapsed => _overviewCollapsed;
    public string OverviewCollapseButtonText => _overviewCollapseButton?.Text ?? string.Empty;
    public float OverviewVisibleWidth
    {
        get
        {
            if (_infoPanel is null)
            {
                return 0.0f;
            }

            var rect = _infoPanel.GetGlobalRect();
            var viewportRect = GetViewport().GetVisibleRect();
            var visibleLeft = Mathf.Max(rect.Position.X, viewportRect.Position.X);
            var visibleRight = Mathf.Min(rect.End.X, viewportRect.End.X);
            return Mathf.Max(0.0f, visibleRight - visibleLeft);
        }
    }
    public bool IsEditorOperationPanelBuildFocused => _editorModeLabel is null
        && _selectionLabel is null
        && _editorPreviewLabel is null
        && _inspectionPanel is null;
    public bool IsWorkspaceChromeEmbeddedInOverview => _workspaceChrome is not null
        && _overviewHeaderChromeHost is not null
        && _workspaceChrome.GetParent() == _topChromePanel
        && _topChromePanel?.GetParent() == _overviewHeaderChromeHost;

    public event Action<BuildPrototypeKind>? EditorPaletteSelected;
    public event Action<BuildPrototypeKind?>? WorldBuildSelectionChanged;
    public event Action<int>? EditorRotateRequested;
    public event Action? EditModeToggleRequested;
    public event Action? EditorBuildModeRequested;
    public event Action? EditorInteractionModeRequested;
    public event Action? EditorDeleteModeRequested;
    public event Action? FactoryCommandModeToggleRequested;
    public event Action? ObserverModeToggleRequested;
    public event Action? DeployModeToggleRequested;
    public event Action<string, Vector2I, Vector2I, bool>? EditorDetailInventoryMoveRequested;
    public event Action<string, Vector2I, string, Vector2I, bool>? EditorDetailInventoryTransferRequested;
    public event Action<string>? EditorDetailRecipeSelected;
    public event Action<string>? EditorDetailActionRequested;
    public event Action? EditorDetailClosed;
    public event Action<string, Vector2I, Vector2I, bool>? WorldDetailInventoryMoveRequested;
    public event Action<string, Vector2I, string, Vector2I, bool>? WorldDetailInventoryTransferRequested;
    public event Action<string>? WorldDetailRecipeSelected;
    public event Action<string>? WorldDetailActionRequested;
    public event Action? WorldDetailClosed;
    public event Action? BlueprintCaptureFullRequested;
    public event Action<string>? BlueprintRuntimeSaveRequested;
    public event Action<string>? BlueprintSourceSaveRequested;
    public event Action<string>? BlueprintSelected;
    public event Action? BlueprintApplyRequested;
    public event Action? BlueprintConfirmRequested;
    public event Action<string>? BlueprintDeleteRequested;
    public event Action? BlueprintCancelRequested;
    public event Action? WorldMapSaveRequested;
    public event Action? InteriorMapSaveRequested;
    public event Action? WorldMapSourceSaveRequested;
    public event Action? InteriorMapSourceSaveRequested;
    public event Action<string>? RuntimeSaveRequested;
    public event Action<string>? RuntimeLoadRequested;
    public event Action? RuntimeSaveLibraryRefreshRequested;
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

        _overwriteSaveDialog = new ConfirmationDialog();
        _overwriteSaveDialog.Title = "确认覆盖保存";
        _overwriteSaveDialog.DialogText = "确认要覆盖现有存档吗？";
        _overwriteSaveDialog.Exclusive = true;
        _overwriteSaveDialog.GetOkButton().Text = "覆盖保存";
        _overwriteSaveDialog.Confirmed += () =>
        {
            var action = _pendingOverwriteSaveAction;
            _pendingOverwriteSaveAction = null;
            action?.Invoke();
        };
        _overwriteSaveDialog.Canceled += () => _pendingOverwriteSaveAction = null;
        AddChild(_overwriteSaveDialog);

        SetPersistenceStatus(FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: true));
        UpdateLayout();
        GetViewport().SizeChanged += UpdateLayout;
    }

    public override void _Process(double delta)
    {
        var target = _editorOpen ? 1.0f : 0.0f;
        _editorProgress = Mathf.MoveToward(_editorProgress, target, (float)delta * 4.5f);
        var overviewTarget = _overviewCollapsed ? 1.0f : 0.0f;
        _overviewCollapseProgress = Mathf.MoveToward(_overviewCollapseProgress, overviewTarget, (float)delta * 5.0f);
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

    public void ToggleOverviewCollapsed()
    {
        SetOverviewCollapsed(!_overviewCollapsed);
    }

    public void SetOverviewCollapsed(bool collapsed)
    {
        _overviewCollapsed = collapsed;
        RefreshOverviewCollapseButton();
    }

    public void SetEditorOpen(bool isOpen)
    {
        _editorOpen = isOpen;
        RefreshEditModeButton();
    }

    public bool IsPointerOverEditor(Vector2 mousePosition)
        => IsPointerOverEditorViewport(mousePosition) || IsPointerOverEditorOperationPanel(mousePosition);

    public bool IsPointerOverEditorViewport(Vector2 mousePosition)
        => _editorViewportRect is not null && _editorProgress > 0.01f && _editorViewportRect.GetGlobalRect().HasPoint(mousePosition);

    public bool IsPointerOverEditorOperationPanel(Vector2 mousePosition)
        => _editorPanel is not null && _editorProgress > 0.01f && _editorPanel.GetGlobalRect().HasPoint(mousePosition);

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

    public void SetPaneFocus(bool editorOpen, bool editorViewportFocused, bool editorOperationFocused)
    {
        _editorOpen = editorOpen;
        _editorViewportFocused = editorViewportFocused;
        _editorOperationFocused = editorOperationFocused;
        RefreshFocusVisuals();
    }

    public void SetControlMode(MobileFactoryControlMode controlMode, MobileFactoryLifecycleState lifecycleState, FacingDirection transitFacing, FacingDirection deployFacing, bool editSessionOpen)
    {
        if (_modeLabel is not null)
        {
            var modeText = controlMode switch
            {
                MobileFactoryControlMode.Player => "[PLAYER] 玩家控制",
                MobileFactoryControlMode.FactoryCommand => "[COMMAND] 工厂控制",
                MobileFactoryControlMode.DeployPreview => "[DEPLOY] 部署预览",
                MobileFactoryControlMode.Observer => "[OBSERVE] 观察模式",
                _ => "[COMMAND] 工厂控制"
            };
            var editText = editSessionOpen ? "编辑会话：开启" : "编辑会话：关闭";
            _modeLabel.Text = $"{modeText} | {editText} | 行进朝向：{FactoryDirection.ToLabel(transitFacing)} | 部署朝向：{FactoryDirection.ToLabel(deployFacing)}";
            _modeLabel.Modulate = controlMode switch
            {
                MobileFactoryControlMode.Player => FactoryUiTheme.StatusOk,
                MobileFactoryControlMode.Observer => FactoryUiTheme.Text,
                _ => FactoryUiTheme.StatusWarn
            };
        }

        if (_factoryCommandButton is not null)
        {
            _factoryCommandButton.Text = controlMode == MobileFactoryControlMode.FactoryCommand
                ? "返回玩家 (C)"
                : "工厂控制 (C)";
            _factoryCommandButton.ButtonPressed = controlMode == MobileFactoryControlMode.FactoryCommand;
            _factoryCommandButton.Disabled = lifecycleState == MobileFactoryLifecycleState.Recalling;
        }

        if (_observerButton is not null)
        {
            _observerButton.Text = controlMode == MobileFactoryControlMode.Observer ? "返回玩家 (Tab)" : "观察模式 (Tab)";
            _observerButton.ButtonPressed = controlMode == MobileFactoryControlMode.Observer;
            _observerButton.Disabled = lifecycleState == MobileFactoryLifecycleState.Recalling;
        }

        if (_deployButton is not null)
        {
            _deployButton.Text = controlMode == MobileFactoryControlMode.DeployPreview ? "取消部署 (G)" : "部署预览 (G)";
            _deployButton.ButtonPressed = controlMode == MobileFactoryControlMode.DeployPreview;
            _deployButton.Disabled = lifecycleState != MobileFactoryLifecycleState.InTransit;
        }

        RefreshEditModeButton();
    }

    public void SetState(MobileFactoryLifecycleState state, Vector2I? anchorCell)
    {
        if (_stateLabel is null)
        {
            return;
        }

        _stateLabel.Text = state switch
        {
            MobileFactoryLifecycleState.Deployed when anchorCell is not null => $"[DEPLOYED] 工厂状态：已部署于 ({anchorCell.Value.X}, {anchorCell.Value.Y})",
            MobileFactoryLifecycleState.AutoDeploying => "[AUTO] 工厂状态：自动部署中，正在进场并对齐朝向",
            MobileFactoryLifecycleState.Recalling => "[RECALL] 工厂状态：切回移动态中，部署机构正在收拢",
            _ => "[TRANSIT] 工厂状态：运输中，可自由移动或下达部署命令"
        };
    }

    public void SetHoverAnchor(Vector2I anchorCell, bool hasHover)
    {
        if (_hoverLabel is not null)
        {
            _hoverLabel.Text = hasHover ? $"[ANCHOR] 当前锚点：({anchorCell.X}, {anchorCell.Y})" : "[ANCHOR] 当前锚点：未选择";
        }
    }

    public void SetPreviewStatus(FactoryStatusTone tone, string text)
    {
        if (_previewLabel is not null)
        {
            var prefix = tone switch
            {
                FactoryStatusTone.Positive => "[OK]",
                FactoryStatusTone.Warning => "[WARN]",
                _ => "[BLOCK]"
            };
            _previewLabel.Text = $"{prefix} 世界提示：{text}";
            _previewLabel.Modulate = FactoryUiTheme.GetStatusTone(tone);
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

    public void SetWorldBuildSelection(BuildPrototypeKind? kind, FacingDirection facing, string? details)
    {
        foreach (var pair in _worldBuildButtons)
        {
            pair.Value.ButtonPressed = kind.HasValue && pair.Key == kind.Value;
        }

        if (_worldBuildSelectionLabel is not null)
        {
            _worldBuildSelectionLabel.Text = kind.HasValue
                ? $"[WORLD] 当前世界建造：{FactoryIndustrialStandards.GetSiteAwarePrototypeLabel(kind.Value, FactorySiteKind.World)}"
                : "[WORLD] 当前世界建造：未选择";
            _worldBuildSelectionLabel.TooltipText = details ?? string.Empty;
        }

        if (_worldBuildRotationLabel is not null)
        {
            _worldBuildRotationLabel.Text = kind.HasValue
                ? $"[FACING] 世界朝向：{FactoryDirection.ToLabel(facing)}"
                : "[FACING] 世界朝向：未启用";
        }
    }

    public void SetWorldSelectionTarget(string text)
    {
        if (_worldSelectionTargetLabel is not null)
        {
            _worldSelectionTargetLabel.Text = $"[TARGET] 世界选中：{text}";
        }
    }

    public void SetWorldInspection(string? title, string? body)
    {
        if (_worldInspectionPanel is null || _worldInspectionTitleLabel is null || _worldInspectionBodyLabel is null)
        {
            return;
        }

        var isVisible = !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body);
        _worldInspectionPanel.Visible = isVisible;
        _worldInspectionTitleLabel.Text = title ?? string.Empty;
        _worldInspectionBodyLabel.Text = body ?? string.Empty;
    }

    public void SetEditorSelection(FactoryInteractionMode interactionMode, BuildPrototypeKind? kind, FacingDirection facing)
    {
        _editorInteractionMode = interactionMode;
        if (interactionMode == FactoryInteractionMode.Build && kind.HasValue)
        {
            if (_selectionLabel is not null)
            {
                _selectionLabel.Text = $"[BUILD] 内部模式：建造 | {FactoryIndustrialStandards.GetSiteAwarePrototypeLabel(kind.Value, FactorySiteKind.Interior)} | 朝向 {FactoryDirection.ToLabel(facing)}";
            }
            RefreshPaletteButtons(kind.Value);
        }
        else if (interactionMode == FactoryInteractionMode.Delete)
        {
            if (_selectionLabel is not null)
            {
                _selectionLabel.Text = "[DELETE] 内部模式：删除 | X 切换，右键退出，Shift 可框选删除";
            }
            RefreshPaletteButtons(null);
        }
        else
        {
            if (_selectionLabel is not null)
            {
                _selectionLabel.Text = "[INTERACT] 内部模式：交互 | 点击建筑查看状态";
            }
            RefreshPaletteButtons(null);
        }

        RefreshEditorModeButtons();
    }

    public void SetEditorPreview(bool isValid, string text)
    {
        if (_editorPreviewLabel is not null)
        {
            _editorPreviewLabel.Text = $"{(isValid ? "[OK]" : "[BLOCK]")} 内部预览：{text}";
            _editorPreviewLabel.Modulate = FactoryUiTheme.GetStatusTone(isValid);
        }

        if (_testingPreviewLabel is not null)
        {
            _testingPreviewLabel.Text = $"{(isValid ? "[OK]" : "[BLOCK]")} 验证提示：{text}";
            _testingPreviewLabel.Modulate = FactoryUiTheme.GetStatusTone(isValid);
        }
    }

    public void SetPortStatus(string text)
    {
        if (_portStatusLabel is not null)
        {
            _portStatusLabel.Text = $"[PORT] {text}";
            _portStatusLabel.Modulate = FactoryUiTheme.TextSubtle;
        }

        if (_testingPortStatusLabel is not null)
        {
            _testingPortStatusLabel.Text = $"[PORT] {text}";
            _testingPortStatusLabel.Modulate = FactoryUiTheme.TextSubtle;
        }
    }

    public void SetCombatStats(int activeEnemies, int kills, int structuresLost)
    {
        if (_combatLabel is not null)
        {
            _combatLabel.Text = $"[{(activeEnemies > 0 ? "THREAT" : "CLEAR")}] 世界威胁：敌人 {activeEnemies} | 击杀 {kills} | 损失建筑 {structuresLost}";
            _combatLabel.Modulate = activeEnemies > 0 ? FactoryUiTheme.StatusError : FactoryUiTheme.StatusWarn;
        }

        if (_diagnosticsCombatLabel is not null)
        {
            _diagnosticsCombatLabel.Text = $"[{(activeEnemies > 0 ? "THREAT" : "CLEAR")}] 世界威胁：敌人 {activeEnemies} | 击杀 {kills} | 损失建筑 {structuresLost}";
            _diagnosticsCombatLabel.Modulate = activeEnemies > 0 ? FactoryUiTheme.StatusError : FactoryUiTheme.StatusWarn;
        }
    }

    public void SetEditorSelectionTarget(string text)
    {
        if (_selectionTargetLabel is not null)
        {
            _selectionTargetLabel.Text = $"[TARGET] 内部选中：{text}";
        }

        if (_testingSelectionTargetLabel is not null)
        {
            _testingSelectionTargetLabel.Text = $"[TARGET] 验证目标：{text}";
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

    public void SetEditorFocusHint(bool overEditorViewport, bool overEditorOperationPanel)
    {
        var focusText = overEditorViewport
            ? "[FOCUS] 鼠标焦点：内部编辑视口"
            : overEditorOperationPanel
                ? "[FOCUS] 鼠标焦点：编辑操作面板"
                : "[FOCUS] 鼠标焦点：大世界";
        if (_focusLabel is not null)
        {
            _focusLabel.Text = focusText;
        }

        if (_diagnosticsFocusLabel is not null)
        {
            _diagnosticsFocusLabel.Text = focusText;
        }
    }

    public void SetEditorState(bool isOpen, MobileFactoryLifecycleState lifecycleState, int structureCount, FactoryInteractionMode interactionMode)
    {
        _editorInteractionMode = interactionMode;
        var stateText = lifecycleState switch
        {
            MobileFactoryLifecycleState.Deployed => "已部署",
            MobileFactoryLifecycleState.AutoDeploying => "自动部署中",
            MobileFactoryLifecycleState.Recalling => "回收中",
            _ => "运输中"
        };
        var paneText = isOpen ? "编辑会话已开启，独立操作面板可用" : "按 F 或主面板按钮进入编辑模式";
        var interactionText = interactionMode switch
        {
            FactoryInteractionMode.Build => "建造模式",
            FactoryInteractionMode.Delete => "删除模式",
            _ => "交互模式"
        };
        var maintenanceNote = FactoryIndustrialStandards.GetBuildCatalog(FactorySiteKind.Interior).MaintenanceNote;
        if (_editorModeLabel is not null)
        {
            _editorModeLabel.Text = $"[EDITOR] {paneText} | 生命周期：{stateText} | {interactionText} | 当前内部件数：{structureCount}\n{maintenanceNote}";
        }

        if (_testingEditorStateLabel is not null)
        {
            _testingEditorStateLabel.Text = $"[CHECK] {paneText} | 生命周期：{stateText} | {interactionText} | 当前内部件数：{structureCount}\n{maintenanceNote}";
        }

        RefreshEditorModeButtons();
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

        if (_testingHintLabel is not null)
        {
            _testingHintLabel.Text = text;
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
        if (_detailWindow is null || (_editorViewportPanel is null && _editorPanel is null))
        {
            return;
        }

        if (model is null)
        {
            if (_detailSurfaceContext == DetailSurfaceContext.Editor)
            {
                _detailWindow.HideWindow();
                _detailSurfaceContext = DetailSurfaceContext.None;
            }
        }
        else
        {
            var anchorPanel = _editorViewportPanel ?? _editorPanel!;
            _detailSurfaceContext = DetailSurfaceContext.Editor;
            _detailWindow.ShowDetails(model, anchorPanel.Position + new Vector2(24.0f, 24.0f));
        }
    }

    public void SetWorldStructureDetails(FactoryStructureDetailModel? model)
    {
        if (_detailWindow is null || _infoPanel is null)
        {
            return;
        }

        if (model is null)
        {
            if (_detailSurfaceContext == DetailSurfaceContext.World)
            {
                _detailWindow.HideWindow();
                _detailSurfaceContext = DetailSurfaceContext.None;
            }

            return;
        }

        _detailSurfaceContext = DetailSurfaceContext.World;
        _detailWindow.ShowDetails(model, _infoPanel.Position + new Vector2(_infoPanel.Size.X + 18.0f, 18.0f));
    }

    public void SetBlueprintState(FactoryBlueprintPanelState state)
    {
        _blueprintPanel?.SetState(state);
        if (state.PendingCaptureId is not null || state.CanConfirmApply || state.ModeText.Contains("框选", StringComparison.Ordinal) || state.ModeText.Contains("应用预览", StringComparison.Ordinal))
        {
            SetActiveWorkspace(BlueprintWorkspaceId, emitSignal: false);
        }
    }

    public void SetPersistenceStatus(string text)
    {
        if (_saveStatusLabel is not null)
        {
            _saveStatusLabel.Text = text;
        }

        if (_saveLibraryStatusLabel is not null)
        {
            _saveLibraryStatusLabel.Text = text;
        }

        if (_testingPersistenceLabel is not null)
        {
            _testingPersistenceLabel.Text = text;
        }
    }

    private void RefreshOverviewCollapseButton()
    {
        if (_overviewCollapseButton is null)
        {
            return;
        }

        _overviewCollapseButton.Text = _overviewCollapsed ? "> 展开" : "收起 <";
        _overviewCollapseButton.TooltipText = _overviewCollapsed
            ? "将移动工厂总览向右侧滑出并重新显示。"
            : "将移动工厂总览向左侧滑隐藏，腾出更多世界与编辑空间。";
    }

    public void SetRuntimeSaveLibrary(IReadOnlyList<FactoryRuntimeSaveSlotMetadata> slots)
    {
        if (_saveLibraryList is null)
        {
            return;
        }

        foreach (var child in _saveLibraryList.GetChildren())
        {
            child.QueueFree();
        }

        if (slots.Count == 0)
        {
            _saveLibraryList.AddChild(CreateEditorLabel("当前还没有进度存档。", 11, FactoryUiTheme.TextMuted));
            return;
        }

        for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            _saveLibraryList.AddChild(CreateSaveLibraryCard(slots[slotIndex]));
        }
    }

    public bool BlocksInput(Control? control)
    {
        return BlocksInput(control, GetViewport().GetMousePosition());
    }

    public bool BlocksInput(Control? control, Vector2 screenPoint)
    {
        if ((_detailWindow?.BlocksInput(control) ?? false)
            || (_detailWindow?.ContainsScreenPoint(screenPoint) ?? false))
        {
            return true;
        }

        if (_blueprintPanel?.BlocksInput(control) ?? false)
        {
            return true;
        }

        return BlocksInteractiveInput(control, _topChromePanel)
            || BlocksInteractiveInput(control, _infoPanel)
            || BlocksInteractiveInput(control, _editorViewportPanel)
            || BlocksInteractiveInput(control, _editorPanel)
            || ContainsScreenPoint(_topChromePanel, screenPoint)
            || ContainsScreenPoint(_infoPanel, screenPoint)
            || ContainsScreenPoint(_editorViewportPanel, screenPoint)
            || ContainsScreenPoint(_editorPanel, screenPoint);
    }

    private void RefreshEditModeButton()
    {
        if (_editModeButton is null)
        {
            return;
        }

        _editModeButton.Text = _editorOpen ? "退出编辑模式 (F)" : "进入编辑模式 (F)";
    }

    private void RefreshEditorModeButtons()
    {
        if (_editorBuildModeButton is not null)
        {
            _editorBuildModeButton.ButtonPressed = _editorInteractionMode == FactoryInteractionMode.Build;
        }

        if (_editorInteractionModeButton is not null)
        {
            _editorInteractionModeButton.ButtonPressed = _editorInteractionMode == FactoryInteractionMode.Interact;
        }

        if (_editorDeleteModeButton is not null)
        {
            _editorDeleteModeButton.ButtonPressed = _editorInteractionMode == FactoryInteractionMode.Delete;
        }
    }

    private void RequestOverwriteSave(FactoryRuntimeSaveSlotMetadata slot)
    {
        if (_overwriteSaveDialog is null || UseLargeScenarioWorkspaces)
        {
            return;
        }

        _pendingOverwriteSaveAction = () =>
        {
            if (_saveWorkspaceSlotEdit is not null)
            {
                _saveWorkspaceSlotEdit.Text = slot.SlotId;
            }

            RuntimeSaveRequested?.Invoke(slot.SlotId);
        };
        _overwriteSaveDialog.DialogText = $"确认要覆盖保存到 {slot.DisplayName} ({slot.SlotId}) 吗？";
        _overwriteSaveDialog.PopupCentered(new Vector2I(420, 0));
    }

    private Control CreateSaveLibraryCard(FactoryRuntimeSaveSlotMetadata slot)
    {
        var card = new PanelContainer();
        card.MouseFilter = Control.MouseFilterEnum.Ignore;
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        card.AddThemeStyleboxOverride("panel", FactoryUiTheme.CreatePanelStyle(FactoryUiTheme.SurfaceOverlay, FactoryUiTheme.BorderSoft, 1, FactoryUiTheme.RadiusNone, 8));
        card.TooltipText = FactoryRuntimeSavePersistence.BuildSlotTooltip(slot);

        var margin = new MarginContainer();
        margin.MouseFilter = Control.MouseFilterEnum.Ignore;
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 7);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 7);
        card.AddChild(margin);

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 3);
        margin.AddChild(body);

        var header = new HBoxContainer();
        header.MouseFilter = Control.MouseFilterEnum.Ignore;
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddThemeConstantOverride("separation", 8);
        body.AddChild(header);

        var title = CreateEditorLabel(slot.DisplayName, 12, FactoryUiTheme.Text);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(title);

        var overwriteButton = new Button
        {
            Text = "覆盖保存",
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(84.0f, 24.0f),
            Disabled = UseLargeScenarioWorkspaces
        };
        overwriteButton.AddThemeFontSizeOverride("font_size", 10);
        FactoryUiTheme.ApplyButtonTheme(overwriteButton, compact: true);
        overwriteButton.TooltipText = UseLargeScenarioWorkspaces
            ? "large scenario 当前不支持运行时进度覆盖保存"
            : $"覆盖保存到存档 {slot.SlotId}";
        overwriteButton.Pressed += () => RequestOverwriteSave(slot);
        header.AddChild(overwriteButton);

        var loadButton = new Button
        {
            Text = "读取",
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(62.0f, 24.0f),
            Disabled = UseLargeScenarioWorkspaces
        };
        loadButton.AddThemeFontSizeOverride("font_size", 10);
        FactoryUiTheme.ApplyButtonTheme(loadButton, compact: true);
        loadButton.TooltipText = UseLargeScenarioWorkspaces
            ? "large scenario 当前不支持运行时进度读档"
            : $"直接读取存档 {slot.SlotId}";
        loadButton.Pressed += () =>
        {
            if (_saveWorkspaceSlotEdit is not null)
            {
                _saveWorkspaceSlotEdit.Text = slot.SlotId;
            }

            RuntimeLoadRequested?.Invoke(slot.SlotId);
        };
        header.AddChild(loadButton);

        var meta = CreateEditorLabel(
            $"{FactoryRuntimeSavePersistence.FormatSavedAtDisplay(slot.SavedAtUtc)}  |  {slot.SiteCount} 站点",
            10,
            FactoryUiTheme.TextSubtle);
        body.AddChild(meta);

        var maps = CreateEditorLabel(FactoryRuntimeSavePersistence.BuildSlotCompactSummary(slot), 10, FactoryUiTheme.TextMuted);
        body.AddChild(maps);

        return card;
    }
}
