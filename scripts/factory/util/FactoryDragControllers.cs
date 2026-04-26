using Godot;
using System.Collections.Generic;

public sealed class BuildDragState
{
    public bool Active;
    public readonly HashSet<Vector2I> StrokeCells;
    public Vector2I LastStrokeCell;
    public bool HasLastStrokeCell;

    public BuildDragState()
    {
        StrokeCells = new HashSet<Vector2I>();
    }

    public static BuildDragState Create()
    {
        return new BuildDragState();
    }

    public void BeginStroke()
    {
        Active = true;
        StrokeCells.Clear();
    }

    public bool TryRegisterCell(Vector2I cell)
    {
        if (!StrokeCells.Add(cell))
        {
            return false;
        }

        LastStrokeCell = cell;
        HasLastStrokeCell = true;
        return true;
    }

    public void Reset()
    {
        Active = false;
        StrokeCells.Clear();
        HasLastStrokeCell = false;
    }

    public Vector2I PreviousStrokeCell => LastStrokeCell;
}

public sealed class DeleteDragState
{
    public bool Active;
    public Vector2I StartCell;
    public Vector2I CurrentCell;

    public void BeginDrag(Vector2I cell)
    {
        Active = true;
        StartCell = cell;
        CurrentCell = cell;
    }

    public void UpdateCurrentCell(Vector2I cell)
    {
        CurrentCell = cell;
    }

    public void EndDrag()
    {
        Active = false;
    }

    public Rect2I GetRect()
    {
        return FactorySelectionRectSupport.BuildInclusiveRect(StartCell, CurrentCell);
    }
}
