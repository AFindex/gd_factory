using Godot;
using System.Collections.Generic;

public partial class BeltStructure : FactoryStructure
{
    private sealed class BeltItemState
    {
        public BeltItemState(FactoryItem item, MeshInstance3D visual)
        {
            Item = item;
            Visual = visual;
        }

        public FactoryItem Item { get; }
        public MeshInstance3D Visual { get; }
        public float Position { get; set; }
        public float PreviousPosition { get; set; }
    }

    private const float ItemSpacing = 0.14f;
    private const float ExitBuffer = 0.985f;

    private readonly List<BeltItemState> _items = new();

    private FacingDirection _inputFacing;
    private MeshInstance3D? _centerMesh;
    private MeshInstance3D? _inputArmMesh;
    private MeshInstance3D? _outputArmMesh;
    private MeshInstance3D? _arrowMesh;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Belt;

    public override string Description => "支持直线与拐弯的传送带，可连续堆积并逐段传递物品。";

    public void RefreshTopology(GridManager grid)
    {
        _inputFacing = DetermineInputFacing(grid);
        Rotation = Vector3.Zero;
        RebuildTrackVisuals();
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!AcceptsFrom(sourceCell))
        {
            return false;
        }

        if (_items.Count > 0 && _items[^1].Position < ItemSpacing)
        {
            return false;
        }

        var itemVisual = CreateItemVisual();
        var state = new BeltItemState(item, itemVisual)
        {
            Position = 0.0f,
            PreviousPosition = 0.0f
        };
        _items.Add(state);
        return true;
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        if (_items.Count == 0)
        {
            return;
        }

        var deltaProgress = (float)(stepSeconds * FactoryConstants.BeltItemsPerSecond);

        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].PreviousPosition = _items[i].Position;
        }

        for (var i = 0; i < _items.Count; i++)
        {
            var itemState = _items[i];
            var desired = itemState.Position + deltaProgress;

            if (i == 0)
            {
                if (desired >= 1.0f)
                {
                    if (simulation.TrySendItemToCell(Cell, GetOutputCell(), itemState.Item))
                    {
                        itemState.Visual.QueueFree();
                        _items.RemoveAt(i);
                        i--;
                        continue;
                    }

                    desired = ExitBuffer;
                }
            }
            else
            {
                desired = Mathf.Min(desired, _items[i - 1].Position - ItemSpacing);
            }

            itemState.Position = Mathf.Clamp(desired, 0.0f, 1.0f);
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var itemState = _items[i];
            var visualProgress = Mathf.Lerp(itemState.PreviousPosition, itemState.Position, tickAlpha);
            itemState.Visual.Position = EvaluatePathPoint(visualProgress);
        }
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return GetOutputCell() == targetCell;
    }

    public new bool AcceptsFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public new Vector2I GetInputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(_inputFacing);
    }

    protected override void BuildVisuals()
    {
        _centerMesh = CreateColoredBox(
            "Center",
            new Vector3(CellSize * 0.42f, 0.12f, CellSize * 0.42f),
            new Color("4B5563"),
            new Vector3(0.0f, 0.08f, 0.0f));

        _inputArmMesh = CreateColoredBox(
            "InputArm",
            new Vector3(CellSize * 0.55f, 0.12f, CellSize * 0.22f),
            new Color("4B5563"),
            Vector3.Zero);

        _outputArmMesh = CreateColoredBox(
            "OutputArm",
            new Vector3(CellSize * 0.55f, 0.12f, CellSize * 0.22f),
            new Color("4B5563"),
            Vector3.Zero);

        _arrowMesh = CreateColoredBox(
            "Arrow",
            new Vector3(CellSize * 0.22f, 0.05f, CellSize * 0.18f),
            new Color("7DD3FC"),
            new Vector3(0.26f * CellSize, 0.16f, 0.0f));

        RebuildTrackVisuals();
    }

    private FacingDirection DetermineInputFacing(GridManager grid)
    {
        var preferred = GetOppositeFacing(Facing);
        var candidates = new FacingDirection[]
        {
            preferred,
            FactoryDirection.RotateCounterClockwise(Facing),
            FactoryDirection.RotateClockwise(Facing)
        };

        foreach (var direction in candidates)
        {
            var sourceCell = Cell + FactoryDirection.ToCellOffset(direction);
            if (grid.TryGetStructure(sourceCell, out var structure) && structure is not null && structure.CanOutputTo(Cell))
            {
                return direction;
            }
        }

        return preferred;
    }

    private void RebuildTrackVisuals()
    {
        if (_inputArmMesh is null || _outputArmMesh is null || _arrowMesh is null)
        {
            return;
        }

        Rotation = Vector3.Zero;

        var inputLocal = ToWorldDirection(_inputFacing);
        var outputLocal = ToWorldDirection(Facing);

        ConfigureArm(_inputArmMesh, inputLocal);
        ConfigureArm(_outputArmMesh, outputLocal);

        ConfigureArrow(_arrowMesh, outputLocal);
    }

    private void ConfigureArm(MeshInstance3D mesh, Vector2 direction)
    {
        var alongX = Mathf.Abs(direction.X) > 0.1f;
        mesh.Mesh = new BoxMesh
        {
            Size = alongX
                ? new Vector3(CellSize * 0.55f, 0.12f, CellSize * 0.22f)
                : new Vector3(CellSize * 0.22f, 0.12f, CellSize * 0.55f)
        };

        mesh.Position = new Vector3(direction.X * CellSize * 0.24f, 0.08f, direction.Y * CellSize * 0.24f);
    }

    private void ConfigureArrow(MeshInstance3D mesh, Vector2 direction)
    {
        var alongX = Mathf.Abs(direction.X) > 0.1f;
        mesh.Mesh = new BoxMesh
        {
            Size = alongX
                ? new Vector3(CellSize * 0.20f, 0.05f, CellSize * 0.14f)
                : new Vector3(CellSize * 0.14f, 0.05f, CellSize * 0.20f)
        };

        mesh.Position = new Vector3(direction.X * CellSize * 0.32f, 0.16f, direction.Y * CellSize * 0.32f);
    }

    private MeshInstance3D CreateItemVisual()
    {
        return CreateColoredBox(
            $"Item_{_items.Count}",
            new Vector3(CellSize * 0.18f, CellSize * 0.18f, CellSize * 0.18f),
            new Color("FFD166"),
            EvaluatePathPoint(0.0f));
    }

    private MeshInstance3D CreateColoredBox(string name, Vector3 size, Color color, Vector3 localPosition)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = name;
        mesh.Mesh = new BoxMesh { Size = size };
        mesh.Position = localPosition;
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 0.85f
        };
        AddChild(mesh);
        return mesh;
    }

    private Vector3 EvaluatePathPoint(float progress)
    {
        var edgeDistance = CellSize * 0.5f;
        var inputLocal = ToWorldDirection(_inputFacing) * edgeDistance;
        var outputLocal = ToWorldDirection(Facing) * edgeDistance;

        if (Mathf.Abs(inputLocal.X + outputLocal.X) < 0.01f && Mathf.Abs(inputLocal.Y + outputLocal.Y) < 0.01f)
        {
            var point = inputLocal.Lerp(outputLocal, progress);
            return new Vector3(point.X, 0.34f, point.Y);
        }

        var start = inputLocal;
        var end = outputLocal;
        var control = Vector2.Zero;
        var oneMinusT = 1.0f - progress;
        var point2D =
            oneMinusT * oneMinusT * start +
            2.0f * oneMinusT * progress * control +
            progress * progress * end;

        return new Vector3(point2D.X, 0.34f, point2D.Y);
    }

    private static Vector2 ToWorldDirection(FacingDirection direction)
    {
        var offset = FactoryDirection.ToCellOffset(direction);
        return new Vector2(offset.X, offset.Y);
    }

    private static FacingDirection GetOppositeFacing(FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.East => FacingDirection.West,
            FacingDirection.West => FacingDirection.East,
            FacingDirection.North => FacingDirection.South,
            _ => FacingDirection.North
        };
    }
}
