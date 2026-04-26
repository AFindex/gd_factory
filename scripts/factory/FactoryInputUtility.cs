using Godot;

public static class FactoryInputUtility
{
    public static bool TryMapHotbarKey(Key keycode, out int hotbarIndex)
    {
        hotbarIndex = keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            Key.Key6 => 5,
            Key.Key7 => 6,
            Key.Key8 => 7,
            Key.Key9 => 8,
            Key.Key0 => 9,
            _ => -1
        };

        return hotbarIndex >= 0;
    }
}
