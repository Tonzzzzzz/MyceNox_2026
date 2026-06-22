using System.Collections; // <--- MAKE SURE THIS IS HERE FOR COROUTINES
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DeckManager : MonoBehaviour
{
    // A Singleton so the PlayZone can easily tell the DeckManager to discard a card
    public static DeckManager Instance { get; private set; }

    [Header("Deck Data")]
    public List<CardSO> startingDeck; 
    
    private List<CardSO> drawPile = new List<CardSO>();
    private List<CardSO> discardPile = new List<CardSO>();

    [Header("UI References")]
    public Transform playerHandTransform;
    public GameObject cardPrefab; 
    
    // --- NEW DISCARD UI REFERENCES ---
    [Header("Pile UI")]
    public Transform discardPileTransform;
    public TMP_Text discardCountText;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        InitializeDeck();
        UpdateDiscardText(); // Ensure it starts at 0
        DrawCards(5); 
    }

    public void InitializeDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        
        // Copy the starting deck into the active draw pile
        drawPile.AddRange(startingDeck);
        ShuffleDeck();
    }

    public void ShuffleDeck()
    {
        // Standard Fisher-Yates shuffle algorithm
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
            if (drawPile.Count == 0)
            {
                // Reshuffle discard into draw pile if we run out
                if (discardPile.Count > 0)
                {
                    Debug.Log("Reshuffling discard pile...");
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    ShuffleDeck();
                }
                else
                {
                    Debug.LogWarning("No cards left to draw in the deck or discard pile!");
                    break;
                }
            }

            // Pop the top card data off the pile
            CardSO drawnCardData = drawPile[0];
            drawPile.RemoveAt(0);

            // 1. Instantiate the visual UI prefab into the hand
            GameObject newCard = Instantiate(cardPrefab, playerHandTransform);
            
            // 2. Inject the data into the display script
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.InitializeCard(drawnCardData);
            }
        }
    }

    // ... Keep InitializeDeck(), ShuffleDeck(), and DrawCards() exactly as they are ...

    // --- NEW DISCARD LOGIC & ANIMATION ---

    public void DiscardCard(DraggableCard cardUI, float delayBeforeDiscard = 1f)
    {
        // 1. Get the data and add it to our backend list
        CardDisplay display = cardUI.GetComponent<CardDisplay>();
        if (display != null && display.cardData != null)
        {
            discardPile.Add(display.cardData);
            UpdateDiscardText();
        }

        // 2. Start the visual animation
        StartCoroutine(AnimateToDiscardRoutine(cardUI.transform, delayBeforeDiscard));
    }

    private void UpdateDiscardText()
    {
        if (discardCountText != null)
        {
            discardCountText.text = discardPile.Count.ToString();
        }
    }

    private IEnumerator AnimateToDiscardRoutine(Transform cardTransform, float delay)
    {
        // Wait a moment so the player can see the card they just played
        yield return new WaitForSeconds(delay);

        if (cardTransform == null) yield break;

        Vector3 startPos = cardTransform.position;
        Vector3 startScale = cardTransform.localScale;
        
        // Fly towards the Discard Pile UI
        Vector3 targetPos = discardPileTransform.position;
        Vector3 targetScale = Vector3.zero; // Shrink to nothing as it goes in

        float duration = 0.4f; // Fast, snappy animation
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cardTransform == null) yield break; // Safety check

            float t = elapsed / duration;
            
            // Move and shrink simultaneously
            cardTransform.position = Vector3.Lerp(startPos, targetPos, t);
            cardTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy the UI object once it reaches the pile
        Destroy(cardTransform.gameObject);
    }
    
}