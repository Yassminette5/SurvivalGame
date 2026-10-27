using UnityEngine;

// P4 - Lac : s'approcher et appuyer sur Action pour boire.
public class WaterSource : MonoBehaviour, IInteractable
{
    public float thirstPerSip = 35f;

    public bool CanInteract => true;
    public int Priority => 0;
    public bool UsesEnergy => false;
    public string Hint => "Boire";

    public void Interact(PlayerActions player)
    {
        if (player.Stats.Thirst >= player.Stats.maxThirst - 1f)
        {
            UIRoot.Toast("Tu n'as pas soif.");
            return;
        }
        player.Stats.Drink(thirstPerSip);
        UIRoot.Toast("Glou glou ! +" + thirstPerSip + " soif", 1.2f);
    }
}
