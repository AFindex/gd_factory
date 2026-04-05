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
            var stakeRoot = new Node3D
            {
                Name = $"PayloadStake_{index}"
            };

            stakeRoot.AddChild(new MeshInstance3D
            {
                Name = "StakePad",
                Mesh = new CylinderMesh
                {
                    TopRadius = FactoryConstants.CellSize * 0.14f,
                    BottomRadius = FactoryConstants.CellSize * 0.18f,
                    Height = 0.16f
                },
                Position = new Vector3(0.0f, 0.08f, 0.0f),
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = AttachmentDefinition.Tint.Darkened(0.18f),
                    Roughness = 0.86f
                }
            });

            stakeRoot.AddChild(new MeshInstance3D
            {
                Name = "StakeMast",
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.06f,
                    BottomRadius = 0.08f,
                    Height = 0.56f
                },
                Position = new Vector3(0.0f, 0.40f, 0.0f),
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = AttachmentDefinition.Tint,
                    Roughness = 0.70f
                }
            });

            stakeRoot.AddChild(new MeshInstance3D
            {
                Name = "StakeHead",
                Mesh = new BoxMesh { Size = new Vector3(0.26f, 0.16f, 0.44f) },
                Position = new Vector3(0.0f, 0.70f, 0.0f),
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = AttachmentDefinition.ConnectorColor,
                    Roughness = 0.56f
                }
            });

            stakeRoot.AddChild(new MeshInstance3D
            {
                Name = "StakeTip",
                Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.10f, 0.26f) },
                Position = new Vector3(0.0f, 0.62f, 0.20f),
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = AttachmentDefinition.ConnectorColor.Lightened(0.14f),
                    Roughness = 0.48f
                }
            });

            payloadRoot.AddChild(stakeRoot);
        }

        for (var rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            payloadRoot.AddChild(new MeshInstance3D
            {
                Name = $"PayloadRowLink_{rowIndex}",
                Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.08f, 0.001f) },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = AttachmentDefinition.ConnectorColor.Darkened(0.10f),
                    Roughness = 0.64f
                }
            });

            payloadRoot.AddChild(new MeshInstance3D
            {
                Name = $"PayloadCollectorLink_{rowIndex}",
                Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.08f, 0.001f) },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = AttachmentDefinition.ConnectorColor.Darkened(0.02f),
                    Roughness = 0.60f
                }
            });
        }

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadRelayBase",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.62f, 0.18f, FactoryConstants.CellSize * 0.82f) },
            Position = new Vector3(0.0f, 0.12f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint.Darkened(0.20f),
                Roughness = 0.82f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadRelayBody",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.34f, 0.34f, FactoryConstants.CellSize * 0.64f) },
            Position = new Vector3(-FactoryConstants.CellSize * 0.08f, 0.30f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.ConnectorColor,
                Roughness = 0.64f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadRelayDeck",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.76f, 0.10f, FactoryConstants.CellSize * 0.24f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.02f, 0.22f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = AttachmentDefinition.Tint,
                Roughness = 0.62f
            }
        });

        payloadRoot.AddChild(new MeshInstance3D
        {
            Name = "PayloadRelayNozzle",
            Mesh = new BoxMesh { Size = new Vector3(FactoryConstants.CellSize * 0.18f, 0.16f, FactoryConstants.CellSize * 0.22f) },
            Position = new Vector3(FactoryConstants.CellSize * 0.34f, 0.30f, 0.0f),
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
        var localPositions = new Vector3[projection.WorldCells.Count];
        var localCells = new Vector2I[projection.WorldCells.Count];
        var nearColumnByRow = new int[3] { -1, -1, -1 };
        var farColumnByRow = new int[3] { -1, -1, -1 };
        var relayColumn = int.MinValue;
        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var world = worldGrid.CellToWorld(projection.WorldCells[index]);
            cellCentersWorld.Add(world);
            centerWorld += world;

            var relativeCell = projection.WorldCells[index] - projection.WorldPortCell;
            var localCell = FactoryDirection.RotateOffset(relativeCell, FactoryDirection.Opposite(projection.WorldFacing));
            localCells[index] = localCell;
            if (projection.WorldCells[index] == projection.WorldPortCell)
            {
                relayColumn = localCell.X;
            }
        }

        if (relayColumn == int.MinValue)
        {
            relayColumn = localCells.Length > 0 ? localCells[0].X : 0;
        }

        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var localCell = localCells[index];
            var row = Mathf.Clamp(localCell.Y + 1, 0, 2);
            if (localCell.X == relayColumn)
            {
                nearColumnByRow[row] = index;
            }
            else
            {
                farColumnByRow[row] = index;
            }
        }

        if (cellCentersWorld.Count > 0)
        {
            centerWorld /= cellCentersWorld.Count;
        }

        for (var index = 0; index < cellCentersWorld.Count; index++)
        {
            localPositions[index] = cellCentersWorld[index] - centerWorld;
        }

        var payloadCenterWorld = centerWorld + new Vector3(0.0f, 0.08f, 0.0f);
        var rootLocalCenter = root.ToLocal(payloadCenterWorld);
        payloadRoot.SetMeta("payload_target_position", rootLocalCenter);
        payloadRoot.Position = rootLocalCenter;
        payloadRoot.Rotation = new Vector3(0.0f, -root.Rotation.Y, 0.0f);
        payloadRoot.Visible = true;

        var relayIndex = -1;
        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            if (projection.WorldCells[index] == projection.WorldPortCell)
            {
                relayIndex = index;
                break;
            }
        }

        if (relayIndex < 0)
        {
            relayIndex = nearColumnByRow[1] >= 0 ? nearColumnByRow[1] : 0;
        }

        var relayWorld = cellCentersWorld[relayIndex];
        var relayLocal = localPositions[relayIndex];

        var towardsFactory = FactoryDirection.ToWorldForward(
            FactoryDirection.ToYRotationRadians(FactoryDirection.Opposite(projection.WorldFacing)));
        var relayFacing = Mathf.Atan2(-towardsFactory.Z, towardsFactory.X);


        if (payloadRoot.GetNodeOrNull<MeshInstance3D>("PayloadRelayBase") is MeshInstance3D relayBase)
        {
            relayBase.Position = relayLocal + new Vector3(0.0f, 0.12f, 0.0f);
            relayBase.Rotation = new Vector3(0.0f, relayFacing, 0.0f);
        }

        if (payloadRoot.GetNodeOrNull<MeshInstance3D>("PayloadRelayBody") is MeshInstance3D relayBody)
        {
            relayBody.Position = relayLocal + new Vector3(-FactoryConstants.CellSize * 0.08f, 0.30f, 0.0f);
            relayBody.Rotation = new Vector3(0.0f, relayFacing, 0.0f);
        }

        if (payloadRoot.GetNodeOrNull<MeshInstance3D>("PayloadRelayDeck") is MeshInstance3D relayDeck)
        {
            relayDeck.Position = relayLocal + new Vector3(FactoryConstants.CellSize * 0.02f, 0.22f, 0.0f);
            relayDeck.Rotation = new Vector3(0.0f, relayFacing, 0.0f);
        }

        if (payloadRoot.GetNodeOrNull<MeshInstance3D>("PayloadRelayNozzle") is MeshInstance3D relayNozzle)
        {
            relayNozzle.Position = relayLocal + towardsFactory * (worldGrid.CellSize * 0.24f) + new Vector3(0.0f, 0.30f, 0.0f);
            relayNozzle.Rotation = new Vector3(0.0f, relayFacing, 0.0f);
        }

        for (var index = 0; index < projection.WorldCells.Count; index++)
        {
            var localWorldAligned = localPositions[index];
            Vector3 desiredDirection;
            var rowIndex = -1;
            var isNearColumn = false;
            for (var row = 0; row < 3; row++)
            {
                if (nearColumnByRow[row] == index)
                {
                    rowIndex = row;
                    isNearColumn = true;
                    break;
                }
                if (farColumnByRow[row] == index)
                {
                    rowIndex = row;
                    break;
                }
            }

            if (!isNearColumn && rowIndex >= 0 && nearColumnByRow[rowIndex] >= 0)
            {
                desiredDirection = cellCentersWorld[nearColumnByRow[rowIndex]] - cellCentersWorld[index];
            }
            else if (rowIndex >= 0 && rowIndex != 1)
            {
                desiredDirection = relayWorld - cellCentersWorld[index];
            }
            else
            {
                desiredDirection = towardsFactory;
            }

            desiredDirection.Y = 0.0f;
            if (desiredDirection.LengthSquared() <= 0.0001f)
            {
                desiredDirection = towardsFactory;
            }
            else
            {
                desiredDirection = desiredDirection.Normalized();
            }

            var relayYaw = Mathf.Atan2(-desiredDirection.Z, desiredDirection.X);

            if (payloadRoot.GetNodeOrNull<Node3D>($"PayloadStake_{index}") is Node3D stakeRoot)
            {
                stakeRoot.Position = localWorldAligned;
                stakeRoot.Rotation = new Vector3(0.0f, relayYaw, 0.0f);
            }
        }

        for (var rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            var leftIndex = farColumnByRow[rowIndex];
            var rightIndex = nearColumnByRow[rowIndex];
            if (leftIndex < 0 || rightIndex < 0)
            {
                if (payloadRoot.GetNodeOrNull<MeshInstance3D>($"PayloadRowLink_{rowIndex}") is MeshInstance3D hiddenRow)
                {
                    hiddenRow.Visible = false;
                }

                if (payloadRoot.GetNodeOrNull<MeshInstance3D>($"PayloadCollectorLink_{rowIndex}") is MeshInstance3D hiddenCollector)
                {
                    hiddenCollector.Visible = false;
                }

                continue;
            }

            var leftLocal = localPositions[leftIndex];
            var rightLocal = localPositions[rightIndex];
            var rowVector = rightLocal - leftLocal;
            var rowLength = Mathf.Max(0.12f, new Vector2(rowVector.X, rowVector.Z).Length());

            if (payloadRoot.GetNodeOrNull<MeshInstance3D>($"PayloadRowLink_{rowIndex}") is MeshInstance3D rowLink)
            {
                rowLink.Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.08f, rowLength) };
                rowLink.Position = (leftLocal + rightLocal) * 0.5f + new Vector3(0.0f, 0.24f, 0.0f);
                rowLink.Rotation = new Vector3(0.0f, Mathf.Atan2(rowVector.X, rowVector.Z), 0.0f);
                rowLink.Visible = true;
            }

            if (rowIndex == 1)
            {
                if (payloadRoot.GetNodeOrNull<MeshInstance3D>($"PayloadCollectorLink_{rowIndex}") is MeshInstance3D hiddenCollector)
                {
                    hiddenCollector.Visible = false;
                }
                continue;
            }

            var relayVector = relayLocal - rightLocal;
            var relayLength = Mathf.Max(0.12f, new Vector2(relayVector.X, relayVector.Z).Length());
            if (payloadRoot.GetNodeOrNull<MeshInstance3D>($"PayloadCollectorLink_{rowIndex}") is MeshInstance3D collectorLink)
            {
                collectorLink.Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.08f, relayLength) };
                collectorLink.Position = (rightLocal + relayLocal) * 0.5f + new Vector3(0.0f, 0.26f, 0.0f);
                collectorLink.Rotation = new Vector3(0.0f, Mathf.Atan2(relayVector.X, relayVector.Z), 0.0f);
                collectorLink.Visible = true;
            }
        }
    }

    public override Vector3 GetWorldConnectorEndWorld(GridManager worldGrid, MobileFactoryAttachmentProjection projection)
    {
        var relayCenter = worldGrid.CellToWorld(projection.WorldPortCell);
        var towardsFactory = FactoryDirection.ToWorldForward(
            FactoryDirection.ToYRotationRadians(FactoryDirection.Opposite(projection.WorldFacing)));
        return relayCenter + towardsFactory * (worldGrid.CellSize * 0.34f) + new Vector3(0.0f, 0.30f, 0.0f);
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
