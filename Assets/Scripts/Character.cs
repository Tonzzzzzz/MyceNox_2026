using UnityEngine;

public abstract class Character : MonoBehaviour
{
    public CharacterData data;
    [HideInInspector] public int currentHealth;

    protected virtual void Awake()
    {
        if (data != null)
            currentHealth = data.maxHealth;
    }

    public virtual void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{data.characterName} took {amount} damage. HP: {currentHealth}");
        if (currentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        Debug.Log($"{data.characterName} has fallen.");
        gameObject.SetActive(false);
    }
}