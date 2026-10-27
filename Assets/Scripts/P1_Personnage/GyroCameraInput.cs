using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

// P1 - Gyroscope : tourner le telephone pour orienter la camera autour du personnage.
// Sans gyroscope (Editor, vieux telephone), la camera se tourne en glissant le doigt / la souris.
public class GyroCameraInput : MonoBehaviour
{
    public static GyroCameraInput Instance { get; private set; }

    public bool useGyro = true;
    public float sensitivity = 1f;
    [Tooltip("Rotation minimale (rad/s) pour eviter que la camera derive toute seule")]
    public float deadZone = 0.03f;
    public bool invertYaw;
    public bool invertPitch;

    public bool Available => SensorUtil.IsOn(Gyroscope.current);

    // Rotation a appliquer a la camera pendant cette frame (degres) : x = gauche/droite, y = haut/bas.
    public Vector2 Delta { get; private set; }

    void Awake() => Instance = this;

    void Start() => SensorUtil.TryEnable(Gyroscope.current);

    void Update()
    {
        Delta = Vector2.zero;
        if (!useGyro || !Available) return;

        Vector3 rate = Gyroscope.current.angularVelocity.ReadValue();
        if (rate.magnitude < deadZone) return;

        float k = Mathf.Rad2Deg * Time.unscaledDeltaTime * sensitivity;
        float yaw = -rate.y * k;
        float pitch = -rate.x * k;
        if (invertYaw) yaw = -yaw;
        if (invertPitch) pitch = -pitch;
        Delta = new Vector2(yaw, pitch);
    }
}
