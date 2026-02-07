using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStats", menuName = "MyceNox/Unit Stats")]
public class UnitStatsSO : ScriptableObject
{
    public string unitName;
    public int maxHealth;
    public int baseDamage;
    public int speed; // Determines turn order
    public Sprite visual;
}