using UnityEngine;
using TMPro; // Required for TextMesh Pro

public class UnitOverlayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitController unit;
    
    [Header("UI Elements")]
    [Tooltip("Drag your TextMeshPro element for the Stance here")]
    [SerializeField] private TMP_Text stanceText;
    
    [Tooltip("Drag your TextMeshPro element for the Engagement ('E') here")]
    [SerializeField] private TMP_Text engagedText;

    private void Awake()
    {
        // Fail-safe just like your HealthBar script
        if (unit == null) 
            unit = GetComponentInParent<UnitController>();
    }

    private void OnEnable()
    {
        if (unit != null)
        {
            // Subscribe to the state changes
            unit.OnStanceChanged += UpdateStanceText;
            unit.OnEngagementChanged += UpdateEngagementText;
        }
    }

    private void OnDisable()
    {
        if (unit != null)
        {
            // Unsubscribe to prevent memory leaks
            unit.OnStanceChanged -= UpdateStanceText;
            unit.OnEngagementChanged -= UpdateEngagementText;
        }
    }

    private void Start()
    {
        // Force an initial update so the texts are correct when the unit spawns
        if (unit != null)
        {
            UpdateStanceText(unit.CurrentStance);
            UpdateEngagementText();
        }
    }

    // --- UI UPDATE LOGIC ---

    private void UpdateStanceText(UnitStance newStance)
    {
        if (stanceText == null) return;

        // We can change colors and text based on how dangerous the stance is
        switch (newStance)
        {
            case UnitStance.Defending:
                stanceText.text = "Defending";
                stanceText.color = Color.green;
                break;
            case UnitStance.Acted:
                stanceText.text = "Acted";
                stanceText.color = Color.white;
                break;
            case UnitStance.Overextended:
                stanceText.text = "Overextended";
                stanceText.color = new Color(1f, 0.5f, 0f); // Orange
                break;
            case UnitStance.EXPOSED:
                stanceText.text = "EXPOSED";
                stanceText.color = Color.red;
                break;
            case UnitStance.Downed:
                stanceText.text = "DOWNED";
                stanceText.color = Color.grey;
                break;
        }
    }

    private void UpdateEngagementText()
    {
        if (engagedText == null) return;

        // If we are engaged with at least 1 unit, show the "E"
        if (unit.EngagedUnits.Count > 0)
        {
            engagedText.text = "E";
            engagedText.color = Color.red;
            engagedText.enabled = true; // Make it visible
        }
        else
        {
            // Hide the text completely if not engaged
            engagedText.enabled = false; 
        }
    }
}