using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public partial class MobileFactoryDemo
{
    private static bool HasRuntimeSaveSmokeTestFlag()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--mobile-factory-runtime-save-smoke-test", StringComparison.Ordinal))
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

            GD.Print("MOBILE_FACTORY_RUNTIME_SAVE_SMOKE_OK");
            GetTree().Quit();
        }
        catch (Exception ex)
        {
            GD.PushError($"MOBILE_FACTORY_RUNTIME_SAVE_SMOKE_FAILED {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private async Task<bool> VerifyRuntimeSaveRoundTrip()
    {
        if (UseLargeTestScenario
            || _grid is null
            || _simulation is null
            || _mobileFactory is null
            || _playerController is null)
        {
            GD.PushError("MOBILE_FACTORY_RUNTIME_SAVE_SMOKE_FAILED missing focused mobile runtime dependencies.");
            return false;
        }

        PrimeMobileFactoryShowcase(_mobileFactory);
        if (_mobileFactory.State != MobileFactoryLifecycleState.Deployed)
        {
            if (!_mobileFactory.TryDeploy(_grid, AnchorA, FacingDirection.East))
            {
                GD.PushError("MOBILE_FACTORY_RUNTIME_SAVE_SMOKE_FAILED could not deploy the focused factory.");
                return false;
            }

            await ToSignal(GetTree().CreateTimer(0.8f), SceneTreeTimer.SignalName.Timeout);
        }

        PrimeFocusedOutputPorts(_mobileFactory);
        _playerController.GlobalPosition = _mobileFactory.WorldFocusPoint + new Vector3(1.4f, 0.0f, 1.2f);
        HandlePlayerHotbarPressed(3);
        HandlePlayerHotbarPressed(3);

        await WaitForCondition(
            () => _mobileFactory.HasConnectedAttachment(BuildPrototypeKind.OutputPort)
                && (_mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.OutputPort) > 0
                    || _mobileFactory.CountAttachmentTransitItems(BuildPrototypeKind.InputPort) > 0)
                && _simulation.ActiveEnemyCount > 0,
            18.0f);

        var slotId = "mobile-runtime-save-smoke";
        var savePath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.BuildRuntimeSaveFilePath(slotId));
        FactoryRuntimeSavePersistence.Save(BuildRuntimeSnapshotDocument(slotId));
        var expectedDocument = FactoryRuntimeSavePersistence.Load(slotId);
        var expectedWorldSite = FactoryDemoSmokeSupport.FindRequiredSite(expectedDocument, _grid.SiteId, FactoryMapKind.World);
        var expectedInteriorSite = FactoryDemoSmokeSupport.FindRequiredSite(expectedDocument, _mobileFactory.InteriorSite.SiteId, FactoryMapKind.Interior);
        var expectedPlayer = FactoryDemoSmokeSupport.SummarizePlayerSnapshot(expectedDocument.Player);
        var expectedCombat = FactoryDemoSmokeSupport.SummarizeCombatSnapshot(expectedDocument.CombatDirector);
        var expectedEnemies = FactoryDemoSmokeSupport.SummarizeEnemySnapshots(expectedDocument.Enemies);
        var expectedMobile = FactoryDemoSmokeSupport.SummarizeMobileFactorySnapshot(expectedDocument.MobileFactory);
        var expectedInteriorStorage = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(expectedInteriorSite, BuildPrototypeKind.Storage, FocusedIronBufferCell));
        var expectedInteriorAssembler = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(expectedInteriorSite, BuildPrototypeKind.Assembler, FocusedAssemblerCell));
        var expectedAttachment = FactoryDemoSmokeSupport.FindFirstStructure(
            expectedInteriorSite,
            snapshot => (snapshot.Kind == BuildPrototypeKind.OutputPort || snapshot.Kind == BuildPrototypeKind.InputPort)
                && snapshot.TransitItems.Count > 0);
        if (expectedAttachment is null)
        {
            GD.PushError("MOBILE_FACTORY_RUNTIME_SAVE_SMOKE_FAILED no attachment transit snapshot was captured.");
            return false;
        }

        var expectedAttachmentSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(expectedAttachment);
        var expectedWorldSinkSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(expectedWorldSite, BuildPrototypeKind.Sink, _sinkA?.Cell ?? AnchorA));
        var expectedWorldPathsCaptured =
            !string.IsNullOrWhiteSpace(expectedWorldSite.LoadedMapPath)
            && !string.IsNullOrWhiteSpace(expectedWorldSite.ProjectMapPath)
            && !string.IsNullOrWhiteSpace(expectedWorldSite.RuntimeMapPath);
        var expectedInteriorPathsCaptured =
            !string.IsNullOrWhiteSpace(expectedInteriorSite.LoadedMapPath)
            && !string.IsNullOrWhiteSpace(expectedInteriorSite.ProjectMapPath)
            && !string.IsNullOrWhiteSpace(expectedInteriorSite.RuntimeMapPath);
        var indexRecord = FactoryRuntimeSavePersistence.LoadIndex().Slots.Find(slot => string.Equals(slot.SlotId, slotId, StringComparison.OrdinalIgnoreCase));
        var indexPathsCaptured = indexRecord is not null
            && indexRecord.Sites.Count == 2
            && indexRecord.Sites.TrueForAll(site =>
                !string.IsNullOrWhiteSpace(site.ProjectMapPath)
                && !string.IsNullOrWhiteSpace(site.RuntimeMapPath));

        _playerController.GlobalPosition = _mobileFactory.WorldFocusPoint + new Vector3(-2.4f, 0.0f, -1.8f);
        _mobileFactory.ReturnToTransitMode();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 2.0f);
        await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);
        _combatDirector?.ClearLanes();

        LoadRuntimeSnapshot(slotId);
        if (_worldPreviewMessage.Contains("失败", StringComparison.Ordinal)
            || _interiorPreviewMessage.Contains("失败", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Focused mobile runtime load failed: world='{_worldPreviewMessage}' interior='{_interiorPreviewMessage}'.");
        }

        var restoredDocument = BuildRuntimeSnapshotDocument($"{slotId}-restored");
        var restoredWorldSite = FactoryDemoSmokeSupport.FindRequiredSite(restoredDocument, _grid.SiteId, FactoryMapKind.World);
        var restoredInteriorSite = FactoryDemoSmokeSupport.FindRequiredSite(restoredDocument, _mobileFactory.InteriorSite.SiteId, FactoryMapKind.Interior);
        var restoredPlayer = FactoryDemoSmokeSupport.SummarizePlayerSnapshot(restoredDocument.Player);
        var restoredCombat = FactoryDemoSmokeSupport.SummarizeCombatSnapshot(restoredDocument.CombatDirector);
        var restoredEnemies = FactoryDemoSmokeSupport.SummarizeEnemySnapshots(restoredDocument.Enemies);
        var restoredMobile = FactoryDemoSmokeSupport.SummarizeMobileFactorySnapshot(restoredDocument.MobileFactory);
        var restoredInteriorStorage = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(restoredInteriorSite, BuildPrototypeKind.Storage, FocusedIronBufferCell));
        var restoredInteriorAssembler = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(restoredInteriorSite, BuildPrototypeKind.Assembler, FocusedAssemblerCell));
        var restoredAttachmentSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(
                restoredInteriorSite,
                expectedAttachment.Kind,
                expectedAttachment.Cell.ToVector2I()));
        var restoredWorldSinkSummary = FactoryDemoSmokeSupport.SummarizeStructureSnapshot(
            FactoryDemoSmokeSupport.FindRequiredStructure(restoredWorldSite, BuildPrototypeKind.Sink, _sinkA?.Cell ?? AnchorA));

        var playerRestored = expectedPlayer == restoredPlayer;
        var combatRestored = expectedCombat == restoredCombat;
        var enemiesRestored = expectedEnemies == restoredEnemies;
        var mobileRestored = expectedMobile == restoredMobile;
        var interiorStorageRestored = expectedInteriorStorage == restoredInteriorStorage;
        var interiorAssemblerRestored = expectedInteriorAssembler == restoredInteriorAssembler;
        var attachmentTransitRestored = expectedAttachmentSummary == restoredAttachmentSummary;
        var worldRestored = expectedWorldSinkSummary == restoredWorldSinkSummary;

        var transitSlotId = $"{slotId}-transit";
        _mobileFactory.ReturnToTransitMode();
        await WaitForCondition(() => _mobileFactory.State == MobileFactoryLifecycleState.InTransit, 2.0f);
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        _mobileFactory.ApplyTransitInput(_grid, 0.45f, 1.0f, 0.41);
        var expectedTransitDocument = BuildRuntimeSnapshotDocument(transitSlotId);
        FactoryRuntimeSavePersistence.Save(expectedTransitDocument);
        expectedTransitDocument = FactoryRuntimeSavePersistence.Load(transitSlotId);
        var expectedTransitMobile = FactoryDemoSmokeSupport.SummarizeMobileFactorySnapshot(expectedTransitDocument.MobileFactory);
        var transitHeadingIsPrecise = expectedTransitDocument.MobileFactory is not null
            && expectedTransitDocument.MobileFactory.HasHullHeadingRadians
            && Mathf.Abs(
                FactoryDirection.ToYRotationRadians(expectedTransitDocument.MobileFactory.TransitFacing)
                - expectedTransitDocument.MobileFactory.HullHeadingRadians) > 0.05f;

        _mobileFactory.ApplyTransitInput(_grid, -0.25f, -1.0f, 0.36);
        _combatDirector?.ClearLanes();
        LoadRuntimeSnapshot(transitSlotId);
        if (_worldPreviewMessage.Contains("失败", StringComparison.Ordinal)
            || _interiorPreviewMessage.Contains("失败", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Focused mobile transit runtime load failed: world='{_worldPreviewMessage}' interior='{_interiorPreviewMessage}'.");
        }

        var restoredTransitDocument = BuildRuntimeSnapshotDocument($"{transitSlotId}-restored");
        var restoredTransitMobile = FactoryDemoSmokeSupport.SummarizeMobileFactorySnapshot(restoredTransitDocument.MobileFactory);
        var transitHeadingRestored = expectedTransitMobile == restoredTransitMobile;

        var stablePlayerPosition = _playerController.GlobalPosition;
        var stableMobileSummary = FactoryDemoSmokeSupport.SummarizeMobileFactorySnapshot(_mobileFactory.CaptureRuntimeSnapshot());
        var stableStructureCount = _simulation.RegisteredStructureCount;

        var unsupportedDocument = FactoryRuntimeSavePersistence.Load(slotId);
        unsupportedDocument.Version = FactoryRuntimeSavePersistence.SupportedVersion + 1;
        File.WriteAllText(savePath, JsonSerializer.Serialize(unsupportedDocument, new JsonSerializerOptions { WriteIndented = true }));
        LoadRuntimeSnapshot(slotId);
        var unsupportedRejected =
            _worldPreviewMessage.Contains("失败", StringComparison.Ordinal)
            && _simulation.RegisteredStructureCount == stableStructureCount
            && _playerController.GlobalPosition.DistanceTo(stablePlayerPosition) < 0.01f
            && FactoryDemoSmokeSupport.SummarizeMobileFactorySnapshot(_mobileFactory.CaptureRuntimeSnapshot()) == stableMobileSummary;

        File.WriteAllText(savePath, "{ invalid json ");
        LoadRuntimeSnapshot(slotId);
        var corruptRejected =
            _worldPreviewMessage.Contains("失败", StringComparison.Ordinal)
            && _simulation.RegisteredStructureCount == stableStructureCount
            && _playerController.GlobalPosition.DistanceTo(stablePlayerPosition) < 0.01f
            && FactoryDemoSmokeSupport.SummarizeMobileFactorySnapshot(_mobileFactory.CaptureRuntimeSnapshot()) == stableMobileSummary;

        FactoryRuntimeSavePersistence.Save(expectedDocument);

        if (!playerRestored
            || !combatRestored
            || !enemiesRestored
            || !mobileRestored
            || !interiorStorageRestored
            || !interiorAssemblerRestored
            || !attachmentTransitRestored
            || !worldRestored
            || !transitHeadingIsPrecise
            || !transitHeadingRestored
            || !expectedWorldPathsCaptured
            || !expectedInteriorPathsCaptured
            || !indexPathsCaptured
            || !unsupportedRejected
            || !corruptRejected)
        {
            if (!playerRestored)
            {
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_PLAYER expected={expectedPlayer}");
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_PLAYER restored={restoredPlayer}");
            }

            if (!combatRestored)
            {
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_COMBAT expected={expectedCombat}");
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_COMBAT restored={restoredCombat}");
            }

            if (!enemiesRestored)
            {
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_ENEMIES expected={expectedEnemies}");
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_ENEMIES restored={restoredEnemies}");
            }

            if (!mobileRestored)
            {
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_MOBILE expected={expectedMobile}");
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_MOBILE restored={restoredMobile}");
            }

            if (!worldRestored)
            {
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_WORLD expected={expectedWorldSinkSummary}");
                GD.Print($"MOBILE_FACTORY_RUNTIME_SAVE_WORLD restored={restoredWorldSinkSummary}");
            }

            GD.PushError(
                $"MOBILE_FACTORY_RUNTIME_SAVE_SMOKE_FAILED player={playerRestored} combat={combatRestored} enemies={enemiesRestored} mobile={mobileRestored} interiorStorage={interiorStorageRestored} interiorAssembler={interiorAssemblerRestored} attachment={attachmentTransitRestored} world={worldRestored} transitPrecise={transitHeadingIsPrecise} transitRestored={transitHeadingRestored} worldPaths={expectedWorldPathsCaptured} interiorPaths={expectedInteriorPathsCaptured} indexPaths={indexPathsCaptured} unsupported={unsupportedRejected} corrupt={corruptRejected}");
            return false;
        }

        GD.Print(
            $"MOBILE_FACTORY_RUNTIME_SAVE_SMOKE player={playerRestored} combat={combatRestored} enemies={enemiesRestored} mobile={mobileRestored} interiorStorage={interiorStorageRestored} interiorAssembler={interiorAssemblerRestored} attachment={attachmentTransitRestored} world={worldRestored} transitPrecise={transitHeadingIsPrecise} transitRestored={transitHeadingRestored} worldPaths={expectedWorldPathsCaptured} interiorPaths={expectedInteriorPathsCaptured} indexPaths={indexPathsCaptured} unsupported={unsupportedRejected} corrupt={corruptRejected}");
        return true;
    }
}
