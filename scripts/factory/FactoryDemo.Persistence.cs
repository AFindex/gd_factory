using System;
using System.Collections.Generic;
using System.Globalization;

public partial class FactoryDemo
{
    private void InitializePersistenceHud()
    {
        _hud?.SetPersistenceStatus(FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: false));
    }

    private void HandleMapSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: false);
    }

    private void HandleMapSourceSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: true);
    }

    private void HandleRuntimeSaveRequested(string slotId)
    {
        SaveRuntimeSnapshot(slotId);
    }

    private void HandleRuntimeLoadRequested(string slotId)
    {
        LoadRuntimeSnapshot(slotId);
    }

    private void SaveCurrentWorldMap(bool saveToSource)
    {
        if (_grid is null)
        {
            return;
        }

        var sourcePath = DemoLaunchOptions.ResolveFactoryWorldMapPath();
        try
        {
            var result = saveToSource
                ? FactoryMapPersistence.SaveWorldMapToSource(sourcePath, _grid)
                : FactoryMapPersistence.SaveWorldMap(sourcePath, _grid);
            var actionLabel = saveToSource ? "已保存到当前地图源" : "已导出运行时副本";
            _previewMessage = $"{actionLabel}：{result.ResourcePath}";
            _hud?.SetPersistenceStatus($"世界地图已保存：{result.GlobalPath}\n当前地图源：{sourcePath}\n蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}");
        }
        catch (Exception ex)
        {
            _previewMessage = $"地图保存失败：{ex.Message}";
            _hud?.SetPersistenceStatus($"地图保存失败：{ex.Message}");
        }

        UpdateHud();
    }

    private void SaveRuntimeSnapshot(string slotId)
    {
        if (_grid is null || _simulation is null)
        {
            return;
        }

        try
        {
            var document = BuildRuntimeSnapshotDocument(slotId);
            var result = FactoryRuntimeSavePersistence.Save(document);
            _previewMessage = $"进度已保存：{result.ResourcePath}";
            _hud?.SetPersistenceStatus(
                $"进度存档已保存：{result.GlobalPath}\n{FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: false)}");
        }
        catch (Exception ex)
        {
            _previewMessage = $"进度保存失败：{ex.Message}";
            _hud?.SetPersistenceStatus($"进度保存失败：{ex.Message}");
        }

        UpdateHud();
    }

    private void LoadRuntimeSnapshot(string slotId)
    {
        if (_grid is null || _structureRoot is null || _simulation is null)
        {
            return;
        }

        try
        {
            var document = FactoryRuntimeSavePersistence.Load(slotId);
            var worldSite = FindRequiredSiteSnapshot(document, _grid.SiteId, FactoryMapKind.World);
            var worldMapDocument = FactoryRuntimeSaveSupport.ParseSiteMap(worldSite, $"{slotId}#world");
            FactoryMapValidator.ValidateAgainstSiteBounds(worldMapDocument, _grid.MinCell, _grid.MaxCell, FactoryMapKind.World);

            if (document.Player is null)
            {
                throw new InvalidOperationException("该进度存档缺少玩家状态。");
            }

            if (_combatDirector is not null && document.CombatDirector is not null)
            {
                _combatDirector.ValidateRuntimeSnapshot(document.CombatDirector);
            }

            FactoryRuntimeSaveSupport.ValidateEnemySnapshots(document.Enemies);

            TearDownRuntimeSession();
            FactoryMapRuntimeLoader.LoadWorldMapDocument(
                $"{slotId}#world",
                worldMapDocument,
                _grid,
                _structureRoot,
                _simulation,
                applyDocumentRuntimeState: false);

            FactoryRuntimeSaveSupport.ApplyStructureSnapshots(worldSite, _grid.GetStructures(), _simulation);
            ConfigureCombatScenarios();
            if (_combatDirector is not null && document.CombatDirector is not null)
            {
                _combatDirector.ApplyRuntimeSnapshot(document.CombatDirector, _simulation);
            }

            _simulation.EnsureNextItemId(document.MaxItemId + 1);

            SpawnPlayerController();
            _playerController!.ApplyRuntimeSnapshot(document.Player, _simulation);
            RestorePlayerSelection(document.Player);

            if (_enemyRoot is not null)
            {
                FactoryRuntimeSaveSupport.RestoreEnemies(_enemyRoot, _simulation, document.Enemies);
            }

            RebuildResourceOverlayVisuals();
            RefreshAllTopology();
            _previewMessage = $"已读取进度：{slotId}";
            _hud?.SetPersistenceStatus(
                $"进度存档已读取：{slotId}\n{FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: false)}");
        }
        catch (Exception ex)
        {
            _previewMessage = $"进度读取失败：{ex.Message}";
            _hud?.SetPersistenceStatus($"进度读取失败：{ex.Message}");
        }

        UpdateHud();
    }

    private FactoryRuntimeSaveSnapshotDocument BuildRuntimeSnapshotDocument(string slotId)
    {
        if (_grid is null || _simulation is null)
        {
            throw new InvalidOperationException("世界站点尚未初始化。");
        }

        var sourcePath = DemoLaunchOptions.ResolveFactoryWorldMapPath();
        var worldDocument = FactoryMapPersistence.CaptureWorldMapDocument(sourcePath, _grid);
        var snapshot = new FactoryRuntimeSaveSnapshotDocument
        {
            Version = FactoryRuntimeSavePersistence.SupportedVersion,
            SlotId = slotId,
            DisplayName = FactoryPersistencePaths.SanitizeRuntimeSaveSlotId(slotId),
            SavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            MaxItemId = Math.Max(0, _simulation.NextItemId - 1),
            Player = _playerController?.CaptureRuntimeSnapshot(),
            CombatDirector = _combatDirector?.CaptureRuntimeSnapshot(_simulation)
        };
        snapshot.Sites.Add(FactoryRuntimeSaveSupport.BuildSiteSnapshot(
            _grid.SiteId,
            FactoryMapKind.World,
            worldDocument,
            _grid.GetStructures()));

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
        _previewMessage = $"已保存蓝图到{targetLabel}：{savedRecord.DisplayName}\n{blueprintPath}";
        _hud.SetPersistenceStatus($"蓝图已保存到{targetLabel}：{blueprintPath}\n运行时蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}\n工程蓝图目录：{FactoryPersistencePaths.GetBlueprintSourceDirectoryGlobalPath()}");
    }

    private void TearDownRuntimeSession()
    {
        if (_grid is null || _simulation is null)
        {
            return;
        }

        _simulation.ClearCombatActors();
        var structures = new List<FactoryStructure>(_grid.GetStructures());
        for (var index = 0; index < structures.Count; index++)
        {
            var structure = structures[index];
            _simulation.UnregisterStructure(structure);
            structure.Site.RemoveStructure(structure);
            structure.QueueFree();
        }

        _grid.SetResourceDeposits(Array.Empty<FactoryResourceDepositDefinition>());
        _combatDirector?.ClearLanes();
        _selectedStructure = null;
        _hoveredStructure = null;
        _pendingBlueprintCapture = null;
        _blueprintApplyPlan = null;
        _blueprintMode = FactoryBlueprintWorkflowMode.None;
    }

    private void RestorePlayerSelection(FactoryPlayerRuntimeSnapshot snapshot)
    {
        _selectedPlayerItemInventoryId = string.IsNullOrWhiteSpace(snapshot.SelectedInventoryId)
            ? FactoryPlayerController.BackpackInventoryId
            : snapshot.SelectedInventoryId;
        _selectedPlayerItemSlot = snapshot.SelectedSlot.ToVector2I();
        _hasSelectedPlayerItemSlot = true;
        _playerPlacementArmed = snapshot.IsHotbarPlacementArmed;
        RefreshInteractionModeFromBuildSource();
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
}
