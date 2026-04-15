using Godot;
using System.Collections.Generic;

internal readonly struct FactoryStructurePortResolution
{
    public FactoryStructurePortResolution(FactoryStructure structure, bool resolvedFromPortCell)
    {
        Structure = structure;
        ResolvedFromPortCell = resolvedFromPortCell;
    }

    public FactoryStructure Structure { get; }
    public bool ResolvedFromPortCell { get; }

    public Vector2I ResolveEffectiveSourceCell(Vector2I sourceCell, Vector2I portCell)
    {
        if (ResolvedFromPortCell)
        {
            return portCell;
        }

        var inputCells = Structure.GetInputCells();
        for (var index = 0; index < inputCells.Count; index++)
        {
            if (inputCells[index] == portCell)
            {
                return portCell;
            }
        }

        return sourceCell;
    }
}

internal static class FactoryStructurePortResolver
{
    private static readonly IReadOnlyList<Vector2I> NeighborOffsets = new[]
    {
        Vector2I.Left,
        Vector2I.Right,
        Vector2I.Up,
        Vector2I.Down
    };

    public static bool TryResolveProvider(IFactorySite site, Vector2I providerCell, out FactoryStructurePortResolution resolution)
    {
        if (site.TryGetStructure(providerCell, out var structure) && structure is not null)
        {
            resolution = new FactoryStructurePortResolution(structure, resolvedFromPortCell: false);
            return true;
        }

        return TryResolveByPortCell(site, providerCell, useInputPorts: false, out resolution);
    }

    public static bool TryResolveReceiver(IFactorySite site, Vector2I targetCell, out FactoryStructurePortResolution resolution)
    {
        return TryResolveDirectReceiver(site, targetCell, out resolution)
            || TryResolveReceiverByInputPort(site, targetCell, out resolution);
    }

    public static bool TryResolveDirectReceiver(IFactorySite site, Vector2I targetCell, out FactoryStructurePortResolution resolution)
    {
        if (site.TryGetStructure(targetCell, out var structure) && structure is not null)
        {
            resolution = new FactoryStructurePortResolution(structure, resolvedFromPortCell: false);
            return true;
        }

        resolution = default;
        return false;
    }

    public static bool TryResolveReceiverByInputPort(IFactorySite site, Vector2I targetCell, out FactoryStructurePortResolution resolution)
    {
        return TryResolveByPortCell(site, targetCell, useInputPorts: true, out resolution);
    }

    private static bool TryResolveByPortCell(
        IFactorySite site,
        Vector2I portCell,
        bool useInputPorts,
        out FactoryStructurePortResolution resolution)
    {
        var seen = new HashSet<ulong>();
        for (var index = 0; index < NeighborOffsets.Count; index++)
        {
            var candidateCell = portCell + NeighborOffsets[index];
            if (!site.TryGetStructure(candidateCell, out var candidate) || candidate is null)
            {
                continue;
            }

            if (!seen.Add(candidate.GetInstanceId()))
            {
                continue;
            }

            var portCells = useInputPorts ? candidate.GetInputCells() : candidate.GetOutputCells();
            for (var portIndex = 0; portIndex < portCells.Count; portIndex++)
            {
                if (portCells[portIndex] != portCell)
                {
                    continue;
                }

                resolution = new FactoryStructurePortResolution(candidate, resolvedFromPortCell: true);
                return true;
            }
        }

        resolution = default;
        return false;
    }
}
