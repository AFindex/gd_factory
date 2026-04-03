using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MobileFactoryDemo : Node3D
{
    private const int InteriorRenderLayer = 1;
    private const int HullRenderLayer = 2;

    private static readonly Vector2I AnchorA = new(-6, -3);
    private static readonly Vector2I AnchorB = new(2, 3);
    private static readonly Vector2I BlockedAnchor = new(-1, 1);
    private static readonly BuildPrototypeKind[] InteriorPalette =
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
        [BuildPrototypeKind.Producer] = new BuildPrototypeDefinition(BuildPrototypeKind.Producer, "生产器", new Color("9DC08B"), "持续向前方投放原料。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把左右两路物流汇成前方一路。"),
        [BuildPrototypeKind.Bridge] = new BuildPrototypeDefinition(BuildPrototypeKind.Bridge, "跨桥", new Color("F59E0B"), "让南北和东西两路物流跨越而不互连。"),
        [BuildPrototypeKind.Loader] = new BuildPrototypeDefinition(BuildPrototypeKind.Loader, "装载器", new Color("FDBA74"), "把后方带上的物品装入前方机器或回收端。"),
        [BuildPrototypeKind.Unloader] = new BuildPrototypeDefinition(BuildPrototypeKind.Unloader, "卸载器", new Color("93C5FD"), "把机器端输出卸到前方传送网络。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收器", new Color("FDE68A"), "吞掉输入物品并作为内部消费端。"),
        [BuildPrototypeKind.Storage] = new BuildPrototypeDefinition(BuildPrototypeKind.Storage, "仓储", new Color("94A3B8"), "缓存多件物品，可向前输出，也能被机械臂抓取。"),
        [BuildPrototypeKind.Inserter] = new BuildPrototypeDefinition(BuildPrototypeKind.Inserter, "机械臂", new Color("FACC15"), "从后方抓取一件物品并向前投送。"),
        [BuildPrototypeKind.Wall] = new BuildPrototypeDefinition(BuildPrototypeKind.Wall, "墙体", new Color("D1D5DB"), "给移动工厂的前缘补上一段高耐久掩体。"),
        [BuildPrototypeKind.AmmoAssembler] = new BuildPrototypeDefinition(BuildPrototypeKind.AmmoAssembler, "弹药组装器", new Color("FB923C"), "在内部持续生产弹药，直接喂给炮塔。"),
        [BuildPrototypeKind.GunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.GunTurret, "机枪炮塔", new Color("CBD5E1"), "会跟随移动工厂整体旋转，对世界中的敌人自动转向并射击。"),
        [BuildPrototypeKind.OutputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.OutputPort, "输出端口", new Color("FB923C"), "将移动工厂内部物流送往世界网格。"),
        [BuildPrototypeKind.InputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.InputPort, "输入端口", new Color("60A5FA"), "把世界侧物流导入移动工厂内部。")
    };

    private GridManager? _grid;
    private SimulationController? _simulation;
    private FactoryCameraRig? _cameraRig;
    private MobileFactoryHud? _hud;
    private Node3D? _structureRoot;
    private Node3D? _enemyRoot;
    private Node3D? _worldPreviewRoot;
    private Node3D? _interiorPreviewRoot;
    private Node3D? _interiorBlueprintPreviewRoot;
    private Camera3D? _editorCamera;
    private FactoryCombatDirector? _combatDirector;
    private readonly List<MeshInstance3D> _worldPreviewFootprintMeshes = new();
    private readonly List<MeshInstance3D> _worldPreviewPortMeshes = new();
    private MeshInstance3D? _worldPreviewFacingArrow;
    private MeshInstance3D? _interiorPreviewCell;
    private MeshInstance3D? _interiorPreviewArrow;
    private readonly List<MeshInstance3D> _interiorPreviewBoundaryMeshes = new();
    private readonly List<MeshInstance3D> _interiorPreviewExteriorMeshes = new();
    private readonly List<MeshInstance3D> _interiorBlueprintPreviewMeshes = new();
    private FactoryBlueprintSiteAdapter? _interiorBlueprintSite;

    private MobileFactoryInstance? _mobileFactory;
    private readonly List<MobileFactoryInstance> _backgroundFactories = new();
    private readonly List<MobileFactoryScenarioActorController> _backgroundControllers = new();
    private readonly List<Label3D> _factoryLabels = new();
    private readonly Dictionary<MobileFactoryInstance, Label3D> _factoryLabelMap = new();
    private readonly List<SinkStructure> _scenarioSinks = new();
    private SinkStructure? _sinkA;
    private SinkStructure? _sinkB;
    private MobileFactoryControlMode _controlMode = MobileFactoryControlMode.FactoryCommand;
    private FacingDirection _selectedDeployFacing = FacingDirection.East;
    private Vector2I _hoveredAnchor;
    private bool _hasHoveredAnchor;
    private bool _canDeployCurrentAnchor;
    private bool _worldStatusPositive = true;
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
    private bool _hasHoveredInteriorCell;
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

    public override void _Ready()
    {
        EnsureInputActions();
        BuildSceneGraph();
        ConfigureGameplay();
        CreateWorldLoops();
        SpawnMobileFactory();
        PullFactoryStatusMessage();
        UpdateWorldStatusMessage(0.0);
        UpdateHud();

        if (HasFocusedSmokeTestFlag())
        {
            CallDeferred(nameof(RunSmokeChecks));
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
    }

    public override void _UnhandledInput(InputEvent @event)
    {
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
        AddChild(CreateEnvironment());
        AddChild(CreateDirectionalLight());
        AddChild(CreateFloor(GetWorldMinCell(), GetWorldMaxCell()));
        AddChild(CreateGridLines(GetWorldMinCell(), GetWorldMaxCell()));

        if (UseLargeTestScenario)
        {
            AddChild(CreateScenarioLandmarks());
        }

        _structureRoot = new Node3D { Name = "MobileDemoStructures" };
        AddChild(_structureRoot);

        _enemyRoot = new Node3D { Name = "MobileEnemyRoot" };
        AddChild(_enemyRoot);

        _worldPreviewRoot = new Node3D { Name = "WorldPreviewRoot" };
        AddChild(_worldPreviewRoot);
        CreateWorldPreviewVisuals(4, 1);

        _interiorPreviewRoot = new Node3D { Name = "InteriorPreviewRoot", Visible = false };
        AddChild(_interiorPreviewRoot);
        CreateInteriorPreviewVisuals();

        _interiorBlueprintPreviewRoot = new Node3D { Name = "InteriorBlueprintPreviewRoot", Visible = false };
        AddChild(_interiorBlueprintPreviewRoot);

        _simulation = new SimulationController { Name = "SimulationController" };
        AddChild(_simulation);

        _combatDirector = new FactoryCombatDirector { Name = "MobileCombatDirector" };
        AddChild(_combatDirector);

        _cameraRig = new FactoryCameraRig();
        AddChild(_cameraRig);

        _hud = new MobileFactoryHud();
        _hud.EditorPaletteSelected += OnEditorPaletteSelected;
        _hud.EditorRotateRequested += OnEditorRotateRequested;
        _hud.ObserverModeToggleRequested += ToggleObserverMode;
        _hud.DeployModeToggleRequested += ToggleDeployPreview;
        _hud.EditorDetailInventoryMoveRequested += HandleEditorDetailInventoryMoveRequested;
        _hud.EditorDetailRecipeSelected += HandleEditorDetailRecipeSelected;
        _hud.EditorDetailClosed += HandleEditorDetailClosed;
        _hud.BlueprintCaptureSelectionRequested += StartInteriorBlueprintCapture;
        _hud.BlueprintCaptureFullRequested += CaptureCurrentInteriorBlueprint;
        _hud.BlueprintSaveRequested += HandleInteriorBlueprintSaveRequested;
        _hud.BlueprintSelected += HandleInteriorBlueprintSelected;
        _hud.BlueprintApplyRequested += EnterInteriorBlueprintApplyMode;
        _hud.BlueprintConfirmRequested += ConfirmInteriorBlueprintApply;
        _hud.BlueprintDeleteRequested += HandleInteriorBlueprintDeleteRequested;
        _hud.BlueprintCancelRequested += CancelInteriorBlueprintWorkflow;
        AddChild(_hud);

        AddChild(new LauncherNavigationOverlay());
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

        var focusedProfile = MobileFactoryScenarioLibrary.CreateFocusedDemoProfile();
        _sinkA = CreatePreparedOutputLine(focusedProfile, AnchorA, FacingDirection.East, 2);
        _sinkB = CreatePreparedOutputLine(focusedProfile, AnchorB, FacingDirection.East, 2);
        CreatePreparedMountOutputLine(focusedProfile, AnchorA, FacingDirection.East, "east-output-aux", 1);
        CreatePreparedMountOutputLine(focusedProfile, AnchorB, FacingDirection.East, "east-output-aux", 1);
        CreatePreparedInputLine(focusedProfile, AnchorA, FacingDirection.East, 1);
        CreatePreparedInputLine(focusedProfile, AnchorB, FacingDirection.East, 3);
        CreateAmbientBranchDepot(new Vector2I(4, -9));
        CreateAmbientStorageDepot(new Vector2I(4, 8));
        CreateAmbientBridgeCrossing(new Vector2I(-1, 8));
        CreateAmbientLoaderRelay(new Vector2I(-10, 6));
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
        }

        if (_mobileFactory is not null)
        {
            _selectedDeployFacing = _mobileFactory.TransitFacing;
            CreateWorldPreviewVisuals(
                _mobileFactory.Profile.FootprintOffsetsEast.Count,
                Mathf.Max(1, _mobileFactory.Profile.AttachmentMounts.Count));
            UpdateInteriorPreviewSizing();
            _interiorBlueprintSite = CreateInteriorBlueprintSiteAdapter();
        }
    }

    private FactoryStructure? PlaceWorldStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (_grid is null || _structureRoot is null || _simulation is null || !_grid.CanPlace(cell))
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
        var heavy = MobileFactoryScenarioLibrary.CreateHeavyProfile();
        var medium = MobileFactoryScenarioLibrary.CreateMediumProfile();
        var compact = MobileFactoryScenarioLibrary.CreateCompactProfile();

        _sinkA = CreatePreparedOutputLine(heavy, new Vector2I(-15, -6), FacingDirection.East, 3);
        CreatePreparedOutputLine(compact, new Vector2I(6, -6), FacingDirection.East, 2);
        CreatePreparedOutputLine(compact, new Vector2I(10, 2), FacingDirection.East, 2);
        _sinkB = CreatePreparedOutputLine(medium, new Vector2I(-4, 7), FacingDirection.East, 2);
        CreatePreparedOutputLine(compact, new Vector2I(1, 9), FacingDirection.East, 2);
        CreatePreparedOutputLine(compact, new Vector2I(-9, 10), FacingDirection.East, 2);
        CreatePreparedOutputLine(medium, new Vector2I(-12, 3), FacingDirection.East, 2);
        CreatePreparedOutputLine(medium, new Vector2I(4, 10), FacingDirection.East, 2);
        CreatePreparedInputLine(medium, new Vector2I(-12, 3), FacingDirection.East, 3);

        CreateAmbientWorldLine(new Vector2I(-18, -1), 5, FacingDirection.East);
        CreateAmbientWorldLine(new Vector2I(12, -12), 4, FacingDirection.North);
        CreateAmbientWorldLine(new Vector2I(-3, -14), 6, FacingDirection.East);
        CreateAmbientBranchDepot(new Vector2I(12, 14));
        CreateAmbientStorageDepot(new Vector2I(-19, 14));
        CreateAmbientBridgeCrossing(new Vector2I(16, -15));
        CreateAmbientLoaderRelay(new Vector2I(-18, -15));
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

            AddFactoryLabel(instance, actor.DisplayLabel, actor.LabelColor);
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

        PlaceWorldStructure(BuildPrototypeKind.Producer, cells[^1], inboundFacing);
        for (var i = 0; i < cells.Count - 1; i++)
        {
            PlaceWorldStructure(BuildPrototypeKind.Belt, cells[i], inboundFacing);
        }
    }

    private void CreateAmbientWorldLine(Vector2I startCell, int beltCount, FacingDirection facing)
    {
        var cell = startCell;
        PlaceWorldStructure(BuildPrototypeKind.Producer, cell, facing);

        for (var i = 1; i <= beltCount; i++)
        {
            cell += FactoryDirection.ToCellOffset(facing);
            PlaceWorldStructure(BuildPrototypeKind.Belt, cell, facing);
        }

        cell += FactoryDirection.ToCellOffset(facing);
        var sink = PlaceWorldStructure(BuildPrototypeKind.Sink, cell, facing) as SinkStructure;
        if (sink is not null)
        {
            _scenarioSinks.Add(sink);
        }
    }

    private void CreateAmbientBranchDepot(Vector2I originCell)
    {
        PlaceWorldStructure(BuildPrototypeKind.Producer, originCell, FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(1, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Splitter, originCell + new Vector2I(2, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(2, -1), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(3, -1), FacingDirection.South);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(2, 1), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(3, 1), FacingDirection.North);
        PlaceWorldStructure(BuildPrototypeKind.Merger, originCell + new Vector2I(3, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(4, 0), FacingDirection.East);
        RegisterScenarioSink(PlaceWorldStructure(BuildPrototypeKind.Sink, originCell + new Vector2I(5, 0), FacingDirection.East) as SinkStructure);
    }

    private void CreateAmbientStorageDepot(Vector2I originCell)
    {
        PlaceWorldStructure(BuildPrototypeKind.Producer, originCell, FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(1, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Storage, originCell + new Vector2I(2, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Inserter, originCell + new Vector2I(3, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(4, 0), FacingDirection.East);
        RegisterScenarioSink(PlaceWorldStructure(BuildPrototypeKind.Sink, originCell + new Vector2I(5, 0), FacingDirection.East) as SinkStructure);
        PlaceWorldStructure(BuildPrototypeKind.Producer, originCell + new Vector2I(2, -2), FacingDirection.South);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(2, -1), FacingDirection.South);
    }

    private void CreateAmbientBridgeCrossing(Vector2I centerCell)
    {
        PlaceWorldStructure(BuildPrototypeKind.Producer, centerCell + new Vector2I(-2, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, centerCell + new Vector2I(-1, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Bridge, centerCell, FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, centerCell + new Vector2I(1, 0), FacingDirection.East);
        RegisterScenarioSink(PlaceWorldStructure(BuildPrototypeKind.Sink, centerCell + new Vector2I(2, 0), FacingDirection.East) as SinkStructure);

        PlaceWorldStructure(BuildPrototypeKind.Producer, centerCell + new Vector2I(0, -2), FacingDirection.South);
        PlaceWorldStructure(BuildPrototypeKind.Belt, centerCell + new Vector2I(0, -1), FacingDirection.South);
        PlaceWorldStructure(BuildPrototypeKind.Belt, centerCell + new Vector2I(0, 1), FacingDirection.South);
        RegisterScenarioSink(PlaceWorldStructure(BuildPrototypeKind.Sink, centerCell + new Vector2I(0, 2), FacingDirection.South) as SinkStructure);
    }

    private void CreateAmbientLoaderRelay(Vector2I originCell)
    {
        PlaceWorldStructure(BuildPrototypeKind.Producer, originCell, FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Unloader, originCell + new Vector2I(1, 0), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(2, 0), FacingDirection.South);
        PlaceWorldStructure(BuildPrototypeKind.Belt, originCell + new Vector2I(2, 1), FacingDirection.South);
        PlaceWorldStructure(BuildPrototypeKind.Loader, originCell + new Vector2I(2, 2), FacingDirection.South);
        RegisterScenarioSink(PlaceWorldStructure(BuildPrototypeKind.Sink, originCell + new Vector2I(2, 3), FacingDirection.South) as SinkStructure);
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

        for (var i = 0; i < InteriorPalette.Length && i < InteriorPaletteKeys.Length; i++)
        {
            if (keyEvent.Keycode != InteriorPaletteKeys[i])
            {
                continue;
            }

            SelectInteriorBuildKind(_selectedInteriorKind == InteriorPalette[i] && _interiorInteractionMode == FactoryInteractionMode.Build
                ? null
                : InteriorPalette[i]);
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
        }

        _hud?.SetPaneFocus(_editorOpen, _hoveringEditorPane);
        _hud?.SetEditorFocusHint(_hoveringEditorPane);
    }

    private void UpdateHoveredAnchor()
    {
        _hasHoveredAnchor = false;
        _canDeployCurrentAnchor = false;

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
        _canDeployCurrentAnchor = _mobileFactory.CanDeployAt(_grid, _hoveredAnchor, _selectedDeployFacing);
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
                _interiorPreviewMessage = $"可在内部格 ({cell.X}, {cell.Y}) 放置{_definitions[_selectedInteriorKind].DisplayName}。";
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

        if (_worldPreviewFacingArrow is not null)
        {
            _worldPreviewFacingArrow.Visible = false;
        }

        _worldPreviewRoot.Visible = _controlMode == MobileFactoryControlMode.DeployPreview && _hasHoveredAnchor && CanUseWorldInput();
        if (!_worldPreviewRoot.Visible)
        {
            return;
        }

        var footprintColor = _canDeployCurrentAnchor
            ? new Color(0.35f, 0.95f, 0.55f, 0.38f)
            : new Color(1.0f, 0.35f, 0.35f, 0.38f);
        var portColor = _canDeployCurrentAnchor
            ? new Color(0.98f, 0.72f, 0.34f, 0.55f)
            : new Color(1.0f, 0.55f, 0.35f, 0.55f);

        var footprintCells = new List<Vector2I>(_mobileFactory.GetFootprintCells(_hoveredAnchor, _selectedDeployFacing));
        var portCells = new List<Vector2I>(_mobileFactory.GetPortCells(_hoveredAnchor, _selectedDeployFacing));
        EnsureWorldPreviewVisualCapacity(footprintCells.Count, portCells.Count);

        var index = 0;
        foreach (var cell in footprintCells)
        {
            var mesh = _worldPreviewFootprintMeshes[index++];
            mesh.Visible = true;
            mesh.Position = _grid.CellToWorld(cell) + new Vector3(0.0f, 0.05f, 0.0f);
            ApplyPreviewColor(mesh, footprintColor);
        }

        index = 0;
        foreach (var cell in portCells)
        {
            var mesh = _worldPreviewPortMeshes[index++];
            mesh.Visible = true;
            mesh.Position = _grid.CellToWorld(cell) + new Vector3(0.0f, 0.08f, 0.0f);
            ApplyPreviewColor(mesh, portColor);
        }

        if (_worldPreviewFacingArrow is not null)
        {
            var center = GetPreviewCenter(_hoveredAnchor, _selectedDeployFacing);
            _worldPreviewFacingArrow.Visible = true;
            _worldPreviewFacingArrow.Position = center + FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(_selectedDeployFacing)) * (FactoryConstants.CellSize * 0.75f) + new Vector3(0.0f, 0.18f, 0.0f);
            _worldPreviewFacingArrow.Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(_selectedDeployFacing), 0.0f);
            ApplyPreviewColor(_worldPreviewFacingArrow, portColor.Lightened(0.1f));
        }
    }

    private void UpdateInteriorPreview()
    {
        if (_mobileFactory is null || _interiorPreviewRoot is null || _interiorPreviewCell is null || _interiorPreviewArrow is null)
        {
            return;
        }

        UpdateInteriorBlueprintPreview();

        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            _interiorPreviewRoot.Visible = false;
            return;
        }

        _interiorPreviewRoot.Visible = _editorOpen
            && _hoveringEditorViewport
            && ((_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection
                    && (_interiorBlueprintSelectionDragActive || _hasInteriorBlueprintSelectionRect || _hasHoveredInteriorCell))
                || (_hasHoveredInteriorCell
                    && (_interiorInteractionMode == FactoryInteractionMode.Build
                        || _interiorInteractionMode == FactoryInteractionMode.Delete)));
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

        if (_blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection)
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
                    _mobileFactory.InteriorSite.CellSize * rect.Size.X - (_mobileFactory.InteriorSite.CellSize * 0.22f),
                    0.06f,
                    _mobileFactory.InteriorSite.CellSize * rect.Size.Y - (_mobileFactory.InteriorSite.CellSize * 0.22f))
            };
            _interiorPreviewCell.Position = new Vector3(0.0f, 0.04f, 0.0f);
            _interiorPreviewArrow.Visible = false;
            var selectionTint = new Color(0.35f, 0.95f, 0.55f, 0.30f);
            ApplyPreviewColor(_interiorPreviewCell, selectionTint);
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
                    _mobileFactory.InteriorSite.CellSize * rect.Size.X - (_mobileFactory.InteriorSite.CellSize * 0.22f),
                    0.06f,
                    _mobileFactory.InteriorSite.CellSize * rect.Size.Y - (_mobileFactory.InteriorSite.CellSize * 0.22f))
            };
            _interiorPreviewCell.Position = new Vector3(0.0f, 0.04f, 0.0f);
            _interiorPreviewArrow.Visible = false;
            var deleteTint = _canDeleteInteriorCell ? new Color(1.0f, 0.35f, 0.35f, 0.42f) : new Color(0.75f, 0.30f, 0.30f, 0.28f);
            ApplyPreviewColor(_interiorPreviewCell, deleteTint);
            return;
        }

        _interiorPreviewRoot.Position = _mobileFactory.InteriorSite.CellToWorld(_hoveredInteriorCell);
        _interiorPreviewRoot.Rotation = new Vector3(
            0.0f,
            _mobileFactory.InteriorSite.WorldRotationRadians + FactoryDirection.ToYRotationRadians(_selectedInteriorFacing),
            0.0f);

        var tint = _canPlaceInteriorCell
            ? new Color(0.35f, 0.95f, 0.55f, 0.45f)
            : new Color(1.0f, 0.35f, 0.35f, 0.45f);
        _interiorPreviewCell.Mesh = new BoxMesh { Size = new Vector3(_mobileFactory.InteriorSite.CellSize * 0.78f, 0.06f, _mobileFactory.InteriorSite.CellSize * 0.78f) };
        _interiorPreviewCell.Position = new Vector3(0.0f, 0.04f, 0.0f);
        _interiorPreviewArrow.Visible = true;
        ApplyPreviewColor(_interiorPreviewCell, tint);
        ApplyPreviewColor(_interiorPreviewArrow, tint.Lightened(0.1f));

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
            ApplyPreviewColor(mesh, tint.Darkened(0.08f));
        }

        for (var i = 0; i < exteriorCells.Count; i++)
        {
            var mesh = _interiorPreviewExteriorMeshes[i];
            mesh.Visible = true;
            mesh.GlobalPosition = _mobileFactory.InteriorSite.CellToWorld(exteriorCells[i]) + new Vector3(0.0f, 0.08f, 0.0f);
            ApplyPreviewColor(mesh, tint.Lightened(0.1f));
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

        _interiorBlueprintPreviewRoot.Visible = _editorOpen && _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _interiorBlueprintPlan is not null;
        if (!_interiorBlueprintPreviewRoot.Visible || _interiorBlueprintPlan is null)
        {
            return;
        }

        _interiorBlueprintPreviewRoot.GlobalPosition = _mobileFactory.InteriorSite.WorldOrigin;
        _interiorBlueprintPreviewRoot.Rotation = new Vector3(
            0.0f,
            _mobileFactory.InteriorSite.WorldRotationRadians,
            0.0f);

        EnsureInteriorBlueprintPreviewCapacity(_interiorBlueprintPlan.Entries.Count);
        for (var index = 0; index < _interiorBlueprintPlan.Entries.Count; index++)
        {
            var entry = _interiorBlueprintPlan.Entries[index];
            var mesh = _interiorBlueprintPreviewMeshes[index];
            mesh.Visible = true;
            mesh.Position = new Vector3(
                entry.TargetCell.X * _mobileFactory.InteriorSite.CellSize,
                0.06f,
                entry.TargetCell.Y * _mobileFactory.InteriorSite.CellSize);
            ApplyPreviewColor(mesh, entry.IsValid
                ? new Color(0.35f, 0.95f, 0.55f, 0.36f)
                : new Color(1.0f, 0.35f, 0.35f, 0.36f));
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
            _worldPreviewMessage = _canDeployCurrentAnchor
                ? $"可部署到 ({_hoveredAnchor.X}, {_hoveredAnchor.Y})，朝向 {FactoryDirection.ToLabel(_selectedDeployFacing)}，确认后会自动进场部署。"
                : $"锚点 ({_hoveredAnchor.X}, {_hoveredAnchor.Y}) 以朝向 {FactoryDirection.ToLabel(_selectedDeployFacing)} 无法部署，可能越界或与现有占用冲突。";
            _worldStatusPositive = _canDeployCurrentAnchor;
            return;
        }

        if (_worldEventTimer > 0.0f && !string.IsNullOrWhiteSpace(_worldEventMessage))
        {
            _worldPreviewMessage = _worldEventMessage!;
            _worldStatusPositive = _worldEventPositive;
            return;
        }

        if (_mobileFactory is null)
        {
            _worldPreviewMessage = "移动工厂尚未生成。";
            _worldStatusPositive = false;
            return;
        }

        switch (_controlMode)
        {
            case MobileFactoryControlMode.Observer:
                _worldPreviewMessage = "观察模式：WASD/方向键移动相机，滚轮缩放，Tab 返回工厂控制。";
                _worldStatusPositive = true;
                break;
            case MobileFactoryControlMode.DeployPreview:
                _worldPreviewMessage = "部署预览：移动鼠标选择落点，Q/E/R 旋转朝向，左键确认，Esc/G 取消。";
                _worldStatusPositive = true;
                break;
            default:
                _worldPreviewMessage = _mobileFactory.State switch
                {
                    MobileFactoryLifecycleState.Deployed => "已部署：按 R 切回移动态，Tab 进入观察模式，F 打开内部编辑。",
                    MobileFactoryLifecycleState.AutoDeploying => "自动部署中：移动工厂会先朝目标行进，抵达后再转向展开，Esc 可取消。",
                    MobileFactoryLifecycleState.Recalling => "切回移动态中：部署机构正在收拢，很快恢复机动。",
                    _ => "工厂控制：W/S 前进后退，A/D 转向，G 进入部署模式，Tab 进入观察模式。"
                };
                _worldStatusPositive = true;
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
        foreach (var child in _structureRoot.GetChildren())
        {
            if (child is FactoryStructure structure)
            {
                var isInteriorStructure = structure.Site is MobileFactorySite;
                structure.SetCombatFocus(
                    isInteriorStructure && structure == _hoveredInteriorStructure,
                    isInteriorStructure && structure == _selectedInteriorStructure);
                structure.UpdateVisuals(alpha);
                structure.SyncCombatVisuals(alpha);
            }
        }
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

        _hud.SetControlMode(_controlMode, _mobileFactory.State, _mobileFactory.TransitFacing, _selectedDeployFacing);
        _hud.SetState(_mobileFactory.State, _mobileFactory.AnchorCell);
        _hud.SetHoverAnchor(_hoveredAnchor, _controlMode == MobileFactoryControlMode.DeployPreview && _hasHoveredAnchor);
        _hud.SetPreviewStatus(_worldStatusPositive, _worldPreviewMessage);
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
        if (_selectedInteriorStructure is not null && GodotObject.IsInstanceValid(_selectedInteriorStructure) && _selectedInteriorStructure.IsInsideTree() && _selectedInteriorStructure is IFactoryInspectable inspectable)
        {
            _hud.SetEditorInspection(inspectable.InspectionTitle, string.Join("\n", inspectable.GetInspectionLines()));
        }
        else
        {
            _hud.SetEditorInspection(null, null);
        }

        if (_selectedInteriorStructure is not null && GodotObject.IsInstanceValid(_selectedInteriorStructure) && _selectedInteriorStructure.IsInsideTree() && _selectedInteriorStructure is IFactoryStructureDetailProvider detailProvider)
        {
            _hud.SetEditorStructureDetails(detailProvider.GetDetailModel());
        }
        else
        {
            _hud.SetEditorStructureDetails(null);
        }

        _hud.SetEditorState(_editorOpen, _mobileFactory.State, CountEditableInteriorStructures(), _interiorInteractionMode);
        _hud.SetHintText(GetHintText());
        _hud.SetBlueprintState(BuildInteriorBlueprintPanelState());
    }

    private FactoryBlueprintPanelState BuildInteriorBlueprintPanelState()
    {
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        var modeText = _blueprintMode switch
        {
            FactoryBlueprintWorkflowMode.CaptureSelection => "蓝图模式：内部框选保存",
            FactoryBlueprintWorkflowMode.ApplyPreview => $"蓝图模式：内部应用预览（旋转 {FactoryDirection.ToLabel(_interiorBlueprintRotation)}）",
            _ => "蓝图模式：待命"
        };
        var activeText = activeBlueprint is null
            ? "当前蓝图：未选择"
            : $"当前蓝图：{activeBlueprint.DisplayName} ({activeBlueprint.GetSummaryText()})";
        var captureSummary = _pendingBlueprintCapture is null
            ? "可框选当前内部布局保存为蓝图，也可一键保存整个内部布局。"
            : $"待保存：{_pendingBlueprintCapture.DisplayName} | {_pendingBlueprintCapture.GetSummaryText()}";
        var issueText = _blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview && _interiorBlueprintPlan is not null
            ? $"当前旋转：{FactoryDirection.ToLabel(_interiorBlueprintRotation)} | 占地 {_interiorBlueprintPlan.FootprintSize.X}x{_interiorBlueprintPlan.FootprintSize.Y}\n{_interiorBlueprintPlan.GetIssueSummary()}"
            : _blueprintMode == FactoryBlueprintWorkflowMode.CaptureSelection
                ? "框选完成后在这里输入名称并保存。"
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
            AllowSelectionCapture = true,
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
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
            ShowWorldEvent("已返回工厂控制模式。", true);
            return;
        }

        SetControlMode(MobileFactoryControlMode.Observer);
        ShowWorldEvent("已进入观察模式，现在 WASD 控制相机。", true);
    }

    private void ToggleDeployPreview()
    {
        if (_mobileFactory is null)
        {
            return;
        }

        if (_controlMode == MobileFactoryControlMode.DeployPreview)
        {
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
            ShowWorldEvent("已取消部署预览。", true);
            return;
        }

        if (_mobileFactory.State != MobileFactoryLifecycleState.InTransit)
        {
            ShowWorldEvent("只有在运输中才能进入部署预览；已部署时请先回收。", false);
            return;
        }

        _selectedDeployFacing = _mobileFactory.TransitFacing;
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
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
            ShowWorldEvent("已取消部署预览。", true);
            return;
        }

        if (_mobileFactory.CancelAutoDeploy())
        {
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
            return;
        }

        if (_controlMode == MobileFactoryControlMode.Observer)
        {
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
            ShowWorldEvent("已退出观察模式。", true);
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
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
        }
    }

    private void ReturnFactoryToTransitMode()
    {
        if (_mobileFactory?.ReturnToTransitMode() == true)
        {
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
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

        if (_mobileFactory.PlaceInteriorStructure(_selectedInteriorKind, _hoveredInteriorCell, _selectedInteriorFacing))
        {
            _selectedInteriorStructure = null;
            _interiorPreviewMessage = $"已在内部格 ({_hoveredInteriorCell.X}, {_hoveredInteriorCell.Y}) 放置{_definitions[_selectedInteriorKind].DisplayName}。";
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
            if (_selectedInteriorStructure is not null && _selectedInteriorStructure.Cell == _hoveredInteriorCell)
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

    private bool IsPointerOverUi()
    {
        return _hud?.BlocksInput(GetViewport().GuiGetHoveredControl()) ?? false;
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
        if (!kind.HasValue)
        {
            EnterInteriorInteractionMode();
            return;
        }

        _selectedInteriorKind = kind.Value;
        _interiorInteractionMode = FactoryInteractionMode.Build;
        _selectedInteriorStructure = null;
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
            EnterInteriorInteractionMode();
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
        var minX = Mathf.Min(start.X, end.X);
        var minY = Mathf.Min(start.Y, end.Y);
        var maxX = Mathf.Max(start.X, end.X);
        var maxY = Mathf.Max(start.Y, end.Y);
        return new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private int CountInteriorStructuresInDeleteRect(Vector2I start, Vector2I end)
    {
        if (_mobileFactory is null)
        {
            return 0;
        }

        var rect = GetDeleteRect(start, end);
        var count = 0;
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                if (_mobileFactory.TryGetInteriorStructure(new Vector2I(x, y), out var structure) && structure is not null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void DeleteInteriorStructuresInRect(Vector2I start, Vector2I end)
    {
        if (_mobileFactory is null)
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
                if (_mobileFactory.TryGetInteriorStructure(cell, out var structure) && structure is not null)
                {
                    cellsToDelete.Add(cell);
                }
            }
        }

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
        if (_interiorBlueprintPreviewRoot is null)
        {
            return;
        }

        while (_interiorBlueprintPreviewMeshes.Count < count)
        {
            var mesh = new MeshInstance3D
            {
                Name = $"InteriorBlueprintPreview_{_interiorBlueprintPreviewMeshes.Count}",
                Visible = false,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(_mobileFactory?.InteriorSite.CellSize * 0.82f ?? 0.82f, 0.08f, _mobileFactory?.InteriorSite.CellSize * 0.82f ?? 0.82f)
                }
            };
            _interiorBlueprintPreviewRoot.AddChild(mesh);
            _interiorBlueprintPreviewMeshes.Add(mesh);
        }
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
        if (!definition.AllowMobileInterior)
        {
            return $"{FactoryPresentation.GetKindLabel(entry.Kind)} 不能放在移动工厂内部。";
        }

        if (!_mobileFactory.InteriorSite.IsInBounds(targetCell))
        {
            return "目标格超出内部编辑范围。";
        }

        if (!_mobileFactory.CanPlaceInterior(entry.Kind, targetCell, targetFacing))
        {
            return MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(entry.Kind)
                ? "该蓝图需要的边界 attachment 挂点在当前内部不可用。"
                : "目标格已被占用。";
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

    private void HandleInteriorBlueprintSaveRequested(string name)
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
        _hasInteriorBlueprintSelectionRect = false;
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
        _interiorPreviewMessage = $"已保存蓝图：{savedRecord.DisplayName}";
    }

    private void HandleInteriorBlueprintSelected(string blueprintId)
    {
        FactoryBlueprintLibrary.SelectActive(blueprintId);
        if (_blueprintMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            UpdateInteriorBlueprintPlan();
        }
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
        FactoryBlueprintLibrary.Remove(blueprintId);
        if (FactoryBlueprintLibrary.GetActive() is null)
        {
            _interiorBlueprintPlan = null;
        }
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

    private void HandleEditorDetailInventoryMoveRequested(string inventoryId, Vector2I fromSlot, Vector2I toSlot)
    {
        if (_selectedInteriorStructure is IFactoryStructureDetailProvider detailProvider && detailProvider.TryMoveDetailInventoryItem(inventoryId, fromSlot, toSlot))
        {
            UpdateHud();
        }
    }

    private void HandleEditorDetailRecipeSelected(string recipeId)
    {
        if (_selectedInteriorStructure is IFactoryStructureDetailProvider detailProvider && detailProvider.TrySetDetailRecipe(recipeId))
        {
            UpdateHud();
        }
    }

    private void HandleEditorDetailClosed()
    {
        _selectedInteriorStructure = null;
        UpdateHud();
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

    private void CreateWorldPreviewVisuals(int footprintCount, int portCount)
    {
        if (_worldPreviewRoot is null)
        {
            return;
        }

        foreach (var child in _worldPreviewRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        _worldPreviewFootprintMeshes.Clear();
        _worldPreviewPortMeshes.Clear();
        _worldPreviewFacingArrow = null;

        for (var i = 0; i < footprintCount; i++)
        {
            var footprint = new MeshInstance3D { Name = $"PreviewFootprint_{i}", Visible = false };
            footprint.Mesh = new BoxMesh
            {
                Size = new Vector3(FactoryConstants.CellSize * 0.92f, 0.08f, FactoryConstants.CellSize * 0.92f)
            };
            _worldPreviewRoot.AddChild(footprint);
            _worldPreviewFootprintMeshes.Add(footprint);
        }

        for (var i = 0; i < portCount; i++)
        {
            var port = new MeshInstance3D { Name = $"PreviewPort_{i}", Visible = false };
            port.Mesh = new BoxMesh
            {
                Size = new Vector3(FactoryConstants.CellSize * 0.55f, 0.12f, FactoryConstants.CellSize * 0.55f)
            };
            _worldPreviewRoot.AddChild(port);
            _worldPreviewPortMeshes.Add(port);
        }

        _worldPreviewFacingArrow = new MeshInstance3D { Name = "PreviewFacingArrow", Visible = false };
        _worldPreviewFacingArrow.Mesh = new BoxMesh
        {
            Size = new Vector3(FactoryConstants.CellSize * 0.42f, 0.16f, FactoryConstants.CellSize * 0.22f)
        };
        _worldPreviewRoot.AddChild(_worldPreviewFacingArrow);
    }

    private void EnsureWorldPreviewVisualCapacity(int footprintCount, int portCount)
    {
        if (_worldPreviewRoot is null)
        {
            return;
        }

        while (_worldPreviewFootprintMeshes.Count < footprintCount)
        {
            var footprint = new MeshInstance3D { Name = $"PreviewFootprint_{_worldPreviewFootprintMeshes.Count}", Visible = false };
            footprint.Mesh = new BoxMesh
            {
                Size = new Vector3(FactoryConstants.CellSize * 0.92f, 0.08f, FactoryConstants.CellSize * 0.92f)
            };
            _worldPreviewRoot.AddChild(footprint);
            _worldPreviewFootprintMeshes.Add(footprint);
        }

        while (_worldPreviewPortMeshes.Count < portCount)
        {
            var port = new MeshInstance3D { Name = $"PreviewPort_{_worldPreviewPortMeshes.Count}", Visible = false };
            port.Mesh = new BoxMesh
            {
                Size = new Vector3(FactoryConstants.CellSize * 0.55f, 0.12f, FactoryConstants.CellSize * 0.55f)
            };
            _worldPreviewRoot.AddChild(port);
            _worldPreviewPortMeshes.Add(port);
        }
    }

    private void CreateInteriorPreviewVisuals()
    {
        if (_interiorPreviewRoot is null)
        {
            return;
        }

        _interiorPreviewCell = new MeshInstance3D { Name = "InteriorPreviewCell" };
        _interiorPreviewCell.Mesh = new BoxMesh { Size = new Vector3(0.48f, 0.06f, 0.48f) };
        _interiorPreviewCell.Position = new Vector3(0.0f, 0.04f, 0.0f);
        _interiorPreviewRoot.AddChild(_interiorPreviewCell);

        _interiorPreviewArrow = new MeshInstance3D { Name = "InteriorPreviewArrow" };
        _interiorPreviewArrow.Mesh = new BoxMesh { Size = new Vector3(0.14f, 0.10f, 0.20f) };
        _interiorPreviewArrow.Position = new Vector3(0.18f, 0.12f, 0.0f);
        _interiorPreviewRoot.AddChild(_interiorPreviewArrow);
    }

    private void UpdateInteriorPreviewSizing()
    {
        if (_mobileFactory is null || _interiorPreviewCell is null || _interiorPreviewArrow is null)
        {
            return;
        }

        var cellSize = _mobileFactory.InteriorSite.CellSize;
        _interiorPreviewCell.Mesh = new BoxMesh { Size = new Vector3(cellSize * 0.78f, 0.06f, cellSize * 0.78f) };
        _interiorPreviewCell.Position = new Vector3(0.0f, 0.04f, 0.0f);

        _interiorPreviewArrow.Mesh = new BoxMesh { Size = new Vector3(cellSize * 0.24f, 0.10f, cellSize * 0.30f) };
        _interiorPreviewArrow.Position = new Vector3(cellSize * 0.28f, 0.12f, 0.0f);
    }

    private void EnsureInteriorAttachmentPreviewMeshCount(List<MeshInstance3D> meshes, int count, Vector3 size)
    {
        if (_interiorPreviewRoot is null)
        {
            return;
        }

        while (meshes.Count < count)
        {
            var mesh = new MeshInstance3D();
            mesh.Mesh = new BoxMesh { Size = size };
            mesh.Visible = false;
            _interiorPreviewRoot.AddChild(mesh);
            meshes.Add(mesh);
        }

        for (var i = 0; i < meshes.Count; i++)
        {
            meshes[i].Mesh = new BoxMesh { Size = size };
        }
    }

    private void EnsureInputActions()
    {
        EnsureAction("camera_pan_left", new InputEventKey { PhysicalKeycode = Key.A }, new InputEventKey { PhysicalKeycode = Key.Left });
        EnsureAction("camera_pan_right", new InputEventKey { PhysicalKeycode = Key.D }, new InputEventKey { PhysicalKeycode = Key.Right });
        EnsureAction("camera_pan_up", new InputEventKey { PhysicalKeycode = Key.W }, new InputEventKey { PhysicalKeycode = Key.Up });
        EnsureAction("camera_pan_down", new InputEventKey { PhysicalKeycode = Key.S }, new InputEventKey { PhysicalKeycode = Key.Down });
        EnsureAction("camera_zoom_in", new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true });
        EnsureAction("camera_zoom_out", new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true });
        EnsureAction("factory_move_forward", new InputEventKey { PhysicalKeycode = Key.W });
        EnsureAction("factory_move_backward", new InputEventKey { PhysicalKeycode = Key.S });
        EnsureAction("factory_turn_left", new InputEventKey { PhysicalKeycode = Key.A });
        EnsureAction("factory_turn_right", new InputEventKey { PhysicalKeycode = Key.D });
        EnsureAction("deploy_rotate_left", new InputEventKey { PhysicalKeycode = Key.Q });
        EnsureAction("deploy_rotate_right", new InputEventKey { PhysicalKeycode = Key.E });
        EnsureAction("toggle_observer_mode", new InputEventKey { PhysicalKeycode = Key.Tab });
        EnsureAction("toggle_deploy_preview", new InputEventKey { PhysicalKeycode = Key.G });
        EnsureAction("cancel_mobile_command", new InputEventKey { PhysicalKeycode = Key.Escape });
        EnsureAction("mobile_factory_auxiliary_command", new InputEventKey { PhysicalKeycode = Key.R });
        EnsureAction("toggle_mobile_editor", new InputEventKey { PhysicalKeycode = Key.F });
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

    private static bool HasFocusedSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--mobile-factory-smoke-test", global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasLargeScenarioSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--mobile-factory-large-smoke-test", global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async void RunSmokeChecks()
    {
        if (_grid is null || _mobileFactory is null || _sinkA is null || _sinkB is null || _hud is null || _cameraRig is null || _simulation is null)
        {
            GD.PushError("MOBILE_FACTORY_SMOKE_FAILED missing grid, factory, hud, camera, or sinks.");
            GetTree().Quit(1);
            return;
        }

        await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
        var startsInCommandMode = _controlMode == MobileFactoryControlMode.FactoryCommand;
        var cameraLockedInCommand = !_cameraRig.AllowPanInput;

        ToggleObserverMode();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        var observerActive = _controlMode == MobileFactoryControlMode.Observer;
        var observerCameraActive = _cameraRig.AllowPanInput;
        ToggleObserverMode();
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
        var interiorRunsInTransit = _mobileFactory.InteriorSite.IsSimulationActive;
        _mobileFactory.TryGetInteriorStructure(new Vector2I(1, 3), out var presetInputSinkStructure);
        var inputSinkInTransit = presetInputSinkStructure as SinkStructure;
        var inputSinkTransitBaseline = inputSinkInTransit?.DeliveredTotal ?? 0;
        _mobileFactory.TryGetInteriorStructure(new Vector2I(4, 0), out var escortTurretStructure);
        var escortTurret = escortTurretStructure as GunTurretStructure;
        var turretShotsBeforeDeploy = escortTurret?.ShotsFired ?? 0;

        var initialPosition = _mobileFactory.WorldFocusPoint;
        _mobileFactory.ApplyTransitInput(_grid, 1.0f, -1.0f, 0.5);
        var movedInTransit = _mobileFactory.WorldFocusPoint.DistanceTo(initialPosition) > 0.05f;

        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);

        var openedInTransit = _hud.IsEditorVisible;
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var rightPaneHover = _hud.IsPointerOverEditor(new Vector2(viewportSize.X - 80.0f, viewportSize.Y * 0.5f));
        var leftPaneHover = !_hud.IsPointerOverEditor(new Vector2(10.0f, 40.0f));
        var detailWindowInTransit = await RunEditorDetailSmoke();
        var blueprintWorkflowInTransit = await RunInteriorBlueprintSmoke();
        _mobileFactory.TryGetInteriorStructure(new Vector2I(1, 3), out presetInputSinkStructure);
        inputSinkInTransit = presetInputSinkStructure as SinkStructure;
        inputSinkTransitBaseline = inputSinkInTransit?.DeliveredTotal ?? 0;
        _mobileFactory.TryGetInteriorStructure(new Vector2I(4, 0), out escortTurretStructure);
        escortTurret = escortTurretStructure as GunTurretStructure;
        turretShotsBeforeDeploy = escortTurret?.ShotsFired ?? 0;

        var placedInterior = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.Splitter, new Vector2I(2, 0), FacingDirection.East);
        var interiorPlacedExists = _mobileFactory.TryGetInteriorStructure(new Vector2I(2, 0), out var placedStructure) && placedStructure is SplitterStructure;
        var placedInteriorSink = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.Sink, new Vector2I(1, 0), FacingDirection.East);
        var interiorSinkExists = _mobileFactory.TryGetInteriorStructure(new Vector2I(1, 0), out var sinkStructure) && sinkStructure is SinkStructure;
        var miniatureSyncedInTransit = placedStructure is not null
            && placedStructure.GlobalPosition.DistanceTo(_mobileFactory.InteriorSite.CellToWorld(new Vector2I(2, 0))) < 0.05f;
        var inputBlockedInTransit = (inputSinkInTransit?.DeliveredTotal ?? 0) == inputSinkTransitBaseline;
        var projectionConflictAnchor = new Vector2I(-10, -8);
        var placedConflictBlocker = false;
        foreach (var projectionConflictCell in _mobileFactory.GetPortCells(projectionConflictAnchor, FacingDirection.East))
        {
            if (PlaceWorldStructure(BuildPrototypeKind.Sink, projectionConflictCell, FacingDirection.East) is not null)
            {
                placedConflictBlocker = true;
                break;
            }
        }
        var blockedDeploy = placedConflictBlocker && !_mobileFactory.CanDeployAt(_grid, projectionConflictAnchor, FacingDirection.East);
        var edgeBlockedDeploy = !_mobileFactory.CanDeployAt(_grid, new Vector2I(GetWorldMaxCell(), GetWorldMaxCell()), FacingDirection.East);
        var southPortCells = new HashSet<Vector2I>(_mobileFactory.GetPortCells(AnchorA, FacingDirection.South));
        var westFootprintCells = new HashSet<Vector2I>(_mobileFactory.GetFootprintCells(AnchorA, FacingDirection.West));
        var facingAwareCells = southPortCells.Contains(new Vector2I(-6, -1)) && westFootprintCells.Contains(new Vector2I(-6, -3));
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        HandleCommandSlot(MobileFactoryCommandSlot.Auxiliary);
        var contextualRotateWorks = _selectedDeployFacing == FacingDirection.South;
        _selectedDeployFacing = FacingDirection.East;

        SetEditorOpenState(false);
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = AnchorA;
        _hasHoveredAnchor = true;
        _canDeployCurrentAnchor = _mobileFactory.CanDeployAt(_grid, AnchorA, FacingDirection.East);
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.Deployed, 4.5f);
        var firstDeploy = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        var moveWhileDeployedStart = _mobileFactory.WorldFocusPoint;
        _mobileFactory.ApplyTransitInput(_grid, 1.0f, 1.0f, 0.6);
        var moveRejectedWhileDeployed = _mobileFactory.WorldFocusPoint.DistanceTo(moveWhileDeployedStart) < 0.01f;
        await ToSignal(GetTree().CreateTimer(4.5f), SceneTreeTimer.SignalName.Timeout);
        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        var openedWhileDeployed = _hud.IsEditorVisible;
        var portConnected = _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.OutputPort);
        var portOverlayConnected = _hud.PortStatusText.Contains("已连接");
        var miniatureSyncedDeployed = placedStructure is not null
            && placedStructure.GlobalPosition.DistanceTo(_mobileFactory.InteriorSite.CellToWorld(new Vector2I(2, 0))) < 0.05f;
        var firstDelivered = _sinkA.DeliveredTotal;
        await ToSignal(GetTree().CreateTimer(4.0f), SceneTreeTimer.SignalName.Timeout);
        var inputAttachmentTransit = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.InputPort);
        var inputDeliveredWhileDeployed = (inputSinkInTransit?.DeliveredTotal ?? 0) > inputSinkTransitBaseline;
        var turretTrackedThreats = (escortTurret?.ShotsFired ?? 0) > turretShotsBeforeDeploy;
        var mobileCombatActive = _simulation.ActiveEnemyCount > 0 || _simulation.DefeatedEnemyCount > 0;

        SetEditorOpenState(false);
        var blockedOutputBeforeRecall = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.OutputPort, onlyDisconnected: true);
        var deployedPositionBeforeRecall = _mobileFactory.WorldFocusPoint;
        var recalled = _mobileFactory.ReturnToTransitMode();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 1.2f);
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        var blockedOutputAfterRecall = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.OutputPort, onlyDisconnected: true);
        var blockedOutputActive = blockedOutputAfterRecall > blockedOutputBeforeRecall;
        var stayedInPlaceAfterReturn = _mobileFactory.WorldFocusPoint.DistanceTo(deployedPositionBeforeRecall) < 0.05f;
        var reservationsReleased =
            _grid.CanReserveAll(_mobileFactory.GetFootprintCells(AnchorA, FacingDirection.East), _mobileFactory.ReservationOwnerId)
            && _grid.CanReserveAll(_mobileFactory.GetPortCells(AnchorA, FacingDirection.East), _mobileFactory.ReservationOwnerId);

        SetControlMode(MobileFactoryControlMode.DeployPreview);
        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = AnchorB;
        _hasHoveredAnchor = true;
        _canDeployCurrentAnchor = _mobileFactory.CanDeployAt(_grid, AnchorB, FacingDirection.East);
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.Deployed, 4.5f);
        var secondDeploy = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;
        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        var secondDelivered = _sinkB.DeliveredTotal;

        if (!startsInCommandMode || !cameraLockedInCommand || !observerActive || !observerCameraActive || !interiorRunsInTransit || !movedInTransit || !openedInTransit || !rightPaneHover || !leftPaneHover || !detailWindowInTransit || !blueprintWorkflowInTransit || !placedInterior || !interiorPlacedExists || !placedInteriorSink || !interiorSinkExists || !miniatureSyncedInTransit || !inputBlockedInTransit || !blockedDeploy || !edgeBlockedDeploy || !facingAwareCells || !contextualRotateWorks || !firstDeploy || !moveRejectedWhileDeployed || !openedWhileDeployed || !portConnected || !portOverlayConnected || !miniatureSyncedDeployed || firstDelivered <= 0 || !inputDeliveredWhileDeployed || !turretTrackedThreats || !mobileCombatActive || !recalled || !blockedOutputActive || !stayedInPlaceAfterReturn || !reservationsReleased || !secondDeploy || secondDelivered <= 0)
        {
            GD.PushError($"MOBILE_FACTORY_SMOKE_FAILED startsCommand={startsInCommandMode} cameraLocked={cameraLockedInCommand} observerActive={observerActive} observerCamera={observerCameraActive} interiorTransit={interiorRunsInTransit} movedInTransit={movedInTransit} openedTransit={openedInTransit} rightHover={rightPaneHover} leftHover={leftPaneHover} detailWindow={detailWindowInTransit} blueprintWorkflow={blueprintWorkflowInTransit} placedInterior={placedInterior} interiorPlacedExists={interiorPlacedExists} placedSink={placedInteriorSink} sinkExists={interiorSinkExists} miniatureTransit={miniatureSyncedInTransit} inputBlockedInTransit={inputBlockedInTransit} blocked={blockedDeploy} edgeBlocked={edgeBlockedDeploy} facingAware={facingAwareCells} contextualRotateWorks={contextualRotateWorks} firstDeploy={firstDeploy} moveRejected={moveRejectedWhileDeployed} openedDeployed={openedWhileDeployed} portConnected={portConnected} portOverlay={portOverlayConnected} miniatureDeployed={miniatureSyncedDeployed} firstDelivered={firstDelivered} inputAttachmentTransit={inputAttachmentTransit} inputDeliveredWhileDeployed={inputDeliveredWhileDeployed} turretShots={(escortTurret?.ShotsFired ?? -1)} mobileCombatActive={mobileCombatActive} recalled={recalled} blockedOutputActive={blockedOutputActive} stayedInPlaceAfterReturn={stayedInPlaceAfterReturn} released={reservationsReleased} secondDeploy={secondDeploy} secondDelivered={secondDelivered}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_SMOKE_OK firstDelivered={firstDelivered} secondDelivered={secondDelivered} detailWindow={detailWindowInTransit} blueprintWorkflow={blueprintWorkflowInTransit} turretShots={(escortTurret?.ShotsFired ?? -1)} combatKills={_simulation.DefeatedEnemyCount}");
        GetTree().Quit();
    }

    private async Task<bool> RunEditorDetailSmoke()
    {
        if (_mobileFactory is null || _hud is null || _simulation is null)
        {
            return false;
        }

        var placedRecipeProducer = _mobileFactory.PlaceInteriorStructure(BuildPrototypeKind.Producer, new Vector2I(2, 2), FacingDirection.East);
        if (!placedRecipeProducer
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(2, 2), out var recipeProducerStructure)
            || recipeProducerStructure is not ProducerStructure recipeProducer
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(1, 1), out var storageStructure)
            || storageStructure is not StorageStructure storage
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(3, 0), out var ammoAssemblerStructure)
            || ammoAssemblerStructure is not AmmoAssemblerStructure ammoAssembler
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(4, 0), out var turretStructure)
            || turretStructure is not GunTurretStructure turret)
        {
            return false;
        }

        var producerRecipeChanged = recipeProducer.TrySetDetailRecipe("machine-parts");
        var ammoRecipeChanged = ammoAssembler.TrySetDetailRecipe("high-velocity-ammo");

        await ToSignal(GetTree().CreateTimer(2.4f), SceneTreeTimer.SignalName.Timeout);

        _selectedInteriorStructure = storage;
        UpdateHud();
        var storageDetailVisible = _hud.IsDetailVisible && _hud.DetailTitleText.Contains("仓储", global::System.StringComparison.Ordinal);

        var storageDetail = storage.GetDetailModel();
        if (storageDetail.InventorySections.Count == 0)
        {
            return false;
        }

        var occupiedSlot = new Vector2I(-1, -1);
        var emptySlot = new Vector2I(-1, -1);
        for (var index = 0; index < storageDetail.InventorySections[0].Slots.Count; index++)
        {
            var slot = storageDetail.InventorySections[0].Slots[index];
            if (slot.HasItem && occupiedSlot.X < 0)
            {
                occupiedSlot = slot.Position;
            }
            else if (!slot.HasItem && emptySlot.X < 0)
            {
                emptySlot = slot.Position;
            }
        }

        var inventoryMoveWorked = occupiedSlot.X >= 0
            && emptySlot.X >= 0
            && storage.TryMoveDetailInventoryItem("storage-buffer", occupiedSlot, emptySlot);

        var movedDetail = storage.GetDetailModel();
        var targetNowOccupied = false;
        for (var index = 0; index < movedDetail.InventorySections[0].Slots.Count; index++)
        {
            var slot = movedDetail.InventorySections[0].Slots[index];
            if (slot.Position == emptySlot && slot.HasItem)
            {
                targetNowOccupied = true;
                break;
            }
        }

        _selectedInteriorStructure = recipeProducer;
        UpdateHud();
        recipeProducer.TryPeekProvidedItem(new Vector2I(3, 2), _simulation, out var producedItem);
        var producerRecipeVerified = producerRecipeChanged
            && _hud.IsDetailVisible
            && _hud.DetailTitleText.Contains("生产器", global::System.StringComparison.Ordinal)
            && producedItem?.ItemKind == FactoryItemKind.MachinePart;

        _selectedInteriorStructure = turret;
        UpdateHud();
        var turretShowsHighVelocityAmmo = false;
        var turretDetail = turret.GetDetailModel();
        if (turretDetail.InventorySections.Count > 0)
        {
            for (var index = 0; index < turretDetail.InventorySections[0].Slots.Count; index++)
            {
                var slot = turretDetail.InventorySections[0].Slots[index];
                if (slot.ItemLabel?.Contains("高速弹药", global::System.StringComparison.Ordinal) ?? false)
                {
                    turretShowsHighVelocityAmmo = true;
                    break;
                }
            }
        }

        return storageDetailVisible
            && inventoryMoveWorked
            && targetNowOccupied
            && producerRecipeVerified
            && ammoRecipeChanged
            && turret.BufferedAmmo > 0
            && turretShowsHighVelocityAmmo
            && _hud.IsEditorVisible;
    }

    private async Task<bool> RunInteriorBlueprintSmoke()
    {
        if (_mobileFactory is null || _interiorBlueprintSite is null)
        {
            return false;
        }

        if (!_mobileFactory.TryGetInteriorStructure(new Vector2I(2, 2), out var producerStructure)
            || producerStructure is not ProducerStructure producer
            || !_mobileFactory.TryGetInteriorStructure(new Vector2I(3, 0), out var ammoAssemblerStructure)
            || ammoAssemblerStructure is not AmmoAssemblerStructure ammoAssembler)
        {
            return false;
        }

        if (!producer.TrySetDetailRecipe("machine-parts") || !ammoAssembler.TrySetDetailRecipe("high-velocity-ammo"))
        {
            return false;
        }

        var captured = FactoryBlueprintCaptureService.CaptureFullSite(
            _interiorBlueprintSite,
            "Smoke Interior Blueprint");
        if (captured is null || captured.StructureCount == 0 || captured.RequiredAttachments.Count == 0)
        {
            return false;
        }

        var savedRecord = new FactoryBlueprintRecord(
            captured.Id,
            "Smoke Interior Blueprint",
            captured.SourceSiteKind,
            captured.SuggestedAnchorCell,
            captured.BoundsSize,
            captured.Entries,
            captured.RequiredAttachments);
        FactoryBlueprintLibrary.AddOrUpdate(savedRecord);
        FactoryBlueprintLibrary.SelectActive(savedRecord.Id);

        var storedBlueprint = FactoryBlueprintLibrary.FindById(savedRecord.Id);
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        var expectedStructureCount = CountEditableInteriorStructures();
        if (storedBlueprint is null || activeBlueprint?.Id != savedRecord.Id || expectedStructureCount != savedRecord.StructureCount)
        {
            return false;
        }

        var oversizeRecord = new FactoryBlueprintRecord(
            $"{savedRecord.Id}-oversize",
            "Oversize Smoke Interior Blueprint",
            savedRecord.SourceSiteKind,
            savedRecord.SuggestedAnchorCell,
            new Vector2I(_mobileFactory.Profile.InteriorWidth + 1, savedRecord.BoundsSize.Y),
            savedRecord.Entries,
            savedRecord.RequiredAttachments);
        var defaultAnchor = _interiorBlueprintSite.GetDefaultApplyAnchor(savedRecord);
        var invalidBoundsPlan = FactoryBlueprintPlanner.CreatePlan(oversizeRecord, _interiorBlueprintSite, defaultAnchor);
        var boundsRejected = !invalidBoundsPlan.IsValid
            && invalidBoundsPlan.GetIssueSummary().Contains("尺寸", global::System.StringComparison.Ordinal);

        if (!ClearInteriorStructuresForBlueprintSmoke())
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

        var invalidAttachmentPlan = FactoryBlueprintPlanner.CreatePlan(
            savedRecord,
            _interiorBlueprintSite,
            defaultAnchor + Vector2I.Right);
        var attachmentRejected = !invalidAttachmentPlan.IsValid
            && invalidAttachmentPlan.GetIssueSummary().Contains("挂点", global::System.StringComparison.Ordinal);

        var validPlan = FactoryBlueprintPlanner.CreatePlan(savedRecord, _interiorBlueprintSite, defaultAnchor);
        if (!validPlan.IsValid || !FactoryBlueprintPlanner.CommitPlan(validPlan, _interiorBlueprintSite))
        {
            return false;
        }

        await ToSignal(GetTree().CreateTimer(2.5f), SceneTreeTimer.SignalName.Timeout);

        var structureCountRestored = CountEditableInteriorStructures() == savedRecord.StructureCount;
        var attachmentsRestored = true;
        for (var index = 0; index < savedRecord.RequiredAttachments.Count; index++)
        {
            var attachment = savedRecord.RequiredAttachments[index];
            var targetCell = defaultAnchor + attachment.LocalCell;
            if (!_mobileFactory.TryGetInteriorStructure(targetCell, out var restoredAttachment)
                || restoredAttachment is null
                || restoredAttachment.Kind != attachment.Kind
                || restoredAttachment.Facing != attachment.Facing)
            {
                attachmentsRestored = false;
                break;
            }
        }

        var producerRecipeRestored =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(2, 2), out var restoredProducerStructure)
            && restoredProducerStructure is ProducerStructure restoredProducer
            && restoredProducer.CaptureBlueprintConfiguration().TryGetValue("recipe_id", out var restoredProducerRecipe)
            && restoredProducerRecipe == "machine-parts";
        var ammoRecipeRestored =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(3, 0), out var restoredAmmoAssemblerStructure)
            && restoredAmmoAssemblerStructure is AmmoAssemblerStructure restoredAmmoAssembler
            && restoredAmmoAssembler.CaptureBlueprintConfiguration().TryGetValue("recipe_id", out var restoredAmmoRecipe)
            && restoredAmmoRecipe == "high-velocity-ammo";
        var turretPrimed =
            _mobileFactory.TryGetInteriorStructure(new Vector2I(4, 0), out var restoredTurretStructure)
            && restoredTurretStructure is GunTurretStructure restoredTurret
            && restoredTurret.BufferedAmmo > 0;

        return boundsRejected
            && attachmentRejected
            && structureCountRestored
            && attachmentsRestored
            && producerRecipeRestored
            && ammoRecipeRestored
            && turretPrimed;
    }

    private bool ClearInteriorStructuresForBlueprintSmoke()
    {
        if (_mobileFactory is null)
        {
            return false;
        }

        var cells = new List<Vector2I>();
        foreach (var structure in _mobileFactory.InteriorSite.GetStructures())
        {
            cells.Add(structure.Cell);
        }

        for (var index = 0; index < cells.Count; index++)
        {
            if (!_mobileFactory.RemoveInteriorStructure(cells[index]))
            {
                return false;
            }
        }

        return CountEditableInteriorStructures() == 0;
    }

    private async void RunLargeScenarioSmokeChecks()
    {
        if (_grid is null || _mobileFactory is null || _hud is null || _cameraRig is null || _simulation is null || _backgroundFactories.Count < 3 || _scenarioSinks.Count < 4)
        {
            GD.PushError("MOBILE_FACTORY_LARGE_SMOKE_FAILED missing grid, player factory, hud, camera, background actors, or scenario sinks.");
            GetTree().Quit(1);
            return;
        }

        await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);

        var allFactories = new List<MobileFactoryInstance> { _mobileFactory };
        allFactories.AddRange(_backgroundFactories);
        var deployedCount = 0;
        var inTransitCount = 0;
        var profileIds = new HashSet<string>();
        var presetIds = new HashSet<string>();
        foreach (var factory in allFactories)
        {
            profileIds.Add(factory.Profile.Id);
            presetIds.Add(factory.InteriorPreset.Id);
            if (factory.State == MobileFactoryLifecycleState.Deployed)
            {
                deployedCount++;
            }
            else if (factory.State == MobileFactoryLifecycleState.InTransit)
            {
                inTransitCount++;
            }
        }

        var mixedStates = deployedCount >= 2 && inTransitCount >= 1;
        var variedProfiles = profileIds.Count >= 3;
        var variedPresets = presetIds.Count >= 3;

        var backgroundStartPositions = new List<Vector3>();
        foreach (var factory in _backgroundFactories)
        {
            backgroundStartPositions.Add(factory.WorldFocusPoint);
        }
        var initialDelivered = GetPrimaryDeliveryTotal() + GetSecondaryDeliveryTotal();

        var playerStartPosition = _mobileFactory.WorldFocusPoint;
        _mobileFactory.ApplyTransitInput(_grid, 1.0f, -1.0f, 0.5);
        var playerMoved = _mobileFactory.WorldFocusPoint.DistanceTo(playerStartPosition) > 0.05f;

        SetEditorOpenState(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        var editorVisible = _hud.IsEditorVisible;
        SetEditorOpenState(false);
        _mobileFactory.TryGetInteriorStructure(new Vector2I(1, 3), out var playerInputSinkStructure);
        var playerInputSink = playerInputSinkStructure as SinkStructure;
        var playerInputDeliveredBaseline = playerInputSink?.DeliveredTotal ?? 0;
        var turretShotsBaseline = CountMobileTurretShots();

        _selectedDeployFacing = FacingDirection.East;
        _hoveredAnchor = new Vector2I(-12, 3);
        _hasHoveredAnchor = true;
        _canDeployCurrentAnchor = _mobileFactory.CanDeployAt(_grid, _hoveredAnchor, FacingDirection.East);
        SetControlMode(MobileFactoryControlMode.DeployPreview);
        ConfirmDeployPreview();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.Deployed, 5.0f);
        var playerDeployed = _mobileFactory.State == MobileFactoryLifecycleState.Deployed;

        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
        var backgroundMoved = false;
        for (var i = 0; i < _backgroundFactories.Count; i++)
        {
            if (_backgroundFactories[i].WorldFocusPoint.DistanceTo(backgroundStartPositions[i]) > 0.10f)
            {
                backgroundMoved = true;
                break;
            }
        }
        var deliveredDuringRun = GetPrimaryDeliveryTotal() + GetSecondaryDeliveryTotal() > initialDelivered;
        var inputDeliveredDuringRun = (playerInputSink?.DeliveredTotal ?? 0) > playerInputDeliveredBaseline;
        var playerTurretTrackedThreats = CountMobileTurretShots() > turretShotsBaseline;
        var heavyEnemyCount = CountActiveHeavyWorldEnemies();
        var worldCombatActive = (_simulation.ActiveEnemyCount > 0 || _simulation.DefeatedEnemyCount > 0) && heavyEnemyCount > 0;

        var anyConnectedBridge = false;
        foreach (var factory in allFactories)
        {
            if (factory.HasConnectedAttachment(BuildPrototypeKind.OutputPort))
            {
                anyConnectedBridge = true;
                break;
            }
        }

        if (!mixedStates || !variedProfiles || !variedPresets || !playerMoved || !editorVisible || !playerDeployed || !backgroundMoved || !deliveredDuringRun || !inputDeliveredDuringRun || !anyConnectedBridge || !playerTurretTrackedThreats || !worldCombatActive)
        {
            GD.PushError($"MOBILE_FACTORY_LARGE_SMOKE_FAILED mixedStates={mixedStates} variedProfiles={variedProfiles} variedPresets={variedPresets} playerMoved={playerMoved} editorVisible={editorVisible} playerDeployed={playerDeployed} backgroundMoved={backgroundMoved} deliveredDuringRun={deliveredDuringRun} inputDeliveredDuringRun={inputDeliveredDuringRun} anyConnectedBridge={anyConnectedBridge} deployedCount={deployedCount} inTransitCount={inTransitCount} playerInputDelivered={playerInputSink?.DeliveredTotal ?? -1} mobileTurretShots={CountMobileTurretShots()} heavyEnemyCount={heavyEnemyCount} activeEnemies={_simulation.ActiveEnemyCount} defeatedEnemies={_simulation.DefeatedEnemyCount}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_LARGE_SMOKE_OK deployed={deployedCount} inTransit={inTransitCount} delivered={GetPrimaryDeliveryTotal() + GetSecondaryDeliveryTotal()} heavyEnemies={heavyEnemyCount} mobileTurretShots={CountMobileTurretShots()}");
        GetTree().Quit();
    }

    private static Vector2I FirstCell(IEnumerable<Vector2I> cells)
    {
        foreach (var cell in cells)
        {
            return cell;
        }

        return Vector2I.Zero;
    }

    private async System.Threading.Tasks.Task WaitForCondition(global::System.Func<bool> predicate, float timeoutSeconds)
    {
        var remaining = timeoutSeconds;
        while (remaining > 0.0f)
        {
            if (predicate())
            {
                return;
            }

            await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
            remaining -= 0.1f;
        }
    }

    private void SetControlMode(MobileFactoryControlMode controlMode)
    {
        _controlMode = controlMode;
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
            MobileFactoryControlMode.Observer => "观察模式：WASD/方向键移动相机 | 滚轮缩放 | Tab 返回工厂控制 | F 内部编辑",
            MobileFactoryControlMode.DeployPreview => "部署预览：左键确认 | Q/E/R 旋转朝向 | G/Esc 取消 | F 内部编辑",
            _ => "工厂控制：W/S 前进后退 | A/D 转向 | G 部署预览 | Tab 观察模式 | R 切回移动态 | F 内部编辑；编辑器里和 sandbox 一样，X 进删除模式，右键或 Esc 回交互，Delete 拆除悬停建筑"
        };
    }

    private void RegisterScenarioSink(SinkStructure? sink)
    {
        if (sink is not null)
        {
            _scenarioSinks.Add(sink);
        }
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

    private static void ApplyPreviewColor(MeshInstance3D meshInstance, Color color)
    {
        meshInstance.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.4f
        };
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

    private static WorldEnvironment CreateEnvironment()
    {
        var environment = new Environment
        {
            BackgroundMode = Environment.BGMode.Color,
            BackgroundColor = new Color("111827"),
            AmbientLightSource = Environment.AmbientSource.Color,
            AmbientLightColor = new Color("D6E4F0"),
            AmbientLightSkyContribution = 0.0f,
            AmbientLightEnergy = 0.7f
        };

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

    private static Node3D CreateFloor(int minCell, int maxCell)
    {
        var floorRoot = new Node3D { Name = "FloorRoot" };
        var floor = new MeshInstance3D { Name = "FactoryFloor" };
        floor.Mesh = new PlaneMesh
        {
            Size = new Vector2(
                (maxCell - minCell + 1) * FactoryConstants.CellSize,
                (maxCell - minCell + 1) * FactoryConstants.CellSize)
        };

        floor.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color("1F2937"),
            Roughness = 1.0f
        };
        floorRoot.AddChild(floor);
        return floorRoot;
    }

    private static Node3D CreateGridLines(int minCell, int maxCell)
    {
        var gridRoot = new Node3D { Name = "GridLines" };
        var lineMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.35f, 0.43f, 0.53f, 0.65f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 1.0f
        };
        var worldMin = (minCell - 0.5f) * FactoryConstants.CellSize;
        var worldMax = (maxCell + 0.5f) * FactoryConstants.CellSize;
        var lineLength = worldMax - worldMin;

        for (var i = minCell; i <= maxCell + 1; i++)
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
}
