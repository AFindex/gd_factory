using Godot;

public enum BuildPrototypeKind
{
    Producer,
    Belt,
    Sink,
    Splitter,
    Merger,
    Bridge,
    Loader,
    Unloader
}

public enum FacingDirection
{
    East,
    South,
    West,
    North
}

public sealed class FactoryItem
{
    public FactoryItem(int id, BuildPrototypeKind sourceKind)
    {
        Id = id;
        SourceKind = sourceKind;
    }

    public int Id { get; }
    public BuildPrototypeKind SourceKind { get; }
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
