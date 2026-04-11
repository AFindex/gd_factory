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

    public static string GetInteriorPresentationLabel(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Belt => "嵌入供料轨",
            BuildPrototypeKind.Splitter => "配流节点",
            BuildPrototypeKind.Merger => "汇流节点",
            BuildPrototypeKind.Bridge => "穿舱跨线",
            BuildPrototypeKind.Storage => "检修仓格",
            BuildPrototypeKind.LargeStorageDepot => "模块仓段",
            BuildPrototypeKind.Inserter => "移载机械臂",
            BuildPrototypeKind.Smelter => "熔炼模块舱",
            BuildPrototypeKind.Assembler => "装配模块舱",
            BuildPrototypeKind.AmmoAssembler => "弹药装填舱",
            BuildPrototypeKind.Generator => "动力核心舱",
            BuildPrototypeKind.PowerPole => "舱内母线节点",
            BuildPrototypeKind.CargoUnpacker => "解包处理舱",
            BuildPrototypeKind.CargoPacker => "封包处理舱",
            BuildPrototypeKind.TransferBuffer => "抽屉缓冲槽",
            BuildPrototypeKind.InputPort => "入舱适配接口",
            BuildPrototypeKind.OutputPort => "出舱适配接口",
            BuildPrototypeKind.MiningInputPort => "采矿接入接口",
            BuildPrototypeKind.GunTurret => "轻型炮塔硬点",
            BuildPrototypeKind.HeavyGunTurret => "重型炮塔硬点",
            BuildPrototypeKind.Wall => "维护隔断",
            BuildPrototypeKind.Sink => "舱内回收端",
            _ => GetSiteAwarePrototypeLabel(kind, FactorySiteKind.Interior)
        };
    }

    public static string GetInteriorPreviewSummary(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Belt => "把维护层上方的标准供料导入嵌入轨道。",
            BuildPrototypeKind.Splitter => "把一条供料轨拆分到多个舱段接口。",
            BuildPrototypeKind.Merger => "把多个舱段输出汇入单一供料轨。",
            BuildPrototypeKind.Bridge => "让两层嵌入物流在同格交叉而不混线。",
            BuildPrototypeKind.Storage => "在维护通路边暂存舱内标准化载具。",
            BuildPrototypeKind.LargeStorageDepot => "提供可维护的大体积舱段仓储。",
            BuildPrototypeKind.Inserter => "在维护层与嵌入物流层之间移载载具。",
            BuildPrototypeKind.CargoUnpacker => "把世界标准货包解成舱内标准载具。",
            BuildPrototypeKind.CargoPacker => "把舱内标准载具重新封成世界货包。",
            BuildPrototypeKind.TransferBuffer => "在交汇位置临时整理和排队载具。",
            BuildPrototypeKind.Generator => "以舱内模块化方式供给动力母线。",
            BuildPrototypeKind.PowerPole => "扩展舱内维护层的供电母线。",
            BuildPrototypeKind.Smelter => "以模块舱方式持续炼制板材。",
            BuildPrototypeKind.Assembler => "以舱段装配面板持续组装中间品。",
            BuildPrototypeKind.AmmoAssembler => "围绕炮塔硬点供给弹药载具。",
            BuildPrototypeKind.InputPort => "把世界侧物流平稳接入舱内维护层。",
            BuildPrototypeKind.OutputPort => "把舱内封包货物推送回世界侧。",
            BuildPrototypeKind.MiningInputPort => "将采矿前端与舱内维护层对接。",
            BuildPrototypeKind.GunTurret => "作为轻型武器硬点守住舱体边界。",
            BuildPrototypeKind.HeavyGunTurret => "作为重型武器井位守住舱体边界。",
            _ => "作为维护层可进入的标准舱段模块。"
        };
    }

    public static string GetCargoPresentationLabel(FactoryItemKind itemKind, FactoryCargoForm cargoForm)
    {
        return cargoForm switch
        {
            FactoryCargoForm.WorldBulk => $"{FactoryItemCatalog.GetDisplayName(itemKind)}（世界散装）",
            FactoryCargoForm.WorldPacked => $"{FactoryItemCatalog.GetDisplayName(itemKind)}（世界封装）",
            FactoryCargoForm.InteriorFeed => $"{FactoryItemCatalog.GetDisplayName(itemKind)}（{GetInteriorCarrierLabel(itemKind)}）",
            _ => $"{FactoryItemCatalog.GetDisplayName(itemKind)}（{FactoryPresentation.GetCargoFormLabel(cargoForm)}）"
        };
    }

    public static string GetInteriorCarrierLabel(FactoryItemKind itemKind)
    {
        return itemKind switch
        {
            FactoryItemKind.Coal or FactoryItemKind.IronOre or FactoryItemKind.CopperOre or FactoryItemKind.StoneOre or FactoryItemKind.SulfurOre or FactoryItemKind.QuartzOre
                => "舱内矿罐",
            FactoryItemKind.IronPlate or FactoryItemKind.CopperPlate or FactoryItemKind.SteelPlate or FactoryItemKind.StoneBrick or FactoryItemKind.Glass
                => "层叠托盘",
            FactoryItemKind.CopperWire or FactoryItemKind.CircuitBoard
                => "电子料盒",
            FactoryItemKind.Gear or FactoryItemKind.MachinePart or FactoryItemKind.BuildingKit
                => "部件匣盒",
            FactoryItemKind.BatteryPack or FactoryItemKind.RepairKit
                => "维护匣盒",
            FactoryItemKind.AmmoMagazine or FactoryItemKind.HighVelocityAmmo
                => "弹药匣",
            FactoryItemKind.SulfurCrystal
                => "密封晶匣",
            _ => "标准供料盒"
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
