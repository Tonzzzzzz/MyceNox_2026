using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Configuration")]
    // Instead of random logic here, we drag in a specific "Encounter" asset
    // e.g., "Rat Ambush" or "Goblin Camp"
    [SerializeField] private EncounterSO currentEncounter; 
    
    [Header("Templates")]
    // These are "Blank" prefabs containing just the UnitController script and UI
    [SerializeField] private GameObject unitBasePrefab; 

    [Header("Spawn Points")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    // We keep a reference to the Player's data (usually held in a GameState manager)
    [SerializeField] private UnitStatsSO playerData;

    private void Start()
    {
        SetupBattle();
    }

    private void SetupBattle()
    {
        // 1. Create the Environment
        if (currentEncounter.environmentPrefab != null)
        {
            Instantiate(currentEncounter.environmentPrefab, transform);
        }

        // 2. Spawn Player
        GameObject playerObj = Instantiate(unitBasePrefab, playerSpawnPoint.position, Quaternion.identity);
        UnitController playerController = playerObj.GetComponent<UnitController>();
        
        // INJECT DATA: "You are now the Player"
        playerController.Initialize(playerData); 
        playerObj.name = "Player_Hero";

        // 3. Spawn Enemy from the Encounter Data
        // (For now we just spawn the first one in the list to keep it simple)
        if (currentEncounter.enemies.Count > 0)
        {
            UnitStatsSO enemyData = currentEncounter.enemies[0];
            
            GameObject enemyObj = Instantiate(unitBasePrefab, enemySpawnPoint.position, Quaternion.identity);
            UnitController enemyController = enemyObj.GetComponent<UnitController>();
            
            // INJECT DATA: "You are now a Goblin"
            enemyController.Initialize(enemyData);
            enemyObj.name = enemyData.unitName;
        }

        // 4. Hand over control to CombatManager
        // We pass the references so CombatManager doesn't have to find them
        CombatManager.Instance.StartCombat();
    }
}