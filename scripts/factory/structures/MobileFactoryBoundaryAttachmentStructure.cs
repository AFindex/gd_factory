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

        for (var index = 0; index < projection.WorldCells.Count; index++)
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

        var cellCentersWorld = new List<Vector3>(projection.WorldCells.Count);
        var centerWorld = Vector3.Zero;
        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var world = worldGrid.CellToWorld(projection.WorldCells[index]);
            cellCentersWorld.Add(world);
            centerWorld += world;
        }

        if (cellCentersWorld.Count > 0)
        {
            centerWorld /= cellCentersWorld.Count;
        }

        var payloadCenterWorld = centerWorld + new Vector3(0.0f, 0.08f, 0.0f);
        var rootLocalCenter = root.ToLocal(payloadCenterWorld);
        payloadRoot.SetMeta("payload_target_position", rootLocalCenter);
        payloadRoot.Position = rootLocalCenter;
        payloadRoot.Rotation = new Vector3(0.0f, -root.Rotation.Y, 0.0f);
        payloadRoot.Visible = true;

        var minX = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var minZ = float.PositiveInfinity;
        var maxZ = float.NegativeInfinity;

        for (var index = 0; index < cellCentersWorld.Count; index++)
        {
            var localWorldAligned = cellCentersWorld[index] - centerWorld;
            minX = Mathf.Min(minX, localWorldAligned.X);
            maxX = Mathf.Max(maxX, localWorldAligned.X);
            minZ = Mathf.Min(minZ, localWorldAligned.Z);
            maxZ = Mathf.Max(maxZ, localWorldAligned.Z);

            if (payloadRoot.GetNodeOrNull<MeshInstance3D>($"PayloadCell_{index}") is MeshInstance3D cellMesh)
            {
                cellMesh.Position = localWorldAligned + new Vector3(0.0f, 0.08f, 0.0f);
            }
        }

        var spanX = maxX - minX + worldGrid.CellSize;
        var spanZ = maxZ - minZ + worldGrid.CellSize;
        if (payloadRoot.GetNodeOrNull<MeshInstance3D>("PayloadBase") is MeshInstance3D payloadBase
            && payloadBase.Mesh is BoxMesh baseMesh)
        {
            baseMesh.Size = new Vector3(
                Mathf.Max(FactoryConstants.CellSize * 1.20f, spanX - (worldGrid.CellSize * 0.12f)),
                0.22f,
                Mathf.Max(FactoryConstants.CellSize * 1.20f, spanZ - (worldGrid.CellSize * 0.12f)));
            payloadBase.Position = new Vector3(0.0f, 0.22f, 0.0f);
        }

        var nearestIndex = 0;
        var nearestDistanceSquared = float.PositiveInfinity;
        for (var index = 0; index < cellCentersWorld.Count; index++)
        {
            var distanceSquared = cellCentersWorld[index].DistanceSquaredTo(worldGrid.CellToWorld(projection.WorldPortCell));
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestIndex = index;
            }
        }

        var towardsPort = worldGrid.CellToWorld(projection.WorldPortCell) - cellCentersWorld[nearestIndex];
        towardsPort.Y = 0.0f;
        if (towardsPort.LengthSquared() <= 0.0001f)
        {
            towardsPort = -FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(projection.WorldFacing));
        }
        else
        {
            towardsPort = towardsPort.Normalized();
        }

        var featureAnchor = cellCentersWorld[nearestIndex] - centerWorld + towardsPort * (worldGrid.CellSize * 0.28f);
        if (payloadRoot.GetNodeOrNull<MeshInstance3D>("PayloadMast") is MeshInstance3D payloadMast)
        {
            payloadMast.Position = featureAnchor + new Vector3(0.0f, 0.66f, 0.0f);
        }

        if (payloadRoot.GetNodeOrNull<MeshInstance3D>("PayloadHead") is MeshInstance3D payloadHead)
        {
            payloadHead.Position = featureAnchor + towardsPort * (worldGrid.CellSize * 0.20f) + new Vector3(0.0f, 1.04f, 0.0f);
            payloadHead.Rotation = new Vector3(0.0f, Mathf.Atan2(-towardsPort.Z, towardsPort.X), 0.0f);
        }
    }

    public override Vector3 GetWorldConnectorEndWorld(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        var nearestCell = projection.WorldCells[0];
        var nearestDistanceSquared = float.PositiveInfinity;
        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var distanceSquared = projection.WorldCells[index].DistanceSquaredTo(projection.WorldPortCell);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestCell = projection.WorldCells[index];
            }
        }

        var nearestCenter = worldGrid.CellToWorld(nearestCell);
        var towardsPort = worldGrid.CellToWorld(projection.WorldPortCell) - nearestCenter;
        towardsPort.Y = 0.0f;
        if (towardsPort.LengthSquared() <= 0.0001f)
        {
            towardsPort = -FactoryDirection.ToWorldForward(FactoryDirection.ToYRotationRadians(projection.WorldFacing));
        }
        else
        {
            towardsPort = towardsPort.Normalized();
        }

        return nearestCenter + towardsPort * (worldGrid.CellSize * 0.46f) + new Vector3(0.0f, 0.08f, 0.0f);
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
