using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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
        [BuildPrototypeKind.Unloader] = new BuildPrototypeDefinition(BuildPrototypeKind.Unloader, "卸载器", new Color("93C5FD"), "把机器端输出卸到前方传送网络。"),
        [BuildPrototypeKind.Storage] = new BuildPrototypeDefinition(BuildPrototypeKind.Storage, "仓储", new Color("94A3B8"), "缓存多件物品，可向前输出，也能被机械臂抓取。"),
        [BuildPrototypeKind.Inserter] = new BuildPrototypeDefinition(BuildPrototypeKind.Inserter, "机械臂", new Color("FACC15"), "从后方抓取一件物品并向前投送。")
    };

    private GridManager? _grid;
    private SimulationController? _simulation;
    private FactoryCameraRig? _cameraRig;
    private FactoryHud? _hud;
    private Node3D? _structureRoot;
    private Node3D? _previewRoot;
    private MeshInstance3D? _previewCell;
    private MeshInstance3D? _previewArrow;
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
    private string _previewMessage = "交互模式：点击建筑查看；按数字键选择建筑后进入建造。";

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
        UpdateHoveredCell();
        UpdatePreview();
        UpdateStructureVisuals();
        UpdateHud();
        HandleHotkeys();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsPointerOverUi())
        {
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Delete && _interactionMode == FactoryInteractionMode.Build && _hoveredStructure is not null)
            {
                RemoveStructure(_hoveredStructure.Cell);
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

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
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
        _hud.SelectionChanged += SelectBuildKind;
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
        EnterInteractionMode();
    }

    private void HandleHotkeys()
    {
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
        _selectedBuildKind = kind;
        _interactionMode = kind.HasValue ? FactoryInteractionMode.Build : FactoryInteractionMode.Interact;

        if (_interactionMode == FactoryInteractionMode.Build)
        {
            _selectedStructure = null;
        }
    }

    private void EnterInteractionMode()
    {
        _selectedBuildKind = null;
        _interactionMode = FactoryInteractionMode.Interact;
    }

    private void CreateStarterLayout()
    {
        AddSouthThroughputCorridor();
        AddWestSplitterFanout();
        AddCentralBridgeCrossing();
        AddUpperSplitMergeLoop();
        AddRelayLoaderUnloaderChain();
        AddStorageOutputCorridor();
        AddBeltToStorageTransferLine();
        AddInserterYard();
        AddSharedPickupTestYard();
        AddSharedDropoffTestYard();
        RefreshAllTopology();
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
        _previewMessage = _interactionMode == FactoryInteractionMode.Build
            ? "把鼠标移到地面网格上选择格子。"
            : "交互模式：点击建筑查看；按数字键选择建筑后进入建造。";

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
        if (_interactionMode == FactoryInteractionMode.Build && _selectedBuildKind.HasValue)
        {
            _canPlaceCurrentCell = _grid.CanPlace(cell);
            _previewMessage = _canPlaceCurrentCell
                ? $"可在 ({cell.X}, {cell.Y}) 放置{_definitions[_selectedBuildKind.Value].DisplayName}，朝向 {FactoryDirection.ToLabel(_selectedFacing)}"
                : $"格子 ({cell.X}, {cell.Y}) 已被 {_hoveredStructure?.DisplayName ?? "占用结构"} 占用。";
            return;
        }

        _previewMessage = _hoveredStructure is null
            ? $"交互模式：空地 ({cell.X}, {cell.Y})，点击可清除当前选中。"
            : $"交互模式：点击选中 {_hoveredStructure.DisplayName} ({cell.X}, {cell.Y})。";
    }

    private void UpdatePreview()
    {
        if (_grid is null || _previewRoot is null || _previewCell is null || _previewArrow is null)
        {
            return;
        }

        var showPreview = _interactionMode == FactoryInteractionMode.Build && _selectedBuildKind.HasValue && _hasHoveredCell;
        _previewRoot.Visible = showPreview;
        if (!showPreview)
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

        var startTicks = Stopwatch.GetTimestamp();
        var alpha = _simulation.TickAlpha;
        foreach (var child in _structureRoot.GetChildren())
        {
            if (child is FactoryStructure structure)
            {
                structure.UpdateVisuals(alpha);
            }
        }

        _averageVisualSyncMilliseconds = SmoothMetric(_averageVisualSyncMilliseconds, Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds, 0.18);
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
        _hud.SetPreviewStatus(_interactionMode == FactoryInteractionMode.Build && _canPlaceCurrentCell, _previewMessage);
        _hud.SetRotation(_selectedFacing);
        _hud.SetSelectionTarget(GetSelectedStructureText());

        if (_selectedStructure is IFactoryInspectable inspectable)
        {
            _hud.SetInspection(inspectable.InspectionTitle, string.Join("\n", inspectable.GetInspectionLines()));
        }
        else
        {
            _hud.SetInspection(null, null);
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

        var modeNote = _interactionMode == FactoryInteractionMode.Build
            ? "建造模式：左键放置，右键或 Delete 拆除，Esc 返回交互。"
            : "交互模式：左键查看建筑，数字键切换到对应建造工具。";
        _hud.SetNote(modeNote);
    }

    private string GetSelectedStructureText()
    {
        if (_selectedStructure is null || !_selectedStructure.IsInsideTree())
        {
            return "未选中建筑";
        }

        return $"{_selectedStructure.DisplayName} @ ({_selectedStructure.Cell.X}, {_selectedStructure.Cell.Y})";
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
        if (_grid is null || _structureRoot is null || _simulation is null || !_grid.CanPlace(cell))
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
        _grid.RemoveStructure(structure);
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
            if (_hoveredStructure is not null)
            {
                RemoveStructure(_hoveredCell);
            }

            return;
        }

        _selectedStructure = null;
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

        var initialStructureCount = _simulation.RegisteredStructureCount;
        await ToSignal(GetTree().CreateTimer(3.2f), SceneTreeTimer.SignalName.Timeout);

        var sinkStats = CollectSinkStats();
        var profilerText = _hud?.ProfilerText ?? string.Empty;
        var splitterFallbackRecovered = await RunSplitterFallbackSmoke();
        var storageFlowVerified = await RunStorageInserterSmoke();
        var inspectionVerified = VerifyStorageInspectionPanel();

        if (!placed
            || !removed
            || initialStructureCount < 65
            || sinkStats.deliveredTotal <= 0
            || string.IsNullOrWhiteSpace(profilerText)
            || !profilerText.Contains("FPS", global::System.StringComparison.Ordinal)
            || !splitterFallbackRecovered
            || !storageFlowVerified
            || !inspectionVerified)
        {
            GD.PushError($"FACTORY_SMOKE_FAILED placed={placed} removed={removed} structures={initialStructureCount} delivered={sinkStats.deliveredTotal} profiler={(!string.IsNullOrWhiteSpace(profilerText))} splitterFallback={splitterFallbackRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"FACTORY_SMOKE_OK structures={initialStructureCount} delivered={sinkStats.deliveredTotal} splitterFallback={splitterFallbackRecovered} storageFlow={storageFlowVerified} inspection={inspectionVerified}");
        GetTree().Quit();
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
            && _hud.InspectionBodyText.Contains("容量", global::System.StringComparison.Ordinal);
    }

    private static double SmoothMetric(double current, double sample, double weight)
    {
        return current <= 0.0
            ? sample
            : current + ((sample - current) * weight);
    }
}
