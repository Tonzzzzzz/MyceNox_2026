using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CombatLogger : MonoBehaviour
{
    public static CombatLogger Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Settings")]
    [SerializeField] private int maxLines = 15;

    private List<string> logLines = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        if (logText != null) logText.text = "Battle Commenced.\n";
    }

    // Call this from anywhere in your game to print to the screen AND the console
    public void Log(string message)
    {
        // 1. Log to the Unity Editor Console automatically!
        Debug.Log($"[Combat Log] {message}");

        // 2. Log to the UI for the Player
        if (logText == null || scrollRect == null) return;

        if (logLines.Count >= maxLines)
        {
            logLines.RemoveAt(0); 
        }
        
        logLines.Add(message);
        logText.text = string.Join("\n", logLines);
        
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}