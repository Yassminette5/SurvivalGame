using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// Bonus - Podometre : les pas reels remontent la barre de faim (voir PlayerStats.hungerPerStep).
// Android demande la permission "activite physique" (ACTIVITY_RECOGNITION).
// Dans l'Editor, la touche P simule 10 pas.
public class Pedometer : MonoBehaviour
{
    public static Pedometer Instance { get; private set; }

    [Tooltip("Ignore les sauts trop gros d'un coup (ex : premiere lecture du compteur)")]
    public int maxStepsPerUpdate = 50;

    public int SessionSteps { get; private set; }
    public bool Available => SensorUtil.IsOn(StepCounter.current);

    public event Action<int> StepsTaken;

    int lastValue = -1;

    void Awake() => Instance = this;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        const string permission = "android.permission.ACTIVITY_RECOGNITION";
        if (!Permission.HasUserAuthorizedPermission(permission))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => SensorUtil.TryEnable(StepCounter.current);
            Permission.RequestUserPermission(permission, callbacks);
            return;
        }
#endif
        SensorUtil.TryEnable(StepCounter.current);
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.pKey.wasPressedThisFrame) AddSteps(10);

        if (!Available) return;
        int value = StepCounter.current.stepCounter.ReadValue();
        if (value <= 0) return;

        // Le compteur Android compte depuis le demarrage du telephone : la 1re valeur sert de reference.
        if (lastValue < 0)
        {
            lastValue = value;
            return;
        }
        int delta = value - lastValue;
        lastValue = value;
        if (delta > 0) AddSteps(Mathf.Min(delta, maxStepsPerUpdate));
    }

    void AddSteps(int steps)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        SessionSteps += steps;
        StepsTaken?.Invoke(steps);
    }
}
