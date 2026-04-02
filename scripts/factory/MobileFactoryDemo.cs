using Godot;
using System.Collections.Generic;

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
        [BuildPrototypeKind.OutputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.OutputPort, "输出端口", new Color("FB923C"), "将移动工厂内部物流送往世界网格。"),
        [BuildPrototypeKind.InputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.InputPort, "输入端口", new Color("60A5FA"), "把世界侧物流导入移动工厂内部。")
    };

    private GridManager? _grid;
    private SimulationController? _simulation;
    private FactoryCameraRig? _cameraRig;
    private MobileFactoryHud? _hud;
    private Node3D? _structureRoot;
    private Node3D? _worldPreviewRoot;
    private Node3D? _interiorPreviewRoot;
    private Camera3D? _editorCamera;
    private readonly List<MeshInstance3D> _worldPreviewFootprintMeshes = new();
    private readonly List<MeshInstance3D> _worldPreviewPortMeshes = new();
    private MeshInstance3D? _worldPreviewFacingArrow;
    private MeshInstance3D? _interiorPreviewCell;
    private MeshInstance3D? _interiorPreviewArrow;
    private readonly List<MeshInstance3D> _interiorPreviewBoundaryMeshes = new();
    private readonly List<MeshInstance3D> _interiorPreviewExteriorMeshes = new();

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
    private Vector2I _hoveredInteriorCell;
    private bool _hasHoveredInteriorCell;
    private bool _canPlaceInteriorCell;
    private string _interiorPreviewMessage = "按 F 展开内部编辑区，然后把鼠标移入右侧区域开始调整移动工厂内部布局。";
    private bool _editorOpen;
    private bool _hoveringEditorPane;
    private bool _hoveringEditorViewport;
    private Vector2 _mousePosition = Vector2.Zero;

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

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        if (CanUseEditorViewportInput())
        {
            if (mouseButton.ButtonIndex == MouseButton.Left && _hasHoveredInteriorCell && _canPlaceInteriorCell)
            {
                PlaceInteriorStructure();
                GetViewport().SetInputAsHandled();
            }

            if (mouseButton.ButtonIndex == MouseButton.Right && _hasHoveredInteriorCell)
            {
                RemoveInteriorStructure();
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

        _worldPreviewRoot = new Node3D { Name = "WorldPreviewRoot" };
        AddChild(_worldPreviewRoot);
        CreateWorldPreviewVisuals(4, 1);

        _interiorPreviewRoot = new Node3D { Name = "InteriorPreviewRoot", Visible = false };
        AddChild(_interiorPreviewRoot);
        CreateInteriorPreviewVisuals();

        _simulation = new SimulationController { Name = "SimulationController" };
        AddChild(_simulation);

        _cameraRig = new FactoryCameraRig();
        AddChild(_cameraRig);

        _hud = new MobileFactoryHud();
        _hud.EditorPaletteSelected += OnEditorPaletteSelected;
        _hud.EditorRotateRequested += OnEditorRotateRequested;
        _hud.ObserverModeToggleRequested += ToggleObserverMode;
        _hud.DeployModeToggleRequested += ToggleDeployPreview;
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
            _editorOpen = !_editorOpen;
            _hud?.SetEditorOpen(_editorOpen);
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

        if (keyEvent.Keycode == Key.Escape)
        {
            _editorOpen = false;
            _hud?.SetEditorOpen(false);
            FocusFactoryForCurrentMode();
            return true;
        }

        if (!CanUseEditorInput())
        {
            return false;
        }

        for (var i = 0; i < InteriorPalette.Length && i < InteriorPaletteKeys.Length; i++)
        {
            if (keyEvent.Keycode != InteriorPaletteKeys[i])
            {
                continue;
            }

            _selectedInteriorKind = InteriorPalette[i];
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

        if (_cameraRig is not null)
        {
            _cameraRig.AllowPanInput = CanUseWorldInput() && _controlMode == MobileFactoryControlMode.Observer;
            _cameraRig.AllowZoomInput = CanUseWorldInput();
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
        _interiorPreviewMessage = "把鼠标移入右侧编辑区，可直接调整移动工厂内部布局。";

        if (!_editorOpen || !_hoveringEditorViewport || _mobileFactory is null || _editorCamera is null)
        {
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

        _interiorPreviewMessage = $"内部格 ({cell.X}, {cell.Y}) 已被占用，右键可拆除已安装的内部结构或边界 attachment。";
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

        _interiorPreviewRoot.Visible = _editorOpen && _hoveringEditorViewport && _hasHoveredInteriorCell;
        if (!_interiorPreviewRoot.Visible)
        {
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
        ApplyPreviewColor(_interiorPreviewCell, tint);
        ApplyPreviewColor(_interiorPreviewArrow, tint.Lightened(0.1f));

        for (var i = 0; i < _interiorPreviewBoundaryMeshes.Count; i++)
        {
            _interiorPreviewBoundaryMeshes[i].Visible = false;
        }

        for (var i = 0; i < _interiorPreviewExteriorMeshes.Count; i++)
        {
            _interiorPreviewExteriorMeshes[i].Visible = false;
        }

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
                structure.UpdateVisuals(alpha);
            }
        }
    }

    private void UpdateEditorCamera()
    {
        if (_editorCamera is null || _mobileFactory is null)
        {
            return;
        }

        var focus = _mobileFactory.GetEditorFocusWorldCenter();
        _editorCamera.Size = _mobileFactory.GetSuggestedEditorCameraSize();
        _editorCamera.Position = focus + new Vector3(0.0f, _editorCamera.Size * 1.8f, 0.0f);
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
        _hud.SetEditorSelection(_selectedInteriorKind, _selectedInteriorFacing);
        _hud.SetEditorPreview(_canPlaceInteriorCell, _interiorPreviewMessage);
        _hud.SetPortStatus(_mobileFactory.GetPortStatusLabel());
        _hud.SetEditorState(_editorOpen, _mobileFactory.State, CountEditableInteriorStructures());
        _hud.SetHintText(GetHintText());
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
            _interiorPreviewMessage = $"已移除内部格 ({_hoveredInteriorCell.X}, {_hoveredInteriorCell.Y}) 的结构。";
        }
    }

    private void AdjustEditorZoom(float delta)
    {
        if (_editorCamera is null)
        {
            return;
        }

        _editorCamera.Size = Mathf.Clamp(_editorCamera.Size + delta, 2.6f, 6.0f);
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

    private void OnEditorPaletteSelected(BuildPrototypeKind kind)
    {
        _selectedInteriorKind = kind;
    }

    private void OnEditorRotateRequested(int direction)
    {
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
        if (_grid is null || _mobileFactory is null || _sinkA is null || _sinkB is null || _hud is null || _cameraRig is null)
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

        var initialPosition = _mobileFactory.WorldFocusPoint;
        _mobileFactory.ApplyTransitInput(_grid, 1.0f, -1.0f, 0.5);
        var movedInTransit = _mobileFactory.WorldFocusPoint.DistanceTo(initialPosition) > 0.05f;

        _editorOpen = true;
        _hud.SetEditorOpen(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);

        var openedInTransit = _hud.IsEditorVisible;
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var rightPaneHover = _hud.IsPointerOverEditor(new Vector2(viewportSize.X - 80.0f, viewportSize.Y * 0.5f));
        var leftPaneHover = !_hud.IsPointerOverEditor(new Vector2(10.0f, 40.0f));

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

        _editorOpen = false;
        _hud.SetEditorOpen(false);
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
        _editorOpen = true;
        _hud.SetEditorOpen(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        var openedWhileDeployed = _hud.IsEditorVisible;
        var portConnected = _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.OutputPort);
        var portOverlayConnected = _hud.PortStatusText.Contains("已连接");
        var miniatureSyncedDeployed = placedStructure is not null
            && placedStructure.GlobalPosition.DistanceTo(_mobileFactory.InteriorSite.CellToWorld(new Vector2I(2, 0))) < 0.05f;
        var firstDelivered = _sinkA.DeliveredTotal;
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        var inputAttachmentTransit = _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.InputPort);
        var inputDeliveredWhileDeployed = (inputSinkInTransit?.DeliveredTotal ?? 0) > inputSinkTransitBaseline;

        _editorOpen = false;
        _hud.SetEditorOpen(false);
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

        if (!startsInCommandMode || !cameraLockedInCommand || !observerActive || !observerCameraActive || !interiorRunsInTransit || !movedInTransit || !openedInTransit || !rightPaneHover || !leftPaneHover || !placedInterior || !interiorPlacedExists || !placedInteriorSink || !interiorSinkExists || !miniatureSyncedInTransit || !inputBlockedInTransit || !blockedDeploy || !edgeBlockedDeploy || !facingAwareCells || !contextualRotateWorks || !firstDeploy || !moveRejectedWhileDeployed || !openedWhileDeployed || !portConnected || !portOverlayConnected || !miniatureSyncedDeployed || firstDelivered <= 0 || !inputDeliveredWhileDeployed || !recalled || !blockedOutputActive || !stayedInPlaceAfterReturn || !reservationsReleased || !secondDeploy || secondDelivered <= 0)
        {
            GD.PushError($"MOBILE_FACTORY_SMOKE_FAILED startsCommand={startsInCommandMode} cameraLocked={cameraLockedInCommand} observerActive={observerActive} observerCamera={observerCameraActive} interiorTransit={interiorRunsInTransit} movedInTransit={movedInTransit} openedTransit={openedInTransit} rightHover={rightPaneHover} leftHover={leftPaneHover} placedInterior={placedInterior} interiorPlacedExists={interiorPlacedExists} placedSink={placedInteriorSink} sinkExists={interiorSinkExists} miniatureTransit={miniatureSyncedInTransit} inputBlockedInTransit={inputBlockedInTransit} blocked={blockedDeploy} edgeBlocked={edgeBlockedDeploy} facingAware={facingAwareCells} contextualRotateWorks={contextualRotateWorks} firstDeploy={firstDeploy} moveRejected={moveRejectedWhileDeployed} openedDeployed={openedWhileDeployed} portConnected={portConnected} portOverlay={portOverlayConnected} miniatureDeployed={miniatureSyncedDeployed} firstDelivered={firstDelivered} inputAttachmentTransit={inputAttachmentTransit} inputDeliveredWhileDeployed={inputDeliveredWhileDeployed} recalled={recalled} blockedOutputActive={blockedOutputActive} stayedInPlaceAfterReturn={stayedInPlaceAfterReturn} released={reservationsReleased} secondDeploy={secondDeploy} secondDelivered={secondDelivered}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_SMOKE_OK firstDelivered={firstDelivered} secondDelivered={secondDelivered}");
        GetTree().Quit();
    }

    private async void RunLargeScenarioSmokeChecks()
    {
        if (_grid is null || _mobileFactory is null || _hud is null || _cameraRig is null || _backgroundFactories.Count < 3 || _scenarioSinks.Count < 4)
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

        _editorOpen = true;
        _hud.SetEditorOpen(true);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        var editorVisible = _hud.IsEditorVisible;
        _editorOpen = false;
        _hud.SetEditorOpen(false);
        _mobileFactory.TryGetInteriorStructure(new Vector2I(1, 3), out var playerInputSinkStructure);
        var playerInputSink = playerInputSinkStructure as SinkStructure;
        var playerInputDeliveredBaseline = playerInputSink?.DeliveredTotal ?? 0;

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

        var anyConnectedBridge = false;
        foreach (var factory in allFactories)
        {
            if (factory.HasConnectedAttachment(BuildPrototypeKind.OutputPort))
            {
                anyConnectedBridge = true;
                break;
            }
        }

        if (!mixedStates || !variedProfiles || !variedPresets || !playerMoved || !editorVisible || !playerDeployed || !backgroundMoved || !deliveredDuringRun || !inputDeliveredDuringRun || !anyConnectedBridge)
        {
            GD.PushError($"MOBILE_FACTORY_LARGE_SMOKE_FAILED mixedStates={mixedStates} variedProfiles={variedProfiles} variedPresets={variedPresets} playerMoved={playerMoved} editorVisible={editorVisible} playerDeployed={playerDeployed} backgroundMoved={backgroundMoved} deliveredDuringRun={deliveredDuringRun} inputDeliveredDuringRun={inputDeliveredDuringRun} anyConnectedBridge={anyConnectedBridge} deployedCount={deployedCount} inTransitCount={inTransitCount} playerInputDelivered={playerInputSink?.DeliveredTotal ?? -1}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_LARGE_SMOKE_OK deployed={deployedCount} inTransit={inTransitCount} delivered={GetPrimaryDeliveryTotal() + GetSecondaryDeliveryTotal()}");
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
            _ => "工厂控制：W/S 前进后退 | A/D 转向 | G 部署预览 | Tab 观察模式 | R 切回移动态 | F 内部编辑；编辑器用 1-0/-/= 选建筑/端口"
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
