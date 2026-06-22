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

    public void StartCombat(UnitController player, List<UnitController> enemies)
    {
        playerUnit = player;
        enemyUnits = enemies;

        State = CombatState.Setup;
        StartCoroutine(BeginBattleRoutine());
    }

    private IEnumerator BeginBattleRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        State = CombatState.PlayerTurn;
        Debug.Log("Combat Started! Player's Turn. Draw your cards!");
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

    /// ////////////////////////////////////////////////////////////
    // --- CARD PLAY LOGIC ---
    /// ////////////////////////////////////////////////////////////
    
    public bool TryPlayCard(CardSO playedCard)
    {
        // 1. Safety Checks
        if (State != CombatState.PlayerTurn || isActing) 
        {
            Debug.LogWarning("Cannot play card: Not your turn or an animation is playing.");
            return false;
        }

        // 2. Resource Check (Do we have enough Speed?)
        if (playerUnit.CurrentSpeed < playedCard.speedCost)
        {
            return false; // Return false so the PlayZone snaps the card back
        }

        // 3. Target Selection (For now, auto-target the first alive enemy)
        UnitController target = enemyUnits.FirstOrDefault(e => !e.IsDead);
        if (target == null) return false;

        // 4. Execute the Card!
        StartCoroutine(ExecuteCardRoutine(playedCard, target));
        
        return true; // The card was successfully played
    }

    private IEnumerator ExecuteCardRoutine(CardSO card, UnitController target)
    {
        
        isActing = true;

        // A. Consume the Speed (This automatically updates the Stance via UnitController!)
        playerUnit.ConsumeSpeed(card.speedCost);

        // B. Engage the target (Flanking logic)
        playerUnit.Engage(target);

        // C. Visuals: Lunge forward
        yield return StartCoroutine(playerUnit.Visuals.PlayAttackAnimation(target.transform.position));

        // D. Apply Damage from the Card Data
        // Notice we are passing the baseDamage from the card, plus the Player's baseStrength!
        int totalDamage = card.baseDamage + playerUnit.Stats.baseStrength;
        
        bool targetDied = false;

        CombatLogger.Instance.Log($"{playerUnit.Stats.unitName} used <b>{card.cardName}</b> for {card.speedCost} Speed.");

        if (card.damageType == DamageType.Subduing)
        {
            targetDied = target.TakeStaminaDamage(totalDamage, card.piercesArmor);
        }

        // Check if it's a Subduing (Stamina) attack or a Lethal (Health) attack
        if (card.damageType == DamageType.Subduing)
        {
            targetDied = target.TakeStaminaDamage(totalDamage, card.piercesArmor);
        }
        else
        {
            targetDied = target.TakeDamage(totalDamage, playerUnit);
        }

        // E. Handle Target Visuals
        if (targetDied)
        {
            // If subduing, maybe use a different faint visual later, but this works for now
            yield return StartCoroutine(target.Visuals.PlayDeathAnimation(target.Stats.deadVisual));
            target.gameObject.SetActive(false);
            enemyUnits.Remove(target);
        }
        else
        {
            yield return StartCoroutine(target.Visuals.PlayHitAnimation());
        }

        // F. Check Win Condition
        if (CheckWinCondition())
        {
            EndBattle(true);
        }

        isActing = false;

        
        
    }
}