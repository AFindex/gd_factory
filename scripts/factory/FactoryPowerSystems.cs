using Godot;

public enum FactoryPowerStatus
{
    Disconnected,
    Underpowered,
    Powered
}

public interface IFactoryPowerNode
{
    int PowerConnectionRangeCells { get; }
}

public interface IFactoryPowerProducer : IFactoryPowerNode
{
    float GetAvailablePower(SimulationController simulation);
}

public interface IFactoryPowerConsumer : IFactoryPowerNode
{
    bool WantsPower(SimulationController simulation);
    float GetRequestedPower(SimulationController simulation);
    void SetPowerState(FactoryPowerStatus status, float satisfaction, int networkId);
}

public static class FactoryPowerPresentation
{
    public static string ToLabel(FactoryPowerStatus status)
    {
        return status switch
        {
            FactoryPowerStatus.Powered => "供电正常",
            FactoryPowerStatus.Underpowered => "供电不足",
            _ => "未接入电网"
        };
    }

    public static Color GetAccentColor(FactoryPowerStatus status)
    {
        return status switch
        {
            FactoryPowerStatus.Powered => new Color("86EFAC"),
            FactoryPowerStatus.Underpowered => new Color("FDE68A"),
            _ => new Color("FCA5A5")
        };
    }
}
