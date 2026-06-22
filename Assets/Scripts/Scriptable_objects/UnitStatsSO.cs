using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStats", menuName = "MyceNox/Unit Stats")]
public class UnitStatsSO : ScriptableObject
{
    [Header("Core Info")]
    public string unitName;
    public int maxHealth;
    
    [Header("RPG Stats (New!)")]
    public int baseStrength; 
    public int maxSpeedPool; // Example: 100. The total speed they have with zero weight.
    public int maxStamina; // Used in fights if a player wants to capture an enemy alive. If stamina is reduced to 0 the enemy faints. Player cannot be captured, so this stat is enemy only.
    public int maxArmor; // Used in fights if a player wants to capture an enemy alive. Any stamina damage is reduced from armor first if enemy has any, unless a card states otherwise.
    
    // We will leave Gear Weight out of here for now, as Gear should 
    // eventually be its own ScriptableObject that the unit "equips"!
    
    [Header("Visuals")]
    public Sprite visual;      
    public Sprite hurtVisual;  
    public Sprite deadVisual;  
    [Range(0f, 1f)] public float hurtThreshold = 0.5f;
}