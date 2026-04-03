using System.Collections.Generic;

public sealed class FactoryRecipeDefinition
{
    public FactoryRecipeDefinition(string id, string displayName, string summary, FactoryItemKind outputItemKind, float cycleSeconds)
    {
        Id = id;
        DisplayName = displayName;
        Summary = summary;
        OutputItemKind = outputItemKind;
        CycleSeconds = cycleSeconds;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Summary { get; }
    public FactoryItemKind OutputItemKind { get; }
    public float CycleSeconds { get; }
}

public static class FactoryRecipeCatalog
{
    public static readonly IReadOnlyList<FactoryRecipeDefinition> ProducerRecipes = new[]
    {
        new FactoryRecipeDefinition("basic-cargo", "基础原料", "稳定输出基础原料箱，节拍均衡。", FactoryItemKind.GenericCargo, FactoryConstants.ProducerSpawnSeconds),
        new FactoryRecipeDefinition("machine-parts", "机加工件", "输出更高价值的机械零件，节拍略慢。", FactoryItemKind.MachinePart, 1.1f)
    };

    public static readonly IReadOnlyList<FactoryRecipeDefinition> AmmoAssemblerRecipes = new[]
    {
        new FactoryRecipeDefinition("standard-ammo", "标准弹药", "标准弹匣，供给稳定。", FactoryItemKind.AmmoMagazine, FactoryConstants.AmmoAssemblerSpawnSeconds),
        new FactoryRecipeDefinition("high-velocity-ammo", "高速弹药", "强化型弹药，产速略慢但依然可供炮塔使用。", FactoryItemKind.HighVelocityAmmo, 1.15f)
    };
}
