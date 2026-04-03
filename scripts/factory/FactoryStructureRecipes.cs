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
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.IronPlate, 1) })
    };

    public static readonly IReadOnlyList<FactoryRecipeDefinition> AssemblerRecipes = new[]
    {
        new FactoryRecipeDefinition(
            "machine-parts",
            "机加工件",
            "消耗铁板装配更高阶的机加工件。",
            BuildPrototypeKind.Assembler,
            1.55f,
            24.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 2) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.MachinePart, 1) }),
        new FactoryRecipeDefinition(
            "standard-ammo",
            "标准弹药",
            "把铁板压装为标准弹药，用于炮塔补给。",
            BuildPrototypeKind.Assembler,
            1.20f,
            22.0f,
            new[] { new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 1) },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.AmmoMagazine, 1) }),
        new FactoryRecipeDefinition(
            "high-velocity-ammo",
            "高速弹药",
            "高压装配强化弹药，节拍更慢。",
            BuildPrototypeKind.Assembler,
            1.65f,
            28.0f,
            new[]
            {
                new FactoryRecipeIngredientDefinition(FactoryItemKind.IronPlate, 2),
                new FactoryRecipeIngredientDefinition(FactoryItemKind.Coal, 1)
            },
            new[] { new FactoryRecipeOutputDefinition(FactoryItemKind.HighVelocityAmmo, 1) })
    };
}

public static class FactoryItemCatalog
{
    public static bool IsFuel(FactoryItemKind itemKind)
    {
        return TryGetFuelValueSeconds(itemKind, out _);
    }

    public static bool TryGetFuelValueSeconds(FactoryItemKind itemKind, out float burnSeconds)
    {
        burnSeconds = itemKind switch
        {
            FactoryItemKind.Coal => 7.5f,
            _ => 0.0f
        };

        return burnSeconds > 0.0f;
    }
}
