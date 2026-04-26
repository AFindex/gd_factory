using Godot;
using System.Collections.Generic;

internal readonly struct FactoryStructurePortResolution
{
    public FactoryStructurePortResolution(
        FactoryStructure structure,
        ResolvedFactoryStructureLogisticsContract contract,
        bool resolvedFromContractEdge,
        Vector2I resolvedCell,
        FactoryStructureLogisticsAnchor? matchedAnchor = null)
    {
        Structure = structure;
        Contract = contract;
        ResolvedFromContractEdge = resolvedFromContractEdge;
        ResolvedCell = resolvedCell;
        MatchedAnchor = matchedAnchor;
    }

    public FactoryStructure Structure { get; }
    public ResolvedFactoryStructureLogisticsContract Contract { get; }
    public bool ResolvedFromContractEdge { get; }
    public Vector2I ResolvedCell { get; }
    public FactoryStructureLogisticsAnchor? MatchedAnchor { get; }

    public Vector2I ResolveReceiverAcceptanceCell(Vector2I fallbackCell)
    {
        return MatchedAnchor is FactoryStructureLogisticsAnchor matchedAnchor && matchedAnchor.IsInput
            ? matchedAnchor.Cell
            : fallbackCell;
    }

    public Vector2I ResolveProviderDispatchCell(Vector2I fallbackCell)
    {
        return MatchedAnchor is FactoryStructureLogisticsAnchor matchedAnchor && !matchedAnchor.IsInput
            ? matchedAnchor.DispatchSourceCell
            : fallbackCell;
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
            var contract = structure.GetResolvedLogisticsContract();
            var matchedAnchor = contract.TryGetOutputAnchor(providerCell, out var outputAnchor)
                ? outputAnchor
                : default(FactoryStructureLogisticsAnchor?);
            resolution = new FactoryStructurePortResolution(
                structure,
                contract,
                resolvedFromContractEdge: matchedAnchor.HasValue,
                providerCell,
                matchedAnchor);
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
            var contract = structure.GetResolvedLogisticsContract();
            var matchedAnchor = contract.TryGetInputAnchor(targetCell, out var inputAnchor)
                ? inputAnchor
                : default(FactoryStructureLogisticsAnchor?);
            resolution = new FactoryStructurePortResolution(
                structure,
                contract,
                resolvedFromContractEdge: matchedAnchor.HasValue,
                targetCell,
                matchedAnchor);
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

            var contract = candidate.GetResolvedLogisticsContract();
            var anchors = useInputPorts ? contract.InputAnchors : contract.OutputAnchors;
            for (var anchorIndex = 0; anchorIndex < anchors.Count; anchorIndex++)
            {
                if (anchors[anchorIndex].Cell != portCell)
                {
                    continue;
                }

                resolution = new FactoryStructurePortResolution(
                    candidate,
                    contract,
                    resolvedFromContractEdge: true,
                    portCell,
                    anchors[anchorIndex]);
                return true;
            }
        }

        resolution = default;
        return false;
    }
}
