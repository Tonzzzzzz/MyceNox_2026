using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Data Source")]
    public CardSO cardData; // The ScriptableObject holding the DNA

    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text speedCostText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private Image cardBackground; // To color-code physical vs subduing vs traps

    // We will use this later to determine if the card can be played based on current Speed
    public bool IsPlayable { get; private set; } 

    // Called when the card is drawn or instantiated
    public void InitializeCard(CardSO data)
    {
        cardData = data;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (cardData == null) return;

        nameText.text = cardData.cardName;
        descriptionText.text = cardData.description; // TMP will automatically parse your <link> tags here!
        speedCostText.text = cardData.speedCost.ToString();

        if (cardData.artwork != null)
        {
            artworkImage.sprite = cardData.artwork;
        }

        // Color coding the card based on type
        switch (cardData.cardType)
        {
            case CardType.Attack:
                cardBackground.color = new Color(0.8f, 0.2f, 0.2f); // Dark Red
                break;
            case CardType.Skill:
                cardBackground.color = new Color(0.2f, 0.6f, 0.8f); // Muted Blue
                break;
            case CardType.Trap:
                cardBackground.color = new Color(0.6f, 0.2f, 0.8f); // Purple
                break;
        }
    }

    // Called by the Hand Manager whenever the Player's Speed Pool changes
    public void CheckPlayability(int currentSpeedPool)
    {
        IsPlayable = currentSpeedPool >= cardData.speedCost;
        
        // Darken the card if we can't afford it
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = IsPlayable ? 1f : 0.5f;
            group.interactable = IsPlayable; // Prevents dragging if too expensive
        }
    }
}