using Godot;
using System.Collections.Generic;

public sealed class PowerLinkPreviewContext
{
    public Node3D? OverlayRoot;
    public readonly List<MeshInstance3D> Dashes = new();
    public string DashNamePrefix = "PowerLinkDash";

    public void RenderPowerLinkSet(
        Node3D structureRoot,
        Vector3 origin,
        Vector2I originCell,
        int originRange,
        Color color,
        IFactorySite? site = null,
        FactoryStructure? exclude = null)
    {
        FactoryPowerPreviewSupport.RenderPowerLinkSet(
            structureRoot,
            origin,
            originCell,
            originRange,
            color,
            GetPowerAnchor,
            DrawDashedLink,
            SetVisibleCount,
            site,
            exclude);
    }

    private int DrawDashedLink(Vector3 start, Vector3 end, Color color, int dashIndex)
    {
        return FactoryPreviewOverlaySupport.DrawDashedPowerLink(
            start, end, color, dashIndex,
            EnsureCapacity, Dashes, ApplyColor);
    }

    private void EnsureCapacity(int count)
    {
        FactoryPreviewOverlaySupport.EnsurePowerLinkDashCapacity(OverlayRoot, Dashes, count, DashNamePrefix);
    }

    private void SetVisibleCount(int visibleCount)
    {
        if (OverlayRoot is null)
        {
            return;
        }

        for (var i = visibleCount; i < Dashes.Count; i++)
        {
            Dashes[i].Visible = false;
        }

        OverlayRoot.Visible = visibleCount > 0;
    }

    public static Vector3 GetPowerAnchor(FactoryStructure structure)
    {
        var height = structure switch
        {
            PowerPoleStructure => 1.44f,
            GeneratorStructure => 1.06f,
            DebugPowerGeneratorStructure => 1.12f,
            _ => 1.18f
        };
        return structure.GlobalPosition + new Vector3(0.0f, height, 0.0f);
    }

    public static Vector3 GetPreviewPowerAnchor(IFactorySite site, Vector2I cell, float height)
    {
        return site.CellToWorld(cell) + new Vector3(0.0f, height, 0.0f);
    }

    public static Vector3 GetPreviewPowerAnchorWorld(Vector2I cell, float height)
    {
        return new Vector3(
            cell.X * FactoryConstants.CellSize,
            height,
            cell.Y * FactoryConstants.CellSize);
    }

    public static void ApplyColor(MeshInstance3D dash, Color color)
    {
        if (dash.MaterialOverride is not StandardMaterial3D material)
        {
            return;
        }

        material.AlbedoColor = color;
        material.Emission = color.Lightened(0.08f);
    }

    public static bool ShouldShowSelectionRange(FactoryStructure structure, bool isPowerPreviewActive, bool isInInteractionMode, bool isSelectedStructure)
    {
        return GodotObject.IsInstanceValid(structure)
            && structure.IsInsideTree()
            && ((isPowerPreviewActive && structure is IFactoryPowerNode)
                || (isInInteractionMode && isSelectedStructure && structure.SupportsSelectionRangeIndicator));
    }

    public static bool IsPowerPreviewActive(
        FactoryInteractionMode interactionMode,
        bool hasHoveredCell,
        BuildPrototypeKind? selectedBuildKind,
        FactoryStructure? selectedStructure)
    {
        return interactionMode == FactoryInteractionMode.Build
            ? hasHoveredCell
                && (selectedBuildKind == BuildPrototypeKind.Generator
                    || selectedBuildKind == BuildPrototypeKind.DebugPowerGenerator
                    || selectedBuildKind == BuildPrototypeKind.PowerPole)
            : interactionMode == FactoryInteractionMode.Interact && selectedStructure is IFactoryPowerNode;
    }
}
