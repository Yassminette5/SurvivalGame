using UnityEngine;
using UnityEngine.InputSystem;

// P3 - Magnetometre : calcule le cap reel du telephone (0 = nord, 90 = est).
// Le magnetometre donne le nord magnetique, l'accelerometre sert a corriger l'inclinaison du telephone.
// Sans capteur (Editor), le cap suit la camera du jeu.
public class MagnetometerCompass : MonoBehaviour
{
    public static MagnetometerCompass Instance { get; private set; }

    [Tooltip("Declinaison magnetique en degres pour passer du nord magnetique au vrai nord (Tunisie : environ +3)")]
    public float declination = 3f;
    public float smoothing = 6f;

    public float Heading { get; private set; }

    public bool Available => SensorUtil.IsOn(MagneticFieldSensor.current) && SensorUtil.IsOn(Accelerometer.current);

    float smoothSin;
    float smoothCos = 1f;

    void Awake() => Instance = this;

    void Start()
    {
        SensorUtil.TryEnable(MagneticFieldSensor.current);
        SensorUtil.TryEnable(Accelerometer.current);
    }

    void Update()
    {
        if (!Available)
        {
            var cam = Camera.main;
            if (cam) Heading = Mathf.Repeat(cam.transform.eulerAngles.y, 360f);
            return;
        }

        // Unity inverse le signe de l'accelerometre : -acceleration pointe vers le haut.
        Vector3 up = -Accelerometer.current.acceleration.ReadValue();
        Vector3 mag = MagneticFieldSensor.current.magneticField.ReadValue();

        Vector3 east = Vector3.Cross(mag, up);
        if (east.sqrMagnitude < 1e-6f || up.sqrMagnitude < 1e-4f) return;
        east.Normalize();
        Vector3 north = Vector3.Cross(up.normalized, east);

        // Direction ou "regarde" le telephone : le haut de l'ecran s'il est a plat,
        // sinon l'arriere du telephone (comme la camera photo).
        Vector3 look = Mathf.Abs(up.normalized.z) > 0.75f ? Vector3.up : Vector3.back;
        float h = Mathf.Atan2(Vector3.Dot(look, east), Vector3.Dot(look, north)) * Mathf.Rad2Deg + declination;

        // Lissage sur le cercle (evite le saut entre 359 et 0 degres).
        float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        float r = h * Mathf.Deg2Rad;
        smoothSin = Mathf.Lerp(smoothSin, Mathf.Sin(r), k);
        smoothCos = Mathf.Lerp(smoothCos, Mathf.Cos(r), k);
        Heading = Mathf.Repeat(Mathf.Atan2(smoothSin, smoothCos) * Mathf.Rad2Deg, 360f);
    }
}
