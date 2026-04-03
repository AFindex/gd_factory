using Godot;
using System.Collections.Generic;

public enum FactoryResourceKind
{
    Coal,
    IronOre
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
            _ => FactoryItemKind.IronOre
        };
    }

    public static string GetDisplayName(FactoryResourceKind resourceKind)
    {
        return resourceKind switch
        {
            FactoryResourceKind.Coal => "煤层",
            _ => "铁矿区"
        };
    }

    public static bool SupportsExtractor(BuildPrototypeKind kind, FactoryResourceKind resourceKind)
    {
        return kind == BuildPrototypeKind.MiningDrill
            && (resourceKind == FactoryResourceKind.Coal || resourceKind == FactoryResourceKind.IronOre);
    }
}
