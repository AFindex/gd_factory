using System.Collections.Generic;

public sealed class FactoryRecipeIngredientDefinition
{
    public FactoryRecipeIngredientDefinition(FactoryItemKind itemKind, int amount)
    {
        ItemKind = itemKind;
        Amount = amount < 1 ? 1 : amount;
    }

    public FactoryItemKind ItemKind { get; }
    public int Amount { get; }
}

public sealed class FactoryRecipeOutputDefinition
{
    public FactoryRecipeOutputDefinition(FactoryItemKind itemKind, int amount)
    {
        ItemKind = itemKind;
        Amount = amount < 1 ? 1 : amount;
    }

    public FactoryItemKind ItemKind { get; }
    public int Amount { get; }
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
            "兼容型弹药输出，供旧防线回归线使用。",
            BuildPrototypeKind.AmmoAssembler,
            FactoryConstants.AmmoAssemblerSpawnSeconds,
            0.0f,
            null,
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.AmmoMagazine, 1) }),
        new FactoryRecipeDefinition(
            "high-velocity-ammo",
            "高速弹药",
            "兼容型强化弹药输出。",
            BuildPrototypeKind.AmmoAssembler,
            1.15f,
            0.0f,
            null,
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
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.CopperOre, 1) })
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
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.SteelPlate, 1) })
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
            "高压装配强化弹药，进一步消耗钢板与煤。",
            BuildPrototypeKind.Assembler,
            1.65f,
            28.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.AmmoMagazine, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.SteelPlate, 1),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.Coal, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.HighVelocityAmmo, 1) })
    };
}

public static partial class FactoryItemCatalog
{
}
