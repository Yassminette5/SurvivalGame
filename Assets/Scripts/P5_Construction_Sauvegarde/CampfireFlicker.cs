using UnityEngine;

// P5 - Fait vaciller la lumiere du feu de camp.
public class CampfireFlicker : MonoBehaviour
{
    public Light fireLight;
    public float baseIntensity = 3f;

    void Update()
    {
        if (fireLight)
            fireLight.intensity = baseIntensity * (0.85f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.3f);
    }
}
