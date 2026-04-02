using Godot;
using System.Collections.Generic;

public abstract partial class FactoryEnemyActor : Node3D
{
    private readonly List<Vector3> _pathPoints = new();
    private MeshInstance3D? _body;
    private StandardMaterial3D? _bodyMaterial;
    private MeshInstance3D? _healthBarBackground;
    private MeshInstance3D? _healthBar;
    private StandardMaterial3D? _healthMaterial;
    private double _attackCooldown;
    private float _damageFlashTimer;
    private float _currentHealth;
    private int _nextPathIndex;

    protected abstract Color BodyColor { get; }
    protected abstract Vector3 BodySize { get; }
    protected virtual Mesh CreateBodyMesh() => new BoxMesh { Size = BodySize };

    public string EnemyId { get; private set; } = string.Empty;
    public bool IsDefeated { get; private set; }
    public float CurrentHealth => _currentHealth;
    public abstract string DisplayName { get; }
    public abstract float MaxHealth { get; }
    public abstract float MoveSpeed { get; }
    public abstract float AggroRange { get; }
    public abstract float AttackRange { get; }
    public abstract float AttackDamage { get; }
    public abstract float AttackCooldownSeconds { get; }
    public virtual IReadOnlyCollection<BuildPrototypeKind>? PreferredTargetKinds => null;

    public void Configure(string enemyId, IEnumerable<Vector3> pathPoints)
    {
        EnemyId = enemyId;
        _pathPoints.Clear();
        _pathPoints.AddRange(pathPoints);
        _currentHealth = MaxHealth;
        _nextPathIndex = _pathPoints.Count > 1 ? 1 : 0;
        if (_pathPoints.Count > 0)
        {
            Position = _pathPoints[0];
        }
    }

    public override void _Ready()
    {
        if (GetNodeOrNull<MeshInstance3D>("Body") is null)
        {
            _bodyMaterial = new StandardMaterial3D
            {
                AlbedoColor = BodyColor,
                Roughness = 0.76f
            };

            _body = new MeshInstance3D
            {
                Name = "Body",
                Mesh = CreateBodyMesh(),
                Position = new Vector3(0.0f, BodySize.Y * 0.5f, 0.0f),
                MaterialOverride = _bodyMaterial
            };
            AddChild(_body);
        }
        else
        {
            _body = GetNodeOrNull<MeshInstance3D>("Body");
            _bodyMaterial = _body?.MaterialOverride as StandardMaterial3D;
        }

        _healthBarBackground = new MeshInstance3D
        {
            Name = "HealthBarBackground",
            Mesh = new BoxMesh { Size = new Vector3(0.78f, 0.05f, 0.09f) },
            Position = new Vector3(0.0f, BodySize.Y + 0.44f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.05f, 0.07f, 0.10f, 0.82f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
        AddChild(_healthBarBackground);

        _healthMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("F87171"),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        _healthBar = new MeshInstance3D
        {
            Name = "HealthBar",
            Mesh = new BoxMesh { Size = new Vector3(0.62f, 0.04f, 0.08f) },
            Position = new Vector3(0.0f, BodySize.Y + 0.44f, 0.0f),
            MaterialOverride = _healthMaterial
        };
        AddChild(_healthBar);
        SyncHealthBar();
    }

    public override void _Process(double delta)
    {
        if (_bodyMaterial is null)
        {
            return;
        }

        _damageFlashTimer = Mathf.Max(0.0f, _damageFlashTimer - (float)delta);
        var bodyColor = _damageFlashTimer > 0.0f
            ? BodyColor.Lerp(new Color("FDE68A"), 0.55f)
            : BodyColor;
        _bodyMaterial.AlbedoColor = bodyColor;
    }

    public void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        if (IsDefeated)
        {
            return;
        }

        _attackCooldown = Mathf.Max(0.0f, (float)(_attackCooldown - stepSeconds));
        var target = simulation.FindNearestAttackableStructure(GlobalPosition, AggroRange, PreferredTargetKinds);
        if (target is not null)
        {
            var distanceToTarget = GlobalPosition.DistanceTo(target.GlobalPosition);
            if (distanceToTarget <= AttackRange + target.CombatRadius)
            {
                if (_attackCooldown <= 0.0f)
                {
                    target.ApplyDamage(AttackDamage, simulation);
                    _attackCooldown = AttackCooldownSeconds;
                }

                return;
            }
        }

        AdvanceAlongPath((float)stepSeconds);
    }

    public void ApplyDamage(float damage, SimulationController simulation)
    {
        if (IsDefeated)
        {
            return;
        }

        _currentHealth = Mathf.Max(0.0f, _currentHealth - damage);
        _damageFlashTimer = 0.16f;
        SyncHealthBar();

        if (_currentHealth <= 0.0f)
        {
            IsDefeated = true;
            simulation.QueueEnemyRemoval(this);
        }
    }

    private void AdvanceAlongPath(float stepSeconds)
    {
        if (_pathPoints.Count == 0 || _nextPathIndex >= _pathPoints.Count)
        {
            return;
        }

        var targetPoint = _pathPoints[_nextPathIndex];
        var offset = targetPoint - GlobalPosition;
        var remainingDistance = offset.Length();
        var stepDistance = MoveSpeed * stepSeconds;
        if (remainingDistance <= stepDistance)
        {
            GlobalPosition = targetPoint;
            _nextPathIndex++;
            return;
        }

        GlobalPosition += offset.Normalized() * stepDistance;
    }

    private void SyncHealthBar()
    {
        if (_healthBar is null || _healthMaterial is null)
        {
            return;
        }

        var ratio = Mathf.Clamp(_currentHealth / MaxHealth, 0.0f, 1.0f);
        _healthBar.Scale = new Vector3(Mathf.Max(0.05f, ratio), 1.0f, 1.0f);
        _healthBar.Position = new Vector3((-0.34f) + (ratio * 0.34f), BodySize.Y + 0.44f, 0.0f);
        _healthMaterial.AlbedoColor = ratio > 0.6f
            ? new Color("4ADE80")
            : ratio > 0.3f
                ? new Color("FACC15")
                : new Color("F87171");
    }
}

public sealed partial class MeleeRaiderEnemy : FactoryEnemyActor
{
    protected override Color BodyColor => new Color("EF4444");
    protected override Vector3 BodySize => new Vector3(0.8f, 0.72f, 0.8f);

    public override string DisplayName => "近战袭击者";
    public override float MaxHealth => 16.0f;
    public override float MoveSpeed => FactoryConstants.EnemyMeleeSpeed;
    public override float AggroRange => FactoryConstants.EnemyAggroRange;
    public override float AttackRange => FactoryConstants.EnemyMeleeAttackRange;
    public override float AttackDamage => 5.0f;
    public override float AttackCooldownSeconds => 0.72f;
    public override IReadOnlyCollection<BuildPrototypeKind>? PreferredTargetKinds => new[] { BuildPrototypeKind.Wall, BuildPrototypeKind.GunTurret };
}

public sealed partial class RangedRaiderEnemy : FactoryEnemyActor
{
    protected override Color BodyColor => new Color("A855F7");
    protected override Vector3 BodySize => new Vector3(0.82f, 0.74f, 0.82f);

    public override string DisplayName => "远程袭击者";
    public override float MaxHealth => 14.0f;
    public override float MoveSpeed => FactoryConstants.EnemyRangedSpeed;
    public override float AggroRange => FactoryConstants.EnemyAggroRange + 1.0f;
    public override float AttackRange => FactoryConstants.EnemyRangedAttackRange;
    public override float AttackDamage => 4.0f;
    public override float AttackCooldownSeconds => 0.95f;
    public override IReadOnlyCollection<BuildPrototypeKind>? PreferredTargetKinds => new[] { BuildPrototypeKind.GunTurret, BuildPrototypeKind.AmmoAssembler, BuildPrototypeKind.Storage };
}
