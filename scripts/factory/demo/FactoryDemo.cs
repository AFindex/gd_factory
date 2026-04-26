using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class FactoryDemo : Node3D
{
    private const string BuildWorkspaceId = "build";
    private const string BlueprintWorkspaceId = "blueprints";
    private const string TelemetryWorkspaceId = "telemetry";
    private const string CombatWorkspaceId = "combat";
    private const string TestingWorkspaceId = "testing";
    private const string SavesWorkspaceId = "saves";
    private const float PreviewPowerPoleWireHeight = FactoryPreviewOverlaySupport.PreviewPowerPoleWireHeight;
    private const int PreviewPowerPoleConnectionRangeCells = FactoryPreviewOverlaySupport.PreviewPowerPoleConnectionRangeCells;

    private readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> _definitions = FactoryPrototypeCatalog.BuildForWorld();

    private GridManager? _grid;
    private SimulationController? _simulation;
    private FactoryCameraRig? _cameraRig;
    private FactoryHud? _hud;
    private FactoryTransportRenderManager? _transportRenderManager;
    private FactoryPlayerController? _playerController;
    private FactoryPlayerHud? _playerHud;
    private Node3D? _structureRoot;
    private Node3D? _enemyRoot;
    private Node3D? _previewRoot;
    private Node3D? _previewPortHintRoot;
    private Node3D? _resourceOverlayRoot;
    private Node3D? _powerLinkOverlayRoot;
    private MeshInstance3D? _previewCell;
    private Node3D? _previewArrow;
    private MeshInstance3D? _previewPowerRange;
    private readonly List<Node3D> _previewPortHintMeshes = new();
    private readonly List<FactoryPortPreviewMarker> _cachedPreviewPortMarkers = new();
    private PowerLinkPreviewContext _powerLinkContext = new();
    private FactoryCombatDirector? _combatDirector;
    private BlueprintWorkflowState _blueprintWorkflow = new();
    private double _averageFrameMilliseconds;
    private double _averageVisualSyncMilliseconds;
    private BuildPrototypeKind? _selectedBuildKind;
    private FacingDirection _selectedFacing = FacingDirection.East;
    private FactoryInteractionMode _interactionMode = FactoryInteractionMode.Interact;
    private FactoryStructure? _selectedStructure;
    private FactoryStructure? _hoveredStructure;
    private FactoryResourceDepositDefinition? _selectedResourceDeposit;
    private FactoryResourceDepositDefinition? _hoveredResourceDeposit;
    private Vector2I _hoveredCell;
    private Vector2I _cachedPreviewPortCell;
    private Rect2I _cachedPreviewPortVisibleRect;
    private bool _hasHoveredCell;
    private bool _hasCachedPreviewPortMarkers;
    private bool _canPlaceCurrentCell;
    private bool _canDeleteCurrentCell;
    private DeleteDragState _deleteDrag = new();
    private BuildDragState _buildDrag = BuildDragState.Create();
    private FactoryBlueprintWorkflowMode _blueprintMode;
    private FactoryBlueprintRecord? _pendingBlueprintCapture;
    private string _previewMessage = "交互模式：点击建筑查看；按数字键选择建筑后进入建造，或按住 Shift 左键框选蓝图。";
    private readonly FactoryBaselinePlayerPlacementState _playerPlacementState = new();
    private BuildPrototypeKind? _cachedPreviewPortKind;
    private FacingDirection _cachedPreviewPortFacing = FacingDirection.East;
    private int _cachedPreviewPortRevision = -1;
    private static void TraceLog(string message) => GD.Print($"[FactoryDemo] {message}");

    public override void _Ready()
    {
        EnsureInputActions();
        BuildSceneGraph();
        ConfigureGameplay();
        RefreshAllTopology();
        if (!HasSmokeTestFlag())
        {
            ConfigureCombatScenarios();
        }
        SpawnPlayerController();
        UpdateHud();

        if (HasSmokeTestFlag())
        {
            CallDeferred(nameof(RunSmokeChecks));
        }
        else if (HasRuntimeSaveSmokeTestFlag())
        {
            CallDeferred(nameof(RunRuntimeSaveSmokeChecks));
        }
    }

    public override void _Process(double delta)
    {
        _averageFrameMilliseconds = FactoryMetrics.SmoothMetric(_averageFrameMilliseconds, delta * 1000.0, 0.1);
        _playerController?.ApplyMovement(GetPlayerMovementBounds(), delta, allowInput: true);
        if (_cameraRig is not null)
        {
            _cameraRig.AllowPanInput = false;
            _cameraRig.AllowZoomInput = !IsWorldPointerInputBlocked();
        }

        UpdateHoveredCell();
        var placedDuringDrag = HandleBuildDragPlacement();
        if (placedDuringDrag)
        {
            UpdateHoveredCell();
        }

        UpdatePreview();
        UpdateStructureVisuals();
        UpdatePowerLinkVisuals();
        UpdateHud();
        HandleHotkeys();
        UpdateCursorShape();

        if (_interactionMode == FactoryInteractionMode.Delete && _deleteDrag.Active && _hasHoveredCell)
        {
            _deleteDrag.CurrentCell = _hoveredCell;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey hotbarKeyEvent
            && hotbarKeyEvent.Pressed
            && !hotbarKeyEvent.Echo
            && FactoryInputUtility.TryMapHotbarKey(hotbarKeyEvent.Keycode, out var hotbarIndex))
        {
            HandlePlayerHotbarPressed(hotbarIndex);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouse && IsWorldPointerInputBlocked())
        {
            if (IsInventoryUiInteractionActive() && @event is InputEventMouseButton blockedMouseButton)
            {
                TraceLog($"mouse input blocked by active inventory interaction button={blockedMouseButton.ButtonIndex} pressed={blockedMouseButton.Pressed}");
            }
            return;
        }

        if (IsWorldPointerInputBlocked())
        {
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (_blueprintMode != FactoryBlueprintWorkflowMode.None)
            {
                if (keyEvent.Keycode == Key.Escape)
                {
                    CancelBlueprintWorkflow();
                    GetViewport().SetInputAsHandled();
                }

                return;
            }

            if (keyEvent.Keycode == Key.X)
            {
                if (_interactionMode == FactoryInteractionMode.Delete)
                {
                    EnterInteractionMode();
                }
                else
                {
                    EnterDeleteMode();
                }

                GetViewport().SetInputAsHandled();
                return;
            }

            if (keyEvent.Keycode == Key.Delete && _interactionMode == FactoryInteractionMode.Build && _hoveredStructure is not null)
            {
                RemoveStructure(_hoveredStructure.Cell);
                GetViewport().SetInputAsHandled();
                return;
            }

            if (keyEvent.Keycode == Key.Delete && _interactionMode == FactoryInteractionMode.Delete)
            {
                DeleteHoveredStructure();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (keyEvent.Keycode == Key.Escape)
            {
                EnterInteractionMode();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is not InputEventMouseButton mouseButton)
        {
            return;
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                CancelBlueprintWorkflow();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    BeginBlueprintSelection();
                }
                else
                {
                    CompleteBlueprintSelection();
                }

                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            if (!mouseButton.Pressed)
            {
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                ConfirmBlueprintApply();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                CancelBlueprintWorkflow(clearActiveBlueprint: false);
                EnterInteractionMode();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (_interactionMode == FactoryInteractionMode.Interact
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed
            && mouseButton.ShiftPressed)
        {
            StartBlueprintCapture();
            BeginBlueprintSelection();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Delete)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                EnterInteractionMode();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    HandleDeletePrimaryPress(mouseButton.ShiftPressed);
                }
                else
                {
                    HandleDeletePrimaryRelease();
                }

                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (_interactionMode == FactoryInteractionMode.Build && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                HandleBuildPrimaryPress();
            }
            else
            {
                HandleBuildPrimaryRelease();
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (!mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            HandlePrimaryClick();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            HandleSecondaryClick();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildSceneGraph()
    {
        var scaffold = FactoryDemoSceneScaffold.Build(
            this,
            FactoryConstants.GridMin,
            FactoryConstants.GridMax,
            new[]
            {
                new FactoryDemoRootSpec("resource", "ResourceOverlayRoot"),
                new FactoryDemoRootSpec("structure", "StructureRoot"),
                new FactoryDemoRootSpec("enemy", "EnemyRoot"),
                new FactoryDemoRootSpec("preview", "PreviewRoot"),
                new FactoryDemoRootSpec("preview-port-hints", "PreviewPortHintRoot", false),
                new FactoryDemoRootSpec("power-links", "PowerLinkOverlayRoot", false),
                new FactoryDemoRootSpec("blueprint-preview", "BlueprintPreviewRoot", false),
                new FactoryDemoRootSpec("blueprint-ghost-preview", "BlueprintGhostPreviewRoot", false)
            },
            combatDirectorName: "CombatDirector");

        _resourceOverlayRoot = scaffold.GetRoot("resource");
        _structureRoot = scaffold.GetRoot("structure");
        _enemyRoot = scaffold.GetRoot("enemy");
        _previewRoot = scaffold.GetRoot("preview");
        _previewPortHintRoot = scaffold.GetRoot("preview-port-hints");
        _powerLinkOverlayRoot = scaffold.GetRoot("power-links");
        _powerLinkContext.OverlayRoot = _powerLinkOverlayRoot;
        _blueprintWorkflow.PreviewRoot = scaffold.GetRoot("blueprint-preview");
        _blueprintWorkflow.GhostPreviewRoot = scaffold.GetRoot("blueprint-ghost-preview");
        _simulation = scaffold.Simulation;
        _combatDirector = scaffold.CombatDirector;
        _cameraRig = scaffold.CameraRig;
        _playerHud = scaffold.PlayerHud;

        _transportRenderManager = new FactoryTransportRenderManager();
        AddChild(_transportRenderManager);

        CreatePreviewVisuals();

        _hud = new FactoryHud();
        _hud.SelectionChanged += SelectBuildKind;
        _hud.DetailInventoryMoveRequested += HandleDetailInventoryMoveRequested;
        _hud.DetailInventoryTransferRequested += HandlePlayerInventoryTransferRequested;
        _hud.DetailRecipeSelected += HandleDetailRecipeSelected;
        _hud.DetailClosed += HandleDetailWindowClosed;
        _hud.WorkspaceSelected += HandleHudWorkspaceSelected;
        _hud.BlueprintRuntimeSaveRequested += name => HandleBlueprintSaveRequested(name, FactoryBlueprintPersistenceTarget.Runtime);
        _hud.BlueprintSourceSaveRequested += name => HandleBlueprintSaveRequested(name, FactoryBlueprintPersistenceTarget.Source);
        _hud.MapSaveRequested += HandleMapSaveRequested;
        _hud.MapSourceSaveRequested += HandleMapSourceSaveRequested;
        _hud.RuntimeSaveRequested += HandleRuntimeSaveRequested;
        _hud.RuntimeLoadRequested += HandleRuntimeLoadRequested;
        _hud.RuntimeSaveLibraryRefreshRequested += RefreshRuntimeSaveLibrary;
        _hud.BlueprintSelected += HandleBlueprintSelected;
        _hud.BlueprintApplyRequested += EnterBlueprintApplyMode;
        _hud.BlueprintConfirmRequested += ConfirmBlueprintApply;
        _hud.BlueprintDeleteRequested += HandleBlueprintDeleteRequested;
        _hud.BlueprintCancelRequested += CancelBlueprintWorkflow;
        AddChild(_hud);
        InitializePersistenceHud();

        _playerHud.HotbarSlotPressed += HandlePlayerHotbarPressed;
        _playerHud.BackpackInventoryMoveRequested += HandlePlayerInventoryMoveRequested;
        _playerHud.BackpackInventoryTransferRequested += HandlePlayerInventoryTransferRequested;
        _playerHud.BackpackSlotActivated += HandlePlayerInventorySlotActivated;
    }

    private void ConfigureGameplay()
    {
        _grid = new GridManager(
            new Vector2I(FactoryConstants.GridMin, FactoryConstants.GridMin),
            new Vector2I(FactoryConstants.GridMax, FactoryConstants.GridMax),
            FactoryConstants.CellSize);

        _simulation!.Configure(_grid);
        _combatDirector?.Configure(_simulation, _enemyRoot!);
        _cameraRig!.ConfigureBounds(_grid.GetWorldMin() + Vector2.One * 4.0f, _grid.GetWorldMax() - Vector2.One * 4.0f);
        LoadStarterWorldMap();
        RebuildResourceOverlayVisuals();
        _blueprintWorkflow.Site = CreateBlueprintSiteAdapter();
        _blueprintWorkflow.FactorySite = _grid;
        EnterInteractionMode();
    }

    private void HandleHotkeys()
    {
        if (_blueprintMode != FactoryBlueprintWorkflowMode.None)
        {
            if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
            {
                if (Input.IsActionJustPressed("camera_rotate_left"))
                {
                    RotateBlueprintApplyPreview(-1);
                }

                if (Input.IsActionJustPressed("camera_rotate_right"))
                {
                    RotateBlueprintApplyPreview(1);
                }
            }

            if (Input.IsActionJustPressed("build_cancel"))
            {
                CancelBlueprintWorkflow();
            }
            return;
        }

        HandleBuildShortcut("select_inserter", BuildPrototypeKind.Inserter);
        HandleBuildShortcut("select_wall", BuildPrototypeKind.Wall);
        HandleBuildShortcut("select_ammo_assembler", BuildPrototypeKind.AmmoAssembler);
        HandleBuildShortcut("select_gun_turret", BuildPrototypeKind.GunTurret);

        if (Input.IsActionJustPressed("build_cancel"))
        {
            EnterInteractionMode();
        }

        if (Input.IsActionJustPressed("camera_rotate_left"))
        {
            _selectedFacing = FactoryDirection.RotateCounterClockwise(_selectedFacing);
        }

        if (Input.IsActionJustPressed("camera_rotate_right"))
        {
            _selectedFacing = FactoryDirection.RotateClockwise(_selectedFacing);
        }
    }

    private void HandleBuildShortcut(string actionName, BuildPrototypeKind kind)
    {
        if (!Input.IsActionJustPressed(actionName))
        {
            return;
        }

        SelectBuildKind(_selectedBuildKind == kind ? null : kind);
    }

    private void SelectBuildKind(BuildPrototypeKind? kind)
    {
        CancelBlueprintWorkflow(clearActiveBlueprint: false);
        _selectedBuildKind = kind;
        if (kind.HasValue)
        {
            _playerPlacementState.DisarmPlacement(_playerController);
            _selectedResourceDeposit = null;
        }

        RefreshInteractionModeFromBuildSource();
        _deleteDrag.Active = false;
        ResetBuildPlacementStroke();

        if (_interactionMode == FactoryInteractionMode.Build)
        {
            _selectedStructure = null;
            _selectedResourceDeposit = null;
        }
    }

    private void EnterInteractionMode()
    {
        _selectedBuildKind = null;
        _playerPlacementState.DisarmPlacement(_playerController);
        _interactionMode = FactoryInteractionMode.Interact;
        _deleteDrag.Active = false;
        ResetBuildPlacementStroke();
    }

    private void EnterDeleteMode()
    {
        CancelBlueprintWorkflow(clearActiveBlueprint: false);
        _selectedBuildKind = null;
        _playerPlacementState.DisarmPlacement(_playerController);
        _selectedStructure = null;
        _selectedResourceDeposit = null;
        _interactionMode = FactoryInteractionMode.Delete;
        _deleteDrag.Active = false;
        ResetBuildPlacementStroke();
    }

    private void SpawnPlayerController()
    {
        if (_playerController is not null)
        {
            return;
        }

        _playerController = new FactoryPlayerController();
        AddChild(_playerController);
        _playerController.GlobalPosition = FindPlayerSpawnPosition();
        _playerController.EnsureStarterLoadout(_simulation);
        _playerController.SelectHotbarIndex(0);
        _playerController.DisarmHotbarPlacement();
        _cameraRig?.SetFollowTarget(_playerController, snapImmediately: true);
        _cameraRig!.FollowTargetEnabled = true;
        _playerPlacementState.SetSelectedSlot(
            FactoryPlayerController.BackpackInventoryId,
            new Vector2I(0, 0),
            _playerController.IsHotbarPlacementArmed);
        RefreshInteractionModeFromBuildSource();
    }

    private Vector3 FindPlayerSpawnPosition()
    {
        if (_grid is null)
        {
            return new Vector3(0.0f, 0.0f, 0.0f);
        }

        var preferred = new Vector2I(-14, -12);
        for (var radius = 0; radius <= 24; radius++)
        {
            for (var y = preferred.Y - radius; y <= preferred.Y + radius; y++)
            {
                for (var x = preferred.X - radius; x <= preferred.X + radius; x++)
                {
                    var candidate = new Vector2I(x, y);
                    if (!_grid.IsInBounds(candidate) || _grid.TryGetStructure(candidate, out _))
                    {
                        continue;
                    }

                    var world = _grid.CellToWorld(candidate);
                    return new Vector3(world.X, 0.0f, world.Z);
                }
            }
        }

        return Vector3.Zero;
    }

    private Rect2 GetPlayerMovementBounds()
    {
        if (_grid is null)
        {
            return new Rect2(-4.0f, -4.0f, 8.0f, 8.0f);
        }

        var min = _grid.GetWorldMin() + Vector2.One * 1.0f;
        var max = _grid.GetWorldMax() - Vector2.One * 1.0f;
        return new Rect2(min, max - min);
    }

    private bool TryGetActivePlacementKind(out BuildPrototypeKind kind, out bool usesPlayerInventory)
    {
        if (_selectedBuildKind.HasValue)
        {
            kind = _selectedBuildKind.Value;
            usesPlayerInventory = false;
            return true;
        }

        if (_playerPlacementState.PlacementArmed && TryResolveSelectedPlayerPlaceable(out var selectedPlayerKind))
        {
            kind = selectedPlayerKind;
            usesPlayerInventory = true;
            return true;
        }

        if (_playerController?.GetArmedPlaceablePrototype() is BuildPrototypeKind playerKind)
        {
            kind = playerKind;
            usesPlayerInventory = true;
            return true;
        }

        kind = default;
        usesPlayerInventory = false;
        return false;
    }

    private void RefreshInteractionModeFromBuildSource()
    {
        _interactionMode = FactoryBaselineInteractionRules.ResolvePlacementInteractionMode(
            _interactionMode,
            TryGetActivePlacementKind(out _, out _));
    }

    private void HandlePlayerHotbarPressed(int index)
    {
        if (_playerController is null)
        {
            return;
        }

        CancelBlueprintWorkflow(clearActiveBlueprint: false);
        _selectedBuildKind = null;
        _playerPlacementState.HandleHotbarPressed(_playerController, index);
        RefreshInteractionModeFromBuildSource();
    }

    private void RebuildResourceOverlayVisuals()
    {
        FactoryMapVisualSupport.RebuildResourceOverlay(
            _resourceOverlayRoot,
            _grid,
            "Resource_",
            0.88f,
            0.06f,
            0.03f,
            1.0f,
            "ResourceChip_",
            0.20f,
            0.14f,
            0.10f,
            0.18f,
            0.85f);
    }

    private void ConfigureCombatScenarios()
    {
        if (_grid is null || _combatDirector is null)
        {
            return;
        }

        _combatDirector.ClearLanes();
        _combatDirector.AddLane(new FactoryEnemyLaneDefinition(
            "success_lane",
            BuildLanePath(new Vector2I(19, 20), new Vector2I(18, 20), new Vector2I(17, 20), new Vector2I(15, 20)),
            new FactoryEnemySpawnRule[]
            {
                new("melee", 4.2f),
                new("melee", 4.8f),
                new("melee", 4.4f)
            }));
        _combatDirector.AddLane(new FactoryEnemyLaneDefinition(
            "starved_lane",
            BuildLanePath(new Vector2I(19, 14), new Vector2I(18, 14), new Vector2I(17, 14), new Vector2I(15, 14)),
            new FactoryEnemySpawnRule[]
            {
                new("melee", 5.0f),
                new("melee", 5.0f)
            }));
        _combatDirector.AddLane(new FactoryEnemyLaneDefinition(
            "heavy_lane",
            BuildLanePath(new Vector2I(-20, 20), new Vector2I(-19, 20), new Vector2I(-18, 20), new Vector2I(-16, 20)),
            new FactoryEnemySpawnRule[]
            {
                new("melee", 4.8f),
                new("ranged", 6.8f)
            }));
    }

    private Vector3[] BuildLanePath(params Vector2I[] cells)
    {
        if (_grid is null)
        {
            return new Vector3[0];
        }

        var path = new Vector3[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            path[i] = _grid.CellToWorld(cells[i]);
        }

        return path;
    }

    private void UpdateHoveredCell()
    {
        _hasHoveredCell = false;
        _hoveredStructure = null;
        _hoveredResourceDeposit = null;
        _canPlaceCurrentCell = false;
        _canDeleteCurrentCell = false;
        _blueprintWorkflow.ApplyPlan = null;
        _previewMessage = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图框选：拖拽选择一片现有布局，然后在右侧面板保存。",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图预览：移动鼠标选择锚点，按 Q/E 旋转，当前朝向 {FactoryDirection.ToLabel(_blueprintWorkflow.ApplyRotation)}。",
            _ => _interactionMode switch
            {
                FactoryInteractionMode.Build => "把鼠标移到地面网格上选择格子。",
                FactoryInteractionMode.Delete => "删除模式：点击建筑删除，按住 Shift 左键拖拽可框选删除。",
                _ => "交互模式：点击建筑查看；按快捷键或左侧按钮选择建筑后进入建造，或按住 Shift 左键直接框选蓝图。"
            }
        };

        if (_grid is null || _cameraRig is null)
        {
            return;
        }

        if (IsWorldPointerInputBlocked())
        {
            return;
        }

        if (!_cameraRig.TryProjectMouseToPlane(GetViewport().GetMousePosition(), out var worldPosition))
        {
            return;
        }

        var cell = _grid.WorldToCell(worldPosition);
        _hoveredCell = cell;
        _hasHoveredCell = _grid.IsInBounds(cell);
        if (!_hasHoveredCell)
        {
            _previewMessage = "超出可建造范围。";
            return;
        }

        _grid.TryGetStructure(cell, out _hoveredStructure);
        FactoryResourceDetailSupport.TryGetDeposit(_grid, cell, out _hoveredResourceDeposit);
        if (_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection)
        {
            if (_blueprintWorkflow.SelectionDragActive)
            {
                _blueprintWorkflow.SelectionCurrentCell = cell;
                var rect = GetDeleteRect(_blueprintWorkflow.SelectionStartCell, _blueprintWorkflow.SelectionCurrentCell);
                var selectedCount = CountStructuresInBlueprintRect(rect);
                _previewMessage = $"蓝图框选：[{rect.Position.X},{rect.Position.Y}] - [{rect.End.X - 1},{rect.End.Y - 1}]，当前覆盖 {selectedCount} 个建筑。";
                return;
            }

            if (_blueprintWorkflow.HasSelectionRect)
            {
                var selectedCount = CountStructuresInBlueprintRect(_blueprintWorkflow.SelectionRect);
                _previewMessage = $"蓝图框选已完成：覆盖 {selectedCount} 个建筑，填写名称后保存。";
                return;
            }

            _previewMessage = "蓝图框选：左键按下并拖拽选择一片现有布局。";
            return;
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            if (_blueprintWorkflow.Site is null)
            {
                _previewMessage = "蓝图预览不可用：缺少目标站点。";
                return;
            }

            var activeBlueprint = FactoryBlueprintLibrary.GetActive();
            if (activeBlueprint is null)
            {
                _previewMessage = "请先在蓝图库里选择一个蓝图。";
                return;
            }

            _blueprintWorkflow.UpdateApplyPlan(cell);
            _previewMessage = _blueprintWorkflow.ApplyPlan.IsValid
                ? $"蓝图 {activeBlueprint.DisplayName} 可应用到锚点 ({cell.X}, {cell.Y})，旋转 {FactoryDirection.ToLabel(_blueprintWorkflow.ApplyRotation)}。"
                : _blueprintWorkflow.ApplyPlan.GetIssueSummary();
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Build && TryGetActivePlacementKind(out var placementKind, out var usesPlayerInventory))
        {
            var previewFacing = ResolveWorldPlacementFacing(placementKind, cell, _buildDrag.Active);
            _canPlaceCurrentCell = TryValidateWorldPlacement(placementKind, cell, previewFacing, out var placementIssue);
            _previewMessage = _canPlaceCurrentCell
                ? DescribeWorldPlacementPreview(placementKind, cell, previewFacing, usesPlayerInventory)
                : placementIssue;
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Delete)
        {
            if (_deleteDrag.Active)
            {
                _deleteDrag.CurrentCell = cell;
                var deletionCount = CountStructuresInDeleteRect(_deleteDrag.StartCell, _deleteDrag.CurrentCell);
                _canDeleteCurrentCell = deletionCount > 0;
                var rect = GetDeleteRect(_deleteDrag.StartCell, _deleteDrag.CurrentCell);
                _previewMessage = $"删除模式：框选 [{rect.Position.X},{rect.Position.Y}] - [{rect.End.X - 1},{rect.End.Y - 1}]，将删除 {deletionCount} 个建筑。";
                return;
            }

            _canDeleteCurrentCell = _hoveredStructure is not null;
            _previewMessage = _hoveredStructure is null
                ? $"删除模式：格子 ({cell.X}, {cell.Y}) 为空，按 X 可返回普通交互。"
                : $"删除模式：左键删除 {_hoveredStructure.DisplayName}，Shift+左键拖拽可框选删除。";
            return;
        }

        _previewMessage = _hoveredStructure is null
            ? _hoveredResourceDeposit is null
                ? $"交互模式：空地 ({cell.X}, {cell.Y})，点击可清除当前选中，Shift+左键可开始蓝图框选。"
                : $"交互模式：点击查看 {_hoveredResourceDeposit.DisplayName}，Shift+左键可开始蓝图框选。"
            : $"交互模式：点击选中 {_hoveredStructure.DisplayName} ({cell.X}, {cell.Y})，Shift+左键可开始蓝图框选。";
    }

    private void UpdatePreview()
    {
        if (_grid is null || _previewRoot is null || _previewCell is null || _previewArrow is null || _previewPowerRange is null)
        {
            return;
        }

        UpdateBlueprintPreview();
        _previewPowerRange.Visible = false;
        SetPreviewPortHintCount(0);

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            _previewRoot.Visible = false;
            return;
        }

        var showCapturePreview = _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection
            && (_blueprintWorkflow.SelectionDragActive || _blueprintWorkflow.HasSelectionRect);
        var hasPlacementPreview = false;
        var previewKind = default(BuildPrototypeKind);
        var previewFacing = _selectedFacing;
        if (_interactionMode == FactoryInteractionMode.Build && TryGetActivePlacementKind(out var activePreviewKind, out _))
        {
            hasPlacementPreview = true;
            previewKind = activePreviewKind;
            if (_hasHoveredCell)
            {
                previewFacing = ResolveWorldPlacementFacing(previewKind, _hoveredCell, _buildDrag.Active);
            }
        }

        var showPreview = showCapturePreview || (_hasHoveredCell
            && (hasPlacementPreview || _interactionMode == FactoryInteractionMode.Delete));
        _previewRoot.Visible = showPreview;
        if (!showPreview)
        {
            _previewArrow.Visible = false;
            return;
        }

        if (showCapturePreview)
        {
            var rect = _blueprintWorkflow.SelectionDragActive
                ? GetDeleteRect(_blueprintWorkflow.SelectionStartCell, _blueprintWorkflow.SelectionCurrentCell)
                : _blueprintWorkflow.SelectionRect;
            var minCell = rect.Position;
            var maxCell = rect.End - Vector2I.One;
            var minWorld = _grid.CellToWorld(minCell);
            var maxWorld = _grid.CellToWorld(maxCell);
            _previewRoot.Position = (minWorld + maxWorld) * 0.5f;
            _previewRoot.Rotation = Vector3.Zero;
            _previewCell.Mesh = new BoxMesh
            {
                Size = new Vector3(
                    FactoryConstants.CellSize * rect.Size.X - (FactoryConstants.CellSize * 0.08f),
                    0.08f,
                    FactoryConstants.CellSize * rect.Size.Y - (FactoryConstants.CellSize * 0.08f))
            };
            _previewCell.Position = new Vector3(0.0f, 0.05f, 0.0f);
            _previewArrow.Visible = false;
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_previewCell, new Color(0.35f, 0.75f, 1.0f, 0.34f));
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Delete)
        {
            var start = _deleteDrag.Active ? _deleteDrag.StartCell : _hoveredCell;
            var end = _deleteDrag.Active ? _deleteDrag.CurrentCell : _hoveredCell;
            var rect = GetDeleteRect(start, end);
            var minCell = rect.Position;
            var maxCell = rect.End - Vector2I.One;
            var minWorld = _grid.CellToWorld(minCell);
            var maxWorld = _grid.CellToWorld(maxCell);
            _previewRoot.Position = (minWorld + maxWorld) * 0.5f;
            _previewRoot.Rotation = Vector3.Zero;
            _previewCell.Mesh = new BoxMesh
            {
                Size = new Vector3(
                    FactoryConstants.CellSize * rect.Size.X - (FactoryConstants.CellSize * 0.08f),
                    0.08f,
                    FactoryConstants.CellSize * rect.Size.Y - (FactoryConstants.CellSize * 0.08f))
            };
            _previewCell.Position = new Vector3(0.0f, 0.05f, 0.0f);
            _previewArrow.Visible = false;
            var deleteTint = _canDeleteCurrentCell ? new Color(1.0f, 0.35f, 0.35f, 0.42f) : new Color(0.75f, 0.30f, 0.30f, 0.28f);
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_previewCell, deleteTint);
            return;
        }

        _previewRoot.Position = FactoryPlacement.GetPreviewCenter(_grid, previewKind, _hoveredCell, previewFacing);
        _previewRoot.Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(previewFacing), 0.0f);
        var previewSize = FactoryPlacement.GetPreviewBaseSize(_grid, previewKind);
        _previewCell.Mesh = new BoxMesh
        {
            Size = new Vector3(
                previewSize.X - (_grid.CellSize * 0.08f),
                0.08f,
                previewSize.Y - (_grid.CellSize * 0.08f))
        };
        _previewCell.Position = new Vector3(0.0f, 0.05f, 0.0f);
        _previewArrow.Visible = true;

        var tint = _canPlaceCurrentCell ? new Color(0.35f, 0.95f, 0.55f, 0.45f) : new Color(1.0f, 0.35f, 0.35f, 0.45f);
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_previewCell, tint);
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_previewArrow, tint.Lightened(0.1f));
        UpdatePreviewPowerRange(previewKind, _grid, _previewPowerRange, tint);
        UpdatePreviewPortHints(previewKind);
    }

    private void UpdateBlueprintPreview()
    {
        if (_grid is null || _blueprintWorkflow.PreviewRoot is null)
        {
            return;
        }

        _blueprintWorkflow.HideAllPreviews();

        var plan = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview ? _blueprintWorkflow.ApplyPlan : null;
        _blueprintWorkflow.PreviewRoot.Visible = plan is not null && _hasHoveredCell;
        if (_blueprintWorkflow.GhostPreviewRoot is not null)
        {
            _blueprintWorkflow.GhostPreviewRoot.Visible = _blueprintWorkflow.PreviewRoot.Visible;
        }
        if (!_blueprintWorkflow.PreviewRoot.Visible || plan is null)
        {
            if (_blueprintWorkflow.GhostPreviewRoot is not null)
            {
                _blueprintWorkflow.GhostPreviewRoot.Visible = false;
            }
            return;
        }

        EnsureBlueprintPreviewCapacity(plan.Entries.Count);
        var showGhostPreview = SupportsGhostBlueprintPreview();
        if (_blueprintWorkflow.GhostPreviewRoot is not null)
        {
            _blueprintWorkflow.GhostPreviewRoot.Visible = showGhostPreview;
        }
        for (var index = 0; index < plan.Entries.Count; index++)
        {
            var entry = plan.Entries[index];
            var mesh = _blueprintWorkflow.PreviewMeshes[index];
            var footprint = FactoryStructureFactory.GetFootprint(entry.SourceEntry.Kind);
            var previewSize = footprint.GetPreviewSize(_grid.CellSize, FacingDirection.East);
            mesh.Visible = true;
            mesh.Position = FactoryPlacement.GetPreviewCenter(_grid, entry.SourceEntry.Kind, entry.TargetCell, entry.TargetFacing) + new Vector3(0.0f, 0.06f, 0.0f);
            mesh.Rotation = new Vector3(0.0f, _grid.WorldRotationRadians + FactoryDirection.ToYRotationRadians(entry.TargetFacing), 0.0f);
            mesh.Mesh = new BoxMesh
            {
                Size = new Vector3(
                    Mathf.Max(_grid.CellSize * 0.92f, previewSize.X - (_grid.CellSize * 0.08f)),
                    0.10f,
                    Mathf.Max(_grid.CellSize * 0.92f, previewSize.Y - (_grid.CellSize * 0.08f)))
            };
            FactoryPreviewOverlaySupport.ApplyPreviewColor(mesh, entry.IsValid
                ? new Color(0.35f, 0.95f, 0.55f, 0.42f)
                : new Color(1.0f, 0.35f, 0.35f, 0.42f));

            if (showGhostPreview)
            {
                var ghost = EnsureBlueprintGhostPreview(entry, index);
                ghost.Visible = true;
                if (ghost.Site != _grid || ghost.Cell != entry.TargetCell || ghost.Facing != entry.TargetFacing)
                {
                    ghost.Configure(_grid, entry.TargetCell, entry.TargetFacing);
                }

                ghost.GlobalPosition = FactoryPlacement.GetPreviewCenter(_grid, entry.SourceEntry.Kind, entry.TargetCell, entry.TargetFacing);
                ghost.GlobalRotation = new Vector3(
                    0.0f,
                    _grid.WorldRotationRadians + FactoryDirection.ToYRotationRadians(entry.TargetFacing),
                    0.0f);

                ghost.ApplyGhostVisual(entry.IsValid
                    ? new Color(0.54f, 0.84f, 1.0f, 0.58f)
                    : new Color(1.0f, 0.52f, 0.52f, 0.54f));
            }
        }

    }

    private void UpdateStructureVisuals()
    {
        if (_simulation is null || _structureRoot is null || _cameraRig is null)
        {
            return;
        }

        var startTicks = Stopwatch.GetTimestamp();
        var alpha = _simulation.TickAlpha;
        var hasVisibleRect = TryGetVisibleWorldCellRect(out var visibleRect);
        var cameraWorldPosition = _cameraRig.Camera?.GlobalPosition ?? Vector3.Zero;
        _transportRenderManager?.BeginFrame(visibleRect, hasVisibleRect, cameraWorldPosition);
        foreach (var child in _structureRoot.GetChildren())
        {
            if (child is FactoryStructure structure)
            {
                structure.SetCombatFocus(structure == _hoveredStructure, structure == _selectedStructure);
                structure.SetSelectionRangeVisible(PowerLinkPreviewContext.ShouldShowSelectionRange(
                    structure,
                    PowerLinkPreviewContext.IsPowerPreviewActive(_interactionMode, _hasHoveredCell, _selectedBuildKind, _selectedStructure),
                    _interactionMode == FactoryInteractionMode.Interact,
                    structure == _selectedStructure));
                structure.SyncVisualPresentation(alpha);
                structure.UpdateVisuals(alpha);
                structure.SyncCombatVisuals(alpha);
            }
        }
        _transportRenderManager?.EndFrame();

        _averageVisualSyncMilliseconds = FactoryMetrics.SmoothMetric(_averageVisualSyncMilliseconds, Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds, 0.18);
    }

    private void UpdatePowerLinkVisuals()
    {
        if (_grid is null || _structureRoot is null || _powerLinkOverlayRoot is null)
        {
            return;
        }

        if (_blueprintMode != FactoryBlueprintWorkflowMode.None)
        {
            _powerLinkOverlayRoot.Visible = false;
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Build
            && _selectedBuildKind == BuildPrototypeKind.PowerPole
            && _hasHoveredCell)
        {
            var previewColor = _canPlaceCurrentCell
                ? new Color(0.98f, 0.89f, 0.52f, 0.92f)
                : new Color(1.0f, 0.45f, 0.45f, 0.90f);
            _powerLinkContext.RenderPowerLinkSet(
                _structureRoot!,
                PowerLinkPreviewContext.GetPreviewPowerAnchorWorld(_hoveredCell, PreviewPowerPoleWireHeight),
                _hoveredCell,
                PreviewPowerPoleConnectionRangeCells,
                previewColor);
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Interact
            && _selectedStructure is PowerPoleStructure selectedPole
            && GodotObject.IsInstanceValid(selectedPole)
            && selectedPole.IsInsideTree())
        {
            _powerLinkContext.RenderPowerLinkSet(
                _structureRoot!,
                PowerLinkPreviewContext.GetPowerAnchor(selectedPole),
                selectedPole.Cell,
                selectedPole.PowerConnectionRangeCells,
                new Color(0.99f, 0.93f, 0.62f, 0.92f),
                exclude: selectedPole);
            return;
        }

        _powerLinkOverlayRoot.Visible = false;
    }

    private static void UpdatePreviewPowerRange(BuildPrototypeKind? kind, IFactorySite site, MeshInstance3D previewPowerRange, Color tint)
    {
        FactoryPowerPreviewSupport.UpdatePreviewPowerRange(kind, site, previewPowerRange, tint);
    }

    private static bool TryGetPowerPreviewInfo(BuildPrototypeKind? kind, out int rangeCells)
    {
        return FactoryPowerPreviewSupport.TryGetPowerPreviewInfo(kind, out rangeCells);
    }

    private void UpdateHud()
    {
        if (_hud is null)
        {
            return;
        }

        if (_blueprintMode != FactoryBlueprintWorkflowMode.None || _pendingBlueprintCapture is not null)
        {
            _hud.SelectWorkspace(BlueprintWorkspaceId);
        }
        else if (_interactionMode == FactoryInteractionMode.Build || _interactionMode == FactoryInteractionMode.Delete)
        {
            _hud.SelectWorkspace(BuildWorkspaceId);
        }

        var previewPositive = _interactionMode switch
        {
            FactoryInteractionMode.Build => _canPlaceCurrentCell,
            FactoryInteractionMode.Delete => _canDeleteCurrentCell,
            _ => true
        };
        var projection = FactoryBaselineHudProjectionBuilder.Create(
            _interactionMode,
            _selectedBuildKind,
            _selectedBuildKind.HasValue ? _definitions[_selectedBuildKind.Value].Details : null,
            previewPositive,
            _previewMessage,
            _selectedFacing,
            _selectedStructure,
            GetSelectedStructureText());
        if (_selectedStructure is null && _selectedResourceDeposit is not null)
        {
            projection.SelectionTargetText = FactoryResourceDetailSupport.GetSelectionTargetText(_selectedResourceDeposit);
            FactoryResourceDetailSupport.GetInspection(_selectedResourceDeposit, out var depositTitle, out var depositBody);
            projection.InspectionTitle = depositTitle;
            projection.InspectionBody = depositBody;
            projection.StructureDetails = FactoryResourceDetailSupport.BuildDetailModel(_selectedResourceDeposit);
        }
        FactoryBaselineHudApplicator.ApplyToFactoryHud(_hud, projection, _hoveredCell, _hasHoveredCell);

        var sinkStats = CollectSinkStats();
        var transportRenderStats = _transportRenderManager?.GetStats() ?? new FactoryTransportRenderStats();
        _hud.SetSinkStats(sinkStats.deliveredTotal, sinkStats.deliveredRate, sinkStats.sinkCount);
        _hud.SetProfilerStats(
            (int)Engine.GetFramesPerSecond(),
            _averageFrameMilliseconds,
            _simulation?.RegisteredStructureCount ?? 0,
            _simulation?.ActiveTransportItemCount ?? 0,
            transportRenderStats.VisibleItems,
            transportRenderStats.ActiveBuckets,
            transportRenderStats.OptimizedPathActive,
            _simulation?.AverageStepMilliseconds ?? 0.0,
            _averageVisualSyncMilliseconds,
            _simulation?.LastTopologyRebuildMilliseconds ?? 0.0);
        _hud.SetCombatStats(
            _simulation?.ActiveEnemyCount ?? 0,
            _simulation?.DefeatedEnemyCount ?? 0,
            _simulation?.DestroyedStructureCount ?? 0);

        var modeNote = _interactionMode == FactoryInteractionMode.Build
            ? "建造模式：左键放置，右键或 Esc 返回交互，Delete 拆除悬停建筑。"
            : _interactionMode == FactoryInteractionMode.Delete
                ? "删除模式：左键删除悬停建筑，Shift+左键拖拽框选删除，右键或 Esc 返回交互。"
                : "交互模式：左键查看建筑，Shift+左键拖拽可直接框选蓝图，数字键或按钮切换到对应建造工具。";
        _hud.SetNote(modeNote);
        _hud.SetBlueprintState(BuildBlueprintPanelState());

        if (_playerHud is not null)
        {
            _playerHud.SetContext(
                _playerController,
                projection.StructureDetails,
                ResolveSelectedPlayerItem());
        }
    }

    private void HandleHudWorkspaceSelected(string workspaceId)
    {
        if (FactoryBlueprintWorkflowBridge.HandleBlueprintWorkspaceExit(
                workspaceId,
                BlueprintWorkspaceId,
                HasActiveBlueprintWorkspaceState(),
                () => CancelBlueprintWorkflow(clearActiveBlueprint: true),
                out var exitMessage))
        {
            _previewMessage = exitMessage ?? string.Empty;
            return;
        }

        if (workspaceId == BlueprintWorkspaceId
            && !HasActiveBlueprintWorkspaceState())
        {
            _previewMessage = "蓝图工作区已打开：按住 Shift 左键拖拽框选保存，或先在库里准备一个蓝图。";
        }

        if (workspaceId == SavesWorkspaceId)
        {
            RefreshRuntimeSaveLibrary();
        }
    }

    private bool HasActiveBlueprintWorkspaceState()
    {
        return _blueprintMode != FactoryBlueprintWorkflowMode.None
            || _pendingBlueprintCapture is not null
            || _blueprintWorkflow.HasActiveState();
    }

    private FactoryBlueprintPanelState BuildBlueprintPanelState()
    {
        var modeText = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图模式：框选保存",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图模式：应用预览（旋转 {FactoryDirection.ToLabel(_blueprintWorkflow.ApplyRotation)}）",
            _ => "蓝图模式：待命"
        };
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        var activeText = FactoryBlueprintWorkflowBridge.BuildActiveBlueprintText();
        var captureSummary = _pendingBlueprintCapture is null
            ? "未捕获待保存蓝图。点击“框选保存”或在交互模式按住 Shift 左键拖拽选择。"
            : $"待保存：{_pendingBlueprintCapture.DisplayName} | {_pendingBlueprintCapture.GetSummaryText()}";
        var issueText = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _blueprintWorkflow.ApplyPlan is not null
            ? $"当前旋转：{FactoryDirection.ToLabel(_blueprintWorkflow.ApplyRotation)} | 占地 {_blueprintWorkflow.ApplyPlan.FootprintSize.X}x{_blueprintWorkflow.ApplyPlan.FootprintSize.Y}\n{_blueprintWorkflow.ApplyPlan.GetIssueSummary()}"
            : _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection
                ? "框选完成后在这里输入名称并保存。"
                : "选择库中的蓝图后进入预览，再移动鼠标选择落点。";

        return new FactoryBlueprintPanelState
        {
            IsVisible = true,
            ModeText = modeText,
            ActiveBlueprintText = activeText,
            CaptureSummaryText = captureSummary,
            IssueText = issueText,
            SuggestedName = _pendingBlueprintCapture?.DisplayName ?? string.Empty,
            PendingCaptureId = _pendingBlueprintCapture?.Id,
            ActiveBlueprintId = activeBlueprint?.Id,
            AllowFullCapture = false,
            CanSaveCapture = _pendingBlueprintCapture is not null,
            CanConfirmApply = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _blueprintWorkflow.ApplyPlan?.IsValid == true,
            Blueprints = FactoryBlueprintLibrary.GetAll()
        };
    }

    private string GetSelectedStructureText()
    {
        if (_selectedResourceDeposit is not null && _selectedStructure is null)
        {
            return FactoryResourceDetailSupport.GetSelectionTargetText(_selectedResourceDeposit);
        }

        if (_selectedStructure is null || !GodotObject.IsInstanceValid(_selectedStructure) || !_selectedStructure.IsInsideTree())
        {
            return "未选中建筑";
        }

        return $"{_selectedStructure.DisplayName} @ ({_selectedStructure.Cell.X}, {_selectedStructure.Cell.Y}) | HP {_selectedStructure.CurrentHealth:0}/{_selectedStructure.MaxHealth:0}";
    }

    private (int deliveredTotal, int deliveredRate, int sinkCount) CollectSinkStats()
    {
        if (_structureRoot is null)
        {
            return (0, 0, 0);
        }

        var deliveredTotal = 0;
        var deliveredRate = 0;
        var sinkCount = 0;
        foreach (var child in _structureRoot.GetChildren())
        {
            if (child is SinkStructure sink)
            {
                deliveredTotal += sink.DeliveredTotal;
                deliveredRate += sink.DeliveredRate;
                sinkCount++;
            }
        }

        return (deliveredTotal, deliveredRate, sinkCount);
    }

    private FactoryStructure? PlaceStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (_grid is null || _structureRoot is null || _simulation is null || !TryValidateWorldPlacement(kind, cell, facing, out _))
        {
            return null;
        }

        var structure = FactoryStructureFactory.Create(kind, new FactoryStructurePlacement(_grid, cell, facing));
        _structureRoot.AddChild(structure);
        _grid.PlaceStructure(structure);
        _simulation.RegisterStructure(structure);
        RefreshAllTopology();
        return structure;
    }

    private FactoryStructure? PlaceStructure(BuildPrototypeKind kind, int x, int y, FacingDirection facing)
    {
        return PlaceStructure(kind, new Vector2I(x, y), facing);
    }

    private void RemoveStructure(Vector2I cell)
    {
        if (_grid is null || _simulation is null || !_grid.TryGetStructure(cell, out var structure) || structure is null)
        {
            return;
        }

        if (_selectedStructure == structure)
        {
            _selectedStructure = null;
        }

        _simulation.UnregisterStructure(structure);
        structure.Site.RemoveStructure(structure);
        structure.QueueFree();
        RefreshAllTopology();
    }

    private void RefreshAllTopology()
    {
        _simulation?.RebuildTopology();
    }

    private void HandlePrimaryClick()
    {
        if (!_hasHoveredCell)
        {
            TraceLog("HandlePrimaryClick ignored because there is no hovered cell");
            return;
        }

        TraceLog($"HandlePrimaryClick interaction mode select hoveredStructure={_hoveredStructure?.DisplayName ?? "none"}");
        _selectedStructure = _hoveredStructure;
        _selectedResourceDeposit = _hoveredStructure is null ? _hoveredResourceDeposit : null;
    }

    private void HandleSecondaryClick()
    {
        if (_interactionMode == FactoryInteractionMode.Build)
        {
            EnterInteractionMode();
            return;
        }

        if (!_hasHoveredCell)
        {
            return;
        }

        _selectedStructure = null;
        _selectedResourceDeposit = null;
    }

    private void HandleDeletePrimaryPress(bool shiftPressed)
    {
        if (!_hasHoveredCell)
        {
            return;
        }

        if (shiftPressed)
        {
            _deleteDrag.BeginDrag(_hoveredCell);
            return;
        }

        DeleteHoveredStructure();
    }

    private void HandleDeletePrimaryRelease()
    {
        if (!_deleteDrag.Active)
        {
            return;
        }

        _deleteDrag.EndDrag();
        DeleteStructuresInRect(_deleteDrag.StartCell, _deleteDrag.CurrentCell);
    }

    private void DeleteHoveredStructure()
    {
        if (_hoveredStructure is not null)
        {
            RemoveStructure(_hoveredStructure.Cell);
        }
    }

    private Rect2I GetDeleteRect(Vector2I start, Vector2I end)
    {
        return FactorySelectionRectSupport.BuildInclusiveRect(start, end);
    }

    private int CountStructuresInDeleteRect(Vector2I start, Vector2I end)
    {
        if (_grid is null)
        {
            return 0;
        }

        return FactorySelectionRectSupport.CountUniqueStructuresInRect(
            start,
            end,
            cell => _grid.TryGetStructure(cell, out var structure) ? structure : null);
    }

    private void DeleteStructuresInRect(Vector2I start, Vector2I end)
    {
        if (_grid is null)
        {
            return;
        }

        var cellsToDelete = FactorySelectionRectSupport.CollectUniqueStructureAnchorCells(
            start,
            end,
            cell => _grid.TryGetStructure(cell, out var structure) ? structure : null);

        for (var index = 0; index < cellsToDelete.Count; index++)
        {
            RemoveStructure(cellsToDelete[index]);
        }
    }

    private int CountStructuresInBlueprintRect(Rect2I rect)
    {
        return _blueprintWorkflow.CountStructuresInRect(rect);
    }

    private void EnsureBlueprintPreviewCapacity(int count)
    {
        _blueprintWorkflow.EnsurePreviewMeshCapacity(count, FactoryConstants.CellSize);
    }

    private FactoryStructure EnsureBlueprintGhostPreview(FactoryBlueprintPlanEntry entry, int index)
    {
        return _blueprintWorkflow.EnsureGhostPreview(index, entry.SourceEntry.Kind, entry.TargetCell, entry.TargetFacing, "BlueprintGhostPreview");
    }

    private static bool SupportsGhostBlueprintPreview()
    {
        return !HasSmokeTestFlag();
    }

    private FactoryBlueprintSiteAdapter CreateBlueprintSiteAdapter()
    {
        return new FactoryBlueprintSiteAdapter(
            FactoryBlueprintSiteKind.WorldGrid,
            _grid!.SiteId,
            "世界沙盒",
            _grid.MinCell,
            _grid.MaxCell,
            () => _grid.GetStructures(),
            ValidateBlueprintPlacement,
            (kind, cell, facing) => PlaceStructure(kind, cell, facing),
            cell =>
            {
                if (_grid.TryGetStructure(cell, out var structure) && structure is not null)
                {
                    RemoveStructure(cell);
                    return true;
                }

                return false;
            });
    }

    private string? ValidateBlueprintPlacement(FactoryBlueprintStructureEntry entry, Vector2I targetCell, FacingDirection targetFacing)
    {
        if (_grid is null)
        {
            return "世界网格不可用。";
        }

        var definition = FactoryStructureFactory.GetDefinition(entry.Kind);
        if (!FactoryIndustrialStandards.IsStructureAllowed(entry.Kind, FactorySiteKind.World))
        {
            return FactoryIndustrialStandards.GetPlacementCompatibilityError(entry.Kind, FactorySiteKind.World);
        }

        if (!_grid.IsInBounds(targetCell))
        {
            return "目标格超出世界建造范围。";
        }

        if (!_grid.CanPlace(targetCell))
        {
            return "目标格已被占用。";
        }

        if (!TryValidateWorldPlacement(entry.Kind, targetCell, targetFacing, out var placementIssue))
        {
            return placementIssue;
        }

        return null;
    }

    private void StartBlueprintCapture()
    {
        _blueprintWorkflow.ResetAll();
        _pendingBlueprintCapture = null;
        EnterInteractionMode();
        _blueprintMode = FactoryBlueprintWorkflowMode.CaptureSelection;
    }

    private void BeginBlueprintSelection()
    {
        if (!_hasHoveredCell)
        {
            return;
        }

        _blueprintWorkflow.BeginSelection(_hoveredCell);
        _pendingBlueprintCapture = null;
    }

    private void CompleteBlueprintSelection()
    {
        if (!_blueprintWorkflow.SelectionDragActive)
        {
            return;
        }

        var selectionRect = _blueprintWorkflow.CompleteDragSelection();

        if (_blueprintWorkflow.Site is null)
        {
            return;
        }

        var suggestedName = $"沙盒蓝图 {CountStructuresInBlueprintRect(selectionRect)} 件";
        _pendingBlueprintCapture = FactoryBlueprintCaptureService.CaptureSelection(_blueprintWorkflow.Site, selectionRect, suggestedName);
        if (_pendingBlueprintCapture is null)
        {
            _previewMessage = "框选区域内没有可保存的建筑。";
            _blueprintWorkflow.ResetSelection();
        }
    }

    private void HandleBlueprintSaveRequested(string name, FactoryBlueprintPersistenceTarget target)
    {
        if (_pendingBlueprintCapture is null)
        {
            return;
        }

        var savedRecord = FactoryBlueprintWorkflowBridge.SavePendingCapture(_pendingBlueprintCapture, name, target);
        _pendingBlueprintCapture = null;
        _blueprintWorkflow.ResetAll();
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        ShowBlueprintPersistenceStatus(savedRecord, target);
    }

    private void HandleBlueprintSelected(string blueprintId)
    {
        FactoryBlueprintWorkflowBridge.SelectBlueprint(
            blueprintId,
            _blueprintMode,
            () => _blueprintWorkflow.UpdateApplyPlan(_hoveredCell));
    }

    private void EnterBlueprintApplyMode()
    {
        if (FactoryBlueprintLibrary.GetActive() is null)
        {
            return;
        }

        _blueprintWorkflow.ResetAll();
        _pendingBlueprintCapture = null;
        EnterInteractionMode();
        _blueprintMode = FactoryBlueprintWorkflowMode.ApplyPreview;
    }

    private void ConfirmBlueprintApply()
    {
        if (_blueprintWorkflow.Site is null || _blueprintWorkflow.ApplyPlan is null)
        {
            return;
        }

        if (!FactoryBlueprintPlanner.CommitPlan(_blueprintWorkflow.ApplyPlan, _blueprintWorkflow.Site))
        {
            _previewMessage = "蓝图应用失败，请检查预览中的阻塞原因。";
            return;
        }

        _selectedStructure = null;
        RefreshAllTopology();
        _previewMessage = $"已应用蓝图：{_blueprintWorkflow.ApplyPlan.Blueprint.DisplayName}（旋转 {FactoryDirection.ToLabel(_blueprintWorkflow.ApplyPlan.Rotation)}）";
    }

    private void HandleBlueprintDeleteRequested(string blueprintId)
    {
        FactoryBlueprintWorkflowBridge.DeleteBlueprint(blueprintId, () => _blueprintWorkflow.ApplyPlan = null);
    }

    private void CancelBlueprintWorkflow()
    {
        CancelBlueprintWorkflow(clearActiveBlueprint: false);
    }

    private void CancelBlueprintWorkflow(bool clearActiveBlueprint)
    {
        _blueprintWorkflow.ResetAll();
        _pendingBlueprintCapture = null;
        _blueprintMode = FactoryBlueprintWorkflowMode.None;

        if (clearActiveBlueprint)
        {
            FactoryBlueprintLibrary.ClearActive();
        }
    }

    private void RotateBlueprintApplyPreview(int direction)
    {
        if (_blueprintMode != FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            return;
        }

        _blueprintWorkflow.RotatePreview(direction);
        _blueprintWorkflow.UpdateApplyPlan(_hoveredCell);
    }

    private void UpdateCursorShape()
    {
        Input.SetDefaultCursorShape(_interactionMode == FactoryInteractionMode.Delete
            ? Input.CursorShape.Cross
            : Input.CursorShape.Arrow);
    }

    private void HandleDetailInventoryMoveRequested(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack)
    {
        if (FactoryDemoInteractionBridge.TryMoveDetailInventoryItem(_selectedStructure, inventoryId, fromSlot, toSlot, splitStack))
        {
            UpdateHud();
        }
    }

    private void HandlePlayerInventoryMoveRequested(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack)
    {
        if (!FactoryDemoInteractionBridge.TryMoveInventoryItem(TryResolveInventoryEndpoint, inventoryId, fromSlot, toSlot, splitStack))
        {
            return;
        }

        UpdateHud();
    }

    private void HandlePlayerInventoryTransferRequested(string fromInventoryId, Vector2I fromSlot, string toInventoryId, Vector2I toSlot, bool splitStack)
    {
        if (FactoryDemoInteractionBridge.TryTransferInventoryItem(TryResolveInventoryEndpoint, fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack))
        {
            UpdateHud();
        }
    }

    private void HandlePlayerInventorySlotActivated(string inventoryId, Vector2I slot)
    {
        if (_playerPlacementState.HandleInventorySlotActivated(
                _playerController,
                TryResolveInventoryEndpoint,
                inventoryId,
                slot,
                HandlePlayerHotbarPressed))
        {
            return;
        }

        _selectedBuildKind = null;
        RefreshInteractionModeFromBuildSource();
        UpdateHud();
    }

    private void HandleDetailRecipeSelected(string recipeId)
    {
        if (FactoryDemoInteractionBridge.TrySetDetailRecipe(_selectedStructure, recipeId))
        {
            UpdateHud();
        }
    }

    private void HandleDetailWindowClosed()
    {
        _selectedStructure = null;
        _selectedResourceDeposit = null;
        UpdateHud();
    }

    private bool TryResolveSelectedPlayerPlaceable(out BuildPrototypeKind kind)
    {
        var item = ResolveSelectedPlayerItem();
        return FactoryPresentation.TryGetPlaceableStructureKind(item, out kind);
    }

    private bool TryConsumeSelectedPlayerPlaceable()
    {
        return _playerPlacementState.TryConsumeSelectedPlaceable(_playerController, TryResolveInventoryEndpoint);
    }

    private FactoryItem? ResolveSelectedPlayerItem()
    {
        return _playerPlacementState.ResolveSelectedPlayerItem(_playerController, TryResolveInventoryEndpoint);
    }

    private bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        return FactoryDemoInteractionBridge.TryResolveInventoryEndpoint(_playerController, _selectedStructure, inventoryId, out endpoint);
    }

    private bool IsPointerOverUi()
    {
        var hoveredControl = GetViewport().GuiGetHoveredControl();
        var pointer = GetViewport().GetMousePosition();
        return (_hud?.BlocksWorldInput(hoveredControl, pointer) ?? false)
            || (_playerHud?.BlocksWorldInput(hoveredControl, pointer) ?? false);
    }

    private bool IsInventoryUiInteractionActive()
    {
        return (_hud?.HasActiveInventoryInteraction ?? false)
            || (_playerHud?.HasActiveInventoryInteraction ?? false);
    }

    private bool IsWorldPointerInputBlocked()
    {
        return FactoryBaselineInteractionRules.BlocksWorldPointerInput(
            IsPointerOverUi(),
            IsInventoryUiInteractionActive());
    }

    private void CreatePreviewVisuals()
    {
        if (_previewRoot is null)
        {
            return;
        }

        _previewCell = FactoryPreviewOverlaySupport.CreatePreviewCell("PreviewCell", FactoryConstants.CellSize);
        _previewRoot.AddChild(_previewCell);

        _previewArrow = FactoryPreviewOverlaySupport.CreateFacingArrow("PreviewArrow", FactoryConstants.CellSize, 0.18f);
        _previewRoot.AddChild(_previewArrow);

        _previewPowerRange = FactoryPreviewOverlaySupport.CreatePreviewPowerRange("PreviewPowerRange");
        _previewRoot.AddChild(_previewPowerRange);

        FactoryPreviewOverlaySupport.ApplyPreviewColor(_previewCell, new Color(0.35f, 0.95f, 0.55f, 0.45f));
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_previewArrow, new Color(0.35f, 0.95f, 0.55f, 0.45f));
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_previewPowerRange, new Color(0.35f, 0.95f, 0.55f, 0.15f));
        _previewRoot.Visible = false;
    }

    private void UpdatePreviewPortHints(BuildPrototypeKind previewKind)
    {
        if (_grid is null || _previewPortHintRoot is null || !_hasHoveredCell || !FactoryLogisticsPreview.ShouldShowContextualPortHints(previewKind))
        {
            SetPreviewPortHintCount(0);
            return;
        }

        var visibleRect = GetVisibleWorldCellRectOrBounds();
        var markers = GetPreviewPortMarkers(previewKind, visibleRect);
        EnsurePreviewPortHintMeshCount(markers.Count);
        var visibleCount = 0;
        for (var index = 0; index < markers.Count; index++)
        {
            var marker = markers[index];
            var arrow = _previewPortHintMeshes[index];
            FactoryPreviewOverlaySupport.ConfigureDirectionalArrow(
                arrow,
                _grid.CellToWorld(marker.Cell) + new Vector3(0.0f, marker.IsHighlighted ? 0.13f : 0.10f, 0.0f),
                marker.Facing,
                marker.IsInput
                    ? marker.IsHighlighted
                        ? new Color(0.38f, 0.78f, 1.0f, 0.82f)
                        : new Color(0.38f, 0.78f, 1.0f, 0.52f)
                    : marker.IsHighlighted
                        ? new Color(1.0f, 0.68f, 0.26f, 0.82f)
                        : new Color(1.0f, 0.68f, 0.26f, 0.52f),
                marker.IsHighlighted ? 1.10f : 0.92f);
            visibleCount++;
        }

        SetPreviewPortHintCount(visibleCount);
    }

    private void EnsurePreviewPortHintMeshCount(int count)
    {
        FactoryPreviewPoolSupport.EnsureNodeCapacity(
            _previewPortHintRoot,
            _previewPortHintMeshes,
            count,
            index => FactoryPreviewOverlaySupport.CreatePortHintArrow($"PreviewPortHint_{index}", _grid?.CellSize ?? FactoryConstants.CellSize));
    }

    private void SetPreviewPortHintCount(int visibleCount)
    {
        FactoryPreviewPoolSupport.SetVisibleNodeCount(_previewPortHintRoot, _previewPortHintMeshes, visibleCount);
    }

    private List<FactoryPortPreviewMarker> GetPreviewPortMarkers(BuildPrototypeKind previewKind, Rect2I visibleRect)
    {
        if (_grid is null)
        {
            _cachedPreviewPortMarkers.Clear();
            return _cachedPreviewPortMarkers;
        }

        var structureRevision = _grid.StructureRevision;
        if (_hasCachedPreviewPortMarkers
            && _cachedPreviewPortKind == previewKind
            && _cachedPreviewPortFacing == _selectedFacing
            && _cachedPreviewPortCell == _hoveredCell
            && _cachedPreviewPortVisibleRect == visibleRect
            && _cachedPreviewPortRevision == structureRevision)
        {
            return _cachedPreviewPortMarkers;
        }

        _cachedPreviewPortMarkers.Clear();
        _cachedPreviewPortMarkers.AddRange(FactoryLogisticsPreview.CollectPortMarkers(
            _grid,
            previewKind,
            _hoveredCell,
            _selectedFacing,
            EnumerateStructuresInRect(visibleRect)));
        _cachedPreviewPortKind = previewKind;
        _cachedPreviewPortFacing = _selectedFacing;
        _cachedPreviewPortCell = _hoveredCell;
        _cachedPreviewPortVisibleRect = visibleRect;
        _cachedPreviewPortRevision = structureRevision;
        _hasCachedPreviewPortMarkers = true;
        return _cachedPreviewPortMarkers;
    }

    private Rect2I GetVisibleWorldCellRectOrBounds()
    {
        if (_grid is null)
        {
            return default;
        }

        return TryGetVisibleWorldCellRect(out var visibleRect)
            ? visibleRect
            : FactoryGridUtility.BuildCellRect(_grid.MinCell, _grid.MaxCell);
    }

    private IEnumerable<FactoryStructure> EnumerateStructuresInRect(Rect2I rect)
    {
        if (_grid is null || rect.Size.X <= 0 || rect.Size.Y <= 0)
        {
            yield break;
        }

        var seenStructureIds = new HashSet<ulong>();
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                if (!_grid.TryGetStructure(new Vector2I(x, y), out var structure) || structure is null)
                {
                    continue;
                }

                if (seenStructureIds.Add(structure.GetInstanceId()))
                {
                    yield return structure;
                }
            }
        }
    }

    private bool TryGetVisibleWorldCellRect(out Rect2I visibleRect)
    {
        visibleRect = default;

        if (_grid is null || _cameraRig is null || !_cameraRig.TryProjectViewportRectToPlane(0.0f, out var projectedRect))
        {
            return false;
        }

        visibleRect = FactoryGridUtility.BuildCellRect(
            _grid.WorldToCell(new Vector3(projectedRect.Position.X, 0.0f, projectedRect.Position.Y)),
            _grid.WorldToCell(new Vector3(projectedRect.End.X, 0.0f, projectedRect.End.Y)),
            1);
        return true;
    }
    private void HandleBuildPrimaryPress()
    {
        _buildDrag.BeginStroke();
        TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: true);
    }

    private void HandleBuildPrimaryRelease()
    {
        ResetBuildPlacementStroke();
    }

    private bool HandleBuildDragPlacement()
    {
        if (!_buildDrag.Active)
        {
            return false;
        }

        if (_interactionMode != FactoryInteractionMode.Build || !Input.IsMouseButtonPressed(MouseButton.Left))
        {
            ResetBuildPlacementStroke();
            return false;
        }

        return TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: true);
    }

    private bool TryPlaceCurrentBuildTarget(bool trackCurrentCellForStroke)
    {
        if (!_hasHoveredCell || _interactionMode != FactoryInteractionMode.Build || !TryGetActivePlacementKind(out var placementKind, out var usesPlayerInventory))
        {
            return false;
        }

        if (trackCurrentCellForStroke && _buildDrag.StrokeCells.Contains(_hoveredCell))
        {
            return false;
        }

        var placementFacing = ResolveWorldPlacementFacing(placementKind, _hoveredCell, trackCurrentCellForStroke);
        var canPlaceCurrentCell = TryValidateWorldPlacement(placementKind, _hoveredCell, placementFacing, out var placementMessage);
        _canPlaceCurrentCell = canPlaceCurrentCell;
        _previewMessage = placementMessage;
        TraceLog($"TryPlaceCurrentBuildTarget cell={_hoveredCell} kind={placementKind} usesPlayerInventory={usesPlayerInventory} canPlace={_canPlaceCurrentCell} drag={trackCurrentCellForStroke}");
        if (!canPlaceCurrentCell)
        {
            TraceLog($"TryPlaceCurrentBuildTarget blocked previewMessage={_previewMessage}");
            return false;
        }

        var targetCell = _hoveredCell;
        var previousStrokeCell = _buildDrag.LastStrokeCell;
        var hadPreviousStrokeCell = _buildDrag.HasLastStrokeCell;
        var placedStructure = PlaceStructure(placementKind, targetCell, placementFacing);
        TraceLog($"TryPlaceCurrentBuildTarget placement result placed={(placedStructure is not null)}");
        if (placedStructure is null)
        {
            return false;
        }

        _selectedFacing = placementFacing;
        if (placementKind == BuildPrototypeKind.Belt && hadPreviousStrokeCell)
        {
            if (ReorientBeltAt(previousStrokeCell, placementFacing))
            {
                RefreshAllTopology();
            }
        }

        if (trackCurrentCellForStroke)
        {
            _buildDrag.TryRegisterCell(targetCell);
        }

        if (usesPlayerInventory)
        {
            var consumed = TryConsumeSelectedPlayerPlaceable();
            TraceLog($"TryPlaceCurrentBuildTarget consumed player placeable={consumed}");
            RefreshInteractionModeFromBuildSource();
        }

        return true;
    }

    private void ResetBuildPlacementStroke()
    {
        _buildDrag.Reset();
    }

    private FacingDirection ResolveWorldPlacementFacing(BuildPrototypeKind placementKind, Vector2I cell, bool trackCurrentCellForStroke)
    {
        if (placementKind != BuildPrototypeKind.Belt)
        {
            return _selectedFacing;
        }

        if (TryResolveExistingBeltConnectionFacing(cell, out var autoConnectFacing))
        {
            return autoConnectFacing;
        }

        if (trackCurrentCellForStroke
            && _buildDrag.HasLastStrokeCell
            && TryResolveBeltDragFacing(_buildDrag.LastStrokeCell, cell, out var dragFacing))
        {
            _selectedFacing = dragFacing;
            return dragFacing;
        }

        return _selectedFacing;
    }

    private bool ReorientBeltAt(Vector2I cell, FacingDirection facing)
    {
        if (_grid is null || !_grid.TryGetStructure(cell, out var structure) || structure is not BeltStructure belt)
        {
            return false;
        }

        if (belt.Facing == facing)
        {
            return false;
        }

        belt.Reorient(facing);
        return true;
    }

    private static bool TryResolveBeltDragFacing(Vector2I fromCell, Vector2I toCell, out FacingDirection facing)
    {
        var delta = toCell - fromCell;
        if (Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) != 1)
        {
            facing = FacingDirection.East;
            return false;
        }

        facing = delta.X > 0
            ? FacingDirection.East
            : delta.X < 0
                ? FacingDirection.West
                : delta.Y > 0
                    ? FacingDirection.South
                    : FacingDirection.North;
        return true;
    }

    private bool TryResolveExistingBeltConnectionFacing(Vector2I cell, out FacingDirection facing)
    {
        facing = FacingDirection.East;

        if (_grid is null)
        {
            return false;
        }

        var resolved = false;
        foreach (FacingDirection candidateFacing in System.Enum.GetValues(typeof(FacingDirection)))
        {
            var inputConnected = false;
            var inputCells = FactoryTransportTopology.GetBeltInputCells(cell, candidateFacing);
            for (var index = 0; index < inputCells.Count; index++)
            {
                if (!_grid.TryGetStructure(inputCells[index], out var inputStructure) || inputStructure is not BeltStructure)
                {
                    continue;
                }

                if (inputStructure.CanOutputTo(cell))
                {
                    inputConnected = true;
                    break;
                }
            }

            if (!inputConnected)
            {
                continue;
            }

            var outputCell = FactoryTransportTopology.GetBeltOutputCell(cell, candidateFacing);
            if (!_grid.TryGetStructure(outputCell, out var outputStructure) || outputStructure is not BeltStructure || !outputStructure.CanReceiveFrom(cell))
            {
                continue;
            }

            if (resolved)
            {
                facing = FacingDirection.East;
                return false;
            }

            facing = candidateFacing;
            resolved = true;
        }

        return resolved;
    }

    private void EnsureInputActions()
    {
        FactoryDemoInputActions.EnsureCommonActions();
        FactoryDemoInputActions.EnsureAction("camera_rotate_left", new InputEventKey { PhysicalKeycode = Key.Q });
        FactoryDemoInputActions.EnsureAction("camera_rotate_right", new InputEventKey { PhysicalKeycode = Key.E });
        FactoryDemoInputActions.EnsureAction("build_confirm", new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        FactoryDemoInputActions.EnsureAction("remove_structure", new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true });
        FactoryDemoInputActions.EnsureAction("build_cancel", new InputEventKey { PhysicalKeycode = Key.Escape });
        FactoryDemoInputActions.EnsureAction("select_producer", new InputEventKey { PhysicalKeycode = Key.Key1 });
        FactoryDemoInputActions.EnsureAction("select_belt", new InputEventKey { PhysicalKeycode = Key.Key2 });
        FactoryDemoInputActions.EnsureAction("select_sink", new InputEventKey { PhysicalKeycode = Key.Key3 });
        FactoryDemoInputActions.EnsureAction("select_splitter", new InputEventKey { PhysicalKeycode = Key.Key4 });
        FactoryDemoInputActions.EnsureAction("select_merger", new InputEventKey { PhysicalKeycode = Key.Key5 });
        FactoryDemoInputActions.EnsureAction("select_bridge", new InputEventKey { PhysicalKeycode = Key.Key6 });
        FactoryDemoInputActions.EnsureAction("select_loader", new InputEventKey { PhysicalKeycode = Key.Key7 });
        FactoryDemoInputActions.EnsureAction("select_unloader", new InputEventKey { PhysicalKeycode = Key.Key8 });
        FactoryDemoInputActions.EnsureAction("select_storage", new InputEventKey { PhysicalKeycode = Key.Key9 });
        FactoryDemoInputActions.EnsureAction("select_inserter", new InputEventKey { PhysicalKeycode = Key.Key0 });
        FactoryDemoInputActions.EnsureAction("select_wall", new InputEventKey { PhysicalKeycode = Key.Minus });
        FactoryDemoInputActions.EnsureAction("select_ammo_assembler", new InputEventKey { PhysicalKeycode = Key.Equal });
        FactoryDemoInputActions.EnsureAction("select_gun_turret", new InputEventKey { PhysicalKeycode = Key.P });
    }



    private bool TryValidateWorldPlacement(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing, out string message)
    {
        message = "世界网格不可用。";
        if (_grid is null)
        {
            return false;
        }

        if (!_grid.IsInBounds(cell))
        {
            message = "超出可建造范围。";
            return false;
        }

        return _grid.CanPlaceStructure(kind, cell, facing, out message);
    }

    private string DescribeWorldPlacementPreview(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing, bool usesPlayerInventory)
    {
        var displayName = usesPlayerInventory
            ? FactoryPresentation.GetBuildPrototypeDisplayName(kind)
            : _definitions[kind].DisplayName;

        if (_grid is not null
            && kind == BuildPrototypeKind.Belt
            && FactoryTransportTopology.TryGetBeltMidspanMergeTarget(_grid, cell, facing, out var mergeTargetCell))
        {
            return $"可在 ({cell.X}, {cell.Y}) 放置{displayName}，朝向 {FactoryDirection.ToLabel(facing)}，并以 T 字方式并入 ({mergeTargetCell.X}, {mergeTargetCell.Y}) 的传送带。";
        }

        if (kind == BuildPrototypeKind.Merger)
        {
            return $"可在 ({cell.X}, {cell.Y}) 放置{displayName}，朝向 {FactoryDirection.ToLabel(facing)}，三入口分别来自后方、左侧和右侧。";
        }

        return $"可在 ({cell.X}, {cell.Y}) 放置{displayName}，朝向 {FactoryDirection.ToLabel(facing)}";
    }
}

