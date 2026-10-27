using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceKind { Tree, Rock, Bush }

// P1 - Arbre (bois), rocher (pierre) ou buisson (baies). Repousse apres un moment.
public class ResourceNode : MonoBehaviour, IInteractable
{
    public static readonly List<ResourceNode> All = new List<ResourceNode>();

    [Tooltip("Identifiant unique pour la sauvegarde (donne par le SceneBuilder)")]
    public int id;
    public ResourceKind kind;
    public int hitsToDeplete = 4;
    public float respawnTime = 120f;
    public GameObject visual;

    int hitsLeft;
    float respawnAt = -1f;

    public bool IsAvailable => respawnAt < 0f;
    public float RemainingRespawn => IsAvailable ? 0f : Mathf.Max(0f, respawnAt - Time.time);

    public bool CanInteract => IsAvailable;
    public int Priority => 1;
    public bool UsesEnergy => kind != ResourceKind.Bush;
    public string Hint => kind == ResourceKind.Tree ? "Couper" : kind == ResourceKind.Rock ? "Miner" : "Cueillir";

    void Awake() => hitsLeft = hitsToDeplete;
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        if (!IsAvailable && Time.time >= respawnAt) ResetNode();
    }

    public void Interact(PlayerActions player)
    {
        if (!IsAvailable) return;
        var inv = player.Inventory;

        ItemType item;
        ItemType bestTool;
        switch (kind)
        {
            case ResourceKind.Tree: item = ItemType.Wood; bestTool = ItemType.Axe; break;
            case ResourceKind.Rock: item = ItemType.Stone; bestTool = ItemType.Pickaxe; break;
            default: item = ItemType.Berry; bestTool = ItemType.None; break;
        }

        bool goodTool = bestTool != ItemType.None && inv.Equipped == bestTool;
        int amount = kind == ResourceKind.Bush ? 2 : goodTool ? 3 : 1;
        inv.Add(item, amount);
        UIRoot.Toast("+" + amount + " " + ItemDB.Name(item), 1.2f);

        hitsLeft -= goodTool ? 2 : 1;
        if (hitsLeft <= 0) Deplete(respawnTime);
        else if (visual) StartCoroutine(Punch());
    }

    public void Deplete(float seconds)
    {
        respawnAt = Time.time + seconds;
        if (visual) visual.SetActive(false);
    }

    public void ResetNode()
    {
        respawnAt = -1f;
        hitsLeft = hitsToDeplete;
        if (visual) visual.SetActive(true);
    }

    IEnumerator Punch()
    {
        Vector3 baseScale = Vector3.one;
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            visual.transform.localScale = baseScale * (1f - Mathf.Sin(t / 0.2f * Mathf.PI) * 0.1f);
            yield return null;
        }
        visual.transform.localScale = baseScale;
    }
}
