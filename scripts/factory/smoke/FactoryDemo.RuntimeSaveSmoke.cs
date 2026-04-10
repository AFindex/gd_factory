using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public partial class FactoryDemo
{
    private static bool HasRuntimeSaveSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--factory-runtime-save-smoke-test", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async void RunRuntimeSaveSmokeChecks()
    {
        try
        {
            var passed = await VerifyRuntimeSaveRoundTrip();
            if (!passed)
            {
                GetTree().Quit(1);
                return;
            }

            GD.Print("FACTORY_RUNTIME_SAVE_SMOKE_OK");
            GetTree().Quit();
        }
        catch (Exception ex)
        {
            GD.PushError($"FACTORY_RUNTIME_SAVE_SMOKE_FAILED {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private async Task<bool> VerifyRuntimeSaveRoundTrip()
    {
        if (_grid is null || _simulation is null || _playerController is null || _enemyRoot is null)
        {
            GD.PushError("FACTORY_RUNTIME_SAVE_SMOKE_FAILED missing runtime dependencies.");
            return false;
        }

        var slotId = "factory-runtime-save-smoke";
        if (!TryFindRuntimeSaveValidationCells(out var storageCell, out var beltACell, out var beltBCell, out var sinkCell, out var generatorCell))
        {
            GD.PushError("FACTORY_RUNTIME_SAVE_SMOKE_FAILED could not find a free validation area.");
            return false;
        }

        var storage = PlaceStructure(BuildPrototypeKind.Storage, storageCell, FacingDirection.East) as StorageStructure;
        var beltA = PlaceStructure(BuildPrototypeKind.Belt, beltACell, FacingDirection.East) as BeltStructure;
        var beltB = PlaceStructure(BuildPrototypeKind.Belt, beltBCell, FacingDirection.East) as BeltStructure;
        var sink = PlaceStructure(BuildPrototypeKind.Sink, sinkCell, FacingDirection.East) as SinkStructure;
        var generator = PlaceStructure(BuildPrototypeKind.Generator, generatorCell, FacingDirection.East) as GeneratorStructure;
        if (storage is null || beltA is null || beltB is null || sink is null || generator is null)
        {
            GD.PushError("FACTORY_RUNTIME_SAVE_SMOKE_FAILED could not place validation structures.");
            return false;
        }

        for (var index = 0; index < 4; index++)
        {
            storage.TryReceiveProvidedItem(
                _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
                storage.Cell + Vector2I.Left,
                _simulation);
        }

        for (var index = 0; index < 3; index++)
        {
            generator.TryAcceptItem(
                _simulation.CreateItem(BuildPrototypeKind.MiningDrill, FactoryItemKind.Coal),
                generator.Cell + Vector2I.Left,
                _simulation);
        }

        _playerController.GlobalPosition = _grid.CellToWorld(storageCell + new Vector2I(0, 2));
        HandlePlayerHotbarPressed(2);
        HandlePlayerHotbarPressed(2);

        await ToSignal(GetTree().CreateTimer(1.6f), SceneTreeTimer.SignalName.Timeout);
        await WaitForCondition(() => _simulation.ActiveEnemyCount > 0, 18.0f);

        beltA.TryReceiveProvidedItem(
            _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
            beltA.Cell + Vector2I.Left,
            _simulation);
        beltB.TryReceiveProvidedItem(
            _simulation.CreateItem(BuildPrototypeKind.Storage, FactoryItemKind.GenericCargo),
            beltB.Cell + Vector2I.Left,
            _simulation);
        await WaitForCondition(() => beltA.TransitItemCount + beltB.TransitItemCount > 0, 2.0f);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);

        var saveResult = SaveAndReadRuntimeSmokeDocument(slotId);
        var expectedDocument = saveResult.Document;
        var expectedWorldSite = FactoryDemoSmokeSupport.FindRequiredSite(expectedDocument, _grid.SiteId, FactoryMapKind.World);
        var expectedStorage = FactoryDemoSmokeSupport.FindRequiredStructure(expectedWorldSite, BuildPrototypeKind.Storage, storageCell);
        var expectedGenerator = FactoryDemoSmokeSupport.FindRequiredStructure(expectedWorldSite, BuildPrototypeKind.Generator, generatorCell);
        var expectedTransit = FactoryDemoSmokeSupport.FindFirstStructure(
            expectedWorldSite,
            snapshot => snapshot.Kind == BuildPrototypeKind.Belt
                && (snapshot.Cell.ToVector2I() == beltACell || snapshot.Cell.ToVector2I() == beltBCell)
                && snapshot.TransitItems.Count > 0);
        if (expectedTransit is null)
        {
            GD.PushError("FACTORY_RUNTIME_SAVE_SMOKE_FAILED no saved transport item was found for the validation belts.");
            return false;
        }

        var expectedPlayer = FactoryDemoSmokeSupport.SummarizePlayerSnapshot(expectedDocument.Player);
        var expectedCombat = FactoryDemoSmokeSupport.SummarizeCombatSnapshot(expectedDocument.CombatDirector);
        var expectedEnemies = FactoryDemoSmokeSupport.SummarizeEnemySnapshots(expectedDocument.Enemies);
        var expectedStorageSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(expectedStorage);
        var expectedGeneratorSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(expectedGenerator);
        var expectedTransitSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(expectedTransit);

        _playerController.GlobalPosition = _grid.CellToWorld(storageCell + new Vector2I(4, 4));
        PlaceStructure(BuildPrototypeKind.Belt, storageCell + new Vector2I(0, 3), FacingDirection.North);
        await ToSignal(GetTree().CreateTimer(2.2f), SceneTreeTimer.SignalName.Timeout);

        LoadRuntimeSnapshot(slotId);

        var restoredDocument = BuildRuntimeSnapshotDocument($"{slotId}-restored");
        var restoredWorldSite = FactoryDemoSmokeSupport.FindRequiredSite(restoredDocument, _grid.SiteId, FactoryMapKind.World);
        var restoredStorageSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(restoredWorldSite, BuildPrototypeKind.Storage, storageCell));
        var restoredGeneratorSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(restoredWorldSite, BuildPrototypeKind.Generator, generatorCell));
        var restoredTransitSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(restoredWorldSite, expectedTransit.Kind, expectedTransit.Cell.ToVector2I()));
        var restoredPlayer = FactoryDemoSmokeSupport.SummarizePlayerSnapshot(restoredDocument.Player);
        var restoredCombat = FactoryDemoSmokeSupport.SummarizeCombatSnapshot(restoredDocument.CombatDirector);
        var restoredEnemies = FactoryDemoSmokeSupport.SummarizeEnemySnapshots(restoredDocument.Enemies);

        var playerRestored = expectedPlayer == restoredPlayer;
        var storageRestored = expectedStorageSummary == restoredStorageSummary;
        var generatorRestored = expectedGeneratorSummary == restoredGeneratorSummary;
        var transitRestored = expectedTransitSummary == restoredTransitSummary;
        var combatRestored = expectedCombat == restoredCombat;
        var enemiesRestored = expectedEnemies == restoredEnemies;

        var stablePlayerPosition = _playerController.GlobalPosition;
        var stableStructureCount = _simulation.RegisteredStructureCount;
        var stableStorageSummary = restoredStorageSummary;
        var savePath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.BuildRuntimeSaveFilePath(slotId));

        var unsupportedDocument = FactoryRuntimeSavePersistence.Load(slotId);
        unsupportedDocument.Version = FactoryRuntimeSavePersistence.SupportedVersion + 1;
        File.WriteAllText(savePath, JsonSerializer.Serialize(unsupportedDocument, new JsonSerializerOptions { WriteIndented = true }));
        LoadRuntimeSnapshot(slotId);
        var unsupportedRejected =
            _previewMessage.Contains("进度读取失败", StringComparison.Ordinal)
            && _simulation.RegisteredStructureCount == stableStructureCount
            && _playerController.GlobalPosition.DistanceTo(stablePlayerPosition) < 0.01f
            && FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
                FactoryDemoSmokeSupport.FindRequiredStructure(
                    FactoryDemoSmokeSupport.FindRequiredSite(BuildRuntimeSnapshotDocument($"{slotId}-unsupported"), _grid.SiteId, FactoryMapKind.World),
                    BuildPrototypeKind.Storage,
                    storageCell)) == stableStorageSummary;

        File.WriteAllText(savePath, "{ invalid json ");
        LoadRuntimeSnapshot(slotId);
        var corruptRejected =
            _previewMessage.Contains("进度读取失败", StringComparison.Ordinal)
            && _simulation.RegisteredStructureCount == stableStructureCount
            && _playerController.GlobalPosition.DistanceTo(stablePlayerPosition) < 0.01f
            && FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
                FactoryDemoSmokeSupport.FindRequiredStructure(
                    FactoryDemoSmokeSupport.FindRequiredSite(BuildRuntimeSnapshotDocument($"{slotId}-corrupt"), _grid.SiteId, FactoryMapKind.World),
                    BuildPrototypeKind.Storage,
                    storageCell)) == stableStorageSummary;

        FactoryRuntimeSavePersistence.Save(saveResult.Document);

        if (!playerRestored
            || !storageRestored
            || !generatorRestored
            || !transitRestored
            || !combatRestored
            || !enemiesRestored
            || !unsupportedRejected
            || !corruptRejected)
        {
            GD.PushError(
                $"FACTORY_RUNTIME_SAVE_SMOKE_FAILED player={playerRestored} storage={storageRestored} generator={generatorRestored} transit={transitRestored} combat={combatRestored} enemies={enemiesRestored} unsupported={unsupportedRejected} corrupt={corruptRejected}");
            return false;
        }

        GD.Print(
            $"FACTORY_RUNTIME_SAVE_SMOKE player={playerRestored} storage={storageRestored} generator={generatorRestored} transit={transitRestored} combat={combatRestored} enemies={enemiesRestored} unsupported={unsupportedRejected} corrupt={corruptRejected}");
        return true;
    }

    private FactoryRuntimeSaveResult SaveAndReadRuntimeSmokeDocument(string slotId)
    {
        SaveRuntimeSnapshot(slotId);
        return new FactoryRuntimeSaveResult(
            FactoryRuntimeSavePersistence.Load(slotId),
            FactoryPersistencePaths.BuildRuntimeSaveFilePath(slotId));
    }

    private bool TryFindRuntimeSaveValidationCells(
        out Vector2I storageCell,
        out Vector2I beltACell,
        out Vector2I beltBCell,
        out Vector2I sinkCell,
        out Vector2I generatorCell)
    {
        storageCell = Vector2I.Zero;
        beltACell = Vector2I.Zero;
        beltBCell = Vector2I.Zero;
        sinkCell = Vector2I.Zero;
        generatorCell = Vector2I.Zero;

        if (_grid is null)
        {
            return false;
        }

        for (var y = FactoryConstants.GridMin + 4; y <= FactoryConstants.GridMax - 4; y++)
        {
            for (var x = FactoryConstants.GridMin + 4; x <= FactoryConstants.GridMax - 5; x++)
            {
                var candidateStorage = new Vector2I(x, y);
                var candidateBeltA = new Vector2I(x + 1, y);
                var candidateBeltB = new Vector2I(x + 2, y);
                var candidateSink = new Vector2I(x + 3, y);
                var candidateGenerator = new Vector2I(x, y + 2);
                if (!TryValidateWorldPlacement(BuildPrototypeKind.Storage, candidateStorage, FacingDirection.East, out _)
                    || !TryValidateWorldPlacement(BuildPrototypeKind.Belt, candidateBeltA, FacingDirection.East, out _)
                    || !TryValidateWorldPlacement(BuildPrototypeKind.Belt, candidateBeltB, FacingDirection.East, out _)
                    || !TryValidateWorldPlacement(BuildPrototypeKind.Sink, candidateSink, FacingDirection.East, out _)
                    || !TryValidateWorldPlacement(BuildPrototypeKind.Generator, candidateGenerator, FacingDirection.East, out _))
                {
                    continue;
                }

                storageCell = candidateStorage;
                beltACell = candidateBeltA;
                beltBCell = candidateBeltB;
                sinkCell = candidateSink;
                generatorCell = candidateGenerator;
                return true;
            }
        }

        return false;
    }

    private async Task WaitForCondition(Func<bool> predicate, float timeoutSeconds)
    {
        var remaining = timeoutSeconds;
        while (remaining > 0.0f)
        {
            if (predicate())
            {
                return;
            }

            await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
            remaining -= 0.1f;
        }
    }
}
