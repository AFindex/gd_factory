using Godot;
using System.Collections.Generic;

public partial class FactoryDemo : Node3D
{
    private readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> _definitions = new()
    {
        [BuildPrototypeKind.Producer] = new BuildPrototypeDefinition(BuildPrototypeKind.Producer, "生产器", new Color("9DC08B"), "持续向前方投放原料。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收站", new Color("FDE68A"), "接收物品并统计送达数量。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把左右两路物流汇成前方一路。"),
        [BuildPrototypeKind.Bridge] = new BuildPrototypeDefinition(BuildPrototypeKind.Bridge, "跨桥", new Color("F59E0B"), "让南北和东西两路物流跨越而不互连。"),
[BuildPrototypeKind.Loader] = new BuildPrototypeDefinition(BuildPrototypeKind.Loader, "装载器", new Color("FDBA74"), "把后方带上的物品装入前方机器或回收端。"),
[BuildPrototypeKind.Unloader] = new BuildPrototypeDefinition(BuildPrototypeKind.Unloader, "卸载器", new Color("93C5FD"), "把机器端输出卸到前方传送网络。")
    };

    private GridManager? _grid;
    private SimulationController? _simulation;
    private FactoryCameraRig? _cameraRig;
    private FactoryHud? _hud;
    private Node3D? _structureRoot;
    private Node3D? _previewRoot;
    private MeshInstance3D? _previewCell;
    private MeshInstance3D? _previewArrow;

    private BuildPrototypeKind _selectedKind = BuildPrototypeKind.Producer;
    private FacingDirection _selectedFacing = FacingDirection.East;
    private Vector2I _hoveredCell;
    private bool _hasHoveredCell;
    private bool _canPlaceCurrentCell;
    private string _previewMessage = "把鼠标移到地面网格上选择格子。";

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
        UpdateHoveredCell();
        UpdatePreview();
        UpdateStructureVisuals();
        UpdateHud();

        if (Input.IsActionJustPressed("select_producer"))
        {
            SelectKind(BuildPrototypeKind.Producer);
        }

        if (Input.IsActionJustPressed("select_belt"))
        {
            SelectKind(BuildPrototypeKind.Belt);
        }

        if (Input.IsActionJustPressed("select_sink"))
        {
            SelectKind(BuildPrototypeKind.Sink);
        }

        if (Input.IsActionJustPressed("select_splitter"))
        {
            SelectKind(BuildPrototypeKind.Splitter);
        }

        if (Input.IsActionJustPressed("select_merger"))
        {
            SelectKind(BuildPrototypeKind.Merger);
        }

        if (Input.IsActionJustPressed("select_bridge"))
        {
            SelectKind(BuildPrototypeKind.Bridge);
        }

        if (Input.IsActionJustPressed("select_loader"))
        {
            SelectKind(BuildPrototypeKind.Loader);
        }

        if (Input.IsActionJustPressed("select_unloader"))
        {
            SelectKind(BuildPrototypeKind.Unloader);
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Delete && _hasHoveredCell)
        {
            RemoveStructure(_hoveredCell);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed || IsPointerOverUi())
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left && _hasHoveredCell && _canPlaceCurrentCell)
        {
            PlaceStructure(_selectedKind, _hoveredCell, _selectedFacing);
            GetViewport().SetInputAsHandled();
        }

        if (mouseButton.ButtonIndex == MouseButton.Right && _hasHoveredCell)
        {
            RemoveStructure(_hoveredCell);
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildSceneGraph()
    {
        AddChild(CreateEnvironment());
        AddChild(CreateDirectionalLight());
        AddChild(CreateFloor());
        AddChild(CreateGridLines());

        _structureRoot = new Node3D { Name = "StructureRoot" };
        AddChild(_structureRoot);

        _previewRoot = new Node3D { Name = "PreviewRoot" };
        AddChild(_previewRoot);
        CreatePreviewVisuals();

        _simulation = new SimulationController { Name = "SimulationController" };
        AddChild(_simulation);

        _cameraRig = new FactoryCameraRig();
        AddChild(_cameraRig);

        _hud = new FactoryHud();
        _hud.SelectionChanged += SelectKind;
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
        SelectKind(BuildPrototypeKind.Producer);
    }

    private void SelectKind(BuildPrototypeKind kind)
    {
        _selectedKind = kind;
    }

    private void CreateStarterLayout()
    {
        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(-4, -2), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(-3, -2), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(-2, -2), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(-1, -2), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(0, -2), FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(2, 0), FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(2, 1), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(3, 1), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(4, 1), FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(-5, 3), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Splitter, new Vector2I(-4, 3), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(-4, 2), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(-4, 4), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(-3, 2), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(-3, 4), FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(5, -5), FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(5, -3), FacingDirection.North);
        PlaceStructure(BuildPrototypeKind.Merger, new Vector2I(5, -4), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(6, -4), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(7, -4), FacingDirection.East);

        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(-1, 6), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Bridge, new Vector2I(0, 6), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(1, 6), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(2, 6), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(0, 4), FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(0, 5), FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(0, 7), FacingDirection.South);

        PlaceStructure(BuildPrototypeKind.Producer, new Vector2I(6, 4), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Unloader, new Vector2I(7, 4), FacingDirection.East);
        PlaceStructure(BuildPrototypeKind.Belt, new Vector2I(8, 4), FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Loader, new Vector2I(8, 5), FacingDirection.South);
        PlaceStructure(BuildPrototypeKind.Sink, new Vector2I(8, 6), FacingDirection.South);

        RefreshAllTopology();
    }

    private void UpdateHoveredCell()
    {
        _hasHoveredCell = false;
        _canPlaceCurrentCell = false;
        _previewMessage = "把鼠标移到地面网格上选择格子。";

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

        _canPlaceCurrentCell = _grid.CanPlace(cell);
        _previewMessage = _canPlaceCurrentCell
            ? $"可在 ({cell.X}, {cell.Y}) 放置{_definitions[_selectedKind].DisplayName}，朝向 {FactoryDirection.ToLabel(_selectedFacing)}"
            : $"格子 ({cell.X}, {cell.Y}) 已被占用。";
    }

    private void UpdatePreview()
    {
        if (_grid is null || _previewRoot is null || _previewCell is null || _previewArrow is null)
        {
            return;
        }

        _previewRoot.Visible = _hasHoveredCell;
        if (!_hasHoveredCell)
        {
            return;
        }

        _previewRoot.Position = _grid.CellToWorld(_hoveredCell);
        _previewRoot.Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(_selectedFacing), 0.0f);

        var tint = _canPlaceCurrentCell ? new Color(0.35f, 0.95f, 0.55f, 0.45f) : new Color(1.0f, 0.35f, 0.35f, 0.45f);
        ApplyPreviewColor(_previewCell, tint);
        ApplyPreviewColor(_previewArrow, tint.Lightened(0.1f));
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

    private void UpdateHud()
    {
        if (_hud is null)
        {
            return;
        }

        var definition = _definitions[_selectedKind];
        _hud.SetSelectedKind(_selectedKind, definition.Details);
        _hud.SetHoverCell(_hoveredCell, _hasHoveredCell);
        _hud.SetPreviewStatus(_hasHoveredCell && _canPlaceCurrentCell, _previewMessage);
        _hud.SetRotation(_selectedFacing);

        var sink = FindSinkStructure();
        if (sink is not null)
        {
            _hud.SetSinkStats(sink.DeliveredTotal, sink.DeliveredRate);
        }
    }

    private SinkStructure? FindSinkStructure()
    {
        if (_structureRoot is null)
        {
            return null;
        }

        foreach (var child in _structureRoot.GetChildren())
        {
            if (child is SinkStructure sink)
            {
                return sink;
            }
        }

        return null;
    }

    private void PlaceStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (_grid is null || _structureRoot is null || _simulation is null || !_grid.CanPlace(cell))
        {
            return;
        }

        var structure = FactoryStructureFactory.Create(kind, new FactoryStructurePlacement(_grid, cell, facing));
        _structureRoot.AddChild(structure);
        _grid.PlaceStructure(structure);
        _simulation.RegisterStructure(structure);
        RefreshAllTopology();
    }

    private void RemoveStructure(Vector2I cell)
    {
        if (_grid is null || _simulation is null || !_grid.TryGetStructure(cell, out var structure) || structure is null)
        {
            return;
        }

        _simulation.UnregisterStructure(structure);
        _grid.RemoveStructure(structure);
        structure.QueueFree();
        RefreshAllTopology();
    }

    private void RefreshAllTopology()
    {
        if (_simulation is null)
        {
            return;
        }

        _simulation.RebuildTopology();
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

        _previewArrow = new MeshInstance3D { Name = "PreviewArrow" };
        _previewArrow.Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.28f, 0.18f, FactoryConstants.CellSize * 0.32f) };
        _previewArrow.Position = new Vector3(FactoryConstants.CellSize * 0.34f, 0.18f, 0.0f);
        _previewRoot.AddChild(_previewArrow);

        ApplyPreviewColor(_previewCell, new Color(0.35f, 0.95f, 0.55f, 0.45f));
        ApplyPreviewColor(_previewArrow, new Color(0.35f, 0.95f, 0.55f, 0.45f));
    }

    private static void ApplyPreviewColor(MeshInstance3D meshInstance, Color color)
    {
        var material = new StandardMaterial3D();
        material.AlbedoColor = color;
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.Roughness = 0.4f;
        meshInstance.MaterialOverride = material;
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
        var lineMaterial = new StandardMaterial3D { AlbedoColor = new Color(0.35f, 0.43f, 0.53f, 0.65f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 1.0f };
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
        EnsureAction("select_producer", new InputEventKey { PhysicalKeycode = Key.Key1 });
        EnsureAction("select_belt", new InputEventKey { PhysicalKeycode = Key.Key2 });
        EnsureAction("select_sink", new InputEventKey { PhysicalKeycode = Key.Key3 });
        EnsureAction("select_splitter", new InputEventKey { PhysicalKeycode = Key.Key4 });
        EnsureAction("select_merger", new InputEventKey { PhysicalKeycode = Key.Key5 });
        EnsureAction("select_bridge", new InputEventKey { PhysicalKeycode = Key.Key6 });
        EnsureAction("select_loader", new InputEventKey { PhysicalKeycode = Key.Key7 });
        EnsureAction("select_unloader", new InputEventKey { PhysicalKeycode = Key.Key8 });
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
        if (_grid is null || _cameraRig is null || _cameraRig.Camera is null)
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

        await ToSignal(GetTree().CreateTimer(2.4f), SceneTreeTimer.SignalName.Timeout);

        var sink = FindSinkStructure();
        var delivered = sink?.DeliveredTotal ?? 0;

        if (!placed || !removed || delivered <= 0)
        {
            GD.PushError($"FACTORY_SMOKE_FAILED placed={placed} removed={removed} delivered={delivered}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"FACTORY_SMOKE_OK delivered={delivered}");
        GetTree().Quit();
    }
}
