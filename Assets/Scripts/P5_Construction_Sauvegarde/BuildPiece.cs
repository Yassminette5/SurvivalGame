using UnityEngine;

public enum BuildPieceType { Floor, Wall, Doorway, Roof, Campfire }

// P5 - Morceau d'abri pose par le joueur (sol, mur, porte, toit, feu de camp).
public class BuildPiece : MonoBehaviour
{
    public BuildPieceType type;

    public static string Name(BuildPieceType t)
    {
        switch (t)
        {
            case BuildPieceType.Floor: return "Sol";
            case BuildPieceType.Wall: return "Mur";
            case BuildPieceType.Doorway: return "Mur avec porte";
            case BuildPieceType.Roof: return "Toit";
            default: return "Feu de camp";
        }
    }

    public static ItemStack[] Cost(BuildPieceType t)
    {
        switch (t)
        {
            case BuildPieceType.Floor: return new[] { new ItemStack(ItemType.Wood, 4) };
            case BuildPieceType.Wall: return new[] { new ItemStack(ItemType.Wood, 5) };
            case BuildPieceType.Doorway: return new[] { new ItemStack(ItemType.Wood, 4) };
            case BuildPieceType.Roof: return new[] { new ItemStack(ItemType.Wood, 4), new ItemStack(ItemType.Stone, 2) };
            default: return new[] { new ItemStack(ItemType.Wood, 5), new ItemStack(ItemType.Stone, 3) };
        }
    }
}
