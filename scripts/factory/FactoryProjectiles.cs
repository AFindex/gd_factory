using Godot;

public abstract partial class FactoryCombatProjectile : Node3D
{
    public bool IsExpired { get; private set; }

    public abstract void SimulationStep(SimulationController simulation, double stepSeconds);

    protected void Expire(SimulationController simulation)
    {
        if (IsExpired)
        {
            return;
        }

        IsExpired = true;
        simulation.QueueProjectileRemoval(this);
    }
}

public sealed partial class HeavyTurretProjectile : FactoryCombatProjectile
{
    private FactoryEnemyActor? _target;
    private float _damage;
    private float _remainingTravel;
    private Vector3 _pendingStartPosition;
    private bool _hasPendingStartPosition;
    private MeshInstance3D? _shellBody;
    private MeshInstance3D? _glow;

    public void Configure(Vector3 start, FactoryEnemyActor target, float damage, float maxTravel)
    {
        _pendingStartPosition = start;
        _hasPendingStartPosition = true;
        if (IsInsideTree())
        {
            GlobalPosition = _pendingStartPosition;
            _hasPendingStartPosition = false;
        }

        _target = target;
        _damage = damage;
        _remainingTravel = maxTravel;
    }

    public override void _Ready()
    {
        if (_hasPendingStartPosition)
        {
            GlobalPosition = _pendingStartPosition;
            _hasPendingStartPosition = false;
        }

        _shellBody = new MeshInstance3D
        {
            Name = "ShellBody",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.12f,
                BottomRadius = 0.15f,
                Height = 0.54f
            },
            Rotation = new Vector3(Mathf.Pi * 0.5f, 0.0f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color("FCD34D"),
                Roughness = 0.32f,
                Metallic = 0.35f
            }
        };
        AddChild(_shellBody);

        _glow = new MeshInstance3D
        {
            Name = "ShellGlow",
            Mesh = new SphereMesh
            {
                Radius = 0.12f,
                Height = 0.24f
            },
            Position = new Vector3(-0.16f, 0.0f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1.0f, 0.86f, 0.34f, 0.88f),
                EmissionEnabled = true,
                Emission = new Color("F59E0B"),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
        AddChild(_glow);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        if (IsExpired)
        {
            return;
        }

        if (_target is null || !GodotObject.IsInstanceValid(_target) || _target.IsDefeated)
        {
            Expire(simulation);
            return;
        }

        var targetPoint = _target.GlobalPosition + new Vector3(0.0f, 0.48f, 0.0f);
        var delta = targetPoint - GlobalPosition;
        var distance = delta.Length();
        if (distance <= 0.001f)
        {
            _target.ApplyDamage(_damage, simulation);
            Expire(simulation);
            return;
        }

        var stepDistance = FactoryConstants.HeavyProjectileSpeed * (float)stepSeconds;
        if (distance <= stepDistance)
        {
            GlobalPosition = targetPoint;
            _target.ApplyDamage(_damage, simulation);
            Expire(simulation);
            return;
        }

        var direction = delta / distance;
        LookAt(GlobalPosition + direction, Vector3.Up, true);
        GlobalPosition += direction * stepDistance;
        _remainingTravel -= stepDistance;
        if (_remainingTravel <= 0.0f)
        {
            Expire(simulation);
        }
    }
}
