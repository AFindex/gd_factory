using Godot;
using System.Collections.Generic;

public static class FactoryPrototypeCatalog
{
    public static readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> SharedDefinitions = new()
    {
        [BuildPrototypeKind.Producer] = new BuildPrototypeDefinition(BuildPrototypeKind.Producer, "兼容生产器", new Color("9DC08B"), "兼容型占位产物流，仅用于 legacy 回归线。"),
        [BuildPrototypeKind.Belt] = new BuildPrototypeDefinition(BuildPrototypeKind.Belt, "传送带", new Color("7DD3FC"), "将物品沿直线向前输送，也允许末端直接并入另一段传送带的中段。"),
        [BuildPrototypeKind.Splitter] = new BuildPrototypeDefinition(BuildPrototypeKind.Splitter, "分流器", new Color("C4B5FD"), "将后方输入分到左右两路。"),
        [BuildPrototypeKind.Merger] = new BuildPrototypeDefinition(BuildPrototypeKind.Merger, "合并器", new Color("99F6E4"), "把后方、左侧和右侧三路物流汇成前方一路。"),
        [BuildPrototypeKind.Bridge] = new BuildPrototypeDefinition(BuildPrototypeKind.Bridge, "跨桥", new Color("F59E0B"), "让南北和东西两路物流跨越而不互连。"),
        [BuildPrototypeKind.CargoPacker] = new BuildPrototypeDefinition(BuildPrototypeKind.CargoPacker, "封包站", new Color("F97316"), "把外场产物压成世界标准封装货物，供移动工厂边界或远程收货链继续运输。"),
        [BuildPrototypeKind.Storage] = new BuildPrototypeDefinition(BuildPrototypeKind.Storage, "仓储", new Color("94A3B8"), "缓存多件物品，可向前输出，也能被机械臂抓取。"),
        [BuildPrototypeKind.LargeStorageDepot] = new BuildPrototypeDefinition(BuildPrototypeKind.LargeStorageDepot, "大型仓储", new Color("64748B"), "占据 2x2 空间的大型缓存仓，可作为更稳定的物流缓冲点。"),
        [BuildPrototypeKind.Inserter] = new BuildPrototypeDefinition(BuildPrototypeKind.Inserter, "机械臂", new Color("FACC15"), "从后方抓取一件物品并向前投送。"),
        [BuildPrototypeKind.Wall] = new BuildPrototypeDefinition(BuildPrototypeKind.Wall, "墙体", new Color("D1D5DB"), "高耐久阻挡物，用来拖延敌人推进。"),
        [BuildPrototypeKind.AmmoAssembler] = new BuildPrototypeDefinition(BuildPrototypeKind.AmmoAssembler, "弹药组装器", new Color("FB923C"), "持续生产弹药，沿物流链补给防线。"),
        [BuildPrototypeKind.GunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.GunTurret, "机枪炮塔", new Color("CBD5E1"), "需要弹药供给，敌人进入射程时自动开火。"),
        [BuildPrototypeKind.HeavyGunTurret] = new BuildPrototypeDefinition(BuildPrototypeKind.HeavyGunTurret, "重型炮塔", new Color("E2E8F0"), "占据 2x2 空间，消耗高速弹药并发射独立炮弹。"),
        [BuildPrototypeKind.Sink] = new BuildPrototypeDefinition(BuildPrototypeKind.Sink, "回收站", new Color("FDE68A"), "接收物品并统计送达数量。"),
        [BuildPrototypeKind.DebugOreSource] = new BuildPrototypeDefinition(BuildPrototypeKind.DebugOreSource, "调试原料源", new Color("4ADE80"), "无成本按配方持续输出单种基础原料，便于快速验证物流与加工链。"),
        [BuildPrototypeKind.DebugPartSource] = new BuildPrototypeDefinition(BuildPrototypeKind.DebugPartSource, "调试部件源", new Color("22D3EE"), "无成本按配方持续输出单种板材或中间件，便于快速验证制造、缓存与装卸。"),
        [BuildPrototypeKind.DebugCombatSource] = new BuildPrototypeDefinition(BuildPrototypeKind.DebugCombatSource, "调试战备源", new Color("FB7185"), "无成本按配方持续输出单种战备或维护补给，便于快速验证防线与支援链。"),
        [BuildPrototypeKind.DebugPowerGenerator] = new BuildPrototypeDefinition(BuildPrototypeKind.DebugPowerGenerator, "永久测试发电机", new Color("FBBF24"), "调试专用永久供电机，无需燃料即可持续给周边电网供能。"),
    };

    public static readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> WorldOnlyDefinitions = new()
    {
        [BuildPrototypeKind.MiningDrill] = new BuildPrototypeDefinition(BuildPrototypeKind.MiningDrill, "采矿机", new Color("FBBF24"), "只能放在矿点上，通电后持续采出煤、铁矿石或铜矿石。"),
        [BuildPrototypeKind.Generator] = new BuildPrototypeDefinition(BuildPrototypeKind.Generator, "发电机", new Color("FB923C"), "消耗煤炭发电，为周边电网提供基础供给。"),
        [BuildPrototypeKind.PowerPole] = new BuildPrototypeDefinition(BuildPrototypeKind.PowerPole, "电线杆", new Color("FDE68A"), "延伸电网覆盖，把发电机接到更远的机器。"),
        [BuildPrototypeKind.Smelter] = new BuildPrototypeDefinition(BuildPrototypeKind.Smelter, "熔炉", new Color("CBD5E1"), "消耗电力把铁矿、铜矿或铁板冶炼成更高阶板材。"),
        [BuildPrototypeKind.Assembler] = new BuildPrototypeDefinition(BuildPrototypeKind.Assembler, "组装机", new Color("67E8F9"), "消耗板材和中间件，组装铜线、电路、机加工件与弹药。"),
        [BuildPrototypeKind.Loader] = new BuildPrototypeDefinition(BuildPrototypeKind.Loader, "装载器", new Color("FDBA74"), "把后方带上的物品装入前方机器或回收端。"),
        [BuildPrototypeKind.Unloader] = new BuildPrototypeDefinition(BuildPrototypeKind.Unloader, "卸载器", new Color("93C5FD"), "把机器端输出卸到前方传送网络。"),
    };

    public static readonly Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> InteriorOnlyDefinitions = new()
    {
        [BuildPrototypeKind.CargoUnpacker] = new BuildPrototypeDefinition(BuildPrototypeKind.CargoUnpacker, "解包模块", new Color("38BDF8"), "模板驱动的解包处理舱。世界大货物会以原尺寸进入舱体，并按 manifest 节拍拆成多个舱内小包。"),
        [BuildPrototypeKind.TransferBuffer] = new BuildPrototypeDefinition(BuildPrototypeKind.TransferBuffer, "中转缓冲槽", new Color("14B8A6"), "重载/节拍缓冲架。既可暂存世界大包，也可作为封包前的小包汇流位。"),
        [BuildPrototypeKind.OutputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.OutputPort, "输出端口", new Color("FB923C"), "把封包完成的世界大货物交给世界侧重型物流，不直接暴露舱内小载具。"),
        [BuildPrototypeKind.InputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.InputPort, "输入端口", new Color("60A5FA"), "把世界大货物接入壳体交接区，再由解包模块转换为舱内料轨可承载的小载具。"),
        [BuildPrototypeKind.MiningInputPort] = new BuildPrototypeDefinition(BuildPrototypeKind.MiningInputPort, "采矿输入端口", new Color("34D399"), "部署后会在工厂外侧展开采矿交接链；散装矿料以世界大件形式进舱，再交给解包模块处理。"),
        [BuildPrototypeKind.Generator] = new BuildPrototypeDefinition(BuildPrototypeKind.Generator, "发电机", new Color("FB923C"), "消耗煤炭发电，为移动工厂内部设备提供基础电力。"),
        [BuildPrototypeKind.PowerPole] = new BuildPrototypeDefinition(BuildPrototypeKind.PowerPole, "电线杆", new Color("FDE68A"), "延伸移动工厂内部的供电覆盖，并可预览连线。"),
        [BuildPrototypeKind.Smelter] = new BuildPrototypeDefinition(BuildPrototypeKind.Smelter, "熔炉", new Color("CBD5E1"), "消耗电力把矿石炼成铁板，便于在内部试配生产链。"),
        [BuildPrototypeKind.Assembler] = new BuildPrototypeDefinition(BuildPrototypeKind.Assembler, "组装机", new Color("67E8F9"), "消耗中间品和电力，在移动工厂内部验证真实配方。"),
        [BuildPrototypeKind.CargoPacker] = new BuildPrototypeDefinition(BuildPrototypeKind.CargoPacker, "封包模块", new Color("F97316"), "模板驱动的封包处理舱。只有累计到目标模板要求后，才会压装成 1 个世界标准大货物。"),
    };

    public static Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> BuildForWorld()
    {
        var dict = new Dictionary<BuildPrototypeKind, BuildPrototypeDefinition>(SharedDefinitions);
        foreach (var kvp in WorldOnlyDefinitions)
        {
            dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }

    public static Dictionary<BuildPrototypeKind, BuildPrototypeDefinition> BuildForInterior()
    {
        var dict = new Dictionary<BuildPrototypeKind, BuildPrototypeDefinition>(SharedDefinitions);
        foreach (var kvp in InteriorOnlyDefinitions)
        {
            dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }
}
