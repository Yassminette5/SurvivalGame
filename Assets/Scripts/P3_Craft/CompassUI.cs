using UnityEngine;
using UnityEngine.UI;

// P3 - Boussole a l'ecran (visible une fois fabriquee et equipee) :
// l'aiguille rouge montre le vrai nord, la fleche verte la direction de l'abri.
public class CompassUI : MonoBehaviour
{
    public Inventory inventory;
    public Transform player;
    public Transform cameraTransform;

    RectTransform root;
    RectTransform northNeedle;
    RectTransform shelterArrow;
    Text info;

    void Start()
    {
        var hud = UIRoot.Layer(UILayer.Hud);
        var dial = UIFactory.MakeImage(hud, "Compass", new Color(0.1f, 0.12f, 0.1f, 0.75f), false, UIFactory.Circle);
        root = UIFactory.Place(dial.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -150f), new Vector2(220f, 220f));

        northNeedle = MakeNeedle(root, "N", new Color(0.9f, 0.2f, 0.2f), 95f);
        shelterArrow = MakeNeedle(root, "Abri", new Color(0.3f, 0.9f, 0.4f), 70f);

        info = UIFactory.MakeText(root, "", 24, TextAnchor.UpperCenter);
        UIFactory.Place(info.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -50f), new Vector2(320f, 50f));
        root.gameObject.SetActive(false);
    }

    static RectTransform MakeNeedle(RectTransform parent, string label, Color color, float length)
    {
        var pivot = UIFactory.MakeRect(label + "Needle", parent);
        UIFactory.Place(pivot, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bar = UIFactory.MakeImage(pivot, "Bar", color);
        bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bar.rectTransform.pivot = new Vector2(0.5f, 0f);
        bar.rectTransform.sizeDelta = new Vector2(10f, length);
        var t = UIFactory.MakeText(pivot, label, 26, TextAnchor.MiddleCenter);
        t.color = color;
        UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, length + 14f), new Vector2(100f, 34f));
        t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return pivot;
    }

    void Update()
    {
        bool show = inventory && inventory.CompassEquipped && GameManager.Instance && GameManager.Instance.IsPlaying;
        if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
        if (!show) return;

        // Le telephone regarde vers "Heading" : le nord est donc a -Heading par rapport au haut de l'ecran.
        var compass = MagnetometerCompass.Instance;
        float heading = compass ? compass.Heading : 0f;
        northNeedle.localEulerAngles = new Vector3(0f, 0f, heading);

        var build = BuildSystem.Instance;
        if (build && build.TryGetNearestShelter(player.position, out Vector3 shelter))
        {
            Vector3 to = shelter - player.position;
            to.y = 0f;
            Vector3 fwd = cameraTransform.forward;
            fwd.y = 0f;
            float angle = Vector3.SignedAngle(fwd, to, Vector3.up);
            shelterArrow.gameObject.SetActive(true);
            shelterArrow.localEulerAngles = new Vector3(0f, 0f, -angle);
            info.text = "Abri : " + Mathf.RoundToInt(to.magnitude) + " m";
        }
        else
        {
            shelterArrow.gameObject.SetActive(false);
            info.text = "Pas encore d'abri";
        }
    }
}
