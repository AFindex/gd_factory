using Godot;
using System;
using System.Collections.Generic;

public abstract partial class FactoryStructure : Node3D, IFactoryInspectable, IFactoryStructureDetailProvider, IFactoryInventoryEndpointProvider
{
    private bool _visualsBuilt;
    private Node3D? _structureVisualRoot;
    private FactoryStructureVisualProfile? _visualProfile;
    private FactoryStructureVisualController? _visualController;
    private Node? _currentVisualParent;
    private Node3D? _combatOverlayRoot;
    private MeshInstance3D? _healthBarBackground;
    private MeshInstance3D? _healthBarFill;
    private MeshInstance3D? _focusRing;
    private StandardMaterial3D? _healthBarFillMaterial;
    private StandardMaterial3D? _focusRingMaterial;
    private bool _isHovered;
    private bool _isSelected;
    private double _recentDamageTimer;
    private float _currentHealth;
    private bool _ghostVisualApplied;
    private Color _ghostTint = new(-1.0f, -1.0f, -1.0f, -1.0f);

    protected float CellSize { get; private set; } = FactoryConstants.CellSize;
    protected FactoryStructureFootprint Footprint { get; private set; } = FactoryStructureFootprint.SingleCell;
    protected FactoryStructureVisualController? VisualController => _visualController;
    protected FactorySiteKind SiteKind => FactoryIndustrialStandards.ResolveSiteKind(Site);

    public IFactorySite Site { get; private set; } = null!;
    public Vector2I Cell { get; private set; }
    public FacingDirection Facing { get; protected set; }
    public string ReservationOwnerId { get; private set; } = string.Empty;
    public string DisplayName => FactoryIndustrialStandards.GetSiteAwarePrototypeLabel(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site));
    public virtual string InspectionTitle => $"{DisplayName} ({Cell.X}, {Cell.Y})";
    public virtual float MaxHealth => 36.0f;
    public float CurrentHealth => _currentHealth;
    public bool IsDestroyed { get; private set; }
    public bool IsUnderAttack => _recentDamageTimer > 0.0;
    public FactoryStructureVisualSourceKind VisualSourceKind => _visualController?.SourceKind ?? FactoryStructureVisualSourceKind.GenericPlaceholder;
    public virtual float CombatRadius => Footprint.GetCombatRadius(CellSize, Facing);

    public abstract BuildPrototypeKind Kind { get; }
    public abstract string Description { get; }
    public virtual bool IsTransportNode => false;

    public void Configure(IFactorySite site, Vector2I cell, FacingDirection facing, string? reservationOwnerId = null, FactoryStructureFootprint? footprint = null)
    {
        Site = site;
        Cell = cell;
        Facing = facing;
        CellSize = site.CellSize;
        Footprint = footprint ?? FactoryStructureFootprint.SingleCell;
        _currentHealth = MaxHealth;
        ReservationOwnerId = string.IsNullOrWhiteSpace(reservationOwnerId)
            ? $"structure:{GetInstanceId()}"
            : reservationOwnerId;
        RefreshPlacement();
    }

    public override void _Ready()
    {
        EnsureVisualsBuilt();
    }

    public virtual void SimulationStep(SimulationController simulation, double stepSeconds)
    {
    }

    public virtual void UpdateVisuals(float tickAlpha)
    {
    }

    public virtual void SetPowerRangeVisible(bool visible)
    {
    }

    public virtual bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return false;
    }

    public virtual bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanReceiveFrom(sourceCell);
    }

    public virtual void RefreshPlacement()
    {
        var anchorWorld = Site.CellToWorld(Cell);
        var centerOffset = Footprint.GetWorldCenterOffset(CellSize, Facing);
        var rotatedCenterOffset = centerOffset.Rotated(Vector3.Up, Site.WorldRotationRadians);
        Position = anchorWorld + rotatedCenterOffset;
        Rotation = new Vector3(0.0f, Site.WorldRotationRadians + FactoryDirection.ToYRotationRadians(Facing), 0.0f);
        Visible = Site.IsVisible;
        SyncCombatOverlayPlacement();
    }

    public virtual IEnumerable<Vector2I> GetOccupiedCells()
    {
        foreach (var cell in Footprint.ResolveOccupiedCells(Cell, Facing))
        {
            yield return cell;
        }
    }

    public virtual IEnumerable<string> GetInspectionLines()
    {
        yield return $"生命：{CurrentHealth:0}/{MaxHealth:0}";
        yield return $"状态：{(IsDestroyed ? "已摧毁" : IsUnderAttack ? "遭受攻击" : "稳定")}";
        yield return Description;
    }

    public virtual FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        return new FactoryStructureDetailModel(
            InspectionTitle,
            $"{DisplayName} 详情",
            summaryLines);
    }

    public virtual bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack = false)
    {
        return false;
    }

    public virtual bool TrySetDetailRecipe(string recipeId)
    {
        return false;
    }

    public virtual bool TryInvokeDetailAction(string actionId)
    {
        return false;
    }

    public virtual bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        endpoint = default;
        return false;
    }

    public virtual IReadOnlyDictionary<string, string> CaptureBlueprintConfiguration()
    {
        return new Dictionary<string, string>();
    }

    public virtual string? CaptureMapRecipeId()
    {
        return null;
    }

    public virtual IReadOnlyList<FactoryMapSeedItemEntry> CaptureMapSeedItems()
    {
        return System.Array.Empty<FactoryMapSeedItemEntry>();
    }

    public virtual bool TryApplyMapRecipe(string recipeId)
    {
        return string.IsNullOrWhiteSpace(recipeId);
    }

    public virtual bool ApplyBlueprintConfiguration(IReadOnlyDictionary<string, string> configuration)
    {
        return configuration.Count == 0;
    }

    public static string BuildRuntimeStructureKey(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        return FactoryRuntimeSnapshotValues.BuildStructureKey(kind, cell, facing);
    }

    public virtual string GetRuntimeStructureKey()
    {
        return BuildRuntimeStructureKey(Kind, Cell, Facing);
    }

    public virtual FactoryStructureRuntimeSnapshot CaptureRuntimeSnapshot()
    {
        var snapshot = new FactoryStructureRuntimeSnapshot
        {
            StructureKey = GetRuntimeStructureKey(),
            SiteId = Site.SiteId,
            Kind = Kind,
            Cell = FactoryRuntimeInt2.FromVector2I(Cell),
            Facing = Facing,
            CurrentHealth = CurrentHealth
        };
        CaptureRuntimeState(snapshot);
        return snapshot;
    }

    public virtual void ApplyRuntimeSnapshot(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        if (snapshot.Kind != Kind
            || snapshot.Cell.ToVector2I() != Cell
            || snapshot.Facing != Facing
            || !string.Equals(snapshot.StructureKey, GetRuntimeStructureKey(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Runtime snapshot '{snapshot.StructureKey}' does not match structure '{GetRuntimeStructureKey()}'.");
        }

        RestoreCurrentHealth(snapshot.CurrentHealth);
        ApplyRuntimeState(snapshot, simulation);
        SyncCombatVisuals(0.0f);
        SyncVisualPresentation(0.0f);
    }

    public Vector2I GetOutputCell()
    {
        return Footprint.ResolveOutputCell(Cell, Facing);
    }

    public IReadOnlyList<Vector2I> GetOutputCells()
    {
        return Footprint.ResolveOutputCells(Cell, Facing);
    }

    public Vector2I GetInputCell()
    {
        return Footprint.ResolveInputCell(Cell, Facing);
    }

    public IReadOnlyList<Vector2I> GetInputCells()
    {
        return Footprint.ResolveInputCells(Cell, Facing);
    }

    public bool AcceptsFrom(Vector2I sourceCell)
    {
        return CanReceiveFrom(sourceCell);
    }

    public virtual bool CanReceiveFrom(Vector2I sourceCell)
    {
        var inputCells = GetInputCells();
        for (var index = 0; index < inputCells.Count; index++)
        {
            if (inputCells[index] == sourceCell)
            {
                return true;
            }
        }

        return false;
    }

    public virtual bool CanOutputTo(Vector2I targetCell)
    {
        var outputCells = GetOutputCells();
        for (var index = 0; index < outputCells.Count; index++)
        {
            if (outputCells[index] == targetCell)
            {
                return true;
            }
        }

        return false;
    }

    public virtual Vector2I GetTransferOutputCell(Vector2I targetCell)
    {
        return Footprint.ResolveOutputTransferCell(Cell, Facing, targetCell);
    }

    public void AdvanceCombatState(double stepSeconds)
    {
        _recentDamageTimer = Mathf.Max(0.0f, (float)(_recentDamageTimer - stepSeconds));
    }

    public virtual void ApplyDamage(float damage, SimulationController simulation)
    {
        if (IsDestroyed)
        {
            return;
        }

        _currentHealth = Mathf.Max(0.0f, _currentHealth - Mathf.Max(0.0f, damage));
        _recentDamageTimer = FactoryConstants.StructureDamageFlashSeconds;

        if (_currentHealth <= 0.0f)
        {
            IsDestroyed = true;
            simulation.QueueStructureDestruction(this);
        }
    }

    public void SetCombatFocus(bool isHovered, bool isSelected)
    {
        _isHovered = isHovered;
        _isSelected = isSelected;
    }

    public void EnsureVisualsBuilt()
    {
        if (_visualsBuilt)
        {
            return;
        }

        _visualsBuilt = true;
        _structureVisualRoot = new Node3D { Name = "StructureVisualRoot" };
        AddChild(_structureVisualRoot);
        _visualProfile ??= CreateVisualProfile();
        _visualController = FactoryStructureVisualFactory.BuildForStructure(this, _visualProfile, _structureVisualRoot, CellSize);
        BuildCombatVisuals();
        SyncVisualPresentation(0.0f);
        SyncCombatVisuals(0.0f);
    }

    public void ApplyGhostVisual(Color tint)
    {
        EnsureVisualsBuilt();
        ProcessMode = ProcessModeEnum.Disabled;

        if (_combatOverlayRoot is not null)
        {
            _combatOverlayRoot.Visible = false;
        }

        if (_ghostVisualApplied && _ghostTint.IsEqualApprox(tint))
        {
            return;
        }

        _ghostVisualApplied = true;
        _ghostTint = tint;
        ApplyGhostTintRecursive(this, tint);
    }

    public void SyncVisualPresentation(float tickAlpha)
    {
        if (_visualController is null)
        {
            return;
        }

        _visualProfile ??= CreateVisualProfile();
        _visualController.ApplyState(CreateVisualState(tickAlpha), _visualProfile, tickAlpha);
    }

    public void SyncCombatVisuals(float tickAlpha)
    {
        if (_combatOverlayRoot is null || _healthBarBackground is null || _healthBarFill is null || _focusRing is null || _healthBarFillMaterial is null || _focusRingMaterial is null)
        {
            return;
        }

        SyncCombatOverlayPlacement();
        var healthRatio = Mathf.Clamp(CurrentHealth / MaxHealth, 0.0f, 1.0f);
        var showBar = IsUnderAttack || _isHovered || _isSelected || healthRatio < 0.999f;
        _healthBarBackground.Visible = showBar;
        _healthBarFill.Visible = showBar;
        _focusRing.Visible = IsUnderAttack || _isHovered || _isSelected;

        _healthBarFill.Scale = new Vector3(Mathf.Max(0.01f, healthRatio), 1.0f, 1.0f);
        _healthBarFill.Position = new Vector3((-0.30f * CellSize) + (healthRatio * 0.30f * CellSize), FactoryConstants.StructureHealthBarHeight, 0.0f);
        _healthBarFillMaterial.AlbedoColor = healthRatio > 0.55f
            ? new Color("4ADE80")
            : healthRatio > 0.25f
                ? new Color("FACC15")
                : new Color("F87171");

        _focusRingMaterial.AlbedoColor = IsUnderAttack
            ? new Color(1.0f, 0.35f, 0.35f, 0.48f)
            : _isSelected
                ? new Color(0.35f, 0.85f, 1.0f, 0.36f)
                : new Color(0.94f, 0.81f, 0.32f, 0.32f);
    }

    internal void BeginVisualBuildScope(Node parent)
    {
        _currentVisualParent = parent;
    }

    internal void EndVisualBuildScope()
    {
        _currentVisualParent = null;
    }

    internal FactoryStructureVisualController CreateDetachedVisualControllerForTesting()
    {
        _visualProfile ??= CreateVisualProfile();
        var root = new Node3D { Name = "DetachedStructureVisualRoot" };
        return FactoryStructureVisualFactory.BuildForStructure(this, _visualProfile, root, CellSize);
    }

    internal void ApplyVisualStateForTesting(FactoryStructureVisualController controller, FactoryStructureVisualState state, float tickAlpha)
    {
        _visualProfile ??= CreateVisualProfile();
        controller.ApplyState(state, _visualProfile, tickAlpha);
    }

    internal bool TryGetVisualMaterial(string alias, out StandardMaterial3D material)
    {
        material = _visualController?.GetMaterialAnchor(alias) ?? null!;
        return material is not null;
    }

    protected static bool IsOrthogonallyAdjacent(Vector2I a, Vector2I b)
    {
        var delta = a - b;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }

    protected virtual FactoryStructureVisualProfile CreateVisualProfile()
    {
        return new FactoryStructureVisualProfile(
            proceduralBuilder: _ =>
            {
                BuildVisuals();
                BuildSitePresentationAccents();
            });
    }

    protected virtual FactoryStructureVisualState CreateVisualState(float tickAlpha)
    {
        return new FactoryStructureVisualState(
            Visible,
            _isHovered,
            _isSelected,
            IsUnderAttack,
            IsDestroyed,
            !IsDestroyed && Site.IsSimulationActive,
            false,
            0.0f,
            false,
            FactoryPowerStatus.Disconnected,
            0.0f,
            Time.GetTicksMsec() / 1000.0);
    }

    protected virtual void BuildVisuals()
    {
    }

    protected virtual void BuildSitePresentationAccents()
    {
        if (SiteKind != FactorySiteKind.Interior)
        {
            return;
        }

        var previewSize = Footprint.GetPreviewSize(CellSize, Facing);
        var deckWidth = Mathf.Max(CellSize * 0.96f, previewSize.X + (CellSize * 0.14f));
        var deckDepth = Mathf.Max(CellSize * 0.96f, previewSize.Y + (CellSize * 0.14f));
        var visualParent = GetVisualParent();

        CreateBox(
            visualParent,
            "MaintenanceDeck",
            new Vector3(deckWidth, 0.07f, deckDepth),
            new Color("0F172A"),
            new Vector3(0.0f, 0.035f, 0.0f));
        CreateBox(
            visualParent,
            "MaintenanceTrim",
            new Vector3(deckWidth * 0.92f, 0.02f, deckDepth * 0.92f),
            new Color("334155"),
            new Vector3(0.0f, 0.075f, 0.0f));

        if (UsesEmbeddedCargoChannel())
        {
            CreateBox(
                visualParent,
                "CargoChannel",
                new Vector3(deckWidth * 0.88f, 0.03f, Mathf.Max(CellSize * 0.16f, deckDepth * 0.24f)),
                new Color("155E75"),
                new Vector3(0.0f, 0.095f, 0.0f));
            CreateBox(
                visualParent,
                "CargoChannelStripe",
                new Vector3(deckWidth * 0.64f, 0.01f, Mathf.Max(CellSize * 0.05f, deckDepth * 0.08f)),
                new Color("67E8F9"),
                new Vector3(0.0f, 0.118f, 0.0f));
            return;
        }

        CreateBox(
            visualParent,
            "MaintenanceWalkway",
            new Vector3(deckWidth * 0.86f, 0.02f, Mathf.Max(CellSize * 0.22f, deckDepth * 0.24f)),
            new Color("475569"),
            new Vector3(0.0f, 0.085f, (deckDepth * 0.5f) - Mathf.Max(CellSize * 0.12f, deckDepth * 0.12f)));
        CreateBox(
            visualParent,
            "AccessPanel",
            new Vector3(Mathf.Max(CellSize * 0.18f, deckWidth * 0.14f), 0.10f, Mathf.Max(CellSize * 0.20f, deckDepth * 0.18f)),
            new Color("CBD5E1"),
            new Vector3((deckWidth * 0.5f) - Mathf.Max(CellSize * 0.14f, deckWidth * 0.10f), 0.13f, 0.0f));
    }

    protected MeshInstance3D CreateBox(string name, Vector3 size, Color color, Vector3? localPosition = null)
    {
        return CreateBox(GetVisualParent(), name, size, color, localPosition);
    }

    protected MeshInstance3D CreateBox(Node parent, string name, Vector3 size, Color color, Vector3? localPosition = null)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = name;
        mesh.Mesh = new BoxMesh { Size = size };

        var material = new StandardMaterial3D();
        material.AlbedoColor = color;
        material.Roughness = 0.85f;
        mesh.MaterialOverride = material;

        if (localPosition is not null)
        {
            mesh.Position = localPosition.Value;
        }

        parent.AddChild(mesh);
        return mesh;
    }

    protected MeshInstance3D CreateDisc(string name, float radius, float height, Color color, Vector3? localPosition = null)
    {
        return CreateDisc(GetVisualParent(), name, radius, height, color, localPosition);
    }

    protected MeshInstance3D CreateDisc(Node parent, string name, float radius, float height, Color color, Vector3? localPosition = null)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = name;
        mesh.Mesh = new CylinderMesh
        {
            TopRadius = radius,
            BottomRadius = radius,
            Height = height
        };

        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Roughness = 1.0f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        mesh.MaterialOverride = material;
        mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        if (localPosition is not null)
        {
            mesh.Position = localPosition.Value;
        }

        parent.AddChild(mesh);
        return mesh;
    }

    protected static FactoryInventorySectionModel CreateInventorySection(
        string inventoryId,
        string title,
        FactorySlottedItemInventory inventory,
        bool allowMove)
    {
        var slots = new List<FactoryInventorySlotModel>();
        var snapshot = inventory.Snapshot();
        for (var index = 0; index < snapshot.Length; index++)
        {
            var state = snapshot[index];
            var item = state.Item;
            slots.Add(new FactoryInventorySlotModel(
                state.Position,
                item?.ItemKind,
                item is null ? null : item.Id.ToString(),
                item is null ? null : FactoryPresentation.GetItemDisplayName(item),
                item is null
                    ? "空槽位"
                    : $"{FactoryPresentation.GetItemDisplayName(item)} x{state.StackCount}/{state.MaxStackSize} | 首件 #{item.Id} | 槽位 ({state.Position.X}, {state.Position.Y})",
                item is null ? new Color("475569") : FactoryPresentation.GetItemAccentColor(item),
                state.StackCount,
                state.MaxStackSize,
                item is null ? null : FactoryPresentation.GetItemIcon(item)));
        }

        return new FactoryInventorySectionModel(inventoryId, title, inventory.GridSize, slots, allowMove);
    }

    protected static IReadOnlyList<FactoryMapSeedItemEntry> CaptureSeedItemsFromInventory(FactorySlottedItemInventory inventory)
    {
        var counts = new Dictionary<FactoryItemKind, int>();
        var snapshot = inventory.Snapshot();
        for (var index = 0; index < snapshot.Length; index++)
        {
            var item = snapshot[index].Item;
            if (item is null || snapshot[index].StackCount <= 0)
            {
                continue;
            }

            counts[item.ItemKind] = counts.TryGetValue(item.ItemKind, out var existingCount)
                ? existingCount + snapshot[index].StackCount
                : snapshot[index].StackCount;
        }

        var kinds = new List<FactoryItemKind>(counts.Keys);
        kinds.Sort();
        var seeds = new List<FactoryMapSeedItemEntry>(kinds.Count);
        for (var index = 0; index < kinds.Count; index++)
        {
            seeds.Add(new FactoryMapSeedItemEntry(kinds[index], counts[kinds[index]]));
        }

        return seeds;
    }

    protected virtual void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
    }

    protected virtual void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
    }

    protected void RestoreCurrentHealth(float currentHealth)
    {
        _currentHealth = Mathf.Clamp(currentHealth, 0.0f, MaxHealth);
        IsDestroyed = _currentHealth <= 0.0f;
        _recentDamageTimer = 0.0;
    }

    private Node GetVisualParent()
    {
        return _currentVisualParent ?? this;
    }

    private bool UsesEmbeddedCargoChannel()
    {
        return IsTransportNode
            || this is FlowTransportStructure
            || Kind == BuildPrototypeKind.TransferBuffer
            || Kind == BuildPrototypeKind.Inserter
            || Kind == BuildPrototypeKind.InputPort
            || Kind == BuildPrototypeKind.MiningInputPort
            || Kind == BuildPrototypeKind.OutputPort
            || Kind == BuildPrototypeKind.CargoUnpacker
            || Kind == BuildPrototypeKind.CargoPacker;
    }

    private void BuildCombatVisuals()
    {
        _combatOverlayRoot = new Node3D
        {
            Name = "CombatOverlayRoot",
            TopLevel = true
        };
        AddChild(_combatOverlayRoot);

        _healthBarBackground = new MeshInstance3D
        {
            Name = "HealthBarBackground",
            Mesh = new BoxMesh { Size = new Vector3(Mathf.Max(CellSize * 0.62f, Footprint.GetPreviewSize(CellSize, Facing).X * 0.42f), 0.04f, CellSize * 0.08f) },
            Position = new Vector3(0.0f, FactoryConstants.StructureHealthBarHeight, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.05f, 0.07f, 0.10f, 0.78f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            },
            Visible = false
        };
        _combatOverlayRoot.AddChild(_healthBarBackground);

        _healthBarFillMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("4ADE80"),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        _healthBarFill = new MeshInstance3D
        {
            Name = "HealthBarFill",
            Mesh = new BoxMesh { Size = new Vector3(Mathf.Max(CellSize * 0.60f, Footprint.GetPreviewSize(CellSize, Facing).X * 0.40f), 0.03f, CellSize * 0.06f) },
            Position = new Vector3(0.0f, FactoryConstants.StructureHealthBarHeight, 0.0f),
            MaterialOverride = _healthBarFillMaterial,
            Visible = false
        };
        _combatOverlayRoot.AddChild(_healthBarFill);

        _focusRingMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.35f, 0.85f, 1.0f, 0.36f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        _focusRing = new MeshInstance3D
        {
            Name = "CombatFocusRing",
            Mesh = new BoxMesh
            {
                Size = new Vector3(
                    Mathf.Max(CellSize * 0.96f, Footprint.GetPreviewSize(CellSize, Facing).X - (CellSize * 0.12f)),
                    0.02f,
                    Mathf.Max(CellSize * 0.96f, Footprint.GetPreviewSize(CellSize, Facing).Y - (CellSize * 0.12f)))
            },
            Position = new Vector3(0.0f, 0.03f, 0.0f),
            MaterialOverride = _focusRingMaterial,
            Visible = false
        };
        _combatOverlayRoot.AddChild(_focusRing);
        SyncCombatOverlayPlacement();
    }

    private void SyncCombatOverlayPlacement()
    {
        if (_combatOverlayRoot is null)
        {
            return;
        }

        _combatOverlayRoot.Visible = Visible;
        _combatOverlayRoot.GlobalPosition = GlobalPosition;
        _combatOverlayRoot.GlobalRotation = new Vector3(0.0f, Site.WorldRotationRadians, 0.0f);
        _combatOverlayRoot.Scale = Vector3.One * Site.CombatOverlayScale;
    }

    private static void ApplyGhostTintRecursive(Node node, Color tint)
    {
        if (node is MeshInstance3D meshInstance)
        {
            meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            meshInstance.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = tint,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                Roughness = 0.18f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                EmissionEnabled = true,
                Emission = tint.Lightened(0.12f)
            };
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                ApplyGhostTintRecursive(childNode, tint);
            }
        }
    }
}
