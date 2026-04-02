using Godot;
using System.Collections.Generic;

public abstract partial class FactoryStructure : Node3D, IFactoryInspectable
{
    private bool _visualsBuilt;
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

    protected float CellSize { get; private set; } = FactoryConstants.CellSize;

    public IFactorySite Site { get; private set; } = null!;
    public Vector2I Cell { get; private set; }
    public FacingDirection Facing { get; protected set; }
    public string ReservationOwnerId { get; private set; } = string.Empty;
    public string DisplayName => FactoryPresentation.GetKindLabel(Kind);
    public virtual string InspectionTitle => $"{DisplayName} ({Cell.X}, {Cell.Y})";
    public virtual float MaxHealth => 36.0f;
    public float CurrentHealth => _currentHealth;
    public bool IsDestroyed { get; private set; }
    public bool IsUnderAttack => _recentDamageTimer > 0.0;
    public virtual float CombatRadius => CellSize * 0.46f;

    public abstract BuildPrototypeKind Kind { get; }
    public abstract string Description { get; }
    public virtual bool IsTransportNode => false;

    public void Configure(IFactorySite site, Vector2I cell, FacingDirection facing, string? reservationOwnerId = null)
    {
        Site = site;
        Cell = cell;
        Facing = facing;
        CellSize = site.CellSize;
        _currentHealth = MaxHealth;
        ReservationOwnerId = string.IsNullOrWhiteSpace(reservationOwnerId)
            ? $"structure:{GetInstanceId()}"
            : reservationOwnerId;
        RefreshPlacement();
    }

    public override void _Ready()
    {
        if (_visualsBuilt)
        {
            return;
        }

        _visualsBuilt = true;
        BuildVisuals();
        BuildCombatVisuals();
        SyncCombatVisuals(0.0f);
    }

    public virtual void SimulationStep(SimulationController simulation, double stepSeconds)
    {
    }

    public virtual void UpdateVisuals(float tickAlpha)
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
        Position = Site.CellToWorld(Cell);
        Rotation = new Vector3(0.0f, Site.WorldRotationRadians + FactoryDirection.ToYRotationRadians(Facing), 0.0f);
        Visible = Site.IsVisible;
        SyncCombatOverlayPlacement();
    }

    public virtual IEnumerable<Vector2I> GetOccupiedCells()
    {
        yield return Cell;
    }

    public virtual IEnumerable<string> GetInspectionLines()
    {
        yield return $"生命：{CurrentHealth:0}/{MaxHealth:0}";
        yield return $"状态：{(IsDestroyed ? "已摧毁" : IsUnderAttack ? "遭受攻击" : "稳定")}";
        yield return $"朝向：{FactoryDirection.ToLabel(Facing)}";
        yield return Description;
    }

    public Vector2I GetOutputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(Facing);
    }

    public Vector2I GetInputCell()
    {
        return Cell - FactoryDirection.ToCellOffset(Facing);
    }

    public bool AcceptsFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public virtual bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public virtual bool CanOutputTo(Vector2I targetCell)
    {
        return GetOutputCell() == targetCell;
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

    protected static bool IsOrthogonallyAdjacent(Vector2I a, Vector2I b)
    {
        var delta = a - b;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }

    protected abstract void BuildVisuals();

    protected MeshInstance3D CreateBox(string name, Vector3 size, Color color, Vector3? localPosition = null)
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

        AddChild(mesh);
        return mesh;
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
            Mesh = new BoxMesh { Size = new Vector3(CellSize * 0.62f, 0.04f, CellSize * 0.08f) },
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
            Mesh = new BoxMesh { Size = new Vector3(CellSize * 0.60f, 0.03f, CellSize * 0.06f) },
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
            Mesh = new BoxMesh { Size = new Vector3(CellSize * 0.96f, 0.02f, CellSize * 0.96f) },
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
}
