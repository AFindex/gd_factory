using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class MobileFactoryDemo
{
    private void InitializePersistenceHud()
    {
        _hud?.SetPersistenceStatus(FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: true));
    }

    private void HandleWorldMapSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: false);
    }

    private void HandleWorldMapSourceSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: true);
    }

    private void SaveCurrentWorldMap(bool saveToSource)
    {
        if (_grid is null)
        {
            return;
        }

        var sourcePath = DemoLaunchOptions.ResolveMobileWorldMapPath();
        try
        {
            var result = saveToSource
                ? FactoryMapPersistence.SaveWorldMapToSource(sourcePath, _grid)
                : FactoryMapPersistence.SaveWorldMap(sourcePath, _grid);
            var status = saveToSource
                ? $"世界地图已保存到当前源：{result.GlobalPath}"
                : $"世界地图运行时副本已导出：{result.GlobalPath}";
            _worldPreviewMessage = saveToSource
                ? $"已保存世界地图到当前源：{result.ResourcePath}"
                : $"已导出世界地图副本：{result.ResourcePath}";
            _hud?.SetPersistenceStatus($"{status}\n世界地图源：{sourcePath}\n内部地图目录：{FactoryPersistencePaths.GetInteriorMapDirectoryGlobalPath()}\n蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}");
            ShowWorldEvent(status, true);
        }
        catch (Exception ex)
        {
            var status = $"世界地图保存失败：{ex.Message}";
            _worldPreviewMessage = status;
            _hud?.SetPersistenceStatus(status);
            ShowWorldEvent(status, false);
        }

        UpdateHud();
    }

    private void HandleInteriorMapSaveRequested()
    {
        SaveCurrentInteriorMap(saveToSource: false);
    }

    private void HandleInteriorMapSourceSaveRequested()
    {
        SaveCurrentInteriorMap(saveToSource: true);
    }

    private void HandleRuntimeSaveRequested(string slotId)
    {
        SaveRuntimeSnapshot(slotId);
    }

    private void HandleRuntimeLoadRequested(string slotId)
    {
        LoadRuntimeSnapshot(slotId);
    }

    private void SaveCurrentInteriorMap(bool saveToSource)
    {
        if (_mobileFactory is null)
        {
            return;
        }

        var sourcePath = DemoLaunchOptions.ResolveMobileInteriorMapPath();
        try
        {
            var result = saveToSource
                ? FactoryMapPersistence.SaveInteriorMapToSource(
                    sourcePath,
                    _mobileFactory.InteriorSite,
                    _mobileFactory.Profile.Id)
                : FactoryMapPersistence.SaveInteriorMap(
                    sourcePath,
                    _mobileFactory.InteriorSite,
                    _mobileFactory.Profile.Id);
            var status = saveToSource
                ? $"内部地图已保存到当前源：{result.GlobalPath}"
                : $"内部地图运行时副本已导出：{result.GlobalPath}";
            _interiorPreviewMessage = saveToSource
                ? $"已保存内部地图到当前源：{result.ResourcePath}"
                : $"已导出内部地图副本：{result.ResourcePath}";
            _hud?.SetPersistenceStatus($"世界地图目录：{FactoryPersistencePaths.GetWorldMapDirectoryGlobalPath()}\n{status}\n内部地图源：{sourcePath}\n蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}");
            ShowWorldEvent(status, true);
        }
        catch (Exception ex)
        {
            var status = $"内部地图保存失败：{ex.Message}";
            _interiorPreviewMessage = status;
            _hud?.SetPersistenceStatus(status);
            ShowWorldEvent(status, false);
        }

        UpdateHud();
    }

    private void SaveRuntimeSnapshot(string slotId)
    {
        if (UseLargeTestScenario)
        {
            _interiorPreviewMessage = "大型场景暂不支持运行时进度存档。";
            _hud?.SetPersistenceStatus(_interiorPreviewMessage);
            ShowWorldEvent(_interiorPreviewMessage, false);
            UpdateHud();
            return;
        }

        if (_grid is null || _simulation is null || _mobileFactory is null)
        {
            return;
        }

        try
        {
            var document = BuildRuntimeSnapshotDocument(slotId);
            var result = FactoryRuntimeSavePersistence.Save(document);
            var status = $"进度存档已保存：{result.GlobalPath}";
            _worldPreviewMessage = $"已保存进度：{result.ResourcePath}";
            _interiorPreviewMessage = _worldPreviewMessage;
            _hud?.SetPersistenceStatus($"{status}\n{FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: true)}");
            ShowWorldEvent(status, true);
        }
        catch (Exception ex)
        {
            var status = $"进度保存失败：{ex.Message}";
            _worldPreviewMessage = status;
            _interiorPreviewMessage = status;
            _hud?.SetPersistenceStatus(status);
            ShowWorldEvent(status, false);
        }

        UpdateHud();
    }

    private void LoadRuntimeSnapshot(string slotId)
    {
        if (UseLargeTestScenario)
        {
            var status = "大型场景暂不支持运行时进度读档。";
            _worldPreviewMessage = status;
            _interiorPreviewMessage = status;
            _hud?.SetPersistenceStatus(status);
            ShowWorldEvent(status, false);
            UpdateHud();
            return;
        }

        if (_grid is null || _structureRoot is null || _simulation is null || _enemyRoot is null)
        {
            return;
        }

        try
        {
            var document = FactoryRuntimeSavePersistence.Load(slotId);
            if (document.Player is null)
            {
                throw new InvalidOperationException("该进度存档缺少玩家状态。");
            }

            if (document.MobileFactory is null)
            {
                throw new InvalidOperationException("该进度存档缺少移动工厂状态。");
            }

            if (!string.Equals(document.MobileFactory.FactoryId, "demo-mobile-factory", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("该进度存档不属于当前 focused mobile demo。");
            }

            var worldSite = FindRequiredSiteSnapshot(document, _grid.SiteId, FactoryMapKind.World);
            var interiorSite = FindRequiredSiteSnapshot(document, "mobile-site:demo-mobile-factory", FactoryMapKind.Interior);
            var worldMapDocument = FactoryRuntimeSaveSupport.ParseSiteMap(worldSite, $"{slotId}#world");
            var interiorMapDocument = FactoryRuntimeSaveSupport.ParseSiteMap(interiorSite, $"{slotId}#interior");
            FactoryMapValidator.ValidateAgainstSiteBounds(worldMapDocument, _grid.MinCell, _grid.MaxCell, FactoryMapKind.World);
            FactoryMapRuntimeLoader.ValidateInteriorDocumentAgainstProfile(
                interiorMapDocument,
                MobileFactoryScenarioLibrary.CreateFocusedDemoProfile(),
                $"{slotId}#interior");
            if (_combatDirector is not null && document.CombatDirector is not null)
            {
                _combatDirector.ValidateRuntimeSnapshot(document.CombatDirector);
            }

            FactoryRuntimeSaveSupport.ValidateEnemySnapshots(document.Enemies);

            TearDownRuntimeSession();

            var worldLoad = FactoryMapRuntimeLoader.LoadWorldMapDocument(
                $"{slotId}#world",
                worldMapDocument,
                _grid,
                _structureRoot,
                _simulation,
                applyDocumentRuntimeState: false);
            RebindFocusedScenarioSinks(worldLoad);
            ConfigureWorldCombatScenarios();

            RecreateFocusedMobileFactoryForRuntimeLoad();
            _mobileFactory!.RebuildInteriorFromMapDocument(interiorMapDocument, rebuildTopology: false);
            _mobileFactory.ApplyRuntimeSnapshot(document.MobileFactory, _grid);

            FactoryRuntimeSaveSupport.ApplyStructureSnapshots(worldSite, _grid.GetStructures(), _simulation);
            FactoryRuntimeSaveSupport.ApplyStructureSnapshots(interiorSite, _mobileFactory.InteriorSite.GetStructures(), _simulation);

            if (_combatDirector is not null && document.CombatDirector is not null)
            {
                _combatDirector.ApplyRuntimeSnapshot(document.CombatDirector, _simulation);
            }

            _simulation.EnsureNextItemId(document.MaxItemId + 1);

            SpawnPlayerController();
            _playerController!.ApplyRuntimeSnapshot(document.Player, _simulation);
            RestorePlayerSelection(document.Player);

            FactoryRuntimeSaveSupport.RestoreEnemies(_enemyRoot, _simulation, document.Enemies);

            RebuildMobileResourceOverlayVisuals();
            PullFactoryStatusMessage();
            RefreshInteriorInteractionModeFromBuildSource();
            _simulation.RebuildTopology();

            var status = $"进度存档已读取：{slotId}";
            _worldPreviewMessage = status;
            _interiorPreviewMessage = status;
            _hud?.SetPersistenceStatus($"{status}\n{FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: true)}");
            ShowWorldEvent(status, true);
        }
        catch (Exception ex)
        {
            var status = $"进度读取失败：{ex.Message}";
            _worldPreviewMessage = status;
            _interiorPreviewMessage = status;
            _hud?.SetPersistenceStatus(status);
            ShowWorldEvent(status, false);
        }

        UpdateHud();
    }

    private FactoryRuntimeSaveSnapshotDocument BuildRuntimeSnapshotDocument(string slotId)
    {
        if (_grid is null || _simulation is null || _mobileFactory is null)
        {
            throw new InvalidOperationException("移动工厂场景尚未初始化。");
        }

        var worldSourcePath = DemoLaunchOptions.ResolveMobileWorldMapPath();
        var interiorSourcePath = DemoLaunchOptions.ResolveMobileInteriorMapPath();
        var worldDocument = FactoryMapPersistence.CaptureWorldMapDocument(worldSourcePath, _grid);
        var interiorDocument = FactoryMapPersistence.CaptureInteriorMapDocument(
            interiorSourcePath,
            _mobileFactory.InteriorSite,
            _mobileFactory.Profile.Id);

        var snapshot = new FactoryRuntimeSaveSnapshotDocument
        {
            Version = FactoryRuntimeSavePersistence.SupportedVersion,
            SlotId = slotId,
            DisplayName = FactoryPersistencePaths.SanitizeRuntimeSaveSlotId(slotId),
            SavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            MaxItemId = Math.Max(0, _simulation.NextItemId - 1),
            Player = _playerController?.CaptureRuntimeSnapshot(),
            CombatDirector = _combatDirector?.CaptureRuntimeSnapshot(_simulation),
            MobileFactory = _mobileFactory.CaptureRuntimeSnapshot()
        };

        snapshot.Sites.Add(FactoryRuntimeSaveSupport.BuildSiteSnapshot(
            _grid.SiteId,
            FactoryMapKind.World,
            worldDocument,
            FilterWorldRuntimeStructures(_grid.GetStructures())));
        snapshot.Sites.Add(FactoryRuntimeSaveSupport.BuildSiteSnapshot(
            _mobileFactory.InteriorSite.SiteId,
            FactoryMapKind.Interior,
            interiorDocument,
            _mobileFactory.InteriorSite.GetStructures()));

        var enemies = _simulation.SnapshotActiveEnemies();
        for (var index = 0; index < enemies.Count; index++)
        {
            snapshot.Enemies.Add(enemies[index].CaptureRuntimeSnapshot());
        }

        return snapshot;
    }

    private void ShowBlueprintPersistenceStatus(FactoryBlueprintRecord savedRecord, FactoryBlueprintPersistenceTarget target)
    {
        if (_hud is null)
        {
            return;
        }

        var blueprintPath = FactoryBlueprintPersistence.ResolveRecordGlobalPath(savedRecord, target);
        var targetLabel = target == FactoryBlueprintPersistenceTarget.Source ? "工程内" : "运行时";
        _interiorPreviewMessage = $"已保存蓝图到{targetLabel}：{savedRecord.DisplayName}\n{blueprintPath}";
        _hud.SetPersistenceStatus($"蓝图已保存到{targetLabel}：{blueprintPath}\n运行时蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}\n工程蓝图目录：{FactoryPersistencePaths.GetBlueprintSourceDirectoryGlobalPath()}");
    }

    private void TearDownRuntimeSession()
    {
        if (_grid is null || _structureRoot is null || _simulation is null)
        {
            return;
        }

        _simulation.ClearCombatActors();

        var worldStructures = new List<FactoryStructure>(_grid.GetStructures());
        for (var index = 0; index < worldStructures.Count; index++)
        {
            var structure = worldStructures[index];
            _simulation.UnregisterStructure(structure);
            structure.Site.RemoveStructure(structure);
            structure.QueueFree();
        }

        _mobileFactory?.ClearInteriorStructures(rebuildTopology: false);
        foreach (var child in _structureRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        _grid.SetResourceDeposits(Array.Empty<FactoryResourceDepositDefinition>());
        _combatDirector?.ClearLanes();
        _scenarioSinks.Clear();
        _sinkA = null;
        _sinkB = null;
        _mobileFactory = null;
        _backgroundFactories.Clear();
        _backgroundControllers.Clear();
        ClearFactoryLabels();
        _selectedInteriorStructure = null;
        _hoveredInteriorStructure = null;
        _pendingBlueprintCapture = null;
        _interiorBlueprintPlan = null;
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
    }

    private void RecreateFocusedMobileFactoryForRuntimeLoad()
    {
        if (_structureRoot is null || _simulation is null)
        {
            throw new InvalidOperationException("移动工厂结构根节点不可用。");
        }

        _backgroundFactories.Clear();
        _backgroundControllers.Clear();
        ClearFactoryLabels();
        _mobileFactory = new MobileFactoryInstance(
            "demo-mobile-factory",
            _structureRoot,
            _simulation,
            MobileFactoryScenarioLibrary.CreateFocusedDemoProfile(),
            MobileFactoryScenarioLibrary.CreateFocusedDemoPreset());

        if (!_factoryLabelMap.ContainsKey(_mobileFactory))
        {
            AddFactoryLabel(_mobileFactory, "玩家工厂", new Color(0.72f, 0.88f, 1.0f, 0.98f));
        }

        _selectedDeployFacing = _mobileFactory.DeploymentFacing;
        CreateWorldPreviewVisuals(
            _mobileFactory.Profile.FootprintOffsetsEast.Count,
            Mathf.Max(1, _mobileFactory.Profile.AttachmentMounts.Count));
        UpdateInteriorPreviewSizing();
        _interiorBlueprintSite = CreateInteriorBlueprintSiteAdapter();
    }

    private void RebindFocusedScenarioSinks(FactoryWorldMapLoadResult worldLoad)
    {
        _scenarioSinks.Clear();
        for (var index = 0; index < worldLoad.LoadedStructures.Count; index++)
        {
            if (worldLoad.LoadedStructures[index] is SinkStructure sink)
            {
                _scenarioSinks.Add(sink);
            }
        }

        _sinkA = worldLoad.TryGetStructure(FocusedPrimarySinkCellA, out var sinkA) ? sinkA as SinkStructure : null;
        _sinkB = worldLoad.TryGetStructure(FocusedPrimarySinkCellB, out var sinkB) ? sinkB as SinkStructure : null;
    }

    private void RestorePlayerSelection(FactoryPlayerRuntimeSnapshot snapshot)
    {
        _selectedPlayerItemInventoryId = string.IsNullOrWhiteSpace(snapshot.SelectedInventoryId)
            ? FactoryPlayerController.BackpackInventoryId
            : snapshot.SelectedInventoryId;
        _selectedPlayerItemSlot = snapshot.SelectedSlot.ToVector2I();
        _hasSelectedPlayerItemSlot = true;
        _playerInteriorPlacementArmed = snapshot.IsHotbarPlacementArmed;
        _selectedInteriorKind = _playerController?.GetArmedPlaceablePrototype() ?? _selectedInteriorKind;
        _selectedDeployFacing = _mobileFactory?.DeploymentFacing ?? _selectedDeployFacing;
    }

    private static FactoryRuntimeSiteSnapshot FindRequiredSiteSnapshot(
        FactoryRuntimeSaveSnapshotDocument document,
        string siteId,
        FactoryMapKind kind)
    {
        for (var index = 0; index < document.Sites.Count; index++)
        {
            var site = document.Sites[index];
            if (string.Equals(site.SiteId, siteId, StringComparison.OrdinalIgnoreCase) && site.Kind == kind)
            {
                return site;
            }
        }

        throw new InvalidOperationException($"进度存档缺少站点 '{siteId}'。");
    }

    private static IEnumerable<FactoryStructure> FilterWorldRuntimeStructures(IEnumerable<FactoryStructure> structures)
    {
        foreach (var structure in structures)
        {
            if (!MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(structure.Kind))
            {
                yield return structure;
            }
        }
    }
}
