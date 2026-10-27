using UnityEngine;

// P1 - Deplacement du personnage (joystick / ZQSD), sprint et saut.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public ThirdPersonCamera cam;
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float sprintEnergyPerSecond = 12f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    public float turnSpeed = 12f;

    CharacterController cc;
    PlayerStats stats;
    float verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || stats.IsDead) return;

        float dt = Time.deltaTime;
        var input = InputHub.Instance;
        Vector2 m = input ? input.Move : Vector2.zero;

        Quaternion camYaw = Quaternion.Euler(0f, cam ? cam.Yaw : 0f, 0f);
        Vector3 dir = camYaw * new Vector3(m.x, 0f, m.y);
        bool moving = dir.sqrMagnitude > 0.01f;

        bool sprint = moving && input && input.Sprint && stats.TryUseEnergy(sprintEnergyPerSecond * dt);
        float speed = sprint ? sprintSpeed : walkSpeed;

        if (moving)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-turnSpeed * dt));

        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            if (input && input.JumpPressed) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVelocity += gravity * dt;

        cc.Move((dir * speed + Vector3.up * verticalVelocity) * dt);
    }

    public void Teleport(Vector3 position, float yRotation)
    {
        cc.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
        cc.enabled = true;
        verticalVelocity = 0f;
    }
}
