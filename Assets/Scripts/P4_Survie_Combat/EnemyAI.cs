using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// P4 - Ennemi controle par une IA (NavMesh) : il se promene, poursuit le joueur puis attaque.
// La nuit (P5) il voit plus loin, court plus vite et frappe plus fort.
// Il ne peut pas atteindre un joueur dans son abri (sous un toit) ou pres d'un feu de camp.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IInteractable
{
    public static readonly List<EnemyAI> All = new List<EnemyAI>();

    public float maxHealth = 60f;
    public float detectRange = 12f;
    public float attackRange = 1.8f;
    public float attackDamage = 8f;
    public float attackCooldown = 1.2f;
    public float walkSpeed = 1.8f;
    public float chaseSpeed = 3.4f;
    public float wanderRadius = 10f;
    public int meatDrop = 2;

    [Header("Bonus de nuit")]
    public float nightDetectMultiplier = 1.8f;
    public float nightSpeedMultiplier = 1.35f;
    public float nightDamageMultiplier = 1.5f;

    enum State { Wander, Chase, Attack, Dead }

    NavMeshAgent agent;
    Renderer[] renderers;
    Transform player;
    PlayerStats playerStats;
    State state;
    float nextAttack;
    float nextWander;
    float nextRepath;
    Vector3 home;

    public float Health { get; private set; }
    public bool IsDead => state == State.Dead;

    public bool CanInteract => !IsDead;
    public int Priority => 2;
    public bool UsesEnergy => true;
    public string Hint => "Attaquer";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        renderers = GetComponentsInChildren<Renderer>();
        Health = maxHealth;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        home = transform.position;
        var gm = GameManager.Instance;
        player = gm.player.transform;
        playerStats = gm.stats;
    }

    void Update()
    {
        if (IsDead || !agent.isOnNavMesh) return;

        bool night = DayNightCycle.Instance && DayNightCycle.Instance.IsNight;
        float dist = Vector3.Distance(transform.position, player.position);

        // Le jour, les ennemis trop loin disparaissent (le spawner en recree pres du joueur).
        if (!night && dist > 70f)
        {
            Destroy(gameObject);
            return;
        }

        bool safe = playerStats.IsDead || playerStats.Sheltered || playerStats.NearFire;
        float detect = detectRange * (night ? nightDetectMultiplier : 1f);

        if (safe) state = State.Wander;
        else if (dist <= attackRange) state = State.Attack;
        else if (dist <= detect || (state != State.Wander && dist <= detect * 1.4f)) state = State.Chase;
        else state = State.Wander;

        switch (state)
        {
            case State.Wander:
                agent.speed = walkSpeed;
                if (Time.time >= nextWander || (!agent.pathPending && agent.remainingDistance < 0.5f))
                {
                    nextWander = Time.time + Random.Range(4f, 8f);
                    Vector3 p = home + Random.insideUnitSphere * wanderRadius;
                    if (NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);
                }
                break;

            case State.Chase:
                agent.speed = chaseSpeed * (night ? nightSpeedMultiplier : 1f);
                if (Time.time >= nextRepath)
                {
                    nextRepath = Time.time + 0.2f;
                    agent.SetDestination(player.position);
                }
                break;

            case State.Attack:
                agent.ResetPath();
                Vector3 to = player.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 10f * Time.deltaTime);
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    playerStats.Damage(attackDamage * (night ? nightDamageMultiplier : 1f));
                }
                break;
        }
    }

    public void Interact(PlayerActions attacker) => TakeDamage(ItemDB.Damage(attacker.Inventory.Equipped));

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health -= amount;
        StartCoroutine(Flash());
        if (agent.isOnNavMesh) agent.Move(-transform.forward * 0.3f);
        if (Health <= 0f) Die();
    }

    public void SetHealth(float value) => Health = Mathf.Clamp(value, 1f, maxHealth);

    void Die()
    {
        state = State.Dead;
        agent.enabled = false;
        GameManager.Instance.inventory.Add(ItemType.Meat, meatDrop);
        UIRoot.Toast("Ennemi vaincu ! +" + meatDrop + " Viande");
        StartCoroutine(FallAndDestroy());
    }

    IEnumerator Flash()
    {
        foreach (var r in renderers) r.material.EnableKeyword("_EMISSION");
        foreach (var r in renderers) r.material.SetColor("_EmissionColor", Color.white * 0.8f);
        yield return new WaitForSeconds(0.1f);
        foreach (var r in renderers) if (r) r.material.SetColor("_EmissionColor", Color.black);
    }

    IEnumerator FallAndDestroy()
    {
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.Euler(-90f, 0f, 0f);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.Slerp(start, end, t / 0.4f);
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
