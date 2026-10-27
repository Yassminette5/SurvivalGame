using System;

public enum ItemType { None, Wood, Stone, Berry, Meat, Axe, Pickaxe, Sword, Compass }

[Serializable]
public struct ItemStack
{
    public ItemType type;
    public int count;

    public ItemStack(ItemType type, int count)
    {
        this.type = type;
        this.count = count;
    }
}

public class Recipe
{
    public ItemType result;
    public ItemStack[] cost;

    public Recipe(ItemType result, params ItemStack[] cost)
    {
        this.result = result;
        this.cost = cost;
    }
}

// P2 - Donnees de tous les objets du jeu (noms, valeurs, recettes).
public static class ItemDB
{
    public static readonly Recipe[] Recipes =
    {
        new Recipe(ItemType.Axe, new ItemStack(ItemType.Wood, 3), new ItemStack(ItemType.Stone, 2)),
        new Recipe(ItemType.Pickaxe, new ItemStack(ItemType.Wood, 3), new ItemStack(ItemType.Stone, 3)),
        new Recipe(ItemType.Sword, new ItemStack(ItemType.Wood, 2), new ItemStack(ItemType.Stone, 5)),
        new Recipe(ItemType.Compass, new ItemStack(ItemType.Wood, 1), new ItemStack(ItemType.Stone, 3)),
    };

    public static string Name(ItemType t)
    {
        switch (t)
        {
            case ItemType.Wood: return "Bois";
            case ItemType.Stone: return "Pierre";
            case ItemType.Berry: return "Baies";
            case ItemType.Meat: return "Viande";
            case ItemType.Axe: return "Hache";
            case ItemType.Pickaxe: return "Pioche";
            case ItemType.Sword: return "Epee";
            case ItemType.Compass: return "Boussole";
            default: return "Mains nues";
        }
    }

    public static bool IsTool(ItemType t) => t == ItemType.Axe || t == ItemType.Pickaxe || t == ItemType.Sword;

    public static float FoodValue(ItemType t)
    {
        switch (t)
        {
            case ItemType.Berry: return 15f;
            case ItemType.Meat: return 30f;
            default: return 0f;
        }
    }

    public static float Damage(ItemType equipped)
    {
        switch (equipped)
        {
            case ItemType.Sword: return 35f;
            case ItemType.Axe: return 18f;
            case ItemType.Pickaxe: return 15f;
            default: return 8f;
        }
    }

    public static string CostText(ItemStack[] cost)
    {
        var parts = new string[cost.Length];
        for (int i = 0; i < cost.Length; i++)
            parts[i] = cost[i].count + " " + Name(cost[i].type);
        return string.Join(", ", parts);
    }
}
