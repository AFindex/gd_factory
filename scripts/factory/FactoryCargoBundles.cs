using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum FactoryBundleSizeTier
{
    Compact,
    Standard,
    Wide
}

public enum FactoryBundleTemplateMode
{
    Fixed,
    Category,
    ControlledMixed
}

public enum FactoryBundleItemCategory
{
    None,
    Ore,
    Plate,
    Component,
    Combat,
    Support
}

public sealed class FactoryBundleTemplate
{
    public FactoryBundleTemplate(
        string id,
        string displayName,
        FactoryItemKind worldItemKind,
        FactoryCargoForm cargoForm,
        FactoryBundleSizeTier sizeTier,
        FactoryBundleTemplateMode mode,
        int totalUnits,
        IReadOnlyDictionary<FactoryItemKind, int>? exactRequirements = null,
        FactoryBundleItemCategory category = FactoryBundleItemCategory.None)
    {
        Id = id;
        DisplayName = displayName;
        WorldItemKind = worldItemKind;
        CargoForm = cargoForm;
        SizeTier = sizeTier;
        Mode = mode;
        TotalUnits = Mathf.Max(1, totalUnits);
        ExactRequirements = FactoryBundleCatalog.CloneContents(exactRequirements);
        Category = category;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public FactoryItemKind WorldItemKind { get; }
    public FactoryCargoForm CargoForm { get; }
    public FactoryBundleSizeTier SizeTier { get; }
    public FactoryBundleTemplateMode Mode { get; }
    public int TotalUnits { get; }
    public IReadOnlyDictionary<FactoryItemKind, int> ExactRequirements { get; }
    public FactoryBundleItemCategory Category { get; }
}

public static class FactoryBundleCatalog
{
    private static readonly IReadOnlyDictionary<string, FactoryBundleTemplate> Templates = CreateTemplates();
    private static readonly IReadOnlyDictionary<(FactoryItemKind ItemKind, FactoryCargoForm CargoForm), string> DefaultTemplateIds = CreateDefaultTemplateIds();

    public static FactoryBundleTemplate Get(string templateId)
    {
        if (TryGet(templateId, out var template) && template is not null)
        {
            return template;
        }

        return Templates["packed-generic-cargo-standard"];
    }

    public static bool TryGet(string templateId, out FactoryBundleTemplate? template)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            template = null;
            return false;
        }

        return Templates.TryGetValue(templateId, out template);
    }

    public static string ResolveTemplateId(
        FactoryItemKind itemKind,
        FactoryCargoForm cargoForm,
        string? preferredTemplateId = null)
    {
        if (cargoForm == FactoryCargoForm.InteriorFeed)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(preferredTemplateId) && Templates.ContainsKey(preferredTemplateId))
        {
            return preferredTemplateId;
        }

        if (DefaultTemplateIds.TryGetValue((itemKind, cargoForm), out var templateId))
        {
            return templateId;
        }

        return cargoForm == FactoryCargoForm.WorldBulk
            ? "bulk-generic-cargo-standard"
            : "packed-generic-cargo-standard";
    }

    public static FactoryBundleTemplate ResolveTemplate(FactoryItem item)
    {
        return Get(ResolveTemplateId(item.ItemKind, item.CargoForm, item.BundleTemplateId));
    }

    public static FactoryBundleSizeTier ResolveSizeTier(FactoryItem item)
    {
        return item.CargoForm == FactoryCargoForm.InteriorFeed
            ? FactoryBundleSizeTier.Compact
            : ResolveTemplate(item).SizeTier;
    }

    public static IReadOnlyDictionary<FactoryItemKind, int> CloneContents(IReadOnlyDictionary<FactoryItemKind, int>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new Dictionary<FactoryItemKind, int>();
        }

        var clone = new Dictionary<FactoryItemKind, int>();
        foreach (var pair in source)
        {
            if (pair.Value > 0)
            {
                clone[pair.Key] = pair.Value;
            }
        }

        return clone;
    }

    public static IReadOnlyDictionary<FactoryItemKind, int> ResolveBundleContents(FactoryItem item)
    {
        if (item.HasBundleContents)
        {
            return CloneContents(item.BundleContents);
        }

        var template = ResolveTemplate(item);
        if (template.Mode == FactoryBundleTemplateMode.Category)
        {
            return new Dictionary<FactoryItemKind, int>
            {
                [item.ItemKind] = template.TotalUnits
            };
        }

        if (template.ExactRequirements.Count > 0)
        {
            return CloneContents(template.ExactRequirements);
        }

        return new Dictionary<FactoryItemKind, int>
        {
            [item.ItemKind] = template.TotalUnits
        };
    }

    public static Queue<FactoryItemKind> ExpandManifest(FactoryItem item)
    {
        var queue = new Queue<FactoryItemKind>();
        foreach (var pair in ResolveBundleContents(item).OrderBy(static pair => pair.Key))
        {
            for (var count = 0; count < pair.Value; count++)
            {
                queue.Enqueue(pair.Key);
            }
        }

        return queue;
    }

    public static bool TryResolveAutoPackTemplate(FactoryItem item, out FactoryBundleTemplate? template)
    {
        if (item.CargoForm != FactoryCargoForm.InteriorFeed)
        {
            template = null;
            return false;
        }

        return TryGetConverterSelectableTemplate(
            $"packed-{ToSlug(item.ItemKind)}-{GetDefaultTierFor(item.ItemKind)}",
            FactoryCargoForm.WorldPacked,
            out template);
    }

    public static bool TryResolveOneToOneWorldTemplate(FactoryItem item, out FactoryBundleTemplate? template)
    {
        template = null;
        if (item.CargoForm != FactoryCargoForm.WorldBulk && item.CargoForm != FactoryCargoForm.WorldPacked)
        {
            return false;
        }

        var templateId = ResolveTemplateId(item.ItemKind, item.CargoForm, item.BundleTemplateId);
        return TryGetConverterSelectableTemplate(templateId, item.CargoForm, out template);
    }

    public static bool IsConverterSelectableTemplate(FactoryBundleTemplate template)
    {
        return (template.CargoForm == FactoryCargoForm.WorldBulk || template.CargoForm == FactoryCargoForm.WorldPacked)
            && IsSingleItemTemplate(template);
    }

    public static bool IsSingleItemTemplate(FactoryBundleTemplate template)
    {
        if (template.Mode != FactoryBundleTemplateMode.Fixed
            || template.WorldItemKind == FactoryItemKind.GenericCargo
            || template.ExactRequirements.Count != 1)
        {
            return false;
        }

        return template.ExactRequirements.TryGetValue(template.WorldItemKind, out var units)
            && units == template.TotalUnits;
    }

    public static bool TryResolveSingleItemRequirement(
        FactoryBundleTemplate template,
        out FactoryItemKind itemKind,
        out int units)
    {
        itemKind = FactoryItemKind.GenericCargo;
        units = 0;
        if (!IsSingleItemTemplate(template)
            || !template.ExactRequirements.TryGetValue(template.WorldItemKind, out units))
        {
            return false;
        }

        itemKind = template.WorldItemKind;
        return true;
    }

    public static bool TryGetConverterSelectableTemplate(string templateId, out FactoryBundleTemplate? template)
    {
        if (!TryGet(templateId, out template) || template is null)
        {
            return false;
        }

        return IsConverterSelectableTemplate(template);
    }

    public static bool TryGetConverterSelectableTemplate(
        string templateId,
        FactoryCargoForm cargoForm,
        out FactoryBundleTemplate? template)
    {
        if (!TryGetConverterSelectableTemplate(templateId, out template) || template is null)
        {
            return false;
        }

        return template.CargoForm == cargoForm;
    }

    public static IReadOnlyList<FactoryBundleTemplate> GetConverterSelectableTemplates(params FactoryCargoForm[] cargoForms)
    {
        var allowedForms = new HashSet<FactoryCargoForm>(
            cargoForms is { Length: > 0 }
                ? cargoForms
                : new[] { FactoryCargoForm.WorldBulk, FactoryCargoForm.WorldPacked });
        var templates = new List<FactoryBundleTemplate>();
        foreach (var template in Templates.Values)
        {
            if (!allowedForms.Contains(template.CargoForm) || !IsConverterSelectableTemplate(template))
            {
                continue;
            }

            templates.Add(template);
        }

        templates.Sort(static (left, right) =>
        {
            var cargoFormCompare = left.CargoForm.CompareTo(right.CargoForm);
            if (cargoFormCompare != 0)
            {
                return cargoFormCompare;
            }

            var displayCompare = string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
            if (displayCompare != 0)
            {
                return displayCompare;
            }

            return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
        });
        return templates;
    }

    public static bool CanAcceptIntoTemplate(
        FactoryBundleTemplate template,
        FactoryItemKind itemKind,
        IReadOnlyDictionary<FactoryItemKind, int> currentCounts,
        out string reason)
    {
        var nextCounts = new Dictionary<FactoryItemKind, int>(currentCounts);
        nextCounts[itemKind] = nextCounts.TryGetValue(itemKind, out var existing) ? existing + 1 : 1;
        if (ValidateTemplateCounts(template, nextCounts, requireSatisfied: false, out reason))
        {
            return true;
        }

        return false;
    }

    public static bool IsSatisfied(
        FactoryBundleTemplate template,
        IReadOnlyDictionary<FactoryItemKind, int> currentCounts,
        out string reason)
    {
        return ValidateTemplateCounts(template, currentCounts, requireSatisfied: true, out reason);
    }

    public static string DescribeTemplate(FactoryBundleTemplate template)
    {
        return template.Mode switch
        {
            FactoryBundleTemplateMode.Fixed => $"{template.DisplayName} / 固定清单 {FormatCounts(template.ExactRequirements)}",
            FactoryBundleTemplateMode.Category => $"{template.DisplayName} / 类别模板 {GetCategoryLabel(template.Category)} x{template.TotalUnits}",
            _ => $"{template.DisplayName} / 受控混装 {FormatCounts(template.ExactRequirements)}"
        };
    }

    public static string GetDisplayName(FactoryItem item)
    {
        if (item.CargoForm == FactoryCargoForm.InteriorFeed)
        {
            return FactoryItemCatalog.GetDisplayName(item.ItemKind, item.CargoForm);
        }

        return ResolveTemplate(item).DisplayName;
    }

    public static string GetSizeTierLabel(FactoryBundleSizeTier tier)
    {
        return tier switch
        {
            FactoryBundleSizeTier.Compact => "紧凑级",
            FactoryBundleSizeTier.Wide => "宽体级",
            _ => "标准级"
        };
    }

    private static bool ValidateTemplateCounts(
        FactoryBundleTemplate template,
        IReadOnlyDictionary<FactoryItemKind, int> counts,
        bool requireSatisfied,
        out string reason)
    {
        switch (template.Mode)
        {
            case FactoryBundleTemplateMode.Fixed:
            case FactoryBundleTemplateMode.ControlledMixed:
            {
                var total = 0;
                foreach (var pair in counts)
                {
                    total += pair.Value;
                    if (!template.ExactRequirements.TryGetValue(pair.Key, out var allowed))
                    {
                        reason = $"{FactoryItemCatalog.GetDisplayName(pair.Key)} 不在模板 {template.DisplayName} 的允许清单内。";
                        return false;
                    }

                    if (pair.Value > allowed)
                    {
                        reason = $"{FactoryItemCatalog.GetDisplayName(pair.Key)} 超出模板 {template.DisplayName} 允许数量 {allowed}。";
                        return false;
                    }
                }

                foreach (var pair in template.ExactRequirements)
                {
                    var actual = counts.TryGetValue(pair.Key, out var existing) ? existing : 0;
                    if (requireSatisfied && actual != pair.Value)
                    {
                        reason = $"{template.DisplayName} 仍缺少 {FactoryItemCatalog.GetDisplayName(pair.Key)} {pair.Value - actual} 件。";
                        return false;
                    }
                }

                if (requireSatisfied && total != template.TotalUnits)
                {
                    reason = $"{template.DisplayName} 当前装箱 {total}/{template.TotalUnits}。";
                    return false;
                }

                reason = string.Empty;
                return true;
            }
            case FactoryBundleTemplateMode.Category:
            {
                var total = 0;
                foreach (var pair in counts)
                {
                    if (!BelongsToCategory(pair.Key, template.Category))
                    {
                        reason = $"{FactoryItemCatalog.GetDisplayName(pair.Key)} 不属于模板 {template.DisplayName} 限定的 {GetCategoryLabel(template.Category)}。";
                        return false;
                    }

                    total += pair.Value;
                }

                if (total > template.TotalUnits)
                {
                    reason = $"{template.DisplayName} 超出装箱总量 {template.TotalUnits}。";
                    return false;
                }

                if (requireSatisfied && total != template.TotalUnits)
                {
                    reason = $"{template.DisplayName} 当前装箱 {total}/{template.TotalUnits}。";
                    return false;
                }

                reason = string.Empty;
                return true;
            }
            default:
                reason = string.Empty;
                return true;
        }
    }

    private static bool BelongsToCategory(FactoryItemKind itemKind, FactoryBundleItemCategory category)
    {
        return category switch
        {
            FactoryBundleItemCategory.Ore => itemKind is FactoryItemKind.Coal or FactoryItemKind.IronOre or FactoryItemKind.CopperOre or FactoryItemKind.StoneOre or FactoryItemKind.SulfurOre or FactoryItemKind.QuartzOre,
            FactoryBundleItemCategory.Plate => itemKind is FactoryItemKind.IronPlate or FactoryItemKind.CopperPlate or FactoryItemKind.StoneBrick or FactoryItemKind.Glass or FactoryItemKind.SteelPlate,
            FactoryBundleItemCategory.Component => itemKind is FactoryItemKind.Gear or FactoryItemKind.CopperWire or FactoryItemKind.CircuitBoard or FactoryItemKind.MachinePart or FactoryItemKind.BuildingKit,
            FactoryBundleItemCategory.Combat => itemKind is FactoryItemKind.AmmoMagazine or FactoryItemKind.HighVelocityAmmo,
            FactoryBundleItemCategory.Support => itemKind is FactoryItemKind.BatteryPack or FactoryItemKind.RepairKit or FactoryItemKind.SulfurCrystal,
            _ => false
        };
    }

    private static string GetCategoryLabel(FactoryBundleItemCategory category)
    {
        return category switch
        {
            FactoryBundleItemCategory.Ore => "矿料",
            FactoryBundleItemCategory.Plate => "板材",
            FactoryBundleItemCategory.Component => "部件",
            FactoryBundleItemCategory.Combat => "战备",
            FactoryBundleItemCategory.Support => "维护补给",
            _ => "未分类"
        };
    }

    private static IReadOnlyDictionary<string, FactoryBundleTemplate> CreateTemplates()
    {
        var templates = new Dictionary<string, FactoryBundleTemplate>(StringComparer.OrdinalIgnoreCase);
        foreach (FactoryItemKind itemKind in Enum.GetValues(typeof(FactoryItemKind)))
        {
            var tier = ResolveSizeTierForItemKind(itemKind);
            var units = tier switch
            {
                FactoryBundleSizeTier.Compact => 4,
                FactoryBundleSizeTier.Wide => 8,
                _ => 6
            };
            var slug = ToSlug(itemKind);
            var displayName = $"{FactoryItemCatalog.GetDisplayName(itemKind)}重载货包";
            templates[$"bulk-{slug}-{GetDefaultTierFor(itemKind)}"] = new FactoryBundleTemplate(
                $"bulk-{slug}-{GetDefaultTierFor(itemKind)}",
                $"{displayName}（散装）",
                itemKind,
                FactoryCargoForm.WorldBulk,
                tier,
                FactoryBundleTemplateMode.Fixed,
                units,
                new Dictionary<FactoryItemKind, int> { [itemKind] = units });
            templates[$"packed-{slug}-{GetDefaultTierFor(itemKind)}"] = new FactoryBundleTemplate(
                $"packed-{slug}-{GetDefaultTierFor(itemKind)}",
                $"{displayName}（封装）",
                itemKind,
                FactoryCargoForm.WorldPacked,
                tier,
                FactoryBundleTemplateMode.Fixed,
                units,
                new Dictionary<FactoryItemKind, int> { [itemKind] = units });
        }

        templates["packed-ore-mixed-standard"] = new FactoryBundleTemplate(
            "packed-ore-mixed-standard",
            "综合矿料货包",
            FactoryItemKind.GenericCargo,
            FactoryCargoForm.WorldPacked,
            FactoryBundleSizeTier.Standard,
            FactoryBundleTemplateMode.Category,
            6,
            category: FactoryBundleItemCategory.Ore);

        templates["packed-frontline-sustainment-wide"] = new FactoryBundleTemplate(
            "packed-frontline-sustainment-wide",
            "前线补给货包",
            FactoryItemKind.GenericCargo,
            FactoryCargoForm.WorldPacked,
            FactoryBundleSizeTier.Wide,
            FactoryBundleTemplateMode.ControlledMixed,
            8,
            new Dictionary<FactoryItemKind, int>
            {
                [FactoryItemKind.AmmoMagazine] = 4,
                [FactoryItemKind.HighVelocityAmmo] = 2,
                [FactoryItemKind.RepairKit] = 2
            });

        return templates;
    }

    private static IReadOnlyDictionary<(FactoryItemKind ItemKind, FactoryCargoForm CargoForm), string> CreateDefaultTemplateIds()
    {
        var map = new Dictionary<(FactoryItemKind, FactoryCargoForm), string>();
        foreach (FactoryItemKind itemKind in Enum.GetValues(typeof(FactoryItemKind)))
        {
            map[(itemKind, FactoryCargoForm.WorldBulk)] = $"bulk-{ToSlug(itemKind)}-{GetDefaultTierFor(itemKind)}";
            map[(itemKind, FactoryCargoForm.WorldPacked)] = $"packed-{ToSlug(itemKind)}-{GetDefaultTierFor(itemKind)}";
        }

        return map;
    }

    private static FactoryBundleSizeTier ResolveSizeTierForItemKind(FactoryItemKind itemKind)
    {
        return itemKind switch
        {
            FactoryItemKind.BuildingKit or FactoryItemKind.MachinePart or FactoryItemKind.SteelPlate => FactoryBundleSizeTier.Wide,
            FactoryItemKind.Gear or FactoryItemKind.CopperWire or FactoryItemKind.CircuitBoard or FactoryItemKind.AmmoMagazine or FactoryItemKind.HighVelocityAmmo => FactoryBundleSizeTier.Compact,
            _ => FactoryBundleSizeTier.Standard
        };
    }

    private static string GetDefaultTierFor(FactoryItemKind itemKind)
    {
        return ResolveSizeTierForItemKind(itemKind).ToString().ToLowerInvariant();
    }

    private static string ToSlug(FactoryItemKind itemKind)
    {
        return itemKind switch
        {
            FactoryItemKind.GenericCargo => "generic-cargo",
            FactoryItemKind.BuildingKit => "building-kit",
            FactoryItemKind.IronOre => "iron-ore",
            FactoryItemKind.CopperOre => "copper-ore",
            FactoryItemKind.StoneOre => "stone-ore",
            FactoryItemKind.SulfurOre => "sulfur-ore",
            FactoryItemKind.QuartzOre => "quartz-ore",
            FactoryItemKind.IronPlate => "iron-plate",
            FactoryItemKind.CopperPlate => "copper-plate",
            FactoryItemKind.StoneBrick => "stone-brick",
            FactoryItemKind.SulfurCrystal => "sulfur-crystal",
            FactoryItemKind.SteelPlate => "steel-plate",
            FactoryItemKind.CopperWire => "copper-wire",
            FactoryItemKind.CircuitBoard => "circuit-board",
            FactoryItemKind.BatteryPack => "battery-pack",
            FactoryItemKind.RepairKit => "repair-kit",
            FactoryItemKind.MachinePart => "machine-part",
            FactoryItemKind.AmmoMagazine => "ammo-magazine",
            FactoryItemKind.HighVelocityAmmo => "high-velocity-ammo",
            _ => itemKind.ToString().ToLowerInvariant()
        };
    }

    private static string FormatCounts(IReadOnlyDictionary<FactoryItemKind, int> counts)
    {
        var parts = new List<string>(counts.Count);
        foreach (var pair in counts.OrderBy(static pair => pair.Key))
        {
            parts.Add($"{FactoryItemCatalog.GetDisplayName(pair.Key)} x{pair.Value}");
        }

        return string.Join(" + ", parts);
    }
}
