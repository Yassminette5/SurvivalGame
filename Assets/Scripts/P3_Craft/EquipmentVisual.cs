using UnityEngine;

// P3 - Affiche l'outil equipe dans la main du joueur.
public class EquipmentVisual : MonoBehaviour
{
    public Inventory inventory;
    public Transform tool;
    public Renderer toolRenderer;

    void Start()
    {
        inventory.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory) inventory.Changed -= Refresh;
    }

    void Refresh()
    {
        var t = inventory.Equipped;
        tool.gameObject.SetActive(t != ItemType.None);

        switch (t)
        {
            case ItemType.Axe:
                tool.localScale = new Vector3(0.12f, 0.12f, 0.9f);
                toolRenderer.material.color = new Color(0.55f, 0.4f, 0.25f);
                break;
            case ItemType.Pickaxe:
                tool.localScale = new Vector3(0.12f, 0.12f, 0.9f);
                toolRenderer.material.color = new Color(0.45f, 0.45f, 0.5f);
                break;
            case ItemType.Sword:
                tool.localScale = new Vector3(0.08f, 0.15f, 1.2f);
                toolRenderer.material.color = new Color(0.85f, 0.85f, 0.9f);
                break;
        }
        tool.localPosition = new Vector3(0f, 0f, tool.localScale.z * 0.5f);
    }
}
