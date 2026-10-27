using System;
using UnityEngine;

// P4 - Vie, faim, soif et energie du joueur.
public class PlayerStats : MonoBehaviour
{
    public float maxHealth = 100f;
    public float maxHunger = 100f;
    public float maxThirst = 100f;
    public float maxEnergy = 100f;

    [Header("Par seconde")]
    public float hungerDecay = 0.2f;
    public float thirstDecay = 0.3f;
    public float starveDamage = 2f;
    public float healthRegen = 0.5f;
    public float shelterHealthRegen = 2f;
    public float energyRegen = 4f;

    [Header("Bonus podometre")]
    [Tooltip("Faim regagnee pour chaque pas reel")]
    public float hungerPerStep = 0.2f;

    public float Health { get; private set; }
    public float Hunger { get; private set; }
    public float Thirst { get; private set; }
    public float Energy { get; private set; }

    public bool IsDead => Health <= 0f;
    public bool Sheltered { get; set; }
    public bool NearFire { get; set; }

    public event Action Died;
    public event Action Damaged;

    float lastEnergyUse;

    void Awake() => ResetStats();

    void Start()
    {
        if (Pedometer.Instance) Pedometer.Instance.StepsTaken += OnRealSteps;
    }

    void OnDestroy()
    {
        if (Pedometer.Instance) Pedometer.Instance.StepsTaken -= OnRealSteps;
    }

    void Update()
    {
        if (IsDead || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        float dt = Time.deltaTime;
        float decay = Sheltered ? 0.5f : 1f;

        Hunger = Mathf.Max(0f, Hunger - hungerDecay * decay * dt);
        Thirst = Mathf.Max(0f, Thirst - thirstDecay * decay * dt);

        if (Hunger <= 0f || Thirst <= 0f)
            Damage(starveDamage * dt, false);
        else if (Hunger > 50f && Thirst > 50f)
            Heal((Sheltered || NearFire ? shelterHealthRegen : healthRegen) * dt);

        if (Time.time - lastEnergyUse > 1f)
            Energy = Mathf.Min(maxEnergy, Energy + energyRegen * dt);
    }

    void OnRealSteps(int steps)
    {
        if (IsDead) return;
        Eat(steps * hungerPerStep);
    }

    public bool TryUseEnergy(float amount)
    {
        if (Energy < amount) return false;
        Energy -= amount;
        lastEnergyUse = Time.time;
        return true;
    }

    public void Damage(float amount, bool notify = true)
    {
        if (IsDead) return;
        Health = Mathf.Max(0f, Health - amount);
        if (notify) Damaged?.Invoke();
        if (Health <= 0f) Died?.Invoke();
    }

    public void Heal(float amount) => Health = Mathf.Min(maxHealth, Health + amount);
    public void Eat(float amount) => Hunger = Mathf.Min(maxHunger, Hunger + amount);
    public void Drink(float amount) => Thirst = Mathf.Min(maxThirst, Thirst + amount);
    public void AddEnergy(float amount) => Energy = Mathf.Min(maxEnergy, Energy + amount);

    public void ResetStats()
    {
        Health = maxHealth;
        Hunger = maxHunger;
        Thirst = maxThirst;
        Energy = maxEnergy;
    }

    public void Load(float health, float hunger, float thirst, float energy)
    {
        Health = Mathf.Clamp(health, 1f, maxHealth);
        Hunger = Mathf.Clamp(hunger, 0f, maxHunger);
        Thirst = Mathf.Clamp(thirst, 0f, maxThirst);
        Energy = Mathf.Clamp(energy, 0f, maxEnergy);
    }
}
