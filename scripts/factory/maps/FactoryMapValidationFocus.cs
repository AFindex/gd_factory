using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public enum FactoryMapValidationMapScope
{
    World,
    Interior
}

public sealed class FactoryMapValidationFocusResult
{
    public FactoryMapValidationFocusResult(
        string targetId,
        FactoryMapValidationMapScope scope,
        string mapPath,
        Vector2I requestedCell,
        FactoryStructure? structure,
        IReadOnlyList<FactoryMapValidationDiagnostic> diagnostics)
    {
        TargetId = targetId;
        Scope = scope;
        MapPath = mapPath;
        RequestedCell = requestedCell;
        Structure = structure;
        Diagnostics = diagnostics;
        Summary = new FactoryMapValidationTargetSummary(diagnostics);
    }

    public string TargetId { get; }
    public FactoryMapValidationMapScope Scope { get; }
    public string MapPath { get; }
    public Vector2I RequestedCell { get; }
    public FactoryStructure? Structure { get; }
    public IReadOnlyList<FactoryMapValidationDiagnostic> Diagnostics { get; }
    public FactoryMapValidationTargetSummary Summary { get; }
    public bool HasErrors => Summary.ErrorCount > 0;
}

internal sealed class FactoryMapValidationContext
{
    private readonly Dictionary<ulong, FactoryMapStructureEntry?> _entriesByStructureId = new();
    private readonly Dictionary<Vector2I, FactoryStructure> _structuresByOccupiedCell = new();

    public FactoryMapValidationContext(
        string targetId,
        string mapPath,
        FactoryMapDocument document,
        IReadOnlyList<FactoryStructure> structures,
        SimulationController simulation)
    {
        TargetId = targetId;
        MapPath = mapPath;
        Document = document;
        Structures = structures;
        Simulation = simulation;

        for (var i = 0; i < structures.Count; i++)
        {
            var structure = structures[i];
            _entriesByStructureId[structure.GetInstanceId()] = document.TryGetStructure(structure.Cell, out var entry)
                ? entry
                : null;

            foreach (var occupiedCell in structure.GetOccupiedCells())
            {
                _structuresByOccupiedCell[occupiedCell] = structure;
            }
        }
    }

    public string TargetId { get; }
    public string MapPath { get; }
    public FactoryMapDocument Document { get; }
    public IReadOnlyList<FactoryStructure> Structures { get; }
    public SimulationController Simulation { get; }

    public FactoryMapStructureEntry? GetEntry(FactoryStructure structure)
    {
        return _entriesByStructureId.TryGetValue(structure.GetInstanceId(), out var entry)
            ? entry
            : null;
    }

    public bool TryGetStructureAt(Vector2I cell, out FactoryStructure? structure)
    {
        return _structuresByOccupiedCell.TryGetValue(cell, out structure);
    }

    public List<FactoryStructure> GetUpstreamNeighbors(FactoryStructure structure)
    {
        var neighbors = new List<FactoryStructure>();
        var seen = new HashSet<ulong>();
        var inputCells = FactoryMapValidationTopologyHelper.GetInputCells(structure);
        for (var i = 0; i < inputCells.Count; i++)
        {
            if (!FactoryMapValidationTopologyHelper.TryGetInputProvider(structure, inputCells[i], out var producer) || producer is null)
            {
                continue;
            }

            if (seen.Add(producer.GetInstanceId()))
            {
                neighbors.Add(producer);
            }
        }

        return neighbors;
    }

    public List<FactoryStructure> GetDownstreamNeighbors(FactoryStructure structure)
    {
        var neighbors = new List<FactoryStructure>();
        var seen = new HashSet<ulong>();
        var outputCells = FactoryMapValidationTopologyHelper.GetOutputCells(structure);
        for (var i = 0; i < outputCells.Count; i++)
        {
            if (!FactoryMapValidationTopologyHelper.TryGetOutputReceiver(structure, outputCells[i], out var neighbor) || neighbor is null)
            {
                continue;
            }

            if (seen.Add(neighbor.GetInstanceId()))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }
}

internal static class FactoryMapValidationTopologyHelper
{
    public static IReadOnlyList<Vector2I> GetInputCells(FactoryStructure structure)
    {
        return structure.Kind switch
        {
            BuildPrototypeKind.InputPort => System.Array.Empty<Vector2I>(),
            BuildPrototypeKind.MiningInputPort => System.Array.Empty<Vector2I>(),
            BuildPrototypeKind.OutputPort => new[]
            {
                structure.Cell - FactoryDirection.ToCellOffset(structure.Facing)
            },
            _ => FactoryTransportTopology.GetInputCells(structure)
        };
    }

    public static IReadOnlyList<Vector2I> GetOutputCells(FactoryStructure structure)
    {
        return structure.Kind switch
        {
            BuildPrototypeKind.InputPort => new[]
            {
                structure.Cell - FactoryDirection.ToCellOffset(structure.Facing)
            },
            BuildPrototypeKind.MiningInputPort => new[]
            {
                structure.Cell - FactoryDirection.ToCellOffset(structure.Facing)
            },
            BuildPrototypeKind.OutputPort => System.Array.Empty<Vector2I>(),
            _ => FactoryTransportTopology.GetOutputCells(structure)
        };
    }

    public static bool TryGetInputProvider(FactoryStructure receiver, Vector2I inputCell, out FactoryStructure? provider)
    {
        provider = null;
        if (!receiver.Site.TryGetStructure(inputCell, out var candidate) || candidate is null)
        {
            return false;
        }

        var candidateOutputCells = GetOutputCells(candidate);
        foreach (var occupiedCell in receiver.GetOccupiedCells())
        {
            for (var outputIndex = 0; outputIndex < candidateOutputCells.Count; outputIndex++)
            {
                if (candidateOutputCells[outputIndex] != occupiedCell)
                {
                    continue;
                }

                provider = candidate;
                return true;
            }
        }

        return false;
    }

    public static bool TryGetOutputReceiver(FactoryStructure source, Vector2I outputCell, out FactoryStructure? receiver)
    {
        receiver = null;
        if (!source.Site.TryGetStructure(outputCell, out var candidate) || candidate is null)
        {
            return false;
        }

        var sourceCell = source.GetTransferOutputCell(outputCell);
        var candidateInputCells = GetInputCells(candidate);
        for (var inputIndex = 0; inputIndex < candidateInputCells.Count; inputIndex++)
        {
            if (candidateInputCells[inputIndex] != sourceCell)
            {
                continue;
            }

            receiver = candidate;
            return true;
        }
        return false;
    }
}

public static partial class FactoryMapValidationService
{
    private static readonly IReadOnlyDictionary<BuildPrototypeKind, IReadOnlyList<FactoryRecipeDefinition>> RecipeCatalogByKind =
        new Dictionary<BuildPrototypeKind, IReadOnlyList<FactoryRecipeDefinition>>
        {
            [BuildPrototypeKind.MiningDrill] = FactoryRecipeCatalog.MiningDrillRecipes,
            [BuildPrototypeKind.Smelter] = FactoryRecipeCatalog.SmelterRecipes,
            [BuildPrototypeKind.Assembler] = FactoryRecipeCatalog.AssemblerRecipes,
            [BuildPrototypeKind.AmmoAssembler] = FactoryRecipeCatalog.AmmoAssemblerRecipes
        };

    public static FactoryMapValidationFocusResult ValidateFocus(string targetId, FactoryMapValidationMapScope scope, Vector2I cell)
    {
        if (!FactoryMapValidationCatalog.TryGetTarget(targetId, out var target) || target is null)
        {
            throw new InvalidOperationException(
                $"Unknown factory map validation target '{targetId}'. Available targets: {string.Join(", ", GetAvailableTargetIds())}.");
        }

        return scope switch
        {
            FactoryMapValidationMapScope.Interior => ValidateInteriorFocus(target, cell),
            _ => ValidateWorldFocus(target, cell)
        };
    }

    public static void PrintFocusReport(FactoryMapValidationFocusResult result)
    {
        var structureText = result.Structure is null
            ? "none"
            : $"{result.Structure.Kind}@({result.Structure.Cell.X},{result.Structure.Cell.Y})";
        GD.Print(
            $"FACTORY_MAP_VALIDATION_FOCUS target={result.TargetId} scope={result.Scope} map={result.MapPath} requested=({result.RequestedCell.X},{result.RequestedCell.Y}) structure={structureText} errors={result.Summary.ErrorCount} warnings={result.Summary.WarningCount} info={result.Summary.InfoCount}");

        for (var i = 0; i < result.Diagnostics.Count; i++)
        {
            PrintDiagnostic(result.Diagnostics[i]);
        }
    }

    private static FactoryMapValidationFocusResult ValidateWorldFocus(FactoryMapValidationTarget target, Vector2I cell)
    {
        Node3D? structureRoot = null;
        SimulationController? simulation = null;
        try
        {
            var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(target.WorldMapPath));
            var worldGrid = new GridManager(document.MinCell, document.MaxCell, FactoryConstants.CellSize);
            simulation = new SimulationController();
            simulation.Configure(worldGrid);
            structureRoot = new Node3D { Name = $"ValidationFocus_{target.Id}" };
            var loadResult = FactoryMapRuntimeLoader.LoadWorldMap(target.WorldMapPath, worldGrid, structureRoot, simulation);
            simulation.RebuildTopology();

            var context = new FactoryMapValidationContext(target.Id, target.WorldMapPath, loadResult.Document, loadResult.LoadedStructures, simulation);
            var diagnostics = BuildFocusDiagnostics(context, cell);
            return new FactoryMapValidationFocusResult(
                target.Id,
                FactoryMapValidationMapScope.World,
                target.WorldMapPath,
                cell,
                context.TryGetStructureAt(cell, out var structure) ? structure : null,
                diagnostics);
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
        {
            return new FactoryMapValidationFocusResult(
                target.Id,
                FactoryMapValidationMapScope.World,
                target.WorldMapPath,
                cell,
                null,
                new[]
                {
                    new FactoryMapValidationDiagnostic(
                        target.Id,
                        FactoryMapValidationSeverity.Error,
                        "world-focus",
                        ex.Message,
                        target.WorldMapPath,
                        cell: cell)
                });
        }
        finally
        {
            structureRoot?.Free();
            simulation?.Free();
        }
    }

    private static FactoryMapValidationFocusResult ValidateInteriorFocus(FactoryMapValidationTarget target, Vector2I cell)
    {
        if (target.Kind != FactoryMapValidationTargetKind.MobileFactoryBundle
            || string.IsNullOrWhiteSpace(target.InteriorMapPath)
            || target.CreateProfile is null)
        {
            throw new InvalidOperationException($"Target '{target.Id}' does not expose an interior map for focused validation.");
        }

        SimulationController? simulation = null;
        Node3D? structureRoot = null;
        try
        {
            var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(target.InteriorMapPath));
            var profile = target.CreateProfile();
            FactoryMapRuntimeLoader.ValidateInteriorDocumentAgainstProfile(document, profile, target.InteriorMapPath);

            simulation = new SimulationController();
            structureRoot = new Node3D { Name = $"ValidationFocus_{target.Id}_Interior" };
            var preset = FactoryMapRuntimeLoader.LoadInteriorPreset(
                target.InteriorMapPath,
                $"{target.Id}-focus",
                $"{target.DisplayName} Focus",
                "Focused validation preset.",
                "Focused validation preset.",
                profile);
            var factory = new MobileFactoryInstance(
                $"focus-{target.Id}",
                structureRoot,
                simulation,
                profile,
                preset);
            FactoryMapRuntimeLoader.ApplyInteriorRuntimeState(target.InteriorMapPath, factory, simulation);
            simulation.RebuildTopology();

            var structures = CollectInteriorStructures(factory);
            var context = new FactoryMapValidationContext(target.Id, target.InteriorMapPath, document, structures, simulation);
            var diagnostics = BuildFocusDiagnostics(context, cell);
            return new FactoryMapValidationFocusResult(
                target.Id,
                FactoryMapValidationMapScope.Interior,
                target.InteriorMapPath,
                cell,
                context.TryGetStructureAt(cell, out var structure) ? structure : null,
                diagnostics);
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
        {
            return new FactoryMapValidationFocusResult(
                target.Id,
                FactoryMapValidationMapScope.Interior,
                target.InteriorMapPath,
                cell,
                null,
                new[]
                {
                    new FactoryMapValidationDiagnostic(
                        target.Id,
                        FactoryMapValidationSeverity.Error,
                        "interior-focus",
                        ex.Message,
                        target.InteriorMapPath,
                        cell: cell)
                });
        }
        finally
        {
            structureRoot?.Free();
            simulation?.Free();
        }
    }

    private static void AnalyzeValidationContext(
        string targetId,
        string mapPath,
        FactoryMapDocument document,
        IReadOnlyList<FactoryStructure> structures,
        SimulationController simulation,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        var context = new FactoryMapValidationContext(targetId, mapPath, document, structures, simulation);
        AnalyzeConnectivity(targetId, mapPath, structures, simulation, diagnostics);
        AnalyzeRecipeSemantics(context, diagnostics);
        AnalyzeBoundaryFlowSemantics(context, diagnostics);
        AnalyzeDualStandardsChain(context, diagnostics);
    }

    private static List<FactoryMapValidationDiagnostic> BuildFocusDiagnostics(FactoryMapValidationContext context, Vector2I cell)
    {
        var diagnostics = new List<FactoryMapValidationDiagnostic>();
        if (!context.TryGetStructureAt(cell, out var structure) || structure is null)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                context.TargetId,
                FactoryMapValidationSeverity.Warning,
                "focus",
                "No structure occupies the requested cell in the reconstructed map.",
                context.MapPath,
                cell: cell));
            return diagnostics;
        }

        diagnostics.Add(new FactoryMapValidationDiagnostic(
            context.TargetId,
            FactoryMapValidationSeverity.Info,
            "focus",
            $"Focused structure resolved to {structure.Kind} anchored at ({structure.Cell.X}, {structure.Cell.Y}).",
            context.MapPath,
            structure.Kind,
            structure.Cell));
        diagnostics.Add(new FactoryMapValidationDiagnostic(
            context.TargetId,
            FactoryMapValidationSeverity.Info,
            "focus",
            $"Occupied cells: {JoinCells(structure.GetOccupiedCells())}. Inputs: {JoinCells(FactoryTransportTopology.GetInputCells(structure))}. Outputs: {JoinCells(FactoryTransportTopology.GetOutputCells(structure))}.",
            context.MapPath,
            structure.Kind,
            structure.Cell));

        var upstream = context.GetUpstreamNeighbors(structure);
        diagnostics.Add(new FactoryMapValidationDiagnostic(
            context.TargetId,
            upstream.Count == 0 ? FactoryMapValidationSeverity.Warning : FactoryMapValidationSeverity.Info,
            "focus-connectivity",
            upstream.Count == 0
                ? "No physically connected upstream neighbors were found."
                : $"Upstream neighbors: {JoinStructures(upstream)}.",
            context.MapPath,
            structure.Kind,
            structure.Cell));

        var downstream = context.GetDownstreamNeighbors(structure);
        diagnostics.Add(new FactoryMapValidationDiagnostic(
            context.TargetId,
            downstream.Count == 0 ? FactoryMapValidationSeverity.Warning : FactoryMapValidationSeverity.Info,
            "focus-connectivity",
            downstream.Count == 0
                ? "No physically connected downstream neighbors were found."
                : $"Downstream neighbors: {JoinStructures(downstream)}.",
            context.MapPath,
            structure.Kind,
            structure.Cell));

        AnalyzeLogisticsConnectivity(context.TargetId, context.MapPath, structure, diagnostics);
        AnalyzePowerConnectivity(context.TargetId, context.MapPath, structure, CollectPowerNodes(context.Structures), context.Simulation, diagnostics);
        AnalyzeRecipeSemantics(context, diagnostics, structure, includeSatisfiedInfo: true);
        AnalyzeBoundaryFlowSemantics(context, diagnostics, structure, includeSatisfiedInfo: true);
        return diagnostics;
    }

    private static void AnalyzeBoundaryFlowSemantics(
        FactoryMapValidationContext context,
        List<FactoryMapValidationDiagnostic> diagnostics,
        FactoryStructure? focusStructure = null,
        bool includeSatisfiedInfo = false)
    {
        for (var i = 0; i < context.Structures.Count; i++)
        {
            var structure = context.Structures[i];
            if (focusStructure is not null && structure != focusStructure)
            {
                continue;
            }

            var expectation = FactoryCargoRules.DescribeBoundaryExpectation(structure.Kind);
            if (includeSatisfiedInfo && !string.IsNullOrWhiteSpace(expectation))
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    context.TargetId,
                    FactoryMapValidationSeverity.Info,
                    "boundary-flow",
                    expectation,
                    context.MapPath,
                    structure.Kind,
                    structure.Cell));
            }

            switch (structure.Kind)
            {
                case BuildPrototypeKind.InputPort:
                case BuildPrototypeKind.MiningInputPort:
                    if (TryFindReachableDownstreamStructure(context, structure, IsInteriorUnpacker, CanRelayCargoPath, out var unpacker))
                    {
                        if (includeSatisfiedInfo && unpacker is not null)
                        {
                            diagnostics.Add(new FactoryMapValidationDiagnostic(
                                context.TargetId,
                                FactoryMapValidationSeverity.Info,
                                "boundary-flow",
                                $"Boundary intake reaches {unpacker.Kind} at ({unpacker.Cell.X}, {unpacker.Cell.Y}) before entering the interior feed network.",
                                context.MapPath,
                                structure.Kind,
                                structure.Cell));
                        }
                    }
                    else
                    {
                        diagnostics.Add(new FactoryMapValidationDiagnostic(
                            context.TargetId,
                            FactoryMapValidationSeverity.Warning,
                            "boundary-flow",
                            "Boundary intake has no reachable CargoUnpacker; cross-standard intake will stay blocked or semantically incomplete.",
                            context.MapPath,
                            structure.Kind,
                            structure.Cell));
                    }

                    break;

                case BuildPrototypeKind.OutputPort:
                    if (TryFindReachableUpstreamStructure(context, structure, IsInteriorPacker, CanRelayCargoPath, out var packer))
                    {
                        if (includeSatisfiedInfo && packer is not null)
                        {
                            diagnostics.Add(new FactoryMapValidationDiagnostic(
                                context.TargetId,
                                FactoryMapValidationSeverity.Info,
                                "boundary-flow",
                                $"Boundary output is staged by {packer.Kind} at ({packer.Cell.X}, {packer.Cell.Y}) before leaving the hull.",
                                context.MapPath,
                                structure.Kind,
                                structure.Cell));
                        }
                    }
                    else
                    {
                        diagnostics.Add(new FactoryMapValidationDiagnostic(
                            context.TargetId,
                            FactoryMapValidationSeverity.Warning,
                            "boundary-flow",
                            "Boundary output has no reachable CargoPacker; interior feed cannot be exported as world-standard cargo.",
                            context.MapPath,
                            structure.Kind,
                            structure.Cell));
                    }

                    break;

                case BuildPrototypeKind.CargoUnpacker:
                    if (!TryFindReachableUpstreamStructure(context, structure, IsBoundaryIntake, CanRelayCargoPath, out _))
                    {
                        diagnostics.Add(new FactoryMapValidationDiagnostic(
                            context.TargetId,
                            FactoryMapValidationSeverity.Warning,
                            "cargo-conversion",
                            "CargoUnpacker has no reachable world-side intake attachment upstream.",
                            context.MapPath,
                            structure.Kind,
                            structure.Cell));
                    }

                    if (!TryFindReachableDownstreamStructure(context, structure, IsInteriorProcessingNode, CanRelayCargoPath, out _))
                    {
                        diagnostics.Add(new FactoryMapValidationDiagnostic(
                            context.TargetId,
                            FactoryMapValidationSeverity.Warning,
                            "cargo-conversion",
                            "CargoUnpacker does not feed any reachable interior processing or staging node.",
                            context.MapPath,
                            structure.Kind,
                            structure.Cell));
                    }

                    break;

                case BuildPrototypeKind.CargoPacker:
                    if (!TryFindReachableUpstreamStructure(context, structure, IsInteriorProcessingNode, CanRelayCargoPath, out _))
                    {
                        diagnostics.Add(new FactoryMapValidationDiagnostic(
                            context.TargetId,
                            FactoryMapValidationSeverity.Warning,
                            "cargo-conversion",
                            "CargoPacker has no reachable interior feed source upstream.",
                            context.MapPath,
                            structure.Kind,
                            structure.Cell));
                    }

                    if (!TryFindReachableDownstreamStructure(context, structure, structureCandidate => structureCandidate.Kind == BuildPrototypeKind.OutputPort, CanRelayCargoPath, out _))
                    {
                        diagnostics.Add(new FactoryMapValidationDiagnostic(
                            context.TargetId,
                            FactoryMapValidationSeverity.Warning,
                            "cargo-conversion",
                            "CargoPacker does not reach any boundary output port downstream.",
                            context.MapPath,
                            structure.Kind,
                            structure.Cell));
                    }

                    break;
            }
        }
    }

    private static void AnalyzeDualStandardsChain(
        FactoryMapValidationContext context,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        FactoryStructure? completedIntake = null;
        FactoryStructure? completedUnpacker = null;
        FactoryStructure? completedProcessing = null;
        FactoryStructure? completedPacker = null;
        FactoryStructure? completedOutput = null;

        for (var i = 0; i < context.Structures.Count; i++)
        {
            var structure = context.Structures[i];
            if (!IsBoundaryIntake(structure))
            {
                continue;
            }

            if (!TryFindReachableDownstreamStructure(context, structure, IsInteriorUnpacker, CanRelayCargoPath, out var unpacker)
                || unpacker is null)
            {
                continue;
            }

            if (!TryFindReachableDownstreamStructure(context, unpacker, IsInteriorProcessingNode, CanRelayCargoPath, out var processingNode)
                || processingNode is null)
            {
                continue;
            }

            if (!TryFindReachableDownstreamStructure(context, processingNode, IsInteriorPacker, CanRelayCargoPath, out var packer)
                || packer is null)
            {
                continue;
            }

            if (!TryFindReachableDownstreamStructure(context, packer, candidate => candidate.Kind == BuildPrototypeKind.OutputPort, CanRelayCargoPath, out var outputPort)
                || outputPort is null)
            {
                continue;
            }

            completedIntake = structure;
            completedUnpacker = unpacker;
            completedProcessing = processingNode;
            completedPacker = packer;
            completedOutput = outputPort;
            break;
        }

        var hasDualStandardStructures = false;
        for (var i = 0; i < context.Structures.Count; i++)
        {
            if (IsBoundaryIntake(context.Structures[i])
                || context.Structures[i].Kind == BuildPrototypeKind.CargoUnpacker
                || context.Structures[i].Kind == BuildPrototypeKind.CargoPacker
                || context.Structures[i].Kind == BuildPrototypeKind.OutputPort)
            {
                hasDualStandardStructures = true;
                break;
            }
        }

        if (!hasDualStandardStructures)
        {
            return;
        }

        if (completedIntake is not null
            && completedUnpacker is not null
            && completedProcessing is not null
            && completedPacker is not null
            && completedOutput is not null)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                context.TargetId,
                FactoryMapValidationSeverity.Info,
                "dual-standards-chain",
                $"Validated one world-to-interior chain: {completedIntake.Kind} -> {completedUnpacker.Kind} -> {completedProcessing.Kind} -> {completedPacker.Kind} -> {completedOutput.Kind}.",
                context.MapPath,
                completedIntake.Kind,
                completedIntake.Cell));
            return;
        }

        diagnostics.Add(new FactoryMapValidationDiagnostic(
            context.TargetId,
            FactoryMapValidationSeverity.Warning,
            "dual-standards-chain",
            "No complete world-to-interior-to-world conversion chain was found in the reconstructed layout.",
            context.MapPath));
    }

    private static void AnalyzeRecipeSemantics(
        FactoryMapValidationContext context,
        List<FactoryMapValidationDiagnostic> diagnostics,
        FactoryStructure? focusStructure = null,
        bool includeSatisfiedInfo = false)
    {
        for (var i = 0; i < context.Structures.Count; i++)
        {
            var structure = context.Structures[i];
            if (focusStructure is not null && structure != focusStructure)
            {
                continue;
            }

            if (!TryGetActiveRecipe(structure, out var recipe) || recipe is null)
            {
                continue;
            }

            if (includeSatisfiedInfo)
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    context.TargetId,
                    FactoryMapValidationSeverity.Info,
                    "recipe",
                    $"Active recipe '{recipe.Id}' expects {FormatIngredientList(recipe.Inputs)} and outputs {FormatOutputList(recipe.Outputs)}.",
                    context.MapPath,
                    structure.Kind,
                    structure.Cell));
            }

            var missingInputs = new List<FactoryItemKind>();
            var satisfiedInputs = new List<string>();
            for (var inputIndex = 0; inputIndex < recipe.Inputs.Count; inputIndex++)
            {
                var ingredient = recipe.Inputs[inputIndex];
                if (TryFindUpstreamSupply(context, structure, ingredient.ItemKind, out var supplier))
                {
                    if (includeSatisfiedInfo && supplier is not null)
                    {
                        satisfiedInputs.Add($"{FactoryItemCatalog.GetDisplayName(ingredient.ItemKind)} <- {supplier.Kind} ({supplier.Cell.X}, {supplier.Cell.Y})");
                    }

                    continue;
                }

                if (!missingInputs.Contains(ingredient.ItemKind))
                {
                    missingInputs.Add(ingredient.ItemKind);
                }
            }

            if (missingInputs.Count > 0)
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    context.TargetId,
                    FactoryMapValidationSeverity.Warning,
                    "recipe-connectivity",
                    $"Configured recipe '{recipe.Id}' has no reachable supplier path for: {FormatItemKinds(missingInputs)}.",
                    context.MapPath,
                    structure.Kind,
                    structure.Cell));
            }
            else if (includeSatisfiedInfo && satisfiedInputs.Count > 0)
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    context.TargetId,
                    FactoryMapValidationSeverity.Info,
                    "recipe-connectivity",
                    $"Reachable recipe suppliers: {string.Join("; ", satisfiedInputs)}.",
                    context.MapPath,
                    structure.Kind,
                    structure.Cell));
            }

            var missingConsumers = new List<FactoryItemKind>();
            var satisfiedConsumers = new List<string>();
            for (var outputIndex = 0; outputIndex < recipe.Outputs.Count; outputIndex++)
            {
                var output = recipe.Outputs[outputIndex];
                if (TryFindDownstreamConsumer(context, structure, output.ItemKind, out var consumer))
                {
                    if (includeSatisfiedInfo && consumer is not null)
                    {
                        satisfiedConsumers.Add($"{FactoryItemCatalog.GetDisplayName(output.ItemKind)} -> {consumer.Kind} ({consumer.Cell.X}, {consumer.Cell.Y})");
                    }

                    continue;
                }

                if (!missingConsumers.Contains(output.ItemKind))
                {
                    missingConsumers.Add(output.ItemKind);
                }
            }

            if (missingConsumers.Count > 0)
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    context.TargetId,
                    FactoryMapValidationSeverity.Info,
                    "recipe-connectivity",
                    $"Configured recipe '{recipe.Id}' has no reachable downstream consumer path for: {FormatItemKinds(missingConsumers)}.",
                    context.MapPath,
                    structure.Kind,
                    structure.Cell));
            }
            else if (includeSatisfiedInfo && satisfiedConsumers.Count > 0)
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    context.TargetId,
                    FactoryMapValidationSeverity.Info,
                    "recipe-connectivity",
                    $"Reachable recipe consumers: {string.Join("; ", satisfiedConsumers)}.",
                    context.MapPath,
                    structure.Kind,
                    structure.Cell));
            }
        }
    }

    private static List<(FactoryStructure Structure, IFactoryPowerNode Node)> CollectPowerNodes(IReadOnlyList<FactoryStructure> structures)
    {
        var powerNodes = new List<(FactoryStructure Structure, IFactoryPowerNode Node)>();
        for (var i = 0; i < structures.Count; i++)
        {
            if (structures[i] is IFactoryPowerNode powerNode && powerNode.PowerConnectionRangeCells > 0)
            {
                powerNodes.Add((structures[i], powerNode));
            }
        }

        return powerNodes;
    }

    private static bool TryFindUpstreamSupply(
        FactoryMapValidationContext context,
        FactoryStructure structure,
        FactoryItemKind itemKind,
        out FactoryStructure? supplier)
    {
        supplier = null;
        var visited = new HashSet<ulong> { structure.GetInstanceId() };
        var queue = new Queue<FactoryStructure>(context.GetUpstreamNeighbors(structure));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current.GetInstanceId()))
            {
                continue;
            }

            if (CanStructureProvideItem(context, current, itemKind))
            {
                supplier = current;
                return true;
            }

            if (!CanRelayUpstream(current))
            {
                continue;
            }

            var upstream = context.GetUpstreamNeighbors(current);
            for (var i = 0; i < upstream.Count; i++)
            {
                queue.Enqueue(upstream[i]);
            }
        }

        return false;
    }

    private static bool TryFindDownstreamConsumer(
        FactoryMapValidationContext context,
        FactoryStructure structure,
        FactoryItemKind itemKind,
        out FactoryStructure? consumer)
    {
        consumer = null;
        var visited = new HashSet<ulong> { structure.GetInstanceId() };
        var queue = new Queue<FactoryStructure>(context.GetDownstreamNeighbors(structure));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current.GetInstanceId()))
            {
                continue;
            }

            if (CanStructureConsumeItem(current, itemKind))
            {
                consumer = current;
                return true;
            }

            if (!CanRelayDownstream(current))
            {
                continue;
            }

            var downstream = context.GetDownstreamNeighbors(current);
            for (var i = 0; i < downstream.Count; i++)
            {
                queue.Enqueue(downstream[i]);
            }
        }

        return false;
    }

    private static bool CanStructureProvideItem(FactoryMapValidationContext context, FactoryStructure structure, FactoryItemKind itemKind)
    {
        if (structure.Kind == BuildPrototypeKind.InputPort || structure.Kind == BuildPrototypeKind.MiningInputPort)
        {
            return true;
        }

        var entry = context.GetEntry(structure);
        if (entry is not null)
        {
            for (var i = 0; i < entry.SeedItems.Count; i++)
            {
                if (entry.SeedItems[i].ItemKind == itemKind)
                {
                    return true;
                }
            }
        }

        if (!TryGetActiveRecipe(structure, out var recipe) || recipe is null)
        {
            return structure.Kind == BuildPrototypeKind.CargoUnpacker
                || structure.Kind == BuildPrototypeKind.CargoPacker;
        }

        for (var i = 0; i < recipe.Outputs.Count; i++)
        {
            if (recipe.Outputs[i].ItemKind == itemKind)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanStructureConsumeItem(FactoryStructure structure, FactoryItemKind itemKind)
    {
        switch (structure.Kind)
        {
            case BuildPrototypeKind.Storage:
            case BuildPrototypeKind.LargeStorageDepot:
            case BuildPrototypeKind.Sink:
            case BuildPrototypeKind.OutputPort:
            case BuildPrototypeKind.CargoUnpacker:
            case BuildPrototypeKind.CargoPacker:
                return true;
            case BuildPrototypeKind.Generator:
                return FactoryItemCatalog.IsFuel(itemKind);
            case BuildPrototypeKind.GunTurret:
            case BuildPrototypeKind.HeavyGunTurret:
                return FactoryPresentation.IsAmmoItem(itemKind);
        }

        if (!TryGetActiveRecipe(structure, out var recipe) || recipe is null)
        {
            return false;
        }

        for (var i = 0; i < recipe.Inputs.Count; i++)
        {
            if (recipe.Inputs[i].ItemKind == itemKind)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanRelayUpstream(FactoryStructure structure)
    {
        return structure is FlowTransportStructure
            || structure.Kind == BuildPrototypeKind.Storage
            || structure.Kind == BuildPrototypeKind.LargeStorageDepot;
    }

    private static bool CanRelayDownstream(FactoryStructure structure)
    {
        return CanRelayUpstream(structure);
    }

    private static bool TryFindReachableDownstreamStructure(
        FactoryMapValidationContext context,
        FactoryStructure structure,
        Func<FactoryStructure, bool> matches,
        Func<FactoryStructure, bool> canRelay,
        out FactoryStructure? found)
    {
        found = null;
        var visited = new HashSet<ulong> { structure.GetInstanceId() };
        var queue = new Queue<FactoryStructure>(context.GetDownstreamNeighbors(structure));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current.GetInstanceId()))
            {
                continue;
            }

            if (matches(current))
            {
                found = current;
                return true;
            }

            if (!canRelay(current))
            {
                continue;
            }

            var downstream = context.GetDownstreamNeighbors(current);
            for (var i = 0; i < downstream.Count; i++)
            {
                queue.Enqueue(downstream[i]);
            }
        }

        return false;
    }

    private static bool TryFindReachableUpstreamStructure(
        FactoryMapValidationContext context,
        FactoryStructure structure,
        Func<FactoryStructure, bool> matches,
        Func<FactoryStructure, bool> canRelay,
        out FactoryStructure? found)
    {
        found = null;
        var visited = new HashSet<ulong> { structure.GetInstanceId() };
        var queue = new Queue<FactoryStructure>(context.GetUpstreamNeighbors(structure));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current.GetInstanceId()))
            {
                continue;
            }

            if (matches(current))
            {
                found = current;
                return true;
            }

            if (!canRelay(current))
            {
                continue;
            }

            var upstream = context.GetUpstreamNeighbors(current);
            for (var i = 0; i < upstream.Count; i++)
            {
                queue.Enqueue(upstream[i]);
            }
        }

        return false;
    }

    private static bool CanRelayCargoPath(FactoryStructure structure)
    {
        return structure is FlowTransportStructure
            || structure.Kind == BuildPrototypeKind.Storage
            || structure.Kind == BuildPrototypeKind.LargeStorageDepot
            || structure.Kind == BuildPrototypeKind.TransferBuffer
            || structure.Kind == BuildPrototypeKind.Smelter
            || structure.Kind == BuildPrototypeKind.Assembler
            || structure.Kind == BuildPrototypeKind.AmmoAssembler
            || structure.Kind == BuildPrototypeKind.CargoUnpacker
            || structure.Kind == BuildPrototypeKind.CargoPacker;
    }

    private static bool IsBoundaryIntake(FactoryStructure structure)
    {
        return structure.Kind == BuildPrototypeKind.InputPort
            || structure.Kind == BuildPrototypeKind.MiningInputPort;
    }

    private static bool IsInteriorUnpacker(FactoryStructure structure)
    {
        return structure.Kind == BuildPrototypeKind.CargoUnpacker;
    }

    private static bool IsInteriorPacker(FactoryStructure structure)
    {
        return structure.Kind == BuildPrototypeKind.CargoPacker;
    }

    private static bool IsInteriorProcessingNode(FactoryStructure structure)
    {
        return structure.Kind == BuildPrototypeKind.TransferBuffer
            || structure.Kind == BuildPrototypeKind.Smelter
            || structure.Kind == BuildPrototypeKind.Assembler
            || structure.Kind == BuildPrototypeKind.AmmoAssembler
            || structure.Kind == BuildPrototypeKind.DebugOreSource
            || structure.Kind == BuildPrototypeKind.DebugPartSource
            || structure.Kind == BuildPrototypeKind.DebugCombatSource
            || structure.Kind == BuildPrototypeKind.Storage
            || structure.Kind == BuildPrototypeKind.LargeStorageDepot;
    }

    private static bool TryGetActiveRecipe(FactoryStructure structure, out FactoryRecipeDefinition? recipe)
    {
        recipe = null;
        if (!RecipeCatalogByKind.TryGetValue(structure.Kind, out var catalog))
        {
            return false;
        }

        if (!structure.CaptureBlueprintConfiguration().TryGetValue("recipe_id", out var recipeId)
            || string.IsNullOrWhiteSpace(recipeId))
        {
            return false;
        }

        for (var i = 0; i < catalog.Count; i++)
        {
            if (string.Equals(catalog[i].Id, recipeId, StringComparison.Ordinal))
            {
                recipe = catalog[i];
                return true;
            }
        }

        return false;
    }

    private static string FormatItemKinds(IReadOnlyList<FactoryItemKind> itemKinds)
    {
        var parts = new string[itemKinds.Count];
        for (var i = 0; i < itemKinds.Count; i++)
        {
            parts[i] = FactoryItemCatalog.GetDisplayName(itemKinds[i]);
        }

        return string.Join(", ", parts);
    }

    private static string FormatIngredientList(IReadOnlyList<FactoryRecipeIngredientDefinition> items)
    {
        if (items.Count == 0)
        {
            return "no inputs";
        }

        var parts = new string[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            parts[i] = $"{FactoryItemCatalog.GetDisplayName(items[i].ItemKind)} x{items[i].Amount}";
        }

        return string.Join(", ", parts);
    }

    private static string FormatOutputList(IReadOnlyList<FactoryRecipeOutputDefinition> items)
    {
        if (items.Count == 0)
        {
            return "no outputs";
        }

        var parts = new string[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            parts[i] = $"{FactoryItemCatalog.GetDisplayName(items[i].ItemKind)} x{items[i].Amount}";
        }

        return string.Join(", ", parts);
    }

    private static string JoinCells(IEnumerable<Vector2I> cells)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var cell in cells)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append('(')
                .Append(cell.X)
                .Append(", ")
                .Append(cell.Y)
                .Append(')');
            first = false;
        }

        return first ? "<none>" : builder.ToString();
    }

    private static string JoinStructures(IReadOnlyList<FactoryStructure> structures)
    {
        var parts = new string[structures.Count];
        for (var i = 0; i < structures.Count; i++)
        {
            parts[i] = $"{structures[i].Kind} ({structures[i].Cell.X}, {structures[i].Cell.Y})";
        }

        return string.Join(", ", parts);
    }
}
