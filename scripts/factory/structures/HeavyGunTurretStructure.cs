using Godot;
using NetFactory.Models;
using System;
using System.Collections.Generic;

public partial class HeavyGunTurretStructure : FactoryStructure, IFactoryItemReceiver
{
    private readonly FactorySlottedItemInventory _ammoInventory = new(7, 2);
    private int _shotsFired;
    private double _attackCooldown;
    private float _targetYaw;
    private float _currentYaw;
    private bool _hasTarget;
    private float _muzzleFlashRemaining;
    private Node3D? _headPivot;
    private MeshInstance3D? _barrel;
    private Node3D? _muzzlePoint;
    private MeshInstance3D? _muzzleFlash;
    private MeshInstance3D? _ammoIndicator;
    private MeshInstance3D? _rangeIndicator;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.HeavyGunTurret;
    public override string Description => "占据 2x2 空间，消耗弹药并发射独立炮弹。";
    public override float MaxHealth => 168.0f;
    public override bool SupportsSelectionRangeIndicator => true;
    public int BufferedAmmo => _ammoInventory.Count;
    public int ShotsFired => _shotsFired;

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return FactoryPresentation.IsAmmoItem(item.ItemKind)
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item)
            && IsAdjacentToFootprint(sourceCell)
            && _ammoInventory.CanAcceptItem(item);
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!CanReceiveProvidedItem(item, sourceCell, simulation))
        {
            return false;
        }

        return _ammoInventory.TryAddItem(item);
    }

    public override bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanReceiveProvidedItem(item, sourceCell, simulation);
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return TryReceiveProvidedItem(item, sourceCell, simulation);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _attackCooldown -= stepSeconds;

        var target = simulation.FindClosestEnemy(GlobalPosition, FactoryConstants.HeavyGunTurretRange);
        _hasTarget = target is not null;
        if (target is not null)
        {
            var localTarget = ToLocal(target.GlobalPosition + new Vector3(0.0f, 0.50f, 0.0f));
            _targetYaw = Mathf.Atan2(-localTarget.Z, localTarget.X);
        }
        else
        {
            _targetYaw = 0.0f;
        }

        if (_ammoInventory.IsEmpty || _attackCooldown > 0.0 || target is null)
        {
            return;
        }

        if (Mathf.Abs(NormalizeAngle(_targetYaw - _currentYaw)) > FactoryConstants.HeavyGunTurretAimToleranceRadians)
        {
            return;
        }

        _ammoInventory.TryTakeFirst(out _);
        _shotsFired++;
        _attackCooldown = FactoryConstants.HeavyGunTurretCooldownSeconds;
        _muzzleFlashRemaining = FactoryConstants.HeavyGunTurretMuzzleFlashLifetime;

        if (_barrel is not null)
        {
            _barrel.Scale = new Vector3(1.10f, 1.0f, 1.0f);
        }

        if (_muzzlePoint is not null)
        {
            var projectile = new HeavyTurretProjectile
            {
                Name = $"HeavyShell_{_shotsFired}",
                TopLevel = true
            };
            simulation.RegisterProjectile(projectile);
            projectile.Configure(_muzzlePoint.GlobalPosition, target, FactoryConstants.HeavyGunTurretDamage, FactoryConstants.HeavyProjectileMaxTravel);
        }
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"弹药：{BufferedAmmo} 件 | 槽位：{_ammoInventory.OccupiedSlotCount}/{_ammoInventory.Capacity}";
        yield return $"齐射：{_shotsFired}";
        yield return $"射程：{FactoryConstants.HeavyGunTurretRange:0.0}";
    }

    public override FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        var inventorySection = CreateInventorySection("heavy-turret-ammo", "重型弹药架", _ammoInventory, true);
        return new FactoryStructureDetailModel(
            InspectionTitle,
            "重型炮塔装填与弹道状态",
            summaryLines,
            new[] { inventorySection });
    }

    public override bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack = false)
    {
        return inventoryId == "heavy-turret-ammo" && _ammoInventory.TryMoveItem(fromSlot, toSlot, splitStack);
    }

    public override bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        if (inventoryId == "heavy-turret-ammo")
        {
            endpoint = new FactoryInventoryTransferEndpoint(
                inventoryId,
                _ammoInventory,
                item => FactoryPresentation.IsAmmoItem(item.ItemKind));
            return true;
        }

        endpoint = default;
        return false;
    }

    public override IReadOnlyList<FactoryMapSeedItemEntry> CaptureMapSeedItems()
    {
        return CaptureSeedItemsFromInventory(_ammoInventory);
    }

    public override void _Process(double delta)
    {
        var deltaF = (float)delta;
        var turnSpeed = _hasTarget
            ? FactoryConstants.HeavyGunTurretTrackingSpeed
            : FactoryConstants.HeavyGunTurretReturnSpeed;
        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Mathf.Clamp(deltaF * turnSpeed, 0.0f, 1.0f));

        if (_headPivot is not null)
        {
            _headPivot.Rotation = new Vector3(0.0f, _currentYaw, 0.0f);
        }

        if (_barrel is not null)
        {
            _barrel.Scale = _barrel.Scale.Lerp(Vector3.One, Mathf.Clamp(deltaF * 5.5f, 0.0f, 1.0f));
        }

        if (_muzzleFlash is not null)
        {
            _muzzleFlashRemaining = Mathf.Max(0.0f, _muzzleFlashRemaining - deltaF);
            _muzzleFlash.Visible = _muzzleFlashRemaining > 0.0f;
            if (_muzzleFlashRemaining > 0.0f)
            {
                var ratio = _muzzleFlashRemaining / FactoryConstants.HeavyGunTurretMuzzleFlashLifetime;
                _muzzleFlash.Scale = Vector3.One * (0.6f + ratio * 1.25f);
            }
        }

        if (_ammoIndicator is not null)
        {
            var ratio = _ammoInventory.Capacity <= 0
                ? 0.0f
                : Mathf.Clamp((float)_ammoInventory.OccupiedSlotCount / _ammoInventory.Capacity, 0.0f, 1.0f);
            _ammoIndicator.Scale = new Vector3(1.0f, Mathf.Max(0.12f, ratio), 1.0f);
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_headPivot is not null)
        {
            _headPivot.Rotation = new Vector3(0.0f, _currentYaw, 0.0f);
        }
    }

    public override void SetSelectionRangeVisible(bool visible)
    {
        if (_rangeIndicator is not null)
        {
            _rangeIndicator.Visible = visible;
        }
    }

    protected override void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
        base.CaptureRuntimeState(snapshot);
        snapshot.Inventories.Add(FactoryRuntimeSnapshotValues.CaptureInventory("heavy-turret-ammo", _ammoInventory));
        snapshot.State["shots_fired"] = FactoryRuntimeSnapshotValues.FormatInt(_shotsFired);
        snapshot.State["attack_cooldown"] = FactoryRuntimeSnapshotValues.FormatDouble(_attackCooldown);
        snapshot.State["target_yaw"] = FactoryRuntimeSnapshotValues.FormatFloat(_targetYaw);
        snapshot.State["current_yaw"] = FactoryRuntimeSnapshotValues.FormatFloat(_currentYaw);
    }

    protected override void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        base.ApplyRuntimeState(snapshot, simulation);
        for (var index = 0; index < snapshot.Inventories.Count; index++)
        {
            if (snapshot.Inventories[index].InventoryId != "heavy-turret-ammo")
            {
                continue;
            }

            if (!FactoryRuntimeSnapshotValues.TryRestoreInventory(_ammoInventory, snapshot.Inventories[index], simulation))
            {
                throw new InvalidOperationException($"Heavy turret '{GetRuntimeStructureKey()}' could not restore ammo.");
            }
        }

        _shotsFired = FactoryRuntimeSnapshotValues.TryGetInt(snapshot.State, "shots_fired", out var shotsFired)
            ? Mathf.Max(0, shotsFired)
            : 0;
        _attackCooldown = FactoryRuntimeSnapshotValues.TryGetDouble(snapshot.State, "attack_cooldown", out var attackCooldown)
            ? Mathf.Max(0.0, attackCooldown)
            : 0.0;
        _targetYaw = FactoryRuntimeSnapshotValues.TryGetFloat(snapshot.State, "target_yaw", out var targetYaw)
            ? targetYaw
            : 0.0f;
        _currentYaw = FactoryRuntimeSnapshotValues.TryGetFloat(snapshot.State, "current_yaw", out var currentYaw)
            ? currentYaw
            : 0.0f;
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        HeavyGunTurretModelDescriptor.BuildModel(builder, SiteKind, GetInteriorVisualRole());
        _rangeIndicator = builder.Root.FindChild("RangeIndicator", true, false) as MeshInstance3D;
        _headPivot = builder.Root.FindChild("HeadPivot", true, false) as Node3D;
        _barrel = builder.Root.FindChild("Barrel", true, false) as MeshInstance3D;
        _muzzlePoint = builder.Root.FindChild("MuzzlePoint", true, false) as Node3D;
        _muzzleFlash = builder.Root.FindChild("MuzzleFlash", true, false) as MeshInstance3D;
        _ammoIndicator = builder.Root.FindChild("AmmoIndicator", true, false) as MeshInstance3D;

        if (_rangeIndicator is not null)
        {
            _rangeIndicator.Visible = false;
        }
    }

    private bool IsAdjacentToFootprint(Vector2I cell)
    {
        foreach (var occupiedCell in GetOccupiedCells())
        {
            if (IsOrthogonallyAdjacent(occupiedCell, cell))
            {
                return true;
            }
        }

        return false;
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

    private static float NormalizeAngle(float angleRadians)
    {
        while (angleRadians > Mathf.Pi)
        {
            angleRadians -= Mathf.Tau;
        }

        while (angleRadians < -Mathf.Pi)
        {
            angleRadians += Mathf.Tau;
        }

        return angleRadians;
    }
}
