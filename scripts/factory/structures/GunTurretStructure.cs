using Godot;
using System.Collections.Generic;

public partial class GunTurretStructure : FactoryStructure, IFactoryItemReceiver
{
    private sealed class TracerState
    {
        public required MeshInstance3D Mesh { get; init; }
        public required StandardMaterial3D Material { get; init; }
        public float RemainingLifetime { get; set; }
        public float InitialLifetime { get; init; }
    }

    private readonly List<TracerState> _tracers = new();

    private int _bufferedAmmo;
    private int _shotsFired;
    private double _attackCooldown;
    private float _targetYaw;
    private float _currentYaw;
    private float _muzzleFlashRemaining;
    private Node3D? _headPivot;
    private MeshInstance3D? _barrel;
    private Node3D? _muzzlePoint;
    private MeshInstance3D? _muzzleFlash;
    private MeshInstance3D? _ammoIndicator;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.GunTurret;
    public override string Description => "消耗弹药攻击靠近的敌人，没有补给时停止射击。";
    public override float MaxHealth => 82.0f;
    public int BufferedAmmo => _bufferedAmmo;
    public int ShotsFired => _shotsFired;

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return item.ItemKind == FactoryItemKind.AmmoMagazine
            && IsOrthogonallyAdjacent(Cell, sourceCell)
            && _bufferedAmmo < FactoryConstants.GunTurretAmmoCapacity;
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveProvidedItem(item, sourceCell, simulation))
        {
            return false;
        }

        _bufferedAmmo++;
        return true;
    }

    public override bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return item.ItemKind == FactoryItemKind.AmmoMagazine
            && IsOrthogonallyAdjacent(Cell, sourceCell)
            && _bufferedAmmo < FactoryConstants.GunTurretAmmoCapacity;
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return TryReceiveProvidedItem(item, sourceCell, simulation);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _attackCooldown -= stepSeconds;

        var target = simulation.FindClosestEnemy(GlobalPosition, FactoryConstants.GunTurretRange);
        if (target is not null)
        {
            var localTarget = ToLocal(target.GlobalPosition + new Vector3(0.0f, 0.35f, 0.0f));
            _targetYaw = Mathf.Atan2(-localTarget.Z, localTarget.X);
        }
        else
        {
            _targetYaw = 0.0f;
        }

        if (_bufferedAmmo <= 0 || _attackCooldown > 0.0 || target is null)
        {
            return;
        }

        target.ApplyDamage(FactoryConstants.GunTurretDamage, simulation);
        _bufferedAmmo--;
        _shotsFired++;
        _attackCooldown = FactoryConstants.GunTurretCooldownSeconds;
        _muzzleFlashRemaining = FactoryConstants.GunTurretMuzzleFlashLifetime;

        if (_barrel is not null)
        {
            _barrel.Scale = new Vector3(1.18f, 1.0f, 1.0f);
        }

        if (_muzzlePoint is not null)
        {
            SpawnTracer(_muzzlePoint.GlobalPosition, target.GlobalPosition + new Vector3(0.0f, 0.42f, 0.0f));
        }
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"弹药：{_bufferedAmmo}/{FactoryConstants.GunTurretAmmoCapacity}";
        yield return $"射击：{_shotsFired}";
        yield return $"射程：{FactoryConstants.GunTurretRange:0.0}";
        yield return $"炮塔朝向：{Mathf.RadToDeg(_currentYaw):0}°";
    }

    public override void _Process(double delta)
    {
        var deltaF = (float)delta;
        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Mathf.Clamp(deltaF * FactoryConstants.GunTurretTurnSpeed, 0.0f, 1.0f));

        if (_headPivot is not null)
        {
            _headPivot.Rotation = new Vector3(0.0f, _currentYaw, 0.0f);
        }

        if (_barrel is not null)
        {
            _barrel.Scale = _barrel.Scale.Lerp(Vector3.One, Mathf.Clamp(deltaF * 10.0f, 0.0f, 1.0f));
        }

        if (_muzzleFlash is not null)
        {
            _muzzleFlashRemaining = Mathf.Max(0.0f, _muzzleFlashRemaining - deltaF);
            _muzzleFlash.Visible = _muzzleFlashRemaining > 0.0f;
            if (_muzzleFlashRemaining > 0.0f)
            {
                var ratio = _muzzleFlashRemaining / FactoryConstants.GunTurretMuzzleFlashLifetime;
                _muzzleFlash.Scale = Vector3.One * (0.4f + ratio * 1.2f);
            }
        }

        if (_ammoIndicator is not null)
        {
            var ratio = Mathf.Clamp((float)_bufferedAmmo / FactoryConstants.GunTurretAmmoCapacity, 0.0f, 1.0f);
            _ammoIndicator.Scale = new Vector3(1.0f, Mathf.Max(0.15f, ratio), 1.0f);
        }

        for (var i = _tracers.Count - 1; i >= 0; i--)
        {
            var tracer = _tracers[i];
            tracer.RemainingLifetime = Mathf.Max(0.0f, tracer.RemainingLifetime - deltaF);
            var alpha = tracer.RemainingLifetime / tracer.InitialLifetime;
            tracer.Material.AlbedoColor = new Color(1.0f, 0.92f, 0.58f, alpha);

            if (tracer.RemainingLifetime <= 0.0f)
            {
                tracer.Mesh.QueueFree();
                _tracers.RemoveAt(i);
            }
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_headPivot is not null)
        {
            _headPivot.Rotation = new Vector3(0.0f, _currentYaw, 0.0f);
        }
    }

    public override void _ExitTree()
    {
        for (var i = 0; i < _tracers.Count; i++)
        {
            _tracers[i].Mesh.QueueFree();
        }

        _tracers.Clear();
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.82f, 0.24f, CellSize * 0.82f), new Color("1F2937"), new Vector3(0.0f, 0.12f, 0.0f));

        _headPivot = new Node3D
        {
            Name = "HeadPivot",
            Position = new Vector3(0.0f, 0.56f, 0.0f)
        };
        AddChild(_headPivot);

        CreateArmMesh(_headPivot, "Pivot", new Vector3(CellSize * 0.32f, 0.58f, CellSize * 0.32f), new Color("64748B"), new Vector3(0.0f, 0.22f, 0.0f));
        _barrel = CreateArmMesh(_headPivot, "Barrel", new Vector3(CellSize * 0.62f, 0.18f, 0.20f), new Color("CBD5E1"), new Vector3(CellSize * 0.22f, 0.22f, 0.0f));
        CreateArmMesh(_headPivot, "TopPlate", new Vector3(CellSize * 0.38f, 0.12f, 0.34f), new Color("94A3B8"), new Vector3(0.0f, 0.08f, 0.0f));

        _muzzlePoint = new Node3D
        {
            Name = "MuzzlePoint",
            Position = new Vector3(CellSize * 0.53f, 0.22f, 0.0f)
        };
        _headPivot.AddChild(_muzzlePoint);

        _muzzleFlash = CreateArmMesh(_muzzlePoint, "MuzzleFlash", new Vector3(0.18f, 0.18f, 0.18f), new Color("FDE68A"), Vector3.Zero);
        _muzzleFlash.Visible = false;

        _ammoIndicator = CreateBox("AmmoIndicator", new Vector3(CellSize * 0.18f, 0.22f, CellSize * 0.18f), new Color("FACC15"), new Vector3(-CellSize * 0.24f, 0.78f, 0.0f));
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        var tracerMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.92f, 0.58f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.80f, 0.24f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        var tracer = new MeshInstance3D
        {
            Name = $"Tracer_{_shotsFired}",
            TopLevel = true,
            Mesh = new BoxMesh(),
            MaterialOverride = tracerMaterial
        };

        var direction = end - start;
        var distance = Mathf.Max(0.08f, direction.Length());
        tracer.Mesh = new BoxMesh { Size = new Vector3(0.06f, 0.06f, distance) };
        AddChild(tracer);
        tracer.GlobalPosition = start.Lerp(end, 0.5f);
        tracer.LookAt(end, Vector3.Up, true);

        _tracers.Add(new TracerState
        {
            Mesh = tracer,
            Material = tracerMaterial,
            RemainingLifetime = FactoryConstants.GunTurretTracerLifetime,
            InitialLifetime = FactoryConstants.GunTurretTracerLifetime
        });
    }

    private static MeshInstance3D CreateArmMesh(Node3D parent, string name, Vector3 size, Color color, Vector3 localPosition)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = localPosition,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.82f
            }
        };
        parent.AddChild(mesh);
        return mesh;
    }
}
