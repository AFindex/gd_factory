using Godot;
using System.Collections.Generic;

public partial class GeneratorStructure : FactoryStructure, IFactoryItemReceiver, IFactoryPowerProducer
{
    private readonly FactorySlottedItemInventory _fuelInventory = new(2, 1);
    private double _burnRemaining;
    private MeshInstance3D? _beacon;
    private MeshInstance3D? _powerRange;
    private Node3D? _rotorRig;
    private MeshInstance3D? _rotorCore;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Generator;
    public override string Description => "消耗煤炭发电，为连通电网提供基础电力。";
    public override float MaxHealth => 74.0f;
    public int PowerConnectionRangeCells => 5;
    public float NominalPowerSupply => 72.0f;
    public bool HasFuelBuffered => !_fuelInventory.IsEmpty;
    public bool IsGenerating => _burnRemaining > 0.01f || TryPeekFuel(out _);

    public float GetAvailablePower(SimulationController simulation)
    {
        return IsGenerating ? NominalPowerSupply : 0.0f;
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return IsOrthogonallyAdjacent(Cell, sourceCell)
            && FactoryItemCatalog.IsFuel(item.ItemKind)
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
        if (_burnRemaining > 0.0f)
        {
            return;
        }

        if (!TryPeekFuel(out var fuelItem) || fuelItem is null || !FactoryItemCatalog.TryGetFuelValueSeconds(fuelItem.ItemKind, out var burnSeconds))
        {
            return;
        }

        _fuelInventory.TryTakeFirstMatching(fuelItem.ItemKind, out _);
        _burnRemaining = burnSeconds;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"发电：{(IsGenerating ? $"{NominalPowerSupply:0} kW" : "待燃料")}";
        yield return $"燃料缓存：{_fuelInventory.Count} 件 | 槽位：{_fuelInventory.OccupiedSlotCount}/{_fuelInventory.Capacity}";
        yield return $"燃烧剩余：{_burnRemaining:0.0} 秒";
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

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_beacon?.MaterialOverride is StandardMaterial3D beaconMaterial)
        {
            beaconMaterial.AlbedoColor = IsGenerating ? new Color("FBBF24") : new Color("64748B");
            beaconMaterial.EmissionEnabled = true;
            beaconMaterial.Emission = IsGenerating ? new Color("F59E0B") : new Color("64748B");
            beaconMaterial.EmissionEnergyMultiplier = IsGenerating ? 1.8f : 0.25f;
        }

        if (_rotorRig is not null)
        {
            var spin = IsGenerating ? 0.16f : HasFuelBuffered ? 0.04f : 0.0f;
            _rotorRig.Rotation += new Vector3(0.0f, 0.0f, spin * tickAlpha * 60.0f);
        }

        if (_rotorCore?.MaterialOverride is StandardMaterial3D rotorMaterial)
        {
            rotorMaterial.AlbedoColor = IsGenerating ? new Color("CBD5E1") : new Color("94A3B8");
            rotorMaterial.EmissionEnabled = true;
            rotorMaterial.Emission = new Color("FBBF24");
            rotorMaterial.EmissionEnergyMultiplier = IsGenerating ? 0.75f : 0.10f;
        }
    }

    public override void SetPowerRangeVisible(bool visible)
    {
        if (_powerRange is not null)
        {
            _powerRange.Visible = visible;
        }
    }

    protected override void BuildVisuals()
    {
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
        CreateTurbineCylinder("TurbineIntakeRing", CellSize * 0.31f, CellSize * 0.08f, new Color("D6D3D1"), new Vector3(0.0f, 0.66f, -CellSize * 0.34f));
        CreateTurbineCylinder("TurbineRearRing", CellSize * 0.22f, CellSize * 0.08f, new Color("A8A29E"), new Vector3(0.0f, 0.66f, CellSize * 0.34f));
        CreateTurbineCylinder("TurbineNozzle", CellSize * 0.17f, CellSize * 0.12f, new Color("78716C"), new Vector3(0.0f, 0.66f, CellSize * 0.46f));
        CreateBox("SupportLeft", new Vector3(CellSize * 0.08f, 0.50f, CellSize * 0.08f), new Color("78716C"), new Vector3(-CellSize * 0.20f, 0.46f, 0.0f));
        CreateBox("SupportRight", new Vector3(CellSize * 0.08f, 0.50f, CellSize * 0.08f), new Color("78716C"), new Vector3(CellSize * 0.20f, 0.46f, 0.0f));

        _rotorRig = new Node3D
        {
            Name = "RotorRig",
            Position = new Vector3(0.0f, 0.66f, -CellSize * 0.31f)
        };
        AddChild(_rotorRig);
        _rotorCore = CreateTurbineCylinder(_rotorRig, "RotorCore", CellSize * 0.08f, CellSize * 0.18f, new Color("CBD5E1"), new Vector3(0.0f, 0.0f, 0.0f));
        CreateFanBlades(_rotorRig);
        CreateBox("IntakeStrutTop", new Vector3(CellSize * 0.05f, 0.16f, CellSize * 0.05f), new Color("A8A29E"), new Vector3(0.0f, 0.87f, -CellSize * 0.31f));
        CreateBox("IntakeStrutBottom", new Vector3(CellSize * 0.05f, 0.16f, CellSize * 0.05f), new Color("A8A29E"), new Vector3(0.0f, 0.45f, -CellSize * 0.31f));
        _beacon = CreateBox("Beacon", new Vector3(CellSize * 0.14f, 0.14f, CellSize * 0.14f), new Color("FBBF24"), new Vector3(CellSize * 0.28f, 1.06f, CellSize * 0.18f));
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

            var blade = CreateBox(
                bladePivot,
                $"Blade{index}",
                new Vector3(CellSize * 0.06f, CellSize * 0.24f, CellSize * 0.03f),
                new Color("E7E5E4"),
                new Vector3(0.0f, CellSize * 0.11f, 0.0f));
            blade.Rotation = new Vector3(0.0f, 0.32f, 0.0f);
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
