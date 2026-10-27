using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Partage : petites fonctions pour construire l'interface en code.
public static class UIFactory
{
    public static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.07f, 0.94f);
    public static readonly Color ButtonColor = new Color(0.18f, 0.22f, 0.2f, 0.9f);
    public static readonly Color AccentColor = new Color(0.25f, 0.55f, 0.3f, 0.95f);

    static Font font;
    static Sprite circle;

    public static Font Font => font ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
    public static Sprite Circle => circle ? circle : (circle = MakeCircle(128));

    public static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Place un element par rapport a un coin/bord de son parent (anchor = pivot).
    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image MakeImage(Transform parent, string name, Color color, bool raycast = false, Sprite sprite = null)
    {
        var img = MakeRect(name, parent).gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = raycast;
        return img;
    }

    public static Text MakeText(Transform parent, string content, int size, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var t = MakeRect("Text", parent).gameObject.AddComponent<Text>();
        t.font = Font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.text = content;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
        return t;
    }

    public static Button MakeButton(Transform parent, string label, UnityAction onClick, int fontSize = 34, Color? color = null, Sprite sprite = null)
    {
        var img = MakeImage(parent, label, color ?? ButtonColor, true, sprite);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        if (onClick != null) b.onClick.AddListener(onClick);
        Stretch(MakeText(img.transform, label, fontSize, TextAnchor.MiddleCenter).rectTransform);
        return b;
    }

    public static void SetLabel(Button b, string label) => b.GetComponentInChildren<Text>().text = label;

    // Panneau centre avec une colonne verticale (titre + contenu).
    public static RectTransform MakePanel(Transform parent, string name, Vector2 size)
    {
        var img = MakeImage(parent, name, PanelColor, true);
        var rt = Place(img.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        var col = img.gameObject.AddComponent<VerticalLayoutGroup>();
        col.padding = new RectOffset(30, 30, 24, 24);
        col.spacing = 12f;
        col.childControlWidth = true;
        col.childControlHeight = true;
        col.childForceExpandWidth = true;
        col.childForceExpandHeight = false;
        img.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    public static Text AddRowText(Transform column, string content, int size = 34, float height = 50f, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var t = MakeText(column, content, size, align);
        t.gameObject.AddComponent<LayoutElement>().minHeight = height;
        return t;
    }

    public static Button AddRowButton(Transform column, string label, UnityAction onClick, float height = 80f, Color? color = null)
    {
        var b = MakeButton(column, label, onClick, 34, color);
        b.gameObject.AddComponent<LayoutElement>().minHeight = height;
        return b;
    }

    // Ligne horizontale : texte a gauche + bouton a droite.
    public static Button AddRowWithButton(Transform column, string text, string buttonLabel, UnityAction onClick, out Text label)
    {
        var row = MakeRect("Row", column);
        row.gameObject.AddComponent<LayoutElement>().minHeight = 80f;
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 16f;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        label = MakeText(row, text, 32);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var b = MakeButton(row, buttonLabel, onClick, 30);
        b.gameObject.AddComponent<LayoutElement>().minWidth = 260f;
        return b;
    }

    public static void ClearChildren(Transform t, int keepFirst = 0)
    {
        for (int i = t.childCount - 1; i >= keepFirst; i--)
        {
            var child = t.GetChild(i).gameObject;
            child.SetActive(false);
            child.transform.SetParent(null, false);
            Object.Destroy(child);
        }
    }

    static Sprite MakeCircle(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
