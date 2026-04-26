using Godot;
using System.Collections.Generic;

public sealed class FactoryDemoRootSpec
{
    public FactoryDemoRootSpec(string id, string nodeName, bool visible = true)
    {
        Id = id;
        NodeName = nodeName;
        Visible = visible;
    }

    public string Id { get; }
    public string NodeName { get; }
    public bool Visible { get; }
}

public sealed class FactoryDemoWorldScaffold
{
    private readonly Dictionary<string, Node3D> _roots;

    public FactoryDemoWorldScaffold(
        Dictionary<string, Node3D> roots,
        SimulationController simulation,
        FactoryCombatDirector combatDirector,
        FactoryCameraRig cameraRig,
        FactoryPlayerHud playerHud)
    {
        _roots = roots;
        Simulation = simulation;
        CombatDirector = combatDirector;
        CameraRig = cameraRig;
        PlayerHud = playerHud;
    }

    public SimulationController Simulation { get; }
    public FactoryCombatDirector CombatDirector { get; }
    public FactoryCameraRig CameraRig { get; }
    public FactoryPlayerHud PlayerHud { get; }

    public Node3D GetRoot(string id)
    {
        return _roots[id];
    }

    public bool TryGetRoot(string id, out Node3D root)
    {
        return _roots.TryGetValue(id, out root!);
    }
}

public static class FactoryDemoSceneScaffold
{
    public static FactoryDemoWorldScaffold Build(
        Node parent,
        int minCell,
        int maxCell,
        IEnumerable<FactoryDemoRootSpec> rootSpecs,
        string combatDirectorName)
    {
        parent.AddChild(FactoryDemoScenePrimitives.CreateEnvironment());
        parent.AddChild(FactoryDemoScenePrimitives.CreateDirectionalLight());
        parent.AddChild(FactoryDemoScenePrimitives.CreateFloor(minCell, maxCell));
        parent.AddChild(FactoryDemoScenePrimitives.CreateGridLines(minCell, maxCell));

        var roots = new Dictionary<string, Node3D>();
        foreach (var rootSpec in rootSpecs)
        {
            var root = new Node3D
            {
                Name = rootSpec.NodeName,
                Visible = rootSpec.Visible
            };
            parent.AddChild(root);
            roots[rootSpec.Id] = root;
        }

        var simulation = new SimulationController { Name = "SimulationController" };
        parent.AddChild(simulation);

        var combatDirector = new FactoryCombatDirector { Name = combatDirectorName };
        parent.AddChild(combatDirector);

        var cameraRig = new FactoryCameraRig();
        parent.AddChild(cameraRig);

        var playerHud = new FactoryPlayerHud();
        parent.AddChild(playerHud);

        parent.AddChild(new LauncherNavigationOverlay());

        return new FactoryDemoWorldScaffold(roots, simulation, combatDirector, cameraRig, playerHud);
    }
}

public static class FactoryDemoScenePrimitives
{
    public static WorldEnvironment CreateEnvironment()
    {
        var environment = new Environment
        {
            BackgroundMode = Environment.BGMode.Color,
            BackgroundColor = new Color("111827"),
            AmbientLightSource = Environment.AmbientSource.Color,
            AmbientLightColor = new Color("D6E4F0"),
            AmbientLightSkyContribution = 0.0f,
            AmbientLightEnergy = 0.7f
        };

        return new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = environment
        };
    }

    public static DirectionalLight3D CreateDirectionalLight()
    {
        return new DirectionalLight3D
        {
            Name = "SunLight",
            RotationDegrees = new Vector3(-56.0f, -34.0f, 0.0f),
            LightEnergy = 1.45f,
            ShadowEnabled = true
        };
    }

    public static Node3D CreateFloor(int minCell, int maxCell)
    {
        var floorRoot = new Node3D { Name = "FloorRoot" };
        var floor = new MeshInstance3D { Name = "FactoryFloor" };
        floor.Mesh = new PlaneMesh
        {
            Size = new Vector2(
                (maxCell - minCell + 1) * FactoryConstants.CellSize,
                (maxCell - minCell + 1) * FactoryConstants.CellSize)
        };

        floor.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color("1F2937"),
            Roughness = 1.0f
        };
        floorRoot.AddChild(floor);
        return floorRoot;
    }

    public static Node3D CreateGridLines(int minCell, int maxCell)
    {
        var gridRoot = new Node3D { Name = "GridLines" };
        var lineMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.35f, 0.43f, 0.53f, 0.65f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 1.0f
        };
        var worldMin = (minCell - 0.5f) * FactoryConstants.CellSize;
        var worldMax = (maxCell + 0.5f) * FactoryConstants.CellSize;
        var lineLength = worldMax - worldMin;

        for (var i = minCell; i <= maxCell + 1; i++)
        {
            var x = (i - 0.5f) * FactoryConstants.CellSize;
            var vertical = new MeshInstance3D();
            vertical.Mesh = new BoxMesh { Size = new Vector3(0.03f, 0.02f, lineLength) };
            vertical.Position = new Vector3(x, 0.02f, 0.0f);
            vertical.MaterialOverride = lineMaterial;
            gridRoot.AddChild(vertical);

            var z = (i - 0.5f) * FactoryConstants.CellSize;
            var horizontal = new MeshInstance3D();
            horizontal.Mesh = new BoxMesh { Size = new Vector3(lineLength, 0.02f, 0.03f) };
            horizontal.Position = new Vector3(0.0f, 0.02f, z);
            horizontal.MaterialOverride = lineMaterial;
            gridRoot.AddChild(horizontal);
        }

        return gridRoot;
    }
}

public static class FactoryDemoInputActions
{
    public static void EnsureCommonActions()
    {
        EnsureAction("camera_pan_left", new InputEventKey { PhysicalKeycode = Key.A }, new InputEventKey { PhysicalKeycode = Key.Left });
        EnsureAction("camera_pan_right", new InputEventKey { PhysicalKeycode = Key.D }, new InputEventKey { PhysicalKeycode = Key.Right });
        EnsureAction("camera_pan_up", new InputEventKey { PhysicalKeycode = Key.W }, new InputEventKey { PhysicalKeycode = Key.Up });
        EnsureAction("camera_pan_down", new InputEventKey { PhysicalKeycode = Key.S }, new InputEventKey { PhysicalKeycode = Key.Down });
        EnsureAction("player_move_left", new InputEventKey { PhysicalKeycode = Key.A });
        EnsureAction("player_move_right", new InputEventKey { PhysicalKeycode = Key.D });
        EnsureAction("player_move_forward", new InputEventKey { PhysicalKeycode = Key.W });
        EnsureAction("player_move_backward", new InputEventKey { PhysicalKeycode = Key.S });
        EnsureAction("camera_zoom_in", new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true });
        EnsureAction("camera_zoom_out", new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true });
    }

    public static void EnsureAction(string actionName, params InputEvent[] events)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        if (InputMap.ActionGetEvents(actionName).Count > 0)
        {
            return;
        }

        foreach (var inputEvent in events)
        {
            InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }
}
