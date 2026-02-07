using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEncounter", menuName = "MyceNox/Encounter")]
public class EncounterSO : ScriptableObject
{
    public List<UnitStatsSO> enemies;
    public GameObject environmentPrefab; // The background for this specific fight
}