using Godot;
using System;
using System.Collections.Generic;

public enum MobileFactoryHeavyHandoffPhase
{
    Idle,
    WaitingWorldCargo,
    ReceivingFromWorld,
    BufferedOuter,
    BridgingInward,
    BufferedInner,
    WaitingForUnpacker,
    WaitingForPacker,
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
        CreateColoredBox("BoundaryDeckRailNorth", new Vector3(deckWidth * 0.88f, 0.10f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(0.06f * CellSize, 0.24f, -deckDepth * 0.34f));
        CreateColoredBox("BoundaryDeckRailSouth", new Vector3(deckWidth * 0.88f, 0.10f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(0.06f * CellSize, 0.24f, deckDepth * 0.34f));
        CreateColoredBox("BoundaryPortalNorth", new Vector3(CellSize * 0.16f, 0.44f, CellSize * 0.12f), tipColor, new Vector3(deckWidth * 0.40f, 0.32f, -deckDepth * 0.22f));
        CreateColoredBox("BoundaryPortalSouth", new Vector3(CellSize * 0.16f, 0.44f, CellSize * 0.12f), tipColor, new Vector3(deckWidth * 0.40f, 0.32f, deckDepth * 0.22f));
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
            Position = new Vector3(0.02f * FactoryConstants.CellSize, 0.18f, -FactoryConstants.CellSize * 0.30f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint.Lightened(0.04f),
                Roughness = 0.66f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadGuideSouth",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.72f, 0.06f, FactoryConstants.CellSize * 0.06f) },
            Position = new Vector3(0.02f * FactoryConstants.CellSize, 0.18f, FactoryConstants.CellSize * 0.30f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint.Lightened(0.04f),
                Roughness = 0.58f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadPortalNorth",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.12f, 0.32f, FactoryConstants.CellSize * 0.10f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.42f, 0.22f, -FactoryConstants.CellSize * 0.18f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor,
                Roughness = 0.54f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadPortalSouth",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.12f, 0.32f, FactoryConstants.CellSize * 0.10f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.42f, 0.22f, FactoryConstants.CellSize * 0.18f),
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
            visual?.QueueFree();
            visual = FactoryTransportVisualFactory.CreateVisual(item, FactoryConstants.CellSize, FactoryTransportVisualContext.BoundaryHandoff);
            visual.Name = "TransitPayloadVisual";
            anchor.AddChild(visual);
            anchor.SetMeta("transit_visual_key", visualKey);
        }

        visual.Visible = true;
    }

    protected static void ClearTransitPayloadVisual(Node3D anchor)
    {
        anchor.GetNodeOrNull<Node3D>("TransitPayloadVisual")?.QueueFree();
        anchor.SetMeta("transit_visual_key", string.Empty);
    }
}

public abstract partial class MobileFactoryHeavyPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    private FactoryItem? _outerBufferedItem;
    private FactoryItem? _innerBufferedItem;
    private MobileFactoryHeavyPortTransferMode _transferMode;
    private MobileFactoryHeavyHandoffPhase _handoffPhase = MobileFactoryHeavyHandoffPhase.Idle;
    private float _handoffPhaseProgress;

    protected abstract bool IsInboundHandoff { get; }

    public override int StagedCargoCount => (_outerBufferedItem is null ? 0 : 1)
        + (_innerBufferedItem is null ? 0 : 1)
        + (HasActiveTransitState() ? 1 : 0);
    public FactoryItem? OuterBufferedItem => _outerBufferedItem;
    public FactoryItem? InnerBufferedItem => _innerBufferedItem;
    public MobileFactoryHeavyPortTransferMode TransferMode => _transferMode;
    public MobileFactoryHeavyHandoffPhase HandoffPhase => _handoffPhase;
    public float HandoffPhaseProgress => _handoffPhaseProgress;
    public bool HasBridgeTransfer => HasActiveTransitState();
    public float BridgeTransferProgress => GetActiveTransitState()?.Position ?? 0.0f;
    public override string ConnectionStateLabel => !IsConnectedToWorld
        ? (StagedCargoCount > 0 ? "离线滞留" : "未连接")
        : !IsWorldFlowReady
            ? "展开中"
            : $"已连接 / {DescribePhase(_handoffPhase)}";

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"重载阶段：{DescribePhase(_handoffPhase)}";
        yield return $"外缓存：{DescribeBufferedItem(_outerBufferedItem)}";
        yield return $"桥接位：{DescribeBridgeState()}";
        yield return $"内缓存：{DescribeBufferedItem(_innerBufferedItem)}";
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        base.SimulationStep(simulation, stepSeconds);
        AdvanceHeavyHandoff(simulation);
        RefreshHandoffPhase(simulation);
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        SyncBufferedPayloadVisual("InnerBufferPayloadAnchor", _innerBufferedItem);
        SyncBridgePayloadVisual();
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
        base.ApplyWorldPayloadVisualProgress(root, progress);

        if (root.GetNodeOrNull<Node3D>("OuterBufferPayloadRoot") is Node3D outerBufferRoot
            && outerBufferRoot.GetNodeOrNull<Node3D>("OuterBufferPayloadAnchor") is Node3D outerBufferAnchor)
        {
            if (_outerBufferedItem is null || progress < 0.92f)
            {
                ClearTransitPayloadVisual(outerBufferAnchor);
                outerBufferRoot.Visible = false;
            }
            else
            {
                SyncTransitPayloadVisual(outerBufferAnchor, _outerBufferedItem);
                outerBufferRoot.Visible = true;
            }
        }

        if (root.GetNodeOrNull<Node3D>("ConnectorTransitPayloadRoot") is not Node3D transitRoot
            || transitRoot.GetNodeOrNull<Node3D>("TransitPayloadAnchor") is not Node3D transitAnchor
            || progress < 0.92f
            || !TryResolveWorldTransitPose(root, out var worldTransitItem, out var worldTransitPosition))
        {
            if (root.GetNodeOrNull<Node3D>("ConnectorTransitPayloadRoot") is Node3D staleTransitRoot
                && staleTransitRoot.GetNodeOrNull<Node3D>("TransitPayloadAnchor") is Node3D staleTransitAnchor)
            {
                ClearTransitPayloadVisual(staleTransitAnchor);
                staleTransitRoot.Visible = false;
            }

            return;
        }

        SyncTransitPayloadVisual(transitAnchor, worldTransitItem);
        transitAnchor.Position = worldTransitPosition;
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
        AddChild(new Node3D
        {
            Name = "BridgePayloadAnchor",
            Position = new Vector3(0.0f, ItemHeight + 0.02f, 0.0f)
        });
        AddChild(new Node3D
        {
            Name = "InnerBufferPayloadAnchor",
            Position = new Vector3(-CellSize * 0.74f, ItemHeight + 0.01f, 0.0f)
        });
        AddChild(new Node3D
        {
            Name = "ConverterHandoffAnchor",
            Position = new Vector3(-deckWidth * 0.46f, ItemHeight + 0.04f, 0.0f)
        });
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
        if (HasActiveTransitState())
        {
            return false;
        }

        SpawnTransitState(item, sourceCell, targetCell);
        _transferMode = mode;
        return true;
    }

    protected override void OnTransitItemAccepted(TransitItemState state)
    {
        if (IsInboundHandoff && state.SourceCell == WorldAdjacentCell)
        {
            _transferMode = MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer;
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
                _transferMode = MobileFactoryHeavyPortTransferMode.None;
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                if (_innerBufferedItem is not null)
                {
                    return false;
                }

                _innerBufferedItem = state.Item;
                _transferMode = MobileFactoryHeavyPortTransferMode.None;
                return true;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (_outerBufferedItem is not null)
                {
                    return false;
                }

                _outerBufferedItem = state.Item;
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
        if (state is null)
        {
            item = null;
            progress = 0.0f;
            return false;
        }

        item = state.Item;
        progress = state.Position;
        return true;
    }

    protected virtual bool TryDispatchOuterToWorld(FactoryItem item, SimulationController simulation)
    {
        return false;
    }

    protected abstract void AdvanceHeavyHandoff(SimulationController simulation);
    protected abstract void RefreshHandoffPhase(SimulationController simulation);

    private void SyncBufferedPayloadVisual(string anchorName, FactoryItem? item)
    {
        if (GetNodeOrNull<Node3D>(anchorName) is not Node3D anchor)
        {
            return;
        }

        if (item is null)
        {
            ClearTransitPayloadVisual(anchor);
            anchor.Visible = false;
            return;
        }

        SyncTransitPayloadVisual(anchor, item);
        anchor.Visible = true;
    }

    private void SyncBridgePayloadVisual()
    {
        if (GetNodeOrNull<Node3D>("BridgePayloadAnchor") is not Node3D anchor
            || !TryResolveInteriorTransitPose(out var bridgeItem, out var bridgePosition))
        {
            if (GetNodeOrNull<Node3D>("BridgePayloadAnchor") is Node3D staleAnchor)
            {
                ClearTransitPayloadVisual(staleAnchor);
                staleAnchor.Visible = false;
            }

            return;
        }

        SyncTransitPayloadVisual(anchor, bridgeItem);
        anchor.Position = bridgePosition;
        anchor.Visible = true;
    }

    private bool TryResolveInteriorTransitPose(out FactoryItem item, out Vector3 localPosition)
    {
        item = default!;
        localPosition = Vector3.Zero;
        if (!TryResolveActiveTransitItem(out var activeItem, out var progress) || activeItem is null)
        {
            return false;
        }

        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                if (progress < 0.32f)
                {
                    return false;
                }

                item = activeItem;
                localPosition = EvaluatePortPath(progress, worldToInterior: true);
                return true;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (progress > 0.68f)
                {
                    return false;
                }

                item = activeItem;
                localPosition = EvaluatePortPath(progress, worldToInterior: false);
                return true;
            default:
                return false;
        }
    }

    private bool TryResolveWorldTransitPose(Node3D root, out FactoryItem item, out Vector3 localPosition)
    {
        item = default!;
        localPosition = Vector3.Zero;
        if (!TryResolveActiveTransitItem(out var activeItem, out var progress) || activeItem is null)
        {
            return false;
        }

        var fullLength = root.GetMeta("full_length", 0.0f).AsSingle();
        var mouthExtension = root.GetMeta("mouth_extension", 0.14f).AsSingle();
        var outerZ = fullLength;
        var routeZ = fullLength + mouthExtension + (CellSize * 0.34f);
        var mouthZ = Mathf.Max(0.14f, fullLength * 0.16f);

        switch (_transferMode)
        {
            case MobileFactoryHeavyPortTransferMode.WorldToOuterBuffer:
                item = activeItem;
                localPosition = new Vector3(0.0f, 0.36f, Mathf.Lerp(routeZ, outerZ, progress));
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToInnerBuffer:
                if (progress > 0.42f)
                {
                    return false;
                }

                item = activeItem;
                localPosition = new Vector3(0.0f, 0.36f, Mathf.Lerp(outerZ, mouthZ, progress / 0.42f));
                return true;
            case MobileFactoryHeavyPortTransferMode.InnerToOuterBuffer:
                if (progress < 0.42f)
                {
                    return false;
                }

                item = activeItem;
                localPosition = new Vector3(0.0f, 0.36f, Mathf.Lerp(mouthZ, outerZ, (progress - 0.42f) / 0.58f));
                return true;
            case MobileFactoryHeavyPortTransferMode.OuterToWorld:
                item = activeItem;
                localPosition = new Vector3(0.0f, 0.36f, Mathf.Lerp(outerZ, routeZ, progress));
                return true;
            default:
                return false;
        }
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

    private string DescribeBridgeState()
    {
        return TryResolveActiveTransitItem(out var item, out var progress) && item is not null
            ? $"{DescribePhase(_handoffPhase)} {progress * 100.0f:0}%"
            : "空";
    }

    private static string DescribeBufferedItem(FactoryItem? item)
    {
        return item is null ? "空" : FactoryPresentation.GetItemDisplayName(item);
    }

    protected static string DescribePhase(MobileFactoryHeavyHandoffPhase phase)
    {
        return phase switch
        {
            MobileFactoryHeavyHandoffPhase.WaitingWorldCargo => "等待世界来货",
            MobileFactoryHeavyHandoffPhase.ReceivingFromWorld => "接收世界大包",
            MobileFactoryHeavyHandoffPhase.BufferedOuter => "世界侧缓存",
            MobileFactoryHeavyHandoffPhase.BridgingInward => "向舱内桥接",
            MobileFactoryHeavyHandoffPhase.BufferedInner => "舱内缓存",
            MobileFactoryHeavyHandoffPhase.WaitingForUnpacker => "等待解包舱",
            MobileFactoryHeavyHandoffPhase.WaitingForPacker => "等待封包产出",
            MobileFactoryHeavyHandoffPhase.BridgingOutward => "向世界桥接",
            MobileFactoryHeavyHandoffPhase.WaitingWorldPickup => "等待世界接货",
            MobileFactoryHeavyHandoffPhase.ReleasingToWorld => "释放到世界",
            _ => "待机"
        };
    }
}

public partial class MobileFactoryOutputPortStructure : MobileFactoryHeavyPortStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.OutputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.OutputPort);
    protected override bool IsInboundHandoff => false;

    protected override void BuildVisuals()
    {
        base.BuildVisuals();
        var deckWidth = GetPortDeckWidth();
        var deckDepth = GetPortDeckDepth();
        CreateColoredBox("OutputLatch", new Vector3(deckWidth * 0.24f, 0.16f, deckDepth * 0.42f), AttachmentDefinition.ConnectorColor.Lightened(0.10f), new Vector3(deckWidth * 0.26f, 0.34f, 0.0f));
        BuildHeavyPortAnchors(AttachmentDefinition.ConnectorColor);
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
        if (!IsConnectedToWorld || BoundWorldSite is null)
        {
            return false;
        }

        if (!IsWorldFlowReady)
        {
            return false;
        }

        return simulation.TrySendItemToSite(this, WorldPortCell, BoundWorldSite, WorldAdjacentCell, item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: false);
    }

    public bool CanAcceptPackedBundle(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return IsWorldFlowReady
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactorySiteKind.Interior, item)
            && InnerBufferedItem is null;
    }

    public bool TryAcceptPackedBundle(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanAcceptPackedBundle(item, sourceCell, simulation))
        {
            return false;
        }

        SetInnerBufferedItem(item);
        return true;
    }

    protected override void AdvanceHeavyHandoff(SimulationController simulation)
    {
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
            && CanReleaseToWorld(OuterBufferedItem, simulation))
        {
            var buffered = TakeOuterBufferedItem();
            if (buffered is not null)
            {
                BeginTransfer(MobileFactoryHeavyPortTransferMode.OuterToWorld, buffered, WorldPortCell, WorldAdjacentCell);
            }
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
                SetHandoffPhase(MobileFactoryHeavyHandoffPhase.BridgingOutward, BridgeTransferProgress);
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
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.BufferedInner);
        }
        else
        {
            SetHandoffPhase(MobileFactoryHeavyHandoffPhase.WaitingForPacker);
        }
    }

    private bool CanReleaseToWorld(FactoryItem item, SimulationController simulation)
    {
        return IsConnectedToWorld
            && BoundWorldSite is not null
            && BoundWorldSite.TryGetStructure(WorldAdjacentCell, out var target)
            && target is not null
            && target.CanAcceptItem(item, WorldPortCell, simulation);
    }
}

public partial class MobileFactoryInputPortStructure : MobileFactoryHeavyPortStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.InputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.InputPort);
    protected override bool IsInboundHandoff => true;

    protected override void BuildVisuals()
    {
        base.BuildVisuals();
        var deckWidth = GetPortDeckWidth();
        var deckDepth = GetPortDeckDepth();
        CreateColoredBox("InputReceiver", new Vector3(deckWidth * 0.24f, 0.16f, deckDepth * 0.42f), AttachmentDefinition.ConnectorColor.Lightened(0.10f), new Vector3(-deckWidth * 0.24f, 0.34f, 0.0f));
        BuildHeavyPortAnchors(AttachmentDefinition.ConnectorColor);
    }

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return IsWorldFlowReady && sourceCell == WorldAdjacentCell;
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return false;
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = WorldPortCell;
        return IsWorldFlowReady
            && !HasBridgeTransfer
            && OuterBufferedItem is null
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactorySiteKind.Interior, item);
    }

    protected override void AdvanceHeavyHandoff(SimulationController simulation)
    {
        if (InnerBufferedItem is not null && TryHandOffToUnpacker(simulation))
        {
            SetInnerBufferedItem(null);
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
                SetHandoffPhase(MobileFactoryHeavyHandoffPhase.BridgingInward, BridgeTransferProgress);
                return;
        }

        if (InnerBufferedItem is not null)
        {
            SetHandoffPhase(
                CanConnectedUnpackerAccept(InnerBufferedItem, simulation)
                    ? MobileFactoryHeavyHandoffPhase.BufferedInner
                    : MobileFactoryHeavyHandoffPhase.WaitingForUnpacker);
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

        var outputCells = GetOutputCells();
        for (var index = 0; index < outputCells.Count; index++)
        {
            var targetCell = outputCells[index];
            if (!Site.TryGetStructure(targetCell, out var structure) || structure is not CargoUnpackerStructure unpacker)
            {
                continue;
            }

            var sourceDispatchCell = GetTransferOutputCell(targetCell);
            if (unpacker.TryAcceptHeavyBundle(InnerBufferedItem, sourceDispatchCell, simulation))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanConnectedUnpackerAccept(FactoryItem item, SimulationController simulation)
    {
        var outputCells = GetOutputCells();
        for (var index = 0; index < outputCells.Count; index++)
        {
            var targetCell = outputCells[index];
            if (!Site.TryGetStructure(targetCell, out var structure) || structure is not CargoUnpackerStructure unpacker)
            {
                continue;
            }

            var sourceDispatchCell = GetTransferOutputCell(targetCell);
            if (unpacker.CanAcceptHeavyBundle(item, sourceDispatchCell, simulation))
            {
                return true;
            }
        }

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
