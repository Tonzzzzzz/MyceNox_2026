using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum UnitStance { Defending, Acted, Overextended, Exposed, Downed }

[RequireComponent(typeof(SpriteRenderer))]
public class UnitController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer visualRenderer;
    public UnitVisuals Visuals;

    // PROPERTIES
    public UnitStatsSO Stats { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    // NEW: PUSH-YOUR-LUCK & TACTICAL PROPERTIES
    public UnitStance CurrentStance { get; private set; } = UnitStance.Defending;
    public int ActionsTakenThisTurn { get; private set; } = 0;
    public List<UnitController> EngagedUnits { get; private set; } = new List<UnitController>();

    // EVENTS
    public event Action<float> OnHealthChanged; 
    public event Action<UnitController> OnDeath; 
    public event Action OnDamaged;
    // NEW: Notify UI when Stance changes (for your text overlay)
    public event Action<UnitStance> OnStanceChanged; 
    public event Action OnEngagementChanged;

    private void Awake()
    {
        Visuals = GetComponent<UnitVisuals>();
        if (visualRenderer == null) 
            visualRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(UnitStatsSO initStats)
    {
        Stats = initStats;
        CurrentHealth = Stats.maxHealth;

        if (Stats.visual != null)
            visualRenderer.sprite = Stats.visual;

        gameObject.name = $"{Stats.unitName}_Actor";
        OnHealthChanged?.Invoke(1f);
        
        // Reset state on spawn
        ResetTurn();
    }

    // --- NEW: ACTION & STANCE LOGIC ---

    public void RegisterAction()
    {
        ActionsTakenThisTurn++;
        UpdateStance();
    }

    private void UpdateStance()
    {
        // If we are already Downed, we stay Downed until healed/reset
        if (CurrentStance == UnitStance.Downed) return;

        switch (ActionsTakenThisTurn)
        {
            case 0: CurrentStance = UnitStance.Defending; break;
            case 1: CurrentStance = UnitStance.Acted; break;
            case 2: CurrentStance = UnitStance.Overextended; break;
            default: CurrentStance = UnitStance.Exposed; break; // 3 or more actions
        }
        
        OnStanceChanged?.Invoke(CurrentStance);
    }

    public void ResetTurn()
    {
        ActionsTakenThisTurn = 0;
        CurrentStance = UnitStance.Defending;
        OnStanceChanged?.Invoke(CurrentStance);
    }

    public void SetDownedState()
    {
        CurrentStance = UnitStance.Downed;
        OnStanceChanged?.Invoke(CurrentStance);
        Debug.Log($"<color=orange>{Stats.unitName} is DOWNED! They lose their next turn.</color>");
    }

    // --- NEW: ENGAGEMENT LOGIC ---

    public void Engage(UnitController target)
    {
        if (!EngagedUnits.Contains(target))
        {
            EngagedUnits.Add(target);
            OnEngagementChanged?.Invoke(); // NEW: Tell UI we engaged!
        }

        if (!target.EngagedUnits.Contains(this))
        {
            target.EngagedUnits.Add(this);
            target.OnEngagementChanged?.Invoke(); // NEW: Tell target's UI they were engaged!
        }
    }

    // --- UPDATED: DAMAGE MATH ENGINE ---

    // We now pass the attacker so we can check for flanking!
    public bool TakeDamage(int baseAmount, UnitController attacker, bool isPowerAttack = false)
    {
        if (IsDead) return true;

        // 1. DODGE CALCULATION
        if (CurrentStance == UnitStance.Defending)
        {
            float dodgeChance = 0.25f;
            if (UnityEngine.Random.value <= dodgeChance)
            {
                Debug.Log($"<color=cyan>{Stats.unitName} DODGED the attack!</color>");
                return false; // Survived, took 0 damage
            }
        }

        // 2. STANCE MULTIPLIERS
        float damageMultiplier = 1f;
        switch (CurrentStance)
        {
            case UnitStance.Defending: damageMultiplier = 0.5f; break; // -50% Damage
            case UnitStance.Acted: damageMultiplier = 1f; break;
            case UnitStance.Overextended: damageMultiplier = 1f; break;
            case UnitStance.Exposed: damageMultiplier = 3f; break; // +200% = 300% total
            case UnitStance.Downed: damageMultiplier = 3f; break;
        }

        // 3. FLANKING CALCULATION
        if (attacker != null && EngagedUnits.Count > 0 && !EngagedUnits.Contains(attacker))
        {
            Debug.Log($"<color=purple>{attacker.Stats.unitName} is FLANKING {Stats.unitName}!</color>");
            damageMultiplier += 0.5f; // Add +50% flanking bonus
        }

        // 4. POWER ATTACK EXPLOIT CALCULATION
        if (isPowerAttack && CurrentStance == UnitStance.Exposed)
        {
            // 90% chance to be Downed
            if (UnityEngine.Random.value <= 0.90f)
            {
                SetDownedState();
            }
        }

        // Calculate Final Damage
        int finalDamage = Mathf.RoundToInt(baseAmount * damageMultiplier);

        // Apply Damage (Keeping your original health & event logic)
        CurrentHealth -= finalDamage;
        Debug.Log($"<color=red>{Stats.unitName}</color> took {finalDamage} damage (Base: {baseAmount}). Remaining: {CurrentHealth}");

        OnDamaged?.Invoke();

        float healthPercent = (float)CurrentHealth / Stats.maxHealth;
        OnHealthChanged?.Invoke(healthPercent);

        if (CurrentHealth > 0 && healthPercent <= Stats.hurtThreshold && Stats.hurtVisual != null)
        {
            Visuals.ChangeSprite(Stats.hurtVisual);
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
            return true; 
        }

        return false; 
    }

    // --- NEW: SELF-CONTAINED ACTION ---

    public IEnumerator PerformAttack(UnitController target, bool isPowerAttack = false)
    {
        // 1. Register the action (Increases Exposedness)
        RegisterAction();

        // 2. Engage the target
        Engage(target);

        // 3. Play Visuals
        yield return StartCoroutine(Visuals.PlayAttackAnimation(target.transform.position));

        // 4. Apply Damage
        int damageToDeal = isPowerAttack ? Stats.baseDamage * 2 : Stats.baseDamage;
        bool targetDied = target.TakeDamage(damageToDeal, this, isPowerAttack);

        // 5. Handle Target Visuals
        if (targetDied)
        {
            yield return StartCoroutine(target.Visuals.PlayDeathAnimation(target.Stats.deadVisual));
            target.gameObject.SetActive(false);
            OnDeath?.Invoke(target); // Notify the manager that someone died
        }
        else
        {
            yield return StartCoroutine(target.Visuals.PlayHitAnimation());
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHealth += amount;
        if (CurrentHealth > Stats.maxHealth) CurrentHealth = Stats.maxHealth;
        
        float healthPercent = (float)CurrentHealth / Stats.maxHealth;
        OnHealthChanged?.Invoke(healthPercent);
        Debug.Log($"<color=green>{Stats.unitName}</color> healed {amount}.");
    }

    private void Die()
    {
        Debug.Log($"{Stats.unitName} has died.");
        // Notify any listeners (like CombatManager) that this unit is dead
        OnDeath?.Invoke(this);
    }
}