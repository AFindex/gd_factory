using System.Collections.Generic;

public sealed class FactoryRecipeIngredientDefinition
{
    public FactoryRecipeIngredientDefinition(FactoryItemKind itemKind, int amount, FactoryCargoForm? cargoForm = null)
    {
        ItemKind = itemKind;
        Amount = amount < 1 ? 1 : amount;
        CargoForm = cargoForm;
    }

    public FactoryItemKind ItemKind { get; }
    public int Amount { get; }
    public FactoryCargoForm? CargoForm { get; }
}

public sealed class FactoryRecipeOutputDefinition
{
    public FactoryRecipeOutputDefinition(FactoryItemKind itemKind, int amount, FactoryCargoForm? cargoForm = null)
    {
        ItemKind = itemKind;
        Amount = amount < 1 ? 1 : amount;
        CargoForm = cargoForm;
    }

    public FactoryItemKind ItemKind { get; }
    public int Amount { get; }
    public FactoryCargoForm? CargoForm { get; }
}

public sealed class FactoryRecipeDefinition
{
    public FactoryRecipeDefinition(
        string id,
        string displayName,
        string summary,
        BuildPrototypeKind machineKind,
        float cycleSeconds,
        float powerDemand,
        IReadOnlyList<FactoryRecipeIngredientDefinition>? inputs,
        IReadOnlyList<FactoryRecipeOutputDefinition> outputs)
    {
        Id = id;
        DisplayName = displayName;
        Summary = summary;
        MachineKind = machineKind;
        CycleSeconds = cycleSeconds;
        PowerDemand = powerDemand;
        Inputs = inputs ?? System.Array.Empty<FactoryRecipeIngredientDefinition>();
        Outputs = outputs;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Summary { get; }
    public BuildPrototypeKind MachineKind { get; }
    public float CycleSeconds { get; }
    public float PowerDemand { get; }
    public IReadOnlyList<FactoryRecipeIngredientDefinition> Inputs { get; }
    public IReadOnlyList<FactoryRecipeOutputDefinition> Outputs { get; }
}

public static class FactoryRecipeCatalog
{
    public static readonly IReadOnlyList<FactoryRecipeDefinition> ProducerRecipes = new[]
    {
        new FactoryRecipeDefinition(
            "basic-cargo",
            "基础原料",
            "兼容型占位产物，供旧回归线继续运行。",
            BuildPrototypeKind.Producer,
            FactoryConstants.ProducerSpawnSeconds,
            0.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.GenericCargo, 1) }),
        new FactoryRecipeDefinition(
            "machine-parts",
            "机加工件",
            "兼容型高价值产出，用于 legacy 产线验证。",
            BuildPrototypeKind.Producer,
            1.1f,
            0.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.MachinePart, 1) })
    };

    public static readonly IReadOnlyList<FactoryRecipeDefinition> AmmoAssemblerRecipes = new[]
    {
        new FactoryRecipeDefinition(
            "standard-ammo",
            "标准弹药",
            "压装铁板与铜线，形成稳定的炮塔补给弹药。",
            BuildPrototypeKind.AmmoAssembler,
            FactoryConstants.AmmoAssemblerSpawnSeconds,
            22.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.CopperWire, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.AmmoMagazine, 1) }),
        new FactoryRecipeDefinition(
            "high-velocity-ammo",
            "高速弹药",
            "进一步压装钢板与硫晶，制造高压穿透弹药。",
            BuildPrototypeKind.AmmoAssembler,
            1.15f,
            28.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.AmmoMagazine, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.SteelPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.SulfurCrystal, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.HighVelocityAmmo, 1) })
    };

    public static readonly IReadOnlyList<FactoryRecipeDefinition> MiningDrillRecipes = new[]
    {
        new FactoryRecipeDefinition(
            "coal-extraction",
            "煤矿开采",
            "从煤层中持续采出可燃资源。",
            BuildPrototypeKind.MiningDrill,
            0.95f,
            12.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.Coal, 1) }),
        new FactoryRecipeDefinition(
            "iron-ore-extraction",
            "铁矿开采",
            "从铁矿区持续采出矿石。",
            BuildPrototypeKind.MiningDrill,
            0.95f,
            12.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.IronOre, 1) })
        ,
        new FactoryRecipeDefinition(
            "copper-ore-extraction",
            "铜矿开采",
            "从铜矿区持续采出铜矿石。",
            BuildPrototypeKind.MiningDrill,
            1.00f,
            13.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.CopperOre, 1) }),
        new FactoryRecipeDefinition(
            "stone-ore-extraction",
            "石矿开采",
            "从石矿带持续采出重型建材原矿。",
            BuildPrototypeKind.MiningDrill,
            1.05f,
            13.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.StoneOre, 1) }),
        new FactoryRecipeDefinition(
            "sulfur-ore-extraction",
            "硫矿开采",
            "从硫矿区持续采出弹药与化工支援原料。",
            BuildPrototypeKind.MiningDrill,
            1.10f,
            14.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.SulfurOre, 1) }),
        new FactoryRecipeDefinition(
            "quartz-ore-extraction",
            "石英开采",
            "从石英矿带持续采出电子与维护支援原料。",
            BuildPrototypeKind.MiningDrill,
            1.10f,
            14.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.QuartzOre, 1) })
    };

    public static readonly IReadOnlyList<FactoryRecipeDefinition> SmelterRecipes = new[]
    {
        new FactoryRecipeDefinition(
            "iron-smelting",
            "炼制铁板",
            "把铁矿石烧结成可供后续制造的铁板。",
            BuildPrototypeKind.Smelter,
            1.35f,
            18.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.IronOre, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.IronPlate, 1) }),
        new FactoryRecipeDefinition(
            "copper-smelting",
            "炼制铜板",
            "把铜矿石冶炼成可用于线缆与电路的铜板。",
            BuildPrototypeKind.Smelter,
            1.30f,
            18.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.CopperOre, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.CopperPlate, 1) }),
        new FactoryRecipeDefinition(
            "steel-smelting",
            "炼制钢板",
            "把多块铁板继续冶炼成更高强度的钢板。",
            BuildPrototypeKind.Smelter,
            2.10f,
            26.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 2) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.SteelPlate, 1) }),
        new FactoryRecipeDefinition(
            "stone-brick",
            "烧结石砖",
            "把石矿石烧结为稳定的站点与防线建材。",
            BuildPrototypeKind.Smelter,
            1.45f,
            18.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.StoneOre, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.StoneBrick, 1) }),
        new FactoryRecipeDefinition(
            "sulfur-crystal",
            "提纯硫晶",
            "把硫矿石提纯为弹药链使用的硫晶。",
            BuildPrototypeKind.Smelter,
            1.40f,
            19.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.SulfurOre, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.SulfurCrystal, 1) }),
        new FactoryRecipeDefinition(
            "glass-smelting",
            "熔融玻璃板",
            "把石英矿石熔融成用于维护与电子链的玻璃板。",
            BuildPrototypeKind.Smelter,
            1.50f,
            20.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.QuartzOre, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.Glass, 1) })
    };

    public static readonly IReadOnlyList<FactoryRecipeDefinition> AssemblerRecipes = new[]
    {
        new FactoryRecipeDefinition(
            "gear",
            "齿轮",
            "使用铁板压制出基础机械齿轮。",
            BuildPrototypeKind.Assembler,
            0.95f,
            16.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.Gear, 1) }),
        new FactoryRecipeDefinition(
            "copper-wire",
            "铜线",
            "把铜板拉制成后续电路与弹药需要的铜线。",
            BuildPrototypeKind.Assembler,
            0.85f,
            14.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.CopperPlate, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.CopperWire, 1) }),
        new FactoryRecipeDefinition(
            "circuit-board",
            "电路板",
            "把铁板与铜线焊接为基础电路板。",
            BuildPrototypeKind.Assembler,
            1.35f,
            20.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.CopperWire, 2)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.CircuitBoard, 1) }),
        new FactoryRecipeDefinition(
            "machine-parts",
            "机加工件",
            "消耗齿轮、钢板和电路板装配更高阶的机加工件。",
            BuildPrototypeKind.Assembler,
            1.80f,
            26.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.Gear, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.SteelPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.CircuitBoard, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.MachinePart, 1) }),
        new FactoryRecipeDefinition(
            "standard-ammo",
            "标准弹药",
            "把铁板和铜线压装为标准弹药，用于炮塔补给。",
            BuildPrototypeKind.Assembler,
            1.20f,
            22.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.CopperWire, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.AmmoMagazine, 1) }),
        new FactoryRecipeDefinition(
            "high-velocity-ammo",
            "高速弹药",
            "高压装配强化弹药，进一步消耗钢板与硫晶。",
            BuildPrototypeKind.Assembler,
            1.65f,
            28.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.AmmoMagazine, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.SteelPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.SulfurCrystal, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.HighVelocityAmmo, 1) }),
        new FactoryRecipeDefinition(
            "battery-pack",
            "电池组",
            "把铜板与玻璃板组装成电力维护使用的电池组。",
            BuildPrototypeKind.Assembler,
            1.25f,
            19.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.CopperPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.Glass, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.BatteryPack, 1) }),
        new FactoryRecipeDefinition(
            "repair-kit",
            "维护包",
            "把石砖、齿轮与电池组组装成站点与电网维护物资。",
            BuildPrototypeKind.Assembler,
            1.55f,
            22.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.StoneBrick, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.Gear, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.BatteryPack, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.RepairKit, 1) })
    };
}

public static partial class FactoryItemCatalog
{
}
