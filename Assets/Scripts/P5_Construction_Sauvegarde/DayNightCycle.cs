using UnityEngine;
using UnityEngine.Rendering;

// P5 - Cycle jour / nuit. Si la piece reelle est sombre (capteur de lumiere), la nuit tombe
// rapidement et reste tant que la piece est sombre. La nuit, les ennemis (P4) sont plus dangereux.
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    public Light sun;
    public Light moon;
    [Tooltip("Duree d'une journee complete en secondes")]
    public float dayLength = 480f;
    [Range(0f, 1f), Tooltip("0 = minuit, 0.25 = lever, 0.5 = midi, 0.75 = coucher")]
    public float timeOfDay = 0.3f;
    public int day = 1;

    [Header("Capteur de lumiere")]
    public bool useLightSensor = true;
    [Tooltip("Vitesse du temps quand la nuit arrive a cause d'une piece sombre")]
    public float darkTransitionSpeed = 25f;

    [Header("Couleurs")]
    public Color dayAmbient = new Color(0.55f, 0.6f, 0.65f);
    public Color nightAmbient = new Color(0.07f, 0.09f, 0.16f);
    public Color dayFog = new Color(0.65f, 0.75f, 0.85f);
    public Color nightFog = new Color(0.03f, 0.04f, 0.08f);
    public Color sunsetColor = new Color(1f, 0.55f, 0.3f);

    public bool IsNight => timeOfDay < 0.23f || timeOfDay > 0.77f;
    public float Daylight { get; private set; }
    public bool ForcedByRoom { get; private set; }

    void Awake() => Instance = this;

    void Start()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        if (sun) RenderSettings.sun = sun;
        Apply();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        var light = AmbientLightSensor.Instance;
        ForcedByRoom = useLightSensor && light && light.IsDarkRoom;

        float speed = 1f;
        if (ForcedByRoom)
        {
            if (!IsNight) speed = darkTransitionSpeed;
            // Garde la nuit tant que la piece est sombre (on ne passe pas le lever du soleil).
            else if (timeOfDay > 0.18f && timeOfDay < 0.23f) speed = 0f;
        }

        timeOfDay += Time.deltaTime / dayLength * speed;
        if (timeOfDay >= 1f)
        {
            timeOfDay -= 1f;
            day++;
        }
        Apply();
    }

    void Apply()
    {
        // Soleil : 0 au lever (0.25), 90 a midi, 180 au coucher (0.75).
        float sunAngle = timeOfDay * 360f - 90f;
        float height = Mathf.Sin((timeOfDay - 0.25f) * Mathf.PI * 2f);
        Daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.15f, 0.3f, height));

        if (sun)
        {
            sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            sun.intensity = 1.3f * Daylight;
            sun.color = Color.Lerp(sunsetColor, Color.white, Mathf.Clamp01(height * 2f));
            sun.enabled = Daylight > 0.01f;
        }
        if (moon)
        {
            moon.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);
            moon.intensity = 0.35f * (1f - Daylight);
            moon.enabled = Daylight < 0.99f;
        }

        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, Daylight);
        RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, Daylight);
        if (Camera.main) Camera.main.backgroundColor = RenderSettings.fogColor;
    }

    public string ClockText()
    {
        float hours = timeOfDay * 24f;
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return h.ToString("00") + ":" + m.ToString("00");
    }
}
