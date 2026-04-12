using Godot;
using System.Collections.Generic;

public abstract partial class FlowTransportStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
{
    protected sealed class TransitItemState
    {
        public TransitItemState(
            FactoryItem item,
            FactoryTransportRenderDescriptorSet renderDescriptors,
            Vector2I sourceCell,
            Vector2I targetCell,
            Node3D? legacyVisual = null)
        {
            Item = item;
            RenderDescriptors = renderDescriptors;
            SourceCell = sourceCell;
            TargetCell = targetCell;
            LegacyVisual = legacyVisual;
            LaneKey = 0;
        }

        public FactoryItem Item { get; }
        public FactoryTransportRenderDescriptorSet RenderDescriptors { get; }
        public Vector2I SourceCell { get; }
        public Vector2I TargetCell { get; set; }
        public Node3D? LegacyVisual { get; set; }
        public int LaneKey { get; set; }
        public float Position { get; set; }
        public float PreviousPosition { get; set; }
        public float OccupiedLengthProgress { get; set; }
    }

    private readonly List<TransitItemState> _items = new();

    protected virtual float ItemSpacing => 0.14f;
    protected virtual float ExitBuffer => 0.985f;
    protected virtual float TravelSpeed => FactoryConstants.BeltItemsPerSecond;
    protected virtual float ItemHeight => 0.34f;
    protected virtual float ProviderPickupThreshold => 0.78f;

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

        var renderDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(item, CellSize);
        var laneKey = GetTransitLaneKey(sourceCell, targetCell);
        var tailIndex = FindLastItemIndexInLane(laneKey);
        var occupiedLengthProgress = ResolveOccupiedLengthProgress(renderDescriptors);
        if (!HasSpawnClearance(tailIndex, occupiedLengthProgress))
        {
            return false;
        }

        var legacyVisual = GetTransportRenderManager() is null ? CreateTransitVisual(item) : null;
        var state = new TransitItemState(item, renderDescriptors, sourceCell, targetCell, legacyVisual)
        {
            LaneKey = laneKey,
            Position = 0.0f,
            PreviousPosition = 0.0f,
            OccupiedLengthProgress = occupiedLengthProgress
        };
        if (state.LegacyVisual is not null)
        {
            state.LegacyVisual.Position = EvaluatePathPoint(state, 0.0f);
        }

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

        if (!TryResolveTargetCell(item, sourceCell, simulation, out var targetCell))
        {
            return false;
        }

        var laneKey = GetTransitLaneKey(sourceCell, targetCell);
        var tailIndex = FindLastItemIndexInLane(laneKey);
        var occupiedLengthProgress = ResolveOccupiedLengthProgress(FactoryTransportVisualFactory.ResolveDescriptorSet(item, CellSize));
        return HasSpawnClearance(tailIndex, occupiedLengthProgress);
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;

        if (_items.Count == 0 || !CanProvideTo(requesterCell))
        {
            return false;
        }

        if (!TryFindProvidedStateIndex(requesterCell, out var stateIndex))
        {
            return false;
        }

        var state = _items[stateIndex];
        item = state.Item;
        return true;
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;

        if (!TryPeekProvidedItem(requesterCell, simulation, out var previewItem))
        {
            return false;
        }

        if (!TryFindProvidedStateIndex(requesterCell, out var stateIndex))
        {
            item = null;
            return false;
        }

        item = previewItem;
        FreeLegacyVisual(_items[stateIndex]);
        _items.RemoveAt(stateIndex);
        return true;
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveProvidedFrom(sourceCell))
        {
            return false;
        }

        if (!TryResolveTargetCell(item, sourceCell, simulation, out var targetCell))
        {
            return false;
        }

        var laneKey = GetTransitLaneKey(sourceCell, targetCell);
        var tailIndex = FindLastItemIndexInLane(laneKey);
        var occupiedLengthProgress = ResolveOccupiedLengthProgress(FactoryTransportVisualFactory.ResolveDescriptorSet(item, CellSize));
        return HasSpawnClearance(tailIndex, occupiedLengthProgress);
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveProvidedFrom(sourceCell))
        {
            return false;
        }

        if (!TryResolveTargetCell(item, sourceCell, simulation, out var targetCell))
        {
            return false;
        }

        var laneKey = GetTransitLaneKey(sourceCell, targetCell);
        var renderDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(item, CellSize);
        var tailIndex = FindLastItemIndexInLane(laneKey);
        var occupiedLengthProgress = ResolveOccupiedLengthProgress(renderDescriptors);
        if (!HasSpawnClearance(tailIndex, occupiedLengthProgress))
        {
            return false;
        }

        var legacyVisual = GetTransportRenderManager() is null ? CreateTransitVisual(item) : null;
        var state = new TransitItemState(item, renderDescriptors, sourceCell, targetCell, legacyVisual)
        {
            LaneKey = laneKey,
            Position = 0.0f,
            PreviousPosition = 0.0f,
            OccupiedLengthProgress = occupiedLengthProgress
        };
        if (state.LegacyVisual is not null)
        {
            state.LegacyVisual.Position = EvaluatePathPoint(state, 0.0f);
        }

        _items.Add(state);
        OnTransitItemAccepted(state);
        return true;
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        if (_items.Count == 0)
        {
            return;
        }

        RefreshTransitTargets(simulation);
        RefreshTransitLanes();

        var deltaProgress = (float)(stepSeconds * TravelSpeed);

        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].PreviousPosition = _items[i].Position;
        }

        for (var i = 0; i < _items.Count; i++)
        {
            var itemState = _items[i];
            var desired = itemState.Position + deltaProgress;
            var previousLaneIndex = FindPreviousItemIndexInLane(i, itemState.LaneKey);

            if (previousLaneIndex < 0)
            {
                if (desired >= 1.0f)
                {
                    if (TryDispatchItem(itemState, simulation))
                    {
                        FreeLegacyVisual(itemState);
                        _items.RemoveAt(i);
                        i--;
                        continue;
                    }

                    desired = ExitBuffer;
                }
            }
            else
            {
                desired = Mathf.Min(
                    desired,
                    _items[previousLaneIndex].Position - ResolvePairSpacing(_items[previousLaneIndex], itemState));
            }

            itemState.Position = Mathf.Clamp(desired, 0.0f, 1.0f);
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        var renderManager = GetTransportRenderManager();
        for (var i = 0; i < _items.Count; i++)
        {
            var itemState = _items[i];
            var visualProgress = Mathf.Lerp(itemState.PreviousPosition, itemState.Position, tickAlpha);
            var localPoint = EvaluatePathPoint(itemState, visualProgress);
            if (renderManager is not null)
            {
                FreeLegacyVisual(itemState);
                renderManager.SubmitSnapshot(new FactoryTransportRenderSnapshot(
                    ToGlobal(localPoint),
                    Cell,
                    itemState.RenderDescriptors));
                continue;
            }

            if (itemState.LegacyVisual is null)
            {
                itemState.LegacyVisual = CreateTransitVisual(itemState.Item);
            }

            itemState.LegacyVisual.Position = localPoint;
        }
    }

    protected Node3D CreateTransitVisual(FactoryItem item)
    {
        var visual = FactoryTransportVisualFactory.CreateVisual(item, CellSize);
        visual.Name = $"Item_{item.Id}";
        AddChild(visual);
        return visual;
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

    protected virtual bool CanProvideTo(Vector2I requesterCell)
    {
        return CanOutputTo(requesterCell);
    }

    protected virtual bool CanRequesterTakeState(TransitItemState state, Vector2I requesterCell)
    {
        return state.TargetCell == requesterCell;
    }

    protected virtual int GetTransitLaneKey(Vector2I sourceCell, Vector2I targetCell)
    {
        return 0;
    }

    protected virtual bool CanReceiveProvidedFrom(Vector2I sourceCell)
    {
        return CanReceiveFrom(sourceCell);
    }

    protected override void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
        base.CaptureRuntimeState(snapshot);
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index];
            snapshot.TransitItems.Add(new FactoryRuntimeTransitItemSnapshot
            {
                Item = FactoryRuntimeItemSnapshot.FromItem(item.Item),
                SourceCell = FactoryRuntimeInt2.FromVector2I(item.SourceCell),
                TargetCell = FactoryRuntimeInt2.FromVector2I(item.TargetCell),
                LaneKey = item.LaneKey,
                Position = item.Position,
                PreviousPosition = item.PreviousPosition
            });
        }
    }

    protected override void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        base.ApplyRuntimeState(snapshot, simulation);
        RestoreTransitItems(snapshot.TransitItems, simulation);
    }

    private FactoryTransportRenderManager? GetTransportRenderManager()
    {
        return GetTree()?.GetFirstNodeInGroup(FactoryTransportRenderManager.GroupName) as FactoryTransportRenderManager;
    }

    private static void FreeLegacyVisual(TransitItemState state)
    {
        if (state.LegacyVisual is null)
        {
            return;
        }

        state.LegacyVisual.QueueFree();
        state.LegacyVisual = null;
    }

    private void RefreshTransitLanes()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].LaneKey = GetTransitLaneKey(_items[i].SourceCell, _items[i].TargetCell);
        }
    }

    private void RestoreTransitItems(
        IReadOnlyList<FactoryRuntimeTransitItemSnapshot> transitItems,
        SimulationController simulation)
    {
        for (var index = 0; index < _items.Count; index++)
        {
            FreeLegacyVisual(_items[index]);
        }

        _items.Clear();

        for (var index = 0; index < transitItems.Count; index++)
        {
            var transit = transitItems[index];
            var item = transit.Item.ToItem(simulation);
            var renderDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(item, CellSize);
            var legacyVisual = GetTransportRenderManager() is null ? CreateTransitVisual(item) : null;
            var state = new TransitItemState(
                item,
                renderDescriptors,
                transit.SourceCell.ToVector2I(),
                transit.TargetCell.ToVector2I(),
                legacyVisual)
            {
                LaneKey = transit.LaneKey,
                Position = Mathf.Clamp(transit.Position, 0.0f, 1.0f),
                PreviousPosition = Mathf.Clamp(transit.PreviousPosition, 0.0f, 1.0f),
                OccupiedLengthProgress = ResolveOccupiedLengthProgress(renderDescriptors)
            };

            if (state.LegacyVisual is not null)
            {
                state.LegacyVisual.Position = EvaluatePathPoint(state, state.Position);
            }

            _items.Add(state);
        }
    }

    private int FindLastItemIndexInLane(int laneKey)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i].LaneKey == laneKey)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindPreviousItemIndexInLane(int index, int laneKey)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (_items[i].LaneKey == laneKey)
            {
                return i;
            }
        }

        return -1;
    }

    private float ResolveOccupiedLengthProgress(FactoryTransportRenderDescriptorSet renderDescriptors)
    {
        return FactoryTransportVisualFactory.EstimateOccupiedLengthProgress(renderDescriptors, CellSize);
    }

    private bool HasSpawnClearance(int tailIndex, float incomingOccupiedLengthProgress)
    {
        return tailIndex < 0
            || _items[tailIndex].Position >= ResolveRequiredSpacing(_items[tailIndex].OccupiedLengthProgress, incomingOccupiedLengthProgress);
    }

    private float ResolvePairSpacing(TransitItemState leadingState, TransitItemState trailingState)
    {
        return ResolveRequiredSpacing(leadingState.OccupiedLengthProgress, trailingState.OccupiedLengthProgress);
    }

    private static float ResolveRequiredSpacing(float leadingOccupiedLengthProgress, float trailingOccupiedLengthProgress)
    {
        var dominantLength = Mathf.Max(leadingOccupiedLengthProgress, trailingOccupiedLengthProgress);
        return Mathf.Clamp(
            dominantLength + 0.06f,
            0.08f,
            0.98f);
    }

    private bool TryFindProvidedStateIndex(Vector2I requesterCell, out int stateIndex)
    {
        stateIndex = -1;
        var furthestProgress = float.MinValue;

        for (var i = 0; i < _items.Count; i++)
        {
            var state = _items[i];
            if (state.Position < ProviderPickupThreshold || !CanRequesterTakeState(state, requesterCell))
            {
                continue;
            }

            if (state.Position <= furthestProgress)
            {
                continue;
            }

            furthestProgress = state.Position;
            stateIndex = i;
        }

        return stateIndex >= 0;
    }

    protected abstract bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell);
}
