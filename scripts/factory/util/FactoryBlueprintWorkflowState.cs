using Godot;
using System.Collections.Generic;

public sealed class BlueprintWorkflowState
{
    public bool SelectionDragActive;
    public bool HasSelectionRect;
    public Vector2I SelectionStartCell;
    public Vector2I SelectionCurrentCell;
    public Rect2I SelectionRect;

    public FactoryBlueprintApplyPlan? ApplyPlan;
    public FacingDirection ApplyRotation = FacingDirection.East;

    public FactoryBlueprintSiteAdapter? Site;

    public Node3D? PreviewRoot;
    public Node3D? GhostPreviewRoot;
    public readonly List<MeshInstance3D> PreviewMeshes = new();
    public readonly List<FactoryStructure> PreviewGhosts = new();

    public IFactorySite? FactorySite;

    public void BeginSelection(Vector2I cell)
    {
        SelectionDragActive = true;
        SelectionStartCell = cell;
        SelectionCurrentCell = cell;
        HasSelectionRect = false;
    }

    public Rect2I CompleteDragSelection()
    {
        SelectionDragActive = false;
        SelectionRect = FactorySelectionRectSupport.BuildInclusiveRect(SelectionStartCell, SelectionCurrentCell);
        HasSelectionRect = true;
        return SelectionRect;
    }

    public void ResetSelection()
    {
        SelectionDragActive = false;
        HasSelectionRect = false;
    }

    public void ResetAll()
    {
        SelectionDragActive = false;
        HasSelectionRect = false;
        ApplyPlan = null;
        ApplyRotation = FacingDirection.East;
    }

    public void UpdateApplyPlan(Vector2I hoveredCell)
    {
        if (Site is null)
        {
            ApplyPlan = null;
            return;
        }

        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        if (activeBlueprint is null)
        {
            ApplyPlan = null;
            return;
        }

        ApplyPlan = FactoryBlueprintPlanner.CreatePlan(activeBlueprint, Site, hoveredCell, ApplyRotation);
    }

    public bool HasActiveState()
    {
        return ApplyPlan is not null
            || HasSelectionRect
            || FactoryBlueprintLibrary.GetActive() is not null;
    }

    public void RotatePreview(int direction)
    {
        ApplyRotation = direction < 0
            ? FactoryDirection.RotateCounterClockwise(ApplyRotation)
            : FactoryDirection.RotateClockwise(ApplyRotation);
    }

    public int CountStructuresInRect(Rect2I rect)
    {
        if (Site is null)
        {
            return 0;
        }

        var seen = new HashSet<ulong>();
        foreach (var structure in Site.EnumerateStructures())
        {
            foreach (var occupiedCell in structure.GetOccupiedCells())
            {
                if (!rect.HasPoint(occupiedCell))
                {
                    continue;
                }

                seen.Add(structure.GetInstanceId());
                break;
            }
        }

        return seen.Count;
    }

    public string GetPlanIssueSummary()
    {
        return ApplyPlan is null
            ? "无应用计划。"
            : ApplyPlan.IsValid
                ? $"可应用，占地 {ApplyPlan.FootprintSize.X}x{ApplyPlan.FootprintSize.Y}"
                : ApplyPlan.GetIssueSummary();
    }

    public void EnsurePreviewMeshCapacity(int count, float cellSize)
    {
        FactoryPreviewPoolSupport.EnsureMeshCapacity(
            PreviewRoot,
            PreviewMeshes,
            count,
            _ => new MeshInstance3D
            {
                Visible = false,
                Mesh = new BoxMesh
                {
                    Size = new Vector3(cellSize * 0.84f, 0.10f, cellSize * 0.84f)
                }
            });
    }

    public FactoryStructure EnsureGhostPreview(int index, BuildPrototypeKind kind, Vector2I cell, FacingDirection facing, string namePrefix)
    {
        return FactoryPreviewPoolSupport.EnsureGhostPreview(
            GhostPreviewRoot,
            PreviewGhosts,
            index,
            kind,
            k => FactoryStructureFactory.CreateGhostPreview(
                k,
                new FactoryStructurePlacement(FactorySite!, cell, facing)),
            namePrefix);
    }

    public void HideAllPreviews()
    {
        foreach (var mesh in PreviewMeshes)
        {
            mesh.Visible = false;
        }

        foreach (var ghost in PreviewGhosts)
        {
            ghost.Visible = false;
        }
    }
}
