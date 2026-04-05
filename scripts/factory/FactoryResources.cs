using Godot;
using System.Collections.Generic;

public enum FactoryResourceKind
{
    Coal,
    IronOre,
    CopperOre
}

public sealed class FactoryResourceDepositDefinition
{
    public FactoryResourceDepositDefinition(
        string id,
        FactoryResourceKind resourceKind,
        string displayName,
        Color tint,
        IReadOnlyList<Vector2I> cells)
    {
        Id = id;
        ResourceKind = resourceKind;
        DisplayName = displayName;
        Tint = tint;
        Cells = cells;
    }

    public string Id { get; }
    public FactoryResourceKind ResourceKind { get; }
    public string DisplayName { get; }
    public Color Tint { get; }
    public IReadOnlyList<Vector2I> Cells { get; }
}

public static class FactoryResourceCatalog
{
    public static FactoryItemKind GetOutputItemKind(FactoryResourceKind resourceKind)
    {
        return resourceKind switch
        {
            FactoryResourceKind.Coal => FactoryItemKind.Coal,
            FactoryResourceKind.IronOre => FactoryItemKind.IronOre,
            _ => FactoryItemKind.CopperOre
        };
    }

    public static string GetDisplayName(FactoryResourceKind resourceKind)
    {
        return resourceKind switch
        {
            FactoryResourceKind.Coal => "煤层",
            FactoryResourceKind.IronOre => "铁矿区",
            _ => "铜矿区"
        };
    }

    public static bool SupportsExtractor(BuildPrototypeKind kind, FactoryResourceKind resourceKind)
    {
        return (kind == BuildPrototypeKind.MiningDrill || kind == BuildPrototypeKind.MiningInputPort)
            && (resourceKind == FactoryResourceKind.Coal
                || resourceKind == FactoryResourceKind.IronOre
                || resourceKind == FactoryResourceKind.CopperOre);
    }
}
