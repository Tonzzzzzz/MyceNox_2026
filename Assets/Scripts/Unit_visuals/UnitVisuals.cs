using System.Collections;
using UnityEngine;

public class UnitVisuals : MonoBehaviour
{
    // Store where we started so we can go back
    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    // This is a "Coroutine" (note the IEnumerator return type)
    // It allows us to use "yield return" to wait over time
    public IEnumerator PlayAttackAnimation(Vector3 targetPosition)
    {
        // 1. Lunge forward (Move halfway to the enemy quickly)
        Vector3 start = transform.position;
        Vector3 end = Vector3.Lerp(start, targetPosition, 0.5f); // Stop halfway
        
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        transform.position = end;

        // 2. WAIT for impact (The "Hit" moment)
        yield return new WaitForSeconds(0.1f); 

        // 3. Return to start (Slightly slower)
        elapsed = 0f;
        duration = 0.4f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(end, originalPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
    }

    public IEnumerator PlayHitAnimation()
    {
        // Simple "Flash Red" or "Shake"
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr.color;
        
        sr.color = Color.red;
        transform.position += new Vector3(0.1f, 0, 0); // Shake right
        yield return new WaitForSeconds(0.05f);
        
        transform.position -= new Vector3(0.2f, 0, 0); // Shake left
        yield return new WaitForSeconds(0.05f);
        
        transform.position = originalPosition; // Reset
        sr.color = originalColor;
    }
}