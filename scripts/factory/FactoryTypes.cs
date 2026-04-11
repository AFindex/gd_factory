using Godot;

public enum BuildPrototypeKind
{
    Producer,
    MiningDrill,
    MiningStake,
    Generator,
    PowerPole,
    Smelter,
    Assembler,
    Belt,
    Sink,
    Splitter,
    Merger,
    Bridge,
    Loader,
    Unloader,
    Storage,
    Inserter,
    Wall,
    AmmoAssembler,
    GunTurret,
    HeavyGunTurret,
    LargeStorageDepot,
    CargoUnpacker,
    CargoPacker,
    TransferBuffer,
    OutputPort,
    InputPort,
    MiningInputPort
}

public enum FactoryItemKind
{
    GenericCargo,
    BuildingKit,
    Coal,
    IronOre,
    CopperOre,
    StoneOre,
    SulfurOre,
    QuartzOre,
    IronPlate,
    CopperPlate,
    StoneBrick,
    SulfurCrystal,
    Glass,
    SteelPlate,
    Gear,
    CopperWire,
    CircuitBoard,
    BatteryPack,
    RepairKit,
    MachinePart,
    AmmoMagazine,
    HighVelocityAmmo
}

public enum FactorySiteKind
{
    World,
    Interior
}

public enum FactoryCargoForm
{
    WorldBulk,
    WorldPacked,
    InteriorFeed
}

public enum FacingDirection
{
    East,
    South,
    West,
    North
}

public enum MobileFactoryLifecycleState
{
    InTransit,
    AutoDeploying,
    Recalling,
    Deployed
}

public enum MobileFactoryControlMode
{
    Player,
    FactoryCommand,
    DeployPreview,
    Observer
}

public enum FactoryInteractionMode
{
    Interact,
    Build,
    Delete
}

public enum FactoryBlueprintWorkflowMode
{
    None,
    CaptureSelection,
    ApplyPreview
}

public enum MobileFactoryInteractionPattern
{
    None,
    DeployPlacement
}

public enum MobileFactoryCommandSlot
{
    Confirm,
    Cancel,
    Auxiliary
}

public enum MobileFactoryAttachmentChannelType
{
    ItemOutput,
    ItemInput
}

public enum MobileFactoryDeployState
{
    Blocked,
    Warning,
    Valid
}

public enum MobileFactoryAttachmentDeployState
{
    Blocked,
    Optional,
    Connected
}

public enum FactoryStatusTone
{
    Positive,
    Warning,
    Negative
}

public sealed class FactoryItem
{
    public FactoryItem(
        int id,
        BuildPrototypeKind sourceKind,
        FactoryItemKind itemKind = FactoryItemKind.GenericCargo,
        FactoryCargoForm cargoForm = FactoryCargoForm.WorldPacked)
    {
        Id = id;
        SourceKind = sourceKind;
        ItemKind = itemKind;
        CargoForm = cargoForm;
    }

    public int Id { get; }
    public BuildPrototypeKind SourceKind { get; }
    public FactoryItemKind ItemKind { get; }
    public FactoryCargoForm CargoForm { get; }
    public FactoryItem WithCargoForm(BuildPrototypeKind sourceKind, FactoryCargoForm cargoForm)
    {
        return new FactoryItem(Id, sourceKind, ItemKind, cargoForm);
    }
}

public sealed class BuildPrototypeDefinition
{
    public BuildPrototypeDefinition(BuildPrototypeKind kind, string displayName, Color tint, string details)
    {
        Kind = kind;
        DisplayName = displayName;
        Tint = tint;
        Details = details;
    }

    public BuildPrototypeKind Kind { get; }
    public string DisplayName { get; }
    public Color Tint { get; }
    public string Details { get; }
}

public static class FactoryPresentation
{
    public static string GetBuildPrototypeDisplayName(BuildPrototypeKind kind)
    {
        return GetKindLabel(kind);
    }

    public static Color GetBuildPrototypeAccentColor(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Producer => new Color("9DC08B"),
            BuildPrototypeKind.MiningDrill => new Color("FBBF24"),
            BuildPrototypeKind.MiningStake => new Color("34D399"),
            BuildPrototypeKind.Generator => new Color("FB923C"),
            BuildPrototypeKind.PowerPole => new Color("FDE68A"),
            BuildPrototypeKind.Smelter => new Color("CBD5E1"),
            BuildPrototypeKind.Assembler => new Color("67E8F9"),
            BuildPrototypeKind.Belt => new Color("7DD3FC"),
            BuildPrototypeKind.Sink => new Color("FDE68A"),
            BuildPrototypeKind.Splitter => new Color("C4B5FD"),
            BuildPrototypeKind.Merger => new Color("99F6E4"),
            BuildPrototypeKind.Bridge => new Color("F59E0B"),
            BuildPrototypeKind.Loader => new Color("FDBA74"),
            BuildPrototypeKind.Unloader => new Color("93C5FD"),
            BuildPrototypeKind.Storage => new Color("94A3B8"),
            BuildPrototypeKind.Inserter => new Color("FACC15"),
            BuildPrototypeKind.Wall => new Color("D1D5DB"),
            BuildPrototypeKind.AmmoAssembler => new Color("FB923C"),
            BuildPrototypeKind.GunTurret => new Color("CBD5E1"),
            BuildPrototypeKind.HeavyGunTurret => new Color("E2E8F0"),
            BuildPrototypeKind.LargeStorageDepot => new Color("64748B"),
            BuildPrototypeKind.CargoUnpacker => new Color("38BDF8"),
            BuildPrototypeKind.CargoPacker => new Color("F97316"),
            BuildPrototypeKind.TransferBuffer => new Color("14B8A6"),
            BuildPrototypeKind.OutputPort => new Color("FB923C"),
            BuildPrototypeKind.InputPort => new Color("60A5FA"),
            BuildPrototypeKind.MiningInputPort => new Color("34D399"),
            _ => new Color("7DD3FC")
        };
    }

    public static string GetKindLabel(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Producer => "生产器",
            BuildPrototypeKind.MiningDrill => "采矿机",
            BuildPrototypeKind.MiningStake => "采矿桩",
            BuildPrototypeKind.Generator => "发电机",
            BuildPrototypeKind.PowerPole => "电线杆",
            BuildPrototypeKind.Smelter => "熔炉",
            BuildPrototypeKind.Assembler => "组装机",
            BuildPrototypeKind.Belt => "传送带",
            BuildPrototypeKind.Sink => "回收站",
            BuildPrototypeKind.Splitter => "分流器",
            BuildPrototypeKind.Merger => "合并器",
            BuildPrototypeKind.Bridge => "跨桥",
            BuildPrototypeKind.Loader => "装载器",
            BuildPrototypeKind.Unloader => "卸载器",
            BuildPrototypeKind.Storage => "仓储",
            BuildPrototypeKind.Inserter => "机械臂",
            BuildPrototypeKind.Wall => "墙体",
            BuildPrototypeKind.AmmoAssembler => "弹药组装器",
            BuildPrototypeKind.GunTurret => "机枪炮塔",
            BuildPrototypeKind.HeavyGunTurret => "重型炮塔",
            BuildPrototypeKind.LargeStorageDepot => "大型仓储",
            BuildPrototypeKind.CargoUnpacker => "解包模块",
            BuildPrototypeKind.CargoPacker => "封包模块",
            BuildPrototypeKind.TransferBuffer => "中转缓冲",
            BuildPrototypeKind.OutputPort => "输出端口",
            BuildPrototypeKind.InputPort => "输入端口",
            BuildPrototypeKind.MiningInputPort => "采矿输入端口",
            _ => kind.ToString()
        };
    }

    public static string GetItemLabel(FactoryItem item)
    {
        return $"{GetItemDisplayName(item)} #{item.Id}";
    }

    public static string GetItemKindLabel(FactoryItemKind itemKind)
    {
        return FactoryItemCatalog.GetDisplayName(itemKind);
    }

    public static string GetCargoFormLabel(FactoryCargoForm cargoForm)
    {
        return cargoForm switch
        {
            FactoryCargoForm.WorldBulk => "世界散装",
            FactoryCargoForm.WorldPacked => "世界封装",
            FactoryCargoForm.InteriorFeed => "内部供料",
            _ => cargoForm.ToString()
        };
    }

    public static string GetItemDisplayName(FactoryItem item)
    {
        return item.ItemKind == FactoryItemKind.BuildingKit
            ? GetBuildPrototypeDisplayName(item.SourceKind)
            : FactoryItemCatalog.GetDisplayName(item.ItemKind, item.CargoForm);
    }

    public static Color GetItemAccentColor(FactoryItemKind itemKind)
    {
        return FactoryItemCatalog.GetAccentColor(itemKind);
    }

    public static Color GetItemAccentColor(FactoryItem item)
    {
        return item.ItemKind == FactoryItemKind.BuildingKit
            ? GetBuildPrototypeAccentColor(item.SourceKind)
            : FactoryItemCatalog.GetAccentColor(item.ItemKind, item.CargoForm);
    }

    public static Texture2D? GetItemIcon(FactoryItemKind itemKind)
    {
        return FactoryItemCatalog.GetIconTexture(itemKind);
    }

    public static Texture2D? GetItemIcon(FactoryItem item)
    {
        return item.ItemKind == FactoryItemKind.BuildingKit
            ? FactoryItemCatalog.GetStructureItemIcon(item.SourceKind)
            : GetItemIcon(item.ItemKind);
    }

    public static bool IsAmmoItem(FactoryItemKind itemKind)
    {
        return itemKind == FactoryItemKind.AmmoMagazine || itemKind == FactoryItemKind.HighVelocityAmmo;
    }

    public static bool IsPlaceableStructureItem(FactoryItem item)
    {
        return item.ItemKind == FactoryItemKind.BuildingKit;
    }

    public static bool TryGetPlaceableStructureKind(FactoryItem? item, out BuildPrototypeKind kind)
    {
        if (item is not null && item.ItemKind == FactoryItemKind.BuildingKit)
        {
            kind = item.SourceKind;
            return true;
        }

        kind = default;
        return false;
    }
}

public static class FactoryDirection
{
    public static Vector2I ToCellOffset(FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.East => Vector2I.Right,
            FacingDirection.South => Vector2I.Down,
            FacingDirection.West => Vector2I.Left,
            FacingDirection.North => Vector2I.Up,
            _ => Vector2I.Right
        };
    }

    public static FacingDirection RotateClockwise(FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.East => FacingDirection.South,
            FacingDirection.South => FacingDirection.West,
            FacingDirection.West => FacingDirection.North,
            _ => FacingDirection.East
        };
    }

    public static FacingDirection RotateCounterClockwise(FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.East => FacingDirection.North,
            FacingDirection.North => FacingDirection.West,
            FacingDirection.West => FacingDirection.South,
            _ => FacingDirection.East
        };
    }

    public static FacingDirection RotateBy(FacingDirection facing, FacingDirection rotationFromEast)
    {
        return rotationFromEast switch
        {
            FacingDirection.East => facing,
            FacingDirection.South => RotateClockwise(facing),
            FacingDirection.West => RotateClockwise(RotateClockwise(facing)),
            FacingDirection.North => RotateCounterClockwise(facing),
            _ => facing
        };
    }

    public static FacingDirection Opposite(FacingDirection facing)
    {
        return RotateClockwise(RotateClockwise(facing));
    }

    public static float ToYRotationRadians(FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.East => 0.0f,
            FacingDirection.South => -Mathf.Pi * 0.5f,
            FacingDirection.West => Mathf.Pi,
            FacingDirection.North => Mathf.Pi * 0.5f,
            _ => 0.0f
        };
    }

    public static FacingDirection FromAngleRadians(float angleRadians)
    {
        var x = Mathf.Cos(angleRadians);
        var z = -Mathf.Sin(angleRadians);

        if (Mathf.Abs(x) >= Mathf.Abs(z))
        {
            return x >= 0.0f ? FacingDirection.East : FacingDirection.West;
        }

        return z >= 0.0f ? FacingDirection.South : FacingDirection.North;
    }

    public static Vector3 ToWorldForward(float angleRadians)
    {
        return new Vector3(Mathf.Cos(angleRadians), 0.0f, -Mathf.Sin(angleRadians));
    }

    public static Vector2I RotateOffset(Vector2I offset, FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.East => offset,
            FacingDirection.South => new Vector2I(-offset.Y, offset.X),
            FacingDirection.West => new Vector2I(-offset.X, -offset.Y),
            FacingDirection.North => new Vector2I(offset.Y, -offset.X),
            _ => offset
        };
    }

    public static string ToLabel(FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.East => "东",
            FacingDirection.South => "南",
            FacingDirection.West => "西",
            FacingDirection.North => "北",
            _ => "东"
        };
    }
}
