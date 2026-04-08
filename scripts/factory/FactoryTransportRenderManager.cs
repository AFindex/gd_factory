using Godot;
using System.Collections.Generic;

public readonly struct FactoryTransportRenderSnapshot
{
    public FactoryTransportRenderSnapshot(
        Vector3 worldPosition,
        Vector2I ownerCell,
        FactoryTransportRenderDescriptorSet descriptors)
    {
        WorldPosition = worldPosition;
        OwnerCell = ownerCell;
        Descriptors = descriptors;
    }

    public Vector3 WorldPosition { get; }
    public Vector2I OwnerCell { get; }
    public FactoryTransportRenderDescriptorSet Descriptors { get; }
}

public sealed class FactoryTransportRenderStats
{
    public int TotalActiveItems { get; init; }
    public int VisibleItems { get; init; }
    public int ActiveBuckets { get; init; }
    public bool OptimizedPathActive { get; init; }
}

public partial class FactoryTransportRenderManager : Node3D
{
    public const string GroupName = "factory_transport_render_manager";
    private const float NearDistanceCells = 8.0f;
    private const float MidDistanceCells = 18.0f;

    private sealed class BatchBucket
    {
        public required FactoryTransportRenderDescriptor Descriptor { get; init; }
        public required MultiMesh MultiMesh { get; init; }
        public required MultiMeshInstance3D Instance { get; init; }
        public int UsedCount { get; set; }
    }

    private readonly Dictionary<string, BatchBucket> _buckets = new();
    private Rect2I _visibleRect;
    private bool _hasVisibleRect;
    private Vector3 _cameraWorldPosition;
    private int _visiblePaddingCells = 2;
    private int _totalActiveItems;
    private int _visibleItems;
    private int _activeBuckets;

    public int VisiblePaddingCells
    {
        get => _visiblePaddingCells;
        set => _visiblePaddingCells = Mathf.Max(0, value);
    }

    public bool OptimizedPathActive => true;

    public override void _Ready()
    {
        Name = "FactoryTransportRenderManager";
        AddToGroup(GroupName);
    }

    public void BeginFrame(Rect2I visibleRect, bool hasVisibleRect, Vector3 cameraWorldPosition)
    {
        _visibleRect = visibleRect;
        _hasVisibleRect = hasVisibleRect;
        _cameraWorldPosition = cameraWorldPosition;
        _totalActiveItems = 0;
        _visibleItems = 0;
        _activeBuckets = 0;

        foreach (var pair in _buckets)
        {
            pair.Value.UsedCount = 0;
            pair.Value.Instance.Visible = false;
        }
    }

    public void SubmitSnapshot(FactoryTransportRenderSnapshot snapshot)
    {
        _totalActiveItems++;
        if (_hasVisibleRect && !IsInsidePaddedRect(snapshot.OwnerCell))
        {
            return;
        }

        var tier = ResolveTier(snapshot.WorldPosition);
        var descriptor = snapshot.Descriptors.ResolveBatchableForTier(tier);
        if (!descriptor.IsBatchable)
        {
            return;
        }

        var bucket = GetOrCreateBucket(descriptor);
        EnsureCapacity(bucket, bucket.UsedCount + 1);
        bucket.MultiMesh.SetInstanceTransform(bucket.UsedCount, new Transform3D(Basis.Identity, snapshot.WorldPosition));
        bucket.UsedCount++;
        _visibleItems++;
    }

    public void EndFrame()
    {
        foreach (var pair in _buckets)
        {
            var bucket = pair.Value;
            bucket.MultiMesh.VisibleInstanceCount = bucket.UsedCount;
            bucket.Instance.Visible = bucket.UsedCount > 0;
            if (bucket.UsedCount > 0)
            {
                _activeBuckets++;
            }
        }
    }

    public FactoryTransportRenderStats GetStats()
    {
        return new FactoryTransportRenderStats
        {
            TotalActiveItems = _totalActiveItems,
            VisibleItems = _visibleItems,
            ActiveBuckets = _activeBuckets,
            OptimizedPathActive = OptimizedPathActive
        };
    }

    private BatchBucket GetOrCreateBucket(FactoryTransportRenderDescriptor descriptor)
    {
        if (_buckets.TryGetValue(descriptor.BatchKey, out var existing))
        {
            return existing;
        }

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = false,
            UseCustomData = false,
            Mesh = FactoryTransportVisualFactory.GetSharedMesh(descriptor),
            InstanceCount = 1,
            VisibleInstanceCount = 0
        };

        var instance = new MultiMeshInstance3D
        {
            Name = $"TransportBatch_{_buckets.Count}",
            Multimesh = multiMesh,
            MaterialOverride = FactoryTransportVisualFactory.GetSharedMaterial(descriptor),
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        AddChild(instance);
        var bucket = new BatchBucket
        {
            Descriptor = descriptor,
            MultiMesh = multiMesh,
            Instance = instance
        };
        _buckets[descriptor.BatchKey] = bucket;
        return bucket;
    }

    private void EnsureCapacity(BatchBucket bucket, int required)
    {
        if (bucket.MultiMesh.InstanceCount >= required)
        {
            return;
        }

        var newCount = Mathf.Max(required, bucket.MultiMesh.InstanceCount * 2);
        bucket.MultiMesh.InstanceCount = newCount;
    }

    private FactoryTransportRenderTier ResolveTier(Vector3 worldPosition)
    {
        var distance = new Vector2(worldPosition.X, worldPosition.Z)
            .DistanceTo(new Vector2(_cameraWorldPosition.X, _cameraWorldPosition.Z));
        if (distance <= NearDistanceCells * FactoryConstants.CellSize)
        {
            return FactoryTransportRenderTier.Near;
        }

        if (distance <= MidDistanceCells * FactoryConstants.CellSize)
        {
            return FactoryTransportRenderTier.Mid;
        }

        return FactoryTransportRenderTier.Far;
    }

    private bool IsInsidePaddedRect(Vector2I cell)
    {
        var minX = _visibleRect.Position.X - _visiblePaddingCells;
        var minY = _visibleRect.Position.Y - _visiblePaddingCells;
        var maxX = _visibleRect.End.X + _visiblePaddingCells;
        var maxY = _visibleRect.End.Y + _visiblePaddingCells;
        return cell.X >= minX && cell.X < maxX && cell.Y >= minY && cell.Y < maxY;
    }
}
