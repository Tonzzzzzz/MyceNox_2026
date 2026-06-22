using UnityEngine;

// Defines what happens to the card after it resolves
public enum CardDestination 
{ 
    Discard,        // Goes to the normal discard pile to be reshuffled later.
    Exhaust,        // Destroyed/Removed from the current battle entirely.
    ActiveField     // Stays on the board as a Trap/Aura until triggered or destroyed.
}

// Defines the element/nature of the attack for vulnerabilities and armor checks
public enum DamageType 
{ 
    Physical, 
    Subduing,       // Attacks Stamina instead of Health
    Fire, 
    Acid, 
    Electric 
    // Add more later as needed...
}

public enum CardType
{
    Attack,
    Skill,
    Trap
}

[CreateAssetMenu(fileName = "NewCard", menuName = "MyceNox/Card")]
public class CardSO : ScriptableObject
{
    [Header("Core Details")]
    public string cardName;
    public CardType cardType;
    public Sprite artwork;
    
    [Header("Resource Costs")]
    [Tooltip("Amount subtracted from the Actor's Speed Pool.")]
    public int speedCost;

    [Header("Combat Stats")]
    public DamageType damageType;
    public int baseDamage;
    public bool piercesArmor; // Useful for Acid or specific piercing attacks

    [Header("Resolution & Effects")]
    public CardDestination destinationAfterPlay = CardDestination.Discard;
    
    [TextArea(3, 5)]
    [Tooltip("Use TMP link tags for tooltips. e.g., Deals 10 <link=\"fire\">Fire</link> damage.")]
    public string description;
}