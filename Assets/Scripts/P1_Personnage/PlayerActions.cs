using System;
using System.Collections;
using UnityEngine;

// P1 - Bouton Action : recolter (arbre, rocher, buisson), boire, ou attaquer (P4).
// La secousse du telephone (P4 - ShakeDetector) appelle aussi DoAction().
public class PlayerActions : MonoBehaviour
{
    public float reach = 2.2f;
    public float actionCooldown = 0.45f;
    public float actionEnergyCost = 3f;
    public Transform toolHolder;

    public Inventory Inventory { get; private set; }
    public PlayerStats Stats { get; private set; }
    public IInteractable CurrentTarget { get; private set; }

    public event Action Swung;

    readonly Collider[] buffer = new Collider[32];
    float lastAction = -10f;
    float nextScan;

    void Awake()
    {
        Inventory = GetComponent<Inventory>();
        Stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 0.15f;
            CurrentTarget = FindTarget();
        }
        if (InputHub.Instance && InputHub.Instance.ActionPressed) DoAction();
    }

    public void DoAction()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || Stats.IsDead) return;
        if (Time.time - lastAction < actionCooldown) return;
        lastAction = Time.time;

        var target = FindTarget();
        CurrentTarget = target;
        if (target != null)
        {
            if (target.UsesEnergy && !Stats.TryUseEnergy(actionEnergyCost))
            {
                UIRoot.Toast("Pas assez d'energie !");
                return;
            }
            Face(target.transform.position);
            target.Interact(this);
        }
        StartCoroutine(Swing());
    }

    IInteractable FindTarget()
    {
        Vector3 center = transform.position + Vector3.up + transform.forward * (reach * 0.5f);
        int n = Physics.OverlapSphereNonAlloc(center, reach, buffer, ~0, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var it = buffer[i].GetComponentInParent<IInteractable>();
            if (it == null || !it.CanInteract) continue;

            Vector3 to = it.transform.position - transform.position;
            to.y = 0f;
            float score = to.magnitude - it.Priority * 10f;
            if (to.sqrMagnitude > 0.01f && Vector3.Dot(transform.forward, to.normalized) < -0.2f) score += 3f;
            if (score < bestScore)
            {
                best = it;
                bestScore = score;
            }
        }
        return best;
    }

    void Face(Vector3 point)
    {
        Vector3 to = point - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
    }

    IEnumerator Swing()
    {
        Swung?.Invoke();
        if (!toolHolder) yield break;
        const float duration = 0.25f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            toolHolder.localRotation = Quaternion.Euler(Mathf.Sin(t / duration * Mathf.PI) * 80f, 0f, 0f);
            yield return null;
        }
        toolHolder.localRotation = Quaternion.identity;
    }
}
