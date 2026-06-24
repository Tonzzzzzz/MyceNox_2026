using System.Collections.Generic;
using UnityEngine;

public enum EquipmentSlot { MainHand, OffHand, Torso, Accessory }

[CreateAssetMenu(fileName = "NewEquipment", menuName = "MyceNox/Equipment")]
public class EquipmentSO : ScriptableObject
{
    public string equipmentName;
    public EquipmentSlot slot;
    
    [Tooltip("Subtracts from the Unit's Speed Pool at the start of the turn.")]
    public int weightPenalty;

    [Header("Granted Cards")]
    [Tooltip("The cards added to the unit's deck when this is equipped.")]
    public List<CardSO> providedCards = new List<CardSO>();
}