using UnityEngine;

// P1 - Camera a la troisieme personne qui tourne autour du joueur.
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 6f;
    public float pivotHeight = 1.6f;
    [Tooltip("Degres par pixel quand on glisse le doigt")]
    public float touchSensitivity = 0.15f;
    public float minPitch = -15f;
    public float maxPitch = 65f;
    [Tooltip("Ce qui bloque la camera (le joueur et les ennemis sont sur 'Ignore Raycast')")]
    public LayerMask collisionMask = ~(1 << 2);

    float yaw;
    float pitch = 18f;

    public float Yaw { get => yaw; set => yaw = value; }
    public float Pitch { get => pitch; set => pitch = Mathf.Clamp(value, minPitch, maxPitch); }

    void Start()
    {
        if (target) yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;

        if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
        {
            Vector2 look = InputHub.Instance ? InputHub.Instance.Look : Vector2.zero;
            yaw += look.x * touchSensitivity;
            pitch -= look.y * touchSensitivity;

            var gyro = GyroCameraInput.Instance;
            if (gyro)
            {
                yaw += gyro.Delta.x;
                pitch += gyro.Delta.y;
            }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * pivotHeight;
        Vector3 back = rot * Vector3.back;
        float dist = distance;
        if (Physics.SphereCast(pivot, 0.25f, back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(0.8f, hit.distance - 0.1f);

        transform.SetPositionAndRotation(pivot + back * dist, rot);
    }
}
