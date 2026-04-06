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
    private const float PowerDashBaseLength = FactoryConstants.CellSize * 0.52f;
    private const float PowerDashGapLength = FactoryConstants.CellSize * 0.28f;
    private const float PowerDashThickness = 0.08f;
    private const float PowerDashWidth = 0.11f;
    private const float PowerLinkEndpointInset = FactoryConstants.CellSize * 0.22f;
    private const float PreviewPowerPoleWireHeight = 1.44f;
    private const int PreviewPowerPoleConnectionRangeCells = 6;

    private readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> _definitions = new()
    {
        [BuildPrototypeKind.Producer] = new BuildPrototypeDefinition(BuildPrototypeKind.Producer, "兼容生产器", new Color("9DC08B"), "兼容型占位产物流，仅用于 legacy 回归线。"),
        [BuildPrototypeKind.MiningDrill] = new BuildPrototypeDefinition(BuildPrototypeKind.MiningDrill, "采矿机", new Color("FBBF24"), "只能放在矿点上，通电后持续采出煤、铁矿石或铜矿石。"),
        [BuildPrototypeKind.Generator] = new BuildPrototypeDefinition(BuildPrototypeKind.Generator, "发电机", new Color("FB923C"), "消耗煤炭发电，为周边电网提供基础供给。"),
        [BuildPrototypeKind.PowerPole] = new BuildPrototypeDefinition(BuildPrototypeKind.PowerPole, "电线杆", new Color("FDE68A"), "延伸电网覆盖，把发电机接到更远的机器。"),
        [BuildPrototypeKind.Smelter] = new BuildPrototypeDefinition(BuildPrototypeKind.Smelter, "熔炉", new Color("CBD5E1"), "消耗电力把铁矿、铜矿或铁板冶炼成更高阶板材。"),
        [BuildPrototypeKind.Assembler] = new BuildPrototypeDefinition(BuildPrototypeKind.Assembler, "组装机", new Color("67E8F9"), "消耗板材和中间件，组装铜线、电路、机加工件与弹药。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收站", new Color("FDE68A"), "接收物品并统计送达数量。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把左右两路物流汇成前方一路。"),
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
    private FactoryPlayerController? _playerController;
    private FactoryPlayerHud? _playerHud;
    private Node3D? _structureRoot;
    private Node3D? _enemyRoot;
    private Node3D? _previewRoot;
    private Node3D? _resourceOverlayRoot;
    private Node3D? _blueprintPreviewRoot;
    private Node3D? _blueprintGhostPreviewRoot;
    private Node3D? _powerLinkOverlayRoot;
    private MeshInstance3D? _previewCell;
    private Node3D? _previewArrow;
    private MeshInstance3D? _previewPowerRange;
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
    private bool _hasHoveredCell;
    private bool _canPlaceCurrentCell;
    private bool _canDeleteCurrentCell;
    private bool _deleteDragActive;
    private Vector2I _deleteDragStartCell;
    private Vector2I _deleteDragCurrentCell;
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
    private static void TraceLog(string message) => GD.Print($"[FactoryDemo] {message}");

    public override void _Ready()
    {
        EnsureInputActions();
        BuildSceneGraph();
        ConfigureGameplay();
        CreateStarterLayout();
        SpawnPlayerController();
        UpdateHud();

        if (HasSmokeTestFlag())
        {
            CallDeferred(nameof(RunSmokeChecks));
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
        AddChild(CreateEnvironment());
        AddChild(CreateDirectionalLight());
        AddChild(CreateFloor());
        AddChild(CreateGridLines());

        _resourceOverlayRoot = new Node3D { Name = "ResourceOverlayRoot" };
        AddChild(_resourceOverlayRoot);

        _structureRoot = new Node3D { Name = "StructureRoot" };
        AddChild(_structureRoot);

        _enemyRoot = new Node3D { Name = "EnemyRoot" };
        AddChild(_enemyRoot);

        _previewRoot = new Node3D { Name = "PreviewRoot" };
        AddChild(_previewRoot);
        CreatePreviewVisuals();

        _powerLinkOverlayRoot = new Node3D { Name = "PowerLinkOverlayRoot", Visible = false };
        AddChild(_powerLinkOverlayRoot);

        _blueprintPreviewRoot = new Node3D { Name = "BlueprintPreviewRoot", Visible = false };
        AddChild(_blueprintPreviewRoot);

        _blueprintGhostPreviewRoot = new Node3D { Name = "BlueprintGhostPreviewRoot", Visible = false };
        AddChild(_blueprintGhostPreviewRoot);

        _simulation = new SimulationController { Name = "SimulationController" };
        AddChild(_simulation);

        _combatDirector = new FactoryCombatDirector { Name = "CombatDirector" };
        AddChild(_combatDirector);

        _cameraRig = new FactoryCameraRig();
        AddChild(_cameraRig);

        _hud = new FactoryHud();
        _hud.SelectionChanged += SelectBuildKind;
        _hud.DetailInventoryMoveRequested += HandleDetailInventoryMoveRequested;
        _hud.DetailInventoryTransferRequested += HandlePlayerInventoryTransferRequested;
        _hud.DetailRecipeSelected += HandleDetailRecipeSelected;
        _hud.DetailClosed += HandleDetailWindowClosed;
        _hud.WorkspaceSelected += HandleHudWorkspaceSelected;
        _hud.BlueprintCaptureRequested += StartBlueprintCapture;
        _hud.BlueprintSaveRequested += HandleBlueprintSaveRequested;
        _hud.BlueprintSelected += HandleBlueprintSelected;
        _hud.BlueprintApplyRequested += EnterBlueprintApplyMode;
        _hud.BlueprintConfirmRequested += ConfirmBlueprintApply;
        _hud.BlueprintDeleteRequested += HandleBlueprintDeleteRequested;
        _hud.BlueprintCancelRequested += CancelBlueprintWorkflow;
        AddChild(_hud);

        _playerHud = new FactoryPlayerHud();
        _playerHud.HotbarSlotPressed += HandlePlayerHotbarPressed;
        _playerHud.BackpackInventoryMoveRequested += HandlePlayerInventoryMoveRequested;
        _playerHud.BackpackInventoryTransferRequested += HandlePlayerInventoryTransferRequested;
        _playerHud.BackpackSlotActivated += HandlePlayerInventorySlotActivated;
        AddChild(_playerHud);

        AddChild(new LauncherNavigationOverlay());
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
        SeedWorldResourceDeposits();
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

    private void CreateStarterLayout()
    {
        AddPoweredBootstrapDistrict();
        AddPoweredManufacturingDistrict();
        AddMaintenanceDepotDistrict();
        AddAmmoFedDefenseLane();
        AddHeavyTurretDefenseLane();
        AddAmmoStarvedBreachLane();
        PrimeDefenseStocks();
        RefreshAllTopology();
        if (!HasSmokeTestFlag())
        {
            ConfigureCombatScenarios();
        }
    }

    private void SeedWorldResourceDeposits()
    {
        if (_grid is null)
        {
            return;
        }

        var deposits = new List<FactoryResourceDepositDefinition>
        {
            new FactoryResourceDepositDefinition(
                "coal_patch_bootstrap",
                FactoryResourceKind.Coal,
                "北西煤层",
                new Color("8B5A2B"),
                BuildRectCells(new Vector2I(-39, -31), new Vector2I(4, 4))),
            new FactoryResourceDepositDefinition(
                "iron_patch_main",
                FactoryResourceKind.IronOre,
                "北西铁矿区",
                new Color("64748B"),
                BuildRectCells(new Vector2I(-39, -23), new Vector2I(4, 4))),
            new FactoryResourceDepositDefinition(
                "copper_patch_main",
                FactoryResourceKind.CopperOre,
                "北西铜矿区",
                new Color("C2410C"),
                BuildRectCells(new Vector2I(-39, -19), new Vector2I(4, 4))),
            new FactoryResourceDepositDefinition(
                "quartz_patch_northwest",
                FactoryResourceKind.QuartzOre,
                "北西石英带",
                new Color("67E8F9"),
                BuildRectCells(new Vector2I(-39, -15), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "sulfur_patch_northwest",
                FactoryResourceKind.SulfurOre,
                "北西硫矿带",
                new Color("FDE047"),
                BuildRectCells(new Vector2I(-39, -10), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "stone_patch_northwest",
                FactoryResourceKind.StoneOre,
                "北西石矿带",
                new Color("A8A29E"),
                BuildRectCells(new Vector2I(-39, -5), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "coal_patch_maintenance",
                FactoryResourceKind.Coal,
                "东侧维护煤带",
                new Color("8B5A2B"),
                BuildRectCells(new Vector2I(5, 7), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "quartz_patch_maintenance",
                FactoryResourceKind.QuartzOre,
                "东侧维护石英带",
                new Color("67E8F9"),
                BuildRectCells(new Vector2I(5, 3), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "copper_patch_maintenance",
                FactoryResourceKind.CopperOre,
                "东侧维护铜带",
                new Color("C2410C"),
                BuildRectCells(new Vector2I(5, -1), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "stone_patch_maintenance",
                FactoryResourceKind.StoneOre,
                "东侧站点石带",
                new Color("A8A29E"),
                BuildRectCells(new Vector2I(5, -5), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "coal_patch_defense",
                FactoryResourceKind.Coal,
                "东侧防线煤带",
                new Color("8B5A2B"),
                BuildRectCells(new Vector2I(0, 23), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "iron_patch_defense",
                FactoryResourceKind.IronOre,
                "东侧防线铁带",
                new Color("64748B"),
                BuildRectCells(new Vector2I(0, 19), new Vector2I(4, 3))),
            new FactoryResourceDepositDefinition(
                "copper_patch_defense",
                FactoryResourceKind.CopperOre,
                "东侧防线铜带",
                new Color("C2410C"),
                BuildRectCells(new Vector2I(0, 15), new Vector2I(4, 3)))
        };

        _grid.SetResourceDeposits(deposits);
    }

    private void RebuildResourceOverlayVisuals()
    {
        if (_resourceOverlayRoot is null || _grid is null)
        {
            return;
        }

        foreach (var child in _resourceOverlayRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        var deposits = _grid.GetResourceDeposits();
        for (var depositIndex = 0; depositIndex < deposits.Count; depositIndex++)
        {
            var deposit = deposits[depositIndex];
            for (var cellIndex = 0; cellIndex < deposit.Cells.Count; cellIndex++)
            {
                var cell = deposit.Cells[cellIndex];
                var mesh = new MeshInstance3D
                {
                    Name = $"Resource_{deposit.Id}_{cell.X}_{cell.Y}",
                    Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.88f, 0.06f, FactoryConstants.CellSize * 0.88f) },
                    Position = _grid.CellToWorld(cell) + new Vector3(0.0f, 0.03f, 0.0f),
                    MaterialOverride = new StandardMaterial3D
                    {
                        AlbedoColor = deposit.Tint,
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        Roughness = 1.0f
                    }
                };
                _resourceOverlayRoot.AddChild(mesh);

                var chip = new MeshInstance3D
                {
                    Name = $"ResourceChip_{deposit.Id}_{cell.X}_{cell.Y}",
                    Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.20f, 0.14f, FactoryConstants.CellSize * 0.20f) },
                    Position = _grid.CellToWorld(cell) + new Vector3(0.0f, 0.10f, 0.0f),
                    MaterialOverride = new StandardMaterial3D
                    {
                        AlbedoColor = deposit.Tint.Lightened(0.18f),
                        Roughness = 0.85f
                    }
                };
                _resourceOverlayRoot.AddChild(chip);
            }
        }
    }

    private void AddPoweredBootstrapDistrict()
    {
        PlaceStructure(BuildPrototypeKind.MiningDrill, -36, -30, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-35, -30), FacingDirection.East, 4);
        var generator = PlaceStructure(BuildPrototypeKind.Generator, -31, -30, FacingDirection.East) as GeneratorStructure;
        var reserveGenerator = PlaceStructure(BuildPrototypeKind.Generator, -31, -28, FacingDirection.East) as GeneratorStructure;
        if (generator is not null && _simulation is not null)
        {
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
            reserveGenerator?.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), reserveGenerator.Cell + Vector2I.Left, _simulation);
            reserveGenerator?.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), reserveGenerator.Cell + Vector2I.Left, _simulation);
        }

        PlaceStructure(BuildPrototypeKind.PowerPole, -34, -27, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -31, -24, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -28, -21, FacingDirection.East);
    }

    private void AddPoweredManufacturingDistrict()
    {
        PlaceStructure(BuildPrototypeKind.MiningDrill, -36, -22, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-35, -22), FacingDirection.East, 5);
        var ironSmelter = PlaceStructure(BuildPrototypeKind.Smelter, -30, -22, FacingDirection.East) as SmelterStructure;
        ironSmelter?.TrySetDetailRecipe("iron-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, -29, -22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -28, -22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -27, -22, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, -27, -21, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.MiningDrill, -36, -18, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-35, -18), FacingDirection.East, 5);
        var copperSmelter = PlaceStructure(BuildPrototypeKind.Smelter, -30, -18, FacingDirection.East) as SmelterStructure;
        copperSmelter?.TrySetDetailRecipe("copper-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, -29, -18, FacingDirection.East);
        var wireAssembler = PlaceStructure(BuildPrototypeKind.Assembler, -28, -18, FacingDirection.East) as AssemblerStructure;
        wireAssembler?.TrySetDetailRecipe("copper-wire");
        PlaceStructure(BuildPrototypeKind.Belt, -27, -18, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Belt, -27, -19, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Merger, -27, -20, FacingDirection.East);

        var ammoAssembler = PlaceStructure(BuildPrototypeKind.AmmoAssembler, -26, -20, FacingDirection.East) as AmmoAssemblerStructure;
        ammoAssembler?.TrySetDetailRecipe("standard-ammo");
        PlaceStructure(BuildPrototypeKind.Belt, -25, -20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -24, -20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, -23, -20, FacingDirection.East);
        var supportGenerator = PlaceStructure(BuildPrototypeKind.Generator, -31, -16, FacingDirection.East) as GeneratorStructure;
        if (supportGenerator is not null && _simulation is not null)
        {
            supportGenerator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), supportGenerator.Cell + Vector2I.Left, _simulation);
            supportGenerator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), supportGenerator.Cell + Vector2I.Left, _simulation);
        }

        PlaceStructure(BuildPrototypeKind.MiningDrill, -36, -14, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-35, -14), FacingDirection.East, 5);
        var glassSmelter = PlaceStructure(BuildPrototypeKind.Smelter, -30, -14, FacingDirection.East) as SmelterStructure;
        glassSmelter?.TrySetDetailRecipe("glass-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, -29, -14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, -28, -14, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.MiningDrill, -36, -10, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-35, -10), FacingDirection.East, 5);
        var sulfurSmelter = PlaceStructure(BuildPrototypeKind.Smelter, -30, -10, FacingDirection.East) as SmelterStructure;
        sulfurSmelter?.TrySetDetailRecipe("sulfur-crystal");
        PlaceStructure(BuildPrototypeKind.Belt, -29, -10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, -28, -10, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.MiningDrill, -36, -6, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-35, -6), FacingDirection.East, 5);
        var stoneSmelter = PlaceStructure(BuildPrototypeKind.Smelter, -30, -6, FacingDirection.East) as SmelterStructure;
        stoneSmelter?.TrySetDetailRecipe("stone-brick");
        PlaceStructure(BuildPrototypeKind.Belt, -29, -6, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, -28, -6, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.PowerPole, -24, -20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -35, -21, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -31, -17, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -31, -13, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -31, -9, FacingDirection.East);
    }

    private void AddMaintenanceDepotDistrict()
    {
        PlaceStructure(BuildPrototypeKind.MiningDrill, 8, 8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 8, FacingDirection.East);
        var generator = PlaceStructure(BuildPrototypeKind.Generator, 10, 8, FacingDirection.East) as GeneratorStructure;
        if (generator is not null && _simulation is not null)
        {
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
        }

        PlaceStructure(BuildPrototypeKind.PowerPole, 12, 6, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, 12, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, 14, -2, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.MiningDrill, 8, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 4, FacingDirection.East);
        var glassSmelter = PlaceStructure(BuildPrototypeKind.Smelter, 10, 4, FacingDirection.East) as SmelterStructure;
        glassSmelter?.TrySetDetailRecipe("glass-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, 11, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 12, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 13, 4, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Belt, 13, 3, FacingDirection.North);

        PlaceStructure(BuildPrototypeKind.MiningDrill, 8, 0, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 0, FacingDirection.East);
        var copperSmelter = PlaceStructure(BuildPrototypeKind.Smelter, 10, 0, FacingDirection.East) as SmelterStructure;
        copperSmelter?.TrySetDetailRecipe("copper-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, 11, 0, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 12, 0, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 13, 0, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 13, 1, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Merger, 13, 2, FacingDirection.East);
        var batteryAssembler = PlaceStructure(BuildPrototypeKind.Assembler, 14, 2, FacingDirection.East) as AssemblerStructure;
        batteryAssembler?.TrySetDetailRecipe("battery-pack");
        PlaceStructure(BuildPrototypeKind.Belt, 15, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 16, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 17, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 18, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.LargeStorageDepot, 19, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 21, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 22, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 23, 2, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.MiningDrill, 8, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 9, -4, FacingDirection.East);
        var stoneSmelter = PlaceStructure(BuildPrototypeKind.Smelter, 10, -4, FacingDirection.East) as SmelterStructure;
        stoneSmelter?.TrySetDetailRecipe("stone-brick");
        PlaceStructure(BuildPrototypeKind.Belt, 11, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 12, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 13, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 14, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.LargeStorageDepot, 15, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 17, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 18, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 19, -4, FacingDirection.East);
    }

    private static Vector2I[] BuildRectCells(Vector2I origin, Vector2I size)
    {
        var cells = new Vector2I[size.X * size.Y];
        var index = 0;
        for (var y = 0; y < size.Y; y++)
        {
            for (var x = 0; x < size.X; x++)
            {
                cells[index++] = origin + new Vector2I(x, y);
            }
        }

        return cells;
    }

    private void AddSouthThroughputCorridor()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -8, -7, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-7, -7), FacingDirection.East, 13);
        PlaceStructure(BuildPrototypeKind.Sink, 6, -7, FacingDirection.East);
    }

    private void AddWestSplitterFanout()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -8, -2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -7, -2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, -6, -2, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-6, -3), FacingDirection.East, 3);
        PlaceStructure(BuildPrototypeKind.Sink, -3, -3, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-6, -1), FacingDirection.East, 3);
        PlaceStructure(BuildPrototypeKind.Sink, -3, -1, FacingDirection.East);
    }

    private void AddCentralBridgeCrossing()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -2, 0, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-1, 0), FacingDirection.East, 3);
        PlaceStructure(BuildPrototypeKind.Bridge, 2, 0, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 3, 0, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 4, 0, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, 2, -4, FacingDirection.South);
        PlaceBeltRun(new Vector2I(2, -3), FacingDirection.South, 3);
        PlaceBeltRun(new Vector2I(2, 1), FacingDirection.South, 2);
        PlaceStructure(BuildPrototypeKind.Sink, 2, 3, FacingDirection.South);
    }

    private void AddUpperSplitMergeLoop()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -4, 5, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -3, 5, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, -2, 5, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-2, 4), FacingDirection.East, 3);
        PlaceStructure(BuildPrototypeKind.Belt, 1, 4, FacingDirection.South);
        PlaceBeltRun(new Vector2I(-2, 6), FacingDirection.East, 3);
        PlaceStructure(BuildPrototypeKind.Belt, 1, 6, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Merger, 1, 5, FacingDirection.East);
        PlaceBeltRun(new Vector2I(2, 5), FacingDirection.East, 2);
        PlaceStructure(BuildPrototypeKind.Sink, 4, 5, FacingDirection.East);
    }

    private void AddRelayLoaderUnloaderChain()
    {
        PlaceStructure(BuildPrototypeKind.Producer, 4, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Unloader, 5, -4, FacingDirection.East);
        PlaceBeltRun(new Vector2I(6, -4), FacingDirection.South, 3);
        PlaceStructure(BuildPrototypeKind.Loader, 6, -1, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Sink, 6, 0, FacingDirection.South);
    }

    private void AddStorageOutputCorridor()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -8, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -7, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, -6, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -5, 2, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-4, 2), FacingDirection.East, 3);
        PlaceStructure(BuildPrototypeKind.Sink, -1, 2, FacingDirection.East);
    }

    private void AddBeltToStorageTransferLine()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -8, 1, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-7, 1), FacingDirection.East, 3);
        PlaceStructure(BuildPrototypeKind.Inserter, -4, 1, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, -3, 1, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-2, 1), FacingDirection.East, 2);
        PlaceStructure(BuildPrototypeKind.Sink, 0, 1, FacingDirection.East);
    }

    private void AddInserterYard()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -8, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -7, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -6, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, -5, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -4, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -3, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Merger, -2, 7, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, -5, 8, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Storage, -5, 6, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -4, 6, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -3, 6, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Belt, -2, 6, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -1, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 0, 7, FacingDirection.East);
    }

    private void AddNorthWarehouseBus()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -5, 11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -4, 11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, -3, 11, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Belt, -3, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -2, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, -1, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 0, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 1, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 2, 10, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Belt, -3, 12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -2, 12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, -1, 12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 0, 12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 1, 12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 2, 12, FacingDirection.North);

        PlaceStructure(BuildPrototypeKind.Merger, 2, 11, FacingDirection.East);
        PlaceBeltRun(new Vector2I(3, 11), FacingDirection.East, 2);
        PlaceStructure(BuildPrototypeKind.Sink, 5, 11, FacingDirection.East);
    }

    private void AddEastBridgeDepot()
    {
        PlaceStructure(BuildPrototypeKind.Producer, 7, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 8, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 9, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 10, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Bridge, 11, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 12, -4, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, 11, -6, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 11, -5, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 11, -3, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Sink, 11, -2, FacingDirection.South);
    }

    private void AddSouthCrossDock()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -4, -10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -3, -10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Bridge, -2, -10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -1, -10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, 0, -10, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, -2, -12, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, -2, -11, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Storage, -2, -9, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Belt, 0, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 1, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 2, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 3, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 4, -11, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Belt, 0, -9, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 1, -9, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 2, -9, FacingDirection.East);
    }

    private void AddSharedPickupTestYard()
    {
        PlaceStructure(BuildPrototypeKind.Producer, 6, 10, FacingDirection.East);
        PlaceBeltRun(new Vector2I(7, 10), FacingDirection.East, 3);

        PlaceStructure(BuildPrototypeKind.Inserter, 9, 9, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Storage, 9, 8, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Inserter, 9, 11, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Storage, 9, 12, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Producer, 10, 8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 11, 8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 12, 8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 12, 7, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Sink, 12, 6, FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Inserter, 12, 9, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Sink, 12, 10, FacingDirection.South);
    }

    private void AddSharedDropoffTestYard()
    {
        PlaceStructure(BuildPrototypeKind.Producer, -12, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -11, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -10, 10, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, -9, 7, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, -9, 8, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Inserter, -9, 9, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Storage, -9, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -8, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -7, 10, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, -6, 10, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, -12, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, -11, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -10, 4, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, -9, 2, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Inserter, -9, 3, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, -9, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -8, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, -7, 4, FacingDirection.East);
    }

    private void AddAmmoFedDefenseLane()
    {
        PlaceStructure(BuildPrototypeKind.MiningDrill, 3, 24, FacingDirection.East);
        PlaceBeltRun(new Vector2I(4, 24), FacingDirection.East, 2);
        var generator = PlaceStructure(BuildPrototypeKind.Generator, 6, 24, FacingDirection.East) as GeneratorStructure;
        if (generator is not null && _simulation is not null)
        {
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
        }

        PlaceStructure(BuildPrototypeKind.PowerPole, 8, 22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, 10, 19, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, 8, 16, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.MiningDrill, 3, 20, FacingDirection.East);
        PlaceBeltRun(new Vector2I(4, 20), FacingDirection.East, 2);
        var ironSmelter = PlaceStructure(BuildPrototypeKind.Smelter, 6, 20, FacingDirection.East) as SmelterStructure;
        ironSmelter?.TrySetDetailRecipe("iron-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, 7, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 8, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 20, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.MiningDrill, 3, 16, FacingDirection.East);
        PlaceBeltRun(new Vector2I(4, 16), FacingDirection.East, 2);
        var copperSmelter = PlaceStructure(BuildPrototypeKind.Smelter, 6, 16, FacingDirection.East) as SmelterStructure;
        copperSmelter?.TrySetDetailRecipe("copper-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, 7, 16, FacingDirection.East);
        var wireAssembler = PlaceStructure(BuildPrototypeKind.Assembler, 8, 16, FacingDirection.East) as AssemblerStructure;
        wireAssembler?.TrySetDetailRecipe("copper-wire");
        PlaceStructure(BuildPrototypeKind.Belt, 9, 16, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 17, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 18, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 19, FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Merger, 9, 20, FacingDirection.East);
        var ammoAssembler = PlaceStructure(BuildPrototypeKind.AmmoAssembler, 10, 20, FacingDirection.East) as AmmoAssemblerStructure;
        ammoAssembler?.TrySetDetailRecipe("standard-ammo");
        PlaceStructure(BuildPrototypeKind.Belt, 11, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 12, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 13, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.GunTurret, 14, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Wall, 15, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Wall, 16, 20, FacingDirection.East);
    }

    private void AddHeavyTurretDefenseLane()
    {
        PlaceStructure(BuildPrototypeKind.Wall, -18, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Wall, -17, 20, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.HeavyGunTurret, -16, 19, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Inserter, -14, 19, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Belt, -13, 19, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.AmmoAssembler, -12, 19, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.LargeStorageDepot, -10, 19, FacingDirection.West);
    }

    private void AddAmmoStarvedBreachLane()
    {
        PlaceStructure(BuildPrototypeKind.MiningDrill, 3, 10, FacingDirection.East);
        PlaceBeltRun(new Vector2I(4, 10), FacingDirection.East, 2);
        var generator = PlaceStructure(BuildPrototypeKind.Generator, 6, 10, FacingDirection.East) as GeneratorStructure;
        if (generator is not null && _simulation is not null)
        {
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
        }

        PlaceStructure(BuildPrototypeKind.PowerPole, 6, 12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, 8, 14, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.MiningDrill, 3, 14, FacingDirection.East);
        PlaceBeltRun(new Vector2I(4, 14), FacingDirection.East, 2);
        var ironSmelter = PlaceStructure(BuildPrototypeKind.Smelter, 6, 14, FacingDirection.East) as SmelterStructure;
        ironSmelter?.TrySetDetailRecipe("iron-smelting");
        PlaceStructure(BuildPrototypeKind.Belt, 7, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 8, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 14, FacingDirection.East);

        var ammoAssembler = PlaceStructure(BuildPrototypeKind.AmmoAssembler, 10, 14, FacingDirection.East) as AmmoAssemblerStructure;
        ammoAssembler?.TrySetDetailRecipe("standard-ammo");
        PlaceStructure(BuildPrototypeKind.Belt, 11, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 12, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 13, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.GunTurret, 14, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Wall, 15, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Wall, 16, 14, FacingDirection.East);
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
            BuildLanePath(new Vector2I(18, 20), new Vector2I(17, 20), new Vector2I(16, 20), new Vector2I(14, 20)),
            new FactoryEnemySpawnRule[]
            {
                new("melee", 4.2f),
                new("melee", 4.8f),
                new("melee", 4.4f)
            }));
        _combatDirector.AddLane(new FactoryEnemyLaneDefinition(
            "starved_lane",
            BuildLanePath(new Vector2I(18, 14), new Vector2I(17, 14), new Vector2I(16, 14), new Vector2I(14, 14)),
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

    private void PrimeDefenseStocks()
    {
        if (_grid is null || _simulation is null)
        {
            return;
        }

        PrimeTurretAmmo(new Vector2I(14, 20), 8, FactoryItemKind.AmmoMagazine);
        PrimeTurretAmmo(new Vector2I(14, 14), 2, FactoryItemKind.AmmoMagazine);
        PrimeTurretAmmo(new Vector2I(-16, 19), 10, FactoryItemKind.HighVelocityAmmo);
        PrimeAmmoAssemblerRecipe(new Vector2I(-12, 19), "high-velocity-ammo");
    }

    private void PrimeTurretAmmo(Vector2I cell, int count, FactoryItemKind itemKind)
    {
        if (_grid is null || _simulation is null)
        {
            return;
        }

        if (!_grid.TryGetStructure(cell, out var structure) || structure is not IFactoryItemReceiver receiver)
        {
            return;
        }

        for (var index = 0; index < count; index++)
        {
            var sourceCell = structure.GetInputCell();
            var item = _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, itemKind);
            if (!receiver.TryReceiveProvidedItem(item, sourceCell, _simulation))
            {
                break;
            }
        }
    }

    private void PrimeAmmoAssemblerRecipe(Vector2I cell, string recipeId)
    {
        if (_grid is null || !_grid.TryGetStructure(cell, out var structure) || structure is not AmmoAssemblerStructure ammoAssembler)
        {
            return;
        }

        ammoAssembler.TrySetDetailRecipe(recipeId);
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

    private void PlaceBeltRun(Vector2I startCell, FacingDirection facing, int length)
    {
        var offset = FactoryDirection.ToCellOffset(facing);
        for (var index = 0; index < length; index++)
        {
            PlaceStructure(BuildPrototypeKind.Belt, startCell + (offset * index), facing);
        }
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
            _canPlaceCurrentCell = TryValidateWorldPlacement(placementKind, cell, _selectedFacing, out var placementIssue);
            _previewMessage = _canPlaceCurrentCell
                ? usesPlayerInventory
                    ? $"可在 ({cell.X}, {cell.Y}) 放置{FactoryPresentation.GetBuildPrototypeDisplayName(placementKind)}，朝向 {FactoryDirection.ToLabel(_selectedFacing)}"
                    : $"可在 ({cell.X}, {cell.Y}) 放置{_definitions[placementKind].DisplayName}，朝向 {FactoryDirection.ToLabel(_selectedFacing)}"
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

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            _previewRoot.Visible = false;
            return;
        }

        var showCapturePreview = _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection
            && (_blueprintSelectionDragActive || _hasBlueprintSelectionRect);
        var hasPlacementPreview = false;
        var previewKind = default(BuildPrototypeKind);
        if (_interactionMode == FactoryInteractionMode.Build && TryGetActivePlacementKind(out var activePreviewKind, out _))
        {
            hasPlacementPreview = true;
            previewKind = activePreviewKind;
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
            ApplyPreviewColor(_previewCell, new Color(0.35f, 0.75f, 1.0f, 0.34f));
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
            ApplyPreviewColor(_previewCell, deleteTint);
            return;
        }

        _previewRoot.Position = FactoryPlacement.GetPreviewCenter(_grid, previewKind, _hoveredCell, _selectedFacing);
        _previewRoot.Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(_selectedFacing), 0.0f);
        var previewSize = FactoryPlacement.GetPreviewSize(_grid, previewKind, _selectedFacing);
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
        ApplyPreviewColor(_previewCell, tint);
        ApplyPreviewColor(_previewArrow, tint.Lightened(0.1f));
        UpdatePreviewPowerRange(previewKind, _grid, _previewPowerRange, tint);
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
            mesh.Visible = true;
            mesh.Position = _grid.CellToWorld(entry.TargetCell) + new Vector3(0.0f, 0.06f, 0.0f);
            ApplyPreviewColor(mesh, entry.IsValid
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

                ghost.ApplyGhostVisual(entry.IsValid
                    ? new Color(0.54f, 0.84f, 1.0f, 0.58f)
                    : new Color(1.0f, 0.52f, 0.52f, 0.54f));
            }
        }
    }

    private void UpdateStructureVisuals()
    {
        if (_simulation is null || _structureRoot is null)
        {
            return;
        }

        var startTicks = Stopwatch.GetTimestamp();
        var alpha = _simulation.TickAlpha;
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
        var targets = CollectConnectablePowerNodes(originCell, originRange, exclude);
        if (targets.Count == 0)
        {
            SetPowerLinkDashCount(0);
            return;
        }

        var dashIndex = 0;
        for (var i = 0; i < targets.Count; i++)
        {
            dashIndex = DrawDashedPowerLink(origin, GetPowerAnchor(targets[i]), color, dashIndex);
        }

        SetPowerLinkDashCount(dashIndex);
    }

    private List<FactoryStructure> CollectConnectablePowerNodes(Vector2I originCell, int originRange, FactoryStructure? exclude)
    {
        var candidates = new List<(FactoryStructure structure, float distance)>();
        var origin = new Vector2(originCell.X, originCell.Y);
        foreach (var child in _structureRoot!.GetChildren())
        {
            if (child is not FactoryStructure structure
                || structure == exclude
                || structure.IsDestroyed
                || !structure.Site.IsSimulationActive
                || structure is not IFactoryPowerNode powerNode
                || powerNode.PowerConnectionRangeCells <= 0
                || structure.Cell == originCell)
            {
                continue;
            }

            var target = new Vector2(structure.Cell.X, structure.Cell.Y);
            var distance = origin.DistanceTo(target);
            if (distance > originRange + powerNode.PowerConnectionRangeCells)
            {
                continue;
            }

            candidates.Add((structure, distance));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        var ordered = new List<FactoryStructure>(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            ordered.Add(candidates[i].structure);
        }

        return ordered;
    }

    private int DrawDashedPowerLink(Vector3 start, Vector3 end, Color color, int dashIndex)
    {
        var startFlat = new Vector3(start.X, 0.0f, start.Z);
        var endFlat = new Vector3(end.X, 0.0f, end.Z);
        var delta = endFlat - startFlat;
        var totalLength = delta.Length();
        if (totalLength <= PowerLinkEndpointInset * 2.0f)
        {
            return dashIndex;
        }

        var direction = delta / totalLength;
        var linkHeight = Mathf.Max(start.Y, end.Y);
        var dashStart = new Vector3(start.X, linkHeight, start.Z) + (direction * PowerLinkEndpointInset);
        var dashEnd = new Vector3(end.X, linkHeight, end.Z) - (direction * PowerLinkEndpointInset);
        var dashVector = dashEnd - dashStart;
        var dashDistance = dashVector.Length();
        if (dashDistance <= 0.05f)
        {
            return dashIndex;
        }

        var rotationY = Mathf.Atan2(direction.X, direction.Z);
        var step = PowerDashBaseLength + PowerDashGapLength;
        var progress = 0.0f;
        while (progress < dashDistance)
        {
            var dashLength = Mathf.Min(PowerDashBaseLength, dashDistance - progress);
            if (dashLength <= 0.02f)
            {
                break;
            }

            EnsurePowerLinkDashCapacity(dashIndex + 1);
            var dash = _powerLinkDashes[dashIndex];
            dash.Visible = true;
            dash.Position = dashStart + (direction * (progress + (dashLength * 0.5f)));
            dash.Rotation = new Vector3(0.0f, rotationY, 0.0f);
            dash.Scale = new Vector3(1.0f, 1.0f, dashLength / PowerDashBaseLength);
            ApplyPowerLinkColor(dash, color);
            dashIndex++;
            progress += step;
        }

        return dashIndex;
    }

    private void EnsurePowerLinkDashCapacity(int count)
    {
        if (_powerLinkOverlayRoot is null)
        {
            return;
        }

        while (_powerLinkDashes.Count < count)
        {
            var dash = new MeshInstance3D
            {
                Name = $"PowerLinkDash_{_powerLinkDashes.Count}",
                Visible = false,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(PowerDashWidth, PowerDashThickness, PowerDashBaseLength)
                },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.99f, 0.93f, 0.62f, 0.92f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Roughness = 0.15f,
                    EmissionEnabled = true,
                    Emission = new Color(0.99f, 0.93f, 0.62f)
                }
            };
            _powerLinkOverlayRoot.AddChild(dash);
            _powerLinkDashes.Add(dash);
        }
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
        if (!TryGetPowerPreviewInfo(kind, out var rangeCells))
        {
            previewPowerRange.Visible = false;
            return;
        }

        previewPowerRange.Mesh = new CylinderMesh
        {
            TopRadius = site.CellSize * rangeCells,
            BottomRadius = site.CellSize * rangeCells,
            Height = 0.03f
        };
        previewPowerRange.Position = new Vector3(0.0f, 0.02f, 0.0f);
        previewPowerRange.Visible = true;
        ApplyPreviewColor(previewPowerRange, new Color(tint.R, tint.G, tint.B, 0.15f));
    }

    private static bool TryGetPowerPreviewInfo(BuildPrototypeKind? kind, out int rangeCells)
    {
        switch (kind)
        {
            case BuildPrototypeKind.Generator:
                rangeCells = 5;
                return true;
            case BuildPrototypeKind.PowerPole:
                rangeCells = PreviewPowerPoleConnectionRangeCells;
                return true;
            default:
                rangeCells = 0;
                return false;
        }
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

        if (_selectedStructure is not null && GodotObject.IsInstanceValid(_selectedStructure) && _selectedStructure.IsInsideTree() && _selectedStructure is IFactoryInspectable inspectable)
        {
            _hud.SetInspection(inspectable.InspectionTitle, string.Join("\n", inspectable.GetInspectionLines()));
        }
        else
        {
            _hud.SetInspection(null, null);
        }

        if (_selectedStructure is not null && GodotObject.IsInstanceValid(_selectedStructure) && _selectedStructure.IsInsideTree() && _selectedStructure is IFactoryStructureDetailProvider detailProvider)
        {
            _hud.SetStructureDetails(detailProvider.GetDetailModel());
        }
        else
        {
            _hud.SetStructureDetails(null);
        }

        var sinkStats = CollectSinkStats();
        _hud.SetSinkStats(sinkStats.deliveredTotal, sinkStats.deliveredRate, sinkStats.sinkCount);
        _hud.SetProfilerStats(
            (int)Engine.GetFramesPerSecond(),
            _averageFrameMilliseconds,
            _simulation?.RegisteredStructureCount ?? 0,
            _simulation?.ActiveTransportItemCount ?? 0,
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
                BuildSelectedStructureLinkedDetailModel(),
                ResolveSelectedPlayerItem());
        }
    }

    private void HandleHudWorkspaceSelected(string workspaceId)
    {
        if (workspaceId != BlueprintWorkspaceId && HasActiveBlueprintWorkspaceState())
        {
            CancelBlueprintWorkflow(clearActiveBlueprint: true);
            _previewMessage = "已切换离开蓝图工作区，并清除当前蓝图选择。";
            return;
        }

        if (workspaceId == BlueprintWorkspaceId
            && !HasActiveBlueprintWorkspaceState())
        {
            _previewMessage = "蓝图工作区已打开：按住 Shift 左键拖拽框选保存，或先在库里准备一个蓝图。";
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
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        var modeText = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图模式：框选保存",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图模式：应用预览（旋转 {FactoryDirection.ToLabel(_blueprintApplyRotation)}）",
            _ => "蓝图模式：待命"
        };
        var activeText = activeBlueprint is null
            ? "当前蓝图：未选择"
            : $"当前蓝图：{activeBlueprint.DisplayName} ({activeBlueprint.GetSummaryText()})";
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
            AllowSelectionCapture = true,
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

        if (_interactionMode == FactoryInteractionMode.Build && TryGetActivePlacementKind(out var placementKind, out var usesPlayerInventory))
        {
            TraceLog($"HandlePrimaryClick build mode cell={_hoveredCell} kind={placementKind} usesPlayerInventory={usesPlayerInventory} canPlace={_canPlaceCurrentCell} selectedInventory={_selectedPlayerItemInventoryId ?? "none"} selectedSlot={_selectedPlayerItemSlot}");
            if (_canPlaceCurrentCell)
            {
                var placedStructure = PlaceStructure(placementKind, _hoveredCell, _selectedFacing);
                TraceLog($"HandlePrimaryClick placement result placed={(placedStructure is not null)}");
                if (placedStructure is not null && usesPlayerInventory)
                {
                    var consumed = TryConsumeSelectedPlayerPlaceable();
                    TraceLog($"HandlePrimaryClick consumed player placeable={consumed}");
                    RefreshInteractionModeFromBuildSource();
                }
            }
            else
            {
                TraceLog($"HandlePrimaryClick placement blocked previewMessage={_previewMessage}");
            }

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
        var minX = Mathf.Min(start.X, end.X);
        var minY = Mathf.Min(start.Y, end.Y);
        var maxX = Mathf.Max(start.X, end.X);
        var maxY = Mathf.Max(start.Y, end.Y);
        return new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private int CountStructuresInDeleteRect(Vector2I start, Vector2I end)
    {
        if (_grid is null)
        {
            return 0;
        }

        var rect = GetDeleteRect(start, end);
        var seen = new HashSet<ulong>();
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                if (_grid.TryGetStructure(new Vector2I(x, y), out var structure) && structure is not null)
                {
                    seen.Add(structure.GetInstanceId());
                }
            }
        }

        return seen.Count;
    }

    private void DeleteStructuresInRect(Vector2I start, Vector2I end)
    {
        if (_grid is null)
        {
            return;
        }

        var rect = GetDeleteRect(start, end);
        var cellsToDelete = new List<Vector2I>();
        var seen = new HashSet<ulong>();
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                var cell = new Vector2I(x, y);
                if (_grid.TryGetStructure(cell, out var structure) && structure is not null && seen.Add(structure.GetInstanceId()))
                {
                    cellsToDelete.Add(structure.Cell);
                }
            }
        }

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
        if (_blueprintPreviewRoot is null)
        {
            return;
        }

        while (_blueprintPreviewMeshes.Count < count)
        {
            var mesh = new MeshInstance3D
            {
                Name = $"BlueprintPreview_{_blueprintPreviewMeshes.Count}",
                Visible = false,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(FactoryConstants.CellSize * 0.84f, 0.10f, FactoryConstants.CellSize * 0.84f)
                }
            };
            _blueprintPreviewRoot.AddChild(mesh);
            _blueprintPreviewMeshes.Add(mesh);
        }
    }

    private FactoryStructure EnsureBlueprintGhostPreview(FactoryBlueprintPlanEntry entry, int index)
    {
        if (_blueprintGhostPreviewRoot is null)
        {
            throw new System.InvalidOperationException("Blueprint ghost preview root is missing.");
        }

        if (index < _blueprintPreviewGhosts.Count && _blueprintPreviewGhosts[index].Kind == entry.SourceEntry.Kind)
        {
            return _blueprintPreviewGhosts[index];
        }

        if (index < _blueprintPreviewGhosts.Count)
        {
            _blueprintPreviewGhosts[index].QueueFree();
            _blueprintPreviewGhosts.RemoveAt(index);
        }

        var ghost = FactoryStructureFactory.CreateGhostPreview(
            entry.SourceEntry.Kind,
            new FactoryStructurePlacement(_grid!, entry.TargetCell, entry.TargetFacing));
        ghost.Name = $"BlueprintGhostPreview_{index}_{entry.SourceEntry.Kind}";
        _blueprintGhostPreviewRoot.AddChild(ghost);
        _blueprintPreviewGhosts.Insert(index, ghost);
        return ghost;
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

    private void HandleBlueprintSaveRequested(string name)
    {
        if (_pendingBlueprintCapture is null)
        {
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(name)
            ? _pendingBlueprintCapture.DisplayName
            : name.Trim();
        var savedRecord = new FactoryBlueprintRecord(
            _pendingBlueprintCapture.Id,
            displayName,
            _pendingBlueprintCapture.SourceSiteKind,
            _pendingBlueprintCapture.SuggestedAnchorCell,
            _pendingBlueprintCapture.BoundsSize,
            _pendingBlueprintCapture.Entries,
            _pendingBlueprintCapture.RequiredAttachments);
        FactoryBlueprintLibrary.AddOrUpdate(savedRecord);
        FactoryBlueprintLibrary.SelectActive(savedRecord.Id);
        _pendingBlueprintCapture = null;
        _hasBlueprintSelectionRect = false;
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        _previewMessage = $"已保存蓝图：{savedRecord.DisplayName}";
    }

    private void HandleBlueprintSelected(string blueprintId)
    {
        FactoryBlueprintLibrary.SelectActive(blueprintId);
        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _hasHoveredCell && _blueprintSite is not null)
        {
            var activeBlueprint = FactoryBlueprintLibrary.GetActive();
            _blueprintApplyPlan = activeBlueprint is null
                ? null
                : FactoryBlueprintPlanner.CreatePlan(activeBlueprint, _blueprintSite, _hoveredCell, _blueprintApplyRotation);
        }
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
        FactoryBlueprintLibrary.Remove(blueprintId);
        if (FactoryBlueprintLibrary.GetActive() is null)
        {
            _blueprintApplyPlan = null;
        }
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
        if (_selectedStructure is IFactoryStructureDetailProvider detailProvider && detailProvider.TryMoveDetailInventoryItem(inventoryId, fromSlot, toSlot, splitStack))
        {
            UpdateHud();
        }
    }

    private void HandlePlayerInventoryMoveRequested(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack)
    {
        if (!TryResolveInventoryEndpoint(inventoryId, out var endpoint))
        {
            TraceLog($"HandlePlayerInventoryMoveRequested failed to resolve inventory={inventoryId}");
            return;
        }

        var moved = endpoint.Inventory.TryMoveItem(fromSlot, toSlot, splitStack);
        TraceLog($"HandlePlayerInventoryMoveRequested inventory={inventoryId} from={fromSlot} to={toSlot} split={splitStack} moved={moved}");
        if (moved)
        {
            UpdateHud();
        }
    }

    private void HandlePlayerInventoryTransferRequested(string fromInventoryId, Vector2I fromSlot, string toInventoryId, Vector2I toSlot, bool splitStack)
    {
        if (!TryResolveInventoryEndpoint(fromInventoryId, out var fromEndpoint)
            || !TryResolveInventoryEndpoint(toInventoryId, out var toEndpoint))
        {
            TraceLog($"HandlePlayerInventoryTransferRequested failed resolve from={fromInventoryId} to={toInventoryId}");
            return;
        }

        var moved = fromEndpoint.Inventory.TryMoveItemTo(toEndpoint.Inventory, fromSlot, toSlot, splitStack, toEndpoint.CanInsert, fromEndpoint.CanInsert);
        TraceLog($"HandlePlayerInventoryTransferRequested from={fromInventoryId}@{fromSlot} to={toInventoryId}@{toSlot} split={splitStack} moved={moved}");
        if (moved)
        {
            UpdateHud();
        }
    }

    private void HandlePlayerInventorySlotActivated(string inventoryId, Vector2I slot)
    {
        _selectedPlayerItemInventoryId = inventoryId;
        _selectedPlayerItemSlot = slot;
        _hasSelectedPlayerItemSlot = true;
        TraceLog($"HandlePlayerInventorySlotActivated inventory={inventoryId} slot={slot}");

        if (inventoryId == FactoryPlayerController.BackpackInventoryId && slot.Y == 0)
        {
            TraceLog("HandlePlayerInventorySlotActivated forwarding to hotbar press");
            HandlePlayerHotbarPressed(slot.X);
            return;
        }

        _selectedBuildKind = null;
        _playerController?.DisarmHotbarPlacement();
        _playerPlacementArmed = inventoryId == FactoryPlayerController.BackpackInventoryId
            && ResolveSelectedPlayerItem() is FactoryItem item
            && FactoryPresentation.IsPlaceableStructureItem(item);
        TraceLog($"HandlePlayerInventorySlotActivated armedPlacement={_playerPlacementArmed} resolvedItem={ResolveSelectedPlayerItem()?.ItemKind.ToString() ?? "none"}");
        RefreshInteractionModeFromBuildSource();
        UpdateHud();
    }

    private void HandleDetailRecipeSelected(string recipeId)
    {
        if (_selectedStructure is IFactoryStructureDetailProvider detailProvider && detailProvider.TrySetDetailRecipe(recipeId))
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
        if (!_hasSelectedPlayerItemSlot
            || string.IsNullOrWhiteSpace(_selectedPlayerItemInventoryId)
            || !TryResolveInventoryEndpoint(_selectedPlayerItemInventoryId!, out var endpoint))
        {
            _playerPlacementArmed = false;
            TraceLog("TryConsumeSelectedPlayerPlaceable failed because selected slot or endpoint was invalid");
            return false;
        }

        var consumed = endpoint.Inventory.TryTakeFromSlot(_selectedPlayerItemSlot, out _);
        TraceLog($"TryConsumeSelectedPlayerPlaceable inventory={_selectedPlayerItemInventoryId} slot={_selectedPlayerItemSlot} consumed={consumed}");
        _playerController?.RefreshActiveSlotState();

        var remainingItem = endpoint.Inventory.GetItemOrDefault(_selectedPlayerItemSlot);
        _playerPlacementArmed = remainingItem is not null && FactoryPresentation.IsPlaceableStructureItem(remainingItem);
        if (!_playerPlacementArmed && _selectedPlayerItemSlot.Y == 0)
        {
            _playerController?.DisarmHotbarPlacement();
        }

        return consumed;
    }

    private FactoryStructureDetailModel? BuildSelectedStructureLinkedDetailModel()
    {
        return _selectedStructure is IFactoryStructureDetailProvider detailProvider
            && GodotObject.IsInstanceValid(_selectedStructure)
            && _selectedStructure.IsInsideTree()
            ? detailProvider.GetDetailModel()
            : null;
    }

    private FactoryItem? ResolveSelectedPlayerItem()
    {
        if (!_hasSelectedPlayerItemSlot || string.IsNullOrWhiteSpace(_selectedPlayerItemInventoryId))
        {
            return _playerController?.GetActiveHotbarItem();
        }

        if (!TryResolveInventoryEndpoint(_selectedPlayerItemInventoryId!, out var endpoint))
        {
            return _playerController?.GetActiveHotbarItem();
        }

        return endpoint.Inventory.GetItemOrDefault(_selectedPlayerItemSlot);
    }

    private bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        if (_playerController?.TryResolveInventoryEndpoint(inventoryId, out endpoint) == true)
        {
            return true;
        }

        if (_selectedStructure is IFactoryInventoryEndpointProvider endpointProvider
            && endpointProvider.TryResolveInventoryEndpoint(inventoryId, out endpoint))
        {
            return true;
        }

        endpoint = default;
        return false;
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

        _previewCell = new MeshInstance3D { Name = "PreviewCell" };
        _previewCell.Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.92f, 0.08f, FactoryConstants.CellSize * 0.92f) };
        _previewCell.Position = new Vector3(0.0f, 0.05f, 0.0f);
        _previewRoot.AddChild(_previewCell);

        _previewArrow = FactoryPreviewVisuals.CreateFacingArrow("PreviewArrow", FactoryConstants.CellSize, 0.18f);
        _previewRoot.AddChild(_previewArrow);

        _previewPowerRange = new MeshInstance3D { Name = "PreviewPowerRange", Visible = false };
        _previewPowerRange.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _previewRoot.AddChild(_previewPowerRange);

        ApplyPreviewColor(_previewCell, new Color(0.35f, 0.95f, 0.55f, 0.45f));
        ApplyPreviewColor(_previewArrow, new Color(0.35f, 0.95f, 0.55f, 0.45f));
        ApplyPreviewColor(_previewPowerRange, new Color(0.35f, 0.95f, 0.55f, 0.15f));
        _previewRoot.Visible = false;
    }

    private static void ApplyPreviewColor(MeshInstance3D meshInstance, Color color)
    {
        var material = new StandardMaterial3D();
        material.AlbedoColor = color;
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.Roughness = 0.4f;
        meshInstance.MaterialOverride = material;
    }

    private static void ApplyPreviewColor(Node3D arrowRoot, Color color)
    {
        FactoryPreviewVisuals.ApplyArrowColor(arrowRoot, color);
    }

    private static WorldEnvironment CreateEnvironment()
    {
        var environment = new Environment();
        environment.BackgroundMode = Environment.BGMode.Color;
        environment.BackgroundColor = new Color("111827");
        environment.AmbientLightSource = Environment.AmbientSource.Color;
        environment.AmbientLightColor = new Color("D6E4F0");
        environment.AmbientLightSkyContribution = 0.0f;
        environment.AmbientLightEnergy = 0.7f;

        return new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = environment
        };
    }

    private static DirectionalLight3D CreateDirectionalLight()
    {
        return new DirectionalLight3D
        {
            Name = "SunLight",
            RotationDegrees = new Vector3(-56.0f, -34.0f, 0.0f),
            LightEnergy = 1.45f,
            ShadowEnabled = true
        };
    }

    private static Node3D CreateFloor()
    {
        var floorRoot = new Node3D { Name = "FloorRoot" };
        var floor = new MeshInstance3D { Name = "FactoryFloor" };
        floor.Mesh = new PlaneMesh
        {
            Size = new Vector2(
                (FactoryConstants.GridMax - FactoryConstants.GridMin + 1) * FactoryConstants.CellSize,
                (FactoryConstants.GridMax - FactoryConstants.GridMin + 1) * FactoryConstants.CellSize)
        };

        var floorMaterial = new StandardMaterial3D();
        floorMaterial.AlbedoColor = new Color("1F2937");
        floorMaterial.Roughness = 1.0f;
        floor.MaterialOverride = floorMaterial;
        floorRoot.AddChild(floor);

        return floorRoot;
    }

    private static Node3D CreateGridLines()
    {
        var gridRoot = new Node3D { Name = "GridLines" };
        var lineMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.35f, 0.43f, 0.53f, 0.65f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 1.0f
        };
        var worldMin = (FactoryConstants.GridMin - 0.5f) * FactoryConstants.CellSize;
        var worldMax = (FactoryConstants.GridMax + 0.5f) * FactoryConstants.CellSize;
        var lineLength = worldMax - worldMin;

        for (var i = FactoryConstants.GridMin; i <= FactoryConstants.GridMax + 1; i++)
        {
            var x = (i - 0.5f) * FactoryConstants.CellSize;
            var vertical = new MeshInstance3D();
            vertical.Mesh = new BoxMesh { Size = new Vector3(0.03f, 0.02f, lineLength) };
            vertical.Position = new Vector3(x, 0.02f, 0.0f);
            vertical.MaterialOverride = lineMaterial;
            gridRoot.AddChild(vertical);

            var z = (i - 0.5f) * FactoryConstants.CellSize;
            var horizontal = new MeshInstance3D();
            horizontal.Mesh = new BoxMesh { Size = new Vector3(lineLength, 0.02f, 0.03f) };
            horizontal.Position = new Vector3(0.0f, 0.02f, z);
            horizontal.MaterialOverride = lineMaterial;
            gridRoot.AddChild(horizontal);
        }

        return gridRoot;
    }

    private void EnsureInputActions()
    {
        EnsureAction("camera_pan_left", new InputEventKey { PhysicalKeycode = Key.A }, new InputEventKey { PhysicalKeycode = Key.Left });
        EnsureAction("camera_pan_right", new InputEventKey { PhysicalKeycode = Key.D }, new InputEventKey { PhysicalKeycode = Key.Right });
        EnsureAction("camera_pan_up", new InputEventKey { PhysicalKeycode = Key.W }, new InputEventKey { PhysicalKeycode = Key.Up });
        EnsureAction("camera_pan_down", new InputEventKey { PhysicalKeycode = Key.S }, new InputEventKey { PhysicalKeycode = Key.Down });
        EnsureAction("player_move_left", new InputEventKey { PhysicalKeycode = Key.A });
        EnsureAction("player_move_right", new InputEventKey { PhysicalKeycode = Key.D });
        EnsureAction("player_move_forward", new InputEventKey { PhysicalKeycode = Key.W });
        EnsureAction("player_move_backward", new InputEventKey { PhysicalKeycode = Key.S });
        EnsureAction("camera_zoom_in", new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true });
        EnsureAction("camera_zoom_out", new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true });
        EnsureAction("camera_rotate_left", new InputEventKey { PhysicalKeycode = Key.Q });
        EnsureAction("camera_rotate_right", new InputEventKey { PhysicalKeycode = Key.E });
        EnsureAction("build_confirm", new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        EnsureAction("remove_structure", new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true });
        EnsureAction("build_cancel", new InputEventKey { PhysicalKeycode = Key.Escape });
        EnsureAction("select_producer", new InputEventKey { PhysicalKeycode = Key.Key1 });
        EnsureAction("select_belt", new InputEventKey { PhysicalKeycode = Key.Key2 });
        EnsureAction("select_sink", new InputEventKey { PhysicalKeycode = Key.Key3 });
        EnsureAction("select_splitter", new InputEventKey { PhysicalKeycode = Key.Key4 });
        EnsureAction("select_merger", new InputEventKey { PhysicalKeycode = Key.Key5 });
        EnsureAction("select_bridge", new InputEventKey { PhysicalKeycode = Key.Key6 });
        EnsureAction("select_loader", new InputEventKey { PhysicalKeycode = Key.Key7 });
        EnsureAction("select_unloader", new InputEventKey { PhysicalKeycode = Key.Key8 });
        EnsureAction("select_storage", new InputEventKey { PhysicalKeycode = Key.Key9 });
        EnsureAction("select_inserter", new InputEventKey { PhysicalKeycode = Key.Key0 });
        EnsureAction("select_wall", new InputEventKey { PhysicalKeycode = Key.Minus });
        EnsureAction("select_ammo_assembler", new InputEventKey { PhysicalKeycode = Key.Equal });
        EnsureAction("select_gun_turret", new InputEventKey { PhysicalKeycode = Key.P });
    }

    private static void EnsureAction(string actionName, params InputEvent[] events)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        if (InputMap.ActionGetEvents(actionName).Count > 0)
        {
            return;
        }

        foreach (var inputEvent in events)
        {
            InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }

    private static bool HasSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--factory-smoke-test", global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async void RunSmokeChecks()
    {
        if (_grid is null || _cameraRig is null || _cameraRig.Camera is null || _simulation is null)
        {
            GD.PushError("FACTORY_SMOKE_FAILED missing grid or camera rig.");
            GetTree().Quit(1);
            return;
        }

        var probeCell = new Vector2I(4, 4);
        if (!_grid.CanPlace(probeCell))
        {
            GD.PushError("FACTORY_SMOKE_FAILED probe cell is unexpectedly occupied.");
            GetTree().Quit(1);
            return;
        }

        PlaceStructure(BuildPrototypeKind.Belt, probeCell, FacingDirection.South);
        var placed = _grid.TryGetStructure(probeCell, out var placedStructure) && placedStructure is BeltStructure;
        RemoveStructure(probeCell);
        var removed = _grid.CanPlace(probeCell);
        var multiCellPlacementVerified = RunMultiCellPlacementSmoke();
        var previewArrowReady = _previewArrow is not null && _previewArrow.GetChildCount() >= 3;
        var playerInteractionVerified = await RunPlayerCharacterSmoke(probeCell);

        var initialStructureCount = _simulation.RegisteredStructureCount;
        var poweredFactoryVerified = await RunPoweredFactorySmoke();
        var sinkStats = CollectSinkStats();
        ConfigureCombatScenarios();
        var profilerText = _hud?.ProfilerText ?? string.Empty;
        var splitterFallbackRecovered = await RunSplitterFallbackSmoke();
        var bridgeLaneRecovered = await RunBridgeLaneIndependenceSmoke();
        var storageFlowVerified = await RunStorageInserterSmoke();
        var inspectionVerified = VerifyStorageInspectionPanel();
        var detailWindowVerified = await RunStructureDetailSmoke();
        var blueprintVerified = RunBlueprintWorkflowSmoke();
        var workspaceVerified = RunWorkspaceNavigationSmoke();
        var itemVisualProfilesVerified = RunItemVisualProfileSmoke();
        var structureVisualProfilesVerified = RunStructureVisualProfileSmoke();
        var combatVerified = await VerifyCombatScenarios();

        if (!placed
            || !removed
            || initialStructureCount < 40
            || !poweredFactoryVerified
            || sinkStats.deliveredTotal <= 0
            || string.IsNullOrWhiteSpace(profilerText)
            || !profilerText.Contains("FPS", global::System.StringComparison.Ordinal)
            || !splitterFallbackRecovered
            || !bridgeLaneRecovered
            || !storageFlowVerified
            || !inspectionVerified
            || !detailWindowVerified
            || !blueprintVerified
            || !workspaceVerified
            || !itemVisualProfilesVerified
            || !structureVisualProfilesVerified
            || !combatVerified
            || !multiCellPlacementVerified
            || !previewArrowReady
            || !playerInteractionVerified)
        {
            GD.PushError($"FACTORY_SMOKE_FAILED placed={placed} removed={removed} multiCell={multiCellPlacementVerified} playerInteraction={playerInteractionVerified} structures={initialStructureCount} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} profiler={(!string.IsNullOrWhiteSpace(profilerText))} splitterFallback={splitterFallbackRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} workspace={workspaceVerified} itemVisualProfiles={itemVisualProfilesVerified} structureVisualProfiles={structureVisualProfilesVerified} combat={combatVerified} previewArrowReady={previewArrowReady}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"FACTORY_SMOKE_OK structures={initialStructureCount} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} splitterFallback={splitterFallbackRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} workspace={workspaceVerified} itemVisualProfiles={itemVisualProfilesVerified} structureVisualProfiles={structureVisualProfilesVerified} combat={combatVerified} multiCell={multiCellPlacementVerified} previewArrowReady={previewArrowReady} playerInteraction={playerInteractionVerified}");
        GetTree().Quit();
    }

    private async Task<bool> RunPlayerCharacterSmoke(Vector2I placementCell)
    {
        if (_playerController is null || _cameraRig is null || _grid is null)
        {
            return false;
        }

        var playerSpawned = _playerController.IsInsideTree() && _playerController.BackpackInventory.Count > 0;
        var startPosition = _playerController.GlobalPosition;
        Input.ActionPress("player_move_right");
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        Input.ActionRelease("player_move_right");
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

        var playerMoved = _playerController.GlobalPosition.DistanceTo(startPosition) > 0.2f;
        var cameraFollowed = new Vector2(_cameraRig.Position.X, _cameraRig.Position.Z)
            .DistanceTo(new Vector2(_playerController.GlobalPosition.X, _playerController.GlobalPosition.Z)) < 3.4f;

        HandlePlayerHotbarPressed(1);
        var hotbarSelected = _playerController.ActiveHotbarIndex == 1 && _playerController.GetArmedPlaceablePrototype().HasValue;
        var stackBeforePlacement = GetInventorySlotCount(_playerController.BackpackInventory, new Vector2I(1, 0));
        var validPlacement = hotbarSelected
            && TryValidateWorldPlacement(_playerController.GetArmedPlaceablePrototype()!.Value, placementCell, FacingDirection.East, out _);

        var placedFromHotbar = false;
        var consumedOneItem = false;
        if (validPlacement)
        {
            _selectedFacing = FacingDirection.East;
            _hoveredCell = placementCell;
            _hasHoveredCell = true;
            _canPlaceCurrentCell = true;
            HandlePrimaryClick();
            placedFromHotbar = _grid.TryGetStructure(placementCell, out var placedStructure) && placedStructure is not null;
            consumedOneItem = GetInventorySlotCount(_playerController.BackpackInventory, new Vector2I(1, 0)) == stackBeforePlacement - 1;
            RemoveStructure(placementCell);
        }

        var crossTransferWorked = false;
        if (_grid.TryGetStructure(new Vector2I(17, 2), out var storageStructure)
            && storageStructure is StorageStructure storage
            && storage.TryResolveInventoryEndpoint("storage-buffer", out var storageEndpoint)
            && _playerController.TryResolveInventoryEndpoint(FactoryPlayerController.BackpackInventoryId, out var playerEndpoint))
        {
            if (_simulation is not null)
            {
                var seededCargo = _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo);
                storage.TryReceiveProvidedItem(seededCargo, storage.Cell + Vector2I.Left, _simulation);
            }

            var sourceSnapshot = storageEndpoint.Inventory.Snapshot();
            var targetSnapshot = playerEndpoint.Inventory.Snapshot();
            var sourceSlot = new Vector2I(-1, -1);
            var targetSlot = new Vector2I(-1, -1);
            for (var index = 0; index < sourceSnapshot.Length; index++)
            {
                if (sourceSnapshot[index].HasItem)
                {
                    sourceSlot = sourceSnapshot[index].Position;
                    break;
                }
            }

            for (var index = 0; index < targetSnapshot.Length; index++)
            {
                if (!targetSnapshot[index].HasItem && targetSnapshot[index].Position.Y > 0)
                {
                    targetSlot = targetSnapshot[index].Position;
                    break;
                }
            }

            if (sourceSlot.X >= 0 && targetSlot.X >= 0)
            {
                var moved = storageEndpoint.Inventory.TryMoveItemTo(playerEndpoint.Inventory, sourceSlot, targetSlot, false, playerEndpoint.CanInsert);
                crossTransferWorked = moved && playerEndpoint.Inventory.GetItemOrDefault(targetSlot) is not null;
            }
        }

        var passed = playerSpawned
            && playerMoved
            && cameraFollowed
            && hotbarSelected
            && placedFromHotbar
            && consumedOneItem
            && crossTransferWorked;

        if (!passed)
        {
            GD.Print($"FACTORY_PLAYER_SMOKE playerSpawned={playerSpawned} playerMoved={playerMoved} cameraFollowed={cameraFollowed} hotbarSelected={hotbarSelected} placedFromHotbar={placedFromHotbar} consumedOneItem={consumedOneItem} crossTransferWorked={crossTransferWorked}");
        }

        return passed;
    }

    private static int GetInventorySlotCount(FactorySlottedItemInventory inventory, Vector2I slot)
    {
        var snapshot = inventory.Snapshot();
        for (var index = 0; index < snapshot.Length; index++)
        {
            if (snapshot[index].Position == slot)
            {
                return snapshot[index].StackCount;
            }
        }

        return 0;
    }

    private bool RunMultiCellPlacementSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var anchor = new Vector2I(18, 18);
        var overflowAnchor = new Vector2I(FactoryConstants.GridMax, FactoryConstants.GridMax);
        if (!TryValidateWorldPlacement(BuildPrototypeKind.LargeStorageDepot, anchor, FacingDirection.East, out _))
        {
            return false;
        }

        if (TryValidateWorldPlacement(BuildPrototypeKind.LargeStorageDepot, overflowAnchor, FacingDirection.East, out _))
        {
            return false;
        }

        var depot = PlaceStructure(BuildPrototypeKind.LargeStorageDepot, anchor, FacingDirection.East) as LargeStorageDepotStructure;
        if (depot is null)
        {
            return false;
        }

        var occupiedCell = anchor + Vector2I.One;
        var resolvedFromSecondaryCell = _grid.TryGetStructure(occupiedCell, out var resolvedStructure) && resolvedStructure == depot;
        RemoveStructure(occupiedCell);
        var released = _grid.CanPlace(anchor) && _grid.CanPlace(occupiedCell);
        var singleCellStillValid = TryValidateWorldPlacement(BuildPrototypeKind.Belt, anchor, FacingDirection.East, out _);
        return resolvedFromSecondaryCell && released && singleCellStillValid;
    }

    private bool RunWorkspaceNavigationSmoke()
    {
        if (_hud is null)
        {
            return false;
        }

        var workspaceIds = _hud.GetWorkspaceIds();
        var hasBuild = HasWorkspace(workspaceIds, BuildWorkspaceId);
        var hasBlueprints = HasWorkspace(workspaceIds, BlueprintWorkspaceId);
        var hasTelemetry = HasWorkspace(workspaceIds, TelemetryWorkspaceId);
        var hasCombat = HasWorkspace(workspaceIds, CombatWorkspaceId);
        var hasTesting = HasWorkspace(workspaceIds, TestingWorkspaceId);

        _hud.SelectWorkspace(BlueprintWorkspaceId);
        var blueprintVisible = _hud.ActiveWorkspaceId == BlueprintWorkspaceId && _hud.IsWorkspaceVisible(BlueprintWorkspaceId);

        _hud.SelectWorkspace(TelemetryWorkspaceId);
        var telemetryVisible = _hud.ActiveWorkspaceId == TelemetryWorkspaceId && _hud.IsWorkspaceVisible(TelemetryWorkspaceId);

        _hud.SelectWorkspace(CombatWorkspaceId);
        var combatVisible = _hud.ActiveWorkspaceId == CombatWorkspaceId && _hud.IsWorkspaceVisible(CombatWorkspaceId);

        _hud.SelectWorkspace(TestingWorkspaceId);
        var testingVisible = _hud.ActiveWorkspaceId == TestingWorkspaceId && _hud.IsWorkspaceVisible(TestingWorkspaceId);

        _hud.SelectWorkspace(BuildWorkspaceId);
        var buildVisible = _hud.ActiveWorkspaceId == BuildWorkspaceId && _hud.IsWorkspaceVisible(BuildWorkspaceId);

        return hasBuild
            && hasBlueprints
            && hasTelemetry
            && hasCombat
            && hasTesting
            && blueprintVisible
            && telemetryVisible
            && combatVisible
            && testingVisible
            && buildVisible;
    }

    private static bool HasWorkspace(IReadOnlyList<string> workspaceIds, string workspaceId)
    {
        for (var index = 0; index < workspaceIds.Count; index++)
        {
            if (string.Equals(workspaceIds[index], workspaceId, global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> RunPoweredFactorySmoke()
    {
        if (_grid is null)
        {
            return false;
        }

        var coalDrillFound = _grid.TryGetStructure(new Vector2I(-36, -30), out var coalDrillStructure) && coalDrillStructure is MiningDrillStructure;
        var generatorFound = _grid.TryGetStructure(new Vector2I(-31, -30), out var generatorStructure) && generatorStructure is GeneratorStructure;
        var ironDrillFound = _grid.TryGetStructure(new Vector2I(-36, -22), out var ironDrillStructure) && ironDrillStructure is MiningDrillStructure;
        var copperDrillFound = _grid.TryGetStructure(new Vector2I(-36, -18), out var copperDrillStructure) && copperDrillStructure is MiningDrillStructure;
        var ironSmelterFound = _grid.TryGetStructure(new Vector2I(-30, -22), out var ironSmelterStructure) && ironSmelterStructure is SmelterStructure;
        var copperSmelterFound = _grid.TryGetStructure(new Vector2I(-30, -18), out var copperSmelterStructure) && copperSmelterStructure is SmelterStructure;
        var wireAssemblerFound = _grid.TryGetStructure(new Vector2I(-28, -18), out var wireAssemblerStructure) && wireAssemblerStructure is AssemblerStructure;
        var ammoAssemblerFound = _grid.TryGetStructure(new Vector2I(-26, -20), out var ammoAssemblerStructure) && ammoAssemblerStructure is AmmoAssemblerStructure;
        var sinkFound = _grid.TryGetStructure(new Vector2I(-23, -20), out var sinkStructure) && sinkStructure is SinkStructure;
        var maintenanceGeneratorFound = _grid.TryGetStructure(new Vector2I(10, 8), out var maintenanceGeneratorStructure) && maintenanceGeneratorStructure is GeneratorStructure;
        var batteryAssemblerFound = _grid.TryGetStructure(new Vector2I(14, 2), out var batteryAssemblerStructure) && batteryAssemblerStructure is AssemblerStructure;
        var maintenanceSinkFound = _grid.TryGetStructure(new Vector2I(23, 2), out var maintenanceSinkStructure) && maintenanceSinkStructure is SinkStructure;
        var successTurretFound = _grid.TryGetStructure(new Vector2I(14, 20), out var successTurretStructure) && successTurretStructure is GunTurretStructure;
        if (!coalDrillFound || !generatorFound || !ironDrillFound || !copperDrillFound || !ironSmelterFound || !copperSmelterFound || !wireAssemblerFound || !ammoAssemblerFound || !sinkFound || !maintenanceGeneratorFound || !batteryAssemblerFound || !maintenanceSinkFound || !successTurretFound)
        {
            GD.Print($"FACTORY_POWERED_SMOKE_MISSING coalDrill={coalDrillFound} generator={generatorFound} ironDrill={ironDrillFound} copperDrill={copperDrillFound} ironSmelter={ironSmelterFound} copperSmelter={copperSmelterFound} wireAssembler={wireAssemblerFound} ammoAssembler={ammoAssemblerFound} sink={sinkFound} maintenanceGenerator={maintenanceGeneratorFound} batteryAssembler={batteryAssemblerFound} maintenanceSink={maintenanceSinkFound} successTurret={successTurretFound}");
            return false;
        }

        var coalDrill = (MiningDrillStructure)coalDrillStructure!;
        var generator = (GeneratorStructure)generatorStructure!;
        var ironDrill = (MiningDrillStructure)ironDrillStructure!;
        var copperDrill = (MiningDrillStructure)copperDrillStructure!;
        var ironSmelter = (SmelterStructure)ironSmelterStructure!;
        var copperSmelter = (SmelterStructure)copperSmelterStructure!;
        var wireAssembler = (AssemblerStructure)wireAssemblerStructure!;
        var ammoAssembler = (AmmoAssemblerStructure)ammoAssemblerStructure!;
        var sink = (SinkStructure)sinkStructure!;
        var maintenanceGenerator = (GeneratorStructure)maintenanceGeneratorStructure!;
        var batteryAssembler = (AssemblerStructure)batteryAssemblerStructure!;
        var maintenanceSink = (SinkStructure)maintenanceSinkStructure!;
        var successTurret = (GunTurretStructure)successTurretStructure!;

        await ToSignal(GetTree().CreateTimer(40.0f), SceneTreeTimer.SignalName.Timeout);
        var ironSummary = ironSmelter.GetDetailModel().SummaryLines;
        var copperSummary = copperSmelter.GetDetailModel().SummaryLines;
        var wireSummary = wireAssembler.GetDetailModel().SummaryLines;
        var ammoSummary = ammoAssembler.GetDetailModel().SummaryLines;
        var batterySummary = batteryAssembler.GetDetailModel().SummaryLines;
        var verified = coalDrill.ResourceKind == FactoryResourceKind.Coal
            && ironDrill.ResourceKind == FactoryResourceKind.IronOre
            && copperDrill.ResourceKind == FactoryResourceKind.CopperOre
            && sink.DeliveredTotal > 0
            && (generator.IsGenerating || generator.HasFuelBuffered)
            && ContainsSummaryLine(ironSummary, "铁板")
            && ContainsSummaryLine(copperSummary, "铜板")
            && ContainsSummaryLine(wireSummary, "铜线")
            && ContainsSummaryLine(ammoSummary, "弹药")
            && (maintenanceGenerator.IsGenerating || maintenanceGenerator.HasFuelBuffered)
            && maintenanceSink.DeliveredTotal > 0
            && ContainsSummaryLine(batterySummary, "电池组")
            && (successTurret.BufferedAmmo > 0 || successTurret.ShotsFired > 0);

        if (!verified)
        {
            GD.Print($"FACTORY_POWERED_SMOKE coalKind={coalDrill.ResourceKind} ironKind={ironDrill.ResourceKind} copperKind={copperDrill.ResourceKind} sink={sink.DeliveredTotal} generator={generator.IsGenerating} generatorFuel={generator.HasFuelBuffered} maintenanceGenerator={maintenanceGenerator.IsGenerating} maintenanceFuel={maintenanceGenerator.HasFuelBuffered} maintenanceSink={maintenanceSink.DeliveredTotal} successShots={successTurret.ShotsFired} ironSummary={string.Join('|', ironSummary)} copperSummary={string.Join('|', copperSummary)} wireSummary={string.Join('|', wireSummary)} ammoSummary={string.Join('|', ammoSummary)} batterySummary={string.Join('|', batterySummary)}");
        }

        return verified;
    }

    private static bool ContainsSummaryLine(IReadOnlyList<string> summaryLines, string pattern)
    {
        for (var index = 0; index < summaryLines.Count; index++)
        {
            if (summaryLines[index].Contains(pattern, global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool RunItemVisualProfileSmoke()
    {
        var placeholderVisual = FactoryTransportVisualFactory.CreateVisual(new FactoryItem(-101, BuildPrototypeKind.MiningDrill, FactoryItemKind.IronOre), FactoryConstants.CellSize);
        var billboardVisual = FactoryTransportVisualFactory.CreateVisual(new FactoryItem(-102, BuildPrototypeKind.Assembler, FactoryItemKind.CopperWire), FactoryConstants.CellSize);
        var modelVisual = FactoryTransportVisualFactory.CreateVisual(new FactoryItem(-103, BuildPrototypeKind.Assembler, FactoryItemKind.AmmoMagazine), FactoryConstants.CellSize);

        var placeholderMesh = FindFirstMesh(placeholderVisual);
        var billboardMesh = FindFirstMesh(billboardVisual);
        var modelMeshCount = CountMeshes(modelVisual);
        var distinctBaselineColors =
            !FactoryItemCatalog.GetAccentColor(FactoryItemKind.Coal).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.IronOre))
            && !FactoryItemCatalog.GetAccentColor(FactoryItemKind.IronOre).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.CopperOre))
            && !FactoryItemCatalog.GetAccentColor(FactoryItemKind.AmmoMagazine).IsEqualApprox(FactoryItemCatalog.GetAccentColor(FactoryItemKind.HighVelocityAmmo));
        var iconsPresent =
            FactoryItemCatalog.GetIconTexture(FactoryItemKind.IronOre) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.IronPlate) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.CopperWire) is not null
            && FactoryItemCatalog.GetIconTexture(FactoryItemKind.HighVelocityAmmo) is not null;

        placeholderVisual.QueueFree();
        billboardVisual.QueueFree();
        modelVisual.QueueFree();

        return placeholderMesh?.Mesh is BoxMesh
            && billboardMesh?.Mesh is QuadMesh
            && billboardMesh.MaterialOverride is StandardMaterial3D billboardMaterial
            && billboardMaterial.BillboardMode == BaseMaterial3D.BillboardModeEnum.Enabled
            && modelMeshCount >= 2
            && distinctBaselineColors
            && iconsPresent;
    }

    private bool RunStructureVisualProfileSmoke()
    {
        var authoredRoot = new Node3D { Name = "AuthoredVisualRoot" };
        var authoredMesh = new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = new BoxMesh { Size = new Vector3(0.4f, 0.4f, 0.4f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("38BDF8") }
        };
        authoredRoot.AddChild(authoredMesh);
        authoredMesh.Owner = authoredRoot;
        var authoredScene = new PackedScene();
        var authoredPacked = authoredScene.Pack(authoredRoot) == Error.Ok;
        authoredRoot.Free();

        var authoredController = FactoryStructureVisualFactory.CreateDetachedController(
            new FactoryStructureVisualProfile(
                authoredScene: authoredScene,
                nodeAnchors: new Dictionary<string, NodePath>
                {
                    ["mesh"] = new NodePath("Mesh")
                },
                materialAnchors: new Dictionary<string, NodePath>
                {
                    ["mesh-material"] = new NodePath("Mesh")
                }),
            FactoryConstants.CellSize);
        var authoredResolved = authoredPacked
            && authoredController.SourceKind == FactoryStructureVisualSourceKind.AuthoredScene
            && authoredController.GetNodeAnchor<MeshInstance3D>("mesh") is not null
            && authoredController.GetMaterialAnchor("mesh-material") is not null
            && CountMeshes(authoredController.Root) >= 1;

        var fallbackController = FactoryStructureVisualFactory.CreateDetachedController(
            new FactoryStructureVisualProfile(
                authoredScenePath: "res://missing/structure_visual_profile_smoke.tscn",
                proceduralBuilder: controller =>
                {
                    controller.Root.AddChild(FactoryStructureVisualFactory.CreateGenericPlaceholderNode(controller.CellSize * 0.8f));
                }),
            FactoryConstants.CellSize);
        var fallbackResolved = fallbackController.SourceKind == FactoryStructureVisualSourceKind.Procedural
            && fallbackController.Root.GetChildCount() > 0;

        var genericController = FactoryStructureVisualFactory.CreateDetachedController(
            new FactoryStructureVisualProfile(authoredScenePath: "res://missing/structure_visual_profile_placeholder.tscn"),
            FactoryConstants.CellSize);
        var genericResolved = genericController.SourceKind == FactoryStructureVisualSourceKind.GenericPlaceholder
            && CountMeshes(genericController.Root) >= 3;

        var legacyController = new GeneratorStructure().CreateDetachedVisualControllerForTesting();
        var legacyResolved = legacyController.SourceKind == FactoryStructureVisualSourceKind.Procedural
            && CountMeshes(legacyController.Root) >= 4;

        var smelter = new SmelterStructure();
        var smelterController = smelter.CreateDetachedVisualControllerForTesting();
        var coolState = new FactoryStructureVisualState(
            IsVisible: true,
            IsHovered: false,
            IsSelected: false,
            IsUnderAttack: false,
            IsDestroyed: false,
            IsActive: true,
            IsProcessing: false,
            ProcessRatio: 0.0f,
            HasBufferedOutput: false,
            PowerStatus: FactoryPowerStatus.Disconnected,
            PowerSatisfaction: 0.0f,
            PresentationTimeSeconds: 0.0);
        var hotState = coolState with
        {
            IsProcessing = true,
            ProcessRatio = 0.72f,
            HasBufferedOutput = true,
            PowerStatus = FactoryPowerStatus.Powered,
            PowerSatisfaction = 1.0f,
            PresentationTimeSeconds = 1.6
        };

        for (var index = 0; index < 6; index++)
        {
            smelter.ApplyVisualStateForTesting(smelterController, coolState with { PresentationTimeSeconds = index * 0.1 }, 1.0f);
        }

        var coolCoreEnergy = smelterController.GetMaterialAnchor("core-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var coolFireboxEnergy = smelterController.GetMaterialAnchor("firebox-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var coolPlumeScale = smelterController.GetNodeAnchor<Node3D>("heat-plume")?.Scale.Y ?? 0.0f;

        for (var index = 0; index < 6; index++)
        {
            smelter.ApplyVisualStateForTesting(smelterController, hotState with { PresentationTimeSeconds = 1.6 + (index * 0.12) }, 1.0f);
        }

        var hotCoreEnergy = smelterController.GetMaterialAnchor("core-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var hotFireboxEnergy = smelterController.GetMaterialAnchor("firebox-glow")?.EmissionEnergyMultiplier ?? 0.0f;
        var hotPlumeScale = smelterController.GetNodeAnchor<Node3D>("heat-plume")?.Scale.Y ?? 0.0f;
        var smelterHotCoolVerified = smelterController.SourceKind == FactoryStructureVisualSourceKind.Procedural
            && hotCoreEnergy > coolCoreEnergy
            && hotFireboxEnergy > coolFireboxEnergy
            && hotPlumeScale > coolPlumeScale;

        authoredController.Root.Free();
        fallbackController.Root.Free();
        genericController.Root.Free();
        legacyController.Root.Free();
        smelterController.Root.Free();

        return authoredResolved
            && fallbackResolved
            && genericResolved
            && legacyResolved
            && smelterHotCoolVerified;
    }

    private static MeshInstance3D? FindFirstMesh(Node node)
    {
        if (node is MeshInstance3D mesh)
        {
            return mesh;
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                var found = FindFirstMesh(childNode);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static int CountMeshes(Node node)
    {
        var total = node is MeshInstance3D ? 1 : 0;
        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                total += CountMeshes(childNode);
            }
        }

        return total;
    }

    private bool RunBlueprintWorkflowSmoke()
    {
        if (_blueprintSite is null || _grid is null || _simulation is null)
        {
            return false;
        }

        var captured = FactoryBlueprintCaptureService.CaptureSelection(
            _blueprintSite,
            new Rect2I(14, 0, 10, 5),
            "Smoke Maintenance Depot Blueprint");
        if (captured is null || captured.StructureCount < 8)
        {
            return false;
        }

        FactoryBlueprintLibrary.AddOrUpdate(captured);
        FactoryBlueprintLibrary.SelectActive(captured.Id);

        var invalidPlan = FactoryBlueprintPlanner.CreatePlan(captured, _blueprintSite, captured.SuggestedAnchorCell);
        var structureCountBefore = _simulation.RegisteredStructureCount;
        if (!TryFindBlueprintAnchor(captured, FacingDirection.South, out var validAnchor))
        {
            return false;
        }

        var validPlan = FactoryBlueprintPlanner.CreatePlan(captured, _blueprintSite, validAnchor, FacingDirection.South);
        var committed = validPlan.IsValid && FactoryBlueprintPlanner.CommitPlan(validPlan, _blueprintSite);
        if (!committed)
        {
            return false;
        }

        var placedEntries = 0;
        for (var index = 0; index < validPlan.Entries.Count; index++)
        {
            var entry = validPlan.Entries[index];
            if (_grid.TryGetStructure(entry.TargetCell, out var structure)
                && structure is not null
                && structure.Kind == entry.SourceEntry.Kind
                && structure.Facing == entry.TargetFacing)
            {
                placedEntries++;
            }
        }

        return !invalidPlan.IsValid
            && validPlan.IsValid
            && placedEntries == captured.Entries.Count
            && _simulation.RegisteredStructureCount >= structureCountBefore + captured.Entries.Count;
    }

    private bool TryFindBlueprintAnchor(FactoryBlueprintRecord blueprint, out Vector2I anchor)
    {
        return TryFindBlueprintAnchor(blueprint, FacingDirection.East, out anchor);
    }

    private bool TryFindBlueprintAnchor(FactoryBlueprintRecord blueprint, FacingDirection rotation, out Vector2I anchor)
    {
        anchor = Vector2I.Zero;
        if (_blueprintSite is null || _grid is null)
        {
            return false;
        }

        for (var y = _grid.MinCell.Y; y <= _grid.MaxCell.Y; y++)
        {
            for (var x = _grid.MinCell.X; x <= _grid.MaxCell.X; x++)
            {
                var candidate = new Vector2I(x, y);
                var plan = FactoryBlueprintPlanner.CreatePlan(blueprint, _blueprintSite, candidate, rotation);
                if (!plan.IsValid)
                {
                    continue;
                }

                anchor = candidate;
                return true;
            }
        }

        return false;
    }

    private async Task<bool> RunSplitterFallbackSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(24, -12),
            new Vector2I(25, -12),
            new Vector2I(26, -12),
            new Vector2I(26, -13),
            new Vector2I(27, -13),
            new Vector2I(26, -11),
            new Vector2I(27, -11),
            new Vector2I(28, -11)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        var sourceStorage = PlaceStructure(BuildPrototypeKind.Storage, 24, -12, FacingDirection.East) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 25, -12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, 26, -12, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 26, -13, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 27, -13, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 26, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 27, -11, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 28, -11, FacingDirection.East);

        if (sourceStorage is null
            || !_grid.TryGetStructure(new Vector2I(28, -11), out var sinkStructure) || sinkStructure is not SinkStructure sink
            || !_grid.TryGetStructure(new Vector2I(27, -13), out var blockerStructure) || blockerStructure is not BeltStructure blockedBelt)
        {
            return false;
        }

        for (var index = 0; index < 8; index++)
        {
            sourceStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), sourceStorage.Cell + Vector2I.Left, _simulation);
        }

        await ToSignal(GetTree().CreateTimer(10.0f), SceneTreeTimer.SignalName.Timeout);
        var blockedBranchOccupied = blockedBelt.TransitItemCount > 0;
        var deliveredAfter = sink.DeliveredTotal;

        var passed = blockedBranchOccupied || deliveredAfter > 0;
        if (!passed)
        {
            GD.Print($"FACTORY_SPLITTER_SMOKE blockedBranchOccupied={blockedBranchOccupied} deliveredAfter={deliveredAfter} sourceBuffered={sourceStorage.BufferedCount}");
        }

        return passed;
    }

    private async Task<bool> RunBridgeLaneIndependenceSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(24, -4),
            new Vector2I(25, -4),
            new Vector2I(26, -4),
            new Vector2I(27, -4),
            new Vector2I(26, -6),
            new Vector2I(26, -5),
            new Vector2I(26, -3),
            new Vector2I(26, -2)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        var eastStorage = PlaceStructure(BuildPrototypeKind.Storage, 24, -4, FacingDirection.East) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 25, -4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Bridge, 26, -4, FacingDirection.East);
        var eastSink = PlaceStructure(BuildPrototypeKind.Sink, 27, -4, FacingDirection.East) as SinkStructure;

        var southStorage = PlaceStructure(BuildPrototypeKind.Storage, 26, -6, FacingDirection.South) as StorageStructure;
        PlaceStructure(BuildPrototypeKind.Belt, 26, -5, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 26, -3, FacingDirection.South);
        var southSink = PlaceStructure(BuildPrototypeKind.Sink, 26, -2, FacingDirection.South) as SinkStructure;

        if (eastStorage is null
            || southStorage is null
            || eastSink is null
            || southSink is null)
        {
            return false;
        }

        for (var index = 0; index < 4; index++)
        {
            eastStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), eastStorage.Cell + Vector2I.Left, _simulation);
            southStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), southStorage.Cell + Vector2I.Left, _simulation);
        }

        await ToSignal(GetTree().CreateTimer(8.0f), SceneTreeTimer.SignalName.Timeout);

        var passed = eastSink.DeliveredTotal > 0 && southSink.DeliveredTotal > 0;
        if (!passed)
        {
            GD.Print($"FACTORY_BRIDGE_SMOKE eastDelivered={eastSink.DeliveredTotal} southDelivered={southSink.DeliveredTotal} eastBuffered={eastStorage.BufferedCount} southBuffered={southStorage.BufferedCount}");
        }

        return passed;
    }

    private async Task<bool> RunStorageInserterSmoke()
    {
        if (_grid is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(24, 4),
            new Vector2I(25, 4),
            new Vector2I(26, 4),
            new Vector2I(27, 4),
            new Vector2I(28, 4)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        PlaceStructure(BuildPrototypeKind.Storage, 24, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 25, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 26, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 27, 4, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 28, 4, FacingDirection.East);

        if (!_grid.TryGetStructure(new Vector2I(25, 4), out var storageStructure) || storageStructure is not StorageStructure storage
            || !_grid.TryGetStructure(new Vector2I(28, 4), out var sinkStructure) || sinkStructure is not SinkStructure sink)
        {
            return false;
        }

        var injectedItems = new List<FactoryItem>();
        for (var index = 0; index < 3; index++)
        {
            var injectedItem = _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo);
            if (!storage.TryReceiveProvidedItem(injectedItem, storage.Cell + Vector2I.Left, _simulation))
            {
                return false;
            }

            injectedItems.Add(injectedItem);
        }

        var deterministicPeek = storage.TryPeekProvidedItem(storage.GetOutputCell(), _simulation, out var peekedItem)
            && peekedItem?.Id == injectedItems[0].Id;
        var deterministicTake = storage.TryTakeProvidedItem(storage.GetOutputCell(), _simulation, out var takenItem)
            && takenItem?.Id == injectedItems[0].Id;

        var stackedBuffered = false;
        var storageDetail = storage.GetDetailModel();
        if (storageDetail.InventorySections.Count > 0)
        {
            for (var index = 0; index < storageDetail.InventorySections[0].Slots.Count; index++)
            {
                if (storageDetail.InventorySections[0].Slots[index].StackCount > 1)
                {
                    stackedBuffered = true;
                    break;
                }
            }
        }

        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        var bufferedBefore = storage.BufferedCount;
        var deliveredBefore = sink.DeliveredTotal;

        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        var deliveredAfter = sink.DeliveredTotal;

        var passed = deterministicPeek
            && deterministicTake
            && stackedBuffered
            && bufferedBefore >= 0
            && deliveredAfter > 0;
        if (!passed)
        {
            GD.Print($"FACTORY_STORAGE_SMOKE deterministicPeek={deterministicPeek} deterministicTake={deterministicTake} stackedBuffered={stackedBuffered} bufferedBefore={bufferedBefore} deliveredBefore={deliveredBefore} deliveredAfter={deliveredAfter}");
        }

        return passed;
    }

    private bool VerifyStorageInspectionPanel()
    {
        if (_grid is null || _hud is null)
        {
            return false;
        }

        EnterInteractionMode();
        if (!_grid.TryGetStructure(new Vector2I(17, 2), out var structure) || structure is null)
        {
            return false;
        }

        _selectedStructure = structure;
        UpdateHud();

        return _hud.InspectionTitleText.Contains("仓储", global::System.StringComparison.Ordinal)
            && _hud.InspectionBodyText.Contains("容量", global::System.StringComparison.Ordinal)
            && _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains("仓储", global::System.StringComparison.Ordinal);
    }

    private async Task<bool> RunStructureDetailSmoke()
    {
        if (_grid is null || _hud is null || _simulation is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(26, 26),
            new Vector2I(27, 26),
            new Vector2I(26, 28),
            new Vector2I(26, 30),
            new Vector2I(27, 30)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        var feederStorage = PlaceStructure(BuildPrototypeKind.Storage, 26, 26, FacingDirection.East) as StorageStructure;
        var storage = PlaceStructure(BuildPrototypeKind.Storage, 27, 26, FacingDirection.East) as StorageStructure;
        var generator = PlaceStructure(BuildPrototypeKind.Generator, 24, 28, FacingDirection.East) as GeneratorStructure;
        PlaceStructure(BuildPrototypeKind.PowerPole, 25, 29, FacingDirection.East);
        var recipeAssembler = PlaceStructure(BuildPrototypeKind.Assembler, 26, 28, FacingDirection.East) as AssemblerStructure;
        var ammoAssembler = PlaceStructure(BuildPrototypeKind.AmmoAssembler, 26, 30, FacingDirection.East) as AmmoAssemblerStructure;
        var turret = PlaceStructure(BuildPrototypeKind.GunTurret, 27, 30, FacingDirection.East) as GunTurretStructure;

        if (feederStorage is null
            || storage is null
            || generator is null
            || recipeAssembler is null
            || ammoAssembler is null
            || turret is null)
        {
            return false;
        }

        generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
        generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);

        var assemblerRecipeChanged = recipeAssembler.TrySetDetailRecipe("gear");
        var ammoRecipeChanged = ammoAssembler.TrySetDetailRecipe("high-velocity-ammo");

        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);

        var stackLimit = FactoryItemCatalog.GetMaxStackSize(FactoryItemKind.GenericCargo);
        for (var index = 0; index < stackLimit + 2; index++)
        {
            var seededCargo = _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo);
            if (!storage.TryReceiveProvidedItem(seededCargo, storage.Cell + Vector2I.Left, _simulation))
            {
                return false;
            }
        }

        feederStorage.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo), feederStorage.Cell + Vector2I.Left, _simulation);
        recipeAssembler.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate), recipeAssembler.Cell + Vector2I.Left, _simulation);
        recipeAssembler.TryReceiveProvidedItem(_simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate), recipeAssembler.Cell + Vector2I.Left, _simulation);

        if (turret.BufferedAmmo <= 0)
        {
            var injectedAmmo = _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.HighVelocityAmmo);
            turret.TryReceiveProvidedItem(injectedAmmo, ammoAssembler.Cell, _simulation);
        }

        _selectedStructure = storage;
        UpdateHud();
        var storageDetailVisible = _hud.IsDetailVisible && _hud.DetailTitleText.Contains("仓储", global::System.StringComparison.Ordinal);

        var storageDetail = storage.GetDetailModel();
        var storageSection = storageDetail.InventorySections.Count > 0 ? storageDetail.InventorySections[0] : null;
        var mergeSourceSlot = new Vector2I(-1, -1);
        var mergeTargetSlot = new Vector2I(-1, -1);
        var emptySlot = new Vector2I(-1, -1);
        var targetStackBeforeMove = 0;
        var totalStackCountBeforeMove = 0;
        var stackCountsVisible = false;
        if (storageSection is null)
        {
            return false;
        }

        for (var index = 0; index < storageSection.Slots.Count; index++)
        {
            var slot = storageSection.Slots[index];
            totalStackCountBeforeMove += slot.StackCount;
            if (slot.StackCount > 1)
            {
                stackCountsVisible = true;
            }

            if (!slot.HasItem && emptySlot.X < 0)
            {
                emptySlot = slot.Position;
            }

            if (!slot.HasItem)
            {
                continue;
            }

            if (slot.StackCount < slot.MaxStackSize && mergeTargetSlot.X < 0)
            {
                mergeTargetSlot = slot.Position;
                targetStackBeforeMove = slot.StackCount;
                continue;
            }

            if (mergeTargetSlot.X >= 0
                && slot.ItemKind == FactoryItemKind.GenericCargo
                && slot.Position != mergeTargetSlot
                && mergeSourceSlot.X < 0)
            {
                mergeSourceSlot = slot.Position;
            }
        }

        if (mergeTargetSlot.X >= 0 && mergeSourceSlot.X < 0)
        {
            for (var index = 0; index < storageSection.Slots.Count; index++)
            {
                var slot = storageSection.Slots[index];
                if (slot.HasItem
                    && slot.ItemKind == FactoryItemKind.GenericCargo
                    && slot.Position != mergeTargetSlot)
                {
                    mergeSourceSlot = slot.Position;
                    break;
                }
            }
        }

        var emptyDragRejected = mergeTargetSlot.X >= 0
            && emptySlot.X >= 0
            && !storage.TryMoveDetailInventoryItem("storage-buffer", emptySlot, mergeTargetSlot);
        var storageMoved = mergeSourceSlot.X >= 0
            && mergeTargetSlot.X >= 0
            && storage.TryMoveDetailInventoryItem("storage-buffer", mergeSourceSlot, mergeTargetSlot);

        var movedDetail = storage.GetDetailModel();
        var movedSection = movedDetail.InventorySections[0];
        var movedTargetStackCount = 0;
        var movedTotalStackCount = 0;
        var splitSourceSlot = new Vector2I(-1, -1);
        var splitSourceCountBefore = 0;
        for (var index = 0; index < movedSection.Slots.Count; index++)
        {
            var slot = movedSection.Slots[index];
            movedTotalStackCount += slot.StackCount;
            if (slot.Position == mergeTargetSlot)
            {
                movedTargetStackCount = slot.StackCount;
            }

            if (slot.HasItem && slot.StackCount > 1 && slot.Position != emptySlot && splitSourceSlot.X < 0)
            {
                splitSourceSlot = slot.Position;
                splitSourceCountBefore = slot.StackCount;
            }
        }

        var splitMoveWorked = splitSourceSlot.X >= 0
            && emptySlot.X >= 0
            && storage.TryMoveDetailInventoryItem("storage-buffer", splitSourceSlot, emptySlot, true);

        var splitDetail = storage.GetDetailModel();
        var splitTotalStackCount = 0;
        var splitTargetCount = 0;
        var splitSourceCountAfter = 0;
        for (var index = 0; index < splitDetail.InventorySections[0].Slots.Count; index++)
        {
            var slot = splitDetail.InventorySections[0].Slots[index];
            splitTotalStackCount += slot.StackCount;
            if (slot.Position == emptySlot)
            {
                splitTargetCount = slot.StackCount;
            }
            else if (slot.Position == splitSourceSlot)
            {
                splitSourceCountAfter = slot.StackCount;
            }
        }

        await ToSignal(GetTree().CreateTimer(1.8f), SceneTreeTimer.SignalName.Timeout);

        _selectedStructure = recipeAssembler;
        UpdateHud();
        recipeAssembler.TryPeekProvidedItem(new Vector2I(27, 28), _simulation, out var producedItem);
        var assemblerRecipeVerified = assemblerRecipeChanged
            && _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains("组装机", global::System.StringComparison.Ordinal)
            && producedItem?.ItemKind == FactoryItemKind.Gear;

        _selectedStructure = turret;
        UpdateHud();
        var turretDetail = turret.GetDetailModel();
        var turretHasAmmo = turret.BufferedAmmo > 0;
        var turretShowsHighVelocityAmmo = false;
        if (turretDetail.InventorySections.Count > 0)
        {
            var ammoSlots = turretDetail.InventorySections[0].Slots;
            for (var index = 0; index < ammoSlots.Count; index++)
            {
                if (ammoSlots[index].ItemLabel?.Contains("高速弹药", global::System.StringComparison.Ordinal) ?? false)
                {
                    turretShowsHighVelocityAmmo = true;
                    break;
                }
            }
        }

        return storageDetailVisible
            && stackCountsVisible
            && emptyDragRejected
            && storageMoved
            && movedTargetStackCount > targetStackBeforeMove
            && movedTotalStackCount == totalStackCountBeforeMove
            && splitMoveWorked
            && splitTargetCount > 0
            && splitTargetCount < splitSourceCountBefore
            && splitSourceCountAfter > 0
            && splitTotalStackCount == totalStackCountBeforeMove
            && assemblerRecipeVerified
            && ammoRecipeChanged
            && turretHasAmmo
            && turretShowsHighVelocityAmmo;
    }

    private async Task<bool> VerifyCombatScenarios()
    {
        if (_grid is null || _simulation is null || _hud is null)
        {
            return false;
        }

        _grid.TryGetStructure(new Vector2I(14, 14), out var breachWallStructure);
        var breachWall = breachWallStructure as WallStructure;

        await ToSignal(GetTree().CreateTimer(20.0f), SceneTreeTimer.SignalName.Timeout);

        var totalTurretShots = 0;
        var totalHeavyTurretShots = 0;
        for (var x = FactoryConstants.GridMin; x <= FactoryConstants.GridMax; x++)
        {
            for (var y = FactoryConstants.GridMin; y <= FactoryConstants.GridMax; y++)
            {
                if (_grid.TryGetStructure(new Vector2I(x, y), out var structure) && structure is GunTurretStructure turret)
                {
                    totalTurretShots += turret.ShotsFired;
                }
                else if (_grid.TryGetStructure(new Vector2I(x, y), out structure) && structure is HeavyGunTurretStructure heavyTurret)
                {
                    totalHeavyTurretShots += heavyTurret.ShotsFired;
                }
            }
        }

        var combatPressureVisible = totalTurretShots > 0
            || totalHeavyTurretShots > 0
            || _simulation.ActiveEnemyCount > 0
            || _simulation.DefeatedEnemyCount > 0
            || _simulation.DestroyedStructureCount > 0;
        var heavyProjectileVerified = totalHeavyTurretShots > 0
            || _simulation.ActiveProjectileCount > 0
            || _simulation.TotalProjectileLaunchCount > 0;
        var breachOccurred = breachWall is null
            || !GodotObject.IsInstanceValid(breachWall)
            || breachWall.CurrentHealth < breachWall.MaxHealth
            || _simulation.DestroyedStructureCount > 0;

        GD.Print($"FACTORY_COMBAT_SMOKE totalTurretShots={totalTurretShots} heavyTurretShots={totalHeavyTurretShots} activeProjectiles={_simulation.ActiveProjectileCount} totalProjectileLaunches={_simulation.TotalProjectileLaunchCount} kills={_simulation.DefeatedEnemyCount} activeEnemies={_simulation.ActiveEnemyCount} destroyedStructures={_simulation.DestroyedStructureCount} breachWallPresent={breachWall is not null} breachWallHealth={(breachWall is not null && GodotObject.IsInstanceValid(breachWall) ? breachWall.CurrentHealth : -1.0f)}");

        return combatPressureVisible && heavyProjectileVerified;
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
}
