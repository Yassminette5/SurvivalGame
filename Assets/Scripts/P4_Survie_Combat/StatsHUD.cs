using UnityEngine;
using UnityEngine.UI;

// P4 - Barres de vie, faim, soif, energie + flash rouge quand le joueur est touche.
public class StatsHUD : MonoBehaviour
{
    public PlayerStats stats;

    RectTransform healthFill, hungerFill, thirstFill, energyFill;
    Text status;
    Image damageFlash;
    float flashAlpha;

    void Start()
    {
        var hud = UIRoot.Layer(UILayer.Hud);
        var box = UIFactory.Place(UIFactory.MakeRect("Stats", hud), new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(520f, 300f));

        healthFill = MakeBar(box, "Vie", new Color(0.85f, 0.2f, 0.2f), 0);
        hungerFill = MakeBar(box, "Faim", new Color(0.9f, 0.55f, 0.15f), 1);
        thirstFill = MakeBar(box, "Soif", new Color(0.2f, 0.55f, 0.95f), 2);
        energyFill = MakeBar(box, "Energie", new Color(0.95f, 0.85f, 0.2f), 3);

        status = UIFactory.MakeText(box, "", 26);
        UIFactory.Place(status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -250f), new Vector2(520f, 40f));

        damageFlash = UIFactory.MakeImage(UIRoot.Layer(UILayer.Overlay), "DamageFlash", new Color(0.8f, 0f, 0f, 0f));
        UIFactory.Stretch(damageFlash.rectTransform);

        stats.Damaged += () => flashAlpha = 0.35f;
    }

    static RectTransform MakeBar(RectTransform parent, string label, Color color, int row)
    {
        float y = -row * 60f;
        var t = UIFactory.MakeText(parent, label, 28);
        UIFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(140f, 44f));

        var bg = UIFactory.MakeImage(parent, label + "Bar", new Color(0f, 0f, 0f, 0.55f));
        UIFactory.Place(bg.rectTransform, new Vector2(0f, 1f), new Vector2(140f, y - 6f), new Vector2(360f, 32f));

        var fill = UIFactory.MakeImage(bg.transform, "Fill", color);
        var rt = UIFactory.Stretch(fill.rectTransform);
        rt.offsetMin = new Vector2(3f, 3f);
        rt.offsetMax = new Vector2(-3f, -3f);
        return rt;
    }

    static void SetFill(RectTransform fill, float value01) => fill.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);

    void Update()
    {
        SetFill(healthFill, stats.Health / stats.maxHealth);
        SetFill(hungerFill, stats.Hunger / stats.maxHunger);
        SetFill(thirstFill, stats.Thirst / stats.maxThirst);
        SetFill(energyFill, stats.Energy / stats.maxEnergy);

        string s = "";
        if (stats.Sheltered) s = "A l'abri";
        else if (stats.NearFire) s = "Pres du feu";
        if (stats.Hunger <= 0f) s += (s.Length > 0 ? " - " : "") + "Tu meurs de faim !";
        if (stats.Thirst <= 0f) s += (s.Length > 0 ? " - " : "") + "Tu meurs de soif !";
        status.text = s;

        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.Max(0f, flashAlpha - Time.unscaledDeltaTime);
            damageFlash.color = new Color(0.8f, 0f, 0f, flashAlpha);
        }
    }
}
