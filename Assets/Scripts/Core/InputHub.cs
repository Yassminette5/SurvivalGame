using UnityEngine;
using UnityEngine.InputSystem;

// Partage : regroupe les commandes tactiles (joystick, boutons) et le clavier (tests dans l'Editor).
[DefaultExecutionOrder(-50)]
public class InputHub : MonoBehaviour
{
    public static InputHub Instance { get; private set; }

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool Sprint { get; private set; }
    public bool ActionPressed { get; private set; }
    public bool JumpPressed { get; private set; }

    [HideInInspector] public Vector2 joystick;
    [HideInInspector] public bool sprintHeld;

    Vector2 lookAccum;
    bool actionQueued;
    bool jumpQueued;

    public void AddLook(Vector2 delta) => lookAccum += delta;
    public void QueueAction() => actionQueued = true;
    public void QueueJump() => jumpQueued = true;

    void Awake() => Instance = this;

    void Update()
    {
        var k = Keyboard.current;
        Vector2 kb = Vector2.zero;
        if (k != null)
        {
            if (k.wKey.isPressed || k.upArrowKey.isPressed) kb.y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) kb.y -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) kb.x += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) kb.x -= 1f;
        }

        Move = Vector2.ClampMagnitude(joystick + kb, 1f);
        Sprint = sprintHeld || (k != null && k.leftShiftKey.isPressed);
        ActionPressed = actionQueued || (k != null && (k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame));
        JumpPressed = jumpQueued || (k != null && k.spaceKey.wasPressedThisFrame);
        actionQueued = false;
        jumpQueued = false;

        Look = lookAccum;
        lookAccum = Vector2.zero;
    }
}
