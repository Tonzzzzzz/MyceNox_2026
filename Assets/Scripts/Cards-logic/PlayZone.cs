using UnityEngine;
using UnityEngine.EventSystems;

public class PlayZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // CRITICAL FIX: Use GetComponentInParent!
        // This ensures that even if the player clicks directly on the Artwork or Text,
        // the PlayZone will look up the chain and find the main DraggableCard script.
        DraggableCard droppedCard = eventData.pointerDrag.GetComponentInParent<DraggableCard>();
        
        if (droppedCard != null)
        {
            CardDisplay display = droppedCard.GetComponent<CardDisplay>();
            
            if (display != null && display.cardData != null)
            {
                // Ask the CombatManager if we can play this card
                bool cardPlayed = CombatManager.Instance.TryPlayCard(display.cardData);

                if (cardPlayed)
                {
                    Debug.Log($"<color=green>Successfully played: {display.cardData.cardName}</color>");
                    
                    // Lock it into the play zone visually
                    droppedCard.transform.SetParent(transform);
                    
                    if (cardPlayed)
                    {
                    Debug.Log($"<color=green>Successfully played: {display.cardData.cardName}</color>");
                    
                    // Lock it into the play zone visually
                    droppedCard.transform.SetParent(transform);
                    
                    // NEW: Tell the DeckManager to handle the discard logic and animation!
                    // We pass a 1-second delay so the card hovers on screen while the attack plays out.
                    DeckManager.Instance.DiscardCard(droppedCard, 1f); 
                    }
                }
                    else
                    {
                        Debug.Log("<color=orange>Play Failed: Not enough Speed or wrong turn!</color>");
                        droppedCard.ReturnToHand(); // Snap back to the layout group!
                    }
            }
        }
    }
}