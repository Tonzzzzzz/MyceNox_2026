using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EncounterSO currentEncounter; 
    
    [Header("Templates")]
    [SerializeField] private GameObject unitBasePrefab; 

    [Header("Spawn Points")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

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
        playerController.Initialize(playerData); 

        // 3. Spawn Enemies
        List<UnitController> spawnedEnemies = new List<UnitController>();
        foreach (UnitStatsSO enemyData in currentEncounter.enemies)
        {
            GameObject enemyObj = Instantiate(unitBasePrefab, enemySpawnPoint.position, Quaternion.identity);
            UnitController enemyController = enemyObj.GetComponent<UnitController>();
            enemyController.Initialize(enemyData);
            
            spawnedEnemies.Add(enemyController);
        }

        // 4. Hand over control with exact references
        CombatManager.Instance.StartCombat(playerController, spawnedEnemies);
    }
}