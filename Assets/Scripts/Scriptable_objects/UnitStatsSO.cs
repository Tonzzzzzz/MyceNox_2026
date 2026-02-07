using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStats", menuName = "MyceNox/Unit Stats")]
public class UnitStatsSO : ScriptableObject
{
    public string unitName;
    public int maxHealth;
    public int baseDamage;
    public int speed; // Determines turn order
    
    [Header("Visuals")]
    public Sprite visual;      // You already have this
    public Sprite hurtVisual;  // <--- ADD THIS
    [Range(0f, 1f)] public float hurtThreshold = 0.5f; // <--- ADD THIS (50% HP)
}