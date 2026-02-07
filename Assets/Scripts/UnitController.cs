using UnityEngine;
using System; // Required for C# Actions (Events)

[RequireComponent(typeof(SpriteRenderer))] // Ensures the GameObject has a renderer
public class UnitController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer visualRenderer;
    
    // PROPERTIES
    // We use Properties to protect data. Other scripts can READ Stats, but only this script can SET them.
    public UnitStatsSO Stats { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    // EVENTS
    // The "Observer Pattern". UI and Managers subscribe to these.
    // They decouple this script from the rest of the game.
    public event Action<float> OnHealthChanged; // Passes generic float (0.0 to 1.0) for health bars
    public event Action<UnitController> OnDeath; // Passes itself so the Manager knows WHO died
    public event Action OnDamaged; // Useful for playing a "Hurt" sound or shake

    private void Awake()
    {
        // Fail-safe: Find the renderer if we forgot to drag it in
        if (visualRenderer == null) 
            visualRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Called by the LevelManager/CombatManager to set up this unit's "DNA".
    /// </summary>
    public void Initialize(UnitStatsSO initStats)
    {
        Stats = initStats;
        CurrentHealth = Stats.maxHealth;

        // Apply the Visual Sprite from the data
        if (Stats.visual != null)
        {
            visualRenderer.sprite = Stats.visual;
        }

        // Set the GameObject name for easier debugging in Hierarchy
        gameObject.name = $"{Stats.unitName}_Actor";

        // Initialize UI (set bars to full)
        OnHealthChanged?.Invoke(1f);
    }

    /// <summary>
    /// The public method to apply damage.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (IsDead) return; // Can't kill what's already dead

        // Apply Damage
        CurrentHealth -= amount;
        
        Debug.Log($"<color=red>{Stats.unitName}</color> took {amount} damage. Remaining: {CurrentHealth}");

        // 1. Trigger "Hurt" effects
        OnDamaged?.Invoke();

        // 2. Update Health Bar
        float healthPercent = (float)CurrentHealth / Stats.maxHealth;
        OnHealthChanged?.Invoke(healthPercent);

        // 3. Check for Death
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        CurrentHealth += amount;
        
        // Clamp health so it doesn't exceed Max
        if (CurrentHealth > Stats.maxHealth) 
            CurrentHealth = Stats.maxHealth;

        float healthPercent = (float)CurrentHealth / Stats.maxHealth;
        OnHealthChanged?.Invoke(healthPercent);
        
        Debug.Log($"<color=green>{Stats.unitName}</color> healed {amount}.");
    }

    private void Die()
    {
        Debug.Log($"<color=grey>{Stats.unitName}</color> has died.");
        
        // Notify the CombatManager (and anyone else listening)
        OnDeath?.Invoke(this);

        // Optional: Play death animation here before disabling
        // For now, we just turn it off
        gameObject.SetActive(false); 
    }
}