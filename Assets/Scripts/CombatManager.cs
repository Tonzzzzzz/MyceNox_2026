using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 

public enum CombatState { Setup, PlayerTurn, EnemyTurn, Victory, Defeat }

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Runtime Data")]
    public CombatState State; // Capital 'S'
    private UnitController playerUnit;
    private List<UnitController> enemyUnits = new List<UnitController>();

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
        State = CombatState.PlayerTurn;
        Debug.Log("Combat Started! Player's Turn.");
    }

    // --- PLAYER INPUT ---

    public void OnPlayerAttackButton()
    {
        // 1. Safety Check: Is it actually the player's turn?
        if (State != CombatState.PlayerTurn) return;

        // 2. Target Selection: Find the first enemy who isn't dead
        UnitController target = enemyUnits.FirstOrDefault(e => !e.IsDead);

        if (target == null)
        {
            Debug.Log("No enemies left to attack!");
            return;
        }

        // 3. Start the "Movie Sequence"
        StartCoroutine(AttackSequence(target));
    }

    // The "Movie Sequence" handles Visuals AND Logic in order
    private IEnumerator AttackSequence(UnitController target)
    {
        // A. Lock Input
        State = CombatState.EnemyTurn; 

        // B. VISUAL: Player Runs to Enemy (Waits here until done)
        yield return StartCoroutine(playerUnit.Visuals.PlayAttackAnimation(target.transform.position));

        // --- IMPACT MOMENT ---

        // C. LOGIC: Apply Damage
        int damage = playerUnit.Stats.baseDamage; 
    
        target.TakeDamage(damage); // 1. Apply the damage
        bool isDead = target.IsDead; // 2. Check the status afterwards
        
        Debug.Log($"Player hit {target.name} for {damage} damage.");

        // D. VISUAL: Enemy Reacts (Waits here until done)
        yield return StartCoroutine(target.Visuals.PlayHitAnimation());

        // E. Check Win/Loss/Next Turn
        if (CheckWinCondition())
        {
            EndBattle(true);
        }
        else
        {
            // Wait a tiny bit before enemy starts acts
            yield return new WaitForSeconds(0.5f);
            
            // Pass the baton to the Enemy
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

            // 1. AI Thinking time
            yield return new WaitForSeconds(1f); 
            
            // 2. VISUAL: Enemy runs to Player
            yield return StartCoroutine(enemy.Visuals.PlayAttackAnimation(playerUnit.transform.position));

            // --- IMPACT MOMENT ---

            // 3. LOGIC: Deal Damage
            int damage = enemy.Stats.baseDamage;
            playerUnit.TakeDamage(damage);
            Debug.Log($"{enemy.name} hit Player for {damage}!");

            // 4. VISUAL: Player Reacts
            yield return StartCoroutine(playerUnit.Visuals.PlayHitAnimation());
            
            if (playerUnit.IsDead)
            {
                EndBattle(false);
                yield break; // Stop the loop immediately
            }
        }

        Debug.Log("Enemy Turn Ended. Back to Player.");
        State = CombatState.PlayerTurn;
    }

    // --- BATTLE END ---

    private bool CheckWinCondition()
    {
        return enemyUnits.All(e => e.IsDead);
    }

    private void EndBattle(bool playerWon)
    {
        State = playerWon ? CombatState.Victory : CombatState.Defeat;
        Debug.Log(playerWon ? "VICTORY!" : "DEFEAT...");
    }
}