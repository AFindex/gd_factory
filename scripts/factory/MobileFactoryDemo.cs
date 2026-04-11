using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MobileFactoryDemo : Node3D
{
    private const string CommandWorkspaceId = "command";
    private const string EditorWorkspaceId = "editor";
    private const string TestingWorkspaceId = "testing";
    private const string BlueprintWorkspaceId = "blueprints";
    private const string DetailsWorkspaceId = "details";
    private const string OverviewWorkspaceId = "overview";
    private const string BuildTestWorkspaceId = "build-test";
    private const string DiagnosticsWorkspaceId = "diagnostics";
    private const string SavesWorkspaceId = "saves";
    private const int InteriorRenderLayer = 1;
    private const int HullRenderLayer = 2;
    private const float PreviewPowerPoleWireHeight = FactoryPreviewOverlaySupport.PreviewPowerPoleWireHeight;
    private const int PreviewPowerPoleConnectionRangeCells = FactoryPreviewOverlaySupport.PreviewPowerPoleConnectionRangeCells;

    private static readonly Vector2I AnchorA = new(-6, -3);
    private static readonly Vector2I AnchorB = new(2, 3);
    private static readonly Vector2I MiningAnchorA = new(-17, -14);
    private static readonly Vector2I MiningAnchorB = new(17, 12);
    private static readonly Vector2I BlockedAnchor = new(-1, 1);
    private static readonly Vector2I FocusedTurretCell = new(1, 0);
    private static readonly Vector2I FocusedSmelterCell = new(2, 3);
    private static readonly Vector2I FocusedAssemblerCell = new(4, 1);
    private static readonly Vector2I FocusedAmmoAssemblerCell = new(4, 4);
    private static readonly Vector2I FocusedIronBufferCell = new(1, 4);
    private static readonly Vector2I FocusedWireBufferCell = new(1, 5);
    private static readonly Vector2I FocusedDepotAnchorCell = new(5, 6);
    private static readonly Key[] InteriorPaletteKeys =
    {
        Key.Key1,
        Key.Key2,
        Key.Key3,
        Key.Key4,
        Key.Key5,
        Key.Key6,
        Key.Key7,
        Key.Key8,
        Key.Key9,
        Key.Key0,
        Key.Minus,
        Key.Equal
    };

    [Export]
    public bool UseLargeTestScenario { get; set; }

    private readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> _definitions = new()
    {
        [BuildPrototypeKind.Producer] = new BuildPrototypeDefinition(BuildPrototypeKind.Producer, "兼容生产器", new Color("9DC08B"), "兼容型占位产物流，仅用于 legacy 验证。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送，也允许末端直接并入另一段传送带的中段。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把后方、左侧和右侧三路物流汇成前方一路。"),
        [BuildPrototypeKind.Bridge] = new BuildPrototypeDefinition(BuildPrototypeKind.Bridge, "跨桥", new Color("F59E0B"), "让南北和东西两路物流跨越而不互连。"),
        [BuildPrototypeKind.CargoUnpacker] = new BuildPrototypeDefinition(BuildPrototypeKind.CargoUnpacker, "解包模块", new Color("38BDF8"), "把世界散装或封装货物拆成舱内供料单元，供后续模块处理。"),
        [BuildPrototypeKind.CargoPacker] = new BuildPrototypeDefinition(BuildPrototypeKind.CargoPacker, "封包模块", new Color("F97316"), "把内部供料重新压成世界标准封装货物，便于跨边界输出。"),
        [BuildPrototypeKind.TransferBuffer] = new BuildPrototypeDefinition(BuildPrototypeKind.TransferBuffer, "中转缓冲槽", new Color("14B8A6"), "位于维护通路边的嵌入缓冲槽，用来整理舱内标准化供料。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收器", new Color("FDE68A"), "吞掉输入物品并作为内部消费端。"),
        [BuildPrototypeKind.Storage] = new BuildPrototypeDefinition(BuildPrototypeKind.Storage, "仓储", new Color("94A3B8"), "缓存多件物品，可向前输出，也能被机械臂抓取。"),
        [BuildPrototypeKind.LargeStorageDepot] = new BuildPrototypeDefinition(BuildPrototypeKind.LargeStorageDepot, "大型仓储", new Color("64748B"), "占据 2x2 内部格子的仓储缓冲区，用于宽体移动工厂案例。"),
        [BuildPrototypeKind.Inserter] = new BuildPrototypeDefinition(BuildPrototypeKind.Inserter, "机械臂", new Color("FACC15"), "从后方抓取一件物品并向前投送。"),
        [BuildPrototypeKind.Wall] = new BuildPrototypeDefinition(BuildPrototypeKind.Wall, "墙体", new Color("D1D5DB"), "给移动工厂的前缘补上一段高耐久掩体。"),
        [BuildPrototypeKind.AmmoAssembler] = new BuildPrototypeDefinition(BuildPrototypeKind.AmmoAssembler, "弹药组装器", new Color("FB923C"), "在内部持续生产弹药，直接喂给炮塔。"),
        [BuildPrototypeKind.GunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.GunTurret, "机枪炮塔", new Color("CBD5E1"), "会跟随移动工厂整体旋转，对世界中的敌人自动转向并射击。"),
        [BuildPrototypeKind.HeavyGunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.HeavyGunTurret, "重型炮塔", new Color("E2E8F0"), "占据 2x2 内部格子，消耗高速弹药并发射独立炮弹。"),
        [BuildPrototypeKind.OutputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.OutputPort, "输出端口", new Color("FB923C"), "将已经封包的舱内货物送往世界网格。"),
        [BuildPrototypeKind.InputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.InputPort, "输入端口", new Color("60A5FA"), "把世界封装物流导入舱内，再由解包模块转换为维护层可理解的供料。"),
        [BuildPrototypeKind.MiningInputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.MiningInputPort, "采矿输入端口", new Color("34D399"), "部署后会在工厂外侧展开完整采矿预览；散装原料会先穿过舱壳，再交给解包模块处理。"),
        [BuildPrototypeKind.Generator] = new BuildPrototypeDefinition(BuildPrototypeKind.Generator, "发电机", new Color("FB923C"), "消耗煤炭发电，为移动工厂内部设备提供基础电力。"),
        [BuildPrototypeKind.PowerPole] = new BuildPrototypeDefinition(BuildPrototypeKind.PowerPole, "电线杆", new Color("FDE68A"), "延伸移动工厂内部的供电覆盖，并可预览连线。"),
        [BuildPrototypeKind.Smelter] = new BuildPrototypeDefinition(BuildPrototypeKind.Smelter, "熔炉", new Color("CBD5E1"), "消耗电力把矿石炼成铁板，便于在内部试配生产链。"),
        [BuildPrototypeKind.Assembler] = new BuildPrototypeDefinition(BuildPrototypeKind.Assembler, "组装机", new Color("67E8F9"), "消耗中间品和电力，在移动工厂内部验证真实配方。")
    };

    private GridManager? _grid;
    private SimulationController? _simulation;
    private FactoryCameraRig? _cameraRig;
    private MobileFactoryHud? _hud;
    private FactoryPlayerController? _playerController;
    private FactoryPlayerHud? _playerHud;
    private Node3D? _resourceOverlayRoot;
    private Node3D? _structureRoot;
    private Node3D? _enemyRoot;
    private Node3D? _worldPreviewRoot;
    private Node3D? _interiorPreviewRoot;
    private Node3D? _interiorPortHintRoot;
    private Node3D? _interiorBlueprintPreviewRoot;
    private Node3D? _interiorBlueprintGhostPreviewRoot;
    private Node3D? _interiorPowerLinkOverlayRoot;
    private Camera3D? _editorCamera;
    private FactoryCombatDirector? _combatDirector;
    private readonly List<MeshInstance3D> _worldPreviewFootprintMeshes = new();
    private readonly List<Node3D> _worldPreviewPortMeshes = new();
    private readonly List<MeshInstance3D> _worldPreviewMiningMeshes = new();
    private readonly List<MeshInstance3D> _worldPreviewMiningLinkMeshes = new();
    private Node3D? _worldPreviewFacingArrow;
    private MeshInstance3D? _interiorPreviewCell;
    private Node3D? _interiorPreviewArrow;
    private MeshInstance3D? _interiorPreviewPowerRange;
    private readonly List<MeshInstance3D> _interiorPreviewBoundaryMeshes = new();
    private readonly List<MeshInstance3D> _interiorPreviewExteriorMeshes = new();
    private readonly List<Node3D> _interiorPortHintMeshes = new();
    private readonly List<FactoryPortPreviewMarker> _cachedInteriorPortMarkers = new();
    private readonly List<MeshInstance3D> _interiorBlueprintPreviewMeshes = new();
    private readonly List<FactoryStructure> _interiorBlueprintPreviewGhosts = new();
    private readonly List<MeshInstance3D> _interiorPowerLinkDashes = new();
    private FactoryBlueprintSiteAdapter? _interiorBlueprintSite;

    private MobileFactoryInstance? _mobileFactory;
    private readonly List<MobileFactoryInstance> _backgroundFactories = new();
    private readonly List<MobileFactoryScenarioActorController> _backgroundControllers = new();
    private readonly List<Label3D> _factoryLabels = new();
    private readonly Dictionary<MobileFactoryInstance, Label3D> _factoryLabelMap = new();
    private readonly List<SinkStructure> _scenarioSinks = new();
    private SinkStructure? _sinkA;
    private SinkStructure? _sinkB;
    private MobileFactoryControlMode _controlMode = MobileFactoryControlMode.Player;
    private FacingDirection _selectedDeployFacing = FacingDirection.East;
    private Vector2I _hoveredAnchor;
    private bool _hasHoveredAnchor;
    private bool _canDeployCurrentAnchor;
    private MobileFactoryDeploymentEvaluation? _currentDeployEvaluation;
    private FactoryStatusTone _worldStatusTone = FactoryStatusTone.Positive;
    private string _worldPreviewMessage = "移动工厂待命中。";
    private string? _worldEventMessage;
    private bool _worldEventPositive;
    private float _worldEventTimer;

    private BuildPrototypeKind _selectedInteriorKind = BuildPrototypeKind.Belt;
    private FacingDirection _selectedInteriorFacing = FacingDirection.East;
    private FactoryInteractionMode _interiorInteractionMode = FactoryInteractionMode.Interact;
    private FactoryStructure? _selectedInteriorStructure;
    private FactoryStructure? _hoveredInteriorStructure;
    private Vector2I _hoveredInteriorCell;
    private Vector2I _cachedInteriorPortCell;
    private Rect2I _cachedInteriorPortVisibleRect;
    private bool _hasHoveredInteriorCell;
    private bool _hasCachedInteriorPortMarkers;
    private bool _canPlaceInteriorCell;
    private bool _canDeleteInteriorCell;
    private bool _deleteInteriorDragActive;
    private Vector2I _deleteInteriorDragStartCell;
    private Vector2I _deleteInteriorDragCurrentCell;
    private FactoryBlueprintWorkflowMode _blueprintMode;
    private bool _interiorBlueprintSelectionDragActive;
    private bool _hasInteriorBlueprintSelectionRect;
    private Vector2I _interiorBlueprintSelectionStartCell;
    private Vector2I _interiorBlueprintSelectionCurrentCell;
    private Rect2I _interiorBlueprintSelectionRect;
    private FactoryBlueprintRecord? _pendingBlueprintCapture;
    private FactoryBlueprintApplyPlan? _interiorBlueprintPlan;
    private FacingDirection _interiorBlueprintRotation = FacingDirection.East;
    private string _interiorPreviewMessage = "按 F 展开内部编辑区，然后把鼠标移入右侧区域开始调整移动工厂内部布局。";
    private bool _editorOpen;
    private bool _hoveringEditorPane;
    private bool _hoveringEditorViewport;
    private Vector2 _mousePosition = Vector2.Zero;
    private Vector2 _editorCameraLocalOffset = Vector2.Zero;
    private string? _selectedPlayerItemInventoryId;
    private Vector2I _selectedPlayerItemSlot;
    private bool _hasSelectedPlayerItemSlot;
    private bool _playerInteriorPlacementArmed;
    private readonly FactoryPlayerInventorySelectionState _playerSelectionState = new();
    private BuildPrototypeKind? _cachedInteriorPortKind;
    private FacingDirection _cachedInteriorPortFacing = FacingDirection.East;
    private int _cachedInteriorPortRevision = -1;

    public override void _Ready()
    {
        EnsureInputActions();
        BuildSceneGraph();
        ConfigureGameplay();
        CreateWorldLoops();
        SpawnMobileFactory();
        SpawnPlayerController();
        PullFactoryStatusMessage();
        UpdateWorldStatusMessage(0.0);
        UpdateHud();

        if (HasFocusedSmokeTestFlag())
        {
            CallDeferred(nameof(RunSmokeChecks));
        }
        else if (HasRuntimeSaveSmokeTestFlag())
        {
            CallDeferred(nameof(RunRuntimeSaveSmokeChecks));
        }
        else if (HasMiningPortSmokeTestFlag())
        {
            CallDeferred(nameof(RunMiningPortSmokeChecks));
        }
        else if (HasLargeScenarioSmokeTestFlag() && UseLargeTestScenario)
        {
            CallDeferred(nameof(RunLargeScenarioSmokeChecks));
        }
    }

    public override void _Process(double delta)
    {
        _mousePosition = GetViewport().GetMousePosition();
        HandleGlobalCommands();
        if (_controlMode == MobileFactoryControlMode.Player)
        {
            _playerController?.ApplyMovement(GetPlayerMovementBounds(), delta, allowInput: true);
        }

        _mobileFactory?.UpdateRuntime(delta);
        foreach (var factory in _backgroundFactories)
        {
            factory.UpdateRuntime(delta);
        }

        foreach (var controller in _backgroundControllers)
        {
            controller.Update(delta);
        }

        PullFactoryStatusMessage();
        UpdatePaneFocus();
        HandleWorldControlInput(delta);
        UpdateHoveredAnchor();
        UpdateHoveredInteriorCell();
        UpdateWorldPreview();
        UpdateInteriorPreview();
        UpdateWorldStatusMessage(delta);
        UpdateStructureVisuals();
        UpdateInteriorPowerVisuals();
        UpdateFactoryLabels();
        UpdateEditorCamera();
        UpdateCameraTracking();
        UpdateHud();
        UpdateCursorShape();

        if (_editorOpen && _interiorInteractionMode == FactoryInteractionMode.Delete && _deleteInteriorDragActive && _hasHoveredInteriorCell)
        {
            _deleteInteriorDragCurrentCell = _hoveredInteriorCell;
        }

        if (_editorOpen && _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection && _interiorBlueprintSelectionDragActive && _hasHoveredInteriorCell)
        {
            _interiorBlueprintSelectionCurrentCell = _hoveredInteriorCell;
        }

        if (_editorOpen && _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection && !IsBlueprintSelectionModifierHeld())
        {
            ExitInteriorBlueprintCaptureMode(preserveExistingSelection: true);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey hotbarKeyEvent
            && hotbarKeyEvent.Pressed
            && !hotbarKeyEvent.Echo
            && !_editorOpen
            && TryMapHotbarKey(hotbarKeyEvent.Keycode, out var hotbarIndex))
        {
            HandlePlayerHotbarPressed(hotbarIndex);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (HandleEditorKeyInput(keyEvent))
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is not InputEventMouseButton mouseButton)
        {
            return;
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection && CanUseEditorInput())
        {
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                CancelInteriorBlueprintWorkflow();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    BeginInteriorBlueprintSelection();
                }
                else
                {
                    CompleteInteriorBlueprintSelection();
                }

                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && CanUseEditorInput())
        {
            if (!mouseButton.Pressed)
            {
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                ConfirmInteriorBlueprintApply();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                CancelInteriorBlueprintWorkflow();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (_interiorInteractionMode == FactoryInteractionMode.Interact
            && CanUseEditorViewportInput()
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed
            && mouseButton.ShiftPressed)
        {
            StartInteriorBlueprintCapture();
            BeginInteriorBlueprintSelection();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (CanUseEditorViewportInput())
        {
            if (_interiorInteractionMode == FactoryInteractionMode.Delete)
            {
                if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
                {
                    EnterInteriorInteractionMode();
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (mouseButton.ButtonIndex == MouseButton.Left)
                {
                    if (mouseButton.Pressed)
                    {
                        HandleEditorDeletePrimaryPress(mouseButton.ShiftPressed);
                    }
                    else
                    {
                        HandleEditorDeletePrimaryRelease();
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
                HandleEditorPrimaryClick();
                GetViewport().SetInputAsHandled();
            }

            if (mouseButton.ButtonIndex == MouseButton.Right && _hasHoveredInteriorCell)
            {
                HandleEditorSecondaryClick();
                GetViewport().SetInputAsHandled();
            }

            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                AdjustEditorZoom(-0.45f);
                GetViewport().SetInputAsHandled();
            }

            if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                AdjustEditorZoom(0.45f);
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (CanUseEditorInput())
        {
            if (_interiorInteractionMode == FactoryInteractionMode.Delete && mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                EnterInteriorInteractionMode();
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (CanUseWorldInput() && mouseButton.ButtonIndex == MouseButton.Left && _controlMode == MobileFactoryControlMode.DeployPreview)
        {
            ConfirmDeployPreview();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildSceneGraph()
    {
        var scaffold = FactoryDemoSceneScaffold.Build(
            this,
            GetWorldMinCell(),
            GetWorldMaxCell(),
            new[]
            {
                new FactoryDemoRootSpec("resource", "MobileResourceOverlayRoot"),
                new FactoryDemoRootSpec("structure", "MobileDemoStructures"),
                new FactoryDemoRootSpec("enemy", "MobileEnemyRoot"),
                new FactoryDemoRootSpec("world-preview", "WorldPreviewRoot"),
                new FactoryDemoRootSpec("interior-preview", "InteriorPreviewRoot", false),
                new FactoryDemoRootSpec("interior-port-hints", "InteriorPortHintRoot", false),
                new FactoryDemoRootSpec("interior-power-links", "InteriorPowerLinkOverlayRoot", false),
                new FactoryDemoRootSpec("interior-blueprint-preview", "InteriorBlueprintPreviewRoot", false),
                new FactoryDemoRootSpec("interior-blueprint-ghost-preview", "InteriorBlueprintGhostPreviewRoot", false)
            },
            combatDirectorName: "MobileCombatDirector");

        if (UseLargeTestScenario)
        {
            AddChild(CreateScenarioLandmarks());
        }

        _resourceOverlayRoot = scaffold.GetRoot("resource");
        _structureRoot = scaffold.GetRoot("structure");
        _enemyRoot = scaffold.GetRoot("enemy");
        _worldPreviewRoot = scaffold.GetRoot("world-preview");
        CreateWorldPreviewVisuals(4, 1);

        _interiorPreviewRoot = scaffold.GetRoot("interior-preview");
        CreateInteriorPreviewVisuals();

        _interiorPortHintRoot = scaffold.GetRoot("interior-port-hints");
        _interiorPowerLinkOverlayRoot = scaffold.GetRoot("interior-power-links");
        _interiorBlueprintPreviewRoot = scaffold.GetRoot("interior-blueprint-preview");
        _interiorBlueprintGhostPreviewRoot = scaffold.GetRoot("interior-blueprint-ghost-preview");
        _simulation = scaffold.Simulation;
        _combatDirector = scaffold.CombatDirector;
        _cameraRig = scaffold.CameraRig;
        _playerHud = scaffold.PlayerHud;

        _hud = new MobileFactoryHud
        {
            UseLargeScenarioWorkspaces = UseLargeTestScenario
        };
        _hud.EditorPaletteSelected += OnEditorPaletteSelected;
        _hud.EditorRotateRequested += OnEditorRotateRequested;
        _hud.FactoryCommandModeToggleRequested += ToggleFactoryCommandMode;
        _hud.ObserverModeToggleRequested += ToggleObserverMode;
        _hud.DeployModeToggleRequested += ToggleDeployPreview;
        _hud.EditorDetailInventoryMoveRequested += HandleEditorDetailInventoryMoveRequested;
        _hud.EditorDetailInventoryTransferRequested += HandleEditorDetailInventoryTransferRequested;
        _hud.EditorDetailRecipeSelected += HandleEditorDetailRecipeSelected;
        _hud.EditorDetailActionRequested += HandleEditorDetailActionRequested;
        _hud.EditorDetailClosed += HandleEditorDetailClosed;
        _hud.BlueprintCaptureFullRequested += CaptureCurrentInteriorBlueprint;
        _hud.BlueprintRuntimeSaveRequested += name => HandleInteriorBlueprintSaveRequested(name, FactoryBlueprintPersistenceTarget.Runtime);
        _hud.BlueprintSourceSaveRequested += name => HandleInteriorBlueprintSaveRequested(name, FactoryBlueprintPersistenceTarget.Source);
        _hud.WorldMapSaveRequested += HandleWorldMapSaveRequested;
        _hud.InteriorMapSaveRequested += HandleInteriorMapSaveRequested;
        _hud.WorldMapSourceSaveRequested += HandleWorldMapSourceSaveRequested;
        _hud.InteriorMapSourceSaveRequested += HandleInteriorMapSourceSaveRequested;
        _hud.RuntimeSaveRequested += HandleRuntimeSaveRequested;
        _hud.RuntimeLoadRequested += HandleRuntimeLoadRequested;
        _hud.RuntimeSaveLibraryRefreshRequested += RefreshRuntimeSaveLibrary;
        _hud.BlueprintSelected += HandleInteriorBlueprintSelected;
        _hud.BlueprintApplyRequested += EnterInteriorBlueprintApplyMode;
        _hud.BlueprintConfirmRequested += ConfirmInteriorBlueprintApply;
        _hud.BlueprintDeleteRequested += HandleInteriorBlueprintDeleteRequested;
        _hud.BlueprintCancelRequested += CancelInteriorBlueprintWorkflow;
        _hud.WorkspaceSelected += HandleHudWorkspaceSelected;
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
            new Vector2I(GetWorldMinCell(), GetWorldMinCell()),
            new Vector2I(GetWorldMaxCell(), GetWorldMaxCell()),
            FactoryConstants.CellSize);

        _simulation!.Configure(_grid);
        _combatDirector?.Configure(_simulation, _enemyRoot!);
        var cameraPadding = UseLargeTestScenario ? 6.0f : 4.0f;
        _cameraRig!.ConfigureBounds(_grid.GetWorldMin() + Vector2.One * cameraPadding, _grid.GetWorldMax() - Vector2.One * cameraPadding);
        if (UseLargeTestScenario)
        {
            SeedMobileWorldResourceDeposits();
            RebuildMobileResourceOverlayVisuals();
        }

        _hud!.EditorViewport.World3D = GetWorld3D();
        _editorCamera = new Camera3D
        {
            Name = "InteriorEditorCamera",
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 3.9f,
            Near = 0.05f,
            Far = 50.0f,
            Current = true
        };
        _editorCamera.SetCullMaskValue(InteriorRenderLayer, true);
        _editorCamera.SetCullMaskValue(HullRenderLayer, false);
        _editorCamera.RotationDegrees = new Vector3(-90.0f, 0.0f, 0.0f);
        _hud.EditorViewport.AddChild(_editorCamera);
    }

    private void CreateWorldLoops()
    {
        _scenarioSinks.Clear();

        if (UseLargeTestScenario)
        {
            CreateLargeScenarioWorld();
            return;
        }

        LoadFocusedWorldMap();
        RebuildMobileResourceOverlayVisuals();
        ConfigureWorldCombatScenarios();
        _simulation!.RebuildTopology();
    }

    private void SpawnMobileFactory()
    {
        _backgroundFactories.Clear();
        _backgroundControllers.Clear();
        ClearFactoryLabels();

        if (UseLargeTestScenario)
        {
            SpawnLargeScenarioFactories();
        }
        else
        {
            _mobileFactory = new MobileFactoryInstance(
                "demo-mobile-factory",
                _structureRoot!,
                _simulation!,
                MobileFactoryScenarioLibrary.CreateFocusedDemoProfile(),
                MobileFactoryScenarioLibrary.CreateFocusedDemoPreset());
            ApplyFocusedInteriorMapRuntimeState();
        }

        if (_mobileFactory is not null)
        {
            if (!_factoryLabelMap.ContainsKey(_mobileFactory))
            {
                AddFactoryLabel(_mobileFactory, "玩家工厂", new Color(0.72f, 0.88f, 1.0f, 0.98f));
            }

            PrimeMobileFactoryShowcase(_mobileFactory);
            _selectedDeployFacing = _mobileFactory.TransitFacing;
            CreateWorldPreviewVisuals(
                _mobileFactory.Profile.FootprintOffsetsEast.Count,
                Mathf.Max(1, _mobileFactory.Profile.AttachmentMounts.Count));
            UpdateInteriorPreviewSizing();
            _interiorBlueprintSite = CreateInteriorBlueprintSiteAdapter();
        }
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
    }

    private Vector3 FindPlayerSpawnPosition()
    {
        var anchor = _mobileFactory?.WorldFocusPoint ?? Vector3.Zero;
        if (_grid is null)
        {
            return anchor + new Vector3(-3.0f, 0.0f, 3.0f);
        }

        var preferredCell = _grid.WorldToCell(new Vector3(anchor.X - (FactoryConstants.CellSize * 3.0f), 0.0f, anchor.Z + (FactoryConstants.CellSize * 3.0f)));
        for (var radius = 0; radius <= 18; radius++)
        {
            for (var y = preferredCell.Y - radius; y <= preferredCell.Y + radius; y++)
            {
                for (var x = preferredCell.X - radius; x <= preferredCell.X + radius; x++)
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

        return anchor + new Vector3(-3.0f, 0.0f, 3.0f);
    }

    private Rect2 GetPlayerMovementBounds()
    {
        if (_grid is null)
        {
            return new Rect2(-8.0f, -8.0f, 16.0f, 16.0f);
        }

        var min = _grid.GetWorldMin() + Vector2.One * 1.0f;
        var max = _grid.GetWorldMax() - Vector2.One * 1.0f;
        return new Rect2(min, max - min);
    }

    private FactoryStructure? PlaceWorldStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (_grid is null || _structureRoot is null || _simulation is null || !_grid.CanPlaceStructure(kind, cell, facing, out _))
        {
            return null;
        }

        var structure = FactoryStructureFactory.Create(kind, new FactoryStructurePlacement(_grid, cell, facing));
        _structureRoot.AddChild(structure);
        _grid.PlaceStructure(structure);
        _simulation.RegisterStructure(structure);
        return structure;
    }

    private void CreateLargeScenarioWorld()
    {
        var focused = MobileFactoryScenarioLibrary.CreateFocusedDemoProfile();
        var heavy = MobileFactoryScenarioLibrary.CreateHeavyProfile();
        var medium = MobileFactoryScenarioLibrary.CreateMediumProfile();
        var compact = MobileFactoryScenarioLibrary.CreateCompactProfile();

        _sinkA = CreatePreparedOutputLine(heavy, new Vector2I(-15, -6), FacingDirection.East, 3);
        CreatePreparedMountOutputLine(heavy, new Vector2I(-15, -6), FacingDirection.East, "east-output-aux", 2);
        CreatePreparedInputLine(heavy, new Vector2I(-15, -6), FacingDirection.East, 4);
        CreatePreparedOutputLine(compact, new Vector2I(6, -6), FacingDirection.East, 2);
        CreatePreparedOutputLine(compact, new Vector2I(10, 2), FacingDirection.East, 2);
        _sinkB = CreatePreparedOutputLine(medium, new Vector2I(-4, 7), FacingDirection.East, 2);
        CreatePreparedMountOutputLine(medium, new Vector2I(-4, 7), FacingDirection.East, "east-output-aux", 1);
        CreatePreparedInputLine(medium, new Vector2I(-4, 7), FacingDirection.East, 3);
        CreatePreparedOutputLine(compact, new Vector2I(1, 9), FacingDirection.East, 2);
        CreatePreparedOutputLine(compact, new Vector2I(-9, 10), FacingDirection.East, 2);
        CreatePreparedOutputLine(focused, new Vector2I(-12, 3), FacingDirection.East, 2);
        CreatePreparedMountOutputLine(focused, new Vector2I(-12, 3), FacingDirection.East, "east-output-aux", 1);
        CreatePreparedOutputLine(medium, new Vector2I(4, 10), FacingDirection.East, 2);
        CreatePreparedInputLine(focused, new Vector2I(-12, 3), FacingDirection.East, 3);
        CreateReceivingStationLandmark(new Vector2I(12, 14));
        CreateReceivingStationLandmark(new Vector2I(-19, 14));
        CreateReceivingStationLandmark(new Vector2I(16, -15));
        CreateReceivingStationLandmark(new Vector2I(-18, -15));
        CreateReceivingStationLandmark(new Vector2I(0, 15));
        CreateReceivingStationLandmark(new Vector2I(17, 4));
        ConfigureWorldCombatScenarios();

        _simulation!.RebuildTopology();
    }

    private void SpawnLargeScenarioFactories()
    {
        foreach (var actor in MobileFactoryScenarioLibrary.CreateLargeScenarioActors())
        {
            var instance = new MobileFactoryInstance(actor.ActorId, _structureRoot!, _simulation!, actor.Profile, actor.InteriorPreset);
            instance.SetTransitPose(actor.TransitPosition, actor.TransitFacing);

            if (actor.InitialDeployAnchor is Vector2I initialAnchor)
            {
                instance.TryDeploy(_grid!, initialAnchor, actor.InitialDeployFacing);
            }

            if (actor.IsPlayerControlled)
            {
                _mobileFactory = instance;
            }
            else
            {
                _backgroundFactories.Add(instance);
                if (actor.RoutePoints.Count > 0)
                {
                    _backgroundControllers.Add(new MobileFactoryScenarioActorController(_grid!, instance, actor));
                }
            }

            PrimeMobileFactoryShowcase(instance);
            AddFactoryLabel(instance, actor.DisplayLabel, actor.LabelColor);
        }
    }

    private void PrimeMobileFactoryShowcase(MobileFactoryInstance factory)
    {
        if (_simulation is null)
        {
            return;
        }

        switch (factory.InteriorPreset.Id)
        {
            case "focused-dual-logistics":
                if (HasFocusedSmokeTestFlag())
                {
                    GD.Print(
                        $"MOBILE_FACTORY_FOCUSED_PRESET cells=" +
                        $"turret={factory.TryGetInteriorStructure(FocusedTurretCell, out _)} " +
                        $"smelter={factory.TryGetInteriorStructure(FocusedSmelterCell, out _)} " +
                        $"assembler={factory.TryGetInteriorStructure(FocusedAssemblerCell, out _)} " +
                        $"ammo={factory.TryGetInteriorStructure(FocusedAmmoAssemblerCell, out _)} " +
                        $"ironStorage={factory.TryGetInteriorStructure(FocusedIronBufferCell, out _)} " +
                        $"wireStorage={factory.TryGetInteriorStructure(FocusedWireBufferCell, out _)} " +
                        $"depot={factory.TryGetInteriorStructure(FocusedDepotAnchorCell, out _)} " +
                        $"count={CountEditableInteriorStructures()}");
                }

                break;

            case "expedition-input-verification":
                if (factory.TryGetInteriorStructure(new Vector2I(1, 0), out var expeditionGeneratorStructure)
                    && expeditionGeneratorStructure is GeneratorStructure expeditionGenerator)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        expeditionGenerator.TryReceiveProvidedItem(
                            _simulation.CreateItem(BuildPrototypeKind.Generator, FactoryItemKind.Coal),
                            expeditionGenerator.Cell + Vector2I.Left,
                            _simulation);
                    }
                }

                if (factory.TryGetInteriorStructure(new Vector2I(2, 1), out var expeditionSmelterStructure)
                    && expeditionSmelterStructure is SmelterStructure expeditionSmelter)
                {
                    expeditionSmelter.TrySetDetailRecipe("iron-smelting");
                    for (var i = 0; i < 4; i++)
                    {
                        expeditionSmelter.TryReceiveProvidedItem(
                            _simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronOre),
                            expeditionSmelter.Cell + Vector2I.Left,
                            _simulation);
                    }
                }

                if (factory.TryGetInteriorStructure(new Vector2I(2, 2), out var expeditionAssemblerStructure)
                    && expeditionAssemblerStructure is AssemblerStructure expeditionAssembler)
                {
                    expeditionAssembler.TrySetDetailRecipe("gear");
                }

                break;

            case "wide-buffer-loop":
                if (factory.TryGetInteriorStructure(new Vector2I(1, 0), out var wideGeneratorStructure)
                    && wideGeneratorStructure is GeneratorStructure wideGenerator)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        wideGenerator.TryReceiveProvidedItem(
                            _simulation.CreateItem(BuildPrototypeKind.Generator, FactoryItemKind.Coal),
                            wideGenerator.Cell + Vector2I.Left,
                            _simulation);
                    }
                }

                if (factory.TryGetInteriorStructure(new Vector2I(3, 3), out var wideAmmoAssemblerStructure)
                    && wideAmmoAssemblerStructure is AmmoAssemblerStructure wideAmmoAssembler)
                {
                    wideAmmoAssembler.TrySetDetailRecipe("standard-ammo");
                }

                if (factory.TryGetInteriorStructure(new Vector2I(0, 3), out var wideIronBufferStructure)
                    && wideIronBufferStructure is StorageStructure wideIronBuffer)
                {
                    for (var i = 0; i < 6; i++)
                    {
                        wideIronBuffer.TryReceiveProvidedItem(
                            _simulation.CreateItem(BuildPrototypeKind.Smelter, FactoryItemKind.IronPlate),
                            wideIronBuffer.Cell + Vector2I.Left,
                            _simulation);
                    }
                }

                if (factory.TryGetInteriorStructure(new Vector2I(4, 1), out var wideWireBufferStructure)
                    && wideWireBufferStructure is StorageStructure wideWireBuffer)
                {
                    for (var i = 0; i < 6; i++)
                    {
                        wideWireBuffer.TryReceiveProvidedItem(
                            _simulation.CreateItem(BuildPrototypeKind.Assembler, FactoryItemKind.CopperWire),
                            wideWireBuffer.Cell + Vector2I.Left,
                            _simulation);
                    }
                }

                if (factory.TryGetInteriorStructure(new Vector2I(5, 0), out var wideTurretStructure)
                    && wideTurretStructure is GunTurretStructure wideTurret)
                {
                    for (var i = 0; i < 8; i++)
                    {
                        wideTurret.TryReceiveProvidedItem(
                            _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.AmmoMagazine),
                            wideTurret.Cell + Vector2I.Left,
                            _simulation);
                    }
                }

                break;
        }
    }

    private void PrimeFocusedOutputPorts(MobileFactoryInstance factory)
    {
        if (_simulation is null || factory.InteriorPreset.Id != "focused-dual-logistics")
        {
            return;
        }

        if (factory.TryGetInteriorStructure(new Vector2I(7, 1), out var mainPortStructure)
            && mainPortStructure is MobileFactoryOutputPortStructure mainPort)
        {
            for (var index = 0; index < 3; index++)
            {
                mainPort.TryReceiveProvidedItem(
                    _simulation.CreateItem(BuildPrototypeKind.Assembler, FactoryItemKind.Gear),
                    mainPort.Cell - FactoryDirection.ToCellOffset(mainPort.Facing),
                    _simulation);
            }
        }

        if (factory.TryGetInteriorStructure(new Vector2I(7, 4), out var auxPortStructure)
            && auxPortStructure is MobileFactoryOutputPortStructure auxPort)
        {
            for (var index = 0; index < 3; index++)
            {
                auxPort.TryReceiveProvidedItem(
                    _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.AmmoMagazine),
                    auxPort.Cell - FactoryDirection.ToCellOffset(auxPort.Facing),
                    _simulation);
            }
        }
    }

    private void PrimeScenarioOutputPorts(MobileFactoryInstance factory)
    {
        if (_simulation is null)
        {
            return;
        }

        var outputPorts = new List<MobileFactoryOutputPortStructure>();
        for (var y = factory.InteriorMinCell.Y; y <= factory.InteriorMaxCell.Y; y++)
        {
            for (var x = factory.InteriorMinCell.X; x <= factory.InteriorMaxCell.X; x++)
            {
                if (factory.TryGetInteriorStructure(new Vector2I(x, y), out var structure)
                    && structure is MobileFactoryOutputPortStructure outputPort
                    && !outputPorts.Contains(outputPort))
                {
                    outputPorts.Add(outputPort);
                }
            }
        }

        for (var index = 0; index < outputPorts.Count; index++)
        {
            var outputPort = outputPorts[index];
            var item = index % 2 == 0
                ? _simulation.CreateItem(BuildPrototypeKind.Assembler, FactoryItemKind.Gear)
                : _simulation.CreateItem(BuildPrototypeKind.AmmoAssembler, FactoryItemKind.AmmoMagazine);
            outputPort.TryReceiveProvidedItem(
                item,
                outputPort.Cell - FactoryDirection.ToCellOffset(outputPort.Facing),
                _simulation);
        }
    }

    private SinkStructure? CreatePreparedOutputLine(MobileFactoryProfile profile, Vector2I anchorCell, FacingDirection facing, int beltCount)
    {
        var portCell = GetProfilePortCell(profile, anchorCell, facing, BuildPrototypeKind.OutputPort);
        var outboundFacing = GetProfilePortFacing(profile, facing, BuildPrototypeKind.OutputPort);
        var cursor = portCell;
        for (var i = 0; i < beltCount; i++)
        {
            cursor += FactoryDirection.ToCellOffset(outboundFacing);
            PlaceWorldStructure(BuildPrototypeKind.Belt, cursor, outboundFacing);
        }

        var sinkCell = cursor + FactoryDirection.ToCellOffset(outboundFacing);
        var sink = PlaceWorldStructure(BuildPrototypeKind.Sink, sinkCell, outboundFacing) as SinkStructure;
        if (sink is not null)
        {
            _scenarioSinks.Add(sink);
        }

        return sink;
    }

    private SinkStructure? CreatePreparedMountOutputLine(MobileFactoryProfile profile, Vector2I anchorCell, FacingDirection facing, string mountId, int beltCount)
    {
        var portCell = GetProfileMountPortCell(profile, anchorCell, facing, mountId);
        var outboundFacing = GetProfileMountFacing(profile, facing, mountId);
        var cursor = portCell;
        for (var i = 0; i < beltCount; i++)
        {
            cursor += FactoryDirection.ToCellOffset(outboundFacing);
            PlaceWorldStructure(BuildPrototypeKind.Belt, cursor, outboundFacing);
        }

        var sinkCell = cursor + FactoryDirection.ToCellOffset(outboundFacing);
        var sink = PlaceWorldStructure(BuildPrototypeKind.Sink, sinkCell, outboundFacing) as SinkStructure;
        if (sink is not null)
        {
            _scenarioSinks.Add(sink);
        }

        return sink;
    }

    private void CreatePreparedInputLine(MobileFactoryProfile profile, Vector2I anchorCell, FacingDirection facing, int beltCount)
    {
        var portCell = GetProfilePortCell(profile, anchorCell, facing, BuildPrototypeKind.InputPort);
        var outboundFacing = GetProfilePortFacing(profile, facing, BuildPrototypeKind.InputPort);
        var inboundFacing = FactoryDirection.Opposite(outboundFacing);
        var offset = FactoryDirection.ToCellOffset(outboundFacing);
        var cells = new List<Vector2I>(beltCount + 1);
        var cursor = portCell + offset;
        for (var i = 0; i < beltCount; i++)
        {
            cells.Add(cursor);
            cursor += offset;
        }

        if (cells.Count == 0)
        {
            return;
        }

        var drill = PlaceWorldStructure(BuildPrototypeKind.MiningDrill, cells[^1], inboundFacing);
        if (drill is null)
        {
            return;
        }
        for (var i = 0; i < cells.Count - 1; i++)
        {
            PlaceWorldStructure(BuildPrototypeKind.Belt, cells[i], inboundFacing);
        }
    }

    private void SeedMobileWorldResourceDeposits()
    {
        if (_grid is null)
        {
            return;
        }

        var deposits = UseLargeTestScenario
            ? new List<FactoryResourceDepositDefinition>
            {
                new FactoryResourceDepositDefinition(
                    "mobile_large_player_coal",
                    FactoryResourceKind.Coal,
                    "玩家工厂西侧煤带",
                    new Color("8B5A2B"),
                    BuildRectCells(new Vector2I(-17, 1), new Vector2I(5, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_north_iron",
                    FactoryResourceKind.IronOre,
                    "北侧试验铁矿带",
                    new Color("64748B"),
                    BuildRectCells(new Vector2I(-10, 8), new Vector2I(5, 4))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_player_iron",
                    FactoryResourceKind.IronOre,
                    "玩家部署铁带",
                    new Color("64748B"),
                    BuildRectCells(new Vector2I(-18, 3), new Vector2I(5, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_farwest_iron",
                    FactoryResourceKind.IronOre,
                    "远西空场铁矿区",
                    new Color("64748B"),
                    BuildRectCells(new Vector2I(-24, -18), new Vector2I(4, 4))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_farwest_stone",
                    FactoryResourceKind.StoneOre,
                    "远西石矿区",
                    new Color("A8A29E"),
                    BuildRectCells(new Vector2I(-24, -10), new Vector2I(4, 4))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_farwest_coal",
                    FactoryResourceKind.Coal,
                    "远西北煤层",
                    new Color("8B5A2B"),
                    BuildRectCells(new Vector2I(-24, 15), new Vector2I(4, 4))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_mid_quartz",
                    FactoryResourceKind.QuartzOre,
                    "中部石英带",
                    new Color("67E8F9"),
                    BuildRectCells(new Vector2I(-2, -4), new Vector2I(5, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_east_coal",
                    FactoryResourceKind.Coal,
                    "东侧空地煤带",
                    new Color("8B5A2B"),
                    BuildRectCells(new Vector2I(16, 10), new Vector2I(4, 4))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_east_sulfur",
                    FactoryResourceKind.SulfurOre,
                    "东侧硫矿带",
                    new Color("FDE047"),
                    BuildRectCells(new Vector2I(12, 14), new Vector2I(4, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_large_southeast_iron",
                    FactoryResourceKind.IronOre,
                    "东南试验铁矿区",
                    new Color("64748B"),
                    BuildRectCells(new Vector2I(18, -18), new Vector2I(4, 4)))
            }
            : new List<FactoryResourceDepositDefinition>
            {
                new FactoryResourceDepositDefinition(
                    "mobile_anchor_a_iron",
                    FactoryResourceKind.IronOre,
                    "A 锚点西侧铁带",
                    new Color("8B5A2B"),
                    BuildRectCells(new Vector2I(-10, -4), new Vector2I(5, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_anchor_b_iron",
                    FactoryResourceKind.IronOre,
                    "B 锚点西侧铁带",
                    new Color("64748B"),
                    BuildRectCells(new Vector2I(-2, 3), new Vector2I(5, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_anchor_b_quartz",
                    FactoryResourceKind.QuartzOre,
                    "B 锚点东侧石英带",
                    new Color("67E8F9"),
                    BuildRectCells(new Vector2I(6, 6), new Vector2I(4, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_anchor_a_stone",
                    FactoryResourceKind.StoneOre,
                    "A 锚点南侧石带",
                    new Color("A8A29E"),
                    BuildRectCells(new Vector2I(-16, -10), new Vector2I(4, 3))),
                new FactoryResourceDepositDefinition(
                    "mobile_farwest_iron",
                    FactoryResourceKind.IronOre,
                    "远西空场铁矿区",
                    new Color("64748B"),
                    BuildRectCells(new Vector2I(-20, -14), new Vector2I(4, 4))),
                new FactoryResourceDepositDefinition(
                    "mobile_northeast_coal",
                    FactoryResourceKind.Coal,
                    "东北空场煤层",
                    new Color("8B5A2B"),
                    BuildRectCells(new Vector2I(14, 12), new Vector2I(4, 4))),
                new FactoryResourceDepositDefinition(
                    "mobile_northeast_sulfur",
                    FactoryResourceKind.SulfurOre,
                    "东北硫矿带",
                    new Color("FDE047"),
                    BuildRectCells(new Vector2I(10, 14), new Vector2I(4, 3)))
            };

        _grid.SetResourceDeposits(deposits);
    }

    private void RebuildMobileResourceOverlayVisuals()
    {
        FactoryMapVisualSupport.RebuildResourceOverlay(
            _resourceOverlayRoot,
            _grid,
            "MobileResource_",
            0.86f,
            0.05f,
            0.025f,
            0.96f,
            "MobileResourceChip_",
            0.18f,
            0.12f,
            0.09f,
            0.16f,
            0.86f);
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

    private void CreateReceivingStationLandmark(Vector2I originCell)
    {
        PlaceWorldStructure(BuildPrototypeKind.LargeStorageDepot, originCell, FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Storage, originCell + new Vector2I(-2, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Inserter, originCell + new Vector2I(-1, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Inserter, originCell + new Vector2I(2, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(3, 0), FacingDirection.East);
        RegisterScenarioSink(PlaceWorldStructure(BuildPrototypeKind.Sink, originCell + new Vector2I(4, 0), FacingDirection.East) as SinkStructure);
    }

    private void AddFactoryLabel(MobileFactoryInstance factory, string labelText, Color color)
    {
        if (_structureRoot is null)
        {
            return;
        }

        var label = new Label3D
        {
            Text = labelText,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 34,
            Modulate = color,
            OutlineSize = 3
        };
        label.SetMeta("base_text", labelText);
        _structureRoot.AddChild(label);
        _factoryLabels.Add(label);
        _factoryLabelMap[factory] = label;
    }

    private void ClearFactoryLabels()
    {
        foreach (var label in _factoryLabels)
        {
            label.QueueFree();
        }

        _factoryLabels.Clear();
        _factoryLabelMap.Clear();
    }

    private void UpdateFactoryLabels()
    {
        if (_factoryLabelMap.Count == 0)
        {
            return;
        }

        foreach (var pair in _factoryLabelMap)
        {
            pair.Value.Position = pair.Key.WorldFocusPoint + new Vector3(0.0f, 1.6f, 0.0f);
            var baseText = pair.Value.GetMeta("base_text", string.Empty).AsString();
            pair.Value.Text = $"{baseText}\n{DescribeFactoryState(pair.Key)}";
        }
    }

    private static string DescribeFactoryState(MobileFactoryInstance factory)
    {
        return factory.State switch
        {
            MobileFactoryLifecycleState.Deployed => $"已部署 | {factory.Profile.DisplayName}",
            MobileFactoryLifecycleState.AutoDeploying => $"自动部署中 | {factory.Profile.DisplayName}",
            MobileFactoryLifecycleState.Recalling => $"回收中 | {factory.Profile.DisplayName}",
            _ => $"运输中 | {factory.Profile.DisplayName}"
        };
    }

    private static Vector2I GetProfilePortCell(MobileFactoryProfile profile, Vector2I anchorCell, FacingDirection facing, BuildPrototypeKind kind)
    {
        for (var i = 0; i < profile.AttachmentMounts.Count; i++)
        {
            var mount = profile.AttachmentMounts[i];
            if (mount.Allows(kind))
            {
                return anchorCell + FactoryDirection.RotateOffset(mount.WorldPortOffsetEast, facing);
            }
        }

        return anchorCell;
    }

    private static FacingDirection GetProfilePortFacing(MobileFactoryProfile profile, FacingDirection deployFacing, BuildPrototypeKind kind)
    {
        for (var i = 0; i < profile.AttachmentMounts.Count; i++)
        {
            var mount = profile.AttachmentMounts[i];
            if (mount.Allows(kind))
            {
                return FactoryDirection.RotateBy(mount.Facing, deployFacing);
            }
        }

        return deployFacing;
    }

    private static Vector2I GetProfileMountPortCell(MobileFactoryProfile profile, Vector2I anchorCell, FacingDirection facing, string mountId)
    {
        for (var i = 0; i < profile.AttachmentMounts.Count; i++)
        {
            var mount = profile.AttachmentMounts[i];
            if (mount.Id == mountId)
            {
                return anchorCell + FactoryDirection.RotateOffset(mount.WorldPortOffsetEast, facing);
            }
        }

        return anchorCell;
    }

    private static FacingDirection GetProfileMountFacing(MobileFactoryProfile profile, FacingDirection deployFacing, string mountId)
    {
        for (var i = 0; i < profile.AttachmentMounts.Count; i++)
        {
            var mount = profile.AttachmentMounts[i];
            if (mount.Id == mountId)
            {
                return FactoryDirection.RotateBy(mount.Facing, deployFacing);
            }
        }

        return deployFacing;
    }

    private void HandleGlobalCommands()
    {
        if (Input.IsActionJustPressed("toggle_mobile_editor"))
        {
            SetEditorOpenState(!_editorOpen);
            FocusFactoryForCurrentMode();
        }

        if (!CanUseWorldInput() || _mobileFactory is null)
        {
            return;
        }

        if (Input.IsActionJustPressed("toggle_factory_command"))
        {
            ToggleFactoryCommandMode();
        }

        if (Input.IsActionJustPressed("toggle_observer_mode"))
        {
            ToggleObserverMode();
        }

        if (Input.IsActionJustPressed("toggle_deploy_preview"))
        {
            ToggleDeployPreview();
        }

        if (Input.IsActionJustPressed("cancel_mobile_command"))
        {
            HandleCommandSlot(MobileFactoryCommandSlot.Cancel);
        }

        if (Input.IsActionJustPressed("mobile_factory_auxiliary_command"))
        {
            HandleCommandSlot(MobileFactoryCommandSlot.Auxiliary);
        }
    }

    private void HandleWorldControlInput(double delta)
    {
        if (!CanUseWorldInput() || _mobileFactory is null || _grid is null)
        {
            return;
        }

        if (_controlMode == MobileFactoryControlMode.DeployPreview)
        {
            if (Input.IsActionJustPressed("deploy_rotate_left"))
            {
                RotateDeployFacing(-1, "Q");
            }

            if (Input.IsActionJustPressed("deploy_rotate_right"))
            {
                RotateDeployFacing(1, "E");
            }

            return;
        }

        if (_controlMode != MobileFactoryControlMode.FactoryCommand || _mobileFactory.State != MobileFactoryLifecycleState.InTransit)
        {
            return;
        }

        var throttle = 0.0f;
        var turn = 0.0f;

        if (Input.IsActionPressed("factory_move_forward"))
        {
            throttle += 1.0f;
        }

        if (Input.IsActionPressed("factory_move_backward"))
        {
            throttle -= 1.0f;
        }

        if (Input.IsActionPressed("factory_turn_left"))
        {
            turn += 1.0f;
        }

        if (Input.IsActionPressed("factory_turn_right"))
        {
            turn -= 1.0f;
        }

        _mobileFactory.ApplyTransitInput(_grid, throttle, turn, delta);
    }

    private bool HandleEditorKeyInput(InputEventKey keyEvent)
    {
        if (!_editorOpen || _mobileFactory is null)
        {
            return false;
        }

        if (_blueprintMode != FactoryBlueprintWorkflowMode.None)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                CancelInteriorBlueprintWorkflow();
                return true;
            }

            if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
            {
                if (keyEvent.Keycode == Key.Q)
                {
                    RotateInteriorBlueprintPreview(-1);
                    return true;
                }

                if (keyEvent.Keycode == Key.E)
                {
                    RotateInteriorBlueprintPreview(1);
                    return true;
                }
            }

            return false;
        }

        if (keyEvent.Keycode == Key.X)
        {
            if (_interiorInteractionMode == FactoryInteractionMode.Delete)
            {
                EnterInteriorInteractionMode();
            }
            else
            {
                EnterInteriorDeleteMode();
            }

            return true;
        }

        if (keyEvent.Keycode == Key.Escape)
        {
            if (_interiorInteractionMode == FactoryInteractionMode.Build || _interiorInteractionMode == FactoryInteractionMode.Delete)
            {
                EnterInteriorInteractionMode();
            }
            else
            {
                SetEditorOpenState(false);
                FocusFactoryForCurrentMode();
            }
            return true;
        }

        if (!CanUseEditorInput())
        {
            return false;
        }

        if (keyEvent.Keycode == Key.Delete && (_interiorInteractionMode == FactoryInteractionMode.Build || _interiorInteractionMode == FactoryInteractionMode.Delete) && _hasHoveredInteriorCell && _hoveredInteriorStructure is not null)
        {
            RemoveInteriorStructure();
            return true;
        }

        var interiorPalette = GetInteriorHotkeyPalette();
        for (var i = 0; i < interiorPalette.Count && i < InteriorPaletteKeys.Length; i++)
        {
            if (keyEvent.Keycode != InteriorPaletteKeys[i])
            {
                continue;
            }

            SelectInteriorBuildKind(_selectedInteriorKind == interiorPalette[i] && _interiorInteractionMode == FactoryInteractionMode.Build
                ? null
                : interiorPalette[i]);
            return true;
        }

        if (keyEvent.Keycode == Key.Q)
        {
            _selectedInteriorFacing = FactoryDirection.RotateCounterClockwise(_selectedInteriorFacing);
            return true;
        }

        if (keyEvent.Keycode == Key.E)
        {
            _selectedInteriorFacing = FactoryDirection.RotateClockwise(_selectedInteriorFacing);
            return true;
        }

        return false;
    }

    private void UpdatePaneFocus()
    {
        _hoveringEditorPane = _editorOpen && (_hud?.IsPointerOverEditor(_mousePosition) ?? false);
        _hoveringEditorViewport = _editorOpen && (_hud?.IsPointerOverEditorViewport(_mousePosition) ?? false);
        var hoveringUi = IsPointerOverUi();

        if (_cameraRig is not null)
        {
            _cameraRig.AllowPanInput = !hoveringUi && CanUseWorldInput() && _controlMode == MobileFactoryControlMode.Observer;
            _cameraRig.AllowZoomInput = !hoveringUi && CanUseWorldInput();
            _cameraRig.FollowTargetEnabled = _controlMode == MobileFactoryControlMode.Player;
        }

        _hud?.SetPaneFocus(_editorOpen, _hoveringEditorPane);
        _hud?.SetEditorFocusHint(_hoveringEditorPane);
    }

    private void UpdateHoveredAnchor()
    {
        _hasHoveredAnchor = false;
        _canDeployCurrentAnchor = false;
        _currentDeployEvaluation = null;

        if (_editorOpen && _hoveringEditorPane)
        {
            return;
        }

        if (_controlMode != MobileFactoryControlMode.DeployPreview || _grid is null || _cameraRig is null || _mobileFactory is null)
        {
            return;
        }

        if (_mobileFactory.State != MobileFactoryLifecycleState.InTransit)
        {
            return;
        }

        if (!_cameraRig.TryProjectMouseToPlane(_mousePosition, out var worldPosition))
        {
            return;
        }

        _hoveredAnchor = _grid.WorldToCell(worldPosition);
        _hasHoveredAnchor = true;
        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, _hoveredAnchor, _selectedDeployFacing);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
    }

    private void UpdateHoveredInteriorCell()
    {
        _hasHoveredInteriorCell = false;
        _canPlaceInteriorCell = false;
        _canDeleteInteriorCell = false;
        _hoveredInteriorStructure = null;
        _interiorBlueprintPlan = null;
        _interiorPreviewMessage = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图框选：按住左键拖拽选择一片内部布局，松开后可保存。",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图预览：移动鼠标选择内部锚点，按 Q/E 或面板旋转按钮旋转，当前朝向 {FactoryDirection.ToLabel(_interiorBlueprintRotation)}。",
            _ => _interiorInteractionMode switch
            {
                FactoryInteractionMode.Build => "把鼠标移入右侧编辑区，可直接调整移动工厂内部布局。",
                FactoryInteractionMode.Delete => "删除模式：左键删除内部建筑，按住 Shift 左键拖拽可框选删除。",
                _ => "交互模式：把鼠标移入右侧编辑区，点击内部建筑查看状态，或按住 Shift 左键直接框选蓝图。"
            }
        };

        if (!_editorOpen || !_hoveringEditorViewport || _mobileFactory is null || _editorCamera is null)
        {
            if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
            {
                UpdateInteriorBlueprintPlan();
            }
            return;
        }

        if (!TryProjectEditorMouseToInterior(out var worldPosition))
        {
            return;
        }

        var cell = _mobileFactory.InteriorSite.WorldToCell(worldPosition);
        _hoveredInteriorCell = cell;
        _hasHoveredInteriorCell = _mobileFactory.InteriorSite.IsInBounds(cell);

        if (!_hasHoveredInteriorCell)
        {
            _interiorPreviewMessage = "当前鼠标不在移动工厂内部可编辑网格上。";
            if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
            {
                UpdateInteriorBlueprintPlan();
            }
            return;
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection)
        {
            if (_interiorBlueprintSelectionDragActive)
            {
                _interiorBlueprintSelectionCurrentCell = cell;
                var rect = GetDeleteRect(_interiorBlueprintSelectionStartCell, _interiorBlueprintSelectionCurrentCell);
                var selectedCount = CountInteriorStructuresInDeleteRect(_interiorBlueprintSelectionStartCell, _interiorBlueprintSelectionCurrentCell);
                _interiorPreviewMessage = $"蓝图框选：[{rect.Position.X},{rect.Position.Y}] - [{rect.End.X - 1},{rect.End.Y - 1}]，当前覆盖 {selectedCount} 个内部建筑。";
                return;
            }

            if (_hasInteriorBlueprintSelectionRect)
            {
                var selectedCount = CountInteriorStructuresInDeleteRect(_interiorBlueprintSelectionRect.Position, _interiorBlueprintSelectionRect.End - Vector2I.One);
                _interiorPreviewMessage = $"蓝图框选已完成：覆盖 {selectedCount} 个内部建筑，填写名称后保存。";
                return;
            }

            _interiorPreviewMessage = "蓝图框选：按住左键拖拽选择一片内部布局。";
            return;
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            UpdateInteriorBlueprintPlan();
            return;
        }

        _mobileFactory.TryGetInteriorStructure(cell, out _hoveredInteriorStructure);
        if (_interiorInteractionMode == FactoryInteractionMode.Interact)
        {
            _interiorPreviewMessage = _hoveredInteriorStructure is not null
                ? $"点击查看 {_hoveredInteriorStructure.DisplayName} 的状态。"
                : $"格 ({cell.X}, {cell.Y}) 当前为空。";
            return;
        }

        if (_interiorInteractionMode == FactoryInteractionMode.Delete)
        {
            if (_deleteInteriorDragActive)
            {
                _deleteInteriorDragCurrentCell = cell;
                var deletionCount = CountInteriorStructuresInDeleteRect(_deleteInteriorDragStartCell, _deleteInteriorDragCurrentCell);
                _canDeleteInteriorCell = deletionCount > 0;
                var rect = GetDeleteRect(_deleteInteriorDragStartCell, _deleteInteriorDragCurrentCell);
                _interiorPreviewMessage = $"删除模式：框选 [{rect.Position.X},{rect.Position.Y}] - [{rect.End.X - 1},{rect.End.Y - 1}]，将删除 {deletionCount} 个内部建筑。";
                return;
            }

            _canDeleteInteriorCell = _hoveredInteriorStructure is not null;
            _interiorPreviewMessage = _hoveredInteriorStructure is not null
                ? $"删除模式：左键删除 {_hoveredInteriorStructure.DisplayName}，Shift+左键拖拽可框选删除。"
                : $"删除模式：格 ({cell.X}, {cell.Y}) 当前为空。";
            return;
        }

        if (_mobileFactory.CanPlaceInterior(_selectedInteriorKind, cell, _selectedInteriorFacing))
        {
            _canPlaceInteriorCell = true;
            if (MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(_selectedInteriorKind)
                && _mobileFactory.TryGetAttachmentPreview(_selectedInteriorKind, cell, _selectedInteriorFacing, out _, out _, out _, out var attachmentPreviewMessage))
            {
                _interiorPreviewMessage = attachmentPreviewMessage;
            }
            else
            {
                _interiorPreviewMessage = DescribeInteriorPlacementPreview(_selectedInteriorKind, cell, _selectedInteriorFacing);
            }
            return;
        }

        if (MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(_selectedInteriorKind)
            && _mobileFactory.TryGetAttachmentPreview(_selectedInteriorKind, cell, _selectedInteriorFacing, out _, out _, out _, out var invalidAttachmentMessage))
        {
            _interiorPreviewMessage = invalidAttachmentMessage;
            return;
        }

        _interiorPreviewMessage = $"内部格 ({cell.X}, {cell.Y}) 已被占用，Delete 可拆除悬停结构，右键可退出建造模式。";
    }

    private MobileFactoryDeploymentEvaluation? GetCurrentDeploymentEvaluation()
    {
        if (_currentDeployEvaluation is not null)
        {
            return _currentDeployEvaluation;
        }

        if (_grid is null || _mobileFactory is null || !_hasHoveredAnchor)
        {
            return null;
        }

        _currentDeployEvaluation = _mobileFactory.EvaluateDeployment(_grid, _hoveredAnchor, _selectedDeployFacing);
        _canDeployCurrentAnchor = _currentDeployEvaluation.CanDeploy;
        return _currentDeployEvaluation;
    }

    private void UpdateWorldPreview()
    {
        if (_grid is null || _worldPreviewRoot is null || _mobileFactory is null)
        {
            return;
        }

        foreach (var mesh in _worldPreviewFootprintMeshes)
        {
            mesh.Visible = false;
        }

        foreach (var mesh in _worldPreviewPortMeshes)
        {
            mesh.Visible = false;
        }

        foreach (var mesh in _worldPreviewMiningMeshes)
        {
            mesh.Visible = false;
        }

        foreach (var mesh in _worldPreviewMiningLinkMeshes)
        {
            mesh.Visible = false;
        }

        if (_worldPreviewFacingArrow is not null)
        {
            _worldPreviewFacingArrow.Visible = false;
        }

        _worldPreviewRoot.Visible = _controlMode == MobileFactoryControlMode.DeployPreview && _hasHoveredAnchor && CanUseWorldInput();
        if (!_worldPreviewRoot.Visible)
        {
            return;
        }

        var evaluation = GetCurrentDeploymentEvaluation() ?? _mobileFactory.EvaluateDeployment(_grid, _hoveredAnchor, _selectedDeployFacing);
        var deployState = evaluation?.State ?? (_canDeployCurrentAnchor ? MobileFactoryDeployState.Valid : MobileFactoryDeployState.Blocked);
        var footprintColor = deployState == MobileFactoryDeployState.Blocked
            ? new Color(1.0f, 0.35f, 0.35f, 0.38f)
            : new Color(0.35f, 0.95f, 0.55f, 0.38f);
        var miningActiveColor = deployState == MobileFactoryDeployState.Blocked
            ? new Color(0.99f, 0.52f, 0.38f, 0.78f)
            : new Color(0.38f, 0.68f, 1.0f, 0.88f);
        var miningEligibleColor = deployState == MobileFactoryDeployState.Blocked
            ? new Color(0.99f, 0.52f, 0.38f, 0.42f)
            : new Color(0.38f, 0.68f, 1.0f, 0.44f);
        var miningInactiveColor = deployState == MobileFactoryDeployState.Blocked
            ? new Color(0.99f, 0.68f, 0.45f, 0.38f)
            : new Color(0.98f, 0.84f, 0.36f, 0.72f);

        var footprintCells = evaluation is not null
            ? new List<Vector2I>(evaluation.FootprintCells)
            : new List<Vector2I>(_mobileFactory.GetFootprintCells(_hoveredAnchor, _selectedDeployFacing));
        var seenPortCells = new HashSet<Vector2I>();
        var portPreviewEntries = new List<(Vector2I Cell, FacingDirection Facing, MobileFactoryAttachmentChannelType ChannelType)>();
        var miningPreviewEntries = new List<(Vector2I Cell, Vector2I PortCell, bool IsEligible, bool IsDeployed)>();
        if (evaluation is not null)
        {
            for (var attachmentIndex = 0; attachmentIndex < evaluation.AttachmentEvaluations.Count; attachmentIndex++)
            {
                var attachmentEvaluation = evaluation.AttachmentEvaluations[attachmentIndex];
                var channelType = attachmentEvaluation.Attachment.AttachmentDefinition.ChannelType;
                if (attachmentEvaluation.Attachment.Kind == BuildPrototypeKind.MiningInputPort)
                {
                    if (attachmentEvaluation.Attachment is MobileFactoryMiningInputPortStructure miningAttachment)
                    {
                        miningAttachment.DescribePreviewStakePlan(_grid, attachmentEvaluation.Projection, out var eligibleStakeCells, out var deployedStakeCells);
                        var eligibleCellSet = new HashSet<Vector2I>(eligibleStakeCells);
                        var deployedCellSet = new HashSet<Vector2I>(deployedStakeCells);
                        for (var cellIndex = 0; cellIndex < attachmentEvaluation.PreviewWorldCells.Count; cellIndex++)
                        {
                            var previewCell = attachmentEvaluation.PreviewWorldCells[cellIndex];
                            miningPreviewEntries.Add((
                                previewCell,
                                attachmentEvaluation.Projection.WorldPortCell,
                                eligibleCellSet.Contains(previewCell),
                                deployedCellSet.Contains(previewCell)));
                        }
                    }
                    continue;
                }

                var previewFacing = channelType == MobileFactoryAttachmentChannelType.ItemInput
                    ? FactoryDirection.Opposite(attachmentEvaluation.Projection.WorldFacing)
                    : attachmentEvaluation.Projection.WorldFacing;
                for (var cellIndex = 0; cellIndex < attachmentEvaluation.PreviewWorldCells.Count; cellIndex++)
                {
                    var previewCell = attachmentEvaluation.PreviewWorldCells[cellIndex];
                    if (!seenPortCells.Add(previewCell))
                    {
                        continue;
                    }

                    portPreviewEntries.Add((previewCell, previewFacing, channelType));
                }
            }
        }

        EnsureWorldPreviewVisualCapacity(footprintCells.Count, portPreviewEntries.Count, miningPreviewEntries.Count);

        var index = 0;
        foreach (var cell in footprintCells)
        {
            var mesh = _worldPreviewFootprintMeshes[index++];
            mesh.Visible = true;
            mesh.Position = _grid.CellToWorld(cell) + new Vector3(0.0f, 0.05f, 0.0f);
            FactoryPreviewOverlaySupport.ApplyPreviewColor(mesh, footprintColor);
        }

        index = 0;
        foreach (var entry in portPreviewEntries)
        {
            FactoryPreviewOverlaySupport.ConfigureDirectionalArrow(
                _worldPreviewPortMeshes[index++],
                _grid.CellToWorld(entry.Cell) + new Vector3(0.0f, 0.08f, 0.0f),
                entry.Facing,
                GetWorldPortPreviewColor(entry.ChannelType, deployState),
                1.02f);
        }

        index = 0;
        foreach (var entry in miningPreviewEntries)
        {
            var mesh = _worldPreviewMiningMeshes[index++];
            mesh.Visible = true;
            ConfigureMiningPreviewMarker(
                mesh,
                entry.Cell,
                entry.IsEligible,
                entry.IsDeployed,
                entry.IsDeployed
                    ? miningActiveColor
                    : entry.IsEligible
                        ? miningEligibleColor
                        : miningInactiveColor);
        }

        index = 0;
        foreach (var entry in miningPreviewEntries)
        {
            ConfigureMiningPreviewLink(
                _worldPreviewMiningLinkMeshes[index++],
                entry.PortCell,
                entry.Cell,
                entry.IsDeployed
                    ? miningActiveColor
                    : entry.IsEligible
                        ? miningEligibleColor
                        : miningInactiveColor);
        }

        if (_worldPreviewFacingArrow is not null)
        {
            FactoryPreviewOverlaySupport.ConfigureDirectionalArrow(
                _worldPreviewFacingArrow,
                GetPreviewFacingArrowPosition(_hoveredAnchor, _selectedDeployFacing),
                _selectedDeployFacing,
                GetWorldPortPreviewColor(MobileFactoryAttachmentChannelType.ItemOutput, deployState).Lightened(0.08f));
        }
    }

    private void UpdateInteriorPreview()
    {
        if (_mobileFactory is null || _interiorPreviewRoot is null || _interiorPreviewCell is null || _interiorPreviewArrow is null || _interiorPreviewPowerRange is null)
        {
            return;
        }

        UpdateInteriorBlueprintPreview();
        SetInteriorPortHintCount(0);
        _interiorPreviewPowerRange.Visible = false;

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            _interiorPreviewRoot.Visible = false;
            return;
        }

        _interiorPreviewRoot.Visible = _editorOpen
            && (HasRetainedInteriorBlueprintSelection()
                || (_hoveringEditorViewport
                    && ((_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection
                            && (_interiorBlueprintSelectionDragActive || _hasInteriorBlueprintSelectionRect || _hasHoveredInteriorCell))
                        || (_hasHoveredInteriorCell
                            && (_interiorInteractionMode == FactoryInteractionMode.Build
                                || _interiorInteractionMode == FactoryInteractionMode.Delete)))));
        if (!_interiorPreviewRoot.Visible)
        {
            return;
        }

        for (var i = 0; i < _interiorPreviewBoundaryMeshes.Count; i++)
        {
            _interiorPreviewBoundaryMeshes[i].Visible = false;
        }

        for (var i = 0; i < _interiorPreviewExteriorMeshes.Count; i++)
        {
            _interiorPreviewExteriorMeshes[i].Visible = false;
        }

        if (_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection || HasRetainedInteriorBlueprintSelection())
        {
            var start = _interiorBlueprintSelectionDragActive
                ? _interiorBlueprintSelectionStartCell
                : _hasInteriorBlueprintSelectionRect
                    ? _interiorBlueprintSelectionRect.Position
                    : _hoveredInteriorCell;
            var end = _interiorBlueprintSelectionDragActive
                ? _interiorBlueprintSelectionCurrentCell
                : _hasInteriorBlueprintSelectionRect
                    ? _interiorBlueprintSelectionRect.End - Vector2I.One
                    : _hoveredInteriorCell;
            var rect = GetDeleteRect(start, end);
            var minCell = rect.Position;
            var maxCell = rect.End - Vector2I.One;
            var minWorld = _mobileFactory.InteriorSite.CellToWorld(minCell);
            var maxWorld = _mobileFactory.InteriorSite.CellToWorld(maxCell);
            _interiorPreviewRoot.Position = (minWorld + maxWorld) * 0.5f;
            _interiorPreviewRoot.Rotation = new Vector3(0.0f, _mobileFactory.InteriorSite.WorldRotationRadians, 0.0f);
            _interiorPreviewCell.Mesh = new BoxMesh
            {
                Size = new Vector3(
                    _mobileFactory.InteriorSite.CellSize * rect.Size.X - (_mobileFactory.InteriorSite.CellSize * 0.02f),
                    0.08f,
                    _mobileFactory.InteriorSite.CellSize * rect.Size.Y - (_mobileFactory.InteriorSite.CellSize * 0.02f))
            };
            _interiorPreviewCell.Position = new Vector3(0.0f, 0.05f, 0.0f);
            _interiorPreviewArrow.Visible = false;
            var selectionTint = new Color(0.35f, 0.75f, 1.0f, 0.34f);
            FactoryPreviewOverlaySupport.ApplyPreviewColor(_interiorPreviewCell, selectionTint);
            return;
        }

        if (_interiorInteractionMode == FactoryInteractionMode.Delete)
        {
            var start = _deleteInteriorDragActive ? _deleteInteriorDragStartCell : _hoveredInteriorCell;
            var end = _deleteInteriorDragActive ? _deleteInteriorDragCurrentCell : _hoveredInteriorCell;
            var rect = GetDeleteRect(start, end);
            var minCell = rect.Position;
            var maxCell = rect.End - Vector2I.One;
            var minWorld = _mobileFactory.InteriorSite.CellToWorld(minCell);
            var maxWorld = _mobileFactory.InteriorSite.CellToWorld(maxCell);
            _interiorPreviewRoot.Position = (minWorld + maxWorld) * 0.5f;
            _interiorPreviewRoot.Rotation = new Vector3(0.0f, _mobileFactory.InteriorSite.WorldRotationRadians, 0.0f);
            _interiorPreviewCell.Mesh = new BoxMesh
            {
                Size = new Vector3(
                    _mobileFactory.InteriorSite.CellSize * rect.Size.X - (_mobileFactory.InteriorSite.CellSize * 0.02f),
                    0.08f,
                    _mobileFactory.InteriorSite.CellSize * rect.Size.Y - (_mobileFactory.InteriorSite.CellSize * 0.02f))
            };
            _interiorPreviewCell.Position = new Vector3(0.0f, 0.05f, 0.0f);
            _interiorPreviewArrow.Visible = false;
            var deleteTint = _canDeleteInteriorCell ? new Color(1.0f, 0.35f, 0.35f, 0.42f) : new Color(0.75f, 0.30f, 0.30f, 0.28f);
            FactoryPreviewOverlaySupport.ApplyPreviewColor(_interiorPreviewCell, deleteTint);
            return;
        }

        _interiorPreviewRoot.Position = FactoryPlacement.GetPreviewCenter(_mobileFactory.InteriorSite, _selectedInteriorKind, _hoveredInteriorCell, _selectedInteriorFacing);
        _interiorPreviewRoot.Rotation = new Vector3(
            0.0f,
            _mobileFactory.InteriorSite.WorldRotationRadians + FactoryDirection.ToYRotationRadians(_selectedInteriorFacing),
            0.0f);

        var accent = _definitions.TryGetValue(_selectedInteriorKind, out var selectedDefinition)
            ? selectedDefinition.Tint
            : new Color("67E8F9");
        var tint = _canPlaceInteriorCell
            ? new Color(accent.R, accent.G, accent.B, 0.45f)
            : new Color(1.0f, 0.35f, 0.35f, 0.45f);
        var previewSize = FactoryPlacement.GetPreviewBaseSize(_mobileFactory.InteriorSite, _selectedInteriorKind);
        _interiorPreviewCell.Mesh = new BoxMesh
        {
            Size = new Vector3(
                previewSize.X - (_mobileFactory.InteriorSite.CellSize * 0.02f),
                0.06f,
                previewSize.Y - (_mobileFactory.InteriorSite.CellSize * 0.02f))
        };
        _interiorPreviewCell.Position = new Vector3(0.0f, 0.04f, 0.0f);
        _interiorPreviewArrow.Visible = true;
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_interiorPreviewCell, tint);
        FactoryPreviewOverlaySupport.ApplyPreviewColor(_interiorPreviewArrow, tint.Lightened(0.1f));
        UpdatePreviewPowerRange(_selectedInteriorKind, _mobileFactory.InteriorSite, _interiorPreviewPowerRange, tint);
        UpdateInteriorPortHints(_selectedInteriorKind);

        if (!MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(_selectedInteriorKind))
        {
            return;
        }

        if (!_mobileFactory.TryGetAttachmentPreview(_selectedInteriorKind, _hoveredInteriorCell, _selectedInteriorFacing, out _, out var boundaryCells, out var exteriorCells, out _))
        {
            boundaryCells = MobileFactoryBoundaryAttachmentGeometry.GetBoundaryCells(
                MobileFactoryBoundaryAttachmentCatalog.Get(_selectedInteriorKind),
                _hoveredInteriorCell,
                _selectedInteriorFacing);
            exteriorCells = MobileFactoryBoundaryAttachmentGeometry.GetExteriorStencilCells(
                MobileFactoryBoundaryAttachmentCatalog.Get(_selectedInteriorKind),
                _hoveredInteriorCell,
                _selectedInteriorFacing);
        }

        EnsureInteriorAttachmentPreviewMeshCount(_interiorPreviewBoundaryMeshes, boundaryCells.Count, new Vector3(_mobileFactory.InteriorSite.CellSize * 0.72f, 0.05f, _mobileFactory.InteriorSite.CellSize * 0.72f));
        EnsureInteriorAttachmentPreviewMeshCount(_interiorPreviewExteriorMeshes, exteriorCells.Count, new Vector3(_mobileFactory.InteriorSite.CellSize * 0.60f, 0.05f, _mobileFactory.InteriorSite.CellSize * 0.60f));

        for (var i = 0; i < boundaryCells.Count; i++)
        {
            var mesh = _interiorPreviewBoundaryMeshes[i];
            mesh.Visible = true;
            mesh.GlobalPosition = _mobileFactory.InteriorSite.CellToWorld(boundaryCells[i]) + new Vector3(0.0f, 0.05f, 0.0f);
            FactoryPreviewOverlaySupport.ApplyPreviewColor(mesh, tint.Darkened(0.08f));
        }

        for (var i = 0; i < exteriorCells.Count; i++)
        {
            var mesh = _interiorPreviewExteriorMeshes[i];
            mesh.Visible = true;
            mesh.GlobalPosition = _mobileFactory.InteriorSite.CellToWorld(exteriorCells[i]) + new Vector3(0.0f, 0.08f, 0.0f);
            FactoryPreviewOverlaySupport.ApplyPreviewColor(mesh, tint.Lightened(0.1f));
        }
    }

    private void UpdateInteriorBlueprintPreview()
    {
        if (_mobileFactory is null || _interiorBlueprintPreviewRoot is null)
        {
            return;
        }

        foreach (var mesh in _interiorBlueprintPreviewMeshes)
        {
            mesh.Visible = false;
        }

        foreach (var ghost in _interiorBlueprintPreviewGhosts)
        {
            ghost.Visible = false;
        }

        _interiorBlueprintPreviewRoot.Visible = _editorOpen && _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _interiorBlueprintPlan is not null;
        if (_interiorBlueprintGhostPreviewRoot is not null)
        {
            _interiorBlueprintGhostPreviewRoot.Visible = _interiorBlueprintPreviewRoot.Visible;
        }
        if (!_interiorBlueprintPreviewRoot.Visible || _interiorBlueprintPlan is null)
        {
            if (_interiorBlueprintGhostPreviewRoot is not null)
            {
                _interiorBlueprintGhostPreviewRoot.Visible = false;
            }
            return;
        }

        _interiorBlueprintPreviewRoot.GlobalPosition = _mobileFactory.InteriorSite.WorldOrigin;
        _interiorBlueprintPreviewRoot.Rotation = new Vector3(
            0.0f,
            _mobileFactory.InteriorSite.WorldRotationRadians,
            0.0f);

        EnsureInteriorBlueprintPreviewCapacity(_interiorBlueprintPlan.Entries.Count);
        var showGhostPreview = SupportsGhostBlueprintPreview();
        if (_interiorBlueprintGhostPreviewRoot is not null)
        {
            _interiorBlueprintGhostPreviewRoot.Visible = showGhostPreview;
        }
        for (var index = 0; index < _interiorBlueprintPlan.Entries.Count; index++)
        {
            var entry = _interiorBlueprintPlan.Entries[index];
            var mesh = _interiorBlueprintPreviewMeshes[index];
            var previewCenter = FactoryPlacement.GetPreviewCenter(
                _mobileFactory.InteriorSite,
                entry.SourceEntry.Kind,
                entry.TargetCell,
                entry.TargetFacing);
            var previewSize = FactoryPlacement.GetPreviewBaseSize(_mobileFactory.InteriorSite, entry.SourceEntry.Kind);
            mesh.Visible = true;
            mesh.Position = _interiorBlueprintPreviewRoot.ToLocal(previewCenter) + new Vector3(0.0f, 0.06f, 0.0f);
            mesh.Rotation = new Vector3(
                0.0f,
                FactoryDirection.ToYRotationRadians(entry.TargetFacing),
                0.0f);
            mesh.Mesh = new BoxMesh
            {
                Size = new Vector3(
                    Mathf.Max(_mobileFactory.InteriorSite.CellSize * 0.92f, previewSize.X - (_mobileFactory.InteriorSite.CellSize * 0.08f)),
                    0.08f,
                    Mathf.Max(_mobileFactory.InteriorSite.CellSize * 0.92f, previewSize.Y - (_mobileFactory.InteriorSite.CellSize * 0.08f)))
            };
            FactoryPreviewOverlaySupport.ApplyPreviewColor(mesh, entry.IsValid
                ? new Color(0.35f, 0.95f, 0.55f, 0.36f)
                : new Color(1.0f, 0.35f, 0.35f, 0.36f));

            if (showGhostPreview)
            {
                var ghost = EnsureInteriorBlueprintGhostPreview(entry, index);
                ghost.Visible = true;
                if (ghost.Site != _mobileFactory.InteriorSite || ghost.Cell != entry.TargetCell || ghost.Facing != entry.TargetFacing)
                {
                    ghost.Configure(_mobileFactory.InteriorSite, entry.TargetCell, entry.TargetFacing);
                }

                ghost.GlobalPosition = previewCenter;
                ghost.GlobalRotation = new Vector3(
                    0.0f,
                    _mobileFactory.InteriorSite.WorldRotationRadians + FactoryDirection.ToYRotationRadians(entry.TargetFacing),
                    0.0f);

                ghost.ApplyGhostVisual(entry.IsValid
                    ? new Color(0.54f, 0.84f, 1.0f, 0.58f)
                    : new Color(1.0f, 0.52f, 0.52f, 0.54f));
            }
        }
    }

    private void UpdateWorldStatusMessage(double delta)
    {
        if (_worldEventTimer > 0.0f)
        {
            _worldEventTimer = Mathf.Max(0.0f, _worldEventTimer - (float)delta);
        }

        if (_controlMode == MobileFactoryControlMode.DeployPreview && _hasHoveredAnchor && _mobileFactory is not null)
        {
            var evaluation = GetCurrentDeploymentEvaluation();
            var deployState = evaluation?.State ?? (_canDeployCurrentAnchor ? MobileFactoryDeployState.Valid : MobileFactoryDeployState.Blocked);
            _worldPreviewMessage = deployState switch
            {
                MobileFactoryDeployState.Valid => $"可部署到 ({_hoveredAnchor.X}, {_hoveredAnchor.Y})，朝向 {FactoryDirection.ToLabel(_selectedDeployFacing)}，确认后会自动进场部署。",
                MobileFactoryDeployState.Warning => string.IsNullOrWhiteSpace(evaluation?.Reason)
                    ? $"可部署到 ({_hoveredAnchor.X}, {_hoveredAnchor.Y})，朝向 {FactoryDirection.ToLabel(_selectedDeployFacing)}，但采矿输入端口会以非完整状态展开。"
                    : $"可部署到 ({_hoveredAnchor.X}, {_hoveredAnchor.Y})，朝向 {FactoryDirection.ToLabel(_selectedDeployFacing)}，但 {evaluation!.Reason}",
                _ => string.IsNullOrWhiteSpace(evaluation?.Reason)
                    ? $"锚点 ({_hoveredAnchor.X}, {_hoveredAnchor.Y}) 以朝向 {FactoryDirection.ToLabel(_selectedDeployFacing)} 无法部署，可能越界或与现有占用冲突。"
                    : $"锚点 ({_hoveredAnchor.X}, {_hoveredAnchor.Y}) 以朝向 {FactoryDirection.ToLabel(_selectedDeployFacing)} 无法部署：{evaluation!.Reason}"
            };
            _worldStatusTone = deployState switch
            {
                MobileFactoryDeployState.Valid => FactoryStatusTone.Positive,
                MobileFactoryDeployState.Warning => FactoryStatusTone.Warning,
                _ => FactoryStatusTone.Negative
            };
            return;
        }

        if (_worldEventTimer > 0.0f && !string.IsNullOrWhiteSpace(_worldEventMessage))
        {
            _worldPreviewMessage = _worldEventMessage!;
            _worldStatusTone = _worldEventPositive ? FactoryStatusTone.Positive : FactoryStatusTone.Negative;
            return;
        }

        if (_mobileFactory is null)
        {
            _worldPreviewMessage = "移动工厂尚未生成。";
            _worldStatusTone = FactoryStatusTone.Negative;
            return;
        }

        switch (_controlMode)
        {
            case MobileFactoryControlMode.Observer:
                _worldPreviewMessage = "观察模式：WASD/方向键移动相机，滚轮缩放，Tab 返回工厂控制。";
                _worldStatusTone = FactoryStatusTone.Positive;
                break;
            case MobileFactoryControlMode.DeployPreview:
                _worldPreviewMessage = "部署预览：移动鼠标选择落点，Q/E/R 旋转朝向，左键确认，Esc/G 取消。";
                _worldStatusTone = FactoryStatusTone.Positive;
                break;
            default:
                _worldPreviewMessage = _mobileFactory.State switch
                {
                    MobileFactoryLifecycleState.Deployed => "已部署：按 R 切回移动态，Tab 进入观察模式，F 打开内部编辑。",
                    MobileFactoryLifecycleState.AutoDeploying => "自动部署中：移动工厂会先朝目标行进，抵达后再转向展开，Esc 可取消。",
                    MobileFactoryLifecycleState.Recalling => "切回移动态中：部署机构正在收拢，很快恢复机动。",
                    _ => "工厂控制：W/S 前进后退，A/D 转向，G 进入部署模式，Tab 进入观察模式。"
                };
                _worldStatusTone = FactoryStatusTone.Positive;
                break;
        }
    }

    private void UpdateStructureVisuals()
    {
        if (_simulation is null || _structureRoot is null)
        {
            return;
        }

        var alpha = _simulation.TickAlpha;
        UpdateStructureVisualsRecursive(_structureRoot, alpha);
    }

    private void UpdateStructureVisualsRecursive(Node node, float tickAlpha)
    {
        if (node is FactoryStructure structure)
        {
            var isInteriorStructure = structure.Site is MobileFactorySite;
            structure.SetCombatFocus(
                isInteriorStructure && structure == _hoveredInteriorStructure,
                isInteriorStructure && structure == _selectedInteriorStructure);
            structure.SetPowerRangeVisible(ShouldShowInteriorPowerRange(structure));
            structure.SyncVisualPresentation(tickAlpha);
            structure.UpdateVisuals(tickAlpha);
            structure.SyncCombatVisuals(tickAlpha);
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                UpdateStructureVisualsRecursive(childNode, tickAlpha);
            }
        }
    }

    private void UpdateInteriorPowerVisuals()
    {
        if (_mobileFactory is null || _interiorPowerLinkOverlayRoot is null)
        {
            return;
        }

        if (!_editorOpen || _blueprintMode != FactoryBlueprintWorkflowMode.None)
        {
            SetInteriorPowerLinkDashCount(0);
            return;
        }

        if (_interiorInteractionMode == FactoryInteractionMode.Build
            && _hasHoveredInteriorCell
            && _selectedInteriorKind == BuildPrototypeKind.PowerPole)
        {
            var previewColor = _canPlaceInteriorCell
                ? new Color(0.98f, 0.89f, 0.52f, 0.92f)
                : new Color(1.0f, 0.45f, 0.45f, 0.90f);
            RenderInteriorPowerLinkSet(
                GetPreviewPowerAnchor(_mobileFactory.InteriorSite, _hoveredInteriorCell, PreviewPowerPoleWireHeight),
                _hoveredInteriorCell,
                PreviewPowerPoleConnectionRangeCells,
                previewColor,
                _mobileFactory.InteriorSite);
            return;
        }

        if (_interiorInteractionMode == FactoryInteractionMode.Interact
            && _selectedInteriorStructure is PowerPoleStructure selectedPole
            && GodotObject.IsInstanceValid(selectedPole)
            && selectedPole.IsInsideTree())
        {
            RenderInteriorPowerLinkSet(
                GetPowerAnchor(selectedPole),
                selectedPole.Cell,
                selectedPole.PowerConnectionRangeCells,
                new Color(0.99f, 0.93f, 0.62f, 0.92f),
                selectedPole.Site,
                selectedPole);
            return;
        }

        SetInteriorPowerLinkDashCount(0);
    }

    private void RenderInteriorPowerLinkSet(
        Vector3 origin,
        Vector2I originCell,
        int originRange,
        Color color,
        IFactorySite site,
        FactoryStructure? exclude = null)
    {
        FactoryPowerPreviewSupport.RenderPowerLinkSet(
            _structureRoot,
            origin,
            originCell,
            originRange,
            color,
            GetPowerAnchor,
            DrawInteriorDashedPowerLink,
            SetInteriorPowerLinkDashCount,
            site,
            exclude);
    }

    private List<FactoryStructure> CollectConnectablePowerNodes(IFactorySite site, Vector2I originCell, int originRange, FactoryStructure? exclude)
    {
        return FactoryPowerPreviewSupport.CollectConnectablePowerNodes(_structureRoot, originCell, originRange, site, exclude);
    }

    private int DrawInteriorDashedPowerLink(Vector3 start, Vector3 end, Color color, int dashIndex)
    {
        return FactoryPreviewOverlaySupport.DrawDashedPowerLink(
            start,
            end,
            color,
            dashIndex,
            EnsureInteriorPowerLinkDashCapacity,
            _interiorPowerLinkDashes,
            ApplyPowerLinkColor);
    }

    private void EnsureInteriorPowerLinkDashCapacity(int count)
    {
        FactoryPreviewOverlaySupport.EnsurePowerLinkDashCapacity(_interiorPowerLinkOverlayRoot, _interiorPowerLinkDashes, count, "InteriorPowerLinkDash");
    }

    private void SetInteriorPowerLinkDashCount(int visibleCount)
    {
        if (_interiorPowerLinkOverlayRoot is null)
        {
            return;
        }

        for (var i = visibleCount; i < _interiorPowerLinkDashes.Count; i++)
        {
            _interiorPowerLinkDashes[i].Visible = false;
        }

        _interiorPowerLinkOverlayRoot.Visible = visibleCount > 0;
    }

    private bool ShouldShowInteriorPowerRange(FactoryStructure structure)
    {
        return IsInteriorPowerPreviewActive()
            && structure is IFactoryPowerNode
            && structure.Site == _mobileFactory?.InteriorSite
            && GodotObject.IsInstanceValid(structure)
            && structure.IsInsideTree();
    }

    private bool IsInteriorPowerPreviewActive()
    {
        if (!_editorOpen)
        {
            return false;
        }

        return _interiorInteractionMode == FactoryInteractionMode.Build
            ? _hasHoveredInteriorCell && (_selectedInteriorKind == BuildPrototypeKind.Generator || _selectedInteriorKind == BuildPrototypeKind.PowerPole)
            : _interiorInteractionMode == FactoryInteractionMode.Interact && _selectedInteriorStructure is IFactoryPowerNode;
    }

    private static void UpdatePreviewPowerRange(BuildPrototypeKind kind, IFactorySite site, MeshInstance3D previewPowerRange, Color tint)
    {
        FactoryPowerPreviewSupport.UpdatePreviewPowerRange(kind, site, previewPowerRange, tint);
    }

    private static bool TryGetPowerPreviewInfo(BuildPrototypeKind kind, out int rangeCells)
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

    private static Vector3 GetPreviewPowerAnchor(IFactorySite site, Vector2I cell, float height)
    {
        return site.CellToWorld(cell) + new Vector3(0.0f, height, 0.0f);
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

    private void UpdateEditorCamera()
    {
        if (_editorCamera is null || _mobileFactory is null)
        {
            return;
        }

        var suggestedSize = _mobileFactory.GetSuggestedEditorCameraSize();
        _editorCamera.Size = Mathf.Clamp(_editorCamera.Size, 2.6f, Mathf.Max(6.0f, suggestedSize + 1.4f));

        if (CanUseEditorInput())
        {
            var panInput = new Vector2(
                Input.GetActionStrength("camera_pan_right") - Input.GetActionStrength("camera_pan_left"),
                Input.GetActionStrength("camera_pan_down") - Input.GetActionStrength("camera_pan_up"));

            if (panInput.LengthSquared() > 1.0f)
            {
                panInput = panInput.Normalized();
            }

            var panSpeed = Mathf.Max(suggestedSize * 1.8f, _mobileFactory.Profile.InteriorCellSize * 5.5f);
            _editorCameraLocalOffset += panInput * panSpeed * (float)GetProcessDeltaTime();
        }

        var panLimitX = Mathf.Max(
            _mobileFactory.Profile.InteriorWidth * _mobileFactory.Profile.InteriorCellSize * 0.5f,
            _mobileFactory.Profile.InteriorCellSize * 1.75f);
        var panLimitY = Mathf.Max(
            _mobileFactory.Profile.InteriorHeight * _mobileFactory.Profile.InteriorCellSize * 0.5f,
            _mobileFactory.Profile.InteriorCellSize * 1.75f);
        _editorCameraLocalOffset = new Vector2(
            Mathf.Clamp(_editorCameraLocalOffset.X, -panLimitX, panLimitX),
            Mathf.Clamp(_editorCameraLocalOffset.Y, -panLimitY, panLimitY));

        var focus = _mobileFactory.GetEditorFocusWorldCenter();
        var worldOffset = new Vector3(_editorCameraLocalOffset.X, 0.0f, _editorCameraLocalOffset.Y)
            .Rotated(Vector3.Up, _mobileFactory.InteriorSite.WorldRotationRadians);
        _editorCamera.Position = focus + worldOffset + new Vector3(0.0f, _editorCamera.Size * 1.8f, 0.0f);
        _editorCamera.Rotation = new Vector3(-Mathf.Pi * 0.5f, _mobileFactory.InteriorSite.WorldRotationRadians, 0.0f);
    }

    private void UpdateCameraTracking()
    {
        if (_cameraRig is null || _mobileFactory is null)
        {
            return;
        }

        if (_controlMode == MobileFactoryControlMode.Player)
        {
            _cameraRig.SetFollowTarget(_playerController);
            return;
        }

        _cameraRig.SetFollowTarget(null);

        if (_controlMode == MobileFactoryControlMode.Observer || _controlMode == MobileFactoryControlMode.DeployPreview)
        {
            return;
        }

        if (_editorOpen)
        {
            FocusWorldStripOnFactory();
            return;
        }

        _cameraRig.FocusWorldPosition(_mobileFactory.WorldFocusPoint);
    }

    private void UpdateHud()
    {
        if (_hud is null || _mobileFactory is null)
        {
            return;
        }

        if (_blueprintMode != FactoryBlueprintWorkflowMode.None || _pendingBlueprintCapture is not null)
        {
            _hud.SelectWorkspace(BlueprintWorkspaceId);
        }
        else if (_editorOpen && (_interiorInteractionMode == FactoryInteractionMode.Build || _interiorInteractionMode == FactoryInteractionMode.Delete))
        {
            _hud.SelectWorkspace(GetHudBuildWorkspaceId());
        }

        _hud.SetControlMode(_controlMode, _mobileFactory.State, _mobileFactory.TransitFacing, _selectedDeployFacing);
        _hud.SetState(_mobileFactory.State, _mobileFactory.AnchorCell);
        _hud.SetHoverAnchor(_hoveredAnchor, _controlMode == MobileFactoryControlMode.DeployPreview && _hasHoveredAnchor);
        _hud.SetPreviewStatus(_worldStatusTone, _worldPreviewMessage);
        _hud.SetDeliveryStats(GetPrimaryDeliveryTotal(), GetSecondaryDeliveryTotal());
        _hud.SetEditorSelection(_interiorInteractionMode, _selectedInteriorKind, _selectedInteriorFacing);
        _hud.SetEditorSelectionTarget(GetSelectedInteriorStructureText());
        var editorPreviewPositive = _interiorInteractionMode switch
        {
            FactoryInteractionMode.Build => _canPlaceInteriorCell,
            FactoryInteractionMode.Delete => _canDeleteInteriorCell,
            _ => true
        };
        _hud.SetEditorPreview(editorPreviewPositive, _interiorPreviewMessage);
        _hud.SetPortStatus(_mobileFactory.GetPortStatusLabel());
        _hud.SetCombatStats(_simulation?.ActiveEnemyCount ?? 0, _simulation?.DefeatedEnemyCount ?? 0, _simulation?.DestroyedStructureCount ?? 0);
        if (FactoryDemoInteractionBridge.TryGetInspection(_selectedInteriorStructure, out var inspectionTitle, out var inspectionBody))
        {
            _hud.SetEditorInspection(inspectionTitle, inspectionBody);
        }
        else
        {
            _hud.SetEditorInspection(null, null);
        }

        _hud.SetEditorStructureDetails(FactoryDemoInteractionBridge.BuildLinkedDetailModel(_selectedInteriorStructure));

        _hud.SetEditorState(_editorOpen, _mobileFactory.State, CountEditableInteriorStructures(), _interiorInteractionMode);
        _hud.SetHintText(GetHintText());
        _hud.SetFactoryDetails(BuildFactoryDetailText());
        _hud.SetBlueprintState(BuildInteriorBlueprintPanelState());
        _playerHud?.SetContext(_playerController, FactoryDemoInteractionBridge.BuildLinkedDetailModel(_selectedInteriorStructure), ResolveSelectedPlayerItem());
    }

    private void HandleHudWorkspaceSelected(string workspaceId)
    {
        if (FactoryBlueprintWorkflowBridge.HandleBlueprintWorkspaceExit(
                workspaceId,
                BlueprintWorkspaceId,
                HasActiveInteriorBlueprintWorkspaceState(),
                () => CancelInteriorBlueprintWorkflow(clearActiveBlueprint: true),
                out var exitMessage))
        {
            _interiorPreviewMessage = exitMessage ?? string.Empty;
        }

        if (workspaceId == BlueprintWorkspaceId || workspaceId == GetHudBuildWorkspaceId() || workspaceId == TestingWorkspaceId || workspaceId == SavesWorkspaceId)
        {
            if (!_editorOpen)
            {
                SetEditorOpenState(true);
                FocusFactoryForCurrentMode();
            }
        }

        if (workspaceId == SavesWorkspaceId)
        {
            RefreshRuntimeSaveLibrary();
        }
    }

    private bool HasActiveInteriorBlueprintWorkspaceState()
    {
        return _blueprintMode != FactoryBlueprintWorkflowMode.None
            || _pendingBlueprintCapture is not null
            || _interiorBlueprintPlan is not null
            || _hasInteriorBlueprintSelectionRect
            || FactoryBlueprintLibrary.GetActive() is not null;
    }

    private string BuildFactoryDetailText()
    {
        if (_mobileFactory is null)
        {
            return "等待工厂状态更新。";
        }

        var anchorText = _mobileFactory.AnchorCell is Vector2I anchorCell
            ? $"({anchorCell.X}, {anchorCell.Y})"
            : "未部署";
        var stateText = _mobileFactory.State switch
        {
            MobileFactoryLifecycleState.Deployed => "已部署",
            MobileFactoryLifecycleState.AutoDeploying => "自动部署中",
            MobileFactoryLifecycleState.Recalling => "回收中",
            _ => "运输中"
        };

        return
            $"Profile: {_mobileFactory.Profile.Id}\n" +
            $"Preset: {_mobileFactory.InteriorPreset.Id}\n" +
            $"State: {stateText} | Anchor: {anchorText}\n" +
            $"Interior standard: {FactoryIndustrialStandards.GetBuildCatalog(FactorySiteKind.Interior).PreviewStyleLabel}\n" +
            $"Interior structures: {CountEditableInteriorStructures()}\n" +
            $"Connected input ports: {CountConnectedAttachments(BuildPrototypeKind.InputPort)} | Mining inputs: {CountConnectedAttachments(BuildPrototypeKind.MiningInputPort)}\n" +
            $"Connected output ports: {CountConnectedAttachments(BuildPrototypeKind.OutputPort)}";
    }

    private string GetHudBuildWorkspaceId()
    {
        return UseLargeTestScenario ? BuildTestWorkspaceId : EditorWorkspaceId;
    }

    private FactoryBlueprintPanelState BuildInteriorBlueprintPanelState()
    {
        var modeText = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图模式：内部框选保存",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图模式：内部应用预览（旋转 {FactoryDirection.ToLabel(_interiorBlueprintRotation)}）",
            _ => "蓝图模式：待命"
        };
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        var activeText = FactoryBlueprintWorkflowBridge.BuildActiveBlueprintText();
        var captureSummary = _pendingBlueprintCapture is null
            ? "可框选当前内部布局保存为蓝图，也可一键保存整个内部布局。"
            : $"待保存：{_pendingBlueprintCapture.DisplayName} | {_pendingBlueprintCapture.GetSummaryText()}";
        var issueText = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _interiorBlueprintPlan is not null
            ? $"当前旋转：{FactoryDirection.ToLabel(_interiorBlueprintRotation)} | 占地 {_interiorBlueprintPlan.FootprintSize.X}x{_interiorBlueprintPlan.FootprintSize.Y}\n{_interiorBlueprintPlan.GetIssueSummary()}"
            : _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection
                ? "框选完成后在这里输入名称并保存。"
                : HasRetainedInteriorBlueprintSelection()
                    ? "已保留刚才的框选结果，可直接保存；左键点新的建筑或空地会清除这次框选。"
                : "保存当前布局，或从蓝图库选择一个内部蓝图进行预览。";

        return new FactoryBlueprintPanelState
        {
            IsVisible = _editorOpen,
            ModeText = modeText,
            ActiveBlueprintText = activeText,
            CaptureSummaryText = captureSummary,
            IssueText = issueText,
            SuggestedName = _pendingBlueprintCapture?.DisplayName ?? string.Empty,
            PendingCaptureId = _pendingBlueprintCapture?.Id,
            ActiveBlueprintId = activeBlueprint?.Id,
            AllowFullCapture = true,
            CanSaveCapture = _pendingBlueprintCapture is not null,
            CanConfirmApply = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _interiorBlueprintPlan?.IsValid == true,
            Blueprints = FactoryBlueprintLibrary.GetAll()
        };
    }

    private void ToggleObserverMode()
    {
        if (_mobileFactory is null)
        {
            return;
        }

        if (_controlMode == MobileFactoryControlMode.Observer)
        {
            SetControlMode(MobileFactoryControlMode.Player);
            ShowWorldEvent("已返回玩家控制模式。", true);
            return;
        }

        SetControlMode(MobileFactoryControlMode.Observer);
        ShowWorldEvent("已进入观察模式，现在 WASD 控制相机。", true);
    }

    private void ToggleFactoryCommandMode()
    {
        if (_mobileFactory is null)
        {
            return;
        }

        if (_controlMode == MobileFactoryControlMode.FactoryCommand)
        {
            SetControlMode(MobileFactoryControlMode.Player);
            ShowWorldEvent("已返回玩家控制模式。", true);
            return;
        }

        SetControlMode(MobileFactoryControlMode.FactoryCommand);
        ShowWorldEvent("已进入工厂控制模式，WASD 现在控制移动工厂。", true);
    }

    private void ToggleDeployPreview()
    {
        if (_mobileFactory is null)
        {
            return;
        }

        if (_controlMode == MobileFactoryControlMode.DeployPreview)
        {
            SetControlMode(MobileFactoryControlMode.Player);
            ShowWorldEvent("已取消部署预览。", true);
            return;
        }

        if (_mobileFactory.State != MobileFactoryLifecycleState.InTransit)
        {
            ShowWorldEvent("只有在运输中才能进入部署预览；已部署时请先回收。", false);
            return;
        }

        _selectedDeployFacing = _mobileFactory.TransitFacing;
        _currentDeployEvaluation = null;
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        ShowWorldEvent("部署预览已开启，移动鼠标选点，Q/E/R 旋转，左键确认。", true);
    }

    private MobileFactoryInteractionPattern GetActiveInteractionPattern()
    {
        return _controlMode switch
        {
            MobileFactoryControlMode.DeployPreview => MobileFactoryInteractionPattern.DeployPlacement,
            _ => MobileFactoryInteractionPattern.None
        };
    }

    private void HandleCommandSlot(MobileFactoryCommandSlot commandSlot)
    {
        if (TryHandleInteractionPatternCommand(GetActiveInteractionPattern(), commandSlot))
        {
            return;
        }

        switch (commandSlot)
        {
            case MobileFactoryCommandSlot.Cancel:
                CancelWorldCommand();
                break;
            case MobileFactoryCommandSlot.Auxiliary:
                ReturnFactoryToTransitMode();
                break;
        }
    }

    private bool TryHandleInteractionPatternCommand(MobileFactoryInteractionPattern interactionPattern, MobileFactoryCommandSlot commandSlot)
    {
        switch (interactionPattern)
        {
            case MobileFactoryInteractionPattern.DeployPlacement:
                return TryHandleDeployPlacementCommand(commandSlot);
            default:
                return false;
        }
    }

    private bool TryHandleDeployPlacementCommand(MobileFactoryCommandSlot commandSlot)
    {
        switch (commandSlot)
        {
            case MobileFactoryCommandSlot.Cancel:
                CancelWorldCommand();
                return true;
            case MobileFactoryCommandSlot.Auxiliary:
                RotateDeployFacing(1, "R");
                return true;
            default:
                return false;
        }
    }

    private void RotateDeployFacing(int direction, string sourceLabel)
    {
        if (_controlMode != MobileFactoryControlMode.DeployPreview)
        {
            return;
        }

        _selectedDeployFacing = direction < 0
            ? FactoryDirection.RotateCounterClockwise(_selectedDeployFacing)
            : FactoryDirection.RotateClockwise(_selectedDeployFacing);
        _currentDeployEvaluation = null;
        ShowWorldEvent($"部署朝向已通过 {sourceLabel} 旋转到 {FactoryDirection.ToLabel(_selectedDeployFacing)}。", true);
    }

    private void CancelWorldCommand()
    {
        if (_mobileFactory is null)
        {
            return;
        }

        if (_controlMode == MobileFactoryControlMode.DeployPreview)
        {
            SetControlMode(MobileFactoryControlMode.Player);
            ShowWorldEvent("已取消部署预览。", true);
            return;
        }

        if (_mobileFactory.CancelAutoDeploy())
        {
            SetControlMode(MobileFactoryControlMode.Player);
            return;
        }

        if (_controlMode == MobileFactoryControlMode.Observer)
        {
            SetControlMode(MobileFactoryControlMode.Player);
            ShowWorldEvent("已退出观察模式。", true);
            return;
        }

        if (_controlMode == MobileFactoryControlMode.FactoryCommand)
        {
            SetControlMode(MobileFactoryControlMode.Player);
            ShowWorldEvent("已退出工厂控制模式。", true);
        }
    }

    private void ConfirmDeployPreview()
    {
        if (!_hasHoveredAnchor || !_canDeployCurrentAnchor || _grid is null || _mobileFactory is null)
        {
            ShowWorldEvent("当前预览目标无效，无法开始部署。", false);
            return;
        }

        if (_mobileFactory.TryStartAutoDeploy(_grid, _hoveredAnchor, _selectedDeployFacing))
        {
            SetControlMode(MobileFactoryControlMode.Player);
        }
    }

    private void ReturnFactoryToTransitMode()
    {
        if (_mobileFactory?.ReturnToTransitMode() == true)
        {
            SetControlMode(MobileFactoryControlMode.Player);
        }
        else if (_mobileFactory is not null && _mobileFactory.State != MobileFactoryLifecycleState.Deployed)
        {
            ShowWorldEvent("当前未处于部署态，不能切回移动态。", false);
        }
    }

    private void PlaceInteriorStructure()
    {
        if (_mobileFactory is null || !_hasHoveredInteriorCell)
        {
            return;
        }

        if (!TryGetActiveInteriorPlacementKind(out var placementKind, out var usesPlayerInventory))
        {
            TraceLog("PlaceInteriorStructure ignored because there is no active placement kind");
            return;
        }

        var placed = _mobileFactory.PlaceInteriorStructure(placementKind, _hoveredInteriorCell, _selectedInteriorFacing);
        TraceLog($"PlaceInteriorStructure cell={_hoveredInteriorCell} kind={placementKind} usesPlayerInventory={usesPlayerInventory} placed={placed}");
        if (placed)
        {
            _interiorPreviewMessage = $"已在内部格 ({_hoveredInteriorCell.X}, {_hoveredInteriorCell.Y}) 放置{GetInteriorDisplayName(placementKind)}。";
            if (usesPlayerInventory)
            {
                var consumed = TryConsumeSelectedPlayerPlaceable();
                TraceLog($"PlaceInteriorStructure consumedPlayerPlaceable={consumed}");
                RefreshInteriorInteractionModeFromBuildSource();
            }
        }
    }

    private void RemoveInteriorStructure()
    {
        if (_mobileFactory is null || !_hasHoveredInteriorCell)
        {
            return;
        }

        if (_mobileFactory.RemoveInteriorStructure(_hoveredInteriorCell))
        {
            if (_selectedInteriorStructure == _hoveredInteriorStructure)
            {
                _selectedInteriorStructure = null;
            }
            _interiorPreviewMessage = $"已移除内部格 ({_hoveredInteriorCell.X}, {_hoveredInteriorCell.Y}) 的结构。";
        }
    }

    private void AdjustEditorZoom(float delta)
    {
        if (_editorCamera is null)
        {
            return;
        }

        var maxZoom = _mobileFactory is null
            ? 6.0f
            : Mathf.Max(6.0f, _mobileFactory.GetSuggestedEditorCameraSize() + 1.4f);
        _editorCamera.Size = Mathf.Clamp(_editorCamera.Size + delta, 2.6f, maxZoom);
    }

    private bool TryProjectEditorMouseToInterior(out Vector3 worldPosition)
    {
        worldPosition = Vector3.Zero;

        if (_editorCamera is null || _hud is null || _mobileFactory is null)
        {
            return false;
        }

        if (!_hud.TryGetEditorMousePosition(_mousePosition, out var mousePosition))
        {
            return false;
        }

        var rayOrigin = _editorCamera.ProjectRayOrigin(mousePosition);
        var rayDirection = _editorCamera.ProjectRayNormal(mousePosition);
        var planeY = _mobileFactory.InteriorSite.WorldOrigin.Y;

        if (Mathf.Abs(rayDirection.Y) < 0.001f)
        {
            return false;
        }

        var distance = (planeY - rayOrigin.Y) / rayDirection.Y;
        if (distance < 0.0f)
        {
            return false;
        }

        worldPosition = rayOrigin + rayDirection * distance;
        return true;
    }

    private bool CanUseWorldInput()
    {
        return !_editorOpen || !_hoveringEditorPane;
    }

    private bool CanUseEditorInput()
    {
        return _editorOpen && _hoveringEditorPane;
    }

    private bool CanUseEditorViewportInput()
    {
        return _editorOpen && _hoveringEditorViewport;
    }

    private void HandlePlayerHotbarPressed(int index)
    {
        if (_playerController is null)
        {
            return;
        }

        _playerController.ToggleHotbarIndex(index);
        _selectedPlayerItemInventoryId = FactoryPlayerController.BackpackInventoryId;
        _selectedPlayerItemSlot = new Vector2I(index, 0);
        _hasSelectedPlayerItemSlot = true;
        _playerInteriorPlacementArmed = _playerController.IsHotbarPlacementArmed;
        if (_playerInteriorPlacementArmed && TryResolveSelectedPlayerPlaceable(out var placementKind))
        {
            _selectedInteriorKind = placementKind;
        }

        TraceLog($"HandlePlayerHotbarPressed index={index} armed={_playerInteriorPlacementArmed} item={ResolveSelectedPlayerItem()?.ItemKind.ToString() ?? "none"}");
        RefreshInteriorInteractionModeFromBuildSource();
        UpdateHud();
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

    private void HandlePlayerInventoryMoveRequested(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack)
    {
        if (FactoryDemoInteractionBridge.TryMoveInventoryItem(TryResolveInventoryEndpoint, inventoryId, fromSlot, toSlot, splitStack))
        {
            UpdateHud();
        }
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

        _playerController?.DisarmHotbarPlacement();
        _playerInteriorPlacementArmed = _playerSelectionState.PlacementArmed;
        if (_playerInteriorPlacementArmed && TryResolveSelectedPlayerPlaceable(out var placementKind))
        {
            _selectedInteriorKind = placementKind;
        }
        RefreshInteriorInteractionModeFromBuildSource();
        UpdateHud();
    }

    private FactoryItem? ResolveSelectedPlayerItem()
    {
        if (_playerController is null)
        {
            return null;
        }

        if (!_hasSelectedPlayerItemSlot || string.IsNullOrWhiteSpace(_selectedPlayerItemInventoryId))
        {
            return _playerController.GetActiveHotbarItem();
        }

        _playerSelectionState.InventoryId = _selectedPlayerItemInventoryId;
        _playerSelectionState.Slot = _selectedPlayerItemSlot;
        _playerSelectionState.HasSlot = _hasSelectedPlayerItemSlot;
        _playerSelectionState.PlacementArmed = _playerInteriorPlacementArmed;
        return FactoryDemoInteractionBridge.ResolveSelectedPlayerItem(_playerController, TryResolveInventoryEndpoint, _playerSelectionState);
    }

    private bool IsPointerOverUi()
    {
        var hoveredControl = GetViewport().GuiGetHoveredControl();
        var pointer = GetViewport().GetMousePosition();
        return (_hud?.BlocksInput(hoveredControl, pointer) ?? false)
            || (_playerHud?.BlocksWorldInput(hoveredControl, pointer) ?? false);
    }

    private void OnEditorPaletteSelected(BuildPrototypeKind kind)
    {
        SelectInteriorBuildKind(_selectedInteriorKind == kind && _interiorInteractionMode == FactoryInteractionMode.Build
            ? null
            : kind);
    }

    private void OnEditorRotateRequested(int direction)
    {
        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            RotateInteriorBlueprintPreview(direction);
            return;
        }

        _selectedInteriorFacing = direction < 0
            ? FactoryDirection.RotateCounterClockwise(_selectedInteriorFacing)
            : FactoryDirection.RotateClockwise(_selectedInteriorFacing);
    }

    private void FocusWorldStripOnFactory()
    {
        if (_cameraRig is null || _mobileFactory is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var targetScreenPosition = new Vector2(
            viewportSize.X / 12.0f,
            viewportSize.Y * 0.58f);
        _cameraRig.FocusWorldPositionInViewport(_mobileFactory.WorldFocusPoint, targetScreenPosition, 1.0f);
    }

    private void FocusWorldCenterOnFactory()
    {
        if (_cameraRig is null || _mobileFactory is null)
        {
            return;
        }

        _cameraRig.FocusWorldPosition(_mobileFactory.WorldFocusPoint);
    }

    private void FocusFactoryForCurrentMode()
    {
        if (_controlMode == MobileFactoryControlMode.Player)
        {
            if (_playerController is not null)
            {
                _cameraRig?.SetFollowTarget(_playerController, snapImmediately: true);
            }

            return;
        }

        if (_controlMode == MobileFactoryControlMode.Observer)
        {
            return;
        }

        if (_editorOpen)
        {
            FocusWorldStripOnFactory();
            return;
        }

        FocusWorldCenterOnFactory();
    }

    private void SetEditorOpenState(bool isOpen)
    {
        _editorOpen = isOpen;
        _hud?.SetEditorOpen(isOpen);
        _mobileFactory?.SetCombatOverlayScale(isOpen
            ? FactoryConstants.NormalCombatOverlayScale
            : FactoryConstants.MobileInteriorCombatOverlayScale);

        if (!isOpen)
        {
            EnterInteriorInteractionMode();
            _editorCameraLocalOffset = Vector2.Zero;
            return;
        }

        _editorCameraLocalOffset = Vector2.Zero;
        if (_editorCamera is not null && _mobileFactory is not null)
        {
            _editorCamera.Size = _mobileFactory.GetSuggestedEditorCameraSize();
        }
    }

    private void SelectInteriorBuildKind(BuildPrototypeKind? kind)
    {
        CancelInteriorBlueprintWorkflow(clearActiveBlueprint: false);
        _playerInteriorPlacementArmed = false;
        _playerController?.DisarmHotbarPlacement();
        if (!kind.HasValue)
        {
            EnterInteriorInteractionMode();
            return;
        }

        _selectedInteriorKind = kind.Value;
        _interiorInteractionMode = FactoryInteractionMode.Build;
    }

    private void EnterInteriorInteractionMode()
    {
        _interiorInteractionMode = FactoryInteractionMode.Interact;
        _deleteInteriorDragActive = false;
    }

    private void EnterInteriorDeleteMode()
    {
        CancelInteriorBlueprintWorkflow(clearActiveBlueprint: false);
        _selectedInteriorStructure = null;
        _interiorInteractionMode = FactoryInteractionMode.Delete;
        _deleteInteriorDragActive = false;
    }

    private void HandleEditorPrimaryClick()
    {
        if (!_hasHoveredInteriorCell)
        {
            return;
        }

        ClearRetainedInteriorBlueprintSelection();

        if (_interiorInteractionMode == FactoryInteractionMode.Build)
        {
            if (_canPlaceInteriorCell)
            {
                PlaceInteriorStructure();
            }

            return;
        }

        _selectedInteriorStructure = _hoveredInteriorStructure;
    }

    private void HandleEditorSecondaryClick()
    {
        if (!_hasHoveredInteriorCell)
        {
            return;
        }

        if (_interiorInteractionMode == FactoryInteractionMode.Build)
        {
            _playerInteriorPlacementArmed = false;
            _playerController?.DisarmHotbarPlacement();
            EnterInteriorInteractionMode();
            UpdateHud();
            return;
        }

        _selectedInteriorStructure = null;
    }

    private void HandleEditorDeletePrimaryPress(bool shiftPressed)
    {
        if (!_hasHoveredInteriorCell)
        {
            return;
        }

        if (shiftPressed)
        {
            _deleteInteriorDragActive = true;
            _deleteInteriorDragStartCell = _hoveredInteriorCell;
            _deleteInteriorDragCurrentCell = _hoveredInteriorCell;
            return;
        }

        RemoveInteriorStructure();
    }

    private void HandleEditorDeletePrimaryRelease()
    {
        if (!_deleteInteriorDragActive)
        {
            return;
        }

        _deleteInteriorDragActive = false;
        DeleteInteriorStructuresInRect(_deleteInteriorDragStartCell, _deleteInteriorDragCurrentCell);
    }

    private Rect2I GetDeleteRect(Vector2I start, Vector2I end)
    {
        return FactorySelectionRectSupport.BuildInclusiveRect(start, end);
    }

    private int CountInteriorStructuresInDeleteRect(Vector2I start, Vector2I end)
    {
        if (_mobileFactory is null)
        {
            return 0;
        }

        return FactorySelectionRectSupport.CountUniqueStructuresInRect(
            start,
            end,
            cell => _mobileFactory.TryGetInteriorStructure(cell, out var structure) ? structure : null);
    }

    private void DeleteInteriorStructuresInRect(Vector2I start, Vector2I end)
    {
        if (_mobileFactory is null)
        {
            return;
        }

        var cellsToDelete = FactorySelectionRectSupport.CollectUniqueStructureAnchorCells(
            start,
            end,
            cell => _mobileFactory.TryGetInteriorStructure(cell, out var structure) ? structure : null);

        for (var index = 0; index < cellsToDelete.Count; index++)
        {
            _hoveredInteriorCell = cellsToDelete[index];
            RemoveInteriorStructure();
        }
    }

    private void UpdateInteriorBlueprintPlan()
    {
        if (_interiorBlueprintSite is null)
        {
            _interiorPreviewMessage = "蓝图预览不可用：缺少内部站点。";
            _interiorBlueprintPlan = null;
            return;
        }

        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        if (activeBlueprint is null)
        {
            _interiorPreviewMessage = "请先从蓝图库中选择一个内部蓝图。";
            _interiorBlueprintPlan = null;
            return;
        }

        var anchor = _hasHoveredInteriorCell
            ? _hoveredInteriorCell
            : _interiorBlueprintSite.GetDefaultApplyAnchor(activeBlueprint);
        _interiorBlueprintPlan = FactoryBlueprintPlanner.CreatePlan(activeBlueprint, _interiorBlueprintSite, anchor, _interiorBlueprintRotation);
        _interiorPreviewMessage = _interiorBlueprintPlan.IsValid
            ? $"蓝图 {activeBlueprint.DisplayName} 可应用到内部锚点 ({anchor.X}, {anchor.Y})，旋转 {FactoryDirection.ToLabel(_interiorBlueprintRotation)}。"
            : _interiorBlueprintPlan.GetIssueSummary();
    }

    private void EnsureInteriorBlueprintPreviewCapacity(int count)
    {
        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _interiorBlueprintPreviewRoot,
            _interiorBlueprintPreviewMeshes,
            count,
            index => new MeshInstance3D
            {
                Name = $"InteriorBlueprintPreview_{index}",
                Visible = false,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(_mobileFactory?.InteriorSite.CellSize * 0.82f ?? 0.82f, 0.08f, _mobileFactory?.InteriorSite.CellSize * 0.82f ?? 0.82f)
                }
            });
    }

    private FactoryStructure EnsureInteriorBlueprintGhostPreview(FactoryBlueprintPlanEntry entry, int index)
    {
        if (_mobileFactory is null)
        {
            throw new System.InvalidOperationException("Interior blueprint ghost preview root is missing.");
        }

        return FactoryPreviewPoolSupport.EnsureGhostPreview(
            _interiorBlueprintGhostPreviewRoot,
            _interiorBlueprintPreviewGhosts,
            index,
            entry.SourceEntry.Kind,
            kind => FactoryStructureFactory.CreateGhostPreview(
                kind,
                new FactoryStructurePlacement(_mobileFactory.InteriorSite, entry.TargetCell, entry.TargetFacing)),
            "InteriorBlueprintGhostPreview");
    }

    private static bool SupportsGhostBlueprintPreview()
    {
        return !HasFocusedSmokeTestFlag() && !HasLargeScenarioSmokeTestFlag();
    }

    private FactoryBlueprintSiteAdapter CreateInteriorBlueprintSiteAdapter()
    {
        return new FactoryBlueprintSiteAdapter(
            FactoryBlueprintSiteKind.MobileInterior,
            _mobileFactory!.InteriorSite.SiteId,
            "移动工厂内部",
            _mobileFactory.InteriorMinCell,
            _mobileFactory.InteriorMaxCell,
            () => _mobileFactory.InteriorSite.GetStructures(),
            ValidateInteriorBlueprintPlacement,
            (kind, cell, facing) =>
            {
                if (!_mobileFactory.PlaceInteriorStructure(kind, cell, facing))
                {
                    return null;
                }

                _mobileFactory.TryGetInteriorStructure(cell, out var structure);
                return structure;
            },
            cell => _mobileFactory.RemoveInteriorStructure(cell),
            defaultApplyAnchor: record =>
            {
                var anchor = record.SuggestedAnchorCell;
                if (_mobileFactory.InteriorSite.IsInBounds(anchor))
                {
                    return anchor;
                }

                return _mobileFactory.InteriorMinCell;
            },
            validateCompatibility: record =>
            {
                if (record.BoundsSize.X > _mobileFactory.Profile.InteriorWidth || record.BoundsSize.Y > _mobileFactory.Profile.InteriorHeight)
                {
                    return "该蓝图尺寸超过当前移动工厂内部可用边界。";
                }

                return null;
            });
    }

    private string? ValidateInteriorBlueprintPlacement(FactoryBlueprintStructureEntry entry, Vector2I targetCell, FacingDirection targetFacing)
    {
        if (_mobileFactory is null)
        {
            return "移动工厂内部不可用。";
        }

        var definition = FactoryStructureFactory.GetDefinition(entry.Kind);
        if (!FactoryIndustrialStandards.IsStructureAllowed(entry.Kind, FactorySiteKind.Interior))
        {
            return FactoryIndustrialStandards.GetPlacementCompatibilityError(entry.Kind, FactorySiteKind.Interior);
        }

        if (!_mobileFactory.InteriorSite.IsInBounds(targetCell))
        {
            return "目标格超出内部编辑范围。";
        }

        if (!_mobileFactory.CanPlaceInterior(entry.Kind, targetCell, targetFacing))
        {
            return MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(entry.Kind)
                ? "该蓝图需要的边界 attachment 挂点在当前内部不可用。"
                : "目标占地已被占用或越界。";
        }

        return null;
    }

    private void StartInteriorBlueprintCapture()
    {
        EnterInteriorInteractionMode();
        _blueprintMode = FactoryBlueprintWorkflowMode.CaptureSelection;
        _interiorBlueprintPlan = null;
        _interiorBlueprintRotation = FacingDirection.East;
        _pendingBlueprintCapture = null;
        _hasInteriorBlueprintSelectionRect = false;
        _interiorBlueprintSelectionDragActive = false;
    }

    private void ExitInteriorBlueprintCaptureMode(bool preserveExistingSelection)
    {
        if (_blueprintMode != FactoryBlueprintWorkflowMode.CaptureSelection)
        {
            return;
        }

        if (_interiorBlueprintSelectionDragActive)
        {
            CompleteInteriorBlueprintSelection();
        }

        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        _interiorBlueprintSelectionDragActive = false;

        if (!preserveExistingSelection)
        {
            _hasInteriorBlueprintSelectionRect = false;
            _pendingBlueprintCapture = null;
        }
    }

    private bool HasRetainedInteriorBlueprintSelection()
    {
        return _blueprintMode == FactoryBlueprintWorkflowMode.None
            && _hasInteriorBlueprintSelectionRect
            && _pendingBlueprintCapture is not null;
    }

    private void ClearRetainedInteriorBlueprintSelection()
    {
        if (!HasRetainedInteriorBlueprintSelection())
        {
            return;
        }

        _hasInteriorBlueprintSelectionRect = false;
        _pendingBlueprintCapture = null;
    }

    private void BeginInteriorBlueprintSelection()
    {
        if (!_hasHoveredInteriorCell)
        {
            return;
        }

        _interiorBlueprintSelectionDragActive = true;
        _interiorBlueprintSelectionStartCell = _hoveredInteriorCell;
        _interiorBlueprintSelectionCurrentCell = _hoveredInteriorCell;
        _hasInteriorBlueprintSelectionRect = false;
        _pendingBlueprintCapture = null;
    }

    private void CompleteInteriorBlueprintSelection()
    {
        if (!_interiorBlueprintSelectionDragActive)
        {
            return;
        }

        _interiorBlueprintSelectionDragActive = false;
        _interiorBlueprintSelectionRect = GetDeleteRect(_interiorBlueprintSelectionStartCell, _interiorBlueprintSelectionCurrentCell);
        _hasInteriorBlueprintSelectionRect = true;

        if (_interiorBlueprintSite is null)
        {
            return;
        }

        var suggestedName = $"内部框选蓝图 {CountInteriorStructuresInDeleteRect(_interiorBlueprintSelectionStartCell, _interiorBlueprintSelectionCurrentCell)} 件";
        _pendingBlueprintCapture = FactoryBlueprintCaptureService.CaptureSelection(
            _interiorBlueprintSite,
            _interiorBlueprintSelectionRect,
            suggestedName);
        if (_pendingBlueprintCapture is null)
        {
            _interiorPreviewMessage = "框选区域内没有可保存的内部建筑。";
            _hasInteriorBlueprintSelectionRect = false;
        }
    }

    private void CaptureCurrentInteriorBlueprint()
    {
        if (_interiorBlueprintSite is null)
        {
            return;
        }

        _hasInteriorBlueprintSelectionRect = false;
        _interiorBlueprintSelectionDragActive = false;
        _pendingBlueprintCapture = FactoryBlueprintCaptureService.CaptureFullSite(
            _interiorBlueprintSite,
            $"内部蓝图 {CountEditableInteriorStructures()} 件");
    }

    private void HandleInteriorBlueprintSaveRequested(string name, FactoryBlueprintPersistenceTarget target)
    {
        if (_pendingBlueprintCapture is null)
        {
            return;
        }

        var savedRecord = FactoryBlueprintWorkflowBridge.SavePendingCapture(_pendingBlueprintCapture, name, target);
        _pendingBlueprintCapture = null;
        _hasInteriorBlueprintSelectionRect = false;
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        ShowBlueprintPersistenceStatus(savedRecord, target);
    }

    private void HandleInteriorBlueprintSelected(string blueprintId)
    {
        FactoryBlueprintWorkflowBridge.SelectBlueprint(blueprintId, _blueprintMode, UpdateInteriorBlueprintPlan);
    }

    private void EnterInteriorBlueprintApplyMode()
    {
        if (!_editorOpen || FactoryBlueprintLibrary.GetActive() is null)
        {
            return;
        }

        EnterInteriorInteractionMode();
        _pendingBlueprintCapture = null;
        _hasInteriorBlueprintSelectionRect = false;
        _blueprintMode = FactoryBlueprintWorkflowMode.ApplyPreview;
        _interiorBlueprintRotation = FacingDirection.East;
        UpdateInteriorBlueprintPlan();
    }

    private void ConfirmInteriorBlueprintApply()
    {
        if (_interiorBlueprintSite is null || _interiorBlueprintPlan is null)
        {
            return;
        }

        if (!FactoryBlueprintPlanner.CommitPlan(_interiorBlueprintPlan, _interiorBlueprintSite))
        {
            _interiorPreviewMessage = "内部蓝图应用失败，请先清理冲突格或边界挂点。";
            return;
        }

        _selectedInteriorStructure = null;
        _interiorPreviewMessage = $"已应用蓝图：{_interiorBlueprintPlan.Blueprint.DisplayName}（旋转 {FactoryDirection.ToLabel(_interiorBlueprintPlan.Rotation)}）";
    }

    private void HandleInteriorBlueprintDeleteRequested(string blueprintId)
    {
        FactoryBlueprintWorkflowBridge.DeleteBlueprint(blueprintId, () => _interiorBlueprintPlan = null);
    }

    private void CancelInteriorBlueprintWorkflow()
    {
        CancelInteriorBlueprintWorkflow(clearActiveBlueprint: false);
    }

    private void CancelInteriorBlueprintWorkflow(bool clearActiveBlueprint)
    {
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        _interiorBlueprintSelectionDragActive = false;
        _hasInteriorBlueprintSelectionRect = false;
        _pendingBlueprintCapture = null;
        _interiorBlueprintPlan = null;
        _interiorBlueprintRotation = FacingDirection.East;

        if (clearActiveBlueprint)
        {
            FactoryBlueprintLibrary.ClearActive();
        }
    }

    private void RotateInteriorBlueprintPreview(int direction)
    {
        if (_blueprintMode != FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            return;
        }

        _interiorBlueprintRotation = direction < 0
            ? FactoryDirection.RotateCounterClockwise(_interiorBlueprintRotation)
            : FactoryDirection.RotateClockwise(_interiorBlueprintRotation);
        UpdateInteriorBlueprintPlan();
    }

    private static bool IsBlueprintSelectionModifierHeld()
    {
        return Input.IsKeyPressed(Key.Shift);
    }

    private void UpdateCursorShape()
    {
        if (_editorOpen && _hoveringEditorViewport && _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection)
        {
            Input.SetDefaultCursorShape(Input.CursorShape.Cross);
            return;
        }

        if (_editorOpen && _hoveringEditorViewport && _interiorInteractionMode == FactoryInteractionMode.Delete)
        {
            Input.SetDefaultCursorShape(Input.CursorShape.Cross);
            return;
        }

        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
    }

    private void HandleEditorDetailInventoryMoveRequested(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack)
    {
        if (FactoryDemoInteractionBridge.TryMoveInventoryItem(TryResolveInventoryEndpoint, inventoryId, fromSlot, toSlot, splitStack))
        {
            UpdateHud();
        }
    }

    private void HandleEditorDetailInventoryTransferRequested(string fromInventoryId, Vector2I fromSlot, string toInventoryId, Vector2I toSlot, bool splitStack)
    {
        if (FactoryDemoInteractionBridge.TryTransferInventoryItem(TryResolveInventoryEndpoint, fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack))
        {
            UpdateHud();
        }
    }

    private void HandleEditorDetailRecipeSelected(string recipeId)
    {
        if (FactoryDemoInteractionBridge.TrySetDetailRecipe(_selectedInteriorStructure, recipeId))
        {
            UpdateHud();
        }
    }

    private void HandleEditorDetailActionRequested(string actionId)
    {
        if (FactoryDemoInteractionBridge.TryInvokeDetailAction(_selectedInteriorStructure, actionId))
        {
            UpdateHud();
        }
    }

    private void HandleEditorDetailClosed()
    {
        _selectedInteriorStructure = null;
        UpdateHud();
    }

    private bool TryResolveSelectedPlayerPlaceable(out BuildPrototypeKind kind)
    {
        return FactoryPresentation.TryGetPlaceableStructureKind(ResolveSelectedPlayerItem(), out kind);
    }

    private bool TryConsumeSelectedPlayerPlaceable()
    {
        _playerSelectionState.InventoryId = _selectedPlayerItemInventoryId;
        _playerSelectionState.Slot = _selectedPlayerItemSlot;
        _playerSelectionState.HasSlot = _hasSelectedPlayerItemSlot;
        _playerSelectionState.PlacementArmed = _playerInteriorPlacementArmed;
        var consumed = FactoryDemoInteractionBridge.TryConsumeSelectedPlaceable(_playerController, TryResolveInventoryEndpoint, _playerSelectionState);
        _playerInteriorPlacementArmed = _playerSelectionState.PlacementArmed;

        if (_playerInteriorPlacementArmed && TryResolveSelectedPlayerPlaceable(out var placementKind))
        {
            _selectedInteriorKind = placementKind;
        }

        return consumed;
    }



    private bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        return FactoryDemoInteractionBridge.TryResolveInventoryEndpoint(_playerController, _selectedInteriorStructure, inventoryId, out endpoint);
    }

    private bool TryGetActiveInteriorPlacementKind(out BuildPrototypeKind kind, out bool usesPlayerInventory)
    {
        if (_playerInteriorPlacementArmed && TryResolveSelectedPlayerPlaceable(out var selectedPlayerKind))
        {
            kind = selectedPlayerKind;
            usesPlayerInventory = true;
            return true;
        }

        if (_interiorInteractionMode == FactoryInteractionMode.Build)
        {
            kind = _selectedInteriorKind;
            usesPlayerInventory = false;
            return true;
        }

        kind = default;
        usesPlayerInventory = false;
        return false;
    }

    private void RefreshInteriorInteractionModeFromBuildSource()
    {
        if (_interiorInteractionMode == FactoryInteractionMode.Delete)
        {
            return;
        }

        _interiorInteractionMode = TryGetActiveInteriorPlacementKind(out _, out _)
            ? FactoryInteractionMode.Build
            : FactoryInteractionMode.Interact;
    }

    private static void TraceLog(string message)
    {
        GD.Print($"[MobileFactoryDemo] {message}");
    }

    private static bool HasBlueprintRecipe(FactoryStructure structure, string recipeId)
    {
        return structure.CaptureBlueprintConfiguration().TryGetValue("recipe_id", out var configuredRecipeId)
            && configuredRecipeId == recipeId;
    }

    private string GetSelectedInteriorStructureText()
    {
        if (_selectedInteriorStructure is null || !GodotObject.IsInstanceValid(_selectedInteriorStructure) || !_selectedInteriorStructure.IsInsideTree())
        {
            return _interiorInteractionMode == FactoryInteractionMode.Build
                ? "建造预览中"
                : "未选中建筑";
        }

        return $"{_selectedInteriorStructure.DisplayName} @ ({_selectedInteriorStructure.Cell.X}, {_selectedInteriorStructure.Cell.Y}) | HP {_selectedInteriorStructure.CurrentHealth:0}/{_selectedInteriorStructure.MaxHealth:0}";
    }

    private int CountEditableInteriorStructures()
    {
        if (_mobileFactory is null)
        {
            return 0;
        }

        var count = 0;
        for (var y = _mobileFactory.InteriorMinCell.Y; y <= _mobileFactory.InteriorMaxCell.Y; y++)
        {
            for (var x = _mobileFactory.InteriorMinCell.X; x <= _mobileFactory.InteriorMaxCell.X; x++)
            {
                var cell = new Vector2I(x, y);
                if (_mobileFactory.TryGetInteriorStructure(cell, out var structure) && structure is not null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private int GetPrimaryDeliveryTotal()
    {
        if (!UseLargeTestScenario)
        {
            return _sinkA?.DeliveredTotal ?? 0;
        }

        var sum = 0;
        for (var i = 0; i < _scenarioSinks.Count; i += 2)
        {
            sum += _scenarioSinks[i].DeliveredTotal;
        }

        return sum;
    }

    private int GetSecondaryDeliveryTotal()
    {
        if (!UseLargeTestScenario)
        {
            return _sinkB?.DeliveredTotal ?? 0;
        }

        var sum = 0;
        for (var i = 1; i < _scenarioSinks.Count; i += 2)
        {
            sum += _scenarioSinks[i].DeliveredTotal;
        }

        return sum;
    }

    private int GetScenarioDeliveryTotal()
    {
        var sum = 0;
        for (var index = 0; index < _scenarioSinks.Count; index++)
        {
            sum += _scenarioSinks[index].DeliveredTotal;
        }

        return sum;
    }

    private void CreateWorldPreviewVisuals(int footprintCount, int portCount)
    {
        if (_worldPreviewRoot is null)
        {
            return;
        }

        FactoryPreviewPoolSupport.ClearChildren(_worldPreviewRoot);

        _worldPreviewFootprintMeshes.Clear();
        _worldPreviewPortMeshes.Clear();
        _worldPreviewMiningMeshes.Clear();
        _worldPreviewMiningLinkMeshes.Clear();
        _worldPreviewFacingArrow = null;

        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _worldPreviewRoot,
            _worldPreviewFootprintMeshes,
            footprintCount,
            index =>
            {
                var footprint = FactoryPreviewOverlaySupport.CreatePreviewCell($"PreviewFootprint_{index}", FactoryConstants.CellSize);
                footprint.Visible = false;
                return footprint;
            });

        FactoryPreviewPoolSupport.EnsureNodeCapacity(
            _worldPreviewRoot,
            _worldPreviewPortMeshes,
            portCount,
            index => FactoryPreviewOverlaySupport.CreatePortHintArrow($"PreviewPort_{index}", FactoryConstants.CellSize));

        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _worldPreviewRoot,
            _worldPreviewMiningLinkMeshes,
            portCount,
            index => new MeshInstance3D
            {
                Name = $"PreviewMiningLink_{index}",
                Visible = false,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(0.10f, 0.04f, 0.001f)
                }
            });

        _worldPreviewFacingArrow = FactoryPreviewOverlaySupport.CreateFacingArrow("PreviewFacingArrow", FactoryConstants.CellSize, 0.32f);
        _worldPreviewRoot.AddChild(_worldPreviewFacingArrow);
    }

    private void EnsureWorldPreviewVisualCapacity(int footprintCount, int portCount, int miningCount)
    {
        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _worldPreviewRoot,
            _worldPreviewFootprintMeshes,
            footprintCount,
            index =>
            {
                var footprint = FactoryPreviewOverlaySupport.CreatePreviewCell($"PreviewFootprint_{index}", FactoryConstants.CellSize);
                footprint.Visible = false;
                return footprint;
            });

        FactoryPreviewPoolSupport.EnsureNodeCapacity(
            _worldPreviewRoot,
            _worldPreviewPortMeshes,
            portCount,
            index => FactoryPreviewOverlaySupport.CreatePortHintArrow($"PreviewPort_{index}", FactoryConstants.CellSize));

        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _worldPreviewRoot,
            _worldPreviewMiningMeshes,
            miningCount,
            index => new MeshInstance3D
            {
                Name = $"PreviewMiningStake_{index}",
                Visible = false,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            });

        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _worldPreviewRoot,
            _worldPreviewMiningLinkMeshes,
            miningCount,
            index => new MeshInstance3D
            {
                Name = $"PreviewMiningLink_{index}",
                Visible = false,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(0.10f, 0.04f, 0.001f)
                }
            });
    }

    private void CreateInteriorPreviewVisuals()
    {
        if (_interiorPreviewRoot is null)
        {
            return;
        }

        _interiorPreviewCell = FactoryPreviewOverlaySupport.CreatePreviewCell("InteriorPreviewCell", 0.615f, scale: 0.78f, height: 0.06f, y: 0.04f);
        _interiorPreviewRoot.AddChild(_interiorPreviewCell);

        _interiorPreviewArrow = FactoryPreviewOverlaySupport.CreateFacingArrow("InteriorPreviewArrow", 0.5f, 0.12f);
        _interiorPreviewRoot.AddChild(_interiorPreviewArrow);

        _interiorPreviewPowerRange = FactoryPreviewOverlaySupport.CreatePreviewPowerRange("InteriorPreviewPowerRange");
        _interiorPreviewRoot.AddChild(_interiorPreviewPowerRange);

        FactoryPreviewOverlaySupport.ApplyPreviewColor(_interiorPreviewPowerRange, new Color(0.35f, 0.95f, 0.55f, 0.15f));
    }

    private void UpdateInteriorPreviewSizing()
    {
        if (_mobileFactory is null || _interiorPreviewCell is null || _interiorPreviewArrow is null || _interiorPreviewRoot is null)
        {
            return;
        }

        var cellSize = _mobileFactory.InteriorSite.CellSize;
        _interiorPreviewCell.Mesh = new BoxMesh { Size = new Vector3(cellSize * 0.78f, 0.06f, cellSize * 0.78f) };
        _interiorPreviewCell.Position = new Vector3(0.0f, 0.04f, 0.0f);

        _interiorPreviewArrow.QueueFree();
        _interiorPreviewArrow = FactoryPreviewOverlaySupport.CreateFacingArrow("InteriorPreviewArrow", cellSize * 0.6f, 0.12f);
        _interiorPreviewRoot.AddChild(_interiorPreviewArrow);
    }

    private void EnsureInteriorAttachmentPreviewMeshCount(List<MeshInstance3D> meshes, int count, Vector3 size)
    {
        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            _interiorPreviewRoot,
            meshes,
            count,
            _ => new MeshInstance3D
            {
                Visible = false,
                Mesh = new BoxMesh { Size = size }
            });
        FactoryPreviewPoolSupport.RefreshMeshGeometry(meshes, mesh => mesh.Mesh = new BoxMesh { Size = size });
    }

    private void UpdateInteriorPortHints(BuildPrototypeKind previewKind)
    {
        if (_mobileFactory is null
            || _interiorPortHintRoot is null
            || !_hasHoveredInteriorCell
            || !FactoryLogisticsPreview.ShouldShowContextualPortHints(previewKind))
        {
            SetInteriorPortHintCount(0);
            return;
        }

        var visibleRect = GetVisibleInteriorCellRectOrBounds();
        var markers = GetInteriorPortMarkers(previewKind, visibleRect);
        EnsureInteriorPortHintMeshCount(markers.Count);
        var visibleCount = 0;
        for (var index = 0; index < markers.Count; index++)
        {
            var marker = markers[index];
            var arrow = _interiorPortHintMeshes[index];
            FactoryPreviewOverlaySupport.ConfigureDirectionalArrow(
                arrow,
                _mobileFactory.InteriorSite.CellToWorld(marker.Cell) + new Vector3(0.0f, marker.IsHighlighted ? 0.13f : 0.10f, 0.0f),
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

        SetInteriorPortHintCount(visibleCount);
    }

    private void EnsureInteriorPortHintMeshCount(int count)
    {
        FactoryPreviewPoolSupport.EnsureNodeCapacity(
            _interiorPortHintRoot,
            _interiorPortHintMeshes,
            count,
            index => FactoryPreviewOverlaySupport.CreatePortHintArrow($"InteriorPortHint_{index}", _mobileFactory?.InteriorSite.CellSize ?? FactoryConstants.CellSize));
    }

    private void SetInteriorPortHintCount(int visibleCount)
    {
        FactoryPreviewPoolSupport.SetVisibleNodeCount(_interiorPortHintRoot, _interiorPortHintMeshes, visibleCount);
    }

    private static Color GetWorldPortPreviewColor(MobileFactoryAttachmentChannelType channelType, MobileFactoryDeployState deployState)
    {
        var blocked = deployState == MobileFactoryDeployState.Blocked;
        return channelType == MobileFactoryAttachmentChannelType.ItemInput
            ? blocked
                ? new Color(0.60f, 0.78f, 1.0f, 0.58f)
                : new Color(0.38f, 0.78f, 1.0f, 0.68f)
            : blocked
                ? new Color(1.0f, 0.55f, 0.35f, 0.58f)
                : new Color(0.98f, 0.72f, 0.34f, 0.68f);
    }

    private List<FactoryPortPreviewMarker> GetInteriorPortMarkers(BuildPrototypeKind previewKind, Rect2I visibleRect)
    {
        if (_mobileFactory is null)
        {
            _cachedInteriorPortMarkers.Clear();
            return _cachedInteriorPortMarkers;
        }

        var structureRevision = _mobileFactory.InteriorSite.StructureRevision;
        if (_hasCachedInteriorPortMarkers
            && _cachedInteriorPortKind == previewKind
            && _cachedInteriorPortFacing == _selectedInteriorFacing
            && _cachedInteriorPortCell == _hoveredInteriorCell
            && _cachedInteriorPortVisibleRect == visibleRect
            && _cachedInteriorPortRevision == structureRevision)
        {
            return _cachedInteriorPortMarkers;
        }

        _cachedInteriorPortMarkers.Clear();
        _cachedInteriorPortMarkers.AddRange(FactoryLogisticsPreview.CollectPortMarkers(
            _mobileFactory.InteriorSite,
            previewKind,
            _hoveredInteriorCell,
            _selectedInteriorFacing,
            EnumerateInteriorStructuresInRect(visibleRect)));
        _cachedInteriorPortKind = previewKind;
        _cachedInteriorPortFacing = _selectedInteriorFacing;
        _cachedInteriorPortCell = _hoveredInteriorCell;
        _cachedInteriorPortVisibleRect = visibleRect;
        _cachedInteriorPortRevision = structureRevision;
        _hasCachedInteriorPortMarkers = true;
        return _cachedInteriorPortMarkers;
    }

    private Rect2I GetVisibleInteriorCellRectOrBounds()
    {
        if (_mobileFactory is null)
        {
            return default;
        }

        return TryGetVisibleInteriorCellRect(out var visibleRect)
            ? visibleRect
            : BuildCellRect(_mobileFactory.InteriorSite.MinCell, _mobileFactory.InteriorSite.MaxCell);
    }

    private IEnumerable<FactoryStructure> EnumerateInteriorStructuresInRect(Rect2I rect)
    {
        if (_mobileFactory is null || rect.Size.X <= 0 || rect.Size.Y <= 0)
        {
            yield break;
        }

        var seenStructureIds = new HashSet<ulong>();
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                if (!_mobileFactory.InteriorSite.TryGetStructure(new Vector2I(x, y), out var structure) || structure is null)
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

    private bool TryGetVisibleInteriorCellRect(out Rect2I visibleRect)
    {
        visibleRect = default;

        if (_mobileFactory is null || _editorCamera is null || _hud is null)
        {
            return false;
        }

        var viewportSize = _hud.EditorViewport.Size;
        if (viewportSize.X <= 0 || viewportSize.Y <= 0)
        {
            return false;
        }

        var corners = new[]
        {
            Vector2.Zero,
            new Vector2(viewportSize.X, 0.0f),
            new Vector2(0.0f, viewportSize.Y),
            new Vector2(viewportSize.X, viewportSize.Y)
        };

        var minCell = new Vector2I(int.MaxValue, int.MaxValue);
        var maxCell = new Vector2I(int.MinValue, int.MinValue);
        for (var index = 0; index < corners.Length; index++)
        {
            if (!TryProjectEditorScreenToInterior(corners[index], out var worldPosition))
            {
                return false;
            }

            var cell = _mobileFactory.InteriorSite.WorldToCell(worldPosition);
            minCell = new Vector2I(System.Math.Min(minCell.X, cell.X), System.Math.Min(minCell.Y, cell.Y));
            maxCell = new Vector2I(System.Math.Max(maxCell.X, cell.X), System.Math.Max(maxCell.Y, cell.Y));
        }

        visibleRect = new Rect2I(
            minCell - Vector2I.One,
            (maxCell - minCell) + new Vector2I(3, 3));
        return true;
    }

    private bool TryProjectEditorScreenToInterior(Vector2 screenPosition, out Vector3 worldPosition)
    {
        worldPosition = Vector3.Zero;

        if (_editorCamera is null || _mobileFactory is null)
        {
            return false;
        }

        var rayOrigin = _editorCamera.ProjectRayOrigin(screenPosition);
        var rayDirection = _editorCamera.ProjectRayNormal(screenPosition);
        var planeY = _mobileFactory.InteriorSite.WorldOrigin.Y;

        if (Mathf.Abs(rayDirection.Y) < 0.001f)
        {
            return false;
        }

        var distance = (planeY - rayOrigin.Y) / rayDirection.Y;
        if (distance < 0.0f)
        {
            return false;
        }

        worldPosition = rayOrigin + rayDirection * distance;
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
    private void EnsureInputActions()
    {
        FactoryDemoInputActions.EnsureCommonActions();
        FactoryDemoInputActions.EnsureAction("factory_move_forward", new InputEventKey { PhysicalKeycode = Key.W });
        FactoryDemoInputActions.EnsureAction("factory_move_backward", new InputEventKey { PhysicalKeycode = Key.S });
        FactoryDemoInputActions.EnsureAction("factory_turn_left", new InputEventKey { PhysicalKeycode = Key.A });
        FactoryDemoInputActions.EnsureAction("factory_turn_right", new InputEventKey { PhysicalKeycode = Key.D });
        FactoryDemoInputActions.EnsureAction("deploy_rotate_left", new InputEventKey { PhysicalKeycode = Key.Q });
        FactoryDemoInputActions.EnsureAction("deploy_rotate_right", new InputEventKey { PhysicalKeycode = Key.E });
        FactoryDemoInputActions.EnsureAction("toggle_factory_command", new InputEventKey { PhysicalKeycode = Key.C });
        FactoryDemoInputActions.EnsureAction("toggle_observer_mode", new InputEventKey { PhysicalKeycode = Key.Tab });
        FactoryDemoInputActions.EnsureAction("toggle_deploy_preview", new InputEventKey { PhysicalKeycode = Key.G });
        FactoryDemoInputActions.EnsureAction("cancel_mobile_command", new InputEventKey { PhysicalKeycode = Key.Escape });
        FactoryDemoInputActions.EnsureAction("mobile_factory_auxiliary_command", new InputEventKey { PhysicalKeycode = Key.R });
        FactoryDemoInputActions.EnsureAction("toggle_mobile_editor", new InputEventKey { PhysicalKeycode = Key.F });
    }

    private string DescribeInteriorPlacementPreview(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        var displayName = GetInteriorDisplayName(kind);
        if (_mobileFactory is not null
            && kind == BuildPrototypeKind.Belt
            && FactoryTransportTopology.TryGetBeltMidspanMergeTarget(_mobileFactory.InteriorSite, cell, facing, out var mergeTargetCell))
        {
            return $"可在内部格 ({cell.X}, {cell.Y}) 铺设{displayName}，并把供料并入 ({mergeTargetCell.X}, {mergeTargetCell.Y}) 的嵌入物流层。";
        }

        return $"可在内部格 ({cell.X}, {cell.Y}) 放置{displayName}。{FactoryIndustrialStandards.GetInteriorPreviewSummary(kind)}";
    }

    private static IReadOnlyList<BuildPrototypeKind> GetInteriorHotkeyPalette()
    {
        return FactoryIndustrialStandards.GetHotkeyPaletteKinds(FactorySiteKind.Interior, InteriorPaletteKeys.Length);
    }

    private string GetInteriorDisplayName(BuildPrototypeKind kind)
    {
        return FactoryIndustrialStandards.GetInteriorPresentationLabel(kind);
    }


    private int CountConnectedAttachments(BuildPrototypeKind kind)
    {
        if (_mobileFactory is null)
        {
            return 0;
        }

        var count = 0;
        foreach (var attachment in _mobileFactory.BoundaryAttachments)
        {
            if (attachment.Kind == kind && attachment.IsConnectedToWorld)
            {
                count++;
            }
        }

        return count;
    }


    private void SetControlMode(MobileFactoryControlMode controlMode)
    {
        _controlMode = controlMode;
        if (controlMode != MobileFactoryControlMode.DeployPreview)
        {
            _currentDeployEvaluation = null;
        }
        FocusFactoryForCurrentMode();
    }

    private void PullFactoryStatusMessage()
    {
        if (_mobileFactory?.ConsumeStatusMessage() is string message)
        {
            ShowWorldEvent(message, true);
        }
    }

    private void ShowWorldEvent(string message, bool positive)
    {
        _worldEventMessage = message;
        _worldEventPositive = positive;
        _worldEventTimer = 2.6f;
    }

    private string GetHintText()
    {
        return _controlMode switch
        {
            MobileFactoryControlMode.Player => "玩家模式：WASD 移动主角，镜头跟随角色；用底部热栏切换建筑，用左上按钮或 C 进入工厂控制。",
            MobileFactoryControlMode.Observer => "观察模式：WASD/方向键移动相机 | 滚轮缩放 | Tab 返回玩家控制 | F 内部编辑",
            MobileFactoryControlMode.DeployPreview => "部署预览：左键确认 | Q/E/R 旋转朝向 | G/Esc 取消并返回玩家控制 | F 内部编辑",
            _ => "工厂控制：W/S 前进后退 | A/D 转向 | C 返回玩家控制 | G 部署预览 | Tab 观察模式 | R 切回移动态 | F 内部编辑；编辑器里和 sandbox 一样，X 进删除模式，右键或 Esc 回交互，Delete 拆除悬停建筑"
        };
    }

    private void RegisterScenarioSink(SinkStructure? sink)
    {
        if (sink is not null)
        {
            _scenarioSinks.Add(sink);
        }
    }

    private Vector3 GetPreviewFacingArrowPosition(Vector2I anchorCell, FacingDirection facing)
    {
        if (_grid is null || _mobileFactory is null)
        {
            return Vector3.Zero;
        }

        var center = GetPreviewCenter(anchorCell, facing);
        return center
            + FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(facing)) * (FactoryConstants.CellSize * 0.75f)
            + new Vector3(0.0f, 0.44f, 0.0f);
    }

    private Vector3 GetPreviewCenter(Vector2I anchorCell, FacingDirection facing)
    {
        if (_grid is null || _mobileFactory is null)
        {
            return Vector3.Zero;
        }

        var sum = Vector3.Zero;
        var count = 0;
        foreach (var cell in _mobileFactory.GetFootprintCells(anchorCell, facing))
        {
            sum += _grid.CellToWorld(cell);
            count++;
        }

        return count > 0 ? sum / count : _grid.CellToWorld(anchorCell);
    }

    private void ConfigureMiningPreviewMarker(MeshInstance3D meshInstance, Vector2I cell, bool isEligible, bool isDeployed, Color color)
    {
        if (_grid is null)
        {
            return;
        }

        if (isEligible)
        {
            meshInstance.Mesh = new CylinderMesh
            {
                TopRadius = FactoryConstants.CellSize * 0.10f,
                BottomRadius = FactoryConstants.CellSize * 0.12f,
                Height = isDeployed ? 0.52f : 0.34f
            };
            meshInstance.Position = _grid.CellToWorld(cell) + new Vector3(0.0f, isDeployed ? 0.26f : 0.17f, 0.0f);
        }
        else
        {
            meshInstance.Mesh = new CylinderMesh
            {
                TopRadius = FactoryConstants.CellSize * 0.18f,
                BottomRadius = FactoryConstants.CellSize * 0.18f,
                Height = 0.06f
            };
            meshInstance.Position = _grid.CellToWorld(cell) + new Vector3(0.0f, 0.05f, 0.0f);
        }

        FactoryPreviewOverlaySupport.ApplyMiningPreviewColor(meshInstance, color);
    }

    private void ConfigureMiningPreviewLink(MeshInstance3D meshInstance, Vector2I portCell, Vector2I targetCell, Color color)
    {
        if (_grid is null || portCell == targetCell)
        {
            meshInstance.Visible = false;
            return;
        }

        var start = _grid.CellToWorld(portCell) + new Vector3(0.0f, 0.12f, 0.0f);
        var end = _grid.CellToWorld(targetCell) + new Vector3(0.0f, 0.12f, 0.0f);
        var delta = end - start;
        var length = new Vector2(delta.X, delta.Z).Length();
        if (length <= 0.05f)
        {
            meshInstance.Visible = false;
            return;
        }

        meshInstance.Visible = true;
        meshInstance.Mesh = new BoxMesh { Size = new Vector3(0.10f, 0.04f, length) };
        meshInstance.Position = start + (delta * 0.5f);
        meshInstance.Rotation = new Vector3(0.0f, Mathf.Atan2(delta.X, delta.Z), 0.0f);
        FactoryPreviewOverlaySupport.ApplyMiningPreviewColor(meshInstance, new Color(color.R, color.G, color.B, color.A * 0.82f));
    }

    private int GetWorldMinCell()
    {
        return UseLargeTestScenario ? -20 : FactoryConstants.GridMin;
    }

    private int GetWorldMaxCell()
    {
        return UseLargeTestScenario ? 20 : FactoryConstants.GridMax;
    }

    private static Node3D CreateScenarioLandmarks()
    {
        var root = new Node3D { Name = "ScenarioLandmarks" };
        root.AddChild(CreateLandmark("NorthYard", new Vector3(-14.0f, 0.0f, 14.0f), new Vector3(8.0f, 0.6f, 6.0f), new Color("1F2937")));
        root.AddChild(CreateLandmark("SouthExport", new Vector3(12.0f, 0.0f, -12.0f), new Vector3(10.0f, 0.6f, 5.0f), new Color("312E81")));
        root.AddChild(CreateLandmark("CentralSpine", new Vector3(0.0f, 0.0f, 0.0f), new Vector3(16.0f, 0.4f, 2.8f), new Color("0F766E")));

        root.AddChild(CreateLandmarkLabel("北侧扩展试验区", new Vector3(-14.0f, 1.8f, 14.0f), new Color("BFDBFE")));
        root.AddChild(CreateLandmarkLabel("南侧输出走廊", new Vector3(12.0f, 1.8f, -12.0f), new Color("C4B5FD")));
        root.AddChild(CreateLandmarkLabel("中央观察脊", new Vector3(0.0f, 1.6f, 0.0f), new Color("A7F3D0")));

        return root;
    }

    private static Node3D CreateLandmark(string name, Vector3 position, Vector3 size, Color color)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Position = position + new Vector3(0.0f, size.Y * 0.5f, 0.0f),
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.95f
            }
        };
        return mesh;
    }

    private static Label3D CreateLandmarkLabel(string text, Vector3 position, Color color)
    {
        return new Label3D
        {
            Text = text,
            Position = position,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 36,
            Modulate = color,
            OutlineSize = 3
        };
    }

}

