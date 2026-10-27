using UnityEngine;
using UnityEngine.UI;

// P5 - Bouton "Construire", liste des pieces et barre Placer / Tourner / Annuler.
public class BuildUI : MonoBehaviour
{
    public BuildSystem build;
    public Inventory inventory;

    RectTransform panel;
    RectTransform placeBar;
    Text placeInfo;

    void Start()
    {
        UIRoot.AddTopButton("Construire", Toggle, 220f);

        panel = UIFactory.MakePanel(UIRoot.Layer(UILayer.Panels), "BuildPanel", new Vector2(1000f, 700f));
        panel.gameObject.SetActive(false);

        placeBar = UIFactory.Place(UIFactory.MakeRect("PlaceBar", UIRoot.Layer(UILayer.Panels)),
            new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(900f, 190f));
        placeInfo = UIFactory.MakeText(placeBar, "", 30, TextAnchor.MiddleCenter);
        UIFactory.Place(placeInfo.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 60f));
        AddBarButton("Placer", () => build.ConfirmPlacement(), 0, new Color(0.25f, 0.6f, 0.3f, 0.95f));
        AddBarButton("Tourner", build.Rotate, 1, UIFactory.ButtonColor);
        AddBarButton("Annuler", build.CancelPlacement, 2, new Color(0.6f, 0.2f, 0.2f, 0.95f));
        placeBar.gameObject.SetActive(false);

        inventory.Changed += Refresh;
    }

    void OnDestroy()
    {
        if (inventory) inventory.Changed -= Refresh;
    }

    void AddBarButton(string label, UnityEngine.Events.UnityAction onClick, int index, Color color)
    {
        var b = UIFactory.MakeButton(placeBar, label, onClick, 34, color);
        UIFactory.Place((RectTransform)b.transform, new Vector2(0f, 0f), new Vector2(index * 310f, 0f), new Vector2(280f, 110f));
    }

    void Toggle()
    {
        if (build.IsPlacing) build.CancelPlacement();
        panel.gameObject.SetActive(!panel.gameObject.activeSelf);
        Refresh();
    }

    void Refresh()
    {
        if (!panel.gameObject.activeSelf) return;
        UIFactory.ClearChildren(panel);
        UIFactory.AddRowText(panel, "Construire un abri", 48, 70f, TextAnchor.MiddleCenter);
        UIFactory.AddRowText(panel, "Un toit au-dessus de toi = a l'abri : les ennemis ne t'attaquent plus.", 26, 60f);

        foreach (BuildPieceType t in System.Enum.GetValues(typeof(BuildPieceType)))
        {
            var type = t;
            var cost = BuildPiece.Cost(t);
            var b = UIFactory.AddRowWithButton(panel, BuildPiece.Name(t) + "  -  " + ItemDB.CostText(cost), "Choisir", () => Choose(type), out _);
            b.interactable = inventory.Has(cost);
        }
        UIFactory.AddRowButton(panel, "Fermer", Toggle);
    }

    void Choose(BuildPieceType type)
    {
        panel.gameObject.SetActive(false);
        build.BeginPlacement(type);
    }

    void Update()
    {
        bool show = build.IsPlacing;
        if (placeBar.gameObject.activeSelf != show) placeBar.gameObject.SetActive(show);
        if (show)
            placeInfo.text = BuildPiece.Name(build.Current) + "  (" + ItemDB.CostText(BuildPiece.Cost(build.Current)) + ")";
    }
}
