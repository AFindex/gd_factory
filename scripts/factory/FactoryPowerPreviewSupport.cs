using Godot;
using System;
using System.Collections.Generic;

public static class FactoryPowerPreviewSupport
{
    public static void RenderPowerLinkSet(
        Node3D? structureRoot,
        Vector3 origin,
        Vector2I originCell,
        int originRange,
        Color color,
        Func<FactoryStructure, Vector3> getPowerAnchor,
        Func<Vector3, Vector3, Color, int, int> drawDash,
        Action<int> setDashCount,
        IFactorySite? site = null,
        FactoryStructure? exclude = null)
    {
        var targets = CollectConnectablePowerNodes(structureRoot, originCell, originRange, site, exclude);
        if (targets.Count == 0)
        {
            setDashCount(0);
            return;
        }

        var dashIndex = 0;
        for (var index = 0; index < targets.Count; index++)
        {
            dashIndex = drawDash(origin, getPowerAnchor(targets[index]), color, dashIndex);
        }

        setDashCount(dashIndex);
    }

    public static List<FactoryStructure> CollectConnectablePowerNodes(
        Node3D? structureRoot,
        Vector2I originCell,
        int originRange,
        IFactorySite? site = null,
        FactoryStructure? exclude = null)
    {
        if (structureRoot is null)
        {
            return new List<FactoryStructure>();
        }

        var candidates = new List<(FactoryStructure structure, float distance)>();
        var origin = new Vector2(originCell.X, originCell.Y);
        foreach (var child in structureRoot.GetChildren())
        {
            if (child is not FactoryStructure structure
                || (site is not null && structure.Site != site)
                || structure == exclude
                || structure.IsDestroyed
                || !structure.Site.IsSimulationActive
                || structure is not IFactoryPowerNode powerNode
                || powerNode.PowerConnectionRangeCells <= 0
                || structure.Cell == originCell)
            {
                continue;
            }

            var target = new Vector2(structure.Cell.X, structure.Cell.Y);
            var distance = origin.DistanceTo(target);
            if (distance > originRange + powerNode.PowerConnectionRangeCells)
            {
                continue;
            }

            candidates.Add((structure, distance));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        var ordered = new List<FactoryStructure>(candidates.Count);
        for (var index = 0; index < candidates.Count; index++)
        {
            ordered.Add(candidates[index].structure);
        }

        return ordered;
    }

    public static void UpdatePreviewPowerRange(BuildPrototypeKind? kind, IFactorySite site, MeshInstance3D previewPowerRange, Color tint)
    {
        if (!TryGetPreviewRangeInfo(kind, site, out var rangeRadius, out var alpha))
        {
            previewPowerRange.Visible = false;
            return;
        }

        previewPowerRange.Mesh = new CylinderMesh
        {
            TopRadius = rangeRadius,
            BottomRadius = rangeRadius,
            Height = 0.03f
        };
        previewPowerRange.Position = new Vector3(0.0f, 0.02f, 0.0f);
        previewPowerRange.Visible = true;
        FactoryPreviewOverlaySupport.ApplyPreviewColor(previewPowerRange, new Color(tint.R, tint.G, tint.B, alpha));
    }

    public static bool TryGetPowerPreviewInfo(BuildPrototypeKind? kind, out int rangeCells)
    {
        switch (kind)
        {
            case BuildPrototypeKind.Generator:
                rangeCells = 5;
                return true;
            case BuildPrototypeKind.DebugPowerGenerator:
                rangeCells = 6;
                return true;
            case BuildPrototypeKind.PowerPole:
                rangeCells = FactoryPreviewOverlaySupport.PreviewPowerPoleConnectionRangeCells;
                return true;
            default:
                rangeCells = 0;
                return false;
        }
    }

    private static bool TryGetPreviewRangeInfo(BuildPrototypeKind? kind, IFactorySite site, out float rangeRadius, out float alpha)
    {
        switch (kind)
        {
            case BuildPrototypeKind.Generator:
                rangeRadius = site.CellSize * 5;
                alpha = 0.15f;
                return true;
            case BuildPrototypeKind.DebugPowerGenerator:
                rangeRadius = site.CellSize * 6;
                alpha = 0.15f;
                return true;
            case BuildPrototypeKind.PowerPole:
                rangeRadius = site.CellSize * FactoryPreviewOverlaySupport.PreviewPowerPoleConnectionRangeCells;
                alpha = 0.15f;
                return true;
            case BuildPrototypeKind.GunTurret:
                rangeRadius = FactoryConstants.GunTurretRange;
                alpha = 0.20f;
                return true;
            case BuildPrototypeKind.HeavyGunTurret:
                rangeRadius = FactoryConstants.HeavyGunTurretRange;
                alpha = 0.20f;
                return true;
            default:
                rangeRadius = 0.0f;
                alpha = 0.0f;
                return false;
        }
    }
}
