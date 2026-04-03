using Godot;
using System.Collections.Generic;

public abstract partial class MobileFactoryBoundaryAttachmentStructure : FlowTransportStructure
{
    private GridManager? _worldSite;
    private MobileFactoryAttachmentProjection? _projection;

    public abstract MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition { get; }
    public MobileFactoryAttachmentChannelType ChannelType => AttachmentDefinition.ChannelType;
    public bool IsConnectedToWorld => _worldSite is not null && _projection is not null;
    public GridManager? BoundWorldSite => _worldSite;
    public MobileFactoryAttachmentProjection? Projection => _projection;
    public Vector2I WorldPortCell => _projection?.WorldPortCell ?? Vector2I.Zero;
    public Vector2I WorldAdjacentCell => _projection?.WorldAdjacentCell ?? Vector2I.Zero;
    public FacingDirection WorldFacing => _projection?.WorldFacing ?? Facing;
    public virtual string ConnectionStateLabel => IsConnectedToWorld ? "已连接" : TransitItemCount > 0 ? "阻塞" : "未连接";

    public virtual bool CanBindToWorld(GridManager worldSite, MobileFactoryAttachmentProjection projection, out string reason)
    {
        reason = string.Empty;
        return true;
    }

    public void BindToWorld(GridManager worldSite, MobileFactoryAttachmentProjection projection)
    {
        _worldSite = worldSite;
        _projection = projection;
        OnWorldBindingChanged();
    }

    public void ClearBinding()
    {
        _worldSite = null;
        _projection = null;
        OnWorldBindingChanged();
    }

    public override string Description => AttachmentDefinition.Description;

    public virtual void BuildWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
    }

    public virtual void ConfigureWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
    }

    protected override void BuildVisuals()
    {
        var baseColor = AttachmentDefinition.Tint.Darkened(0.2f);
        var accentColor = AttachmentDefinition.Tint;
        var tipColor = AttachmentDefinition.ConnectorColor;

        CreateColoredBox("Pad", new Vector3(CellSize * 0.76f, 0.16f, CellSize * 0.76f), baseColor, new Vector3(0.0f, 0.08f, 0.0f));
        CreateColoredBox("Stem", new Vector3(CellSize * 0.64f, 0.18f, CellSize * 0.24f), accentColor, new Vector3(CellSize * 0.10f, 0.18f, 0.0f));
        CreateColoredBox("Mouth", new Vector3(CellSize * 0.18f, 0.14f, CellSize * 0.24f), tipColor, new Vector3(CellSize * 0.34f, 0.24f, 0.0f));
        CreateColoredBox("Beacon", new Vector3(CellSize * 0.16f, 0.10f, CellSize * 0.16f), tipColor.Lightened(0.12f), new Vector3(0.0f, 0.30f, 0.0f));
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
}

public partial class MobileFactoryOutputPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.OutputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.OutputPort);

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == Cell - FactoryDirection.ToCellOffset(Facing);
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

        return simulation.TrySendItemToSite(this, WorldPortCell, BoundWorldSite, WorldAdjacentCell, state.Item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: false);
    }
}

public partial class MobileFactoryInputPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.InputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.InputPort);

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return IsConnectedToWorld && sourceCell == WorldAdjacentCell;
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == Cell - FactoryDirection.ToCellOffset(Facing);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = Cell - FactoryDirection.ToCellOffset(Facing);
        return IsConnectedToWorld;
    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        return simulation.TrySendItem(this, Cell - FactoryDirection.ToCellOffset(Facing), state.Item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        return EvaluatePortPath(progress, worldToInterior: true);
    }
}

public partial class MobileFactoryMiningInputPortStructure : MobileFactoryBoundaryAttachmentStructure
{
    private const float MiningCycleSeconds = 0.95f;

    private FactoryResourceKind? _resourceKind;
    private string _depositName = "未绑定矿区";
    private float _miningTimer;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.MiningInputPort;
    public override MobileFactoryBoundaryAttachmentDefinition AttachmentDefinition => MobileFactoryBoundaryAttachmentCatalog.Get(BuildPrototypeKind.MiningInputPort);
    public override string ConnectionStateLabel => !IsConnectedToWorld
        ? TransitItemCount > 0 ? "阻塞" : "未连接"
        : _resourceKind.HasValue
            ? TransitItemCount > 0 ? "采集中" : "就绪"
            : "待机";

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
        if (Projection is not null)
        {
            yield return $"外侧占地：{Projection.WorldCells.Count} 格";
        }
    }

    public override bool CanBindToWorld(GridManager worldSite, MobileFactoryAttachmentProjection projection, out string reason)
    {
        reason = string.Empty;
        FactoryResourceDepositDefinition? deposit = null;

        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var worldCell = projection.WorldCells[index];
            if (!worldSite.TryGetResourceDeposit(worldCell, out var candidate) || candidate is null)
            {
                reason = "采矿输入端口的世界侧占地必须完整覆盖同一片矿区。";
                return false;
            }

            if (!FactoryResourceCatalog.SupportsExtractor(Kind, candidate.ResourceKind))
            {
                reason = $"{candidate.DisplayName} 不能由当前采矿端口开采。";
                return false;
            }

            if (deposit is null)
            {
                deposit = candidate;
                continue;
            }

            if (deposit.Id != candidate.Id)
            {
                reason = "采矿输入端口当前不能跨越多片矿区部署。";
                return false;
            }
        }

        return deposit is not null;
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        base.SimulationStep(simulation, stepSeconds);

        if (!IsConnectedToWorld || !_resourceKind.HasValue)
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

    public override void BuildWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not null)
        {
            return;
        }

        var payloadRoot = new Node3D { Name = "WorldPayloadRoot" };
        root.AddChild(payloadRoot);

        for (var index = 0; index < 4; index++)
        {
            payloadRoot.AddChild(new MeshInstance3D
            {
                Name = $"PayloadCell_{index}",
                Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.82f, 0.16f, FactoryConstants.CellSize * 0.82f) },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = AttachmentDefinition.Tint.Darkened(0.12f),
                    Roughness = 0.82f
                }
            });
        }

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadBase",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 1.34f, 0.22f, FactoryConstants.CellSize * 1.34f) },
            Position = new Vector3(0.0f, 0.22f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint,
                Roughness = 0.76f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadMast",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.10f,
                BottomRadius = 0.14f,
                Height = 0.78f
            },
            Position = new Vector3(0.0f, 0.66f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor,
                Roughness = 0.64f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadHead",
            Mesh = new BoxMesh { Size = new Vector3(0.44f, 0.24f, 0.72f) },
            Position = new Vector3(0.0f, 1.04f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor.Lightened(0.12f),
                Roughness = 0.58f
            }
        });
    }

    public override void ConfigureWorldPayload(Node3D root, GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        if (root.GetNodeOrNull<Node3D>("WorldPayloadRoot") is not Node3D payloadRoot)
        {
            return;
        }

        var cellPositions = new List<Vector3>(projection.WorldCells.Count);
        var center = Vector3.Zero;
        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var local = root.ToLocal(worldGrid.CellToWorld(projection.WorldCells[index]) + new Vector3(0.0f, 0.08f, 0.0f));
            cellPositions.Add(local);
            center += local;
        }

        if (cellPositions.Count > 0)
        {
            center /= cellPositions.Count;
        }

        payloadRoot.SetMeta("payload_target_position", center);
        payloadRoot.Position = center;
        payloadRoot.Visible = true;

        for (var index = 0; index < cellPositions.Count; index++)
        {
            if (payloadRoot.GetNodeOrNull<MeshInstance3D>($"PayloadCell_{index}") is MeshInstance3D cellMesh)
            {
                cellMesh.Position = cellPositions[index] - center;
                cellMesh.Position += new Vector3(0.0f, 0.08f, 0.0f);
            }
        }
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = Cell - FactoryDirection.ToCellOffset(Facing);
        return IsConnectedToWorld;
    }

    protected override bool CanReceiveProvidedFrom(Vector2I sourceCell)
    {
        if (!IsConnectedToWorld || Projection is null)
        {
            return false;
        }

        for (var index = 0; index < Projection.WorldCells.Count; index++)
        {
            if (Projection.WorldCells[index] == sourceCell)
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
        _miningTimer = 0.0f;

        if (BoundWorldSite is null || Projection is null)
        {
            _resourceKind = null;
            _depositName = "未绑定矿区";
            return;
        }

        if (!TryResolveBoundDeposit(BoundWorldSite, Projection, out var deposit) || deposit is null)
        {
            _resourceKind = null;
            _depositName = "未绑定矿区";
            return;
        }

        _resourceKind = deposit.ResourceKind;
        _depositName = deposit.DisplayName;
    }

    private static bool TryResolveBoundDeposit(
        GridManager worldSite,
        MobileFactoryAttachmentProjection projection,
        out FactoryResourceDepositDefinition? deposit)
    {
        deposit = null;
        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            if (!worldSite.TryGetResourceDeposit(projection.WorldCells[index], out var candidate) || candidate is null)
            {
                return false;
            }

            deposit ??= candidate;
            if (deposit.Id != candidate.Id)
            {
                return false;
            }
        }

        return deposit is not null;
    }
}
