using Godot;

public sealed class MobileFactoryScenarioActorController
{
    private enum RouteState
    {
        MovingToTransitPoint,
        HoldingTransit,
        AwaitingDeploy,
        Deploying,
        HoldingDeployed,
        AwaitingRecall
    }

    private readonly GridManager _worldGrid;
    private readonly MobileFactoryInstance _factory;
    private readonly MobileFactoryScenarioActorDefinition _definition;
    private int _routeIndex;
    private float _stateTimer;
    private RouteState _state;

    public MobileFactoryScenarioActorController(GridManager worldGrid, MobileFactoryInstance factory, MobileFactoryScenarioActorDefinition definition)
    {
        _worldGrid = worldGrid;
        _factory = factory;
        _definition = definition;

        if (_definition.RoutePoints.Count == 0)
        {
            _state = RouteState.HoldingTransit;
            _stateTimer = 0.0f;
            return;
        }

        _state = _factory.State == MobileFactoryLifecycleState.Deployed
            ? RouteState.HoldingDeployed
            : RouteState.MovingToTransitPoint;

        if (_factory.State == MobileFactoryLifecycleState.Deployed)
        {
            _stateTimer = _definition.RoutePoints[0].DeployedHoldSeconds;
        }
    }

    public void Update(double delta)
    {
        if (_definition.RoutePoints.Count == 0)
        {
            return;
        }

        var routePoint = _definition.RoutePoints[_routeIndex];

        switch (_state)
        {
            case RouteState.MovingToTransitPoint:
            {
                var arrived = _factory.UpdateTransitAutopilot(_worldGrid, routePoint.TransitPosition, routePoint.TransitFacing, delta);
                if (arrived)
                {
                    _stateTimer = routePoint.TransitHoldSeconds;
                    _state = _stateTimer > 0.01f ? RouteState.HoldingTransit : RouteState.AwaitingDeploy;
                }

                break;
            }
            case RouteState.HoldingTransit:
            {
                _stateTimer -= (float)delta;
                if (_stateTimer <= 0.0f)
                {
                    _state = RouteState.AwaitingDeploy;
                }

                break;
            }
            case RouteState.AwaitingDeploy:
            {
                if (_factory.State == MobileFactoryLifecycleState.InTransit)
                {
                    if (_factory.TryStartAutoDeploy(_worldGrid, routePoint.DeployAnchor, routePoint.DeployFacing))
                    {
                        _state = RouteState.Deploying;
                    }
                }

                break;
            }
            case RouteState.Deploying:
            {
                if (_factory.State == MobileFactoryLifecycleState.Deployed)
                {
                    _stateTimer = routePoint.DeployedHoldSeconds;
                    _state = RouteState.HoldingDeployed;
                }

                break;
            }
            case RouteState.HoldingDeployed:
            {
                if (_factory.State != MobileFactoryLifecycleState.Deployed)
                {
                    break;
                }

                _stateTimer -= (float)delta;
                if (_stateTimer <= 0.0f && _factory.Recall())
                {
                    _state = RouteState.AwaitingRecall;
                }

                break;
            }
            case RouteState.AwaitingRecall:
            {
                if (_factory.State == MobileFactoryLifecycleState.InTransit)
                {
                    _routeIndex = (_routeIndex + 1) % _definition.RoutePoints.Count;
                    _state = RouteState.MovingToTransitPoint;
                }

                break;
            }
        }
    }
}
