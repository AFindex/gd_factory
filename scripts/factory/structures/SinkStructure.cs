using Godot;
using NetFactory.Models;
using System;

public partial class SinkStructure : FactoryStructure, IFactoryItemReceiver
{
    private int _recentDelivered;
    private double _rateTimer;
    private MeshInstance3D? _indicator;

    public int DeliveredTotal { get; private set; }
    public int DeliveredRate { get; private set; }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Sink;

    public override string Description => "Destination hub counting delivered items.";

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return IsOrthogonallyAdjacent(Cell, sourceCell)
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!AcceptsFrom(sourceCell)
            || !FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item))
        {
            return false;
        }

        DeliveredTotal++;
        _recentDelivered++;

        if (_indicator is not null)
        {
            _indicator.Scale = new Vector3(1.15f, 1.15f, 1.15f);
        }

        return true;
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return TryAcceptItem(item, sourceCell, simulation);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _rateTimer += stepSeconds;

        if (_rateTimer >= 1.0)
        {
            DeliveredRate = _recentDelivered;
            _recentDelivered = 0;
            _rateTimer = 0.0;
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_indicator is not null)
        {
            _indicator.Scale = _indicator.Scale.Lerp(Vector3.One, tickAlpha * 0.5f);
        }
    }

    protected override void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
        base.CaptureRuntimeState(snapshot);
        snapshot.State["delivered_total"] = FactoryRuntimeSnapshotValues.FormatInt(DeliveredTotal);
        snapshot.State["delivered_rate"] = FactoryRuntimeSnapshotValues.FormatInt(DeliveredRate);
        snapshot.State["recent_delivered"] = FactoryRuntimeSnapshotValues.FormatInt(_recentDelivered);
        snapshot.State["rate_timer"] = FactoryRuntimeSnapshotValues.FormatDouble(_rateTimer);
    }

    protected override void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        base.ApplyRuntimeState(snapshot, simulation);
        DeliveredTotal = FactoryRuntimeSnapshotValues.TryGetInt(snapshot.State, "delivered_total", out var deliveredTotal)
            ? Mathf.Max(0, deliveredTotal)
            : 0;
        DeliveredRate = FactoryRuntimeSnapshotValues.TryGetInt(snapshot.State, "delivered_rate", out var deliveredRate)
            ? Mathf.Max(0, deliveredRate)
            : 0;
        _recentDelivered = FactoryRuntimeSnapshotValues.TryGetInt(snapshot.State, "recent_delivered", out var recentDelivered)
            ? Mathf.Max(0, recentDelivered)
            : 0;
        _rateTimer = FactoryRuntimeSnapshotValues.TryGetDouble(snapshot.State, "rate_timer", out var rateTimer)
            ? Mathf.Max(0.0, rateTimer)
            : 0.0;
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        SinkModelDescriptor.BuildModel(builder, SiteKind, GetInteriorVisualRole());
        _indicator = builder.Root.FindChild("Beacon", true, false) as MeshInstance3D;
    }
}
