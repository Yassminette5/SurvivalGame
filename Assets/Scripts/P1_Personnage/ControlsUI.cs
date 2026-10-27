using UnityEngine;
using UnityEngine.UI;

// P1 - Commandes tactiles : joystick, zone camera, boutons Action / Saut / Sprint.
public class ControlsUI : MonoBehaviour
{
    public PlayerActions player;

    Text actionLabel;

    void Start()
    {
        var layer = UIRoot.Layer(UILayer.Controls);

        // Zone camera (droite) - en premier pour etre sous les boutons.
        var look = UIFactory.MakeImage(layer, "LookArea", new Color(0f, 0f, 0f, 0f), true);
        look.rectTransform.anchorMin = new Vector2(0.4f, 0f);
        look.rectTransform.anchorMax = new Vector2(1f, 1f);
        look.rectTransform.offsetMin = look.rectTransform.offsetMax = Vector2.zero;
        look.gameObject.AddComponent<TouchLookArea>();

        // Joystick (gauche).
        var area = UIFactory.MakeImage(layer, "JoystickArea", new Color(0f, 0f, 0f, 0f), true);
        area.rectTransform.anchorMin = Vector2.zero;
        area.rectTransform.anchorMax = new Vector2(0.4f, 0.75f);
        area.rectTransform.offsetMin = area.rectTransform.offsetMax = Vector2.zero;
        area.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var bg = UIFactory.MakeImage(area.transform, "Stick", new Color(1f, 1f, 1f, 0.2f), false, UIFactory.Circle);
        UIFactory.Place(bg.rectTransform, Vector2.zero, new Vector2(280f, 260f), new Vector2(260f, 260f));
        bg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var handle = UIFactory.MakeImage(bg.transform, "Handle", new Color(1f, 1f, 1f, 0.6f), false, UIFactory.Circle);
        UIFactory.Place(handle.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
        area.gameObject.AddComponent<VirtualJoystick>().Init(bg.rectTransform, handle.rectTransform, 120f);

        // Bouton Action (gros, en bas a droite).
        var action = UIFactory.MakeButton(layer, "Action", () => InputHub.Instance.QueueAction(), 38,
            new Color(0.85f, 0.45f, 0.15f, 0.85f), UIFactory.Circle);
        UIFactory.Place((RectTransform)action.transform, new Vector2(1f, 0f), new Vector2(-60f, 60f), new Vector2(240f, 240f));
        actionLabel = action.GetComponentInChildren<Text>();

        var jump = UIFactory.MakeButton(layer, "Saut", () => InputHub.Instance.QueueJump(), 30,
            new Color(0.2f, 0.4f, 0.75f, 0.8f), UIFactory.Circle);
        UIFactory.Place((RectTransform)jump.transform, new Vector2(1f, 0f), new Vector2(-330f, 50f), new Vector2(150f, 150f));

        var sprint = UIFactory.MakeImage(layer, "Sprint", new Color(0.75f, 0.7f, 0.2f, 0.8f), true, UIFactory.Circle);
        UIFactory.Place(sprint.rectTransform, new Vector2(1f, 0f), new Vector2(-110f, 330f), new Vector2(150f, 150f));
        UIFactory.Stretch(UIFactory.MakeText(sprint.transform, "Sprint", 28, TextAnchor.MiddleCenter).rectTransform);
        sprint.gameObject.AddComponent<HoldButton>().Held += held => InputHub.Instance.sprintHeld = held;
    }

    void Update()
    {
        if (!actionLabel || !player) return;
        var target = player.CurrentTarget;
        actionLabel.text = target != null ? target.Hint : "Action";
    }
}
