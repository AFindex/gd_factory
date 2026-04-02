using Godot;
using System.Collections.Generic;

public abstract partial class FactoryEnemyActor : Node3D
{
    private sealed class TracerState
    {
        public required MeshInstance3D Mesh { get; init; }
        public required StandardMaterial3D Material { get; init; }
        public float RemainingLifetime { get; set; }
        public float InitialLifetime { get; init; }
    }

    private readonly List<Vector3> _pathPoints = new();
    private readonly List<TracerState> _attackTracers = new();
    private MeshInstance3D? _body;
    private StandardMaterial3D? _bodyMaterial;
    private MeshInstance3D? _healthBarBackground;
    private MeshInstance3D? _healthBar;
    private StandardMaterial3D? _healthMaterial;
    private double _attackCooldown;
    private float _damageFlashTimer;
    private float _currentHealth;
    private int _nextPathIndex;
    private FactoryStructure? _engagedTarget;

    protected abstract Color BodyColor { get; }
    protected abstract Vector3 BodySize { get; }
    protected virtual Mesh CreateBodyMesh() => new BoxMesh { Size = BodySize };
    protected virtual bool UsesAttackTracerVisual => false;
    protected virtual Color AttackTracerColor => new Color("F87171");
    protected virtual Color AttackTracerEmission => AttackTracerColor;
    protected virtual float AttackTracerWidth => 0.08f;
    protected virtual float PursuitLeashRange => AggroRange * FactoryConstants.EnemyPursuitLeashMultiplier;
    protected virtual float PursuitStopDistance => AttackRange;
    protected virtual Vector3 AttackOriginOffset => new Vector3(0.0f, BodySize.Y * 0.72f, 0.0f);
    protected virtual Vector3 AttackImpactOffset => new Vector3(0.0f, 0.52f, 0.0f);

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

        for (var i = _attackTracers.Count - 1; i >= 0; i--)
        {
            var tracer = _attackTracers[i];
            tracer.RemainingLifetime = Mathf.Max(0.0f, tracer.RemainingLifetime - (float)delta);
            var alpha = tracer.RemainingLifetime / tracer.InitialLifetime;
            tracer.Material.AlbedoColor = new Color(
                tracer.Material.AlbedoColor.R,
                tracer.Material.AlbedoColor.G,
                tracer.Material.AlbedoColor.B,
                alpha);

            if (tracer.RemainingLifetime <= 0.0f)
            {
                tracer.Mesh.QueueFree();
                _attackTracers.RemoveAt(i);
            }
        }
    }

    public void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        if (IsDefeated)
        {
            return;
        }

        _attackCooldown = Mathf.Max(0.0f, (float)(_attackCooldown - stepSeconds));
        var target = AcquireTarget(simulation);
        if (target is not null)
        {
            var distanceToTarget = GlobalPosition.DistanceTo(target.GlobalPosition);
            if (distanceToTarget <= AttackRange + target.CombatRadius)
            {
                if (_attackCooldown <= 0.0f)
                {
                    AttackTarget(target, simulation);
                    _attackCooldown = AttackCooldownSeconds;
                }

                return;
            }

            AdvanceTowardTarget(target, (float)stepSeconds);
            return;
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

    public override void _ExitTree()
    {
        for (var i = 0; i < _attackTracers.Count; i++)
        {
            _attackTracers[i].Mesh.QueueFree();
        }

        _attackTracers.Clear();
    }

    private void AdvanceAlongPath(float stepSeconds)
    {
        if (_pathPoints.Count == 0 || _nextPathIndex >= _pathPoints.Count)
        {
            return;
        }

        AdvanceTowardPoint(_pathPoints[_nextPathIndex], stepSeconds, 0.0f, advancePathOnArrival: true);
    }

    private FactoryStructure? AcquireTarget(SimulationController simulation)
    {
        if (IsTargetValid(_engagedTarget))
        {
            return _engagedTarget;
        }

        _engagedTarget = simulation.FindNearestAttackableStructure(GlobalPosition, AggroRange, PreferredTargetKinds);
        return _engagedTarget;
    }

    private bool IsTargetValid(FactoryStructure? target)
    {
        return target is not null
            && GodotObject.IsInstanceValid(target)
            && !target.IsDestroyed
            && target.Site.IsVisible
            && target.Site.IsSimulationActive
            && GlobalPosition.DistanceTo(target.GlobalPosition) <= PursuitLeashRange;
    }

    private void AttackTarget(FactoryStructure target, SimulationController simulation)
    {
        target.ApplyDamage(AttackDamage, simulation);
        if (UsesAttackTracerVisual)
        {
            SpawnAttackTracer(ToGlobal(AttackOriginOffset), target.GlobalPosition + AttackImpactOffset);
        }
    }

    private void AdvanceTowardTarget(FactoryStructure target, float stepSeconds)
    {
        var stopDistance = Mathf.Max(0.12f, PursuitStopDistance + (target.CombatRadius * 0.35f));
        AdvanceTowardPoint(target.GlobalPosition, stepSeconds, stopDistance, advancePathOnArrival: false);
    }

    private void AdvanceTowardPoint(Vector3 worldPoint, float stepSeconds, float stopDistance, bool advancePathOnArrival)
    {
        var offset = worldPoint - GlobalPosition;
        var planarOffset = new Vector3(offset.X, 0.0f, offset.Z);
        var remainingDistance = planarOffset.Length();
        if (remainingDistance <= stopDistance)
        {
            if (advancePathOnArrival)
            {
                GlobalPosition = new Vector3(worldPoint.X, GlobalPosition.Y, worldPoint.Z);
                _nextPathIndex++;
            }

            return;
        }

        var stepDistance = MoveSpeed * stepSeconds;
        var travelDistance = Mathf.Min(stepDistance, remainingDistance - stopDistance);
        if (travelDistance <= 0.0f)
        {
            return;
        }

        GlobalPosition += planarOffset.Normalized() * travelDistance;
    }

    private void SpawnAttackTracer(Vector3 start, Vector3 end)
    {
        var tracerMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(AttackTracerColor.R, AttackTracerColor.G, AttackTracerColor.B, 1.0f),
            EmissionEnabled = true,
            Emission = AttackTracerEmission,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        var tracer = new MeshInstance3D
        {
            Name = $"{Name}_AttackTracer",
            TopLevel = true,
            MaterialOverride = tracerMaterial
        };

        var direction = end - start;
        var distance = Mathf.Max(0.08f, direction.Length());
        tracer.Mesh = new BoxMesh { Size = new Vector3(AttackTracerWidth, AttackTracerWidth, distance) };
        AddChild(tracer);
        tracer.LookAtFromPosition(start.Lerp(end, 0.5f), end, Vector3.Up, true);

        _attackTracers.Add(new TracerState
        {
            Mesh = tracer,
            Material = tracerMaterial,
            RemainingLifetime = FactoryConstants.EnemyAttackTracerLifetime,
            InitialLifetime = FactoryConstants.EnemyAttackTracerLifetime
        });
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
    public override float MaxHealth => 160.0f;
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
    protected override bool UsesAttackTracerVisual => true;
    protected override Color AttackTracerColor => new Color("C084FC");
    protected override Color AttackTracerEmission => new Color("E9D5FF");
    protected override float AttackTracerWidth => 0.08f;
    protected override float PursuitStopDistance => AttackRange * 0.86f;
    protected override Vector3 AttackOriginOffset => new Vector3(0.0f, 0.68f, 0.0f);
    protected override Vector3 AttackImpactOffset => new Vector3(0.0f, 0.68f, 0.0f);

    public override string DisplayName => "远程袭击者";
    public override float MaxHealth => 140.0f;
    public override float MoveSpeed => FactoryConstants.EnemyRangedSpeed;
    public override float AggroRange => FactoryConstants.EnemyAggroRange + 1.0f;
    public override float AttackRange => FactoryConstants.EnemyRangedAttackRange;
    public override float AttackDamage => 4.0f;
    public override float AttackCooldownSeconds => 0.95f;
    public override IReadOnlyCollection<BuildPrototypeKind>? PreferredTargetKinds => new[] { BuildPrototypeKind.GunTurret, BuildPrototypeKind.AmmoAssembler, BuildPrototypeKind.Storage };
}
