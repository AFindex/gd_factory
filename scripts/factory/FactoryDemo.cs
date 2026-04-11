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

    private readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> _definitions = new()
    {
        [BuildPrototypeKind.Producer] = new BuildPrototypeDefinition(BuildPrototypeKind.Producer, "兼容生产器", new Color("9DC08B"), "兼容型占位产物流，仅用于 legacy 回归线。"),
        [BuildPrototypeKind.MiningDrill] = new BuildPrototypeDefinition(BuildPrototypeKind.MiningDrill, "采矿机", new Color("FBBF24"), "只能放在矿点上，通电后持续采出煤、铁矿石或铜矿石。"),
        [BuildPrototypeKind.Generator] = new BuildPrototypeDefinition(BuildPrototypeKind.Generator, "发电机", new Color("FB923C"), "消耗煤炭发电，为周边电网提供基础供给。"),
        [BuildPrototypeKind.PowerPole] = new BuildPrototypeDefinition(BuildPrototypeKind.PowerPole, "电线杆", new Color("FDE68A"), "延伸电网覆盖，把发电机接到更远的机器。"),
        [BuildPrototypeKind.Smelter] = new BuildPrototypeDefinition(BuildPrototypeKind.Smelter, "熔炉", new Color("CBD5E1"), "消耗电力把铁矿、铜矿或铁板冶炼成更高阶板材。"),
        [BuildPrototypeKind.Assembler] = new BuildPrototypeDefinition(BuildPrototypeKind.Assembler, "组装机", new Color("67E8F9"), "消耗板材和中间件，组装铜线、电路、机加工件与弹药。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送，也允许末端直接并入另一段传送带的中段。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收站", new Color("FDE68A"), "接收物品并统计送达数量。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把后方、左侧和右侧三路物流汇成前方一路。"),
        [BuildPrototypeKind.Bridge] = new BuildPrototypeDefinition(BuildPrototypeKind.Bridge, "跨桥", new Color("F59E0B"), "让南北和东西两路物流跨越而不互连。"),
        [BuildPrototypeKind.Loader] = new BuildPrototypeDefinition(BuildPrototypeKind.Loader, "装载器", new Color("FDBA74"), "把后方带上的物品装入前方机器或回收端。"),
        [BuildPrototypeKind.Unloader] = new BuildPrototypeDefinition(BuildPrototypeKind.Unloader, "卸载器", new Color("93C5FD"), "把机器端输出卸到前方传送网络。"),
        [BuildPrototypeKind.Storage] = new BuildPrototypeDefinition(BuildPrototypeKind.Storage, "仓储", new Color("94A3B8"), "缓存多件物品，可向前输出，也能被机械臂抓取。"),
        [BuildPrototypeKind.LargeStorageDepot] = new BuildPrototypeDefinition(BuildPrototypeKind.LargeStorageDepot, "大型仓储", new Color("64748B"), "占据 2x2 空间的大型缓存仓，可作为更稳定的物流缓冲点。"),
        [BuildPrototypeKind.Inserter] = new BuildPrototypeDefinition(BuildPrototypeKind.Inserter, "机械臂", new Color("FACC15"), "从后方抓取一件物品并向前投送。"),
        [BuildPrototypeKind.Wall] = new BuildPrototypeDefinition(BuildPrototypeKind.Wall, "墙体", new Color("D1D5DB"), "高耐久阻挡物，用来拖延敌人推进。"),
        [BuildPrototypeKind.AmmoAssembler] = new BuildPrototypeDefinition(BuildPrototypeKind.AmmoAssembler, "弹药组装器", new Color("FB923C"), "持续生产弹药，沿物流链补给防线。"),
        [BuildPrototypeKind.GunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.GunTurret, "机枪炮塔", new Color("CBD5E1"), "需要弹药供给，敌人进入射程时自动开火。"),
        [BuildPrototypeKind.HeavyGunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.HeavyGunTurret, "重型炮塔", new Color("E2E8F0"), "占据 2x2 空间，消耗高速弹药并发射独立炮弹。")
    };

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
    private Node3D? _blueprintPreviewRoot;
    private Node3D? _blueprintGhostPreviewRoot;
    private Node3D? _powerLinkOverlayRoot;
    private MeshInstance3D? _previewCell;
    private Node3D? _previewArrow;
    private MeshInstance3D? _previewPowerRange;
    private readonly List<Node3D> _previewPortHintMeshes = new();
    private readonly List<FactoryPortPreviewMarker> _cachedPreviewPortMarkers = new();
    private readonly List<MeshInstance3D> _blueprintPreviewMeshes = new();
    private readonly List<FactoryStructure> _blueprintPreviewGhosts = new();
    private readonly List<MeshInstance3D> _powerLinkDashes = new();
    private FactoryCombatDirector? _combatDirector;
    private FactoryBlueprintSiteAdapter? _blueprintSite;
    private double _averageFrameMilliseconds;
    private double _averageVisualSyncMilliseconds;
    private BuildPrototypeKind? _selectedBuildKind;
    private FacingDirection _selectedFacing = FacingDirection.East;
    private FactoryInteractionMode _interactionMode = FactoryInteractionMode.Interact;
    private FactoryStructure? _selectedStructure;
    private FactoryStructure? _hoveredStructure;
    private Vector2I _hoveredCell;
    private Vector2I _cachedPreviewPortCell;
    private Rect2I _cachedPreviewPortVisibleRect;
    private Vector2I _lastBuildStrokeCell;
    private bool _hasHoveredCell;
    private bool _hasCachedPreviewPortMarkers;
    private bool _hasLastBuildStrokeCell;
    private bool _canPlaceCurrentCell;
    private bool _canDeleteCurrentCell;
    private bool _deleteDragActive;
    private Vector2I _deleteDragStartCell;
    private Vector2I _deleteDragCurrentCell;
    private bool _buildPlacementDragActive;
    private readonly HashSet<Vector2I> _buildPlacementStrokeCells = new();
    private FactoryBlueprintWorkflowMode _blueprintMode;
    private bool _blueprintSelectionDragActive;
    private bool _hasBlueprintSelectionRect;
    private Vector2I _blueprintSelectionStartCell;
    private Vector2I _blueprintSelectionCurrentCell;
    private Rect2I _blueprintSelectionRect;
    private FactoryBlueprintRecord? _pendingBlueprintCapture;
    private FactoryBlueprintApplyPlan? _blueprintApplyPlan;
    private FacingDirection _blueprintApplyRotation = FacingDirection.East;
    private string _previewMessage = "交互模式：点击建筑查看；按数字键选择建筑后进入建造，或按住 Shift 左键框选蓝图。";
    private string? _selectedPlayerItemInventoryId;
    private Vector2I _selectedPlayerItemSlot;
    private bool _hasSelectedPlayerItemSlot;
    private bool _playerPlacementArmed;
    private readonly FactoryPlayerInventorySelectionState _playerSelectionState = new();
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
        _averageFrameMilliseconds = SmoothMetric(_averageFrameMilliseconds, delta * 1000.0, 0.1);
        _playerController?.ApplyMovement(GetPlayerMovementBounds(), delta, allowInput: true);
        if (_cameraRig is not null)
        {
            _cameraRig.AllowPanInput = false;
            _cameraRig.AllowZoomInput = !IsPointerOverUi();
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

        if (_interactionMode == FactoryInteractionMode.Delete && _deleteDragActive && _hasHoveredCell)
        {
            _deleteDragCurrentCell = _hoveredCell;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey hotbarKeyEvent
            && hotbarKeyEvent.Pressed
            && !hotbarKeyEvent.Echo
            && TryMapHotbarKey(hotbarKeyEvent.Keycode, out var hotbarIndex))
        {
            HandlePlayerHotbarPressed(hotbarIndex);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouse && IsInventoryUiInteractionActive())
        {
            if (@event is InputEventMouseButton blockedMouseButton)
            {
                TraceLog($"mouse input blocked by active inventory interaction button={blockedMouseButton.ButtonIndex} pressed={blockedMouseButton.Pressed}");
            }
            return;
        }

        if (IsPointerOverUi())
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
        _blueprintPreviewRoot = scaffold.GetRoot("blueprint-preview");
        _blueprintGhostPreviewRoot = scaffold.GetRoot("blueprint-ghost-preview");
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
        _blueprintSite = CreateBlueprintSiteAdapter();
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
        _playerPlacementArmed = false;
        if (kind.HasValue)
        {
            _playerController?.DisarmHotbarPlacement();
        }

        RefreshInteractionModeFromBuildSource();
        _deleteDragActive = false;
        ResetBuildPlacementStroke();

        if (_interactionMode == FactoryInteractionMode.Build)
        {
            _selectedStructure = null;
        }
    }

    private void EnterInteractionMode()
    {
        _selectedBuildKind = null;
        _playerPlacementArmed = false;
        _playerController?.DisarmHotbarPlacement();
        _interactionMode = FactoryInteractionMode.Interact;
        _deleteDragActive = false;
        ResetBuildPlacementStroke();
    }

    private void EnterDeleteMode()
    {
        CancelBlueprintWorkflow(clearActiveBlueprint: false);
        _selectedBuildKind = null;
        _playerPlacementArmed = false;
        _playerController?.DisarmHotbarPlacement();
        _selectedStructure = null;
        _interactionMode = FactoryInteractionMode.Delete;
        _deleteDragActive = false;
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
        _cameraRig?.SetFollowTarget(_playerController, snapImmediately: true);
        _cameraRig!.FollowTargetEnabled = true;
        _selectedPlayerItemInventoryId = FactoryPlayerController.BackpackInventoryId;
        _selectedPlayerItemSlot = new Vector2I(0, 0);
        _hasSelectedPlayerItemSlot = true;
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

        if (_playerPlacementArmed && TryResolveSelectedPlayerPlaceable(out var selectedPlayerKind))
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
        if (_interactionMode == FactoryInteractionMode.Delete)
        {
            return;
        }

        _interactionMode = TryGetActivePlacementKind(out _, out _)
            ? FactoryInteractionMode.Build
            : FactoryInteractionMode.Interact;
    }

    private void HandlePlayerHotbarPressed(int index)
    {
        if (_playerController is null)
        {
            return;
        }

        CancelBlueprintWorkflow(clearActiveBlueprint: false);
        _selectedBuildKind = null;
        _playerController.ToggleHotbarIndex(index);
        _selectedPlayerItemInventoryId = FactoryPlayerController.BackpackInventoryId;
        _selectedPlayerItemSlot = new Vector2I(index, 0);
        _hasSelectedPlayerItemSlot = true;
        _playerPlacementArmed = _playerController.IsHotbarPlacementArmed;
        RefreshInteractionModeFromBuildSource();
    }

    private static bool TryMapHotbarKey(Key keycode, out int hotbarIndex)
    {
        hotbarIndex = keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            Key.Key6 => 5,
            Key.Key7 => 6,
            Key.Key8 => 7,
            Key.Key9 => 8,
            Key.Key0 => 9,
            _ => -1
        };

        return hotbarIndex >= 0;
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
        _canPlaceCurrentCell = false;
        _canDeleteCurrentCell = false;
        _blueprintApplyPlan = null;
        _previewMessage = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图框选：拖拽选择一片现有布局，然后在右侧面板保存。",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图预览：移动鼠标选择锚点，按 Q/E 旋转，当前朝向 {FactoryDirection.ToLabel(_blueprintApplyRotation)}。",
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

        if (IsPointerOverUi())
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
        if (_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection)
        {
            if (_blueprintSelectionDragActive)
            {
                _blueprintSelectionCurrentCell = cell;
                var rect = GetDeleteRect(_blueprintSelectionStartCell, _blueprintSelectionCurrentCell);
                var selectedCount = CountStructuresInBlueprintRect(rect);
                _previewMessage = $"蓝图框选：[{rect.Position.X},{rect.Position.Y}] - [{rect.End.X - 1},{rect.End.Y - 1}]，当前覆盖 {selectedCount} 个建筑。";
                return;
            }

            if (_hasBlueprintSelectionRect)
            {
                var selectedCount = CountStructuresInBlueprintRect(_blueprintSelectionRect);
                _previewMessage = $"蓝图框选已完成：覆盖 {selectedCount} 个建筑，填写名称后保存。";
                return;
            }

            _previewMessage = "蓝图框选：左键按下并拖拽选择一片现有布局。";
            return;
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            if (_blueprintSite is null)
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

            _blueprintApplyPlan = FactoryBlueprintPlanner.CreatePlan(activeBlueprint, _blueprintSite, cell, _blueprintApplyRotation);
            _previewMessage = _blueprintApplyPlan.IsValid
                ? $"蓝图 {activeBlueprint.DisplayName} 可应用到锚点 ({cell.X}, {cell.Y})，旋转 {FactoryDirection.ToLabel(_blueprintApplyRotation)}。"
                : _blueprintApplyPlan.GetIssueSummary();
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Build && TryGetActivePlacementKind(out var placementKind, out var usesPlayerInventory))
        {
            var previewFacing = ResolveWorldPlacementFacing(placementKind, cell, _buildPlacementDragActive);
            _canPlaceCurrentCell = TryValidateWorldPlacement(placementKind, cell, previewFacing, out var placementIssue);
            _previewMessage = _canPlaceCurrentCell
                ? DescribeWorldPlacementPreview(placementKind, cell, previewFacing, usesPlayerInventory)
                : placementIssue;
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Delete)
        {
            if (_deleteDragActive)
            {
                _deleteDragCurrentCell = cell;
                var deletionCount = CountStructuresInDeleteRect(_deleteDragStartCell, _deleteDragCurrentCell);
                _canDeleteCurrentCell = deletionCount > 0;
                var rect = GetDeleteRect(_deleteDragStartCell, _deleteDragCurrentCell);
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
            ? $"交互模式：空地 ({cell.X}, {cell.Y})，点击可清除当前选中，Shift+左键可开始蓝图框选。"
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
            && (_blueprintSelectionDragActive || _hasBlueprintSelectionRect);
        var hasPlacementPreview = false;
        var previewKind = default(BuildPrototypeKind);
        var previewFacing = _selectedFacing;
        if (_interactionMode == FactoryInteractionMode.Build && TryGetActivePlacementKind(out var activePreviewKind, out _))
        {
            hasPlacementPreview = true;
            previewKind = activePreviewKind;
            if (_hasHoveredCell)
            {
                previewFacing = ResolveWorldPlacementFacing(previewKind, _hoveredCell, _buildPlacementDragActive);
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
            var rect = _blueprintSelectionDragActive
                ? GetDeleteRect(_blueprintSelectionStartCell, _blueprintSelectionCurrentCell)
                : _blueprintSelectionRect;
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
            var start = _deleteDragActive ? _deleteDragStartCell : _hoveredCell;
            var end = _deleteDragActive ? _deleteDragCurrentCell : _hoveredCell;
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
        if (_grid is null || _blueprintPreviewRoot is null)
        {
            return;
        }

        foreach (var mesh in _blueprintPreviewMeshes)
        {
            mesh.Visible = false;
        }

        foreach (var ghost in _blueprintPreviewGhosts)
        {
            ghost.Visible = false;
        }

        var plan = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview ? _blueprintApplyPlan : null;
        _blueprintPreviewRoot.Visible = plan is not null && _hasHoveredCell;
        if (_blueprintGhostPreviewRoot is not null)
        {
            _blueprintGhostPreviewRoot.Visible = _blueprintPreviewRoot.Visible;
        }
        if (!_blueprintPreviewRoot.Visible || plan is null)
        {
            if (_blueprintGhostPreviewRoot is not null)
            {
                _blueprintGhostPreviewRoot.Visible = false;
            }
            return;
        }

        EnsureBlueprintPreviewCapacity(plan.Entries.Count);
        var showGhostPreview = SupportsGhostBlueprintPreview();
        if (_blueprintGhostPreviewRoot is not null)
        {
            _blueprintGhostPreviewRoot.Visible = showGhostPreview;
        }
        for (var index = 0; index < plan.Entries.Count; index++)
        {
            var entry = plan.Entries[index];
            var mesh = _blueprintPreviewMeshes[index];
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
                structure.SetPowerRangeVisible(ShouldShowPowerRange(structure));
                structure.SyncVisualPresentation(alpha);
                structure.UpdateVisuals(alpha);
                structure.SyncCombatVisuals(alpha);
            }
        }
        _transportRenderManager?.EndFrame();

        _averageVisualSyncMilliseconds = SmoothMetric(_averageVisualSyncMilliseconds, Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds, 0.18);
    }

    private void UpdatePowerLinkVisuals()
    {
        if (_grid is null || _structureRoot is null || _powerLinkOverlayRoot is null)
        {
            return;
        }

        if (_blueprintMode != FactoryBlueprintWorkflowMode.None)
        {
            SetPowerLinkDashCount(0);
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Build
            && _selectedBuildKind == BuildPrototypeKind.PowerPole
            && _hasHoveredCell)
        {
            var previewColor = _canPlaceCurrentCell
                ? new Color(0.98f, 0.89f, 0.52f, 0.92f)
                : new Color(1.0f, 0.45f, 0.45f, 0.90f);
            RenderPowerLinkSet(
                GetPreviewPowerAnchor(_hoveredCell),
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
            RenderPowerLinkSet(
                GetPowerAnchor(selectedPole),
                selectedPole.Cell,
                selectedPole.PowerConnectionRangeCells,
                new Color(0.99f, 0.93f, 0.62f, 0.92f),
                selectedPole);
            return;
        }

        SetPowerLinkDashCount(0);
    }

    private void RenderPowerLinkSet(Vector3 origin, Vector2I originCell, int originRange, Color color, FactoryStructure? exclude = null)
    {
        FactoryPowerPreviewSupport.RenderPowerLinkSet(
            _structureRoot,
            origin,
            originCell,
            originRange,
            color,
            GetPowerAnchor,
            DrawDashedPowerLink,
            SetPowerLinkDashCount,
            exclude: exclude);
    }

    private List<FactoryStructure> CollectConnectablePowerNodes(Vector2I originCell, int originRange, FactoryStructure? exclude)
    {
        return FactoryPowerPreviewSupport.CollectConnectablePowerNodes(_structureRoot, originCell, originRange, exclude: exclude);
    }

    private int DrawDashedPowerLink(Vector3 start, Vector3 end, Color color, int dashIndex)
    {
        return FactoryPreviewOverlaySupport.DrawDashedPowerLink(
            start,
            end,
            color,
            dashIndex,
            EnsurePowerLinkDashCapacity,
            _powerLinkDashes,
            ApplyPowerLinkColor);
    }

    private void EnsurePowerLinkDashCapacity(int count)
    {
        FactoryPreviewOverlaySupport.EnsurePowerLinkDashCapacity(_powerLinkOverlayRoot, _powerLinkDashes, count, "PowerLinkDash");
    }

    private void SetPowerLinkDashCount(int visibleCount)
    {
        if (_powerLinkOverlayRoot is null)
        {
            return;
        }

        for (var i = visibleCount; i < _powerLinkDashes.Count; i++)
        {
            _powerLinkDashes[i].Visible = false;
        }

        _powerLinkOverlayRoot.Visible = visibleCount > 0;
    }

    private bool ShouldShowPowerRange(FactoryStructure structure)
    {
        return IsPowerPreviewActive()
            && structure is IFactoryPowerNode
            && GodotObject.IsInstanceValid(structure)
            && structure.IsInsideTree();
    }

    private bool IsPowerPreviewActive()
    {
        return _interactionMode == FactoryInteractionMode.Build
            ? _hasHoveredCell && (_selectedBuildKind == BuildPrototypeKind.Generator || _selectedBuildKind == BuildPrototypeKind.PowerPole)
            : _interactionMode == FactoryInteractionMode.Interact && _selectedStructure is IFactoryPowerNode;
    }

    private static void UpdatePreviewPowerRange(BuildPrototypeKind? kind, IFactorySite site, MeshInstance3D previewPowerRange, Color tint)
    {
        FactoryPowerPreviewSupport.UpdatePreviewPowerRange(kind, site, previewPowerRange, tint);
    }

    private static bool TryGetPowerPreviewInfo(BuildPrototypeKind? kind, out int rangeCells)
    {
        return FactoryPowerPreviewSupport.TryGetPowerPreviewInfo(kind, out rangeCells);
    }

    private static void ApplyPowerLinkColor(MeshInstance3D dash, Color color)
    {
        if (dash.MaterialOverride is not StandardMaterial3D material)
        {
            return;
        }

        material.AlbedoColor = color;
        material.Emission = color.Lightened(0.08f);
    }

    private static Vector3 GetPreviewPowerAnchor(Vector2I cell)
    {
        return new Vector3(
            cell.X * FactoryConstants.CellSize,
            PreviewPowerPoleWireHeight,
            cell.Y * FactoryConstants.CellSize);
    }

    private static Vector3 GetPowerAnchor(FactoryStructure structure)
    {
        var height = structure switch
        {
            PowerPoleStructure => 1.44f,
            GeneratorStructure => 1.06f,
            _ => 1.18f
        };
        return structure.GlobalPosition + new Vector3(0.0f, height, 0.0f);
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

        _hud.SetMode(_interactionMode);

        if (_selectedBuildKind.HasValue)
        {
            var definition = _definitions[_selectedBuildKind.Value];
            _hud.SetBuildSelection(_selectedBuildKind, definition.Details);
        }
        else
        {
            _hud.SetBuildSelection(null, null);
        }

        _hud.SetHoverCell(_hoveredCell, _hasHoveredCell);
        var previewPositive = _interactionMode switch
        {
            FactoryInteractionMode.Build => _canPlaceCurrentCell,
            FactoryInteractionMode.Delete => _canDeleteCurrentCell,
            _ => true
        };
        _hud.SetPreviewStatus(previewPositive, _previewMessage);
        _hud.SetRotation(_selectedFacing);
        _hud.SetSelectionTarget(GetSelectedStructureText());

        if (FactoryDemoInteractionBridge.TryGetInspection(_selectedStructure, out var inspectionTitle, out var inspectionBody))
        {
            _hud.SetInspection(inspectionTitle, inspectionBody);
        }
        else
        {
            _hud.SetInspection(null, null);
        }

        _hud.SetStructureDetails(FactoryDemoInteractionBridge.BuildLinkedDetailModel(_selectedStructure));

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
                FactoryDemoInteractionBridge.BuildLinkedDetailModel(_selectedStructure),
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
            || _blueprintApplyPlan is not null
            || _hasBlueprintSelectionRect
            || FactoryBlueprintLibrary.GetActive() is not null;
    }

    private FactoryBlueprintPanelState BuildBlueprintPanelState()
    {
        var modeText = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图模式：框选保存",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图模式：应用预览（旋转 {FactoryDirection.ToLabel(_blueprintApplyRotation)}）",
            _ => "蓝图模式：待命"
        };
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        var activeText = FactoryBlueprintWorkflowBridge.BuildActiveBlueprintText();
        var captureSummary = _pendingBlueprintCapture is null
            ? "未捕获待保存蓝图。点击“框选保存”或在交互模式按住 Shift 左键拖拽选择。"
            : $"待保存：{_pendingBlueprintCapture.DisplayName} | {_pendingBlueprintCapture.GetSummaryText()}";
        var issueText = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _blueprintApplyPlan is not null
            ? $"当前旋转：{FactoryDirection.ToLabel(_blueprintApplyRotation)} | 占地 {_blueprintApplyPlan.FootprintSize.X}x{_blueprintApplyPlan.FootprintSize.Y}\n{_blueprintApplyPlan.GetIssueSummary()}"
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
            CanConfirmApply = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _blueprintApplyPlan?.IsValid == true,
            Blueprints = FactoryBlueprintLibrary.GetAll()
        };
    }

    private string GetSelectedStructureText()
    {
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
    }

    private void HandleDeletePrimaryPress(bool shiftPressed)
    {
        if (!_hasHoveredCell)
        {
            return;
        }

        if (shiftPressed)
        {
            _deleteDragActive = true;
            _deleteDragStartCell = _hoveredCell;
            _deleteDragCurrentCell = _hoveredCell;
            return;
        }

        DeleteHoveredStructure();
    }

    private void HandleDeletePrimaryRelease()
    {
        if (!_deleteDragActive)
        {
            return;
        }

        _deleteDragActive = false;
        DeleteStructuresInRect(_deleteDragStartCell, _deleteDragCurrentCell);
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
        if (_blueprintSite is null)
        {
            return 0;
        }

        var seen = new HashSet<ulong>();
        foreach (var structure in _blueprintSite.EnumerateStructures())
        {
            foreach (var occupiedCell in structure.GetOccupiedCells())
            {
                if (!rect.HasPoint(occupiedCell))
                {
                    continue;
                }

                seen.Add(structure.GetInstanceId());
                break;
            }
        }

        return seen.Count;
    }

    private void EnsureBlueprintPreviewCapacity(int count)
    {
        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _blueprintPreviewRoot,
            _blueprintPreviewMeshes,
            count,
            index => new MeshInstance3D
            {
                Name = $"BlueprintPreview_{index}",
                Visible = false,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(FactoryConstants.CellSize * 0.84f, 0.10f, FactoryConstants.CellSize * 0.84f)
                }
            });
    }

    private FactoryStructure EnsureBlueprintGhostPreview(FactoryBlueprintPlanEntry entry, int index)
    {
        return FactoryPreviewPoolSupport.EnsureGhostPreview(
            _blueprintGhostPreviewRoot,
            _blueprintPreviewGhosts,
            index,
            entry.SourceEntry.Kind,
            kind => FactoryStructureFactory.CreateGhostPreview(
                kind,
                new FactoryStructurePlacement(_grid!, entry.TargetCell, entry.TargetFacing)),
            "BlueprintGhostPreview");
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
        if (!definition.AllowWorldPlacement)
        {
            return $"{FactoryPresentation.GetKindLabel(entry.Kind)} 只能放在移动工厂内部。";
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
        EnterInteractionMode();
        _blueprintMode = FactoryBlueprintWorkflowMode.CaptureSelection;
        _blueprintApplyPlan = null;
        _blueprintApplyRotation = FacingDirection.East;
        _pendingBlueprintCapture = null;
        _hasBlueprintSelectionRect = false;
        _blueprintSelectionDragActive = false;
    }

    private void BeginBlueprintSelection()
    {
        if (!_hasHoveredCell)
        {
            return;
        }

        _blueprintSelectionDragActive = true;
        _blueprintSelectionStartCell = _hoveredCell;
        _blueprintSelectionCurrentCell = _hoveredCell;
        _hasBlueprintSelectionRect = false;
        _pendingBlueprintCapture = null;
    }

    private void CompleteBlueprintSelection()
    {
        if (!_blueprintSelectionDragActive)
        {
            return;
        }

        _blueprintSelectionDragActive = false;
        _blueprintSelectionRect = GetDeleteRect(_blueprintSelectionStartCell, _blueprintSelectionCurrentCell);
        _hasBlueprintSelectionRect = true;

        if (_blueprintSite is null)
        {
            return;
        }

        var suggestedName = $"沙盒蓝图 {CountStructuresInBlueprintRect(_blueprintSelectionRect)} 件";
        _pendingBlueprintCapture = FactoryBlueprintCaptureService.CaptureSelection(_blueprintSite, _blueprintSelectionRect, suggestedName);
        if (_pendingBlueprintCapture is null)
        {
            _previewMessage = "框选区域内没有可保存的建筑。";
            _hasBlueprintSelectionRect = false;
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
        _hasBlueprintSelectionRect = false;
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        ShowBlueprintPersistenceStatus(savedRecord, target);
    }

    private void HandleBlueprintSelected(string blueprintId)
    {
        FactoryBlueprintWorkflowBridge.SelectBlueprint(
            blueprintId,
            _blueprintMode,
            () =>
            {
                if (_hasHoveredCell && _blueprintSite is not null && FactoryBlueprintLibrary.GetActive() is FactoryBlueprintRecord activeBlueprint)
                {
                    _blueprintApplyPlan = FactoryBlueprintPlanner.CreatePlan(activeBlueprint, _blueprintSite, _hoveredCell, _blueprintApplyRotation);
                }
                else
                {
                    _blueprintApplyPlan = null;
                }
            });
    }

    private void EnterBlueprintApplyMode()
    {
        if (FactoryBlueprintLibrary.GetActive() is null)
        {
            return;
        }

        EnterInteractionMode();
        _pendingBlueprintCapture = null;
        _hasBlueprintSelectionRect = false;
        _blueprintMode = FactoryBlueprintWorkflowMode.ApplyPreview;
        _blueprintApplyRotation = FacingDirection.East;
    }

    private void ConfirmBlueprintApply()
    {
        if (_blueprintSite is null || _blueprintApplyPlan is null)
        {
            return;
        }

        if (!FactoryBlueprintPlanner.CommitPlan(_blueprintApplyPlan, _blueprintSite))
        {
            _previewMessage = "蓝图应用失败，请检查预览中的阻塞原因。";
            return;
        }

        _selectedStructure = null;
        RefreshAllTopology();
        _previewMessage = $"已应用蓝图：{_blueprintApplyPlan.Blueprint.DisplayName}（旋转 {FactoryDirection.ToLabel(_blueprintApplyPlan.Rotation)}）";
    }

    private void HandleBlueprintDeleteRequested(string blueprintId)
    {
        FactoryBlueprintWorkflowBridge.DeleteBlueprint(blueprintId, () => _blueprintApplyPlan = null);
    }

    private void CancelBlueprintWorkflow()
    {
        CancelBlueprintWorkflow(clearActiveBlueprint: false);
    }

    private void CancelBlueprintWorkflow(bool clearActiveBlueprint)
    {
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        _blueprintSelectionDragActive = false;
        _hasBlueprintSelectionRect = false;
        _pendingBlueprintCapture = null;
        _blueprintApplyPlan = null;
        _blueprintApplyRotation = FacingDirection.East;

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

        _blueprintApplyRotation = direction < 0
            ? FactoryDirection.RotateCounterClockwise(_blueprintApplyRotation)
            : FactoryDirection.RotateClockwise(_blueprintApplyRotation);

        if (_hasHoveredCell && _blueprintSite is not null && FactoryBlueprintLibrary.GetActive() is FactoryBlueprintRecord activeBlueprint)
        {
            _blueprintApplyPlan = FactoryBlueprintPlanner.CreatePlan(activeBlueprint, _blueprintSite, _hoveredCell, _blueprintApplyRotation);
        }
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
        _selectedPlayerItemInventoryId = inventoryId;
        _selectedPlayerItemSlot = slot;
        _hasSelectedPlayerItemSlot = true;
        _playerSelectionState.InventoryId = inventoryId;
        _playerSelectionState.Slot = slot;
        _playerSelectionState.HasSlot = true;
        FactoryDemoInteractionBridge.ActivatePlayerInventorySlot(_playerController, TryResolveInventoryEndpoint, _playerSelectionState, inventoryId, slot, HandlePlayerHotbarPressed);

        if (inventoryId == FactoryPlayerController.BackpackInventoryId && slot.Y == 0)
        {
            return;
        }

        _selectedBuildKind = null;
        _playerPlacementArmed = _playerSelectionState.PlacementArmed;
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
        UpdateHud();
    }

    private bool TryResolveSelectedPlayerPlaceable(out BuildPrototypeKind kind)
    {
        var item = ResolveSelectedPlayerItem();
        return FactoryPresentation.TryGetPlaceableStructureKind(item, out kind);
    }

    private bool TryConsumeSelectedPlayerPlaceable()
    {
        _playerSelectionState.InventoryId = _selectedPlayerItemInventoryId;
        _playerSelectionState.Slot = _selectedPlayerItemSlot;
        _playerSelectionState.HasSlot = _hasSelectedPlayerItemSlot;
        _playerSelectionState.PlacementArmed = _playerPlacementArmed;
        var consumed = FactoryDemoInteractionBridge.TryConsumeSelectedPlaceable(_playerController, TryResolveInventoryEndpoint, _playerSelectionState);
        _playerPlacementArmed = _playerSelectionState.PlacementArmed;
        return consumed;
    }

    private FactoryItem? ResolveSelectedPlayerItem()
    {
        _playerSelectionState.InventoryId = _selectedPlayerItemInventoryId;
        _playerSelectionState.Slot = _selectedPlayerItemSlot;
        _playerSelectionState.HasSlot = _hasSelectedPlayerItemSlot;
        _playerSelectionState.PlacementArmed = _playerPlacementArmed;
        return FactoryDemoInteractionBridge.ResolveSelectedPlayerItem(_playerController, TryResolveInventoryEndpoint, _playerSelectionState);
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
            : BuildCellRect(_grid.MinCell, _grid.MaxCell);
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

        visibleRect = BuildCellRect(
            _grid.WorldToCell(new Vector3(projectedRect.Position.X, 0.0f, projectedRect.Position.Y)),
            _grid.WorldToCell(new Vector3(projectedRect.End.X, 0.0f, projectedRect.End.Y)),
            1);
        return true;
    }

    private static Rect2I BuildCellRect(Vector2I a, Vector2I b, int padding = 0)
    {
        var minCell = new Vector2I(
            System.Math.Min(a.X, b.X) - padding,
            System.Math.Min(a.Y, b.Y) - padding);
        var maxCell = new Vector2I(
            System.Math.Max(a.X, b.X) + padding,
            System.Math.Max(a.Y, b.Y) + padding);
        return new Rect2I(
            minCell,
            new Vector2I(maxCell.X - minCell.X + 1, maxCell.Y - minCell.Y + 1));
    }
    private void HandleBuildPrimaryPress()
    {
        _buildPlacementDragActive = true;
        _buildPlacementStrokeCells.Clear();
        TryPlaceCurrentBuildTarget(trackCurrentCellForStroke: true);
    }

    private void HandleBuildPrimaryRelease()
    {
        ResetBuildPlacementStroke();
    }

    private bool HandleBuildDragPlacement()
    {
        if (!_buildPlacementDragActive)
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

        if (trackCurrentCellForStroke && _buildPlacementStrokeCells.Contains(_hoveredCell))
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
        var previousStrokeCell = _lastBuildStrokeCell;
        var hadPreviousStrokeCell = _hasLastBuildStrokeCell;
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
            _buildPlacementStrokeCells.Add(targetCell);
            _lastBuildStrokeCell = targetCell;
            _hasLastBuildStrokeCell = true;
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
        _buildPlacementDragActive = false;
        _buildPlacementStrokeCells.Clear();
        _hasLastBuildStrokeCell = false;
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
            && _hasLastBuildStrokeCell
            && TryResolveBeltDragFacing(_lastBuildStrokeCell, cell, out var dragFacing))
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


    private static double SmoothMetric(double current, double sample, double weight)
    {
        return current <= 0.0
            ? sample
            : current + ((sample - current) * weight);
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

