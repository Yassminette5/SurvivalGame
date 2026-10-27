using UnityEngine;
using UnityEngine.UI;

// P2 - Fenetre "Sac" : liste des objets, manger la nourriture.
public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public PlayerStats stats;

    RectTransform panel;
    Text sensorInfo;

    public bool IsOpen => panel && panel.gameObject.activeSelf;

    void Start()
    {
        UIRoot.AddTopButton("Sac", Toggle, 150f);

        panel = UIFactory.MakePanel(UIRoot.Layer(UILayer.Panels), "InventoryPanel", new Vector2(900f, 820f));
        panel.gameObject.SetActive(false);
        inventory.Changed += Refresh;
    }

    void OnDestroy()
    {
        if (inventory) inventory.Changed -= Refresh;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        panel.gameObject.SetActive(true);
        Refresh();
    }

    public void Close() => panel.gameObject.SetActive(false);

    void Refresh()
    {
        if (!IsOpen) return;
        UIFactory.ClearChildren(panel);

        UIFactory.AddRowText(panel, "Sac", 48, 70f, TextAnchor.MiddleCenter);

        bool empty = true;
        foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
        {
            int count = inventory.Count(t);
            if (t == ItemType.None || count == 0) continue;
            empty = false;

            string line = ItemDB.Name(t) + "  x" + count;
            if (ItemDB.FoodValue(t) > 0f)
            {
                var type = t;
                UIFactory.AddRowWithButton(panel, line, "Manger", () => Eat(type), out _);
            }
            else
            {
                if (t == inventory.Equipped) line += "   (en main)";
                UIFactory.AddRowText(panel, line);
            }
        }
        if (empty) UIFactory.AddRowText(panel, "Le sac est vide. Coupe des arbres et casse des rochers !", 30, 90f);

        var prox = GetComponent<ProximityInventoryToggle>();
        string hint = prox && prox.Available
            ? "Astuce : passe la main devant le haut du telephone pour ouvrir/fermer le sac."
            : "Capteur de proximite absent : utilise le bouton Sac (ou Tab).";
        UIFactory.AddRowText(panel, hint, 24, 70f);

        UIFactory.AddRowButton(panel, "Fermer", Close);
    }

    void Eat(ItemType food)
    {
        if (stats.Hunger >= stats.maxHunger - 1f)
        {
            UIRoot.Toast("Tu n'as pas faim.");
            return;
        }
        if (inventory.Remove(food))
        {
            stats.Eat(ItemDB.FoodValue(food));
            UIRoot.Toast("Miam ! +" + ItemDB.FoodValue(food) + " faim");
        }
    }
}
