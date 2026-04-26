using Godot;
using System.Collections.Generic;

public partial class FactoryDemo
{
    private void ConfigureWorldCombatScenarios()
    {
        if (_combatDirector is null || _grid is null)
        {
            return;
        }

        _combatDirector.ClearLanes();

        if (UseLargeTestScenario)
        {
            ConfigureLargeWorldCombatScenarios();
            return;
        }

        ConfigureFocusedWorldCombatScenarios();
    }

    private void ConfigureFocusedWorldCombatScenarios()
    {
        var profile = MobileFactoryScenarioLibrary.CreateFocusedDemoProfile();
        AddAnchorPressureLane(
            "focused-anchor-a",
            profile,
            AnchorA,
            FacingDirection.East,
            new Vector2I(-1, -1),
            new[]
            {
                new FactoryEnemySpawnRule("melee", 2.7f),
                new FactoryEnemySpawnRule("ranged", 4.6f)
            });
    }

    private void ConfigureLargeWorldCombatScenarios()
    {
        AddAnchorPressureLane(
            "world-heavy-east",
            MobileFactoryScenarioLibrary.CreateHeavyProfile(),
            new Vector2I(-15, -6),
            FacingDirection.East,
            new Vector2I(-1, -1),
            new[]
            {
                new FactoryEnemySpawnRule("world-brute", 4.6f),
                new FactoryEnemySpawnRule("world-siege", 8.8f)
            });
        AddAnchorPressureLane(
            "world-medium-north",
            MobileFactoryScenarioLibrary.CreateMediumProfile(),
            new Vector2I(-4, 7),
            FacingDirection.East,
            new Vector2I(1, 1),
            new[]
            {
                new FactoryEnemySpawnRule("world-brute", 5.2f),
                new FactoryEnemySpawnRule("world-siege", 9.6f)
            });
        AddAnchorPressureLane(
            "world-medium-central",
            MobileFactoryScenarioLibrary.CreateMediumProfile(),
            new Vector2I(-12, 3),
            FacingDirection.East,
            new Vector2I(-1, 1),
            new[]
            {
                new FactoryEnemySpawnRule("world-brute", 5.8f),
                new FactoryEnemySpawnRule("world-siege", 10.8f)
            });
        AddAnchorPressureLane(
            "world-compact-east",
            MobileFactoryScenarioLibrary.CreateCompactProfile(),
            new Vector2I(10, 2),
            FacingDirection.East,
            new Vector2I(1, -1),
            new[]
            {
                new FactoryEnemySpawnRule("world-brute", 7.2f)
            });
    }

    private void AddAnchorPressureLane(
        string laneId,
        MobileFactoryProfile profile,
        Vector2I anchorCell,
        FacingDirection deployFacing,
        Vector2I approachNormal,
        IReadOnlyList<FactoryEnemySpawnRule> rules)
    {
        if (_grid is null || _combatDirector is null || rules.Count == 0)
        {
            return;
        }

        var portCell = GetProfilePortCell(profile, anchorCell, deployFacing, BuildPrototypeKind.OutputPort);
        var portFacing = GetProfilePortFacing(profile, deployFacing, BuildPrototypeKind.OutputPort);
        var forward = FactoryDirection.ToCellOffset(portFacing);
        var flank = NormalizeCardinalOrDiagonal(approachNormal);
        if (flank == Vector2I.Zero)
        {
            flank = new Vector2I(-forward.Y, forward.X);
        }

        var pressureCell = portCell + (forward * 3);
        var pathCells = new[]
        {
            ClampToWorld(pressureCell + (flank * 5)),
            ClampToWorld(pressureCell + (flank * 3)),
            ClampToWorld(pressureCell + (flank * 2)),
            ClampToWorld(pressureCell + flank),
            ClampToWorld(pressureCell)
        };

        var path = new List<Vector3>(pathCells.Length);
        for (var i = 0; i < pathCells.Length; i++)
        {
            path.Add(_grid.CellToWorld(pathCells[i]));
        }

        _combatDirector.AddLane(new FactoryEnemyLaneDefinition(laneId, path, rules));
    }

    private Vector2I ClampToWorld(Vector2I cell)
    {
        return new Vector2I(
            Mathf.Clamp(cell.X, GetWorldMinCell(), GetWorldMaxCell()),
            Mathf.Clamp(cell.Y, GetWorldMinCell(), GetWorldMaxCell()));
    }

    private static Vector2I NormalizeCardinalOrDiagonal(Vector2I value)
    {
        return new Vector2I(
            Mathf.Clamp(value.X, -1, 1),
            Mathf.Clamp(value.Y, -1, 1));
    }

    private int CountActiveHeavyWorldEnemies()
    {
        if (_enemyRoot is null)
        {
            return 0;
        }

        var total = 0;
        foreach (var child in _enemyRoot.GetChildren())
        {
            if (child is WorldBruteEnemy or WorldSiegeEnemy)
            {
                total++;
            }
        }

        return total;
    }

    private int CountMobileTurretShots()
    {
        if (_structureRoot is null)
        {
            return 0;
        }

        var total = 0;
        foreach (var child in _structureRoot.GetChildren())
        {
            if (child is GunTurretStructure turret && turret.Site is MobileFactorySite)
            {
                total += turret.ShotsFired;
            }
        }

        return total;
    }
}
