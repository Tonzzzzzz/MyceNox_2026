using UnityEngine;
using UnityEngine.EventSystems;

public class DrawPileUI : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (CombatManager.Instance.State == CombatState.Setup) return;

        if (CombatManager.Instance.State == CombatState.PlayerTurn)
        {
            UnitController player = CombatManager.Instance.PlayerUnit;

            // 1. If an animation is physically playing, interrupt and finish it.
            if (DeckManager.Instance.IsReshuffling)
            {
                DeckManager.Instance.CompleteReshuffleInstantly();
            }

            // 2. If the deck is empty...
            if (DeckManager.Instance.GetDrawPileCount() == 0)
            {
                // NEW: Check if the player has the "Draw Haste" ability!
                if (player != null && player.Stats.canReshuffleMidTurn && DeckManager.Instance.GetDiscardPileCount() > 0)
                {
                    CombatLogger.Instance?.Log("<color=magenta>Draw Haste Activated!</color>");
                    DeckManager.Instance.CheckForReshuffle();
                }
                else
                {
                    CombatLogger.Instance?.Log("<color=red>No cards left in draw pile!</color>");
                }
                return;
            }

            // 3. Standard Draw Logic
            if (player != null && player.CurrentDrawPoints > 0)
            {
                player.ConsumeDrawPoints(1); 
                DeckManager.Instance.DrawCards(1); 
                
                CombatLogger.Instance?.Log($"<color=#3498db>Drew a card. Draw Points remaining: {player.CurrentDrawPoints}</color>");
            }
            else
            {
                CombatLogger.Instance?.Log("<color=orange>Play Failed: Not enough Draw Points!</color>");
            }
        }
        else
        {
            CombatLogger.Instance?.Log("<color=orange>You can only draw on your turn!</color>");
        }
    }
}