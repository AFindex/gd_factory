using Godot;
using System.Collections.Generic;

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

    public virtual void ApplyWorldPayloadVisualProgress(Node3D root, float progress)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not Node3D payloadRoot)
        {
            return;
        }

        var targetPosition = payloadRoot.GetMeta("payload_target_position", Vector3.Zero).AsVector3();
        payloadRoot.Position = targetPosition * progress;
        payloadRoot.Scale = Vector3.One * Mathf.Max(0.001f, progress);
        payloadRoot.Visible = progress > 0.001f;
    }

    public virtual Vector3 GetWorldConnectorEndWorld(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        var cellCenter = worldGrid.CellToWorld(projection.WorldPortCell);
        var facing = FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(projection.WorldFacing));
        return cellCenter - facing * (worldGrid.CellSize * 0.18f);
    }

    protected override void BuildVisuals()
    {
        var baseColor = AttachmentDefinition.Tint.Darkened(0.2f);
        var accentColor = AttachmentDefinition.Tint;
        var tipColor = AttachmentDefinition.ConnectorColor;

        CreateColoredBox("Pad", new Vector3(CellSize * 0.82f, 0.14f, CellSize * 0.82f), baseColor, new Vector3(0.0f, 0.07f, 0.0f));
        CreateColoredBox("Housing", new Vector3(CellSize * 0.44f, 0.28f, CellSize * 0.64f), accentColor, new Vector3(-CellSize * 0.12f, 0.22f, 0.0f));
        CreateColoredBox("Deck", new Vector3(CellSize * 0.78f, 0.10f, CellSize * 0.28f), accentColor.Lightened(0.04f), new Vector3(0.06f * CellSize, 0.18f, 0.0f));
        CreateColoredBox("Nozzle", new Vector3(CellSize * 0.22f, 0.16f, CellSize * 0.24f), tipColor, new Vector3(CellSize * 0.34f, 0.26f, 0.0f));
        CreateColoredBox("GuideTop", new Vector3(CellSize * 0.30f, 0.05f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(CellSize * 0.12f, 0.34f, -CellSize * 0.18f));
        CreateColoredBox("GuideBottom", new Vector3(CellSize * 0.30f, 0.05f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(CellSize * 0.12f, 0.34f, CellSize * 0.18f));
        CreateColoredBox("Beacon", new Vector3(CellSize * 0.14f, 0.12f, CellSize * 0.14f), tipColor.Lightened(0.22f), new Vector3(-CellSize * 0.20f, 0.42f, 0.0f));
    }

    protected Vector3 EvaluatePortPath(float progress, bool worldToInterior)
    {
        var outer = new Vector3(CellSize * 0.36f, ItemHeight + 0.05f, 0.0f);
        var inner = new Vector3(-CellSize * 0.36f, ItemHeight, 0.0f);
        return worldToInterior ? outer.Lerp(inner, progress) : inner.Lerp(outer, progress);
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
        root.AddChild(payloadRoot);

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadPad",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.92f, 0.16f, FactoryConstants.CellSize * 0.92f) },
            Position = new Vector3(0.0f, 0.08f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint.Darkened(0.22f),
                Roughness = 0.84f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadHousing",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.36f, 0.42f, FactoryConstants.CellSize * 0.68f) },
            Position = new Vector3(-FactoryConstants.CellSize * 0.18f, 0.30f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint,
                Roughness = 0.72f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadDeck",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.86f, 0.10f, FactoryConstants.CellSize * 0.28f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.04f, 0.18f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint.Lightened(0.04f),
                Roughness = 0.66f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadNozzle",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.22f, 0.18f, FactoryConstants.CellSize * 0.24f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.42f, 0.26f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor,
                Roughness = 0.58f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadGuideNorth",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.44f, 0.06f, FactoryConstants.CellSize * 0.08f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.10f, 0.34f, -FactoryConstants.CellSize * 0.22f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor.Lightened(0.12f),
                Roughness = 0.54f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadGuideSouth",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.44f, 0.06f, FactoryConstants.CellSize * 0.08f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.10f, 0.34f, FactoryConstants.CellSize * 0.22f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor.Lightened(0.12f),
                Roughness = 0.54f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadIndicator",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.12f, 0.12f, FactoryConstants.CellSize * 0.12f) },
            Position = new Vector3(-FactoryConstants.CellSize * 0.26f, 0.54f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor.Lightened(0.24f),
                Roughness = 0.42f
            }
        });
    }

    protected void ConfigureStandardPortWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not Node3D payloadRoot)
        {
            return;
        }

        var payloadCenterWorld = worldGrid.CellToWorld(projection.WorldPortCell) + new Vector3(0.0f, 0.02f, 0.0f);
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
        return cellCenter - facing * (worldGrid.CellSize * 0.48f) + new Vector3(0.0f, 0.02f, 0.0f);
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
}

public partial class MobileFactoryOutputPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.OutputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.OutputPort);

    protected override void BuildVisuals()
    {
        var baseColor = AttachmentDefinition.Tint.Darkened(0.22f);
        var accentColor = AttachmentDefinition.Tint;
        var tipColor = AttachmentDefinition.ConnectorColor;

        CreateColoredBox("Pad", new Vector3(CellSize * 0.84f, 0.16f, CellSize * 0.84f), baseColor, new Vector3(0.0f, 0.08f, 0.0f));
        CreateColoredBox("RearHousing", new Vector3(CellSize * 0.34f, 0.34f, CellSize * 0.62f), accentColor, new Vector3(-CellSize * 0.20f, 0.25f, 0.0f));
        CreateColoredBox("FeedDeck", new Vector3(CellSize * 0.82f, 0.10f, CellSize * 0.28f), accentColor.Lightened(0.04f), new Vector3(0.02f * CellSize, 0.18f, 0.0f));
        CreateColoredBox("FrontNozzle", new Vector3(CellSize * 0.22f, 0.18f, CellSize * 0.24f), tipColor, new Vector3(CellSize * 0.34f, 0.27f, 0.0f));
        CreateColoredBox("GuideNorth", new Vector3(CellSize * 0.34f, 0.05f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(CellSize * 0.14f, 0.34f, -CellSize * 0.18f));
        CreateColoredBox("GuideSouth", new Vector3(CellSize * 0.34f, 0.05f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(CellSize * 0.14f, 0.34f, CellSize * 0.18f));
        CreateColoredBox("Indicator", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.12f), tipColor.Lightened(0.22f), new Vector3(-CellSize * 0.26f, 0.44f, 0.0f));
    }

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return IsWorldFlowReady && sourceCell == Cell - FactoryDirection.ToCellOffset(Facing);
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return false;
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = Cell + FactoryDirection.ToCellOffset(Facing);
        return true;
    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        if (!IsConnectedToWorld || BoundWorldSite is null)
        {
            return false;
        }

        if (!IsWorldFlowReady)
        {
            return false;
        }

        return simulation.TrySendItemToSite(this, WorldPortCell, BoundWorldSite, WorldAdjacentCell, state.Item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: false);
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
}

public partial class MobileFactoryInputPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.InputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.InputPort);

    protected override void BuildVisuals()
    {
        var baseColor = AttachmentDefinition.Tint.Darkened(0.22f);
        var accentColor = AttachmentDefinition.Tint;
        var tipColor = AttachmentDefinition.ConnectorColor;

        CreateColoredBox("Pad", new Vector3(CellSize * 0.84f, 0.16f, CellSize * 0.84f), baseColor, new Vector3(0.0f, 0.08f, 0.0f));
        CreateColoredBox("FrontHousing", new Vector3(CellSize * 0.34f, 0.34f, CellSize * 0.62f), accentColor, new Vector3(CellSize * 0.12f, 0.25f, 0.0f));
        CreateColoredBox("FeedDeck", new Vector3(CellSize * 0.82f, 0.10f, CellSize * 0.28f), accentColor.Lightened(0.04f), new Vector3(-0.02f * CellSize, 0.18f, 0.0f));
        CreateColoredBox("RearMouth", new Vector3(CellSize * 0.22f, 0.18f, CellSize * 0.24f), tipColor, new Vector3(-CellSize * 0.34f, 0.27f, 0.0f));
        CreateColoredBox("GuideNorth", new Vector3(CellSize * 0.34f, 0.05f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(-CellSize * 0.14f, 0.34f, -CellSize * 0.18f));
        CreateColoredBox("GuideSouth", new Vector3(CellSize * 0.34f, 0.05f, CellSize * 0.08f), tipColor.Lightened(0.12f), new Vector3(-CellSize * 0.14f, 0.34f, CellSize * 0.18f));
        CreateColoredBox("Indicator", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.12f), tipColor.Lightened(0.22f), new Vector3(CellSize * 0.26f, 0.44f, 0.0f));
    }

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return IsWorldFlowReady && sourceCell == WorldAdjacentCell;
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == Cell - FactoryDirection.ToCellOffset(Facing);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = Cell - FactoryDirection.ToCellOffset(Facing);
        return IsWorldFlowReady;
    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        return simulation.TrySendItem(this, Cell - FactoryDirection.ToCellOffset(Facing), state.Item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: true);
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
}

public partial class MobileFactoryMiningInputPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    private const float MiningCycleSeconds = 0.95f;
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
    private Node3D? _worldStructureRoot;
    private SimulationController? _worldSimulation;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.MiningInputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.MiningInputPort);
    public override float WorldVisualAnimationDurationSeconds => 1.0f;
    public int MaxStakeCapacity => Mathf.Max(0, AttachmentDefinition.ExteriorStencil.Count - 1);
    public int BuiltStakeCount => _builtStakeCount;
    public int DeployedStakeCount => _deployedStakes.Count;
    public int EligibleStakeCount => _eligibleStakeCount;
    public override string ConnectionStateLabel => IsConnectedToWorld
        ? !IsWorldFlowReady
            ? "展开中"
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

        var item = simulation.CreateItem(Kind, FactoryResourceCatalog.GetOutputItemKind(_resourceKind.Value));
        if (TryReceiveProvidedItem(item, WorldPortCell, simulation))
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
            _deployedStakes.Clear();
            for (var index = 0; index < staleStakes.Count; index++)
            {
                var stake = staleStakes[index];
                if (!GodotObject.IsInstanceValid(stake))
                {
                    continue;
                }

                simulation.UnregisterStructure(stake);
                worldSite.RemoveStructure(stake);
                stake.QueueFree();
            }
        }

        _worldStructureRoot = null;
        _worldSimulation = null;
        _miningTimer = 0.0f;
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
        return IsWorldFlowReady;
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
        if (!_deployedStakes.Remove(destroyedStake.Cell))
        {
            return;
        }

        _builtStakeCount = Mathf.Max(0, _builtStakeCount - 1);
        if (_deployedStakes.Count == 0 && IsConnectedToWorld)
        {
            ClearBinding();
        }

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

            if (!GodotObject.IsInstanceValid(staleStake))
            {
                continue;
            }

            simulation.UnregisterStructure(staleStake);
            worldSite.RemoveStructure(staleStake);
            staleStake.QueueFree();
        }

        for (var index = 0; index < evaluation.ActiveWorldCells.Count; index++)
        {
            var worldCell = evaluation.ActiveWorldCells[index];
            if (_deployedStakes.ContainsKey(worldCell))
            {
                continue;
            }

            if (!worldSite.TryGetResourceDeposit(worldCell, out var deposit) || deposit is null)
            {
                continue;
            }

            var facing = FactoryDirection.Opposite(evaluation.Projection.WorldFacing);
            var stake = new MobileFactoryMiningStakeStructure();
            var hubWorld = worldSite.CellToWorld(evaluation.Projection.WorldPortCell) + new Vector3(0.0f, 0.18f, 0.0f);
            stake.ConfigureStake(this, worldSite, worldCell, facing, deposit, hubWorld, $"{ReservationOwnerId}:stake:{worldCell.X}:{worldCell.Y}");
            worldStructureRoot.AddChild(stake);
            worldSite.PlaceStructure(stake);
            simulation.RegisterStructure(stake);
            _deployedStakes[worldCell] = stake;
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
