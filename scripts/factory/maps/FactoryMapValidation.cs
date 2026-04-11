using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public enum FactoryMapValidationTargetKind
{
    WorldMap,
    MobileFactoryBundle
}

public enum FactoryMapValidationSeverity
{
    Error,
    Warning,
    Info
}

public sealed class FactoryMapValidationDeploymentProbe
{
    public FactoryMapValidationDeploymentProbe(string id, string description, Vector2I anchorCell, FacingDirection facing, bool mustBeDeployable = true)
    {
        Id = id;
        Description = description;
        AnchorCell = anchorCell;
        Facing = facing;
        MustBeDeployable = mustBeDeployable;
    }

    public string Id { get; }
    public string Description { get; }
    public Vector2I AnchorCell { get; }
    public FacingDirection Facing { get; }
    public bool MustBeDeployable { get; }
}

public sealed class FactoryMapValidationTarget
{
    public FactoryMapValidationTarget(
        string id,
        string displayName,
        FactoryMapValidationTargetKind kind,
        string worldMapPath,
        string? interiorMapPath = null,
        Func<MobileFactoryProfile>? createProfile = null,
        IReadOnlyList<FactoryMapValidationDeploymentProbe>? deploymentProbes = null)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        WorldMapPath = worldMapPath;
        InteriorMapPath = interiorMapPath;
        CreateProfile = createProfile;
        DeploymentProbes = deploymentProbes ?? Array.Empty<FactoryMapValidationDeploymentProbe>();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public FactoryMapValidationTargetKind Kind { get; }
    public string WorldMapPath { get; }
    public string? InteriorMapPath { get; }
    public Func<MobileFactoryProfile>? CreateProfile { get; }
    public IReadOnlyList<FactoryMapValidationDeploymentProbe> DeploymentProbes { get; }
}

public sealed class FactoryMapValidationDiagnostic
{
    public FactoryMapValidationDiagnostic(
        string targetId,
        FactoryMapValidationSeverity severity,
        string category,
        string message,
        string? mapPath = null,
        BuildPrototypeKind? structureKind = null,
        Vector2I? cell = null,
        string? subjectId = null)
    {
        TargetId = targetId;
        Severity = severity;
        Category = category;
        Message = message;
        MapPath = mapPath;
        StructureKind = structureKind;
        Cell = cell;
        SubjectId = subjectId;
    }

    public string TargetId { get; }
    public FactoryMapValidationSeverity Severity { get; }
    public string Category { get; }
    public string Message { get; }
    public string? MapPath { get; }
    public BuildPrototypeKind? StructureKind { get; }
    public Vector2I? Cell { get; }
    public string? SubjectId { get; }

    public string FormatForConsole()
    {
        var locationParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(MapPath))
        {
            locationParts.Add(MapPath!);
        }

        if (StructureKind.HasValue)
        {
            locationParts.Add($"kind={StructureKind.Value}");
        }

        if (Cell.HasValue)
        {
            var locationCell = Cell.Value;
            locationParts.Add($"cell=({locationCell.X},{locationCell.Y})");
        }

        if (!string.IsNullOrWhiteSpace(SubjectId))
        {
            locationParts.Add($"subject={SubjectId}");
        }

        var location = locationParts.Count > 0
            ? $" [{string.Join(" | ", locationParts)}]"
            : string.Empty;
        return $"{Severity.ToString().ToUpperInvariant()} {Category}: {Message}{location}";
    }
}

public sealed class FactoryMapValidationTargetSummary
{
    public FactoryMapValidationTargetSummary(IReadOnlyList<FactoryMapValidationDiagnostic> diagnostics)
    {
        for (var i = 0; i < diagnostics.Count; i++)
        {
            switch (diagnostics[i].Severity)
            {
                case FactoryMapValidationSeverity.Error:
                    ErrorCount++;
                    break;
                case FactoryMapValidationSeverity.Warning:
                    WarningCount++;
                    break;
                default:
                    InfoCount++;
                    break;
            }
        }
    }

    public int ErrorCount { get; }
    public int WarningCount { get; }
    public int InfoCount { get; }
}

public sealed class FactoryMapValidationTargetResult
{
    public FactoryMapValidationTargetResult(
        FactoryMapValidationTarget target,
        IReadOnlyList<FactoryMapValidationDiagnostic> diagnostics)
    {
        Target = target;
        Diagnostics = diagnostics;
        Summary = new FactoryMapValidationTargetSummary(diagnostics);
    }

    public FactoryMapValidationTarget Target { get; }
    public IReadOnlyList<FactoryMapValidationDiagnostic> Diagnostics { get; }
    public FactoryMapValidationTargetSummary Summary { get; }
    public bool HasErrors => Summary.ErrorCount > 0;
}

public sealed class FactoryMapValidationReport
{
    public FactoryMapValidationReport(
        IReadOnlyList<FactoryMapValidationDiagnostic> globalDiagnostics,
        IReadOnlyList<FactoryMapValidationTargetResult> targetResults)
    {
        GlobalDiagnostics = globalDiagnostics;
        TargetResults = targetResults;

        for (var i = 0; i < globalDiagnostics.Count; i++)
        {
            Count(globalDiagnostics[i].Severity);
        }

        for (var resultIndex = 0; resultIndex < targetResults.Count; resultIndex++)
        {
            var result = targetResults[resultIndex];
            for (var diagnosticIndex = 0; diagnosticIndex < result.Diagnostics.Count; diagnosticIndex++)
            {
                Count(result.Diagnostics[diagnosticIndex].Severity);
            }
        }
    }

    public IReadOnlyList<FactoryMapValidationDiagnostic> GlobalDiagnostics { get; }
    public IReadOnlyList<FactoryMapValidationTargetResult> TargetResults { get; }
    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }
    public int InfoCount { get; private set; }
    public bool HasErrors => ErrorCount > 0;

    private void Count(FactoryMapValidationSeverity severity)
    {
        switch (severity)
        {
            case FactoryMapValidationSeverity.Error:
                ErrorCount++;
                break;
            case FactoryMapValidationSeverity.Warning:
                WarningCount++;
                break;
            default:
                InfoCount++;
                break;
        }
    }
}

public static class FactoryMapValidationCatalog
{
    public const string StaticSandboxWorldTargetId = "static-sandbox-world";
    public const string FocusedMobileBundleTargetId = "focused-mobile-bundle";
    public const string DualStandardsMobileBundleTargetId = "dual-standards-mobile-bundle";

    private static readonly FactoryMapValidationTarget[] Targets =
    {
        new(
            StaticSandboxWorldTargetId,
            "Static Sandbox World",
            FactoryMapValidationTargetKind.WorldMap,
            FactoryMapPaths.StaticSandboxWorld),
        new(
            FocusedMobileBundleTargetId,
            "Focused Mobile Bundle",
            FactoryMapValidationTargetKind.MobileFactoryBundle,
            FactoryMapPaths.FocusedMobileWorld,
            FactoryMapPaths.FocusedMobileInterior,
            MobileFactoryScenarioLibrary.CreateFocusedDemoProfile,
            new[]
            {
                new FactoryMapValidationDeploymentProbe("anchor-a", "Focused logistics anchor A", new Vector2I(-6, -3), FacingDirection.East),
                new FactoryMapValidationDeploymentProbe("anchor-b", "Focused logistics anchor B", new Vector2I(2, 3), FacingDirection.East)
            }),
        new(
            DualStandardsMobileBundleTargetId,
            "Dual Standards Mobile Bundle",
            FactoryMapValidationTargetKind.MobileFactoryBundle,
            FactoryMapPaths.DualStandardsMobileWorld,
            FactoryMapPaths.DualStandardsMobileInterior,
            MobileFactoryScenarioLibrary.CreateFocusedDemoProfile,
            new[]
            {
                new FactoryMapValidationDeploymentProbe("dual-anchor-b", "Dual-standards logistics anchor B", new Vector2I(2, 3), FacingDirection.East)
            })
    };

    public static IReadOnlyList<FactoryMapValidationTarget> GetTargets()
    {
        return Targets;
    }

    public static bool TryGetTarget(string id, out FactoryMapValidationTarget? target)
    {
        for (var i = 0; i < Targets.Length; i++)
        {
            if (string.Equals(Targets[i].Id, id, StringComparison.OrdinalIgnoreCase))
            {
                target = Targets[i];
                return true;
            }
        }

        target = null;
        return false;
    }
}

public static partial class FactoryMapValidationService
{
    public static FactoryMapValidationReport ValidateAllTargets()
    {
        return ValidateTargets(FactoryMapValidationCatalog.GetTargets());
    }

    public static FactoryMapValidationReport ValidateTarget(string targetId)
    {
        if (!FactoryMapValidationCatalog.TryGetTarget(targetId, out var target) || target is null)
        {
            throw new InvalidOperationException(
                $"Unknown factory map validation target '{targetId}'. Available targets: {string.Join(", ", GetAvailableTargetIds())}.");
        }

        return ValidateTargets(new[] { target });
    }

    public static string[] GetAvailableTargetIds()
    {
        var targets = FactoryMapValidationCatalog.GetTargets();
        var ids = new string[targets.Count];
        for (var i = 0; i < targets.Count; i++)
        {
            ids[i] = targets[i].Id;
        }

        return ids;
    }

    public static FactoryMapValidationReport ValidateTargets(IReadOnlyList<FactoryMapValidationTarget> targets)
    {
        var globalDiagnostics = new List<FactoryMapValidationDiagnostic>();
        AddMalformedRejectionDiagnostic(globalDiagnostics);

        var results = new List<FactoryMapValidationTargetResult>(targets.Count);
        for (var i = 0; i < targets.Count; i++)
        {
            results.Add(ValidateTarget(targets[i]));
        }

        return new FactoryMapValidationReport(globalDiagnostics, results);
    }

    public static void PrintReport(FactoryMapValidationReport report)
    {
        GD.Print($"FACTORY_MAP_VALIDATION summary targets={report.TargetResults.Count} errors={report.ErrorCount} warnings={report.WarningCount} info={report.InfoCount}");

        for (var i = 0; i < report.GlobalDiagnostics.Count; i++)
        {
            PrintDiagnostic(report.GlobalDiagnostics[i]);
        }

        for (var resultIndex = 0; resultIndex < report.TargetResults.Count; resultIndex++)
        {
            var result = report.TargetResults[resultIndex];
            GD.Print(
                $"FACTORY_MAP_VALIDATION_TARGET id={result.Target.Id} kind={result.Target.Kind} errors={result.Summary.ErrorCount} warnings={result.Summary.WarningCount} info={result.Summary.InfoCount}");
            for (var diagnosticIndex = 0; diagnosticIndex < result.Diagnostics.Count; diagnosticIndex++)
            {
                PrintDiagnostic(result.Diagnostics[diagnosticIndex]);
            }
        }
    }

    private static FactoryMapValidationTargetResult ValidateTarget(FactoryMapValidationTarget target)
    {
        var diagnostics = new List<FactoryMapValidationDiagnostic>();
        AddRoundTripDiagnostic(target.Id, target.WorldMapPath, diagnostics);

        switch (target.Kind)
        {
            case FactoryMapValidationTargetKind.MobileFactoryBundle:
                if (!string.IsNullOrWhiteSpace(target.InteriorMapPath))
                {
                    AddRoundTripDiagnostic(target.Id, target.InteriorMapPath!, diagnostics);
                }

                ValidateMobileBundleTarget(target, diagnostics);
                break;
            default:
                ValidateWorldTarget(target, diagnostics);
                break;
        }

        return new FactoryMapValidationTargetResult(target, diagnostics);
    }

    private static void ValidateWorldTarget(
        FactoryMapValidationTarget target,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        Node3D? structureRoot = null;
        SimulationController? simulation = null;
        try
        {
            var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(target.WorldMapPath));
            var worldGrid = new GridManager(document.MinCell, document.MaxCell, FactoryConstants.CellSize);
            simulation = new SimulationController();
            simulation.Configure(worldGrid);
            structureRoot = new Node3D { Name = $"ValidationRoot_{target.Id}" };
            var loadResult = FactoryMapRuntimeLoader.LoadWorldMap(target.WorldMapPath, worldGrid, structureRoot, simulation);
            simulation.RebuildTopology();

            AnalyzeValidationContext(target.Id, target.WorldMapPath, loadResult.Document, loadResult.LoadedStructures, simulation, diagnostics);
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                target.Id,
                FactoryMapValidationSeverity.Error,
                "world-replay",
                ex.Message,
                target.WorldMapPath));
        }
        finally
        {
            structureRoot?.Free();
            simulation?.Free();
        }
    }

    private static void ValidateMobileBundleTarget(
        FactoryMapValidationTarget target,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        GridManager? worldGrid = null;
        SimulationController? simulation = null;
        Node3D? structureRoot = null;

        try
        {
            var worldDocument = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(target.WorldMapPath));
            worldGrid = new GridManager(worldDocument.MinCell, worldDocument.MaxCell, FactoryConstants.CellSize);
            simulation = new SimulationController();
            simulation.Configure(worldGrid);
            structureRoot = new Node3D { Name = $"ValidationRoot_{target.Id}" };
            var worldLoadResult = FactoryMapRuntimeLoader.LoadWorldMap(target.WorldMapPath, worldGrid, structureRoot, simulation);
            simulation.RebuildTopology();

            AnalyzeValidationContext(target.Id, target.WorldMapPath, worldLoadResult.Document, worldLoadResult.LoadedStructures, simulation, diagnostics);
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                target.Id,
                FactoryMapValidationSeverity.Error,
                "world-replay",
                ex.Message,
                target.WorldMapPath));
        }

        if (string.IsNullOrWhiteSpace(target.InteriorMapPath) || target.CreateProfile is null)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                target.Id,
                FactoryMapValidationSeverity.Error,
                "mobile-bundle",
                "Mobile-factory bundle target is missing an interior map path or profile factory.",
                target.InteriorMapPath));
            return;
        }

        var profile = target.CreateProfile();

        try
        {
            var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(target.InteriorMapPath));
            FactoryMapRuntimeLoader.ValidateInteriorDocumentAgainstProfile(document, profile, target.InteriorMapPath);

            simulation ??= new SimulationController();
            structureRoot ??= new Node3D { Name = $"ValidationRoot_{target.Id}_Interior" };
            var preset = FactoryMapRuntimeLoader.LoadInteriorPreset(
                target.InteriorMapPath,
                $"{target.Id}-validation",
                $"{target.DisplayName} Validation",
                "Headless validation preset.",
                "Headless validation preset.",
                profile);
            var factory = new MobileFactoryInstance(
                $"validation-{target.Id}",
                structureRoot,
                simulation,
                profile,
                preset);

            FactoryMapRuntimeLoader.ApplyInteriorRuntimeState(target.InteriorMapPath, factory, simulation);
            simulation.RebuildTopology();

            var interiorStructures = CollectInteriorStructures(factory);
            AnalyzeValidationContext(target.Id, target.InteriorMapPath, document, interiorStructures, simulation, diagnostics);

            if (worldGrid is not null)
            {
                AnalyzeDeploymentProbes(target, worldGrid, factory, diagnostics);
            }
            else
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    target.Id,
                    FactoryMapValidationSeverity.Warning,
                    "mobile-profile",
                    "Skipped deployment-probe validation because the world map did not replay successfully.",
                    target.InteriorMapPath));
            }
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                target.Id,
                FactoryMapValidationSeverity.Error,
                "interior-replay",
                ex.Message,
                target.InteriorMapPath));
        }
        finally
        {
            structureRoot?.Free();
            simulation?.Free();
        }
    }

    private static void AnalyzeDeploymentProbes(
        FactoryMapValidationTarget target,
        GridManager worldGrid,
        MobileFactoryInstance factory,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        for (var i = 0; i < target.DeploymentProbes.Count; i++)
        {
            var probe = target.DeploymentProbes[i];
            var evaluation = factory.EvaluateDeployment(worldGrid, probe.AnchorCell, probe.Facing);
            if (probe.MustBeDeployable && !evaluation.CanDeploy)
            {
                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    target.Id,
                    FactoryMapValidationSeverity.Error,
                    "mobile-profile",
                    $"Deployment probe '{probe.Description}' is blocked: {evaluation.Reason}",
                    target.WorldMapPath,
                    cell: probe.AnchorCell,
                    subjectId: probe.Id));
                continue;
            }

            diagnostics.Add(new FactoryMapValidationDiagnostic(
                target.Id,
                evaluation.HasWarnings ? FactoryMapValidationSeverity.Warning : FactoryMapValidationSeverity.Info,
                "mobile-profile",
                evaluation.HasWarnings
                    ? $"Deployment probe '{probe.Description}' is deployable with warnings: {evaluation.Reason}"
                    : $"Deployment probe '{probe.Description}' validated successfully.",
                target.WorldMapPath,
                cell: probe.AnchorCell,
                subjectId: probe.Id));

            for (var attachmentIndex = 0; attachmentIndex < evaluation.AttachmentEvaluations.Count; attachmentIndex++)
            {
                var attachmentEvaluation = evaluation.AttachmentEvaluations[attachmentIndex];
                if (attachmentEvaluation.State == MobileFactoryAttachmentDeployState.Connected)
                {
                    continue;
                }

                diagnostics.Add(new FactoryMapValidationDiagnostic(
                    target.Id,
                    attachmentEvaluation.State == MobileFactoryAttachmentDeployState.Blocked
                        ? FactoryMapValidationSeverity.Warning
                        : FactoryMapValidationSeverity.Info,
                    "attachment-connectivity",
                    $"{attachmentEvaluation.Attachment.AttachmentDefinition.DisplayName} probe state is {attachmentEvaluation.State}: {attachmentEvaluation.Reason}",
                    target.WorldMapPath,
                    attachmentEvaluation.Attachment.Kind,
                    attachmentEvaluation.Attachment.Cell,
                    probe.Id));
            }
        }
    }

    private static List<FactoryStructure> CollectInteriorStructures(MobileFactoryInstance factory)
    {
        var structures = new List<FactoryStructure>();
        var seen = new HashSet<ulong>();
        for (var y = factory.InteriorMinCell.Y; y <= factory.InteriorMaxCell.Y; y++)
        {
            for (var x = factory.InteriorMinCell.X; x <= factory.InteriorMaxCell.X; x++)
            {
                if (!factory.TryGetInteriorStructure(new Vector2I(x, y), out var structure) || structure is null)
                {
                    continue;
                }

                if (seen.Add(structure.GetInstanceId()))
                {
                    structures.Add(structure);
                }
            }
        }

        return structures;
    }

    private static void AnalyzeConnectivity(
        string targetId,
        string mapPath,
        IReadOnlyList<FactoryStructure> structures,
        SimulationController simulation,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        var powerNodes = new List<(FactoryStructure Structure, IFactoryPowerNode Node)>();
        for (var i = 0; i < structures.Count; i++)
        {
            if (structures[i] is IFactoryPowerNode powerNode && powerNode.PowerConnectionRangeCells > 0)
            {
                powerNodes.Add((structures[i], powerNode));
            }
        }

        for (var i = 0; i < structures.Count; i++)
        {
            var structure = structures[i];
            AnalyzeLogisticsConnectivity(targetId, mapPath, structure, diagnostics);
            AnalyzePowerConnectivity(targetId, mapPath, structure, powerNodes, simulation, diagnostics);
        }
    }

    private static void AnalyzeLogisticsConnectivity(
        string targetId,
        string mapPath,
        FactoryStructure structure,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        if (structure is not IFactoryItemProvider && structure is not IFactoryItemReceiver)
        {
            return;
        }

        var inputCells = FactoryMapValidationTopologyHelper.GetInputCells(structure);
        var outputCells = FactoryMapValidationTopologyHelper.GetOutputCells(structure);
        if (inputCells.Count == 0 && outputCells.Count == 0)
        {
            return;
        }

        var connectedInputCount = CountConnectedInputCells(structure, inputCells);
        var connectedOutputCount = CountConnectedOutputCells(structure, outputCells);

        if (structure is FlowTransportStructure && connectedInputCount == 0 && connectedOutputCount == 0)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                targetId,
                FactoryMapValidationSeverity.Warning,
                "connectivity",
                "Transport structure is isolated from both upstream and downstream neighbors.",
                mapPath,
                structure.Kind,
                structure.Cell));
            return;
        }

        if (inputCells.Count > 0
            && connectedInputCount == 0
            && structure.Kind != BuildPrototypeKind.MiningDrill
            && structure.Kind != BuildPrototypeKind.OutputPort)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                targetId,
                FactoryMapValidationSeverity.Info,
                "connectivity",
                "Structure currently has no connected upstream input source.",
                mapPath,
                structure.Kind,
                structure.Cell));
        }

        if (outputCells.Count > 0
            && connectedOutputCount == 0
            && structure.Kind != BuildPrototypeKind.Sink
            && structure.Kind != BuildPrototypeKind.InputPort
            && structure.Kind != BuildPrototypeKind.MiningInputPort)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                targetId,
                FactoryMapValidationSeverity.Info,
                "connectivity",
                "Structure currently has no connected downstream target.",
                mapPath,
                structure.Kind,
                structure.Cell));
        }
    }

    private static void AnalyzePowerConnectivity(
        string targetId,
        string mapPath,
        FactoryStructure structure,
        IReadOnlyList<(FactoryStructure Structure, IFactoryPowerNode Node)> powerNodes,
        SimulationController simulation,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        if (structure is not IFactoryPowerConsumer consumer || !consumer.WantsPower(simulation))
        {
            return;
        }

        var bestNodeIndex = -1;
        var bestDistance = float.MaxValue;
        var consumerCell = new Vector2(structure.Cell.X, structure.Cell.Y);
        for (var i = 0; i < powerNodes.Count; i++)
        {
            if (powerNodes[i].Structure.Site != structure.Site)
            {
                continue;
            }

            var nodeCell = new Vector2(powerNodes[i].Structure.Cell.X, powerNodes[i].Structure.Cell.Y);
            var distance = consumerCell.DistanceTo(nodeCell);
            if (distance > powerNodes[i].Node.PowerConnectionRangeCells || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestNodeIndex = i;
        }

        if (bestNodeIndex < 0)
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                targetId,
                FactoryMapValidationSeverity.Warning,
                "power",
                "Powered structure has no reachable power node in range.",
                mapPath,
                structure.Kind,
                structure.Cell));
            return;
        }

        if (!PowerNodeNetworkHasProducer(bestNodeIndex, powerNodes))
        {
            diagnostics.Add(new FactoryMapValidationDiagnostic(
                targetId,
                FactoryMapValidationSeverity.Warning,
                "power",
                "Powered structure is near power nodes, but that local power network has no producer.",
                mapPath,
                structure.Kind,
                structure.Cell));
        }
    }

    private static int CountConnectedInputCells(FactoryStructure structure, IReadOnlyList<Vector2I> inputCells)
    {
        var connected = 0;
        for (var i = 0; i < inputCells.Count; i++)
        {
            if (FactoryMapValidationTopologyHelper.TryGetInputProvider(structure, inputCells[i], out _))
            {
                connected++;
            }
        }

        return connected;
    }

    private static int CountConnectedOutputCells(FactoryStructure structure, IReadOnlyList<Vector2I> outputCells)
    {
        var connected = 0;
        for (var i = 0; i < outputCells.Count; i++)
        {
            if (FactoryMapValidationTopologyHelper.TryGetOutputReceiver(structure, outputCells[i], out _))
            {
                connected++;
            }
        }

        return connected;
    }
    private static bool PowerNodeNetworkHasProducer(
        int startIndex,
        IReadOnlyList<(FactoryStructure Structure, IFactoryPowerNode Node)> powerNodes)
    {
        var visited = new bool[powerNodes.Count];
        var queue = new Queue<int>();
        queue.Enqueue(startIndex);
        visited[startIndex] = true;

        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            var current = powerNodes[currentIndex];
            if (current.Node is IFactoryPowerProducer)
            {
                return true;
            }

            for (var candidateIndex = 0; candidateIndex < powerNodes.Count; candidateIndex++)
            {
                if (visited[candidateIndex]
                    || powerNodes[candidateIndex].Structure.Site != current.Structure.Site
                    || !ArePowerNodesConnected(current, powerNodes[candidateIndex]))
                {
                    continue;
                }

                visited[candidateIndex] = true;
                queue.Enqueue(candidateIndex);
            }
        }

        return false;
    }

    private static bool ArePowerNodesConnected(
        (FactoryStructure Structure, IFactoryPowerNode Node) a,
        (FactoryStructure Structure, IFactoryPowerNode Node) b)
    {
        var maxDistance = a.Node.PowerConnectionRangeCells + b.Node.PowerConnectionRangeCells;
        var aCell = new Vector2(a.Structure.Cell.X, a.Structure.Cell.Y);
        var bCell = new Vector2(b.Structure.Cell.X, b.Structure.Cell.Y);
        return aCell.DistanceTo(bCell) <= maxDistance;
    }

    private static void AddMalformedRejectionDiagnostic(List<FactoryMapValidationDiagnostic> diagnostics)
    {
        var passed = FactoryMapRuntimeLoader.VerifyMalformedMapRejected();
        diagnostics.Add(new FactoryMapValidationDiagnostic(
            "runtime-self-check",
            passed ? FactoryMapValidationSeverity.Info : FactoryMapValidationSeverity.Error,
            "serializer",
            passed
                ? "Malformed map rejection self-check passed."
                : "Malformed map rejection self-check failed."));
    }

    private static void AddRoundTripDiagnostic(
        string targetId,
        string mapPath,
        List<FactoryMapValidationDiagnostic> diagnostics)
    {
        var passed = FactoryMapRuntimeLoader.VerifyRoundTrip(mapPath);
        diagnostics.Add(new FactoryMapValidationDiagnostic(
            targetId,
            passed ? FactoryMapValidationSeverity.Info : FactoryMapValidationSeverity.Error,
            "serializer",
            passed
                ? "Round-trip serialization check passed."
                : "Round-trip serialization check failed.",
            mapPath));
    }

    private static void PrintDiagnostic(FactoryMapValidationDiagnostic diagnostic)
    {
        var line = diagnostic.FormatForConsole();
        if (diagnostic.Severity == FactoryMapValidationSeverity.Error)
        {
            GD.PrintErr(line);
            return;
        }

        GD.Print(line);
    }
}
