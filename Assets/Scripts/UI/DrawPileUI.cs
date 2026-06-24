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

            // NEW: If an animation is playing, or the draw pile is empty but discards exist, FORCE the swap instantly!
            if (DeckManager.Instance.IsReshuffling || (DeckManager.Instance.GetDrawPileCount() == 0 && DeckManager.Instance.GetDiscardPileCount() > 0))
            {
                DeckManager.Instance.CompleteReshuffleInstantly();
            }

            // If it is STILL 0, it means the player has completely exhausted both piles!
            if (DeckManager.Instance.GetDrawPileCount() == 0)
            {
                CombatLogger.Instance?.Log("<color=red>No cards left in deck or discard!</color>");
                return;
            }

            // Standard Draw Logic
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