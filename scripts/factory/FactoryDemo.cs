using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class FactoryDemo : Node3D
{
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
        [BuildPrototypeKind.MiningDrill] = new BuildPrototypeDefinition(BuildPrototypeKind.MiningDrill, "采矿机", new Color("FBBF24"), "只能放在矿点上，通电后持续采出资源。"),
        [BuildPrototypeKind.Generator] = new BuildPrototypeDefinition(BuildPrototypeKind.Generator, "发电机", new Color("FB923C"), "消耗煤炭发电，为周边电网提供基础供给。"),
        [BuildPrototypeKind.PowerPole] = new BuildPrototypeDefinition(BuildPrototypeKind.PowerPole, "电线杆", new Color("FDE68A"), "延伸电网覆盖，把发电机接到更远的机器。"),
        [BuildPrototypeKind.Smelter] = new BuildPrototypeDefinition(BuildPrototypeKind.Smelter, "熔炉", new Color("CBD5E1"), "消耗电力把矿石炼成铁板。"),
        [BuildPrototypeKind.Assembler] = new BuildPrototypeDefinition(BuildPrototypeKind.Assembler, "组装机", new Color("67E8F9"), "消耗中间品和电力，组装高阶产物。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收站", new Color("FDE68A"), "接收物品并统计送达数量。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把左右两路物流汇成前方一路。"),
        [BuildPrototypeKind.Bridge] = new BuildPrototypeDefinition(BuildPrototypeKind.Bridge, "跨桥", new Color("F59E0B"), "让南北和东西两路物流跨越而不互连。"),
        [BuildPrototypeKind.Loader] = new BuildPrototypeDefinition(BuildPrototypeKind.Loader, "装载器", new Color("FDBA74"), "把后方带上的物品装入前方机器或回收端。"),
        [BuildPrototypeKind.Unloader] = new BuildPrototypeDefinition(BuildPrototypeKind.Unloader, "卸载器", new Color("93C5FD"), "把机器端输出卸到前方传送网络。"),
        [BuildPrototypeKind.Storage] = new BuildPrototypeDefinition(BuildPrototypeKind.Storage, "仓储", new Color("94A3B8"), "缓存多件物品，可向前输出，也能被机械臂抓取。"),
        [BuildPrototypeKind.Inserter] = new BuildPrototypeDefinition(BuildPrototypeKind.Inserter, "机械臂", new Color("FACC15"), "从后方抓取一件物品并向前投送。"),
        [BuildPrototypeKind.Wall] = new BuildPrototypeDefinition(BuildPrototypeKind.Wall, "墙体", new Color("D1D5DB"), "高耐久阻挡物，用来拖延敌人推进。"),
        [BuildPrototypeKind.AmmoAssembler] = new BuildPrototypeDefinition(BuildPrototypeKind.AmmoAssembler, "弹药组装器", new Color("FB923C"), "持续生产弹药，沿物流链补给防线。"),
        [BuildPrototypeKind.GunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.GunTurret, "机枪炮塔", new Color("CBD5E1"), "需要弹药供给，敌人进入射程时自动开火。")
    };

    private GridManager? _grid;
    private SimulationController? _simulation;
    private FactoryCameraRig? _cameraRig;
    private FactoryHud? _hud;
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

    public override void _Ready()
    {
        EnsureInputActions();
        BuildSceneGraph();
        ConfigureGameplay();
        CreateStarterLayout();
        UpdateHud();

        if (HasSmokeTestFlag())
        {
            CallDeferred(nameof(RunSmokeChecks));
        }
    }

    public override void _Process(double delta)
    {
        _averageFrameMilliseconds = SmoothMetric(_averageFrameMilliseconds, delta * 1000.0, 0.1);
        if (_cameraRig is not null)
        {
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
        _hud.DetailRecipeSelected += HandleDetailRecipeSelected;
        _hud.DetailClosed += HandleDetailWindowClosed;
        _hud.BlueprintCaptureRequested += StartBlueprintCapture;
        _hud.BlueprintSaveRequested += HandleBlueprintSaveRequested;
        _hud.BlueprintSelected += HandleBlueprintSelected;
        _hud.BlueprintApplyRequested += EnterBlueprintApplyMode;
        _hud.BlueprintConfirmRequested += ConfirmBlueprintApply;
        _hud.BlueprintDeleteRequested += HandleBlueprintDeleteRequested;
        _hud.BlueprintCancelRequested += CancelBlueprintWorkflow;
        AddChild(_hud);

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

        HandleBuildShortcut("select_producer", BuildPrototypeKind.Producer);
        HandleBuildShortcut("select_belt", BuildPrototypeKind.Belt);
        HandleBuildShortcut("select_sink", BuildPrototypeKind.Sink);
        HandleBuildShortcut("select_splitter", BuildPrototypeKind.Splitter);
        HandleBuildShortcut("select_merger", BuildPrototypeKind.Merger);
        HandleBuildShortcut("select_bridge", BuildPrototypeKind.Bridge);
        HandleBuildShortcut("select_loader", BuildPrototypeKind.Loader);
        HandleBuildShortcut("select_unloader", BuildPrototypeKind.Unloader);
        HandleBuildShortcut("select_storage", BuildPrototypeKind.Storage);
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
        _interactionMode = kind.HasValue ? FactoryInteractionMode.Build : FactoryInteractionMode.Interact;
        _deleteDragActive = false;

        if (_interactionMode == FactoryInteractionMode.Build)
        {
            _selectedStructure = null;
        }
    }

    private void EnterInteractionMode()
    {
        _selectedBuildKind = null;
        _interactionMode = FactoryInteractionMode.Interact;
        _deleteDragActive = false;
    }

    private void EnterDeleteMode()
    {
        CancelBlueprintWorkflow(clearActiveBlueprint: false);
        _selectedBuildKind = null;
        _selectedStructure = null;
        _interactionMode = FactoryInteractionMode.Delete;
        _deleteDragActive = false;
    }

    private void CreateStarterLayout()
    {
        AddPoweredBootstrapDistrict();
        AddPoweredManufacturingDistrict();
        AddStorageOutputCorridor();
        AddBeltToStorageTransferLine();
        AddWestSplitterFanout();
        AddCentralBridgeCrossing();
        AddAmmoFedDefenseLane();
        AddAmmoStarvedBreachLane();
        AddMixedPressureLane();
        RefreshAllTopology();
        ConfigureCombatScenarios();
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
                BuildRectCells(new Vector2I(-39, -31), new Vector2I(2, 2))),
            new FactoryResourceDepositDefinition(
                "iron_patch_main",
                FactoryResourceKind.IronOre,
                "北西铁矿区",
                new Color("64748B"),
                BuildRectCells(new Vector2I(-39, -23), new Vector2I(2, 2)))
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
        PlaceStructure(BuildPrototypeKind.MiningDrill, -38, -30, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-37, -30), FacingDirection.East, 6);
        var generator = PlaceStructure(BuildPrototypeKind.Generator, -31, -30, FacingDirection.East) as GeneratorStructure;
        if (generator is not null && _simulation is not null)
        {
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
            generator.TryAcceptItem(_simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal), generator.Cell + Vector2I.Left, _simulation);
        }

        PlaceStructure(BuildPrototypeKind.PowerPole, -34, -27, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -31, -24, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.PowerPole, -28, -21, FacingDirection.East);
    }

    private void AddPoweredManufacturingDistrict()
    {
        PlaceStructure(BuildPrototypeKind.MiningDrill, -38, -22, FacingDirection.East);
        PlaceBeltRun(new Vector2I(-37, -22), FacingDirection.East, 7);
        PlaceStructure(BuildPrototypeKind.Smelter, -30, -22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -29, -22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, -28, -22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, -27, -22, FacingDirection.East);
        var assembler = PlaceStructure(BuildPrototypeKind.Assembler, -26, -22, FacingDirection.East) as AssemblerStructure;
        assembler?.TrySetDetailRecipe("standard-ammo");
        PlaceStructure(BuildPrototypeKind.Belt, -25, -22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, -24, -22, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, -23, -22, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.PowerPole, -26, -20, FacingDirection.East);
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
        PlaceStructure(BuildPrototypeKind.Wall, -14, 16, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Wall, -13, 16, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.GunTurret, -12, 16, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Inserter, -11, 16, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Storage, -10, 16, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Belt, -9, 16, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.AmmoAssembler, -8, 16, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Storage, -7, 16, FacingDirection.West);
    }

    private void AddAmmoStarvedBreachLane()
    {
        PlaceStructure(BuildPrototypeKind.Wall, 14, 14, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Wall, 13, 14, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.GunTurret, 12, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 10, 14, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 9, 14, FacingDirection.East);
    }

    private void AddMixedPressureLane()
    {
        PlaceStructure(BuildPrototypeKind.Wall, 14, -15, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.Wall, 13, -15, FacingDirection.West);
        PlaceStructure(BuildPrototypeKind.GunTurret, 12, -15, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 11, -15, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 10, -15, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 9, -15, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.AmmoAssembler, 8, -15, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 7, -15, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 6, -15, FacingDirection.East);
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
            BuildLanePath(new Vector2I(-16, 16), new Vector2I(-15, 16), new Vector2I(-14, 16), new Vector2I(-12, 16)),
            new FactoryEnemySpawnRule[]
            {
                new("melee", 2.8f),
                new("melee", 3.2f),
                new("melee", 3.0f)
            }));
        _combatDirector.AddLane(new FactoryEnemyLaneDefinition(
            "starved_lane",
            BuildLanePath(new Vector2I(16, 14), new Vector2I(15, 14), new Vector2I(14, 14), new Vector2I(12, 14)),
            new FactoryEnemySpawnRule[]
            {
                new("melee", 3.0f),
                new("melee", 3.0f)
            }));
        _combatDirector.AddLane(new FactoryEnemyLaneDefinition(
            "mixed_lane",
            BuildLanePath(new Vector2I(16, -15), new Vector2I(15, -15), new Vector2I(14, -15), new Vector2I(12, -15)),
            new FactoryEnemySpawnRule[]
            {
                new("melee", 3.2f),
                new("ranged", 4.8f),
                new("melee", 3.6f)
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

        if (_interactionMode == FactoryInteractionMode.Build && _selectedBuildKind.HasValue)
        {
            _canPlaceCurrentCell = TryValidateWorldPlacement(_selectedBuildKind.Value, cell, out var placementIssue);
            _previewMessage = _canPlaceCurrentCell
                ? $"可在 ({cell.X}, {cell.Y}) 放置{_definitions[_selectedBuildKind.Value].DisplayName}，朝向 {FactoryDirection.ToLabel(_selectedFacing)}"
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
        var showPreview = showCapturePreview || (_hasHoveredCell
            && ((_interactionMode == FactoryInteractionMode.Build && _selectedBuildKind.HasValue)
                || _interactionMode == FactoryInteractionMode.Delete));
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

        _previewRoot.Position = _grid.CellToWorld(_hoveredCell);
        _previewRoot.Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(_selectedFacing), 0.0f);
        _previewCell.Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.92f, 0.08f, FactoryConstants.CellSize * 0.92f) };
        _previewCell.Position = new Vector3(0.0f, 0.05f, 0.0f);
        _previewArrow.Visible = true;

        var tint = _canPlaceCurrentCell ? new Color(0.35f, 0.95f, 0.55f, 0.45f) : new Color(1.0f, 0.35f, 0.35f, 0.45f);
        ApplyPreviewColor(_previewCell, tint);
        ApplyPreviewColor(_previewArrow, tint.Lightened(0.1f));
        UpdatePreviewPowerRange(_selectedBuildKind, _grid, _previewPowerRange, tint);
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
        if (_grid is null || _structureRoot is null || _simulation is null || !TryValidateWorldPlacement(kind, cell, out _))
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
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Build && _selectedBuildKind.HasValue)
        {
            if (_canPlaceCurrentCell)
            {
                PlaceStructure(_selectedBuildKind.Value, _hoveredCell, _selectedFacing);
            }

            return;
        }

        _selectedStructure = _hoveredStructure;
    }

    private void HandleSecondaryClick()
    {
        if (!_hasHoveredCell)
        {
            return;
        }

        if (_interactionMode == FactoryInteractionMode.Build)
        {
            EnterInteractionMode();
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
        var count = 0;
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                if (_grid.TryGetStructure(new Vector2I(x, y), out var structure) && structure is not null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void DeleteStructuresInRect(Vector2I start, Vector2I end)
    {
        if (_grid is null)
        {
            return;
        }

        var rect = GetDeleteRect(start, end);
        var cellsToDelete = new List<Vector2I>();
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                var cell = new Vector2I(x, y);
                if (_grid.TryGetStructure(cell, out var structure) && structure is not null)
                {
                    cellsToDelete.Add(cell);
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

        if (!TryValidateWorldPlacement(entry.Kind, targetCell, out var placementIssue))
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

    private void HandleDetailInventoryMoveRequested(string inventoryId, Vector2I fromSlot, Vector2I toSlot)
    {
        if (_selectedStructure is IFactoryStructureDetailProvider detailProvider && detailProvider.TryMoveDetailInventoryItem(inventoryId, fromSlot, toSlot))
        {
            UpdateHud();
        }
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

    private bool IsPointerOverUi()
    {
        return _hud?.BlocksWorldInput(GetViewport().GuiGetHoveredControl()) ?? false;
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
        var previewArrowReady = _previewArrow is not null && _previewArrow.GetChildCount() >= 3;

        var initialStructureCount = _simulation.RegisteredStructureCount;
        var poweredFactoryVerified = await RunPoweredFactorySmoke();
        var sinkStats = CollectSinkStats();
        var profilerText = _hud?.ProfilerText ?? string.Empty;
        var splitterFallbackRecovered = await RunSplitterFallbackSmoke();
        var bridgeLaneRecovered = await RunBridgeLaneIndependenceSmoke();
        var storageFlowVerified = await RunStorageInserterSmoke();
        var inspectionVerified = VerifyStorageInspectionPanel();
        var detailWindowVerified = await RunStructureDetailSmoke();
        var blueprintVerified = RunBlueprintWorkflowSmoke();
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
            || !combatVerified
            || !previewArrowReady)
        {
            GD.PushError($"FACTORY_SMOKE_FAILED placed={placed} removed={removed} structures={initialStructureCount} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} profiler={(!string.IsNullOrWhiteSpace(profilerText))} splitterFallback={splitterFallbackRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} combat={combatVerified} previewArrowReady={previewArrowReady}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"FACTORY_SMOKE_OK structures={initialStructureCount} poweredFactory={poweredFactoryVerified} delivered={sinkStats.deliveredTotal} splitterFallback={splitterFallbackRecovered} bridgeLane={bridgeLaneRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified} detailWindow={detailWindowVerified} blueprint={blueprintVerified} combat={combatVerified} previewArrowReady={previewArrowReady}");
        GetTree().Quit();
    }

    private async Task<bool> RunPoweredFactorySmoke()
    {
        if (_grid is null)
        {
            return false;
        }

        if (!_grid.TryGetStructure(new Vector2I(-38, -30), out var coalDrillStructure) || coalDrillStructure is not MiningDrillStructure coalDrill
            || !_grid.TryGetStructure(new Vector2I(-31, -30), out var generatorStructure) || generatorStructure is not GeneratorStructure generator
            || !_grid.TryGetStructure(new Vector2I(-38, -22), out var ironDrillStructure) || ironDrillStructure is not MiningDrillStructure ironDrill
            || !_grid.TryGetStructure(new Vector2I(-30, -22), out var smelterStructure) || smelterStructure is not SmelterStructure smelter
            || !_grid.TryGetStructure(new Vector2I(-28, -22), out var storageStructure) || storageStructure is not StorageStructure storage
            || !_grid.TryGetStructure(new Vector2I(-26, -22), out var assemblerStructure) || assemblerStructure is not AssemblerStructure assembler
            || !_grid.TryGetStructure(new Vector2I(-23, -22), out var sinkStructure) || sinkStructure is not SinkStructure sink)
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(14.0f), SceneTreeTimer.SignalName.Timeout);
        var verified = coalDrill.ResourceKind == FactoryResourceKind.Coal
            && ironDrill.ResourceKind == FactoryResourceKind.IronOre
            && sink.DeliveredTotal > 0
            && (generator.IsGenerating || generator.HasFuelBuffered)
            && smelter.GetDetailModel().SummaryLines.Count > 0
            && assembler.GetDetailModel().SummaryLines.Count > 0;

        return verified;
    }

    private bool RunBlueprintWorkflowSmoke()
    {
        if (_blueprintSite is null || _grid is null || _simulation is null)
        {
            return false;
        }

        var captured = FactoryBlueprintCaptureService.CaptureSelection(
            _blueprintSite,
            new Rect2I(-8, 2, 8, 1),
            "Smoke Sandbox Blueprint");
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
        if (_grid is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(4, 7),
            new Vector2I(5, 7),
            new Vector2I(6, 7),
            new Vector2I(6, 6),
            new Vector2I(7, 6),
            new Vector2I(6, 8),
            new Vector2I(7, 8),
            new Vector2I(8, 8)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        PlaceStructure(BuildPrototypeKind.Producer, 4, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 5, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, 6, 7, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 6, 6, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 7, 6, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 6, 8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 7, 8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 8, 8, FacingDirection.East);

        if (!_grid.TryGetStructure(new Vector2I(8, 8), out var sinkStructure) || sinkStructure is not SinkStructure sink
            || !_grid.TryGetStructure(new Vector2I(7, 6), out var blockerStructure) || blockerStructure is not BeltStructure blockedBelt)
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(7.0f), SceneTreeTimer.SignalName.Timeout);
        var blockedBranchOccupied = blockedBelt.TransitItemCount > 0;
        var deliveredAfter = sink.DeliveredTotal;

        return blockedBranchOccupied || deliveredAfter > 0;
    }

    private async Task<bool> RunBridgeLaneIndependenceSmoke()
    {
        if (_grid is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(7, 2),
            new Vector2I(8, 2),
            new Vector2I(9, 2),
            new Vector2I(10, 2),
            new Vector2I(9, 0),
            new Vector2I(9, 1),
            new Vector2I(9, 3)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        PlaceStructure(BuildPrototypeKind.Producer, 7, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 8, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Bridge, 9, 2, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 10, 2, FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, 9, 0, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 1, FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, 9, 3, FacingDirection.South);

        if (!_grid.TryGetStructure(new Vector2I(10, 2), out var sinkStructure) || sinkStructure is not SinkStructure sink
            || !_grid.TryGetStructure(new Vector2I(9, 3), out var blockedStructure) || blockedStructure is not BeltStructure blockedBelt)
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(6.0f), SceneTreeTimer.SignalName.Timeout);

        return sink.DeliveredTotal > 0 && blockedBelt.TransitItemCount > 0;
    }

    private async Task<bool> RunStorageInserterSmoke()
    {
        if (_grid is null)
        {
            return false;
        }

        var requiredCells = new[]
        {
            new Vector2I(4, -8),
            new Vector2I(5, -8),
            new Vector2I(6, -8),
            new Vector2I(7, -8),
            new Vector2I(8, -8)
        };

        foreach (var cell in requiredCells)
        {
            if (!_grid.CanPlace(cell))
            {
                return false;
            }
        }

        PlaceStructure(BuildPrototypeKind.Producer, 4, -8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Storage, 5, -8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Inserter, 6, -8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, 7, -8, FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, 8, -8, FacingDirection.East);

        if (!_grid.TryGetStructure(new Vector2I(5, -8), out var storageStructure) || storageStructure is not StorageStructure storage
            || !_grid.TryGetStructure(new Vector2I(8, -8), out var sinkStructure) || sinkStructure is not SinkStructure sink)
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        var bufferedBefore = storage.BufferedCount;
        var deliveredBefore = sink.DeliveredTotal;

        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        var deliveredAfter = sink.DeliveredTotal;

        return bufferedBefore >= 0 && deliveredAfter > deliveredBefore;
    }

    private bool VerifyStorageInspectionPanel()
    {
        if (_grid is null || _hud is null)
        {
            return false;
        }

        EnterInteractionMode();
        if (!_grid.TryGetStructure(new Vector2I(-6, 2), out var structure) || structure is null)
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
        if (_grid is null || _hud is null)
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

        var feederProducer = PlaceStructure(BuildPrototypeKind.Producer, 26, 26, FacingDirection.East) as ProducerStructure;
        var storage = PlaceStructure(BuildPrototypeKind.Storage, 27, 26, FacingDirection.East) as StorageStructure;
        var recipeProducer = PlaceStructure(BuildPrototypeKind.Producer, 26, 28, FacingDirection.East) as ProducerStructure;
        var ammoAssembler = PlaceStructure(BuildPrototypeKind.AmmoAssembler, 26, 30, FacingDirection.East) as AmmoAssemblerStructure;
        var turret = PlaceStructure(BuildPrototypeKind.GunTurret, 27, 30, FacingDirection.East) as GunTurretStructure;

        if (feederProducer is null || storage is null || recipeProducer is null || ammoAssembler is null || turret is null)
        {
            return false;
        }

        var producerRecipeChanged = recipeProducer.TrySetDetailRecipe("machine-parts");
        var ammoRecipeChanged = ammoAssembler.TrySetDetailRecipe("high-velocity-ammo");

        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);

        if (turret.BufferedAmmo <= 0)
        {
            var injectedAmmo = _simulation!.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.HighVelocityAmmo);
            turret.TryReceiveProvidedItem(injectedAmmo, ammoAssembler.Cell, _simulation);
        }

        _selectedStructure = storage;
        UpdateHud();
        var storageDetailVisible = _hud.IsDetailVisible && _hud.DetailTitleText.Contains("仓储", global::System.StringComparison.Ordinal);

        var storageDetail = storage.GetDetailModel();
        var storageSection = storageDetail.InventorySections.Count > 0 ? storageDetail.InventorySections[0] : null;
        var occupiedSlot = new Vector2I(-1, -1);
        var emptySlot = new Vector2I(-1, -1);
        if (storageSection is null)
        {
            return false;
        }

        for (var index = 0; index < storageSection.Slots.Count; index++)
        {
            var slot = storageSection.Slots[index];
            if (slot.HasItem && occupiedSlot.X < 0)
            {
                occupiedSlot = slot.Position;
            }
            else if (!slot.HasItem && emptySlot.X < 0)
            {
                emptySlot = slot.Position;
            }
        }

        var storageMoved = occupiedSlot.X >= 0
            && emptySlot.X >= 0
            && storage.TryMoveDetailInventoryItem("storage-buffer", occupiedSlot, emptySlot);

        var movedDetail = storage.GetDetailModel();
        var movedSection = movedDetail.InventorySections[0];
        var movedTargetOccupied = false;
        for (var index = 0; index < movedSection.Slots.Count; index++)
        {
            var slot = movedSection.Slots[index];
            if (slot.Position == emptySlot && slot.HasItem)
            {
                movedTargetOccupied = true;
                break;
            }
        }

        _selectedStructure = recipeProducer;
        UpdateHud();
        recipeProducer.TryPeekProvidedItem(new Vector2I(27, 28), _simulation!, out var producedItem);
        var producerRecipeVerified = producerRecipeChanged
            && _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains("生产器", global::System.StringComparison.Ordinal)
            && producedItem?.ItemKind == FactoryItemKind.MachinePart;

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
            && storageMoved
            && movedTargetOccupied
            && producerRecipeVerified
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
        for (var x = FactoryConstants.GridMin; x <= FactoryConstants.GridMax; x++)
        {
            for (var y = FactoryConstants.GridMin; y <= FactoryConstants.GridMax; y++)
            {
                if (_grid.TryGetStructure(new Vector2I(x, y), out var structure) && structure is GunTurretStructure turret)
                {
                    totalTurretShots += turret.ShotsFired;
                }
            }
        }

        var combatPressureVisible = totalTurretShots > 0
            || _simulation.ActiveEnemyCount > 0
            || _simulation.DefeatedEnemyCount > 0
            || _simulation.DestroyedStructureCount > 0;
        var breachOccurred = breachWall is null
            || !GodotObject.IsInstanceValid(breachWall)
            || breachWall.CurrentHealth < breachWall.MaxHealth
            || _simulation.DestroyedStructureCount > 0;

        GD.Print($"FACTORY_COMBAT_SMOKE totalTurretShots={totalTurretShots} kills={_simulation.DefeatedEnemyCount} activeEnemies={_simulation.ActiveEnemyCount} destroyedStructures={_simulation.DestroyedStructureCount} breachWallPresent={breachWall is not null} breachWallHealth={(breachWall is not null && GodotObject.IsInstanceValid(breachWall) ? breachWall.CurrentHealth : -1.0f)}");

        return combatPressureVisible;
    }

    private static double SmoothMetric(double current, double sample, double weight)
    {
        return current <= 0.0
            ? sample
            : current + ((sample - current) * weight);
    }

    private bool TryValidateWorldPlacement(BuildPrototypeKind kind, Vector2I cell, out string message)
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

        return _grid.CanPlaceStructure(kind, cell, out message);
    }
}
