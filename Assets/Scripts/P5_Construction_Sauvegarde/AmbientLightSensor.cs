using UnityEngine;
using UnityEngine.InputSystem;

// P5 - Capteur de lumiere ambiante : piece sombre = nuit dans le jeu.
// Dans l'Editor, la touche L simule le capteur (desactive -> piece sombre -> piece eclairee).
public class AmbientLightSensor : MonoBehaviour
{
    public static AmbientLightSensor Instance { get; private set; }

    [Tooltip("En dessous (lux), la piece est consideree sombre")]
    public float darkLux = 10f;
    [Tooltip("Au-dessus (lux), la piece redevient eclairee (evite de clignoter entre les deux)")]
    public float brightLux = 25f;
    [Tooltip("Temps (s) pendant lequel la lumiere doit rester stable avant de changer")]
    public float stableTime = 1.5f;

    public float Lux { get; private set; } = -1f;
    public bool IsDarkRoom { get; private set; }
    public bool Available => simulation != 0 || SensorUtil.IsOn(LightSensor.current);

    int simulation; // 0 = capteur reel, 1 = piece sombre, 2 = piece eclairee
    float changeTimer;

    void Awake() => Instance = this;

    void Start() => SensorUtil.TryEnable(LightSensor.current);

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.lKey.wasPressedThisFrame)
        {
            simulation = (simulation + 1) % 3;
            UIRoot.Toast(simulation == 0 ? "Simulation lumiere : OFF" : simulation == 1 ? "Simulation : piece sombre" : "Simulation : piece eclairee");
        }

        if (simulation == 1) Lux = 2f;
        else if (simulation == 2) Lux = 300f;
        else if (SensorUtil.IsOn(LightSensor.current)) Lux = LightSensor.current.lightLevel.ReadValue();
        else
        {
            Lux = -1f;
            IsDarkRoom = false;
            return;
        }

        bool wantDark = IsDarkRoom ? Lux < brightLux : Lux < darkLux;
        if (wantDark == IsDarkRoom)
        {
            changeTimer = 0f;
            return;
        }
        changeTimer += Time.unscaledDeltaTime;
        if (changeTimer >= stableTime)
        {
            IsDarkRoom = wantDark;
            changeTimer = 0f;
            UIRoot.Toast(IsDarkRoom ? "La piece est sombre... la nuit tombe !" : "La lumiere revient.");
        }
    }
}
