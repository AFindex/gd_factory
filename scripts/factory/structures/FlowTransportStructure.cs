using Godot;
using System.Collections.Generic;

public abstract partial class FlowTransportStructure : FactoryStructure
{
    protected sealed class TransitItemState
    {
        public TransitItemState(FactoryItem item, MeshInstance3D visual, Vector2I sourceCell, Vector2I targetCell)
        {
            Item = item;
            Visual = visual;
            SourceCell = sourceCell;
            TargetCell = targetCell;
        }

        public FactoryItem Item { get; }
        public MeshInstance3D Visual { get; }
        public Vector2I SourceCell { get; }
        public Vector2I TargetCell { get; set; }
        public float Position { get; set; }
        public float PreviousPosition { get; set; }
    }

    private readonly List<TransitItemState> _items = new();

    protected virtual float ItemSpacing => 0.14f;
    protected virtual float ExitBuffer => 0.985f;
    protected virtual float TravelSpeed => FactoryConstants.BeltItemsPerSecond;
    protected virtual float ItemHeight => 0.34f;

    public override bool IsTransportNode => true;
    public int TransitItemCount => _items.Count;
    protected IList<TransitItemState> TransitItems => _items;

    public sealed override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveFrom(sourceCell))
        {
            return false;
        }

        if (!TryResolveTargetCell(item, sourceCell, simulation, out var targetCell))
        {
            return false;
        }

        if (_items.Count > 0 && _items[^1].Position < ItemSpacing)
        {
            return false;
        }

        var state = new TransitItemState(item, CreateTransitVisual(), sourceCell, targetCell)
        {
            Position = 0.0f,
            PreviousPosition = 0.0f
        };
        state.Visual.Position = EvaluatePathPoint(state, 0.0f);
        _items.Add(state);
        OnTransitItemAccepted(state);
        return true;
    }

    public sealed override bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveFrom(sourceCell))
        {
            return false;
        }

        if (!TryResolveTargetCell(item, sourceCell, simulation, out _))
        {
            return false;
        }

        return _items.Count == 0 || _items[^1].Position >= ItemSpacing;
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        if (_items.Count == 0)
        {
            return;
        }

        RefreshTransitTargets(simulation);

        var deltaProgress = (float)(stepSeconds * TravelSpeed);

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
                    if (TryDispatchItem(itemState, simulation))
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
            itemState.Visual.Position = EvaluatePathPoint(itemState, visualProgress);
        }
    }

    protected MeshInstance3D CreateTransitVisual()
    {
        return CreateColoredBox(
            $"Item_{_items.Count}",
            new Vector3(CellSize * 0.18f, CellSize * 0.18f, CellSize * 0.18f),
            new Color("FFD166"),
            new Vector3(0.0f, ItemHeight, 0.0f));
    }

    protected MeshInstance3D CreateColoredBox(string name, Vector3 size, Color color, Vector3 localPosition)
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

    protected virtual Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var edgeDistance = CellSize * 0.5f;
        var input = ToDirectionVector(state.SourceCell - Cell) * edgeDistance;
        var output = ToDirectionVector(state.TargetCell - Cell) * edgeDistance;

        if (Mathf.Abs(input.X + output.X) < 0.01f && Mathf.Abs(input.Y + output.Y) < 0.01f)
        {
            var point = input.Lerp(output, progress);
            return new Vector3(point.X, ItemHeight, point.Y);
        }

        var oneMinus = 1.0f - progress;
        var point2D =
            oneMinus * oneMinus * input +
            2.0f * oneMinus * progress * Vector2.Zero +
            progress * progress * output;

        return new Vector3(point2D.X, ItemHeight, point2D.Y);
    }

    protected static Vector2 ToDirectionVector(Vector2I offset)
    {
        return new Vector2(Mathf.Clamp(offset.X, -1, 1), Mathf.Clamp(offset.Y, -1, 1));
    }

    protected virtual bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        return simulation.TrySendItem(this, state.TargetCell, state.Item);
    }

    protected virtual void RefreshTransitTargets(SimulationController simulation)
    {
    }

    protected virtual void OnTransitItemAccepted(TransitItemState state)
    {
    }

    protected abstract bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell);
}
