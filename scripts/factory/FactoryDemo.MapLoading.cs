using Godot;

public partial class FactoryDemo
{
    private void LoadStarterWorldMap()
    {
        if (_grid is null || _structureRoot is null || _simulation is null)
        {
            return;
        }

        var mapPath = DemoLaunchOptions.ResolveFactoryWorldMapPath();
        FactoryMapRuntimeLoader.LoadWorldMap(
            mapPath,
            _grid,
            _structureRoot,
            _simulation);
    }


}

