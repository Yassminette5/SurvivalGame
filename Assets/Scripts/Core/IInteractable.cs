using UnityEngine;

// Partage : tout ce que le joueur peut utiliser avec le bouton Action ou une secousse
// (arbre, rocher, buisson, ennemi, eau...).
public interface IInteractable
{
    bool CanInteract { get; }
    // Plus la priorite est haute, plus la cible est choisie en premier (ennemi > ressource > eau).
    int Priority { get; }
    bool UsesEnergy { get; }
    string Hint { get; }
    Transform transform { get; }
    void Interact(PlayerActions player);
}
