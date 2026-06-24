using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    // Removed the hardcoded 'startingDeck' variable.

    private List<CardSO> drawPile = new List<CardSO>();
    private List<CardSO> discardPile = new List<CardSO>();

    [Header("UI References")]
    public Transform playerHandTransform;
    public GameObject cardPrefab; 
    
    [Header("Pile UI")]
    public Transform discardPileTransform;
    public TMP_Text discardCountText;
    
    // NEW: Draw Pile UI references
    public Transform drawPileTransform;
    public TMP_Text drawCountText;

    // --- NEW: RESHUFFLE TRACKING ---
    public bool IsReshuffling { get; private set; }
    private Coroutine reshuffleCoroutine;
    private List<GameObject> activeFakeCards = new List<GameObject>();
    private Vector3 originalDrawPilePos;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Remember the resting position of the draw pile for the shake animation
        if (drawPileTransform != null) originalDrawPilePos = drawPileTransform.localPosition;
    }
    

    // NEW: We no longer build the deck in Start(). 
    // We wait for CombatManager or LevelManager to pass us the Player's data.
    public void BuildPlayerDeck(UnitStatsSO playerStats)
    {
        drawPile.Clear();
        discardPile.Clear();
        
        // Loop through all equipped gear and add their cards to the draw pile
        foreach (EquipmentSO gear in playerStats.equippedGear)
        {
            if (gear != null && gear.providedCards.Count > 0)
            {
                drawPile.AddRange(gear.providedCards);
            }
        }

        ShuffleDeck();
        UpdatePileUI();
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            CardSO temp = drawPile[i];
            int randomIndex = Random.Range(i, drawPile.Count);
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // 1. If the deck is empty, we simply stop drawing. No automatic reshuffles!
            if (drawPile.Count == 0)
            {
                CombatLogger.Instance?.Log("<color=red>No cards left in draw pile!</color>");
                break;
            }

            CardSO drawnCardData = drawPile[0];
            drawPile.RemoveAt(0);

            GameObject newCard = Instantiate(cardPrefab, playerHandTransform);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerHandTransform.GetComponent<RectTransform>());

            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null) 
            {
                display.InitializeCard(drawnCardData);
                display.SetFaceUp(false);
            }

            StartCoroutine(AnimateDrawRoutine(newCard.transform, display));
        }

        UpdatePileUI();
    }

    public void DiscardCard(DraggableCard cardUI, float delayBeforeDiscard = 1f)
    {
        CardDisplay display = cardUI.GetComponent<CardDisplay>();
        if (display != null && display.cardData != null)
        {
            discardPile.Add(display.cardData);
        }

        // Pass the explicit position so it cannot snap
        StartCoroutine(AnimateToDiscardRoutine(cardUI.transform, cardUI.transform.position, cardUI.transform.localScale, delayBeforeDiscard));
    }

    // Consolidated UI updates into one method
    private void UpdatePileUI()
    {
        if (discardCountText != null) discardCountText.text = discardPile.Count.ToString();
        if (drawCountText != null) drawCountText.text = drawPile.Count.ToString();
    }

    private IEnumerator AnimateDrawRoutine(Transform cardTransform, CardDisplay display)
    {
        if (drawPileTransform == null || cardTransform == null) yield break;

        Vector3 finalPos = cardTransform.position;
        Vector3 finalScale = cardTransform.localScale;

        // Snap to Draw Pile, start face down
        cardTransform.position = drawPileTransform.position;
        
        float moveDuration = 0.15f; // Very fast flight
        float flipDuration = 0.1f;  // Very fast flip
        float elapsed = 0f;

        // Phase 1: Fly to hand and squish horizontally (Simulating a card turning sideways)
        while (elapsed < moveDuration)
        {
            if (cardTransform == null) yield break;
            
            float t = elapsed / moveDuration;
            cardTransform.position = Vector3.Lerp(drawPileTransform.position, finalPos, t);
            
            // Squish the X scale down to 0, keep Y and Z normal
            cardTransform.localScale = new Vector3(Mathf.Lerp(finalScale.x, 0f, t), finalScale.y, finalScale.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2: The exact moment the card is "sideways" (Scale X is 0), flip it face up!
        if (display != null) display.SetFaceUp(true);

        // Phase 3: Un-squish the card
        elapsed = 0f;
        while (elapsed < flipDuration)
        {
            if (cardTransform == null) yield break;

            float t = elapsed / flipDuration;
            cardTransform.localScale = new Vector3(Mathf.Lerp(0f, finalScale.x, t), finalScale.y, finalScale.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- THE DRAW CARD BUG FIX ---
        cardTransform.localScale = finalScale;

        if (playerHandTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerHandTransform.GetComponent<RectTransform>());
        }
    }

    // Notice the new Vector3 parameters in the signature!
    private IEnumerator AnimateToDiscardRoutine(Transform cardTransform, Vector3 startPos, Vector3 startScale, float delay)
    {
        if (cardTransform == null) yield break;

        // Force the card to stay locked in its starting position during the wait time
        cardTransform.position = startPos;
        cardTransform.localScale = startScale;

        yield return new WaitForSeconds(delay);

        if (cardTransform == null) yield break;

        Vector3 targetPos = discardPileTransform.position;
        Vector3 targetScale = Vector3.zero; 

        float duration = 0.4f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cardTransform == null) yield break; 

            float t = elapsed / duration;
            cardTransform.position = Vector3.Lerp(startPos, targetPos, t);
            cardTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        UpdatePileUI(); 
        Destroy(cardTransform.gameObject);
    }

    public int GetDrawPileCount() => drawPile.Count;
    public int GetDiscardPileCount() => discardPile.Count;

    public void CheckForReshuffle()
    {
        // If the draw pile is empty, and we have discards, start the visual flow!
        if (drawPile.Count == 0 && discardPile.Count > 0 && !IsReshuffling)
        {
            reshuffleCoroutine = StartCoroutine(ReshuffleRoutine());
        }
    }

    public void CompleteReshuffleInstantly()
    {
        if (reshuffleCoroutine != null) StopCoroutine(reshuffleCoroutine);
        
        IsReshuffling = false;

        // Clean up any fake flying cards currently on screen
        foreach (var card in activeFakeCards)
        {
            if (card != null) Destroy(card);
        }
        activeFakeCards.Clear();

        // Perform the instant backend data swap
        if (discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            ShuffleDeck();
            UpdatePileUI();
            
            // Fix any weird rotation/scale left over from the interrupted shake animation
            if (drawPileTransform != null) drawPileTransform.localPosition = originalDrawPilePos; 
        }
    }

    /// /////////////////////////////////////
    /// Reshuffling logic
    /// /////////////////////////////////////
    private IEnumerator ReshuffleRoutine()
    {
        IsReshuffling = true;
        CombatLogger.Instance?.Log("<color=yellow>Reshuffling discard pile...</color>");

        // Phase 1: "Flow" animation (Fly fake cards from Discard to Draw)
        int fakeCardsToFly = Mathf.Min(5, discardPile.Count);
        for (int i = 0; i < fakeCardsToFly; i++)
        {
            // Spawn a visual-only dummy card
            GameObject fakeCard = Instantiate(cardPrefab, discardPileTransform.position, Quaternion.identity, transform);
            
            // Rip out its logic scripts so it doesn't try to drag or register data
            Destroy(fakeCard.GetComponent<DraggableCard>());
            CardDisplay display = fakeCard.GetComponent<CardDisplay>();
            if (display != null) display.SetFaceUp(false); // Face down
            
            activeFakeCards.Add(fakeCard);

            // Fly it over
            StartCoroutine(FlyFakeCardRoutine(fakeCard.transform, discardPileTransform.position, drawPileTransform.position, 0.3f));
            yield return new WaitForSeconds(0.08f); // Machine-gun stagger effect
        }

        yield return new WaitForSeconds(0.3f); // Wait for the last card to land

        // Phase 2: Perform the actual backend data swap
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();
        UpdatePileUI();

        // Phase 3: "Shuffle" Shake Animation on the Draw Pile
        float shakeDuration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-5f, 5f);
            float offsetY = Random.Range(-5f, 5f);
            drawPileTransform.localPosition = originalDrawPilePos + new Vector3(offsetX, offsetY, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        drawPileTransform.localPosition = originalDrawPilePos;
        IsReshuffling = false;
        reshuffleCoroutine = null;
    }

    private IEnumerator FlyFakeCardRoutine(Transform card, Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (card == null) yield break;
            float t = elapsed / duration;
            card.position = Vector3.Lerp(start, end, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Destroy the fake card the moment it touches the pile
        if (card != null) Destroy(card.gameObject);
    }

    public void DiscardHand()
    {
        float staggerDelay = 0f;

        for (int i = playerHandTransform.childCount - 1; i >= 0; i--)
        {
            DraggableCard cardUI = playerHandTransform.GetChild(i).GetComponent<DraggableCard>();
            if (cardUI != null)
            {
                CardDisplay display = cardUI.GetComponent<CardDisplay>();
                
                if (display != null && display.cardData != null && !display.cardData.retainInHand)
                {
                    discardPile.Add(display.cardData);
                    
                    // 1. THE BULLETPROOF UI TRICK: Tell the layout group to ignore this card instantly!
                    LayoutElement layoutElement = cardUI.gameObject.GetComponent<LayoutElement>();
                    if (layoutElement == null) layoutElement = cardUI.gameObject.AddComponent<LayoutElement>();
                    layoutElement.ignoreLayout = true;
                    
                    // 2. Strip drag logic so the player can't grab it while it's flying
                    Destroy(cardUI); 
                    CanvasGroup cg = display.GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = false;

                    // 3. Animate the REAL card to the discard pile (it is no longer confined by the layout!)
                    StartCoroutine(AnimateToDiscardRoutine(display.transform, display.transform.position, display.transform.localScale, staggerDelay));
                    
                    staggerDelay += 0.1f; 
                }
            }
        }
        
        // Force the layout group to instantly collapse the gaps left by the ignored cards
        if (playerHandTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerHandTransform.GetComponent<RectTransform>());
        }
    }
}