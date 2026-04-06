using Godot;
using System.Collections.Generic;

public enum FactoryResourceKind
{
    Coal,
    IronOre,
    CopperOre,
    StoneOre,
    SulfurOre,
    QuartzOre
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
            FactoryResourceKind.CopperOre => FactoryItemKind.CopperOre,
            FactoryResourceKind.StoneOre => FactoryItemKind.StoneOre,
            FactoryResourceKind.SulfurOre => FactoryItemKind.SulfurOre,
            _ => FactoryItemKind.QuartzOre
        };
    }

    public static string GetDisplayName(FactoryResourceKind resourceKind)
    {
        return resourceKind switch
        {
            FactoryResourceKind.Coal => "煤层",
            FactoryResourceKind.IronOre => "铁矿区",
            FactoryResourceKind.CopperOre => "铜矿区",
            FactoryResourceKind.StoneOre => "石矿区",
            FactoryResourceKind.SulfurOre => "硫矿区",
            _ => "石英矿区"
        };
    }

    public static Color GetTint(FactoryResourceKind resourceKind)
    {
        return resourceKind switch
        {
            FactoryResourceKind.Coal => new Color("8B5A2B"),
            FactoryResourceKind.IronOre => new Color("64748B"),
            FactoryResourceKind.CopperOre => new Color("C2410C"),
            FactoryResourceKind.StoneOre => new Color("A8A29E"),
            FactoryResourceKind.SulfurOre => new Color("FDE047"),
            _ => new Color("67E8F9")
        };
    }

    public static bool SupportsExtractor(BuildPrototypeKind kind, FactoryResourceKind resourceKind)
    {
        return (kind == BuildPrototypeKind.MiningDrill || kind == BuildPrototypeKind.MiningInputPort)
            && (resourceKind == FactoryResourceKind.Coal
                || resourceKind == FactoryResourceKind.IronOre
                || resourceKind == FactoryResourceKind.CopperOre
                || resourceKind == FactoryResourceKind.StoneOre
                || resourceKind == FactoryResourceKind.SulfurOre
                || resourceKind == FactoryResourceKind.QuartzOre);
    }
}
