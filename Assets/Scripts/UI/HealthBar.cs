using UnityEngine;
using UnityEngine.UI; // Required for Slider

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private UnitController unit;

    private void Awake()
    {
        // Fail-safe: Try to find the UnitController on the parent object
        if (unit == null) 
            unit = GetComponentInParent<UnitController>();
            
        if (slider == null)
            slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (unit != null)
        {
            // Subscribe to the event
            unit.OnHealthChanged += UpdateHealthBar;
        }
    }

    private void OnDisable()
    {
        if (unit != null)
        {
            // Unsubscribe to prevent errors when objects are destroyed
            unit.OnHealthChanged -= UpdateHealthBar;
        }
    }

    // This function runs automatically whenever the Unit takes damage/heals
    private void UpdateHealthBar(float percent)
    {
        if (slider != null)
        {
            slider.value = percent;
        }
    }
}