using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStats", menuName = "MyceNox/Unit Stats")]
public class UnitStatsSO : ScriptableObject
{
    public string unitName;
    public int maxHealth;
    public int baseDamage;
    public int speed; // Determines turn order
    
    [Header("Visuals")]
    public Sprite visual;      // Normal
    public Sprite hurtVisual;  // Hurt ( < 50% )
    public Sprite deadVisual;  // Dead ( 0% ) <--- ADD THIS
    [Range(0f, 1f)] public float hurtThreshold = 0.5f;
}