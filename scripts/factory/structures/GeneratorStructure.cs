using Godot;
using System.Collections.Generic;

public partial class GeneratorStructure : FactoryStructure, IFactoryItemReceiver, IFactoryPowerProducer
{
    private readonly FactorySlottedItemInventory _fuelInventory = new(2, 1);
    private double _burnRemaining;
    private MeshInstance3D? _beacon;
    private MeshInstance3D? _powerRange;

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

    public override bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot)
    {
        return inventoryId == "generator-fuel" && _fuelInventory.TryMoveItem(fromSlot, toSlot);
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_beacon?.MaterialOverride is StandardMaterial3D material)
        {
            material.AlbedoColor = IsGenerating ? new Color("FBBF24") : new Color("64748B");
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
        CreateBox("Base", new Vector3(CellSize * 0.96f, 0.22f, CellSize * 0.96f), new Color("3F3F46"), new Vector3(0.0f, 0.11f, 0.0f));
        CreateBox("Core", new Vector3(CellSize * 0.76f, 0.92f, CellSize * 0.76f), new Color("57534E"), new Vector3(0.0f, 0.68f, 0.0f));
        CreateBox("Stack", new Vector3(CellSize * 0.20f, 0.72f, CellSize * 0.20f), new Color("A8A29E"), new Vector3(-CellSize * 0.18f, 1.34f, 0.0f));
        _beacon = CreateBox("Beacon", new Vector3(CellSize * 0.18f, 0.18f, CellSize * 0.18f), new Color("FBBF24"), new Vector3(CellSize * 0.26f, 1.06f, 0.0f));
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
