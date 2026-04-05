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
    OutputPort,
    InputPort,
    MiningInputPort
}

public enum FactoryItemKind
{
    GenericCargo,
    Coal,
    IronOre,
    CopperOre,
    IronPlate,
    CopperPlate,
    SteelPlate,
    Gear,
    CopperWire,
    CircuitBoard,
    MachinePart,
    AmmoMagazine,
    HighVelocityAmmo
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
    public FactoryItem(int id, BuildPrototypeKind sourceKind, FactoryItemKind itemKind = FactoryItemKind.GenericCargo)
    {
        Id = id;
        SourceKind = sourceKind;
        ItemKind = itemKind;
    }

    public int Id { get; }
    public BuildPrototypeKind SourceKind { get; }
    public FactoryItemKind ItemKind { get; }
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
            BuildPrototypeKind.OutputPort => "输出端口",
            BuildPrototypeKind.InputPort => "输入端口",
            BuildPrototypeKind.MiningInputPort => "采矿输入端口",
            _ => kind.ToString()
        };
    }

    public static string GetItemLabel(FactoryItem item)
    {
        return $"{GetItemKindLabel(item.ItemKind)} #{item.Id}";
    }

    public static string GetItemKindLabel(FactoryItemKind itemKind)
    {
        return FactoryItemCatalog.GetDisplayName(itemKind);
    }

    public static Color GetItemAccentColor(FactoryItemKind itemKind)
    {
        return FactoryItemCatalog.GetAccentColor(itemKind);
    }

    public static bool IsAmmoItem(FactoryItemKind itemKind)
    {
        return itemKind == FactoryItemKind.AmmoMagazine || itemKind == FactoryItemKind.HighVelocityAmmo;
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
