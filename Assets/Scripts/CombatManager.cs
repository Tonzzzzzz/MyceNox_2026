using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for sorting lists easily

public enum CombatState { Setup, PlayerTurn, EnemyTurn, Victory, Defeat }

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Runtime Data")]
    public CombatState State;
    private UnitController playerUnit;
    private List<UnitController> enemyUnits = new List<UnitController>();

    private void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Called by LevelManager after it spawns everything
    public void StartCombat()
    {
        // 1. Find the actors in the scene
        UnitController[] allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        
        // 2. Separate Player from Enemies
        // (We assume the player is the one with "Hero" in the name, or you can use Tags)
        enemyUnits.Clear();
        foreach(var unit in allUnits)
        {
            if (unit.name.Contains("Player")) // Or unit.CompareTag("Player")
            {
                playerUnit = unit;
            }
            else
            {
                enemyUnits.Add(unit);
            }
        }

        State = CombatState.Setup;
        StartCoroutine(BeginBattleRoutine());
    }

    private IEnumerator BeginBattleRoutine()
    {
        yield return new WaitForSeconds(0.5f); // Short delay for smooth transition

        // Default to Player Turn for now
        State = CombatState.PlayerTurn;
        Debug.Log("Combat Started! Player's Turn.");
    }

    // --- PLAYER INPUT ---
    
    // Connect this to your UI Button "Attack"
    public void OnPlayerAttackButton()
    {
        if (State != CombatState.PlayerTurn) return;

        StartCoroutine(PlayerAttackRoutine());
    }

    private IEnumerator PlayerAttackRoutine()
    {
        // 1. Find a target (First living enemy)
        // Note: We use .Stats (Capital S) here!
        UnitController target = enemyUnits.FirstOrDefault(e => !e.IsDead);
        
        if (target != null)
        {
            // Calculate Damage using the properties (Stats with Capital S)
            int damage = playerUnit.Stats.baseDamage;
            
            Debug.Log($"Player attacks {target.Stats.unitName} for {damage}!");
            
            // Animation delay
            yield return new WaitForSeconds(0.5f); 
            
            target.TakeDamage(damage);
        }

        yield return new WaitForSeconds(1f); // End of turn pause

        if (CheckWinCondition())
        {
            EndBattle(true);
        }
        else
        {
            State = CombatState.EnemyTurn;
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    // --- ENEMY AI ---

    private IEnumerator EnemyTurnRoutine()
    {
        Debug.Log("Enemy Turn Started...");
        
        foreach(var enemy in enemyUnits)
        {
            if (enemy.IsDead) continue;

            yield return new WaitForSeconds(1f); // AI Thinking time
            
            // AI Logic: Attack Player
            // Note: Using .Stats (Capital S)
            int damage = enemy.Stats.baseDamage;
            Debug.Log($"{enemy.Stats.unitName} attacks Player!");
            
            playerUnit.TakeDamage(damage);
            
            if (playerUnit.IsDead)
            {
                EndBattle(false);
                yield break;
            }
        }

        Debug.Log("Enemy Turn Ended. Back to Player.");
        State = CombatState.PlayerTurn;
    }

    // --- BATTLE END ---

    private bool CheckWinCondition()
    {
        // Returns true if all enemies are dead
        return enemyUnits.All(e => e.IsDead);
    }

    private void EndBattle(bool playerWon)
    {
        State = playerWon ? CombatState.Victory : CombatState.Defeat;
        Debug.Log(playerWon ? "VICTORY!" : "DEFEAT...");
    }
}