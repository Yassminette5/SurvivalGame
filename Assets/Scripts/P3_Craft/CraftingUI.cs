using UnityEngine;
using UnityEngine.UI;

// P3 - Fenetre "Fabriquer" : recettes (hache, pioche, epee, boussole) et equipement.
public class CraftingUI : MonoBehaviour
{
    public Inventory inventory;

    RectTransform panel;

    public bool IsOpen => panel && panel.gameObject.activeSelf;

    void Start()
    {
        UIRoot.AddTopButton("Fabriquer", Toggle, 210f);
        panel = UIFactory.MakePanel(UIRoot.Layer(UILayer.Panels), "CraftingPanel", new Vector2(1100f, 800f));
        panel.gameObject.SetActive(false);
        inventory.Changed += Refresh;
    }

    void OnDestroy()
    {
        if (inventory) inventory.Changed -= Refresh;
    }

    public void Toggle()
    {
        panel.gameObject.SetActive(!IsOpen);
        Refresh();
    }

    void Refresh()
    {
        if (!IsOpen) return;
        UIFactory.ClearChildren(panel);
        UIFactory.AddRowText(panel, "Fabrication", 48, 70f, TextAnchor.MiddleCenter);

        foreach (var recipe in ItemDB.Recipes)
        {
            var r = recipe;
            string text = ItemDB.Name(r.result) + "  -  " + ItemDB.CostText(r.cost);
            var b = UIFactory.AddRowWithButton(panel, text, "Fabriquer", () => Craft(r), out _);
            b.interactable = inventory.Has(r.cost);
        }

        UIFactory.AddRowText(panel, "Equipement", 40, 60f, TextAnchor.MiddleCenter);
        AddEquipRow(ItemType.None);
        AddEquipRow(ItemType.Axe);
        AddEquipRow(ItemType.Pickaxe);
        AddEquipRow(ItemType.Sword);

        if (inventory.Count(ItemType.Compass) > 0)
        {
            bool on = inventory.CompassEquipped;
            UIFactory.AddRowWithButton(panel, "Boussole" + (on ? "  (equipee)" : ""), on ? "Retirer" : "Equiper",
                () => inventory.SetCompass(!on), out _);
        }

        UIFactory.AddRowButton(panel, "Fermer", Toggle);
    }

    void AddEquipRow(ItemType tool)
    {
        if (tool != ItemType.None && inventory.Count(tool) == 0) return;
        bool equipped = inventory.Equipped == tool;
        string damage = "degats " + ItemDB.Damage(tool);
        var b = UIFactory.AddRowWithButton(panel, ItemDB.Name(tool) + "  (" + damage + ")",
            equipped ? "En main" : "Equiper", () => inventory.Equip(tool), out _);
        b.interactable = !equipped;
    }

    void Craft(Recipe r)
    {
        if (inventory.Craft(r)) UIRoot.Toast(ItemDB.Name(r.result) + " fabrique(e) !");
        else UIRoot.Toast("Il manque des ressources.");
    }
}
