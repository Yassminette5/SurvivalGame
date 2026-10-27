using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Ordre d'affichage : les couches du bas recoivent les touches en dernier.
public enum UILayer { Controls, Hud, Panels, Menus, Overlay }

// Partage : un seul Canvas pour tout le jeu. Chaque module ajoute son UI dans une couche.
public class UIRoot : MonoBehaviour
{
    static UIRoot instance;

    RectTransform[] layers;
    RectTransform topBar;
    Image toastBg;
    Text toast;
    float toastUntil;

    static UIRoot Get()
    {
        if (instance) return instance;

        var go = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = 5;
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        instance = go.AddComponent<UIRoot>();
        instance.Build();
        return instance;
    }

    void Build()
    {
        var names = System.Enum.GetNames(typeof(UILayer));
        layers = new RectTransform[names.Length];
        for (int i = 0; i < names.Length; i++)
            layers[i] = UIFactory.Stretch(UIFactory.MakeRect(names[i], transform));

        // Barre de boutons en haut a droite (Sac, Fabriquer, Construire, Pause...).
        topBar = UIFactory.Place(UIFactory.MakeRect("TopBar", layers[(int)UILayer.Hud]),
            new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(1100f, 100f));
        var h = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.UpperRight;
        h.spacing = 14f;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        // Message temporaire : sous la barre de boutons, entre les barres de vie et la boussole, sur fond sombre
        // pour rester lisible meme au-dessus d'une fenetre ouverte.
        toastBg = UIFactory.MakeImage(layers[(int)UILayer.Overlay], "Toast", new Color(0f, 0f, 0f, 0.65f));
        UIFactory.Place(toastBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(ToastMaxWidth, 70f));
        toast = UIFactory.MakeText(toastBg.transform, "", 36, TextAnchor.MiddleCenter);
        var rt = UIFactory.Stretch(toast.rectTransform);
        rt.offsetMin = new Vector2(24f, 8f);
        rt.offsetMax = new Vector2(-24f, -8f);
        toastBg.gameObject.SetActive(false);
    }

    const float ToastMaxWidth = 840f;

    void Update()
    {
        if (toastBg.gameObject.activeSelf && Time.unscaledTime > toastUntil) toastBg.gameObject.SetActive(false);
    }

    public static RectTransform Layer(UILayer layer) => Get().layers[(int)layer];

    public static Button AddTopButton(string label, UnityAction onClick, float width = 200f)
    {
        var b = UIFactory.MakeButton(Get().topBar, label, onClick, 30);
        b.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
        return b;
    }

    public static void Toast(string message, float seconds = 2f)
    {
        var root = Get();
        root.toast.text = message;
        // Largeur ajustee au texte (retour a la ligne au-dela de ToastMaxWidth), puis hauteur selon le nombre de lignes.
        var bg = root.toastBg.rectTransform;
        bg.sizeDelta = new Vector2(Mathf.Min(root.toast.preferredWidth + 48f, ToastMaxWidth), bg.sizeDelta.y);
        bg.sizeDelta = new Vector2(bg.sizeDelta.x, root.toast.preferredHeight + 16f);
        root.toastBg.gameObject.SetActive(true);
        root.toastUntil = Time.unscaledTime + seconds;
    }
}
