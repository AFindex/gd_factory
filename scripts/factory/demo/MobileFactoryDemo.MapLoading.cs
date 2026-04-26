using Godot;

public partial class MobileFactoryDemo
{
    private static readonly Vector2I FocusedPrimarySinkCellA = new(0, -3);
    private static readonly Vector2I FocusedPrimarySinkCellB = new(8, 3);

    private void LoadFocusedWorldMap()
    {
        if (_grid is null || _structureRoot is null || _simulation is null)
        {
            return;
        }

        var mapPath = DemoLaunchOptions.ResolveMobileWorldMapPath();
        var result = FactoryMapRuntimeLoader.LoadWorldMap(
            mapPath,
            _grid,
            _structureRoot,
            _simulation);

        _scenarioSinks.Clear();
        for (var i = 0; i < result.LoadedStructures.Count; i++)
        {
            if (result.LoadedStructures[i] is SinkStructure sink)
            {
                _scenarioSinks.Add(sink);
            }
        }

        _sinkA = result.TryGetStructure(FocusedPrimarySinkCellA, out var sinkA) ? sinkA as SinkStructure : null;
        _sinkB = result.TryGetStructure(FocusedPrimarySinkCellB, out var sinkB) ? sinkB as SinkStructure : null;
    }

    private void ApplyFocusedInteriorMapRuntimeState()
    {
        if (_mobileFactory is null || _simulation is null)
        {
            return;
        }

        var mapPath = DemoLaunchOptions.ResolveMobileInteriorMapPath();
        FactoryMapRuntimeLoader.ApplyInteriorRuntimeState(
            mapPath,
            _mobileFactory,
            _simulation);
    }


}


