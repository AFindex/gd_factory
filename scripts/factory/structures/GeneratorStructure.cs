using Godot;
using System;
using System.Collections.Generic;

public partial class GeneratorStructure : FactoryStructure, IFactoryItemReceiver, IFactoryPowerProducer
{
    private const float ColdStartRampSeconds = 2.0f;
    private const float ShutdownCoastSeconds = 2.4f;
    private const float RotorSpinUpSeconds = 2.0f;
    private const float RotorSpinDownSeconds = 3.0f;

    private readonly FactorySlottedItemInventory _fuelInventory = new(2, 1);
    private double _burnRemaining;
    private float _powerOutputFraction;
    private float _rotorSpeedFraction;
    private MeshInstance3D? _beacon;
    private MeshInstance3D? _powerRange;
    private Node3D? _rotorRig;
    private Node3D? _frontBandRig;
    private Node3D? _rearBandRig;
    private MeshInstance3D? _rotorCore;
    private MeshInstance3D? _rotorMarker;
    private GpuParticles3D? _steamParticles;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Generator;
    public override string Description => "消耗煤炭发电，为连通电网提供基础电力。";
    public override float MaxHealth => 74.0f;
    public int PowerConnectionRangeCells => 5;
    public float NominalPowerSupply => 72.0f;
    public bool HasFuelBuffered => !_fuelInventory.IsEmpty;
    public bool IsGenerating => _powerOutputFraction > 0.08f;
    public bool IsColdStarting => HasActiveCombustion && _powerOutputFraction < 0.98f;
    public float CurrentPowerOutput => NominalPowerSupply * _powerOutputFraction;
    public float CurrentRotorSpeedFraction => _rotorSpeedFraction;
    private bool HasActiveCombustion => _burnRemaining > 0.01f;

    public float GetAvailablePower(SimulationController simulation)
    {
        return CurrentPowerOutput;
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return IsOrthogonallyAdjacent(Cell, sourceCell)
            && FactoryItemCatalog.IsFuel(item.ItemKind)
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item)
            && _fuelInventory.CanAcceptItem(item);
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanReceiveProvidedItem(item, sourceCell, simulation) && _fuelInventory.TryAddItem(item);
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
        _burnRemaining = Mathf.Max(0.0, (float)(_burnRemaining - stepSeconds));
        if (_burnRemaining <= 0.0f
            && TryPeekFuel(out var fuelItem)
            && fuelItem is not null
            && FactoryItemCatalog.TryGetFuelValueSeconds(fuelItem.ItemKind, out var burnSeconds))
        {
            _fuelInventory.TryTakeFirstMatching(fuelItem.ItemKind, out _);
            _burnRemaining = burnSeconds;
        }

        var targetPowerFraction = HasActiveCombustion ? 1.0f : 0.0f;
        var powerRamp = (float)(stepSeconds / (HasActiveCombustion ? ColdStartRampSeconds : ShutdownCoastSeconds));
        _powerOutputFraction = Mathf.MoveToward(_powerOutputFraction, targetPowerFraction, powerRamp);

        var targetRotorFraction = HasActiveCombustion
            ? Mathf.Lerp(0.24f, 1.0f, _powerOutputFraction)
            : 0.0f;
        var rotorRamp = (float)(stepSeconds / (HasActiveCombustion ? RotorSpinUpSeconds : RotorSpinDownSeconds));
        _rotorSpeedFraction = Mathf.MoveToward(_rotorSpeedFraction, targetRotorFraction, rotorRamp);
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"发电：{(CurrentPowerOutput > 0.1f ? $"{CurrentPowerOutput:0} / {NominalPowerSupply:0} kW" : "待燃料")}";
        yield return $"燃料缓存：{_fuelInventory.Count} 件 | 槽位：{_fuelInventory.OccupiedSlotCount}/{_fuelInventory.Capacity}";
        yield return $"燃烧剩余：{_burnRemaining:0.0} 秒";
        yield return $"转速：{_rotorSpeedFraction * 100.0f:0}%";
        yield return HasActiveCombustion
            ? IsColdStarting
                ? "状态：冷启动升功率"
                : "状态：稳定发电"
            : _rotorSpeedFraction > 0.02f
                ? "状态：惯性停机"
                : "状态：待燃料";
    }

    public override FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        return new FactoryStructureDetailModel(
            InspectionTitle,
            "燃料缓存与发电状态",
            summaryLines,
            new[] { CreateInventorySection("generator-fuel", "燃料仓", _fuelInventory, true) });
    }

    public override bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot, bool splitStack = false)
    {
        return inventoryId == "generator-fuel" && _fuelInventory.TryMoveItem(fromSlot, toSlot, splitStack);
    }

    public override bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        if (inventoryId == "generator-fuel")
        {
            endpoint = new FactoryInventoryTransferEndpoint(
                inventoryId,
                _fuelInventory,
                item => FactoryItemCatalog.IsFuel(item.ItemKind));
            return true;
        }

        endpoint = default;
        return false;
    }

    public override IReadOnlyList<FactoryMapSeedItemEntry> CaptureMapSeedItems()
    {
        return CaptureSeedItemsFromInventory(_fuelInventory);
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_beacon?.MaterialOverride is StandardMaterial3D beaconMaterial)
        {
            beaconMaterial.AlbedoColor = _powerOutputFraction > 0.08f ? new Color("FBBF24") : new Color("64748B");
            beaconMaterial.EmissionEnabled = true;
            beaconMaterial.Emission = _powerOutputFraction > 0.08f ? new Color("F59E0B") : new Color("64748B");
            beaconMaterial.EmissionEnergyMultiplier = 0.25f + (_powerOutputFraction * 1.8f);
        }

        if (_rotorRig is not null)
        {
            var spin = Mathf.Lerp(0.0f, 0.16f, _rotorSpeedFraction);
            _rotorRig.Rotation += new Vector3(0.0f, 0.0f, spin * tickAlpha * 60.0f);
        }

        if (_frontBandRig is not null)
        {
            var bandSpin = Mathf.Lerp(0.0f, 0.08f, _rotorSpeedFraction);
            _frontBandRig.Rotation += new Vector3(0.0f, 0.0f, bandSpin * tickAlpha * 60.0f);
        }

        if (_rearBandRig is not null)
        {
            var bandSpin = Mathf.Lerp(0.0f, -0.06f, _rotorSpeedFraction);
            _rearBandRig.Rotation += new Vector3(0.0f, 0.0f, bandSpin * tickAlpha * 60.0f);
        }

        if (_rotorCore?.MaterialOverride is StandardMaterial3D rotorMaterial)
        {
            rotorMaterial.AlbedoColor = _rotorSpeedFraction > 0.08f ? new Color("CBD5E1") : new Color("94A3B8");
            rotorMaterial.EmissionEnabled = true;
            rotorMaterial.Emission = new Color("FBBF24");
            rotorMaterial.EmissionEnergyMultiplier = 0.10f + (_powerOutputFraction * 0.75f);
        }

        if (_rotorMarker?.MaterialOverride is StandardMaterial3D rotorMarkerMaterial)
        {
            rotorMarkerMaterial.EmissionEnabled = true;
            rotorMarkerMaterial.Emission = new Color("FDE68A");
            rotorMarkerMaterial.EmissionEnergyMultiplier = 0.12f + (_rotorSpeedFraction * 1.4f);
        }

        if (_steamParticles is not null)
        {
            var steamActive = HasActiveCombustion || _rotorSpeedFraction > 0.08f;
            _steamParticles.Visible = steamActive;
            _steamParticles.Emitting = steamActive;
            _steamParticles.Amount = 6 + Mathf.RoundToInt(_powerOutputFraction * 16.0f);
            _steamParticles.SpeedScale = 0.35f + (_powerOutputFraction * 0.95f);
            _steamParticles.Position = new Vector3(-CellSize * 0.28f, 1.36f, CellSize * 0.20f);
            var steamScale = 0.72f + (_powerOutputFraction * 0.42f);
            _steamParticles.Scale = new Vector3(steamScale, steamScale * 1.12f, steamScale);
        }
    }

    public override void SetPowerRangeVisible(bool visible)
    {
        if (_powerRange is not null)
        {
            _powerRange.Visible = visible;
        }
    }

    protected override void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
        base.CaptureRuntimeState(snapshot);
        snapshot.Inventories.Add(FactoryRuntimeSnapshotValues.CaptureInventory("generator-fuel", _fuelInventory));
        snapshot.State["burn_remaining"] = FactoryRuntimeSnapshotValues.FormatDouble(_burnRemaining);
        snapshot.State["power_output_fraction"] = FactoryRuntimeSnapshotValues.FormatFloat(_powerOutputFraction);
        snapshot.State["rotor_speed_fraction"] = FactoryRuntimeSnapshotValues.FormatFloat(_rotorSpeedFraction);
    }

    protected override void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        base.ApplyRuntimeState(snapshot, simulation);
        for (var index = 0; index < snapshot.Inventories.Count; index++)
        {
            if (snapshot.Inventories[index].InventoryId != "generator-fuel")
            {
                continue;
            }

            if (!FactoryRuntimeSnapshotValues.TryRestoreInventory(_fuelInventory, snapshot.Inventories[index], simulation))
            {
                throw new InvalidOperationException($"Generator '{GetRuntimeStructureKey()}' could not restore fuel.");
            }
        }

        _burnRemaining = FactoryRuntimeSnapshotValues.TryGetDouble(snapshot.State, "burn_remaining", out var burnRemaining)
            ? Mathf.Max(0.0, burnRemaining)
            : 0.0;
        _powerOutputFraction = FactoryRuntimeSnapshotValues.TryGetFloat(snapshot.State, "power_output_fraction", out var powerFraction)
            ? Mathf.Clamp(powerFraction, 0.0f, 1.0f)
            : 0.0f;
        _rotorSpeedFraction = FactoryRuntimeSnapshotValues.TryGetFloat(snapshot.State, "rotor_speed_fraction", out var rotorFraction)
            ? Mathf.Clamp(rotorFraction, 0.0f, 1.0f)
            : 0.0f;
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            _powerRange = CreateDisc(
                "PowerRange",
                CellSize * PowerConnectionRangeCells,
                0.03f,
                new Color(0.98f, 0.66f, 0.19f, 0.12f),
                new Vector3(0.0f, 0.02f, 0.0f));
            _powerRange.Visible = false;

            CreateBox("Base", new Vector3(CellSize * 0.96f, 0.16f, CellSize * 0.96f), new Color("1C1917"), new Vector3(0.0f, 0.08f, 0.0f));
            CreateInteriorModuleShell(this, "GeneratorCabin", new Vector3(CellSize * 0.78f, 0.74f, CellSize * 0.78f), new Color("44403C"), new Color("A8A29E"), new Vector3(0.0f, 0.56f, 0.0f));
            CreateBox("FuelDrawer", new Vector3(CellSize * 0.22f, 0.30f, CellSize * 0.26f), new Color("57534E"), new Vector3(CellSize * 0.28f, 0.32f, 0.18f));
            CreateBox("BusCoupler", new Vector3(CellSize * 0.18f, 0.18f, CellSize * 0.54f), new Color("FBBF24"), new Vector3(-CellSize * 0.28f, 0.72f, 0.0f));
            CreateBox("HeatStack", new Vector3(CellSize * 0.12f, 0.40f, CellSize * 0.12f), new Color("A8A29E"), new Vector3(-CellSize * 0.28f, 1.08f, CellSize * 0.20f));
            _beacon = CreateBox("Beacon", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.12f), new Color("FBBF24"), new Vector3(CellSize * 0.26f, 0.96f, CellSize * 0.18f));

            _rotorRig = new Node3D
            {
                Name = "RotorRig",
                Position = new Vector3(0.0f, 0.70f, 0.0f)
            };
            AddChild(_rotorRig);
            _rotorCore = CreateTurbineCylinder(_rotorRig, "RotorCore", CellSize * 0.08f, CellSize * 0.18f, new Color("CBD5E1"), Vector3.Zero);
            CreateFanBlades(_rotorRig);
            _rotorMarker = CreateBox(_rotorRig, "RotorMarker", new Vector3(CellSize * 0.05f, CellSize * 0.10f, CellSize * 0.06f), new Color("FDE047"), new Vector3(CellSize * 0.17f, 0.0f, 0.0f));
            _steamParticles = CreateSteamParticles();
            AddChild(_steamParticles);
            return;
        }

        _powerRange = CreateDisc(
            "PowerRange",
            CellSize * PowerConnectionRangeCells,
            0.03f,
            new Color(0.98f, 0.66f, 0.19f, 0.14f),
            new Vector3(0.0f, 0.02f, 0.0f));
        _powerRange.Visible = false;

        CreateBox("Base", new Vector3(CellSize * 0.96f, 0.20f, CellSize * 0.96f), new Color("292524"), new Vector3(0.0f, 0.10f, 0.0f));
        CreateBox("ServiceDeck", new Vector3(CellSize * 0.84f, 0.10f, CellSize * 0.84f), new Color("44403C"), new Vector3(0.0f, 0.24f, 0.0f));
        CreateBox("FuelCabinet", new Vector3(CellSize * 0.24f, 0.40f, CellSize * 0.28f), new Color("57534E"), new Vector3(CellSize * 0.28f, 0.46f, 0.18f));
        CreateBox("ExhaustStack", new Vector3(CellSize * 0.12f, 0.62f, CellSize * 0.12f), new Color("A8A29E"), new Vector3(-CellSize * 0.28f, 1.08f, CellSize * 0.20f));

        CreateTurbineCylinder("TurbineShell", CellSize * 0.26f, CellSize * 0.78f, new Color("71717A"), new Vector3(0.0f, 0.66f, 0.0f));
        _frontBandRig = CreateDashedBandRing("ShellBandFront", -CellSize * 0.14f, new Color("38BDF8"));
        _rearBandRig = CreateDashedBandRing("ShellBandRear", CellSize * 0.16f, new Color("F59E0B"));
        CreateBox("ShellMaintenancePanel", new Vector3(CellSize * 0.10f, CellSize * 0.18f, CellSize * 0.24f), new Color("FDE68A"), new Vector3(CellSize * 0.22f, 0.66f, 0.06f));
        CreateBox("ShellHeatPanel", new Vector3(CellSize * 0.10f, CellSize * 0.18f, CellSize * 0.22f), new Color("FB7185"), new Vector3(-CellSize * 0.22f, 0.66f, -0.04f));
        CreateTurbineCylinder("TurbineIntakeRing", CellSize * 0.31f, CellSize * 0.08f, new Color("D6D3D1"), new Vector3(0.0f, 0.66f, -CellSize * 0.34f));
        CreateTurbineCylinder("TurbineRearRing", CellSize * 0.22f, CellSize * 0.08f, new Color("A8A29E"), new Vector3(0.0f, 0.66f, CellSize * 0.34f));
        CreateTurbineCylinder("TurbineNozzle", CellSize * 0.17f, CellSize * 0.12f, new Color("78716C"), new Vector3(0.0f, 0.66f, CellSize * 0.46f));
        CreateBox("SupportLeft", new Vector3(CellSize * 0.08f, 0.50f, CellSize * 0.08f), new Color("78716C"), new Vector3(-CellSize * 0.20f, 0.46f, 0.0f));
        CreateBox("SupportRight", new Vector3(CellSize * 0.08f, 0.50f, CellSize * 0.08f), new Color("78716C"), new Vector3(CellSize * 0.20f, 0.46f, 0.0f));
        CreateBox("IntakeRingAccentTop", new Vector3(CellSize * 0.18f, CellSize * 0.06f, CellSize * 0.040f), new Color("F59E0B"), new Vector3(0.0f, 0.92f, -CellSize * 0.34f));
        CreateBox("IntakeRingAccentBottom", new Vector3(CellSize * 0.18f, CellSize * 0.06f, CellSize * 0.040f), new Color("F59E0B"), new Vector3(0.0f, 0.40f, -CellSize * 0.34f));
        CreateBox("IntakeRingAccentLeft", new Vector3(CellSize * 0.06f, CellSize * 0.18f, CellSize * 0.040f), new Color("38BDF8"), new Vector3(-CellSize * 0.26f, 0.66f, -CellSize * 0.34f));
        CreateBox("IntakeRingAccentRight", new Vector3(CellSize * 0.06f, CellSize * 0.18f, CellSize * 0.040f), new Color("38BDF8"), new Vector3(CellSize * 0.26f, 0.66f, -CellSize * 0.34f));
        CreateBox("IntakeRingAccentTopLeft", new Vector3(CellSize * 0.06f, CellSize * 0.06f, CellSize * 0.040f), new Color("FDE68A"), new Vector3(-CellSize * 0.18f, 0.84f, -CellSize * 0.34f));
        CreateBox("IntakeRingAccentBottomRight", new Vector3(CellSize * 0.06f, CellSize * 0.06f, CellSize * 0.040f), new Color("7DD3FC"), new Vector3(CellSize * 0.18f, 0.48f, -CellSize * 0.34f));

        _rotorRig = new Node3D
        {
            Name = "RotorRig",
            Position = new Vector3(0.0f, 0.66f, -CellSize * 0.31f)
        };
        AddChild(_rotorRig);
        _rotorCore = CreateTurbineCylinder(_rotorRig, "RotorCore", CellSize * 0.08f, CellSize * 0.18f, new Color("CBD5E1"), new Vector3(0.0f, 0.0f, 0.0f));
        CreateFanBlades(_rotorRig);
        _rotorMarker = CreateBox(_rotorRig, "RotorMarker", new Vector3(CellSize * 0.05f, CellSize * 0.10f, CellSize * 0.06f), new Color("FDE047"), new Vector3(CellSize * 0.17f, 0.0f, 0.0f));
        CreateBox("IntakeStrutTop", new Vector3(CellSize * 0.05f, 0.16f, CellSize * 0.05f), new Color("A8A29E"), new Vector3(0.0f, 0.87f, -CellSize * 0.31f));
        CreateBox("IntakeStrutBottom", new Vector3(CellSize * 0.05f, 0.16f, CellSize * 0.05f), new Color("A8A29E"), new Vector3(0.0f, 0.45f, -CellSize * 0.31f));
        _beacon = CreateBox("Beacon", new Vector3(CellSize * 0.14f, 0.14f, CellSize * 0.14f), new Color("FBBF24"), new Vector3(CellSize * 0.28f, 1.06f, CellSize * 0.18f));
        _steamParticles = CreateSteamParticles();
        AddChild(_steamParticles);
    }

    private void CreateFanBlades(Node parent)
    {
        const int bladeCount = 6;
        for (var index = 0; index < bladeCount; index++)
        {
            var bladePivot = new Node3D
            {
                Name = $"BladePivot{index}",
                Rotation = new Vector3(0.0f, 0.0f, Mathf.Tau * index / bladeCount)
            };
            parent.AddChild(bladePivot);

            var bladeColor = index == 0
                ? new Color("F59E0B")
                : index % 2 == 0
                    ? new Color("E7E5E4")
                    : new Color("94A3B8");
            var blade = CreateBox(
                bladePivot,
                $"Blade{index}",
                new Vector3(CellSize * 0.06f, CellSize * 0.24f, CellSize * 0.03f),
                bladeColor,
                new Vector3(0.0f, CellSize * 0.11f, 0.0f));
            blade.Rotation = new Vector3(0.0f, 0.32f, 0.0f);

            var tipColor = index == 0
                ? new Color("FDE68A")
                : index % 2 == 0
                    ? new Color("475569")
                    : new Color("CBD5E1");
            var tip = CreateBox(
                bladePivot,
                $"BladeTip{index}",
                new Vector3(CellSize * 0.07f, CellSize * 0.06f, CellSize * 0.035f),
                tipColor,
                new Vector3(0.0f, CellSize * 0.22f, 0.0f));
            tip.Rotation = new Vector3(0.0f, 0.32f, 0.0f);

            if (index == 0)
            {
                if (blade.MaterialOverride is StandardMaterial3D bladeMaterial)
                {
                    bladeMaterial.EmissionEnabled = true;
                    bladeMaterial.Emission = new Color("F59E0B");
                    bladeMaterial.EmissionEnergyMultiplier = 0.55f;
                }

                if (tip.MaterialOverride is StandardMaterial3D tipMaterial)
                {
                    tipMaterial.EmissionEnabled = true;
                    tipMaterial.Emission = new Color("FDE68A");
                    tipMaterial.EmissionEnergyMultiplier = 0.75f;
                }
            }
        }
    }

    private MeshInstance3D CreateTurbineCylinder(string name, float radius, float length, Color color, Vector3 position)
    {
        return CreateTurbineCylinder(this, name, radius, length, color, position);
    }

    private MeshInstance3D CreateTurbineCylinder(Node parent, string name, float radius, float length, Color color, Vector3 position)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new CylinderMesh
            {
                TopRadius = radius,
                BottomRadius = radius,
                Height = length
            },
            Position = position,
            Rotation = new Vector3(Mathf.Pi * 0.5f, 0.0f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.72f
            }
        };
        parent.AddChild(mesh);
        return mesh;
    }

    private Node3D CreateDashedBandRing(string prefix, float zPosition, Color color)
    {
        const int segmentCount = 8;
        var ringRadius = CellSize * 0.29f;
        var rig = new Node3D
        {
            Name = $"{prefix}Rig",
            Position = new Vector3(0.0f, 0.66f, zPosition)
        };
        AddChild(rig);

        for (var index = 0; index < segmentCount; index++)
        {
            var angle = Mathf.Tau * index / segmentCount;
            var segment = CreateBox(
                rig,
                $"{prefix}_{index}",
                new Vector3(CellSize * 0.12f, CellSize * 0.045f, CellSize * 0.07f),
                color,
                new Vector3(
                    Mathf.Cos(angle) * ringRadius,
                    Mathf.Sin(angle) * ringRadius,
                    0.0f));
            segment.Rotation = new Vector3(0.0f, 0.0f, angle + (Mathf.Pi * 0.5f));
        }

        return rig;
    }

    private GpuParticles3D CreateSteamParticles()
    {
        var particles = new GpuParticles3D
        {
            Name = "SteamParticles",
            Amount = 18,
            Lifetime = 1.4,
            OneShot = false,
            Explosiveness = 0.0f,
            Randomness = 0.35f,
            Emitting = true,
            DrawPasses = 1,
            VisibilityAabb = new Aabb(new Vector3(-0.45f, -0.2f, -0.45f), new Vector3(0.9f, 2.0f, 0.9f)),
            Position = new Vector3(-CellSize * 0.28f, 1.36f, CellSize * 0.20f)
        };

        var steamMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.86f, 0.90f, 0.96f, 0.20f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var steamQuad = new QuadMesh
        {
            Size = new Vector2(CellSize * 0.20f, CellSize * 0.20f),
            Material = steamMaterial
        };
        particles.DrawPass1 = steamQuad;

        var processMaterial = new ParticleProcessMaterial
        {
            Direction = new Vector3(0.0f, 1.0f, 0.10f),
            Spread = 12.0f,
            Gravity = new Vector3(0.0f, 0.12f, 0.0f),
            InitialVelocityMin = 0.20f,
            InitialVelocityMax = 0.46f,
            ScaleMin = 0.28f,
            ScaleMax = 0.56f,
            DampingMin = 0.04f,
            DampingMax = 0.14f,
            AngleMin = -8.0f,
            AngleMax = 8.0f
        };
        particles.ProcessMaterial = processMaterial;

        return particles;
    }

    private bool TryPeekFuel(out FactoryItem? item)
    {
        if (_fuelInventory.TryPeekFirst(out item) && item is not null && FactoryItemCatalog.IsFuel(item.ItemKind))
        {
            return true;
        }

        item = null;
        return false;
    }
}
