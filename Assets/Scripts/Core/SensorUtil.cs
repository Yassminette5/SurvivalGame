using UnityEngine.InputSystem;

// Partage : active un capteur du nouvel Input System s'il existe sur le telephone.
public static class SensorUtil
{
    public static bool TryEnable(InputDevice device)
    {
        if (device == null) return false;
        if (!device.enabled) InputSystem.EnableDevice(device);
        return device.enabled;
    }

    public static bool IsOn(InputDevice device) => device != null && device.enabled;
}
