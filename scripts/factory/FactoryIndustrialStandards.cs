using Godot;
using System;
using System.Collections.Generic;

public sealed class FactoryBuildPaletteCategoryDefinition
{
    public FactoryBuildPaletteCategoryDefinition(string title, IReadOnlyList<BuildPrototypeKind> kinds)
    {
        Title = title;
        Kinds = kinds;
    }

    public string Title { get; }
    public IReadOnlyList<BuildPrototypeKind> Kinds { get; }
}

public sealed class FactoryBuildCatalogDefinition
{
    public FactoryBuildCatalogDefinition(
        FactorySiteKind siteKind,
        string displayName,
        string previewStyleLabel,
        string maintenanceNote,
        IReadOnlyList<FactoryBuildPaletteCategoryDefinition> categories)
    {
        SiteKind = siteKind;
        DisplayName = displayName;
        PreviewStyleLabel = previewStyleLabel;
        MaintenanceNote = maintenanceNote;
        Categories = categories;
    }

    public FactorySiteKind SiteKind { get; }
    public string DisplayName { get; }
    public string PreviewStyleLabel { get; }
    public string MaintenanceNote { get; }
    public IReadOnlyList<FactoryBuildPaletteCategoryDefinition> Categories { get; }
}

public static class FactoryIndustrialStandards
{
    private static readonly FactoryBuildCatalogDefinition WorldCatalog = new(
        FactorySiteKind.World,
        "WorldBuildCatalog",
        "重型外场设施",
        "世界站点继续表现为大型地表工业设施，物流件默认服务于重型散装/封装运输。",
        new[]
        {
            new FactoryBuildPaletteCategoryDefinition("物流与缓存", new[]
            {
                BuildPrototypeKind.Belt,
                BuildPrototypeKind.Splitter,
                BuildPrototypeKind.Merger,
                BuildPrototypeKind.Bridge,
                BuildPrototypeKind.Storage,
                BuildPrototypeKind.LargeStorageDepot,
                BuildPrototypeKind.Inserter,
                BuildPrototypeKind.Sink
            }),
            new FactoryBuildPaletteCategoryDefinition("采集与转换", new[]
            {
                BuildPrototypeKind.MiningDrill,
                BuildPrototypeKind.Loader,
                BuildPrototypeKind.Unloader,
                BuildPrototypeKind.CargoPacker
            }),
            new FactoryBuildPaletteCategoryDefinition("生产与电力", new[]
            {
                BuildPrototypeKind.Generator,
                BuildPrototypeKind.PowerPole,
                BuildPrototypeKind.Smelter,
                BuildPrototypeKind.Assembler,
                BuildPrototypeKind.AmmoAssembler
            }),
            new FactoryBuildPaletteCategoryDefinition("防御与设施", new[]
            {
                BuildPrototypeKind.Wall,
                BuildPrototypeKind.GunTurret,
                BuildPrototypeKind.HeavyGunTurret
            })
        });

    private static readonly FactoryBuildCatalogDefinition InteriorCatalog = new(
        FactorySiteKind.Interior,
        "InteriorBuildCatalog",
        "嵌入式舱段模块",
        "内部维护空间分成人行维护层与嵌入物流层，预览文案会优先强调检修面、舱段模块和供料轨。",
        new[]
        {
            new FactoryBuildPaletteCategoryDefinition("嵌入物流", new[]
            {
                BuildPrototypeKind.Belt,
                BuildPrototypeKind.Splitter,
                BuildPrototypeKind.Merger,
                BuildPrototypeKind.Bridge,
                BuildPrototypeKind.TransferBuffer,
                BuildPrototypeKind.Storage,
                BuildPrototypeKind.LargeStorageDepot,
                BuildPrototypeKind.Inserter
            }),
            new FactoryBuildPaletteCategoryDefinition("转换与边界", new[]
            {
                BuildPrototypeKind.CargoUnpacker,
                BuildPrototypeKind.CargoPacker,
                BuildPrototypeKind.InputPort,
                BuildPrototypeKind.OutputPort,
                BuildPrototypeKind.MiningInputPort
            }),
            new FactoryBuildPaletteCategoryDefinition("舱段模块", new[]
            {
                BuildPrototypeKind.Generator,
                BuildPrototypeKind.PowerPole,
                BuildPrototypeKind.Smelter,
                BuildPrototypeKind.Assembler,
                BuildPrototypeKind.AmmoAssembler
            }),
            new FactoryBuildPaletteCategoryDefinition("维护与防务", new[]
            {
                BuildPrototypeKind.Wall,
                BuildPrototypeKind.GunTurret,
                BuildPrototypeKind.HeavyGunTurret,
                BuildPrototypeKind.Sink
            })
        });

    private static readonly IReadOnlyDictionary<BuildPrototypeKind, FactorySiteKind[]> AllowedSiteKinds = new Dictionary<BuildPrototypeKind, FactorySiteKind[]>
    {
        [BuildPrototypeKind.MiningDrill] = new[] { FactorySiteKind.World },
        [BuildPrototypeKind.OutputPort] = new[] { FactorySiteKind.Interior },
        [BuildPrototypeKind.InputPort] = new[] { FactorySiteKind.Interior },
        [BuildPrototypeKind.MiningInputPort] = new[] { FactorySiteKind.Interior },
        [BuildPrototypeKind.CargoUnpacker] = new[] { FactorySiteKind.Interior },
        [BuildPrototypeKind.TransferBuffer] = new[] { FactorySiteKind.Interior },
        [BuildPrototypeKind.CargoPacker] = new[] { FactorySiteKind.World, FactorySiteKind.Interior }
    };

    public static FactorySiteKind ResolveSiteKind(IFactorySite site)
    {
        return site is MobileFactorySite
            ? FactorySiteKind.Interior
            : FactorySiteKind.World;
    }

    public static string GetSiteKindLabel(FactorySiteKind siteKind)
    {
        return siteKind == FactorySiteKind.Interior ? "内部工业标准" : "世界工业标准";
    }

    public static FactoryBuildCatalogDefinition GetBuildCatalog(FactorySiteKind siteKind)
    {
        return siteKind == FactorySiteKind.Interior ? InteriorCatalog : WorldCatalog;
    }

    public static bool IsStructureAllowed(BuildPrototypeKind kind, FactorySiteKind siteKind)
    {
        return !AllowedSiteKinds.TryGetValue(kind, out var siteKinds)
            || Array.IndexOf(siteKinds, siteKind) >= 0;
    }

    public static string GetPlacementCompatibilityError(BuildPrototypeKind kind, FactorySiteKind siteKind)
    {
        return $"{GetSiteKindLabel(siteKind)} 不允许放置 {GetSiteAwarePrototypeLabel(kind, siteKind)}。";
    }

    public static string GetSiteAwarePrototypeLabel(BuildPrototypeKind kind, FactorySiteKind siteKind)
    {
        if (siteKind != FactorySiteKind.Interior)
        {
            return FactoryPresentation.GetKindLabel(kind);
        }

        return kind switch
        {
            BuildPrototypeKind.Belt => "供料轨",
            BuildPrototypeKind.Splitter => "分配节点",
            BuildPrototypeKind.Merger => "汇流节点",
            BuildPrototypeKind.Bridge => "穿舱跨线",
            BuildPrototypeKind.Storage => "检修仓格",
            BuildPrototypeKind.LargeStorageDepot => "模块仓段",
            BuildPrototypeKind.Inserter => "移载机械臂",
            BuildPrototypeKind.Smelter => "嵌入熔炼舱",
            BuildPrototypeKind.Assembler => "嵌入装配舱",
            BuildPrototypeKind.AmmoAssembler => "弹药舱段",
            BuildPrototypeKind.Generator => "动力模块",
            BuildPrototypeKind.PowerPole => "舱内电力节点",
            BuildPrototypeKind.CargoUnpacker => "解包模块",
            BuildPrototypeKind.CargoPacker => "封包模块",
            BuildPrototypeKind.TransferBuffer => "中转缓冲槽",
            BuildPrototypeKind.InputPort => "入舱接口",
            BuildPrototypeKind.OutputPort => "出舱接口",
            BuildPrototypeKind.MiningInputPort => "采矿接入接口",
            BuildPrototypeKind.GunTurret => "防卫炮座",
            BuildPrototypeKind.HeavyGunTurret => "重型防卫炮座",
            BuildPrototypeKind.Wall => "维护隔断",
            BuildPrototypeKind.Sink => "内部回收端",
            _ => FactoryPresentation.GetKindLabel(kind)
        };
    }

    public static string GetBuildPaletteLabel(BuildPrototypeKind kind, FactorySiteKind siteKind)
    {
        if (siteKind == FactorySiteKind.Interior)
        {
            return kind switch
            {
                BuildPrototypeKind.Belt => "2 供料轨",
                BuildPrototypeKind.Splitter => "4 分配节点",
                BuildPrototypeKind.Merger => "5 汇流节点",
                BuildPrototypeKind.Bridge => "6 穿舱跨线",
                BuildPrototypeKind.TransferBuffer => "7 中转缓冲",
                BuildPrototypeKind.CargoUnpacker => "解包模块",
                BuildPrototypeKind.CargoPacker => "封包模块",
                BuildPrototypeKind.InputPort => "入舱接口",
                BuildPrototypeKind.OutputPort => "出舱接口",
                BuildPrototypeKind.MiningInputPort => "采矿接口",
                _ => GetSiteAwarePrototypeLabel(kind, siteKind)
            };
        }

        return kind switch
        {
            BuildPrototypeKind.CargoPacker => "封包站",
            _ => FactoryPresentation.GetKindLabel(kind)
        };
    }

    public static IReadOnlyList<BuildPrototypeKind> GetHotkeyPaletteKinds(FactorySiteKind siteKind, int maxCount)
    {
        var catalog = GetBuildCatalog(siteKind);
        if (maxCount <= 0)
        {
            return Array.Empty<BuildPrototypeKind>();
        }

        var result = new List<BuildPrototypeKind>(maxCount);
        var seen = new HashSet<BuildPrototypeKind>();
        for (var categoryIndex = 0; categoryIndex < catalog.Categories.Count; categoryIndex++)
        {
            var category = catalog.Categories[categoryIndex];
            for (var kindIndex = 0; kindIndex < category.Kinds.Count; kindIndex++)
            {
                var kind = category.Kinds[kindIndex];
                if (!seen.Add(kind))
                {
                    continue;
                }

                result.Add(kind);
                if (result.Count >= maxCount)
                {
                    return result;
                }
            }
        }

        return result;
    }
}

public static class FactoryCargoRules
{
    public static FactoryCargoForm ResolveProducedCargoForm(
        FactorySiteKind siteKind,
        BuildPrototypeKind sourceKind,
        FactoryItemKind itemKind,
        FactoryCargoForm? explicitCargoForm = null)
    {
        if (explicitCargoForm.HasValue)
        {
            return explicitCargoForm.Value;
        }

        return sourceKind switch
        {
            BuildPrototypeKind.MiningDrill => FactoryCargoForm.WorldBulk,
            BuildPrototypeKind.MiningInputPort => FactoryCargoForm.WorldBulk,
            BuildPrototypeKind.CargoUnpacker => FactoryCargoForm.InteriorFeed,
            BuildPrototypeKind.CargoPacker => FactoryCargoForm.WorldPacked,
            _ => siteKind == FactorySiteKind.Interior
                ? FactoryCargoForm.InteriorFeed
                : FactoryCargoForm.WorldPacked
        };
    }

    public static bool StructureAcceptsItem(BuildPrototypeKind kind, FactorySiteKind siteKind, FactoryItem item)
    {
        return kind switch
        {
            BuildPrototypeKind.InputPort => item.CargoForm == FactoryCargoForm.WorldPacked,
            BuildPrototypeKind.MiningInputPort => item.CargoForm == FactoryCargoForm.WorldBulk,
            BuildPrototypeKind.OutputPort => item.CargoForm == FactoryCargoForm.WorldPacked,
            BuildPrototypeKind.CargoUnpacker => item.CargoForm == FactoryCargoForm.WorldBulk || item.CargoForm == FactoryCargoForm.WorldPacked,
            BuildPrototypeKind.CargoPacker => siteKind == FactorySiteKind.Interior
                ? item.CargoForm == FactoryCargoForm.InteriorFeed
                : item.CargoForm == FactoryCargoForm.WorldBulk,
            BuildPrototypeKind.Smelter => siteKind == FactorySiteKind.Interior
                ? item.CargoForm == FactoryCargoForm.InteriorFeed
                : item.CargoForm != FactoryCargoForm.InteriorFeed,
            BuildPrototypeKind.Assembler => siteKind == FactorySiteKind.Interior
                ? item.CargoForm == FactoryCargoForm.InteriorFeed
                : item.CargoForm != FactoryCargoForm.InteriorFeed,
            BuildPrototypeKind.AmmoAssembler => siteKind == FactorySiteKind.Interior
                ? item.CargoForm == FactoryCargoForm.InteriorFeed
                : item.CargoForm != FactoryCargoForm.InteriorFeed,
            BuildPrototypeKind.Generator => siteKind == FactorySiteKind.Interior
                ? item.CargoForm == FactoryCargoForm.InteriorFeed
                : item.CargoForm != FactoryCargoForm.InteriorFeed,
            _ => true
        };
    }

    public static string DescribeBoundaryExpectation(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.InputPort => "入舱接口需要世界封装货物，之后需接解包模块转为内部供料。",
            BuildPrototypeKind.OutputPort => "出舱接口只接受已封包的世界标准货物。",
            BuildPrototypeKind.MiningInputPort => "采矿接入接口会把世界散装原料导入舱内，后续仍需解包转换。",
            _ => string.Empty
        };
    }
}
