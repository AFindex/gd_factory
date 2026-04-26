using Godot;
using System;
using System.Collections.Generic;

public enum MobileFactoryHeavyHandoffPhase
{
    Idle,
    WaitingWorldCargo,
    ReceivingFromWorld,
    BufferedOuter,
    SlidingToBridgeInward,
    BridgingInward,
    BufferedInner,
    WaitingForUnpacker,
    WaitingForPacker,
    ReceivingFromPacker,
    SlidingToBridgeOutward,
    BridgingOutward,
    WaitingWorldPickup,
    ReleasingToWorld
}

public enum MobileFactoryHeavyPortTransferMode
{
    None,
    WorldToOuterBuffer,
    OuterToInnerBuffer,
    InnerToOuterBuffer,
    OuterToWorld
}

public enum MobileFactoryHeavyCargoPresentationOwner
{
    None,
    HeavyPort,
    CargoUnpacker,
    CargoPacker
}

public enum MobileFactoryHeavyCargoPresentationHost
{
    None,
    WorldRouteHandoff,
    WorldOuterBuffer,
    InteriorBridge,
    InteriorInnerBuffer,
    ConverterStaging,
    ConverterProcessing,
    ConverterDispatch
}

public readonly struct MobileFactoryHeavyCargoPresentationState
{
    public MobileFactoryHeavyCargoPresentationState(
        FactoryItem item,
        MobileFactoryHeavyCargoPresentationOwner owner,
        MobileFactoryHeavyCargoPresentationHost host,
        MobileFactoryHeavyHandoffPhase phase,
        float progress)
    {
        Item = item;
        Owner = owner;
        Host = host;
        Phase = phase;
        Progress = Mathf.Clamp(progress, 0.0f, 1.0f);
    }

    public FactoryItem Item { get; }
    public MobileFactoryHeavyCargoPresentationOwner Owner { get; }
    public MobileFactoryHeavyCargoPresentationHost Host { get; }
    public MobileFactoryHeavyHandoffPhase Phase { get; }
    public float Progress { get; }
}

public abstract partial class MobileFactoryBoundaryAttachmentStructure : FlowTransportStructure
{
    private GridManager? _worldSite;
    private MobileFactoryAttachmentProjection? _projection;
    private GridManager? _deploymentWorldSite;
    private MobileFactoryAttachmentProjection? _deploymentProjection;
    private bool _worldVisualReady;

    public abstract MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition { get; }
    public MobileFactoryAttachmentChannelType ChannelType => AttachmentDefinition.ChannelType;
    public bool IsConnectedToWorld => _worldSite is not null && _projection is not null;
    public bool IsWorldFlowReady => IsConnectedToWorld && _worldVisualReady;
    public GridManager? BoundWorldSite => _worldSite;
    public MobileFactoryAttachmentProjection? Projection => _projection;
    public GridManager? DeploymentWorldSite => _deploymentWorldSite;
    public MobileFactoryAttachmentProjection? DeploymentProjection => _deploymentProjection;
    public bool HasDeploymentProjection => _deploymentWorldSite is not null && _deploymentProjection is not null;
    public Vector2I WorldPortCell => _projection?.WorldPortCell ?? Vector2I.Zero;
    public Vector2I WorldAdjacentCell => _projection?.WorldAdjacentCell ?? Vector2I.Zero;
    public FacingDirection WorldFacing => _projection?.WorldFacing ?? Facing;
    public virtual int StagedCargoCount => TransitItemCount;
    public virtual string ConnectionStateLabel => IsConnectedToWorld ? (IsWorldFlowReady ? "已连接" : "展开中") : TransitItemCount > 0 ? "阻塞" : "未连接";
    public virtual float WorldVisualAnimationDurationSeconds => 0.26f;

    public virtual bool CanBindToWorld(GridManager worldSite, MobileFactoryAttachmentProjection projection, out string reason)
    {
        reason = string.Empty;
        return true;
    }

    public virtual MobileFactoryAttachmentDeploymentEvaluation EvaluateDeployment(GridManager worldSite, MobileFactoryAttachmentProjection projection)
    {
        var previewWorldCells = new List<Vector2I>(projection.WorldCells);
        if (!CanBindToWorld(worldSite, projection, out var reason))
        {
            return new MobileFactoryAttachmentDeploymentEvaluation(
                this,
                projection,
                MobileFactoryAttachmentDeployState.Blocked,
                previewWorldCells,
                new List<Vector2I>(),
                new List<Vector2I>(),
                reason);
        }

        return new MobileFactoryAttachmentDeploymentEvaluation(
            this,
            projection,
            MobileFactoryAttachmentDeployState.Connected,
            previewWorldCells,
            new List<Vector2I>(GetReservedWorldCells(worldSite, projection)),
            previewWorldCells,
            string.Empty);
    }

    public void BindToWorld(GridManager worldSite, MobileFactoryAttachmentProjection projection)
    {
        RecordDeploymentContext(worldSite, projection);
        _worldSite = worldSite;
        _projection = projection;
        _worldVisualReady = false;
        OnWorldBindingChanged();
    }

    public void ClearBinding()
    {
        _worldSite = null;
        _projection = null;
        _worldVisualReady = false;
        OnWorldBindingChanged();
    }

    public void RecordDeploymentContext(GridManager worldSite, MobileFactoryAttachmentProjection projection)
    {
        _deploymentWorldSite = worldSite;
        _deploymentProjection = projection;
        OnDeploymentContextChanged();
    }

    public void ClearDeploymentContext()
    {
        _deploymentWorldSite = null;
        _deploymentProjection = null;
        OnDeploymentContextChanged();
    }

    public void UpdateWorldVisualReadiness(float progress, float target)
    {
        _worldVisualReady = IsConnectedToWorld && target >= 0.999f && progress >= 0.999f;
    }

    public override string Description => AttachmentDefinition.Description;

    public virtual void OnDeploymentActivated(
        Node3D worldStructureRoot,
        SimulationController simulation,
        GridManager worldGrid,
        MobileFactoryAttachmentDeploymentEvaluation evaluation)
    {
    }

    public virtual void OnDeploymentCleared(Node3D worldStructureRoot, SimulationController simulation)
    {
    }

    public virtual void BuildWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
    }

    public virtual void ConfigureWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
    }

    public virtual IReadOnlyList<Vector2I> GetReservedWorldCells(GridManager worldSite, MobileFactoryAttachmentProjection projection)
    {
        return projection.WorldCells;
    }

    public virtual IReadOnlyList<Vector2I> GetActivationReservedWorldCells(MobileFactoryAttachmentDeploymentEvaluation evaluation)
    {
        return evaluation.ReservedWorldCells;
    }

    public virtual void ApplyWorldPayloadVisualProgress(Node3D root, float progress)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not Node3D payloadRoot)
        {
            return;
        }

        var targetPosition = payloadRoot.GetMeta("payload_target_position", Vector3.Zero).AsVector3();
        var revealThreshold = Mathf.Clamp(payloadRoot.GetMeta("payload_reveal_threshold", 0.0f).AsSingle(), 0.0f, 0.98f);
        var revealProgress = Mathf.Clamp((progress - revealThreshold) / Mathf.Max(0.001f, 1.0f - revealThreshold), 0.0f, 1.0f);
        var preserveScale = payloadRoot.GetMeta("payload_preserve_scale", false).AsBool();
        payloadRoot.Position = targetPosition * progress;
        payloadRoot.Scale = preserveScale
            ? Vector3.One
            : Vector3.One * Mathf.Max(0.001f, revealProgress);
        payloadRoot.Visible = revealProgress > 0.001f;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return Kind is BuildPrototypeKind.InputPort or BuildPrototypeKind.OutputPort
            ? "接口规格：双格重载交接舱"
            : "接口规格：重载交接舱";
        yield return $"物流方向：{(ChannelType == MobileFactoryAttachmentChannelType.ItemInput ? "世界 -> 舱内" : "舱内 -> 世界")}";
        yield return $"连接状态：{ConnectionStateLabel}";
        if (IsConnectedToWorld)
        {
            yield return $"世界挂点：({WorldPortCell.X}, {WorldPortCell.Y}) / 朝向 {FactoryDirection.ToLabel(WorldFacing)}";
        }
    }

    public virtual Vector3 GetWorldConnectorEndWorld(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        var cellCenter = worldGrid.CellToWorld(projection.WorldPortCell);
        var facing = FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(projection.WorldFacing));
        return cellCenter - facing * (worldGrid.CellSize * 0.18f);
    }

    protected override void BuildVisuals()
    {
        var deckWidth = GetPortDeckWidth();
        var baseColor = AttachmentDefinition.Tint.Darkened(0.2f);
        var accentColor = AttachmentDefinition.Tint;
        var tipColor = AttachmentDefinition.ConnectorColor;
        var deckDepth = GetPortDeckDepth();

        CreateColoredBox("BoundaryBaseSkid", new Vector3(deckWidth, 0.12f, deckDepth), baseColor, new Vector3(0.0f, 0.06f, 0.0f));
        CreateColoredBox("BoundaryDeck", new Vector3(deckWidth * 0.90f, 0.08f, deckDepth * 0.90f), baseColor.Lightened(0.06f), new Vector3(0.02f * CellSize, 0.12f, 0.0f));
        CreateColoredBox("BoundaryHandoffCradle", new Vector3(deckWidth * 0.82f, 0.10f, deckDepth * 0.58f), accentColor.Darkened(0.06f), new Vector3(0.06f * CellSize, 0.18f, 0.0f));
        CreateInteriorTray(this, "BoundaryTransferLane", new Vector3(deckWidth * 0.84f, 0.08f, CellSize * 0.34f), accentColor, tipColor.Lightened(0.16f), new Vector3(0.08f * CellSize, 0.20f, 0.0f));
        CreateColoredBox("BoundaryDeckRailNorth", new Vector3(deckWidth * 0.74f, 0.10f, CellSize * 0.10f), tipColor.Lightened(0.12f), new Vector3(0.06f * CellSize, 0.24f, 0.0f));
        CreateColoredBox("BoundaryPortalNorth", new Vector3(CellSize * 0.18f, 0.44f, CellSize * 0.16f), tipColor, new Vector3(deckWidth * 0.40f, 0.32f, 0.0f));
        CreateColoredBox("HullMouth", new Vector3(deckWidth * 0.28f, 0.14f, deckDepth * 0.48f), tipColor.Lightened(0.04f), new Vector3(deckWidth * 0.48f, 0.24f, 0.0f));
        CreateColoredBox("BoundaryScaleMarker", new Vector3(CellSize * 0.30f, 0.06f, CellSize * 0.30f), tipColor.Lightened(0.18f), new Vector3(-CellSize * 0.40f, 0.14f, 0.0f));
        CreateInteriorLabelPlate(this, "BoundaryScaleLabel", "重载", tipColor, new Vector3(-deckWidth * 0.10f, 0.12f, -deckDepth * 0.38f), 1.18f);
        CreateInteriorIndicatorLight(this, "Beacon", tipColor.Lightened(0.22f), new Vector3(-deckWidth * 0.30f, 0.40f, 0.0f), CellSize * 0.07f);
    }

    protected Vector3 EvaluatePortPath(float progress, bool worldToInterior)
    {
        var outer = new Vector3(CellSize * 0.96f, ItemHeight + 0.10f, 0.0f);
        var throat = new Vector3(CellSize * 0.26f, ItemHeight + 0.08f, 0.0f);
        var inner = new Vector3(-CellSize * 0.72f, ItemHeight + 0.02f, 0.0f);
        if (progress <= 0.45f)
        {
            var approach = progress / 0.45f;
            return worldToInterior
                ? outer.Lerp(throat, approach)
                : inner.Lerp(throat, approach);
        }

        var travel = (progress - 0.45f) / 0.55f;
        return worldToInterior
            ? throat.Lerp(inner, travel)
            : throat.Lerp(outer, travel);
    }

    protected virtual void OnWorldBindingChanged()
    {
    }

    protected virtual void OnDeploymentContextChanged()
    {
    }

    protected void BuildStandardPortWorldPayload(Node3D root)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not null)
        {
            return;
        }

        var payloadRoot = new Node3D { Name = "WorldPayloadRoot" };
        payloadRoot.SetMeta("payload_preserve_scale", true);
        payloadRoot.SetMeta("payload_reveal_threshold", 0.56f);
        root.AddChild(payloadRoot);

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadPad",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.94f, 0.10f, FactoryConstants.CellSize * 0.94f) },
            Position = new Vector3(0.0f, 0.05f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint.Darkened(0.22f),
                Roughness = 0.84f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadCradle",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.74f, 0.08f, FactoryConstants.CellSize * 0.62f) },
            Position = new Vector3(0.02f * FactoryConstants.CellSize, 0.13f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint,
                Roughness = 0.72f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadGuideNorth",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.72f, 0.06f, FactoryConstants.CellSize * 0.06f) },
            Position = new Vector3(0.02f * FactoryConstants.CellSize, 0.18f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint.Lightened(0.04f),
                Roughness = 0.66f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadPortalNorth",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.12f, 0.32f, FactoryConstants.CellSize * 0.18f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.42f, 0.22f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor,
                Roughness = 0.54f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadIndicator",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.14f, 0.10f, FactoryConstants.CellSize * 0.14f) },
            Position = new Vector3(-FactoryConstants.CellSize * 0.30f, 0.42f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor.Lightened(0.24f),
                Roughness = 0.42f
            }
        });

        var transitPayloadRoot = new Node3D
        {
            Name = "ConnectorTransitPayloadRoot",
            Visible = false
        };
        transitPayloadRoot.AddChild(new Node3D
        {
            Name = "TransitPayloadAnchor",
            Position = new Vector3(0.0f, 0.34f, 0.0f)
        });
        root.AddChild(transitPayloadRoot);

        var outerBufferPayloadRoot = new Node3D
        {
            Name = "OuterBufferPayloadRoot",
            Visible = false
        };
        outerBufferPayloadRoot.AddChild(new Node3D
        {
            Name = "OuterBufferPayloadAnchor",
            Position = new Vector3(0.0f, 0.34f, 0.0f)
        });
        if (payloadRoot.GetNodeOrNull<Node3D>("PayloadBufferAnchor") is null)
        {
            payloadRoot.AddChild(new Node3D
            {
                Name = "PayloadBufferAnchor",
                Position = new Vector3(0.0f, 0.0f, 0.0f)
            });
        }
        payloadRoot.AddChild(outerBufferPayloadRoot);
    }

    protected void ConfigureStandardPortWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not Node3D payloadRoot)
        {
            return;
        }

        var payloadCenterWorld = worldGrid.CellToWorld(projection.WorldPortCell) + new Vector3(0.0f, 0.04f, 0.0f);
        var payloadLocalPosition = root.ToLocal(payloadCenterWorld);
        payloadRoot.SetMeta("payload_target_position", payloadLocalPosition);
        payloadRoot.Position = payloadLocalPosition;
        payloadRoot.Rotation = new Vector3(
            0.0f,
            FactoryDirection.ToYRotationRadians(projection.WorldFacing) - root.Rotation.Y,
            0.0f);
        root.SetMeta("world_route_yaw", payloadRoot.Rotation.Y + Mathf.Pi);
        var portCenterWorld = worldGrid.CellToWorld(projection.WorldPortCell);
        var adjacentCenterWorld = worldGrid.CellToWorld(projection.WorldAdjacentCell);
        var handoffDirection = (portCenterWorld - adjacentCenterWorld).Normalized();
        var beltContactWorld = adjacentCenterWorld + (handoffDirection * (worldGrid.CellSize * 0.5f)) + new Vector3(0.0f, 0.36f, 0.0f);
        var bridgeCacheWorld = GetStandardPortConnectorEndWorld(worldGrid, projection) + new Vector3(0.0f, 0.34f, 0.0f);
        root.SetMeta("world_route_start_position", root.ToLocal(beltContactWorld));
        root.SetMeta("world_outer_buffer_position", root.ToLocal(bridgeCacheWorld));

        var bufferAnchorWorld = payloadRoot.GetNodeOrNull<Node3D>("PayloadBufferAnchor");
        if (bufferAnchorWorld is not null)
        {
            bufferAnchorWorld.Position = payloadRoot.ToLocal(bridgeCacheWorld);
        }

        if (payloadRoot.GetNodeOrNull<Node3D>("OuterBufferPayloadRoot/OuterBufferPayloadAnchor") is Node3D outerBufferAnchor)
        {
            outerBufferAnchor.Position = payloadRoot.ToLocal(bridgeCacheWorld);
            outerBufferAnchor.Rotation = Vector3.Zero;
        }

        payloadRoot.Visible = true;
    }

    protected Vector3 GetStandardPortConnectorEndWorld(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        var cellCenter = worldGrid.CellToWorld(projection.WorldPortCell);
        var facing = FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(projection.WorldFacing));
        return cellCenter - facing * (worldGrid.CellSize * 0.16f) + new Vector3(0.0f, 0.02f, 0.0f);
    }

    protected float GetPortDeckWidth()
    {
        return Mathf.Max(CellSize * 1.28f, Footprint.GetPreviewSize(CellSize, Facing).X * 0.90f);
    }

    protected float GetPortDeckDepth()
    {
        return Mathf.Max(CellSize * 1.86f, Footprint.GetPreviewSize(CellSize, Facing).Y * 0.94f);
    }

    protected TransitItemState? GetActiveTransitState()
    {
        return TransitItems.Count > 0 ? TransitItems[0] : null;
    }

    protected bool HasActiveTransitState()
    {
        return TransitItems.Count > 0;
    }

    protected TransitItemState SpawnTransitState(
        FactoryItem item,
        Vector2I sourceCell,
        Vector2I targetCell,
        FactoryTransportVisualContext visualContext = FactoryTransportVisualContext.BoundaryHandoff)
    {
        var renderDescriptors = FactoryTransportVisualFactory.ResolveDescriptorSet(item, CellSize, visualContext);
        var state = new TransitItemState(item, renderDescriptors, sourceCell, targetCell)
        {
            LaneKey = 0,
            Position = 0.0f,
            PreviousPosition = 0.0f,
            OccupiedLengthProgress = FactoryTransportVisualFactory.EstimateOccupiedLengthProgress(renderDescriptors, CellSize)
        };
        TransitItems.Add(state);
        return state;
    }

    protected void ApplyStandardPortConnectorTransit(Node3D root, float deploymentProgress, bool worldToInterior)
    {
        if (root.GetNodeOrNull<Node3D>("ConnectorTransitPayloadRoot") is not Node3D transitRoot
            || transitRoot.GetNodeOrNull<Node3D>("TransitPayloadAnchor") is not Node3D transitAnchor)
        {
            return;
        }

        if (deploymentProgress < 0.92f
            || !TrySelectConnectorTransitItem(worldToInterior, out var transitItem, out var transitProgress)
            || transitItem is null)
        {
            ClearTransitPayloadVisual(transitAnchor);
            transitRoot.Visible = false;
            return;
        }

        SyncTransitPayloadVisual(transitAnchor, transitItem);

        var fullLength = root.GetMeta("full_length", 0.0f).AsSingle();
        var mouthExtension = root.GetMeta("mouth_extension", 0.14f).AsSingle();
        var localProgress = worldToInterior
            ? Mathf.Clamp(transitProgress / 0.58f, 0.0f, 1.0f)
            : Mathf.Clamp((transitProgress - 0.42f) / 0.58f, 0.0f, 1.0f);
        var travelStart = worldToInterior
            ? fullLength + (mouthExtension * 0.44f)
            : 0.14f;
        var travelEnd = worldToInterior
            ? 0.14f
            : fullLength + (mouthExtension * 0.44f);
        var z = Mathf.Lerp(travelStart, travelEnd, localProgress);
        transitAnchor.Position = new Vector3(0.0f, 0.36f, z);
        transitRoot.Visible = true;
    }

    protected static void ConfigurePayloadReveal(Node3D node, float start, float end, string mode = "uniform")
    {
        node.SetMeta("payload_reveal_start", start);
        node.SetMeta("payload_reveal_end", end);
        node.SetMeta("payload_reveal_mode", mode);
        node.SetMeta("payload_reveal_base_position", node.Position);
        node.SetMeta("payload_reveal_base_scale", node.Scale);
    }

    protected static void ApplyPayloadRevealTree(Node3D root, float progress)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Node3D childNode)
            {
                ApplyPayloadRevealNode(childNode, progress);
            }
        }
    }

    private static void ApplyPayloadRevealNode(Node3D node, float progress)
    {
        if (node.HasMeta("payload_reveal_start") || node.HasMeta("payload_reveal_end"))
        {
            var start = node.GetMeta("payload_reveal_start", 0.0f).AsSingle();
            var end = node.GetMeta("payload_reveal_end", 1.0f).AsSingle();
            var mode = node.GetMeta("payload_reveal_mode", "uniform").AsString();
            var basePosition = node.GetMeta("payload_reveal_base_position", node.Position).AsVector3();
            var baseScale = node.GetMeta("payload_reveal_base_scale", node.Scale).AsVector3();
            var localProgress = end <= start
                ? (progress >= end ? 1.0f : 0.0f)
                : Mathf.Clamp((progress - start) / (end - start), 0.0f, 1.0f);

            node.Visible = localProgress > 0.001f;

            switch (mode)
            {
                case "depth":
                    node.Position = basePosition;
                    node.Scale = new Vector3(baseScale.X, baseScale.Y, Mathf.Max(0.001f, baseScale.Z * localProgress));
                    break;
                case "vertical":
                    node.Position = new Vector3(basePosition.X, basePosition.Y * localProgress, basePosition.Z);
                    node.Scale = new Vector3(baseScale.X, Mathf.Max(0.001f, baseScale.Y * localProgress), baseScale.Z);
                    break;
                default:
                    node.Position = basePosition;
                    node.Scale = baseScale * Mathf.Max(0.001f, localProgress);
                    break;
            }
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node3D childNode)
            {
                ApplyPayloadRevealNode(childNode, progress);
            }
        }
    }

    private bool TrySelectConnectorTransitItem(bool worldToInterior, out FactoryItem? item, out float transitProgress)
    {
        item = null;
        transitProgress = 0.0f;
        var selectedProgress = worldToInterior ? float.MaxValue : float.MinValue;
        var found = false;

        for (var index = 0; index < TransitItems.Count; index++)
        {
            var candidate = TransitItems[index];
            var isVisibleInConnector = worldToInterior
                ? candidate.Position <= 0.58f
                : candidate.Position >= 0.42f;
            if (!isVisibleInConnector)
            {
                continue;
            }

            if (worldToInterior)
            {
                if (candidate.Position >= selectedProgress)
                {
                    continue;
                }
            }
            else if (candidate.Position <= selectedProgress)
            {
                continue;
            }

            selectedProgress = candidate.Position;
            transitProgress = candidate.Position;
            item = candidate.Item;
            found = true;
        }

        return found;
    }

    protected static void SyncTransitPayloadVisual(Node3D anchor, FactoryItem item)
    {
        var visualKey = $"{item.ItemKind}:{item.CargoForm}:{item.BundleTemplateId}:{FactoryTransportVisualContext.BoundaryHandoff}";
        var currentKey = anchor.GetMeta("transit_visual_key", string.Empty).AsString();
        var visual = anchor.GetNodeOrNull<Node3D>("TransitPayloadVisual");
        if (visual is null || !string.Equals(currentKey, visualKey, StringComparison.Ordinal))
        {
            RemoveTransitPayloadVisual(anchor, visual);
            visual = FactoryTransportVisualFactory.CreateVisual(item, FactoryConstants.CellSize, FactoryTransportVisualContext.BoundaryHandoff);
            visual.Name = "TransitPayloadVisual";
            anchor.AddChild(visual);
            anchor.SetMeta("transit_visual_key", visualKey);
        }

        visual.Visible = true;
    }

    protected static void SetTransitPayloadVisualLocalYaw(Node3D anchor, float yaw)
    {
        if (anchor.GetNodeOrNull<Node3D>("TransitPayloadVisual") is not Node3D visual)
        {
            return;
        }

        visual.Rotation = new Vector3(0.0f, yaw, 0.0f);
    }

    protected static void ClearTransitPayloadVisual(Node3D anchor)
    {
        RemoveTransitPayloadVisual(anchor, anchor.GetNodeOrNull<Node3D>("TransitPayloadVisual"));
        anchor.SetMeta("transit_visual_key", string.Empty);
    }

    private static void RemoveTransitPayloadVisual(Node3D anchor, Node3D? visual)
    {
        if (visual is null)
        {
            return;
        }

        visual.Visible = false;
        if (visual.GetParent() == anchor)
        {
            anchor.RemoveChild(visual);
        }

        visual.QueueFree();
    }
}

public abstract partial class MobileFactoryHeavyPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    protected const float BridgeAlignmentProgress = 0.48f;
    protected const float BufferSettleSeconds = 0.16f;
    private const float WorldTransitPayloadHeight = 0.36f;
    protected const float OuterToInnerRotateInEndProgress = 0.18f;
    protected const float OuterToInnerWorldMoveEndProgress = BridgeAlignmentProgress;
    protected const float OuterToInnerInteriorRotateEndProgress = 0.68f;
    protected const float InnerToOuterRotateOutEndProgress = 0.18f;
    protected const float InnerToOuterWorldMoveEndProgress = 0.82f;

    private FactoryItem? _outerBufferedItem;
    private FactoryItem? _innerBufferedItem;
    private FactoryItem? _manualTransferItem;
    private Vector2I _manualTransferSourceCell;
    private Vector2I _manualTransferTargetCell;
    private float _manualTransferProgress;
    private MobileFactoryHeavyPortTransferMode _transferMode;
    private MobileFactoryHeavyHandoffPhase _handoffPhase = MobileFactoryHeavyHandoffPhase.Idle;
    private float _handoffPhaseProgress;
    private Node3D? _activeWorldConnectorRoot;
    protected float BufferSettleTimer;

    protected abstract bool IsInboundHandoff { get; }
    protected virtual bool EnableHeavyCargoPresentation => true;
    protected virtual bool PreferOuterBufferedPresentationWhenIdle => false;
    protected virtual bool ShouldRenderHeavyCargoHost(MobileFactoryHeavyCargoPresentationHost host)
    {
        return EnableHeavyCargoPresentation;
    }

    public override int StagedCargoCount => (_outerBufferedItem is null ? 0 : 1)
        + (_innerBufferedItem is null ? 0 : 1)
        + (HasActiveTransitState() ? 1 : 0)
        + (HasManualTransferState() ? 1 : 0);
    public FactoryItem? OuterBufferedItem => _outerBufferedItem;
    public FactoryItem? InnerBufferedItem => _innerBufferedItem;
    public MobileFactoryHeavyPortTransferMode TransferMode => _transferMode;
    public MobileFactoryHeavyHandoffPhase HandoffPhase => _handoffPhase;
    public float HandoffPhaseProgress => _handoffPhaseProgress;
    public bool HasBridgeTransfer => HasActiveTransitState() || HasManualTransferState();
    public float BridgeTransferProgress => GetActiveTransitState()?.Position
        ?? (HasManualTransferState() ? _manualTransferProgress : 0.0f);
    public override string ConnectionStateLabel => !IsConnectedToWorld
        ? (StagedCargoCount > 0 ? "离线滞留" : "未连接")
        : !IsWorldFlowReady
            ? "展开中"
            : $"已连接 / {DescribePhase(_handoffPhase)}{DescribePresentationSuffix()}";

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"重载阶段：{DescribePhase(_handoffPhase)}";
        yield return $"显示所有权：{DescribePresentationOwnership()}";
        yield return $"外缓存：{DescribeBufferedItem(_outerBufferedItem)}";
        yield return $"桥接位：{DescribeBridgeState()}";
        yield return $"内缓存：{DescribeBufferedItem(_innerBufferedItem)}";
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        base.SimulationStep(simulation, stepSeconds);
        AdvanceManualTransfer(simulation, stepSeconds);
        BufferSettleTimer = Mathf.Max(0.0f, BufferSettleTimer - (float)stepSeconds);
        if (BufferSettleTimer <= 0.0f)
        {
            AdvanceHeavyHandoff(simulation);
        }
        RefreshHandoffPhase(simulation);
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        PurgeTransitLegacyVisuals();
        if (!EnableHeavyCargoPresentation)
        {
            ClearInteriorPresentationVisual();
            if (GetNodeOrNull<MeshInstance3D>("Beacon") is MeshInstance3D beacon)
            {
                beacon.Scale = Vector3.One;
            }

            return;
        }

        UpdateInteriorPresentationVisual();
        UpdateBeaconPulse();
    }

    public override void BuildWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        BuildStandardPortWorldPayload(root);
    }

    public override void ConfigureWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        ConfigureStandardPortWorldPayload(root, worldGrid, projection);
    }

    public override Vector3 GetWorldConnectorEndWorld(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        return GetStandardPortConnectorEndWorld(worldGrid, projection);
    }

    public override void ApplyWorldPayloadVisualProgress(Node3D root, float progress)
    {
        _activeWorldConnectorRoot = root;
        base.ApplyWorldPayloadVisualProgress(root, progress);

        if (!EnableHeavyCargoPresentation)
        {
            if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot/OuterBufferPayloadRoot") is Node3D disabledOuterBufferRoot
                && disabledOuterBufferRoot.GetNodeOrNull<Node3D>("OuterBufferPayloadAnchor") is Node3D disabledOuterBufferAnchor)
            {
                ClearTransitPayloadVisual(disabledOuterBufferAnchor);
                disabledOuterBufferRoot.Visible = false;
            }

            if (root.GetNodeOrNull<Node3D>("ConnectorTransitPayloadRoot") is Node3D disabledTransitRoot
                && disabledTransitRoot.GetNodeOrNull<Node3D>("TransitPayloadAnchor") is Node3D disabledTransitAnchor)
            {
                ClearTransitPayloadVisual(disabledTransitAnchor);
                disabledTransitRoot.Visible = false;
            }

            return;
        }

        var hasActivePresentation = TryResolveCurrentPresentation(out var activePresentation);

        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot/OuterBufferPayloadRoot") is Node3D outerBufferRoot
            && outerBufferRoot.GetNodeOrNull<Node3D>("OuterBufferPayloadAnchor") is Node3D outerBufferAnchor)
        {
            var showOuterBuffer = false;
            MobileFactoryHeavyCargoPresentationState outerBufferPresentation = default;
            if (progress >= 0.92f
                && ShouldRenderHeavyCargoHost(MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer))
            {
                if (hasActivePresentation
                    && activePresentation.Host == MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer)
                {
                    outerBufferPresentation = activePresentation;
                    showOuterBuffer = true;
                }
                else if (TryBuildBufferedPresentation(_outerBufferedItem, MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer, out outerBufferPresentation))
                {
                    showOuterBuffer = true;
                }
            }

            if (!showOuterBuffer)
            {
                ClearTransitPayloadVisual(outerBufferAnchor);
                outerBufferRoot.Visible = false;
            }
            else
            {
                SyncTransitPayloadVisual(outerBufferAnchor, outerBufferPresentation.Item);
                outerBufferAnchor.Rotation = Vector3.Zero;
                SetTransitPayloadVisualLocalYaw(outerBufferAnchor, ResolveWorldOuterBufferVisualYaw(root, outerBufferAnchor, outerBufferPresentation));
                outerBufferRoot.Visible = true;
            }
        }

        if (root.GetNodeOrNull<Node3D>("ConnectorTransitPayloadRoot") is not Node3D transitRoot
            || transitRoot.GetNodeOrNull<Node3D>("TransitPayloadAnchor") is not Node3D transitAnchor
            || progress < 0.92f
            || !hasActivePresentation
            || !ShouldRenderHeavyCargoHost(MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff)
            || activePresentation.Host != MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff
            || !TryResolveWorldTransitPose(root, activePresentation, out var worldTransitPosition))
        {
            if (root.GetNodeOrNull<Node3D>("ConnectorTransitPayloadRoot") is Node3D staleTransitRoot
                && staleTransitRoot.GetNodeOrNull<Node3D>("TransitPayloadAnchor") is Node3D staleTransitAnchor)
            {
                ClearTransitPayloadVisual(staleTransitAnchor);
                staleTransitRoot.Visible = false;
            }

            return;
        }

        SyncTransitPayloadVisual(transitAnchor, activePresentation.Item);
        transitAnchor.Position = worldTransitPosition;
        transitAnchor.Rotation = Vector3.Zero;
        SetTransitPayloadVisualLocalYaw(transitAnchor, ResolveWorldTransitVisualYaw(root, transitAnchor, activePresentation));
        transitRoot.Visible = true;
    }

    protected void BuildHeavyPortAnchors(Color accentColor)
    {
        var deckWidth = GetPortDeckWidth();
        var deckDepth = GetPortDeckDepth();
        CreateColoredBox("BridgeGuideWest", new Vector3(CellSize * 0.08f, 0.18f, deckDepth * 0.48f), accentColor.Darkened(0.18f), new Vector3(CellSize * 0.18f, 0.20f, -deckDepth * 0.18f));
        CreateColoredBox("BridgeGuideEast", new Vector3(CellSize * 0.08f, 0.18f, deckDepth * 0.48f), accentColor.Darkened(0.18f), new Vector3(-CellSize * 0.04f, 0.20f, deckDepth * 0.18f));
        CreateColoredBox("InnerBufferDeck", new Vector3(CellSize * 0.72f, 0.08f, deckDepth * 0.32f), accentColor.Darkened(0.08f), new Vector3(-CellSize * 0.74f, 0.18f, 0.0f));
        CreateColoredBox("ConverterHandoffPad", new Vector3(CellSize * 0.22f, 0.10f, CellSize * 0.42f), accentColor.Lightened(0.08f), new Vector3(-deckWidth * 0.46f, 0.22f, 0.0f));
        var bridgePayloadAnchor = new Node3D
        {
            Name = "BridgePayloadAnchor",
            Position = new Vector3(CellSize * 0.45f, ItemHeight + 0.02f, 0.0f)
        };
        bridgePayloadAnchor.SetMeta("default_local_position", bridgePayloadAnchor.Position);
        AddChild(bridgePayloadAnchor);

        var innerBufferPayloadAnchor = new Node3D
        {
            Name = "InnerBufferPayloadAnchor",
            Position = new Vector3(-CellSize * 0.74f, ItemHeight + 0.01f, 0.0f)
        };
        innerBufferPayloadAnchor.SetMeta("default_local_position", innerBufferPayloadAnchor.Position);
        AddChild(innerBufferPayloadAnchor);

        var converterHandoffAnchor = new Node3D
        {
            Name = "ConverterHandoffAnchor",
            Position = new Vector3(-deckWidth * 0.46f, ItemHeight + 0.04f, 0.0f)
        };
        converterHandoffAnchor.SetMeta("default_local_position", converterHandoffAnchor.Position);
        AddChild(converterHandoffAnchor);
    }

    protected void SetInnerBufferedItem(FactoryItem? item)
    {
        _innerBufferedItem = item;
    }

    protected void SetOuterBufferedItem(FactoryItem? item)
    {
        _outerBufferedItem = item;
    }

    protected FactoryItem? TakeInnerBufferedItem()
    {
        var item = _innerBufferedItem;
        _innerBufferedItem = null;
        return item;
    }

    protected FactoryItem? TakeOuterBufferedItem()
    {
        var item = _outerBufferedItem;
        _outerBufferedItem = null;
        return item;
    }

    protected bool BeginTransfer(MobileFactoryHeavyPortTransferMode mode, FactoryItem item, Vector2I sourceCell, Vector2I targetCell)
    {
        if (HasBridgeTransfer)
        {
            return false;
        }

        _transferMode = mode;
        if (mode == MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer)
        {
            SpawnTransitState(item, sourceCell, targetCell);
        }
        else
        {
            _manualTransferItem = item;
            _manualTransferSourceCell = sourceCell;
            _manualTransferTargetCell = targetCell;
            _manualTransferProgress = 0.0f;
        }

        return true;
    }

    protected override void OnTransitItemAccepted(TransitItemState state)
    {
        if (state.LegacyVisual is not null)
        {
            state.LegacyVisual.QueueFree();
            state.LegacyVisual = null;
        }

        if (IsInboundHandoff && state.SourceCell == WorldAdjacentCell)
        {
            _transferMode = MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer;
            return;
        }


    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer:
                if (_outerBufferedItem is not null)
                {
                    return false;
                }

                _outerBufferedItem = state.Item;
                BufferSettleTimer = BufferSettleSeconds;
                _transferMode = MobileFactoryHeavyPortTransferMode.None;
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                if (_innerBufferedItem is not null)
                {
                    return false;
                }

                _innerBufferedItem = state.Item;
                BufferSettleTimer = BufferSettleSeconds;
                _transferMode = MobileFactoryHeavyPortTransferMode.None;
                return true;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (_outerBufferedItem is not null)
                {
                    return false;
                }

                _outerBufferedItem = state.Item;
                BufferSettleTimer = BufferSettleSeconds;
                _transferMode = MobileFactoryHeavyPortTransferMode.None;
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToWorld:
                if (!TryDispatchOuterToWorld(state.Item, simulation))
                {
                    return false;
                }

                _transferMode = MobileFactoryHeavyPortTransferMode.None;
                return true;
            default:
                return false;
        }
    }

    protected void SetHandoffPhase(MobileFactoryHeavyHandoffPhase phase, float progress = 0.0f)
    {
        _handoffPhase = phase;
        _handoffPhaseProgress = Mathf.Clamp(progress, 0.0f, 1.0f);
    }

    protected bool TryResolveActiveTransitItem(out FactoryItem? item, out float progress)
    {
        var state = GetActiveTransitState();
        if (state is not null)
        {
            item = state.Item;
            progress = state.Position;
            return true;
        }

        if (_manualTransferItem is not null)
        {
            item = _manualTransferItem;
            progress = _manualTransferProgress;
            return true;
        }

        item = null;
        progress = 0.0f;
        return false;
    }

    protected virtual bool TryDispatchOuterToWorld(FactoryItem item, SimulationController simulation)
    {
        return false;
    }

    protected virtual float ResolveManualTransferCompletionProgress(MobileFactoryHeavyPortTransferMode mode)
    {
        return 1.0f;
    }

    protected virtual void OnManualTransferCompleted(MobileFactoryHeavyPortTransferMode mode, FactoryItem item)
    {
    }

    protected virtual bool TryResolveBufferedInteriorBufferPose(
        Node3D anchor,
        MobileFactoryHeavyCargoPresentationState presentation,
        out Vector3 localPosition,
        out float yaw)
    {
        localPosition = Vector3.Zero;
        yaw = 0.0f;
        return false;
    }

    protected abstract void AdvanceHeavyHandoff(SimulationController simulation);
    protected abstract void RefreshHandoffPhase(SimulationController simulation);

    public bool TryGetCurrentPresentationState(out MobileFactoryHeavyCargoPresentationState state)
    {
        if (!EnableHeavyCargoPresentation)
        {
            state = default;
            return false;
        }

        if (TryResolveCurrentPresentation(out var presentation))
        {
            if (!ShouldRenderHeavyCargoHost(presentation.Host))
            {
                state = default;
                return false;
            }

            state = presentation;
            return true;
        }

        state = default;
        return false;
    }

    public int CountVisiblePayloadVisuals()
    {
        var count = CountVisiblePayloadVisualsRecursive(this);
        if (_activeWorldConnectorRoot is not null && GodotObject.IsInstanceValid(_activeWorldConnectorRoot))
        {
            count += CountVisiblePayloadVisualsRecursive(_activeWorldConnectorRoot);
        }

        return count;
    }

    public override void OnDeploymentCleared(Node3D worldStructureRoot, SimulationController simulation)
    {
        _activeWorldConnectorRoot = null;
        CollapseManualTransferToBuffer();
    }

    private void UpdateInteriorPresentationVisual()
    {
        var hasActivePresentation = TryResolveCurrentPresentation(out var activePresentation);
        var showBridge = hasActivePresentation
            && activePresentation.Host == MobileFactoryHeavyCargoPresentationHost.InteriorBridge
            && ShouldRenderHeavyCargoHost(MobileFactoryHeavyCargoPresentationHost.InteriorBridge);
        var showInnerBuffer = false;
        MobileFactoryHeavyCargoPresentationState innerBufferPresentation = default;

        if (ShouldRenderHeavyCargoHost(MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer))
        {
            if (hasActivePresentation
                && activePresentation.Host == MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer)
            {
                innerBufferPresentation = activePresentation;
                showInnerBuffer = true;
            }
            else if (TryBuildBufferedPresentation(_innerBufferedItem, MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer, out innerBufferPresentation))
            {
                showInnerBuffer = true;
            }
        }

        if (showBridge)
        {
            ApplyInteriorAnchorVisual("BridgePayloadAnchor", activePresentation.Item, out var bridgeAnchor);
            if (bridgeAnchor is not null
                && TryResolveInteriorTransitPose(activePresentation, out var bridgePosition))
            {
                bridgeAnchor.Position = bridgePosition;
                SetTransitPayloadVisualLocalYaw(bridgeAnchor, ResolveInteriorBridgeVisualYaw(bridgeAnchor, activePresentation));
            }
        }
        else
        {
            ClearInteriorAnchorVisual("BridgePayloadAnchor");
        }

        if (showInnerBuffer)
        {
            ApplyInteriorAnchorVisual("InnerBufferPayloadAnchor", innerBufferPresentation.Item, out var innerBufferAnchor);
            if (innerBufferAnchor is not null)
            {
                if (TryResolveBufferedInteriorBufferPose(innerBufferAnchor, innerBufferPresentation, out var innerBufferPosition, out var innerBufferYaw))
                {
                    innerBufferAnchor.Position = innerBufferPosition;
                    SetTransitPayloadVisualLocalYaw(innerBufferAnchor, innerBufferYaw);
                }
                else
                {
                    innerBufferAnchor.Position = ResolveInteriorAnchorPosition(
                        "InnerBufferPayloadAnchor",
                        new Vector3(-CellSize * 0.74f, ItemHeight + 0.01f, 0.0f));
                    SetTransitPayloadVisualLocalYaw(innerBufferAnchor, ResolveInteriorBufferVisualYaw(innerBufferAnchor));
                }
            }
        }
        else
        {
            ClearInteriorAnchorVisual("InnerBufferPayloadAnchor");
        }
    }

    private void ClearInteriorPresentationVisual()
    {
        ClearInteriorAnchorVisual("BridgePayloadAnchor");
        ClearInteriorAnchorVisual("InnerBufferPayloadAnchor");
    }

    private void ApplyInteriorAnchorVisual(string anchorName, FactoryItem item, out Node3D? anchor)
    {
        anchor = GetNodeOrNull<Node3D>(anchorName);
        if (anchor is null)
        {
            return;
        }

        SyncTransitPayloadVisual(anchor, item);
        anchor.Visible = true;
    }

    private void ClearInteriorAnchorVisual(string anchorName)
    {
        if (GetNodeOrNull<Node3D>(anchorName) is not Node3D anchor)
        {
            return;
        }

        ClearTransitPayloadVisual(anchor);
        anchor.Visible = false;
    }

    private bool TryResolveCurrentPresentation(out MobileFactoryHeavyCargoPresentationState state)
    {
        if (!TryResolveActiveTransitItem(out var activeItem, out var progress) || activeItem is null)
        {
            if (TryResolveIdleBufferedPresentation(out state))
            {
                return true;
            }

            state = default;
            return false;
        }

        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer:
                state = new MobileFactoryHeavyCargoPresentationState(
                    activeItem,
                    MobileFactoryHeavyCargoPresentationOwner.HeavyPort,
                    MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff,
                    _handoffPhase,
                    progress);
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                var outerToInnerHost = ResolveOuterToInnerPresentationHost(progress);
                state = new MobileFactoryHeavyCargoPresentationState(
                    activeItem,
                    MobileFactoryHeavyCargoPresentationOwner.HeavyPort,
                    outerToInnerHost,
                    _handoffPhase,
                    progress);
                return true;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                state = new MobileFactoryHeavyCargoPresentationState(
                    activeItem,
                    MobileFactoryHeavyCargoPresentationOwner.HeavyPort,
                    ResolveInnerToOuterPresentationHost(progress),
                    _handoffPhase,
                    progress);
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToWorld:
                state = new MobileFactoryHeavyCargoPresentationState(
                    activeItem,
                    MobileFactoryHeavyCargoPresentationOwner.HeavyPort,
                    MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff,
                    _handoffPhase,
                    progress);
                return true;
            default:
                if (TryResolveIdleBufferedPresentation(out state))
                {
                    return true;
                }

                state = default;
                return false;
        }
    }

    private bool TryResolveIdleBufferedPresentation(out MobileFactoryHeavyCargoPresentationState state)
    {
        if (PreferOuterBufferedPresentationWhenIdle)
        {
            if (TryBuildBufferedPresentation(_outerBufferedItem, MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer, out state))
            {
                return true;
            }

            if (TryBuildBufferedPresentation(_innerBufferedItem, MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer, out state))
            {
                return true;
            }
        }
        else
        {
            if (TryBuildBufferedPresentation(_innerBufferedItem, MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer, out state))
            {
                return true;
            }

            if (TryBuildBufferedPresentation(_outerBufferedItem, MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer, out state))
            {
                return true;
            }
        }

        state = default;
        return false;
    }

    private bool TryBuildBufferedPresentation(
        FactoryItem? item,
        MobileFactoryHeavyCargoPresentationHost host,
        out MobileFactoryHeavyCargoPresentationState state)
    {
        if (item is null)
        {
            state = default;
            return false;
        }

        state = new MobileFactoryHeavyCargoPresentationState(
            item,
            MobileFactoryHeavyCargoPresentationOwner.HeavyPort,
            host,
            _handoffPhase,
            _handoffPhaseProgress);
        return true;
    }

    private bool TryResolveInteriorTransitPose(MobileFactoryHeavyCargoPresentationState presentation, out Vector3 localPosition)
    {
        localPosition = Vector3.Zero;
        var progress = presentation.Progress;
        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                var outerToInnerStage = ResolveOuterToInnerPresentationStage(progress);
                if (outerToInnerStage == OuterToInnerPresentationStage.InteriorRotate)
                {
                    localPosition = ResolveInteriorBridgeEntryPosition();
                    return true;
                }

                if (outerToInnerStage == OuterToInnerPresentationStage.InteriorMove)
                {
                    localPosition = ResolveInteriorBridgePath(
                        NormalizeSegmentProgress(progress, OuterToInnerInteriorRotateEndProgress, 1.0f),
                        worldToInterior: true);
                    return true;
                }

                return false;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                var innerToOuterStage = ResolveInnerToOuterPresentationStage(progress);
                if (innerToOuterStage != InnerToOuterPresentationStage.InteriorMove)
                {
                    return false;
                }

                localPosition = ResolveInteriorBridgePath(
                    NormalizeSegmentProgress(progress, InnerToOuterRotateOutEndProgress, BridgeAlignmentProgress),
                    worldToInterior: false);
                return true;
            default:
                return false;
        }
    }

    private bool TryResolveWorldTransitPose(Node3D root, MobileFactoryHeavyCargoPresentationState presentation, out Vector3 localPosition)
    {
        localPosition = Vector3.Zero;
        var fullLength = root.GetMeta("full_length", 0.0f).AsSingle();
        var mouthExtension = root.GetMeta("mouth_extension", 0.14f).AsSingle();
        var mouthZ = Mathf.Max(0.14f, fullLength * 0.16f);
        var outerBufferPosition = ResolveWorldOuterBufferPosition(root);
        var routePosition = root.GetMeta("world_route_start_position", new Vector3(outerBufferPosition.X, outerBufferPosition.Y, fullLength + mouthExtension + (CellSize * 0.34f))).AsVector3();
        var bridgePosition = new Vector3(outerBufferPosition.X, WorldTransitPayloadHeight, mouthZ);
        var progress = presentation.Progress;

        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer:
                localPosition = routePosition.Lerp(outerBufferPosition, progress);
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                var outerToInnerStage = ResolveOuterToInnerPresentationStage(progress);
                if (outerToInnerStage == OuterToInnerPresentationStage.OuterRotate)
                {
                    localPosition = outerBufferPosition;
                    return true;
                }

                if (outerToInnerStage == OuterToInnerPresentationStage.WorldMove)
                {
                    localPosition = outerBufferPosition.Lerp(
                        bridgePosition,
                        NormalizeSegmentProgress(progress, OuterToInnerRotateInEndProgress, OuterToInnerWorldMoveEndProgress));
                    return true;
                }

                if (progress >= BridgeAlignmentProgress)
                {
                    return false;
                }
                return false;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (ResolveInnerToOuterPresentationStage(progress) != InnerToOuterPresentationStage.WorldMove)
                {
                    return false;
                }

                localPosition = bridgePosition.Lerp(
                    outerBufferPosition,
                    NormalizeSegmentProgress(progress, BridgeAlignmentProgress, InnerToOuterWorldMoveEndProgress));
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToWorld:
                localPosition = outerBufferPosition.Lerp(routePosition, progress);
                return true;
            default:
                return false;
        }
    }

    private Vector3 ResolveInteriorBridgePath(float progress, bool worldToInterior)
    {
        var bridgeAnchor = ResolveInteriorBridgeEntryPosition();
        var innerAnchor = ResolveInteriorAnchorPosition("InnerBufferPayloadAnchor", new Vector3(-CellSize * 0.74f, ItemHeight + 0.01f, 0.0f));
        return worldToInterior
            ? bridgeAnchor.Lerp(innerAnchor, progress)
            : innerAnchor.Lerp(bridgeAnchor, progress);
    }

    protected Vector3 ResolveInteriorBridgeEntryPosition()
    {
        if (TryResolveBridgeWorldPoint(_activeWorldConnectorRoot, out var bridgeWorldPoint))
        {
            return ToLocal(bridgeWorldPoint);
        }

        return ResolveInteriorAnchorPosition("BridgePayloadAnchor", new Vector3(CellSize * 0.45f, ItemHeight + 0.02f, 0.0f));
    }

    protected Vector3 ResolveInteriorAnchorPosition(string anchorName, Vector3 fallback)
    {
        return GetNodeOrNull<Node3D>(anchorName) is Node3D anchor
            ? (anchor.HasMeta("default_local_position")
                ? anchor.GetMeta("default_local_position", anchor.Position).AsVector3()
                : anchor.Position)
            : fallback;
    }

    private Vector3 ResolveWorldOuterBufferPosition(Node3D root)
    {
        if (root.HasMeta("world_outer_buffer_position"))
        {
            return root.GetMeta("world_outer_buffer_position", Vector3.Zero).AsVector3();
        }

        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot/OuterBufferPayloadRoot/OuterBufferPayloadAnchor") is Node3D anchor)
        {
            return root.ToLocal(anchor.GlobalPosition);
        }

        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot/PayloadBufferAnchor") is Node3D payloadAnchor)
        {
            return root.ToLocal(payloadAnchor.GlobalPosition) + new Vector3(0.0f, WorldTransitPayloadHeight, 0.0f);
        }

        var fullLength = root.GetMeta("full_length", 0.0f).AsSingle();
        return new Vector3(0.0f, WorldTransitPayloadHeight, fullLength);
    }

    private float ResolveWorldRouteVisualYaw(Node3D anchor)
    {
        return ResolveAnchorLocalYawForWorldDirection(anchor, FactoryDirection.ToWorldForward(0.0f));
    }

    private float ResolveWorldTransitVisualYaw(
        Node3D root,
        Node3D anchor,
        MobileFactoryHeavyCargoPresentationState presentation)
    {
        if (_transferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer)
        {
            var outboundWorldYaw = ResolveWorldRouteVisualYaw(anchor);
            if (!TryResolveBridgeWorldDirection(root, out var outboundBridgeWorldDirection))
            {
                return outboundWorldYaw;
            }

            var outboundBridgeYaw = ResolveAnchorLocalYawForWorldDirection(anchor, outboundBridgeWorldDirection);
            return ResolveInnerToOuterPresentationStage(presentation.Progress) switch
            {
                InnerToOuterPresentationStage.WorldMove => outboundBridgeYaw,
                _ => outboundWorldYaw
            };
        }

        if (_transferMode != MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer)
        {
            return ResolveWorldRouteVisualYaw(anchor);
        }

        var worldYaw = ResolveWorldRouteVisualYaw(anchor);
        if (!TryResolveBridgeWorldDirection(root, out var bridgeWorldDirection))
        {
            return worldYaw;
        }

        var bridgeYaw = ResolveAnchorLocalYawForWorldDirection(anchor, bridgeWorldDirection);
        return ResolveOuterToInnerPresentationStage(presentation.Progress) switch
        {
            OuterToInnerPresentationStage.OuterRotate => LerpShortestAngle(
                worldYaw,
                bridgeYaw,
                Mathf.SmoothStep(0.0f, 1.0f, NormalizeSegmentProgress(presentation.Progress, 0.0f, OuterToInnerRotateInEndProgress))),
            _ => bridgeYaw
        };
    }

    private float ResolveWorldOuterBufferVisualYaw(
        Node3D root,
        Node3D anchor,
        MobileFactoryHeavyCargoPresentationState presentation)
    {
        var worldYaw = ResolveWorldRouteVisualYaw(anchor);
        if (_transferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer)
        {
            if (ResolveInnerToOuterPresentationStage(presentation.Progress) != InnerToOuterPresentationStage.OuterRotate)
            {
                return worldYaw;
            }

            if (!TryResolveBridgeWorldDirection(root, out var outboundBridgeWorldDirection))
            {
                return worldYaw;
            }

            var outboundBridgeYaw = ResolveAnchorLocalYawForWorldDirection(anchor, outboundBridgeWorldDirection);
            return LerpShortestAngle(
                outboundBridgeYaw,
                worldYaw,
                Mathf.SmoothStep(0.0f, 1.0f, NormalizeSegmentProgress(presentation.Progress, InnerToOuterWorldMoveEndProgress, 1.0f)));
        }

        if (_transferMode != MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer)
        {
            return worldYaw;
        }

        if (ResolveOuterToInnerPresentationStage(presentation.Progress) != OuterToInnerPresentationStage.OuterRotate)
        {
            return worldYaw;
        }

        if (!TryResolveBridgeWorldDirection(root, out var bridgeWorldDirection))
        {
            return worldYaw;
        }

        var bridgeYaw = ResolveAnchorLocalYawForWorldDirection(anchor, bridgeWorldDirection);
        return LerpShortestAngle(
            worldYaw,
            bridgeYaw,
            Mathf.SmoothStep(0.0f, 1.0f, NormalizeSegmentProgress(presentation.Progress, 0.0f, OuterToInnerRotateInEndProgress)));
    }

    private float ResolveInteriorBridgeVisualYaw(Node3D anchor, MobileFactoryHeavyCargoPresentationState presentation)
    {
        var bridgeYaw = ResolveInteriorBridgeDirectionYaw(anchor);
        var interiorYaw = ResolveInteriorBufferVisualYaw(anchor);

        if (_transferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer)
        {
            return ResolveInnerToOuterPresentationStage(presentation.Progress) switch
            {
                InnerToOuterPresentationStage.InteriorMove => bridgeYaw,
                _ => interiorYaw
            };
        }

        return ResolveOuterToInnerPresentationStage(presentation.Progress) switch
        {
            OuterToInnerPresentationStage.InteriorRotate => LerpShortestAngle(
                bridgeYaw,
                interiorYaw,
                Mathf.SmoothStep(0.0f, 1.0f, NormalizeSegmentProgress(presentation.Progress, OuterToInnerWorldMoveEndProgress, OuterToInnerInteriorRotateEndProgress))),
            OuterToInnerPresentationStage.InteriorMove => interiorYaw,
            _ => bridgeYaw
        };
    }

    protected float ResolveInteriorBridgeDirectionYaw(Node3D anchor)
    {
        if (!TryResolveBridgeWorldDirection(_activeWorldConnectorRoot, out var bridgeWorldDirection))
        {
            return ResolveInteriorBufferVisualYaw(anchor);
        }

        return ResolveAnchorLocalYawForWorldDirection(anchor, bridgeWorldDirection);
    }

    protected float ResolveInteriorBufferVisualYaw(Node3D anchor)
    {
        if (!TryResolveInteriorInterfaceWorldDirection(out var interiorWorldDirection))
        {
            return ResolveAnchorLocalYawForWorldDirection(anchor, -Vector3.Right);
        }

        return ResolveAnchorLocalYawForWorldDirection(anchor, interiorWorldDirection);
    }

    private bool TryResolveBridgeWorldDirection(Node3D? root, out Vector3 direction)
    {
        direction = FactoryDirection.ToWorldForward(0.0f);
        if (!TryResolveBridgeWorldSegment(root, out var worldStart, out var worldEnd))
        {
            return false;
        }

        var delta = worldEnd - worldStart;
        delta.Y = 0.0f;
        if (delta.LengthSquared() <= 0.0001f)
        {
            return false;
        }

        direction = delta.Normalized();
        return true;
    }

    private bool TryResolveBridgeWorldPoint(Node3D? root, out Vector3 worldPoint)
    {
        worldPoint = Vector3.Zero;
        if (!TryResolveBridgeWorldSegment(root, out _, out var worldEnd))
        {
            return false;
        }

        worldPoint = worldEnd;
        return true;
    }

    private bool TryResolveBridgeWorldSegment(Node3D? root, out Vector3 worldStart, out Vector3 worldEnd)
    {
        worldStart = Vector3.Zero;
        worldEnd = Vector3.Zero;
        if (root is null || !GodotObject.IsInstanceValid(root))
        {
            return false;
        }

        var fullLength = root.GetMeta("full_length", 0.0f).AsSingle();
        var mouthExtension = root.GetMeta("mouth_extension", 0.14f).AsSingle();
        var mouthZ = Mathf.Max(0.14f, fullLength * 0.16f);
        var outerBufferPosition = ResolveWorldOuterBufferPosition(root);
        var bridgePosition = new Vector3(outerBufferPosition.X, WorldTransitPayloadHeight, mouthZ);
        worldStart = root.ToGlobal(outerBufferPosition);
        worldEnd = root.ToGlobal(bridgePosition);
        return true;
    }

    private bool TryResolveInteriorInterfaceWorldDirection(out Vector3 direction)
    {
        direction = -Vector3.Right;
        var worldStart = ToGlobal(ResolveInteriorAnchorPosition("BridgePayloadAnchor", new Vector3(CellSize * 0.45f, ItemHeight + 0.02f, 0.0f)));
        var worldEnd = ToGlobal(ResolveInteriorAnchorPosition("InnerBufferPayloadAnchor", new Vector3(-CellSize * 0.74f, ItemHeight + 0.01f, 0.0f)));
        var delta = worldEnd - worldStart;
        delta.Y = 0.0f;
        if (delta.LengthSquared() <= 0.0001f)
        {
            return false;
        }

        direction = delta.Normalized();
        return true;
    }

    private static float ResolveAnchorLocalYawForWorldDirection(Node3D anchor, Vector3 worldForward)
    {
        if (anchor.GetParent() is not Node3D orientationFrame)
        {
            return 0.0f;
        }

        var localOrigin = orientationFrame.ToLocal(orientationFrame.GlobalPosition);
        var localTarget = orientationFrame.ToLocal(orientationFrame.GlobalPosition + worldForward);
        var delta = localTarget - localOrigin;
        delta.Y = 0.0f;
        if (delta.LengthSquared() <= 0.0001f)
        {
            return 0.0f;
        }

        return Mathf.Atan2(-delta.Z, delta.X);
    }

    private static float NormalizeSegmentProgress(float progress, float start, float end)
    {
        if (end <= start)
        {
            return progress >= end ? 1.0f : 0.0f;
        }

        return Mathf.Clamp((progress - start) / (end - start), 0.0f, 1.0f);
    }

    protected static float LerpShortestAngle(float from, float to, float weight)
    {
        var clamped = Mathf.Clamp(weight, 0.0f, 1.0f);
        var delta = NormalizeAngleRadians(to - from);
        return NormalizeAngleRadians(from + delta * clamped);
    }

    private static float NormalizeAngleRadians(float angle)
    {
        while (angle > Mathf.Pi)
        {
            angle -= Mathf.Tau;
        }

        while (angle < -Mathf.Pi)
        {
            angle += Mathf.Tau;
        }

        return angle;
    }

    private MobileFactoryHeavyCargoPresentationHost ResolveOuterToInnerPresentationHost(float progress)
    {
        return ResolveOuterToInnerPresentationStage(progress) switch
        {
            OuterToInnerPresentationStage.OuterRotate => MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer,
            OuterToInnerPresentationStage.WorldMove => MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff,
            OuterToInnerPresentationStage.InteriorRotate => MobileFactoryHeavyCargoPresentationHost.InteriorBridge,
            OuterToInnerPresentationStage.InteriorMove => MobileFactoryHeavyCargoPresentationHost.InteriorBridge,
            _ => MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff
        };
    }

    private MobileFactoryHeavyCargoPresentationHost ResolveInnerToOuterPresentationHost(float progress)
    {
        return ResolveInnerToOuterPresentationStage(progress) switch
        {
            InnerToOuterPresentationStage.InnerRotate => MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer,
            InnerToOuterPresentationStage.InteriorMove => MobileFactoryHeavyCargoPresentationHost.InteriorBridge,
            InnerToOuterPresentationStage.WorldMove => MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff,
            InnerToOuterPresentationStage.OuterRotate => MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer,
            _ => MobileFactoryHeavyCargoPresentationHost.InteriorBridge
        };
    }

    private static OuterToInnerPresentationStage ResolveOuterToInnerPresentationStage(float progress)
    {
        if (progress < OuterToInnerRotateInEndProgress)
        {
            return OuterToInnerPresentationStage.OuterRotate;
        }

        if (progress < OuterToInnerWorldMoveEndProgress)
        {
            return OuterToInnerPresentationStage.WorldMove;
        }

        if (progress < OuterToInnerInteriorRotateEndProgress)
        {
            return OuterToInnerPresentationStage.InteriorRotate;
        }

        return OuterToInnerPresentationStage.InteriorMove;
    }

    protected static InnerToOuterPresentationStage ResolveInnerToOuterPresentationStage(float progress)
    {
        if (progress < InnerToOuterRotateOutEndProgress)
        {
            return InnerToOuterPresentationStage.InnerRotate;
        }

        if (progress < BridgeAlignmentProgress)
        {
            return InnerToOuterPresentationStage.InteriorMove;
        }

        if (progress < InnerToOuterWorldMoveEndProgress)
        {
            return InnerToOuterPresentationStage.WorldMove;
        }

        return InnerToOuterPresentationStage.OuterRotate;
    }

    private enum OuterToInnerPresentationStage
    {
        OuterRotate,
        WorldMove,
        InteriorRotate,
        InteriorMove
    }

    protected enum InnerToOuterPresentationStage
    {
        InnerRotate,
        InteriorMove,
        WorldMove,
        OuterRotate
    }

    private void UpdateBeaconPulse()
    {
        if (GetNodeOrNull<MeshInstance3D>("Beacon") is not MeshInstance3D beacon)
        {
            return;
        }

        var pulse = 0.88f + Mathf.Sin((float)(Time.GetTicksMsec() * 0.008f)) * 0.12f;
        beacon.Scale = new Vector3(pulse, pulse, pulse);
    }

    private void PurgeTransitLegacyVisuals()
    {
        for (var index = 0; index < TransitItems.Count; index++)
        {
            var legacyVisual = TransitItems[index].LegacyVisual;
            if (legacyVisual is null)
            {
                continue;
            }

            legacyVisual.QueueFree();
            TransitItems[index].LegacyVisual = null;
        }
    }

    private string DescribeBridgeState()
    {
        return TryResolveActiveTransitItem(out var item, out var progress) && item is not null
            ? $"{DescribePhase(_handoffPhase)} {progress * 100.0f:0}%"
            : "空";
    }

    private string DescribePresentationOwnership()
    {
        return TryResolveCurrentPresentation(out var presentation)
            ? $"{DescribePresentationOwner(presentation.Owner)} / {DescribePresentationHost(presentation.Host)} / {FactoryPresentation.GetItemDisplayName(presentation.Item)}"
            : "空";
    }

    private string DescribePresentationSuffix()
    {
        return TryResolveCurrentPresentation(out var presentation)
            ? $" / 显示:{DescribePresentationHost(presentation.Host)}"
            : string.Empty;
    }

    private static string DescribeBufferedItem(FactoryItem? item)
    {
        return item is null ? "空" : FactoryPresentation.GetItemDisplayName(item);
    }

    private static int CountVisiblePayloadVisualsRecursive(Node root)
    {
        if (!GodotObject.IsInstanceValid(root))
        {
            return 0;
        }

        var count = root is Node3D node3D && node3D.Visible && string.Equals(root.Name.ToString(), "TransitPayloadVisual", StringComparison.Ordinal)
            ? 1
            : 0;
        foreach (var child in root.GetChildren())
        {
            if (child is Node childNode)
            {
                count += CountVisiblePayloadVisualsRecursive(childNode);
            }
        }

        return count;
    }

    public static string DescribePresentationOwner(MobileFactoryHeavyCargoPresentationOwner owner)
    {
        return owner switch
        {
            MobileFactoryHeavyCargoPresentationOwner.HeavyPort => "接口装卸链",
            MobileFactoryHeavyCargoPresentationOwner.CargoUnpacker => "解包舱",
            MobileFactoryHeavyCargoPresentationOwner.CargoPacker => "封包舱",
            _ => "空"
        };
    }

    public static string DescribePresentationHost(MobileFactoryHeavyCargoPresentationHost host)
    {
        return host switch
        {
            MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff => "世界侧交接位",
            MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer => "世界侧缓存位",
            MobileFactoryHeavyCargoPresentationHost.InteriorBridge => "舱桥位",
            MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer => "舱内缓存位",
            MobileFactoryHeavyCargoPresentationHost.ConverterStaging => "转换接舱位",
            MobileFactoryHeavyCargoPresentationHost.ConverterProcessing => "转换处理位",
            MobileFactoryHeavyCargoPresentationHost.ConverterDispatch => "转换出舱位",
            _ => "空"
        };
    }

    private bool HasManualTransferState()
    {
        return _manualTransferItem is not null;
    }

    private void AdvanceManualTransfer(SimulationController simulation, double stepSeconds)
    {
        if (_manualTransferItem is null)
        {
            return;
        }

        _manualTransferProgress = Mathf.Clamp(_manualTransferProgress + ((float)stepSeconds * TravelSpeed), 0.0f, 1.0f);
        var completionProgress = Mathf.Clamp(ResolveManualTransferCompletionProgress(_transferMode), 0.0f, 1.0f);
        if (_manualTransferProgress < completionProgress)
        {
            return;
        }

        _manualTransferProgress = Mathf.Max(_manualTransferProgress, completionProgress);
        TryCompleteManualTransfer(simulation);
    }

    private void TryCompleteManualTransfer(SimulationController simulation)
    {
        if (_manualTransferItem is null)
        {
            return;
        }

        var item = _manualTransferItem;
        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                if (_innerBufferedItem is not null)
                {
                    return;
                }

                _innerBufferedItem = item;
                BufferSettleTimer = BufferSettleSeconds;
                OnManualTransferCompleted(MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer, item);
                ClearManualTransferState();
                return;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (_outerBufferedItem is not null)
                {
                    return;
                }

                _outerBufferedItem = item;
                BufferSettleTimer = BufferSettleSeconds;
                OnManualTransferCompleted(MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer, item);
                ClearManualTransferState();
                return;
            case MobileFactoryHeavyPortTransferMode.OuterToWorld:
                if (!TryDispatchOuterToWorld(item, simulation))
                {
                    return;
                }

                OnManualTransferCompleted(MobileFactoryHeavyPortTransferMode.OuterToWorld, item);
                ClearManualTransferState();
                return;
        }
    }

    private void ClearManualTransferState()
    {
        _manualTransferItem = null;
        _manualTransferSourceCell = Vector2I.Zero;
        _manualTransferTargetCell = Vector2I.Zero;
        _manualTransferProgress = 0.0f;
        _transferMode = MobileFactoryHeavyPortTransferMode.None;
    }

    private void CollapseManualTransferToBuffer()
    {
        if (_manualTransferItem is null)
        {
            return;
        }

        var item = _manualTransferItem;
        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                if (_manualTransferProgress >= BridgeAlignmentProgress && _innerBufferedItem is null)
                {
                    _innerBufferedItem = item;
                }
                else if (_outerBufferedItem is null)
                {
                    _outerBufferedItem = item;
                }
                break;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (_manualTransferProgress < BridgeAlignmentProgress && _innerBufferedItem is null)
                {
                    _innerBufferedItem = item;
                }
                else if (_outerBufferedItem is null)
                {
                    _outerBufferedItem = item;
                }
                break;
            case MobileFactoryHeavyPortTransferMode.OuterToWorld:
                if (_outerBufferedItem is null)
                {
                    _outerBufferedItem = item;
                }
                break;
        }

        ClearManualTransferState();
    }

    protected static string DescribePhase(MobileFactoryHeavyHandoffPhase phase)
    {
        return phase switch
        {
            MobileFactoryHeavyHandoffPhase.WaitingWorldCargo => "等待世界来货",
            MobileFactoryHeavyHandoffPhase.ReceivingFromWorld => "接收世界大包",
            MobileFactoryHeavyHandoffPhase.BufferedOuter => "世界侧缓存",
            MobileFactoryHeavyHandoffPhase.SlidingToBridgeInward => "滑向入舱桥位",
            MobileFactoryHeavyHandoffPhase.BridgingInward => "向舱内桥接",
            MobileFactoryHeavyHandoffPhase.BufferedInner => "舱内缓存",
            MobileFactoryHeavyHandoffPhase.WaitingForUnpacker => "等待解包舱",
            MobileFactoryHeavyHandoffPhase.WaitingForPacker => "等待封包产出",
            MobileFactoryHeavyHandoffPhase.ReceivingFromPacker => "封包舱交接",
            MobileFactoryHeavyHandoffPhase.SlidingToBridgeOutward => "滑向出舱桥位",
            MobileFactoryHeavyHandoffPhase.BridgingOutward => "向世界桥接",
            MobileFactoryHeavyHandoffPhase.WaitingWorldPickup => "等待世界接货",
            MobileFactoryHeavyHandoffPhase.ReleasingToWorld => "释放到世界",
            _ => "待机"
        };
    }
}

public partial class MobileFactoryOutputPortStructure : MobileFactoryHeavyPortStructure
{
    private const float PackerArrivalSlideSeconds = 0.18f;
    private const float OuterBufferPresentationHoldSeconds = 0.24f;
    private int _packerArrivalSlideItemId = -1;
    private float _packerArrivalSlideTimer;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.OutputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.OutputPort);
    protected override bool IsInboundHandoff => false;
    protected override bool EnableHeavyCargoPresentation => true;
    protected override bool PreferOuterBufferedPresentationWhenIdle => true;

    protected override bool ShouldRenderHeavyCargoHost(MobileFactoryHeavyCargoPresentationHost host)
    {
        return host switch
        {
            MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff
                => (TransferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer
                    && ResolveInnerToOuterPresentationStage(BridgeTransferProgress) == InnerToOuterPresentationStage.WorldMove)
                    || TransferMode == MobileFactoryHeavyPortTransferMode.OuterToWorld,
            MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer
                => (OuterBufferedItem is not null && TransferMode == MobileFactoryHeavyPortTransferMode.None)
                    || (TransferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer
                        && ResolveInnerToOuterPresentationStage(BridgeTransferProgress) == InnerToOuterPresentationStage.OuterRotate),
            MobileFactoryHeavyCargoPresentationHost.InteriorBridge
                => TransferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer
                    && ResolveInnerToOuterPresentationStage(BridgeTransferProgress) == InnerToOuterPresentationStage.InteriorMove,
            MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer
                => InnerBufferedItem is not null
                    || TransferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer
                        && ResolveInnerToOuterPresentationStage(BridgeTransferProgress) == InnerToOuterPresentationStage.InnerRotate,
            _ => false
        };
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        base.SimulationStep(simulation, stepSeconds);
        UpdatePackerArrivalSlide((float)stepSeconds);
    }

    protected override void BuildVisuals()
    {
        base.BuildVisuals();
        var deckWidth = GetPortDeckWidth();
        var deckDepth = GetPortDeckDepth();
        CreateColoredBox("OutputLatch", new Vector3(deckWidth * 0.24f, 0.16f, deckDepth * 0.42f), AttachmentDefinition.ConnectorColor.Lightened(0.10f), new Vector3(deckWidth * 0.26f, 0.34f, 0.0f));
        BuildHeavyPortAnchors(AttachmentDefinition.ConnectorColor);
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return "出舱链路：封包舱交接 -> 舱内缓存 -> 出舱桥位 -> 世界侧缓存 -> 世界释放";
        if (HandoffPhase == MobileFactoryHeavyHandoffPhase.WaitingWorldPickup)
        {
            yield return "世界接货：世界路线尚未就绪，成品包会停在外缓存位等待。";
        }
    }

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return false;
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return false;
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = WorldAdjacentCell;
        return false;
    }

    protected override bool TryDispatchOuterToWorld(FactoryItem item, SimulationController simulation)
    {
        if (!TryResolveWorldReleaseTarget(item, simulation, out var sourceCell, out var targetCell))
        {
            return false;
        }

        if (!BoundWorldSite!.TryGetStructure(targetCell, out var target) || target is null)
        {
            return false;
        }

        if (target is BeltStructure belt)
        {
            return belt.TryAcceptExternalHandoff(item, sourceCell, simulation);
        }

        return simulation.TrySendItemToSite(this, sourceCell, BoundWorldSite!, targetCell, item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: false);
    }

    public bool CanAcceptPackedBundle(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return IsWorldFlowReady
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactorySiteKind.Interior, item)
            && InnerBufferedItem is null
            && OuterBufferedItem is null
            && !HasBridgeTransfer;
    }

    public bool TryAcceptPackedBundle(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanAcceptPackedBundle(item, sourceCell, simulation))
        {
            return false;
        }

        SetInnerBufferedItem(item);
        StartPackerArrivalSlide(item);
        BufferSettleTimer = BufferSettleSeconds;
        return true;
    }

    protected override void AdvanceHeavyHandoff(SimulationController simulation)
    {
        if (_packerArrivalSlideTimer > 0.0f)
        {
            return;
        }

        if (!IsWorldFlowReady)
        {
            return;
        }

        if (InnerBufferedItem is not null
            && OuterBufferedItem is null
            && !HasBridgeTransfer)
        {
            var buffered = TakeInnerBufferedItem();
            if (buffered is not null)
            {
                BeginTransfer(MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer, buffered, Cell, WorldPortCell);
            }
        }

        if (OuterBufferedItem is not null
            && !HasBridgeTransfer
            && TryResolveWorldReleaseTarget(OuterBufferedItem, simulation, out var releaseSourceCell, out var releaseTargetCell))
        {
            var buffered = TakeOuterBufferedItem();
            if (buffered is not null)
            {
                BeginTransfer(MobileFactoryHeavyPortTransferMode.OuterToWorld, buffered, releaseSourceCell, releaseTargetCell);
            }
        }
    }

    protected override float ResolveManualTransferCompletionProgress(MobileFactoryHeavyPortTransferMode mode)
    {
        if (mode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer)
        {
            return InnerToOuterWorldMoveEndProgress;
        }

        return base.ResolveManualTransferCompletionProgress(mode);
    }

    protected override void OnManualTransferCompleted(MobileFactoryHeavyPortTransferMode mode, FactoryItem item)
    {
        if (mode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer)
        {
            BufferSettleTimer = Mathf.Max(BufferSettleTimer, OuterBufferPresentationHoldSeconds);

        }
    }

    protected override void RefreshHandoffPhase(SimulationController simulation)
    {
        if (!IsConnectedToWorld)
        {
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.Idle);
            return;
        }

        switch (TransferMode)
        {
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (BridgeTransferProgress < BridgeAlignmentProgress)
                {
                    SetHandoffPhase(
                        MobileFactoryHeavyHandoffPhase.SlidingToBridgeOutward,
                        BridgeTransferProgress / Mathf.Max(0.001f, BridgeAlignmentProgress));
                }
                else
                {
                    SetHandoffPhase(
                        MobileFactoryHeavyHandoffPhase.BridgingOutward,
                        (BridgeTransferProgress - BridgeAlignmentProgress) / Mathf.Max(0.001f, 1.0f - BridgeAlignmentProgress));
                }
                return;
            case MobileFactoryHeavyPortTransferMode.OuterToWorld:
                SetHandoffPhase(MobileFactoryHeavyHandoffPhase.ReleasingToWorld, BridgeTransferProgress);
                return;
        }

        if (OuterBufferedItem is not null)
        {
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.WaitingWorldPickup);
        }
        else if (InnerBufferedItem is not null)
        {
            if (_packerArrivalSlideTimer > 0.0f)
            {
                SetHandoffPhase(
                    MobileFactoryHeavyHandoffPhase.ReceivingFromPacker,
                    1.0f - (_packerArrivalSlideTimer / PackerArrivalSlideSeconds));
            }
            else
            {
                SetHandoffPhase(MobileFactoryHeavyHandoffPhase.BufferedInner);
            }
        }
        else
        {
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.WaitingForPacker);
        }
    }

    protected override bool TryResolveBufferedInteriorBufferPose(
        Node3D anchor,
        MobileFactoryHeavyCargoPresentationState presentation,
        out Vector3 localPosition,
        out float yaw)
    {
        if (TransferMode == MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer
            && ResolveInnerToOuterPresentationStage(presentation.Progress) == InnerToOuterPresentationStage.InnerRotate)
        {
            var alpha = Mathf.SmoothStep(
                0.0f,
                1.0f,
                Mathf.Clamp(presentation.Progress / Mathf.Max(0.001f, InnerToOuterRotateOutEndProgress), 0.0f, 1.0f));
            localPosition = ResolveOutputInnerBufferPosition();
            yaw = LerpShortestAngle(
                ResolveInteriorBufferVisualYaw(anchor),
                ResolveInteriorBridgeDirectionYaw(anchor),
                alpha);
            return true;
        }

        if (_packerArrivalSlideTimer > 0.0f
            && _packerArrivalSlideItemId == presentation.Item.Id)
        {
            var alpha = 1.0f - (_packerArrivalSlideTimer / PackerArrivalSlideSeconds);
            alpha = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp(alpha, 0.0f, 1.0f));
            localPosition = ResolveOutputPackerHandoffPosition().Lerp(ResolveOutputInnerBufferPosition(), alpha);
            yaw = ResolveInteriorBufferVisualYaw(anchor);
            return true;
        }

        localPosition = Vector3.Zero;
        yaw = 0.0f;
        return false;
    }

    private bool CanReleaseToWorld(FactoryItem item, SimulationController simulation)
    {
        return TryResolveWorldReleaseTarget(item, simulation, out _, out _);
    }

    private bool TryResolveWorldReleaseTarget(
        FactoryItem item,
        SimulationController simulation,
        out Vector2I sourceCell,
        out Vector2I targetCell)
    {
        sourceCell = Vector2I.Zero;
        targetCell = Vector2I.Zero;
        if (!IsConnectedToWorld || !IsWorldFlowReady || BoundWorldSite is null)
        {
            return false;
        }

        if (CanWorldTargetAccept(item, WorldPortCell, WorldAdjacentCell, simulation))
        {
            sourceCell = WorldPortCell;
            targetCell = WorldAdjacentCell;
            return true;
        }

        var releaseEdgeCell = WorldAdjacentCell;
        var downstreamCell = releaseEdgeCell + FactoryDirection.ToCellOffset(WorldFacing);
        if (CanWorldTargetAccept(item, releaseEdgeCell, downstreamCell, simulation))
        {
            sourceCell = releaseEdgeCell;
            targetCell = downstreamCell;
            return true;
        }

        return false;
    }

    private bool CanWorldTargetAccept(
        FactoryItem item,
        Vector2I sourceCell,
        Vector2I targetCell,
        SimulationController simulation)
    {
        if (BoundWorldSite is null
            || !BoundWorldSite.TryGetStructure(targetCell, out var target)
            || target is null)
        {
            return false;
        }

        if (target is BeltStructure belt)
        {
            return belt.CanAcceptExternalHandoff(item, sourceCell, simulation);
        }

        return target.CanAcceptItem(item, sourceCell, simulation);
    }

    private void StartPackerArrivalSlide(FactoryItem item)
    {
        _packerArrivalSlideItemId = item.Id;
        _packerArrivalSlideTimer = PackerArrivalSlideSeconds;
    }

    private void UpdatePackerArrivalSlide(float stepSeconds)
    {
        if (InnerBufferedItem is null || _packerArrivalSlideItemId != InnerBufferedItem.Id)
        {
            _packerArrivalSlideTimer = 0.0f;
            _packerArrivalSlideItemId = InnerBufferedItem?.Id ?? -1;
            return;
        }

        _packerArrivalSlideTimer = Mathf.Max(0.0f, _packerArrivalSlideTimer - stepSeconds);
        if (_packerArrivalSlideTimer <= 0.0f)
        {
            _packerArrivalSlideItemId = -1;
        }
    }

    private Vector3 ResolveOutputPackerHandoffPosition()
    {
        return ResolveInteriorAnchorPosition(
            "ConverterHandoffAnchor",
            new Vector3(-GetPortDeckWidth() * 0.46f, ItemHeight + 0.04f, 0.0f));
    }

    private Vector3 ResolveOutputInnerBufferPosition()
    {
        return ResolveInteriorAnchorPosition(
            "InnerBufferPayloadAnchor",
            new Vector3(-CellSize * 0.74f, ItemHeight + 0.01f, 0.0f));
    }
}

public partial class MobileFactoryInputPortStructure : MobileFactoryHeavyPortStructure
{
    private static readonly bool EnableInboundBridgeFlow = true;
    private static readonly bool EnableInboundBufferedArrivalSlide = true;
    private static readonly bool EnableInboundUnpackerHandoff = true;
    private const float WorldOuterBufferPresentationHoldSeconds = 0.55f;
    private const float InteriorArrivalSlideSeconds = 0.18f;
    private static readonly Vector3 InputInnerBufferDeckPosition = new(-0.38f, 0.18f, 0.0f);

    private float _worldOuterBufferPresentationHoldTimer;
    private int _worldOuterBufferPresentationItemId = -1;
    private float _interiorArrivalSlideTimer;
    private int _interiorArrivalSlideItemId = -1;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.InputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.InputPort);
    protected override bool IsInboundHandoff => true;
    protected override bool EnableHeavyCargoPresentation => true;
    protected override bool PreferOuterBufferedPresentationWhenIdle => true;
    protected override bool ShouldRenderHeavyCargoHost(MobileFactoryHeavyCargoPresentationHost host)
    {
        return host switch
        {
            MobileFactoryHeavyCargoPresentationHost.WorldRouteHandoff
                => TransferMode == MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer
                    || TransferMode == MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer,
            MobileFactoryHeavyCargoPresentationHost.WorldOuterBuffer
                => (OuterBufferedItem is not null && TransferMode == MobileFactoryHeavyPortTransferMode.None)
                    || TransferMode == MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer,
            MobileFactoryHeavyCargoPresentationHost.InteriorBridge
                => TransferMode == MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer,
            MobileFactoryHeavyCargoPresentationHost.InteriorInnerBuffer
                => InnerBufferedItem is not null
                    || TransferMode == MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer,
            _ => false
        };
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        base.SimulationStep(simulation, stepSeconds);
        UpdateWorldOuterBufferPresentationHold((float)stepSeconds);
        UpdateInteriorArrivalSlide((float)stepSeconds);
    }

    protected override void BuildVisuals()
    {
        base.BuildVisuals();
        var deckWidth = GetPortDeckWidth();
        var deckDepth = GetPortDeckDepth();
        CreateColoredBox("InputReceiver", new Vector3(deckWidth * 0.24f, 0.16f, deckDepth * 0.42f), AttachmentDefinition.ConnectorColor.Lightened(0.10f), new Vector3(-deckWidth * 0.24f, 0.34f, 0.0f));
        BuildHeavyPortAnchors(AttachmentDefinition.ConnectorColor);
        if (GetNodeOrNull<Node3D>("InnerBufferDeck") is Node3D innerBufferDeck)
        {
            innerBufferDeck.Position = InputInnerBufferDeckPosition;
        }

        if (GetNodeOrNull<Node3D>("InnerBufferPayloadAnchor") is Node3D innerBufferAnchor)
        {
            innerBufferAnchor.Position = new Vector3(InputInnerBufferDeckPosition.X, ItemHeight + 0.04f, 0.0f);
            innerBufferAnchor.SetMeta("default_local_position", innerBufferAnchor.Position);
        }
    }

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return IsWorldFlowReady
            && sourceCell == WorldAdjacentCell
            && CanStageInboundWorldCargo();
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return false;
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = WorldPortCell;
        return IsWorldFlowReady
            && CanStageInboundWorldCargo()
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactorySiteKind.Interior, item);
    }

    protected override void AdvanceHeavyHandoff(SimulationController simulation)
    {
        if (_interiorArrivalSlideTimer > 0.0f)
        {
            return;
        }

        if (EnableInboundUnpackerHandoff
            && InnerBufferedItem is not null
            && TryHandOffToUnpacker(simulation))
        {
            SetInnerBufferedItem(null);
        }

        if (_worldOuterBufferPresentationHoldTimer > 0.0f)
        {
            return;
        }

        if (IsWorldFlowReady
            && OuterBufferedItem is not null
            && InnerBufferedItem is null
            && !HasBridgeTransfer)
        {
            var buffered = TakeOuterBufferedItem();
            if (buffered is not null)
            {
                BeginTransfer(MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer, buffered, WorldPortCell, Cell);
            }
        }
    }

    protected override float ResolveManualTransferCompletionProgress(MobileFactoryHeavyPortTransferMode mode)
    {
        if (EnableInboundBufferedArrivalSlide && mode == MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer)
        {
            return OuterToInnerInteriorRotateEndProgress;
        }

        return base.ResolveManualTransferCompletionProgress(mode);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: true);
    }

    protected override void RefreshHandoffPhase(SimulationController simulation)
    {
        if (!IsConnectedToWorld)
        {
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.Idle);
            return;
        }

        switch (TransferMode)
        {
            case MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer:
                SetHandoffPhase(MobileFactoryHeavyHandoffPhase.ReceivingFromWorld, BridgeTransferProgress);
                return;
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                if (!EnableInboundBridgeFlow)
                {
                    break;
                }

                if (BridgeTransferProgress < BridgeAlignmentProgress)
                {
                    SetHandoffPhase(
                        MobileFactoryHeavyHandoffPhase.SlidingToBridgeInward,
                        BridgeTransferProgress / Mathf.Max(0.001f, BridgeAlignmentProgress));
                }
                else
                {
                    SetHandoffPhase(
                        MobileFactoryHeavyHandoffPhase.BridgingInward,
                        (BridgeTransferProgress - BridgeAlignmentProgress) / Mathf.Max(0.001f, 1.0f - BridgeAlignmentProgress));
                }
                return;
        }

        if (InnerBufferedItem is not null)
        {
            var canHandOffToUnpacker = EnableInboundUnpackerHandoff
                && EnableInboundBridgeFlow
                && CanConnectedUnpackerAccept(InnerBufferedItem, simulation);
            SetHandoffPhase(
                canHandOffToUnpacker
                    ? MobileFactoryHeavyHandoffPhase.BufferedInner
                    : EnableInboundUnpackerHandoff
                        ? MobileFactoryHeavyHandoffPhase.WaitingForUnpacker
                        : MobileFactoryHeavyHandoffPhase.BufferedInner);
        }
        else if (OuterBufferedItem is not null)
        {
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.BufferedOuter);
        }
        else
        {
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.WaitingWorldCargo);
        }
    }

    private bool TryHandOffToUnpacker(SimulationController simulation)
    {
        if (InnerBufferedItem is null)
        {
            return false;
        }

        var contract = GetResolvedLogisticsContract();
        for (var index = 0; index < contract.OutputAnchors.Count; index++)
        {
            var outputAnchor = contract.OutputAnchors[index];
            if (!FactoryStructurePortResolver.TryResolveReceiver(Site, outputAnchor.Cell, out var resolution)
                || resolution.Structure is not IFactoryHeavyBundleReceiver unpacker)
            {
                continue;
            }

            var sourceDispatchCell = outputAnchor.DispatchSourceCell;
            if (unpacker.TryAcceptHeavyBundle(InnerBufferedItem, sourceDispatchCell, ResolveInputInnerBufferWorldPosition(), simulation))
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 ResolveInputInnerBufferWorldPosition()
    {
        if (GetNodeOrNull<Node3D>("ConverterHandoffAnchor") is Node3D converterHandoffAnchor)
        {
            return converterHandoffAnchor.GlobalPosition;
        }

        if (GetNodeOrNull<Node3D>("InnerBufferPayloadAnchor") is Node3D innerBufferAnchor)
        {
            return innerBufferAnchor.GlobalPosition;
        }

        return ToGlobal(ResolveInteriorAnchorPosition(
            "ConverterHandoffAnchor",
            new Vector3(-GetPortDeckWidth() * 0.46f, ItemHeight + 0.04f, 0.0f)));
    }

    private bool CanStageInboundWorldCargo()
    {
        if (HasBridgeTransfer || OuterBufferedItem is not null)
        {
            return false;
        }
        return true;
    }

    private bool CanConnectedUnpackerAccept(FactoryItem item, SimulationController simulation)
    {
        var contract = GetResolvedLogisticsContract();
        for (var index = 0; index < contract.OutputAnchors.Count; index++)
        {
            var outputAnchor = contract.OutputAnchors[index];
            if (!FactoryStructurePortResolver.TryResolveReceiver(Site, outputAnchor.Cell, out var resolution)
                || resolution.Structure is not IFactoryHeavyBundleReceiver unpacker)
            {
                continue;
            }

            var sourceDispatchCell = outputAnchor.DispatchSourceCell;
            if (unpacker.CanAcceptHeavyBundle(item, sourceDispatchCell, simulation))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateWorldOuterBufferPresentationHold(float stepSeconds)
    {
        if (OuterBufferedItem is null)
        {
            _worldOuterBufferPresentationHoldTimer = 0.0f;
            _worldOuterBufferPresentationItemId = -1;
            return;
        }

        if (_worldOuterBufferPresentationItemId != OuterBufferedItem.Id)
        {
            _worldOuterBufferPresentationItemId = OuterBufferedItem.Id;
            _worldOuterBufferPresentationHoldTimer = WorldOuterBufferPresentationHoldSeconds;
            return;
        }

        _worldOuterBufferPresentationHoldTimer = Mathf.Max(0.0f, _worldOuterBufferPresentationHoldTimer - stepSeconds);
    }

    private void UpdateInteriorArrivalSlide(float stepSeconds)
    {
        if (InnerBufferedItem is null || _interiorArrivalSlideItemId != InnerBufferedItem.Id)
        {
            _interiorArrivalSlideTimer = 0.0f;
            _interiorArrivalSlideItemId = InnerBufferedItem?.Id ?? -1;
            return;
        }

        _interiorArrivalSlideTimer = Mathf.Max(0.0f, _interiorArrivalSlideTimer - stepSeconds);
        if (_interiorArrivalSlideTimer <= 0.0f)
        {
            _interiorArrivalSlideItemId = -1;
        }
    }

    private void StartInteriorArrivalSlide(FactoryItem item)
    {
        _interiorArrivalSlideItemId = item.Id;
        _interiorArrivalSlideTimer = InteriorArrivalSlideSeconds;
    }

    protected override void OnManualTransferCompleted(MobileFactoryHeavyPortTransferMode mode, FactoryItem item)
    {
        if (EnableInboundBufferedArrivalSlide && mode == MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer)
        {
            StartInteriorArrivalSlide(item);
        }
    }

    protected override bool TryResolveBufferedInteriorBufferPose(
        Node3D anchor,
        MobileFactoryHeavyCargoPresentationState presentation,
        out Vector3 localPosition,
        out float yaw)
    {
        if (_interiorArrivalSlideTimer > 0.0f
            && _interiorArrivalSlideItemId == presentation.Item.Id)
        {
            var alpha = 1.0f - (_interiorArrivalSlideTimer / InteriorArrivalSlideSeconds);
            alpha = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp(alpha, 0.0f, 1.0f));
            localPosition = ResolveInteriorBridgeEntryPosition().Lerp(
                ResolveInteriorAnchorPosition(
                    "InnerBufferPayloadAnchor",
                    new Vector3(InputInnerBufferDeckPosition.X, ItemHeight + 0.04f, 0.0f)),
                alpha);
            yaw = ResolveInteriorBufferVisualYaw(anchor);
            return true;
        }

        localPosition = Vector3.Zero;
        yaw = 0.0f;
        return false;
    }
}

public partial class MobileFactoryMiningInputPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    private const float MiningCycleSeconds = 0.95f;
    private const float StakeDeployDurationSeconds = 0.28f;
    private const string BuildOneStakeActionId = "build-one-stake";
    private const string BuildAllStakesActionId = "build-all-stakes";
    private static readonly Color MiningHubBaseColor = new("FACC15");
    private static readonly Color MiningHubCoreColor = new("93C5FD");

    private FactoryResourceKind? _resourceKind;
    private string _depositName = "未绑定矿区";
    private float _miningTimer;
    private int _builtStakeCount = Mathf.Max(0, MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.MiningInputPort).ExteriorStencil.Count - 1);
    private int _eligibleStakeCount;
    private readonly Dictionary<Vector2I, MobileFactoryMiningStakeStructure> _deployedStakes = new();
    private readonly Dictionary<Vector2I, MobileFactoryMiningStakeStructure> _deployingStakes = new();
    private readonly List<Vector2I> _pendingStakeCells = new();
    private Node3D? _worldStructureRoot;
    private SimulationController? _worldSimulation;
    private float _stakeDeployCooldownTimer;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.MiningInputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.MiningInputPort);
    public override float WorldVisualAnimationDurationSeconds => 1.0f;
    public int MaxStakeCapacity => Mathf.Max(0, AttachmentDefinition.ExteriorStencil.Count - 1);
    public int BuiltStakeCount => _builtStakeCount;
    public int DeployedStakeCount => _deployedStakes.Count;
    public int EligibleStakeCount => _eligibleStakeCount;
    public int DeployingStakeCount => _deployingStakes.Count;
    public override string ConnectionStateLabel => IsConnectedToWorld
        ? !IsWorldFlowReady
            ? "展开中"
            : _deployingStakes.Count > 0 || _pendingStakeCells.Count > 0
                ? "部署中"
                : _deployedStakes.Count == 0
                ? "待机"
                : _deployedStakes.Count < _eligibleStakeCount
                    ? TransitItemCount > 0 ? "部分采集" : "部分部署"
                    : TransitItemCount > 0 ? "采集中" : "就绪"
        : HasDeploymentProjection
            ? _eligibleStakeCount == 0
                ? "待机"
                : _builtStakeCount == 0
                    ? "缺少采矿桩"
                    : "部分部署"
            : TransitItemCount > 0 ? "阻塞" : "未连接";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return false;
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == Cell - FactoryDirection.ToCellOffset(Facing);
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"矿区：{_depositName}";
        yield return $"采矿桩库存：{_builtStakeCount}/{MaxStakeCapacity}";
        yield return $"已部署采矿桩：{_deployedStakes.Count}/{Mathf.Max(_eligibleStakeCount, 1)}";
        if (_deployingStakes.Count > 0 || _pendingStakeCells.Count > 0)
        {
            yield return $"部署中采矿桩：{_deployingStakes.Count} | 排队：{_pendingStakeCells.Count}";
        }
        if (HasDeploymentProjection)
        {
            yield return $"当前可部署矿位：{_eligibleStakeCount}";
        }
    }

    public override bool CanBindToWorld(GridManager worldSite, MobileFactoryAttachmentProjection projection, out string reason)
    {
        ResolveMiningStakePlan(worldSite, projection, _builtStakeCount, out _, out _, out var deployedStakeCells);
        reason = deployedStakeCells.Count > 0 ? string.Empty : "采矿输入端口当前没有可部署的采矿桩。";
        return deployedStakeCells.Count > 0;
    }

    public override MobileFactoryAttachmentDeploymentEvaluation EvaluateDeployment(GridManager worldSite, MobileFactoryAttachmentProjection projection)
    {
        var previewWorldCells = new List<Vector2I>(projection.WorldCells);
        var reservedWorldCells = new List<Vector2I>(projection.WorldCells);
        ResolveMiningStakePlan(worldSite, projection, _builtStakeCount, out _, out var eligibleStakeCells, out var deployedStakeCells);
        if (eligibleStakeCells.Count == 0)
        {
            return new MobileFactoryAttachmentDeploymentEvaluation(
                this,
                projection,
                MobileFactoryAttachmentDeployState.Optional,
                previewWorldCells,
                reservedWorldCells,
                new List<Vector2I>(),
                "采矿输入端口未覆盖矿点，将以待机状态部署。");
        }

        if (deployedStakeCells.Count == 0)
        {
            return new MobileFactoryAttachmentDeploymentEvaluation(
                this,
                projection,
                MobileFactoryAttachmentDeployState.Optional,
                previewWorldCells,
                reservedWorldCells,
                new List<Vector2I>(),
                $"采矿输入端口可覆盖 {eligibleStakeCells.Count} 个矿位，但采矿桩库存不足，将以待机状态部署。");
        }

        if (deployedStakeCells.Count < eligibleStakeCells.Count)
        {
            return new MobileFactoryAttachmentDeploymentEvaluation(
                this,
                projection,
                MobileFactoryAttachmentDeployState.Optional,
                previewWorldCells,
                reservedWorldCells,
                new List<Vector2I>(deployedStakeCells),
                $"采矿桩库存不足，当前仅会部署 {deployedStakeCells.Count}/{eligibleStakeCells.Count} 个矿位。");
        }

        return new MobileFactoryAttachmentDeploymentEvaluation(
            this,
            projection,
            MobileFactoryAttachmentDeployState.Connected,
            previewWorldCells,
            reservedWorldCells,
            new List<Vector2I>(deployedStakeCells),
            string.Empty);
    }

    public override IReadOnlyList<Vector2I> GetReservedWorldCells(GridManager worldSite, MobileFactoryAttachmentProjection projection)
    {
        return new List<Vector2I>(projection.WorldCells);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        base.SimulationStep(simulation, stepSeconds);
        UpdateStakeDeploymentQueue((float)stepSeconds, simulation);

        if (!IsConnectedToWorld || !_resourceKind.HasValue || _deployedStakes.Count == 0)
        {
            return;
        }

        if (!IsWorldFlowReady)
        {
            return;
        }

        _miningTimer = Mathf.Max(0.0f, _miningTimer - (float)stepSeconds);
        if (_miningTimer > 0.0f)
        {
            return;
        }

        var producedItemCount = 0;
        var outputKind = FactoryResourceCatalog.GetOutputItemKind(_resourceKind.Value);
        foreach (var stakeCell in GetReadyStakeCellsForMining())
        {
            var item = simulation.CreateItem(FactorySiteKind.World, Kind, outputKind, FactoryCargoForm.WorldBulk);
            if (!TryReceiveProvidedItem(item, stakeCell, simulation))
            {
                continue;
            }

            producedItemCount++;
        }

        if (producedItemCount > 0)
        {
            _miningTimer = MiningCycleSeconds;
        }
    }

    public override FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        return new FactoryStructureDetailModel(
            InspectionTitle,
            "采矿桩库存、部署状态与重建",
            summaryLines,
            actions: new[]
            {
                new FactoryDetailActionModel(
                    BuildOneStakeActionId,
                    "建造 1 个采矿桩",
                    $"当前库存 {_builtStakeCount}/{MaxStakeCapacity}",
                    _builtStakeCount < MaxStakeCapacity),
                new FactoryDetailActionModel(
                    BuildAllStakesActionId,
                    "补满采矿桩",
                    $"补至容量上限 {MaxStakeCapacity}",
                    _builtStakeCount < MaxStakeCapacity)
            });
    }

    public void DescribePreviewStakePlan(
        GridManager worldSite,
        MobileFactoryAttachmentProjection projection,
        out List<Vector2I> eligibleStakeCells,
        out List<Vector2I> deployedStakeCells)
    {
        ResolveMiningStakePlan(worldSite, projection, _builtStakeCount, out _, out eligibleStakeCells, out deployedStakeCells);
    }

    public override bool TryInvokeDetailAction(string actionId)
    {
        return actionId switch
        {
            BuildOneStakeActionId => RebuildStakes(_builtStakeCount + 1) > 0,
            BuildAllStakesActionId => RebuildStakes(MaxStakeCapacity) > 0,
            _ => false
        };
    }

    public override void OnDeploymentActivated(
        Node3D worldStructureRoot,
        SimulationController simulation,
        GridManager worldGrid,
        MobileFactoryAttachmentDeploymentEvaluation evaluation)
    {
        _worldStructureRoot = worldStructureRoot;
        _worldSimulation = simulation;
        SyncStakeDeployment(worldGrid, worldStructureRoot, simulation, evaluation);
        RefreshMiningRuntimeState();
    }

    public override void OnDeploymentCleared(Node3D worldStructureRoot, SimulationController simulation)
    {
        var worldSite = BoundWorldSite ?? DeploymentWorldSite;
        if (worldSite is not null)
        {
            var staleStakes = new List<MobileFactoryMiningStakeStructure>(_deployedStakes.Values);
            staleStakes.AddRange(_deployingStakes.Values);
            _deployedStakes.Clear();
            _deployingStakes.Clear();
            _pendingStakeCells.Clear();
            for (var index = 0; index < staleStakes.Count; index++)
            {
                var stake = staleStakes[index];
                if (!GodotObject.IsInstanceValid(stake))
                {
                    continue;
                }

                stake.PrepareForRemoval();
                simulation.UnregisterStructure(stake);
                worldSite.RemoveStructure(stake);
                stake.QueueFree();
            }
        }

        _worldStructureRoot = null;
        _worldSimulation = null;
        _miningTimer = 0.0f;
        _stakeDeployCooldownTimer = 0.0f;
        RefreshMiningRuntimeState();
    }

    public override void BuildWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not null)
        {
            return;
        }

        var payloadRoot = new Node3D
        {
            Name = "WorldPayloadRoot"
        };
        root.AddChild(payloadRoot);

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "MiningHubPad",
            Mesh = new CylinderMesh
            {
                TopRadius = worldGrid.CellSize * 0.20f,
                BottomRadius = worldGrid.CellSize * 0.20f,
                Height = 0.06f
            },
            Position = new Vector3(0.0f, 0.03f, 0.0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = MiningHubBaseColor,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                Roughness = 0.34f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "MiningHubCore",
            Mesh = new CylinderMesh
            {
                TopRadius = worldGrid.CellSize * 0.07f,
                BottomRadius = worldGrid.CellSize * 0.10f,
                Height = 0.34f
            },
            Position = new Vector3(0.0f, 0.18f, 0.0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = MiningHubCoreColor,
                Roughness = 0.28f,
                EmissionEnabled = true,
                Emission = MiningHubCoreColor.Darkened(0.12f)
            }
        });
    }

    public override void ConfigureWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        if (root.GetNodeOrNull<MeshInstance3D>("ConnectorEndpoint") is MeshInstance3D endpoint)
        {
            endpoint.Visible = false;
        }

        if (root.GetNodeOrNull<MeshInstance3D>("ConnectorMouth") is MeshInstance3D mouth)
        {
            mouth.Visible = false;
        }

        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not Node3D payloadRoot)
        {
            return;
        }

        var payloadCenterWorld = worldGrid.CellToWorld(projection.WorldPortCell);
        var payloadLocalPosition = root.ToLocal(payloadCenterWorld);
        payloadRoot.SetMeta("payload_target_position", payloadLocalPosition);
        payloadRoot.Position = payloadLocalPosition;
        payloadRoot.Rotation = new Vector3(
            0.0f,
            FactoryDirection.ToYRotationRadians(projection.WorldFacing) - root.Rotation.Y,
            0.0f);
        payloadRoot.Visible = true;
    }

    public override Vector3 GetWorldConnectorEndWorld(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        return worldGrid.CellToWorld(projection.WorldPortCell) + new Vector3(0.0f, 0.18f, 0.0f);
    }

    public override void ApplyWorldPayloadVisualProgress(Node3D root, float progress)
    {
        base.ApplyWorldPayloadVisualProgress(root, progress);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = Cell - FactoryDirection.ToCellOffset(Facing);
        return IsWorldFlowReady
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactorySiteKind.Interior, item);
    }

    protected override int GetTransitLaneKey(Vector2I sourceCell, Vector2I targetCell)
    {
        return sourceCell == WorldPortCell ? 0 : sourceCell.GetHashCode();
    }

    protected override bool CanReceiveProvidedFrom(Vector2I sourceCell)
    {
        if (!IsConnectedToWorld || Projection is null)
        {
            return false;
        }

        if (!IsWorldFlowReady)
        {
            return false;
        }

        foreach (var deployedStakeCell in _deployedStakes.Keys)
        {
            if (deployedStakeCell == sourceCell)
            {
                return true;
            }
        }

        return sourceCell == WorldPortCell;
    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        return simulation.TrySendItem(this, Cell - FactoryDirection.ToCellOffset(Facing), state.Item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: true);
    }

    protected override void OnWorldBindingChanged()
    {
        RefreshMiningRuntimeState();
    }

    protected override void OnDeploymentContextChanged()
    {
        RefreshMiningRuntimeState();
    }

    public void HandleDeployedStakeDestroyed(MobileFactoryMiningStakeStructure destroyedStake)
    {
        var removed = _deployedStakes.Remove(destroyedStake.Cell);
        removed |= _deployingStakes.Remove(destroyedStake.Cell);
        if (!removed)
        {
            return;
        }

        _builtStakeCount = Mathf.Max(0, _builtStakeCount - 1);
        if (_worldStructureRoot is not null
            && _worldSimulation is not null
            && DeploymentWorldSite is GridManager worldSite
            && DeploymentProjection is not null)
        {
            SyncStakeDeployment(worldSite, _worldStructureRoot, _worldSimulation, EvaluateDeployment(worldSite, DeploymentProjection));
        }

        RefreshMiningRuntimeState();
    }

    public void HandleDeployingStakeCompleted(MobileFactoryMiningStakeStructure completedStake)
    {
        if (!_deployingStakes.Remove(completedStake.Cell))
        {
            return;
        }

        _deployedStakes[completedStake.Cell] = completedStake;
        _stakeDeployCooldownTimer = 0.0f;
        RefreshMiningRuntimeState();
    }

    private int RebuildStakes(int targetBuiltCount)
    {
        var desiredBuiltCount = Mathf.Clamp(targetBuiltCount, 0, MaxStakeCapacity);
        if (desiredBuiltCount <= _builtStakeCount)
        {
            return 0;
        }

        var rebuiltCount = desiredBuiltCount - _builtStakeCount;
        _builtStakeCount = desiredBuiltCount;
        RefreshMiningRuntimeState();

        if (_worldStructureRoot is not null
            && _worldSimulation is not null
            && DeploymentWorldSite is GridManager worldSite
            && DeploymentProjection is not null)
        {
            var evaluation = EvaluateDeployment(worldSite, DeploymentProjection);
            SyncStakeDeployment(worldSite, _worldStructureRoot, _worldSimulation, evaluation);
            if (!IsConnectedToWorld && evaluation.ActiveWorldCells.Count > 0)
            {
                BindToWorld(worldSite, DeploymentProjection);
            }
            RefreshMiningRuntimeState();
        }

        return rebuiltCount;
    }

    private void SyncStakeDeployment(
        GridManager worldSite,
        Node3D worldStructureRoot,
        SimulationController simulation,
        MobileFactoryAttachmentDeploymentEvaluation evaluation)
    {
        var desiredStakeCells = new HashSet<Vector2I>(evaluation.ActiveWorldCells);
        var orderedStakeCells = OrderStakeCellsForDeployment(evaluation.Projection, evaluation.ActiveWorldCells);
        var staleStakeCells = new List<Vector2I>();
        foreach (var deployedStakeCell in _deployedStakes.Keys)
        {
            if (!desiredStakeCells.Contains(deployedStakeCell))
            {
                staleStakeCells.Add(deployedStakeCell);
            }
        }

        for (var index = 0; index < staleStakeCells.Count; index++)
        {
            var staleCell = staleStakeCells[index];
            if (!_deployedStakes.Remove(staleCell, out var staleStake))
            {
                continue;
            }

            RemoveStakeInstance(staleStake, worldSite, simulation);
        }

        staleStakeCells.Clear();
        foreach (var deployingStakeCell in _deployingStakes.Keys)
        {
            if (!desiredStakeCells.Contains(deployingStakeCell))
            {
                staleStakeCells.Add(deployingStakeCell);
            }
        }

        for (var index = 0; index < staleStakeCells.Count; index++)
        {
            var staleCell = staleStakeCells[index];
            if (!_deployingStakes.Remove(staleCell, out var staleStake))
            {
                continue;
            }

            RemoveStakeInstance(staleStake, worldSite, simulation);
        }

        for (var index = _pendingStakeCells.Count - 1; index >= 0; index--)
        {
            if (!desiredStakeCells.Contains(_pendingStakeCells[index]))
            {
                _pendingStakeCells.RemoveAt(index);
            }
        }

        for (var index = 0; index < orderedStakeCells.Count; index++)
        {
            var worldCell = orderedStakeCells[index];
            if (_deployedStakes.ContainsKey(worldCell)
                || _deployingStakes.ContainsKey(worldCell)
                || _pendingStakeCells.Contains(worldCell))
            {
                continue;
            }

            _pendingStakeCells.Add(worldCell);
        }
    }

    private void RefreshMiningRuntimeState()
    {
        _miningTimer = 0.0f;

        var worldSite = BoundWorldSite ?? DeploymentWorldSite;
        var projection = Projection ?? DeploymentProjection;
        if (worldSite is null || projection is null)
        {
            _resourceKind = null;
            _depositName = "未绑定矿区";
            _eligibleStakeCount = 0;
            return;
        }

        ResolveMiningStakePlan(worldSite, projection, _builtStakeCount, out var deposit, out var eligibleStakeCells, out _);
        _eligibleStakeCount = eligibleStakeCells.Count;
        if (deposit is null)
        {
            _resourceKind = null;
            _depositName = "未绑定矿区";
            return;
        }

        _resourceKind = deposit.ResourceKind;
        _depositName = deposit.DisplayName;
    }

    private void UpdateStakeDeploymentQueue(float stepSeconds, SimulationController simulation)
    {
        if (!IsConnectedToWorld || !IsWorldFlowReady)
        {
            _stakeDeployCooldownTimer = 0.0f;
            return;
        }

        if (_pendingStakeCells.Count == 0)
        {
            _stakeDeployCooldownTimer = 0.0f;
            return;
        }

        if (_deployingStakes.Count > 0)
        {
            return;
        }

        _stakeDeployCooldownTimer = Mathf.Max(0.0f, _stakeDeployCooldownTimer - stepSeconds);
        if (_stakeDeployCooldownTimer > 0.0f
            || _worldStructureRoot is null
            || BoundWorldSite is not GridManager worldSite
            || Projection is null)
        {
            return;
        }

        var worldCell = _pendingStakeCells[0];
        _pendingStakeCells.RemoveAt(0);
        if (!worldSite.TryGetResourceDeposit(worldCell, out var deposit) || deposit is null)
        {
            return;
        }

        var facing = FactoryDirection.Opposite(Projection.WorldFacing);
        var stake = new MobileFactoryMiningStakeStructure();
        var hubWorld = worldSite.CellToWorld(Projection.WorldPortCell) + new Vector3(0.0f, 0.18f, 0.0f);
        stake.ConfigureStake(this, worldSite, worldCell, facing, deposit, hubWorld, $"{ReservationOwnerId}:stake:{worldCell.X}:{worldCell.Y}");
        _worldStructureRoot.AddChild(stake);
        worldSite.PlaceStructure(stake);
        simulation.RegisterStructure(stake);
        stake.BeginDeployment(StakeDeployDurationSeconds);
        _deployingStakes[worldCell] = stake;
        _stakeDeployCooldownTimer = StakeDeployDurationSeconds;
        RefreshMiningRuntimeState();
    }

    private void RemoveStakeInstance(MobileFactoryMiningStakeStructure stake, GridManager worldSite, SimulationController simulation)
    {
        if (!GodotObject.IsInstanceValid(stake))
        {
            return;
        }

        stake.PrepareForRemoval();
        simulation.UnregisterStructure(stake);
        worldSite.RemoveStructure(stake);
        stake.QueueFree();
    }

    public override IReadOnlyList<Vector2I> GetActivationReservedWorldCells(MobileFactoryAttachmentDeploymentEvaluation evaluation)
    {
        if (_deployedStakes.Count == 0 && _deployingStakes.Count == 0)
        {
            return evaluation.ReservedWorldCells;
        }

        var occupiedStakeCells = new HashSet<Vector2I>(_deployedStakes.Keys);
        foreach (var stakeCell in _deployingStakes.Keys)
        {
            occupiedStakeCells.Add(stakeCell);
        }

        var filteredCells = new List<Vector2I>(evaluation.ReservedWorldCells.Count);
        for (var index = 0; index < evaluation.ReservedWorldCells.Count; index++)
        {
            var worldCell = evaluation.ReservedWorldCells[index];
            if (!occupiedStakeCells.Contains(worldCell))
            {
                filteredCells.Add(worldCell);
            }
        }

        return filteredCells;
    }

    private List<Vector2I> GetReadyStakeCellsForMining()
    {
        var readyStakeCells = new List<Vector2I>(_deployedStakes.Count);
        foreach (var pair in _deployedStakes)
        {
            var stake = pair.Value;
            if (!GodotObject.IsInstanceValid(stake)
                || stake.IsDestroyed
                || !stake.IsDeploymentComplete)
            {
                continue;
            }

            readyStakeCells.Add(pair.Key);
        }

        readyStakeCells.Sort(static (left, right) =>
        {
            if (left.X != right.X)
            {
                return left.X.CompareTo(right.X);
            }

            return left.Y.CompareTo(right.Y);
        });
        return readyStakeCells;
    }

    private static List<Vector2I> OrderStakeCellsForDeployment(MobileFactoryAttachmentProjection projection, IReadOnlyList<Vector2I> candidateCells)
    {
        var ordered = new List<(Vector2I Cell, int ScoreX, int ScoreY)>(candidateCells.Count);
        for (var index = 0; index < candidateCells.Count; index++)
        {
            var worldCell = candidateCells[index];
            var relativeCell = worldCell - projection.WorldPortCell;
            var localCell = FactoryDirection.RotateOffset(relativeCell, FactoryDirection.Opposite(projection.WorldFacing));
            ordered.Add((worldCell, Mathf.Abs(localCell.X) * 4 + Mathf.Abs(localCell.Y), Mathf.Abs(localCell.Y)));
        }

        ordered.Sort(static (left, right) =>
        {
            if (left.ScoreX != right.ScoreX)
            {
                return left.ScoreX.CompareTo(right.ScoreX);
            }

            if (left.ScoreY != right.ScoreY)
            {
                return left.ScoreY.CompareTo(right.ScoreY);
            }

            if (left.Cell.X != right.Cell.X)
            {
                return left.Cell.X.CompareTo(right.Cell.X);
            }

            return left.Cell.Y.CompareTo(right.Cell.Y);
        });

        var result = new List<Vector2I>(ordered.Count);
        for (var index = 0; index < ordered.Count; index++)
        {
            result.Add(ordered[index].Cell);
        }

        return result;
    }

    private static void ResolveMiningStakePlan(
        GridManager worldSite,
        MobileFactoryAttachmentProjection projection,
        int builtStakeCount,
        out FactoryResourceDepositDefinition? deposit,
        out List<Vector2I> eligibleStakeCells,
        out List<Vector2I> deployedStakeCells)
    {
        deposit = null;
        eligibleStakeCells = new List<Vector2I>();
        deployedStakeCells = new List<Vector2I>();
        var compatibleCandidates = new List<(Vector2I WorldCell, Vector2I LocalCell, FactoryResourceDepositDefinition Deposit)>();

        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var worldCell = projection.WorldCells[index];
            if (worldCell == projection.WorldPortCell)
            {
                continue;
            }

            if (!worldSite.TryGetResourceDeposit(worldCell, out var candidate) || candidate is null)
            {
                continue;
            }

            if (!FactoryResourceCatalog.SupportsExtractor(BuildPrototypeKind.MiningInputPort, candidate.ResourceKind))
            {
                continue;
            }

            var relativeCell = worldCell - projection.WorldPortCell;
            var localCell = FactoryDirection.RotateOffset(relativeCell, FactoryDirection.Opposite(projection.WorldFacing));
            compatibleCandidates.Add((worldCell, localCell, candidate));
        }

        if (compatibleCandidates.Count == 0)
        {
            return;
        }

        var bestCandidate = compatibleCandidates[0];
        var bestScore = Mathf.Abs(bestCandidate.LocalCell.X) * 3 + Mathf.Abs(bestCandidate.LocalCell.Y);
        for (var index = 1; index < compatibleCandidates.Count; index++)
        {
            var candidate = compatibleCandidates[index];
            var score = Mathf.Abs(candidate.LocalCell.X) * 3 + Mathf.Abs(candidate.LocalCell.Y);
            if (score < bestScore)
            {
                bestCandidate = candidate;
                bestScore = score;
            }
        }

        deposit = bestCandidate.Deposit;
        for (var index = 0; index < compatibleCandidates.Count; index++)
        {
            var candidate = compatibleCandidates[index];
            if (candidate.Deposit.Id == deposit.Id)
            {
                eligibleStakeCells.Add(candidate.WorldCell);
            }
        }

        eligibleStakeCells.Sort(static (left, right) =>
        {
            if (left.X != right.X)
            {
                return left.X.CompareTo(right.X);
            }

            return left.Y.CompareTo(right.Y);
        });

        var deployCount = Mathf.Min(Mathf.Max(0, builtStakeCount), eligibleStakeCells.Count);
        for (var index = 0; index < deployCount; index++)
        {
            deployedStakeCells.Add(eligibleStakeCells[index]);
        }
    }
}
