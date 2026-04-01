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
        BuildPrototypeKind.Sink
    };

    private readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> _definitions = new()
    {
        [BuildPrototypeKind.Producer] = new BuildPrototypeDefinition(BuildPrototypeKind.Producer, "生产器", new Color("9DC08B"), "持续向前方投放原料。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把左右两路物流汇成前方一路。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收器", new Color("FDE68A"), "吞掉输入物品并作为内部消费端。")
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

    private MobileFactoryInstance? _mobileFactory;
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

        if (HasSmokeTestFlag())
        {
            CallDeferred(nameof(RunSmokeChecks));
        }
    }

    public override void _Process(double delta)
    {
        _mousePosition = GetViewport().GetMousePosition();
        HandleGlobalCommands();
        _mobileFactory?.UpdateRuntime(delta);
        PullFactoryStatusMessage();
        UpdatePaneFocus();
        HandleWorldControlInput(delta);
        UpdateHoveredAnchor();
        UpdateHoveredInteriorCell();
        UpdateWorldPreview();
        UpdateInteriorPreview();
        UpdateWorldStatusMessage(delta);
        UpdateStructureVisuals();
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
        AddChild(CreateFloor());
        AddChild(CreateGridLines());

        _structureRoot = new Node3D { Name = "MobileDemoStructures" };
        AddChild(_structureRoot);

        _worldPreviewRoot = new Node3D { Name = "WorldPreviewRoot" };
        AddChild(_worldPreviewRoot);
        CreateWorldPreviewVisuals();

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
    }

    private void ConfigureGameplay()
    {
        _grid = new GridManager(
            new Vector2I(FactoryConstants.GridMin, FactoryConstants.GridMin),
            new Vector2I(FactoryConstants.GridMax, FactoryConstants.GridMax),
            FactoryConstants.CellSize);

        _simulation!.Configure(_grid);
        _cameraRig!.ConfigureBounds(_grid.GetWorldMin() + Vector2.One * 4.0f, _grid.GetWorldMax() - Vector2.One * 4.0f);

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
        _sinkA = PlaceWorldStructure(BuildPrototypeKind.Sink, new Vector2I(-1, -3), FacingDirection.East) as SinkStructure;
        PlaceWorldStructure(BuildPrototypeKind.Belt, new Vector2I(-3, -3), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, new Vector2I(-2, -3), FacingDirection.East);

        _sinkB = PlaceWorldStructure(BuildPrototypeKind.Sink, new Vector2I(7, 3), FacingDirection.East) as SinkStructure;
        PlaceWorldStructure(BuildPrototypeKind.Belt, new Vector2I(5, 3), FacingDirection.East);
        PlaceWorldStructure(BuildPrototypeKind.Belt, new Vector2I(6, 3), FacingDirection.East);

        PlaceWorldStructure(BuildPrototypeKind.Sink, new Vector2I(0, 1), FacingDirection.East);
        _simulation!.RebuildTopology();
    }

    private void SpawnMobileFactory()
    {
        _mobileFactory = new MobileFactoryInstance("demo-mobile-factory", _structureRoot!, _simulation!);
        _selectedDeployFacing = _mobileFactory.TransitFacing;
        UpdateInteriorPreviewSizing();
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
            CancelWorldCommand();
        }

        if (Input.IsActionJustPressed("recall_mobile_factory"))
        {
            RecallFactory();
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
                _selectedDeployFacing = FactoryDirection.RotateCounterClockwise(_selectedDeployFacing);
            }

            if (Input.IsActionJustPressed("deploy_rotate_right"))
            {
                _selectedDeployFacing = FactoryDirection.RotateClockwise(_selectedDeployFacing);
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

        if (keyEvent.Keycode == Key.Key1)
        {
            _selectedInteriorKind = InteriorPalette[0];
            return true;
        }

        if (keyEvent.Keycode == Key.Key2)
        {
            _selectedInteriorKind = InteriorPalette[1];
            return true;
        }

        if (keyEvent.Keycode == Key.Key3)
        {
            _selectedInteriorKind = InteriorPalette[2];
            return true;
        }

        if (keyEvent.Keycode == Key.Key4)
        {
            _selectedInteriorKind = InteriorPalette[3];
            return true;
        }

        if (keyEvent.Keycode == Key.Key5)
        {
            _selectedInteriorKind = InteriorPalette[4];
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

        if (_mobileFactory.CanPlaceInterior(cell))
        {
            _canPlaceInteriorCell = true;
            _interiorPreviewMessage = $"可在内部格 ({cell.X}, {cell.Y}) 放置{_definitions[_selectedInteriorKind].DisplayName}。";
            return;
        }

        if (_mobileFactory.IsProtectedInteriorCell(cell))
        {
            _interiorPreviewMessage = "这个格子是对外输出端口，不能拆除或覆盖。";
            return;
        }

        _interiorPreviewMessage = $"内部格 ({cell.X}, {cell.Y}) 已被占用，右键可拆除普通内部件。";
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

        var index = 0;
        foreach (var cell in _mobileFactory.GetFootprintCells(_hoveredAnchor, _selectedDeployFacing))
        {
            var mesh = _worldPreviewFootprintMeshes[index++];
            mesh.Visible = true;
            mesh.Position = _grid.CellToWorld(cell) + new Vector3(0.0f, 0.05f, 0.0f);
            ApplyPreviewColor(mesh, footprintColor);
        }

        index = 0;
        foreach (var cell in _mobileFactory.GetPortCells(_hoveredAnchor, _selectedDeployFacing))
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
                _worldPreviewMessage = "部署预览：移动鼠标选择落点，Q/E 旋转朝向，左键确认，Esc/G 取消。";
                _worldStatusPositive = true;
                break;
            default:
                _worldPreviewMessage = _mobileFactory.State switch
                {
                    MobileFactoryLifecycleState.Deployed => "已部署：按 R 回收，Tab 进入观察模式，F 打开内部编辑。",
                    MobileFactoryLifecycleState.AutoDeploying => "自动部署中：移动工厂会自行前往目标并对齐朝向，Esc 可取消。",
                    MobileFactoryLifecycleState.Recalling => "回收中：部署机构正在收拢，很快返回运输位。",
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
        _editorCamera.Position = focus + new Vector3(0.0f, 7.0f, 0.0f);
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
        _hud.SetDeliveryStats(_sinkA?.DeliveredTotal ?? 0, _sinkB?.DeliveredTotal ?? 0);
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
        ShowWorldEvent("部署预览已开启，移动鼠标选点，Q/E 旋转，左键确认。", true);
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

    private void RecallFactory()
    {
        if (_mobileFactory?.Recall() == true)
        {
            SetControlMode(MobileFactoryControlMode.FactoryCommand);
        }
        else if (_mobileFactory is not null && _mobileFactory.State != MobileFactoryLifecycleState.Deployed)
        {
            ShowWorldEvent("当前未处于部署态，不能回收。", false);
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
        _cameraRig.FocusWorldPositionInViewport(_mobileFactory.WorldFocusPoint, targetScreenPosition);
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
                if (_mobileFactory.TryGetInteriorStructure(cell, out var structure) && structure is not null && !_mobileFactory.IsProtectedInteriorCell(cell))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void CreateWorldPreviewVisuals()
    {
        if (_worldPreviewRoot is null)
        {
            return;
        }

        for (var i = 0; i < 4; i++)
        {
            var footprint = new MeshInstance3D { Name = $"PreviewFootprint_{i}", Visible = false };
            footprint.Mesh = new BoxMesh
            {
                Size = new Vector3(FactoryConstants.CellSize * 0.92f, 0.08f, FactoryConstants.CellSize * 0.92f)
            };
            _worldPreviewRoot.AddChild(footprint);
            _worldPreviewFootprintMeshes.Add(footprint);
        }

        var port = new MeshInstance3D { Name = "PreviewPort", Visible = false };
        port.Mesh = new BoxMesh
        {
            Size = new Vector3(FactoryConstants.CellSize * 0.55f, 0.12f, FactoryConstants.CellSize * 0.55f)
        };
        _worldPreviewRoot.AddChild(port);
        _worldPreviewPortMeshes.Add(port);

        _worldPreviewFacingArrow = new MeshInstance3D { Name = "PreviewFacingArrow", Visible = false };
        _worldPreviewFacingArrow.Mesh = new BoxMesh
        {
            Size = new Vector3(FactoryConstants.CellSize * 0.42f, 0.16f, FactoryConstants.CellSize * 0.22f)
        };
        _worldPreviewRoot.AddChild(_worldPreviewFacingArrow);
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
        EnsureAction("recall_mobile_factory", new InputEventKey { PhysicalKeycode = Key.R });
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

    private static bool HasSmokeTestFlag()
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

        var blockedDeploy = !_mobileFactory.CanDeployAt(_grid, BlockedAnchor, FacingDirection.East);
        var southPortCell = FirstCell(_mobileFactory.GetPortCells(AnchorA, FacingDirection.South));
        var westFootprintCell = FirstCell(_mobileFactory.GetFootprintCells(AnchorA, FacingDirection.West));
        var facingAwareCells = southPortCell == new Vector2I(-6, -1) && westFootprintCell == new Vector2I(-6, -3);

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
        var portConnected = _mobileFactory.OutputBridge.IsConnectedToWorld;
        var portOverlayConnected = _hud.PortStatusText.Contains("已连接");
        var miniatureSyncedDeployed = placedStructure is not null
            && placedStructure.GlobalPosition.DistanceTo(_mobileFactory.InteriorSite.CellToWorld(new Vector2I(2, 0))) < 0.05f;
        var firstDelivered = _sinkA.DeliveredTotal;

        _editorOpen = false;
        _hud.SetEditorOpen(false);
        var recalled = _mobileFactory.Recall();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 1.2f);
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

        if (!startsInCommandMode || !cameraLockedInCommand || !observerActive || !observerCameraActive || !interiorRunsInTransit || !movedInTransit || !openedInTransit || !rightPaneHover || !leftPaneHover || !placedInterior || !interiorPlacedExists || !placedInteriorSink || !interiorSinkExists || !miniatureSyncedInTransit || !blockedDeploy || !facingAwareCells || !firstDeploy || !moveRejectedWhileDeployed || !openedWhileDeployed || !portConnected || !portOverlayConnected || !miniatureSyncedDeployed || firstDelivered <= 0 || !recalled || !reservationsReleased || !secondDeploy || secondDelivered <= 0)
        {
            GD.PushError($"MOBILE_FACTORY_SMOKE_FAILED startsCommand={startsInCommandMode} cameraLocked={cameraLockedInCommand} observerActive={observerActive} observerCamera={observerCameraActive} interiorTransit={interiorRunsInTransit} movedInTransit={movedInTransit} openedTransit={openedInTransit} rightHover={rightPaneHover} leftHover={leftPaneHover} placedInterior={placedInterior} interiorPlacedExists={interiorPlacedExists} placedSink={placedInteriorSink} sinkExists={interiorSinkExists} miniatureTransit={miniatureSyncedInTransit} blocked={blockedDeploy} facingAware={facingAwareCells} firstDeploy={firstDeploy} moveRejected={moveRejectedWhileDeployed} openedDeployed={openedWhileDeployed} portConnected={portConnected} portOverlay={portOverlayConnected} miniatureDeployed={miniatureSyncedDeployed} firstDelivered={firstDelivered} recalled={recalled} released={reservationsReleased} secondDeploy={secondDeploy} secondDelivered={secondDelivered}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"MOBILE_FACTORY_SMOKE_OK firstDelivered={firstDelivered} secondDelivered={secondDelivered}");
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
            MobileFactoryControlMode.DeployPreview => "部署预览：左键确认 | Q/E 旋转朝向 | G/Esc 取消 | F 内部编辑",
            _ => "工厂控制：W/S 前进后退 | A/D 转向 | G 部署预览 | Tab 观察模式 | R 回收 | F 内部编辑；编辑器用 1-5 选建筑"
        };
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

        floor.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color("1F2937"),
            Roughness = 1.0f
        };
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
}
