using System;
using UnityEngine;
using UnityEngine.InputSystem;

// P4 - Accelerometre : secouer le telephone pour attaquer un ennemi (ou frapper un arbre / rocher).
// Secours : bouton Action a l'ecran, touche E ou F dans l'Editor.
public class ShakeDetector : MonoBehaviour
{
    public PlayerActions player;
    [Tooltip("Force de la secousse en g (1 g = gravite)")]
    public float threshold = 1.5f;
    public float cooldown = 0.45f;
    [Tooltip("Temps de lissage pour separer la gravite des mouvements rapides")]
    public float filterTime = 0.2f;

    public bool Available => SensorUtil.IsOn(Accelerometer.current);
    public event Action Shaken;

    Vector3 slow;
    bool initialized;
    float lastShake = -10f;

    void Start() => SensorUtil.TryEnable(Accelerometer.current);

    void Update()
    {
        if (!Available) return;

        Vector3 a = Accelerometer.current.acceleration.ReadValue();
        if (!initialized)
        {
            slow = a;
            initialized = true;
        }
        float dt = Time.unscaledDeltaTime;
        slow = Vector3.Lerp(slow, a, dt / (filterTime + dt));

        if ((a - slow).magnitude > threshold && Time.unscaledTime - lastShake > cooldown)
        {
            lastShake = Time.unscaledTime;
            Shaken?.Invoke();
            if (player) player.DoAction();
        }
    }
}
