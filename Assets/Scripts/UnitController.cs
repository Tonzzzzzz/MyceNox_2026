using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum UnitStance { Defending, Acted, Overextended, EXPOSED, Downed }

[RequireComponent(typeof(SpriteRenderer))]
public class UnitController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer visualRenderer;
    public UnitVisuals Visuals;

    /// /////////////////////////////////////////
    // PROPERTIES
    /// /////////////////////////////////////////
    public UnitStatsSO Stats { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    /// /////////////////////////////////////////
    // CAPTURE PROPERTIES
    /// /////////////////////////////////////////
    public int CurrentStamina { get; private set; }
    public int CurrentArmor { get; private set; }
    public bool IsFainted => CurrentStamina <= 0;

    public event Action<UnitController> OnFainted;

    /// /////////////////////////////////////////
    // SPEED & TACTICAL PROPERTIES (SMT SYSTEM)
    /// /////////////////////////////////////////
    public UnitStance CurrentStance { get; private set; } = UnitStance.Defending;
    public int CurrentSpeed { get; private set; } = 0;
    public List<UnitController> EngagedUnits { get; private set; } = new List<UnitController>();

    [Header("Temporary Gear Setup")]
    [Tooltip("Placeholder until we build the Gear system!")]
    public int EquippedGearWeight = 0; 

    /// /////////////////////////////////////////
    //  EVENTS
    /// /////////////////////////////////////////
    public event Action<float> OnHealthChanged; 
    public event Action<UnitController> OnDeath; 
    public event Action OnDamaged;
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
        
        // NEW: Initialize Capture Stats
        CurrentStamina = Stats.maxStamina;
        CurrentArmor = Stats.maxArmor;

        if (Stats.visual != null)
            visualRenderer.sprite = Stats.visual;

        gameObject.name = $"{Stats.unitName}_Actor";
        OnHealthChanged?.Invoke(1f);
        
        ResetTurn();
    }
    
    /// /////////////////////////////////////////
    //  SPEED & STANCE LOGIC
    /// /////////////////////////////////////////
    
    // NEW: Replaces the old RegisterAction()
    public void ConsumeSpeed(int amount)
    {
        CurrentSpeed -= amount;
        UpdateStance();
    }

    private void UpdateStance()
    {
        // If already Downed, stay Downed.
        if (CurrentStance == UnitStance.Downed) return;

        // Divide the max pool into 4 quarters
        float thresholdSize = Stats.maxSpeedPool / 4f;

        // Determine Stance based on remaining speed
        if (CurrentSpeed >= thresholdSize * 3) 
            CurrentStance = UnitStance.Defending;       // e.g., 75 to 100
        else if (CurrentSpeed >= thresholdSize * 2) 
            CurrentStance = UnitStance.Acted;           // e.g., 50 to 74
        else if (CurrentSpeed >= thresholdSize * 1) 
            CurrentStance = UnitStance.Overextended;    // e.g., 25 to 49
        else 
            CurrentStance = UnitStance.EXPOSED;         // e.g., 0 to 24 (or negative)
        
        OnStanceChanged?.Invoke(CurrentStance);
    }

    // NEW: Calculates Effective Weight and Starting Speed
    public void ResetTurn()
    {
        // 1. Calculate Effective Weight (Cannot drop below 0)
        int effectiveWeight = Mathf.Max(0, EquippedGearWeight - Stats.baseStrength);

        // 2. Set Turn Start Speed
        CurrentSpeed = Stats.maxSpeedPool - effectiveWeight;

        // 3. Immediately evaluate stance based on starting speed
        UpdateStance();
    }

    public void SetDownedState()
    {
        CurrentStance = UnitStance.Downed;
        OnStanceChanged?.Invoke(CurrentStance);
        Debug.Log($"<color=orange>{Stats.unitName} is DOWNED! They lose their next turn.</color>");
        CombatLogger.Instance.Log($"<color=orange>{Stats.unitName} is DOWNED! They lose their next turn.</color>");
    }

    /// /////////////////////////////////////////
    //  ENGAGEMENT LOGIC
    /// /////////////////////////////////////////

    public void Engage(UnitController target)
    {
        if (!EngagedUnits.Contains(target))
        {
            EngagedUnits.Add(target);
            OnEngagementChanged?.Invoke(); 
        }

        if (!target.EngagedUnits.Contains(this))
        {
            target.EngagedUnits.Add(this);
            target.OnEngagementChanged?.Invoke(); 
        }
    }

    /// /////////////////////////////////////////
    //  DAMAGE MATH ENGINE
    /// /////////////////////////////////////////

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
                CombatLogger.Instance.Log($"<color=cyan>{Stats.unitName} DODGED the attack!</color>");
                return false; 
            }
        }
        
        // 2. STANCE MULTIPLIERS
        float damageMultiplier = 1f;
        switch (CurrentStance)
        {
            case UnitStance.Defending: damageMultiplier = 0.5f; break; 
            case UnitStance.Acted: damageMultiplier = 1f; break;
            case UnitStance.Overextended: damageMultiplier = 1f; break;
            case UnitStance.EXPOSED: damageMultiplier = 3f; break; 
            case UnitStance.Downed: damageMultiplier = 3f; break;
        }

        // 3. FLANKING CALCULATION
        if (attacker != null && EngagedUnits.Count > 0 && !EngagedUnits.Contains(attacker))
        {
            Debug.Log($"<color=purple>{attacker.Stats.unitName} is FLANKING {Stats.unitName}!</color>");
            CombatLogger.Instance.Log($"<color=purple>{attacker.Stats.unitName} is FLANKING {Stats.unitName}!</color>");
            damageMultiplier += 0.5f; 
        }

        // 4. POWER ATTACK EXPLOIT CALCULATION
        if (isPowerAttack && CurrentStance == UnitStance.EXPOSED)
        {
            if (UnityEngine.Random.value <= 0.90f)
            {
                SetDownedState();
            }
        }

        // Calculate Final Damage
        int finalDamage = Mathf.RoundToInt(baseAmount * damageMultiplier);

        CurrentHealth -= finalDamage;
        Debug.Log($"<color=red>{Stats.unitName}</color> took {finalDamage} damage (Base: {baseAmount}). Remaining: {CurrentHealth}");
        CombatLogger.Instance.Log($"<color=red>{Stats.unitName}</color> took {finalDamage} damage (Base: {baseAmount}). Remaining: {CurrentHealth}");

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

    /// /////////////////////////////////////////
    // CAPTURE MATH ENGINE
    /// /////////////////////////////////////////
    public bool TakeStaminaDamage(int baseAmount, bool ignoresArmor = false)
    {
        // Don't do anything if they are already dead or captured
        if (IsDead || IsFainted) return true; 

        int damageToStamina = baseAmount;

        // 1. Armor Calculation
        if (!ignoresArmor && CurrentArmor > 0)
        {
            if (CurrentArmor >= damageToStamina)
            {
                // Armor absorbs everything
                CurrentArmor -= damageToStamina;
                Debug.Log($"<color=blue>{Stats.unitName}'s Armor absorbed the hit! Remaining Armor: {CurrentArmor}</color>");
                CombatLogger.Instance.Log($"<color=blue>{Stats.unitName}'s Armor absorbed the hit! Remaining Armor: {CurrentArmor}</color>");
                return false; 
            }
            else
            {
                // Armor breaks, remaining damage spills over to Stamina
                damageToStamina -= CurrentArmor;
                Debug.Log($"<color=blue>{Stats.unitName}'s Armor was destroyed!</color>");
                CombatLogger.Instance.Log($"<color=blue>{Stats.unitName}'s Armor was destroyed!</color>");
                CurrentArmor = 0;
            }
        }

        // 2. Apply Stamina Damage
        CurrentStamina -= damageToStamina;
        Debug.Log($"<color=yellow>{Stats.unitName}</color> took {damageToStamina} Stamina damage. Remaining: {CurrentStamina}");
        CombatLogger.Instance.Log($"<color=yellow>{Stats.unitName}</color> took {damageToStamina} Stamina damage. Remaining: {CurrentStamina}");

        // 3. Check for Faint / Capture
        if (CurrentStamina <= 0)
        {
            CurrentStamina = 0;
            Faint();
            return true; // Return TRUE (Yes, they fainted)
        }

        return false; // Return FALSE (No, they are still awake)
    }

    private void Faint()
    {
        Debug.Log($"{Stats.unitName} has fainted and is ready for capture!");
        CombatLogger.Instance.Log($"{Stats.unitName} has fainted and is ready for capture!");
        
        // Change sprite to the dead/fainted visual
        if (Stats.deadVisual != null)
        {
            Visuals.ChangeSprite(Stats.deadVisual);
        }

        OnFainted?.Invoke(this);
    }

    /// /////////////////////////////////////////
    //  ACTIONS.
    /// /////////////////////////////////////////

    public IEnumerator PerformAttack(UnitController target, bool isPowerAttack = false)
    {
        // Simulate a card cost since we don't have cards yet.
        int speedCost = isPowerAttack ? 50 : 25;
        ConsumeSpeed(speedCost);

        Engage(target);

        yield return StartCoroutine(Visuals.PlayAttackAnimation(target.transform.position));

        // OLD CODE CAUSING ERROR:
        // int damageToDeal = isPowerAttack ? Stats.baseDamage * 2 : Stats.baseDamage;
        
        // NEW CODE: Use baseStrength instead!
        int damageToDeal = isPowerAttack ? Stats.baseStrength * 2 : Stats.baseStrength;
        
        bool targetDied = target.TakeDamage(damageToDeal, this, isPowerAttack);

        // ... rest of the visual logic

        if (targetDied)
        {
            yield return StartCoroutine(target.Visuals.PlayDeathAnimation(target.Stats.deadVisual));
            target.gameObject.SetActive(false);
            OnDeath?.Invoke(target); 
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
        CombatLogger.Instance.Log($"<color=green>{Stats.unitName}</color> healed {amount}.");
    }

    private void Die()
    {
        Debug.Log($"{Stats.unitName} has died.");
        CombatLogger.Instance.Log($"{Stats.unitName} has died.");
        OnDeath?.Invoke(this);
    }
}