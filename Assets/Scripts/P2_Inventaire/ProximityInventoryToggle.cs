using UnityEngine;
using UnityEngine.InputSystem;

// P2 - Capteur de proximite : passer la main devant le haut du telephone ouvre ou ferme le sac.
// Secours : bouton "Sac" a l'ecran, touche Tab ou I dans l'Editor.
[RequireComponent(typeof(InventoryUI))]
public class ProximityInventoryToggle : MonoBehaviour
{
    [Tooltip("En dessous de cette distance (cm), la main est consideree devant le capteur")]
    public float nearDistance = 3f;
    public float cooldown = 1f;

    InventoryUI inventoryUI;
    bool sawFar;
    bool wasNear;
    float lastToggle = -10f;

    public bool Available => SensorUtil.IsOn(ProximitySensor.current);

    void Awake() => inventoryUI = GetComponent<InventoryUI>();

    void Start() => SensorUtil.TryEnable(ProximitySensor.current);

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && (k.tabKey.wasPressedThisFrame || k.iKey.wasPressedThisFrame)) Toggle();

        if (!Available) return;
        float distance = ProximitySensor.current.distance.ReadValue();

        // Le capteur renvoie 0 tant qu'il n'a rien mesure : on attend une premiere valeur "loin".
        if (!sawFar)
        {
            if (distance >= nearDistance) sawFar = true;
            return;
        }

        bool near = distance < nearDistance;
        if (near && !wasNear && Time.unscaledTime - lastToggle > cooldown)
        {
            lastToggle = Time.unscaledTime;
            Toggle();
        }
        wasNear = near;
    }

    void Toggle()
    {
        if (GameManager.Instance && GameManager.Instance.IsPlaying) inventoryUI.Toggle();
    }
}
