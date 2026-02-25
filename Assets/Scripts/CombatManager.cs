using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum CombatState { Setup, PlayerTurn, EnemyTurn, Victory, Defeat }

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Runtime Data")]
    public CombatState State; 
    private UnitController playerUnit;
    private List<UnitController> enemyUnits = new List<UnitController>();

    // NEW: Prevents the player from spam-clicking buttons while an animation is playing
    private bool isActing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void StartCombat()
    {
        UnitController[] allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        
        enemyUnits.Clear();
        foreach(var unit in allUnits)
        {
            if (unit.name.Contains("Player")) 
                playerUnit = unit;
            else 
                enemyUnits.Add(unit);
        }

        State = CombatState.Setup;
        StartCoroutine(BeginBattleRoutine());
    }

    private IEnumerator BeginBattleRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        State = CombatState.PlayerTurn;
        // Reset the player's Stance back to "Defending" at the start of their turn
        playerUnit.ResetTurn(); 
        Debug.Log("Combat Started! Player's Turn.");
    }

    // --- PLAYER INPUT ---

    public void OnPlayerAttackButton()
    {
        // Safety Check: Is it the player's turn? Are they already mid-attack?
        if (State != CombatState.PlayerTurn || isActing) return;

        UnitController target = enemyUnits.FirstOrDefault(e => !e.IsDead);

        if (target == null)
        {
            Debug.Log("No enemies left to attack!");
            return;
        }

        // Execute the action WITHOUT ending the turn
        StartCoroutine(PlayerActionRoutine(target, false));
    }

    // You can hook this up to a UI button later!
    public void OnPlayerPowerAttackButton() 
    {
        if (State != CombatState.PlayerTurn || isActing) return;
        UnitController target = enemyUnits.FirstOrDefault(e => !e.IsDead);
        if (target != null) StartCoroutine(PlayerActionRoutine(target, true));
    }

    private IEnumerator PlayerActionRoutine(UnitController target, bool isPowerAttack)
    {
        isActing = true;

        // DECOUPLED: We tell the UnitController to handle the entire attack sequence!
        yield return StartCoroutine(playerUnit.PerformAttack(target, isPowerAttack));

        // Clean up dead enemies from the tracking list
        enemyUnits.RemoveAll(e => e.IsDead);

        if (CheckWinCondition())
        {
            EndBattle(true);
        }
        
        isActing = false;
        // Notice we do NOT start the Enemy Turn here. The player can keep attacking!
    }

    // --- NEW: END TURN LOGIC ---

    // Hook this up to a new "End Turn" button in your UI
    public void OnEndTurnButton()
    {
        // Don't let them end turn if an animation is still playing
        if (State != CombatState.PlayerTurn || isActing) return;

        Debug.Log("Player ended their turn.");
        State = CombatState.EnemyTurn;
        StartCoroutine(EnemyTurnRoutine());
    }

    // --- ENEMY AI ---

    private IEnumerator EnemyTurnRoutine()
    {
        Debug.Log("Enemy Turn Started...");
        
        foreach(var enemy in enemyUnits)
        {
            if (enemy.IsDead) continue;

            // Reset enemy Stance so they get their dodge chances back
            enemy.ResetTurn();

            yield return new WaitForSeconds(1f); 
            
            // DECOUPLED: Enemy handles its own attack logic targeting the player
            yield return StartCoroutine(enemy.PerformAttack(playerUnit, false));

            if (playerUnit.IsDead)
            {
                EndBattle(false);
                yield break; // Stop loop immediately
            }
        }

        Debug.Log("Enemy Turn Ended. Back to Player.");
        StartPlayerTurn(); // Loop back to the player
    }

    // --- BATTLE END ---

    private bool CheckWinCondition()
    {
        return enemyUnits.Count == 0 || enemyUnits.All(e => e.IsDead);
    }

    private void EndBattle(bool playerWon)
    {
        State = playerWon ? CombatState.Victory : CombatState.Defeat;
        Debug.Log(playerWon ? "VICTORY!" : "DEFEAT...");
    }
}