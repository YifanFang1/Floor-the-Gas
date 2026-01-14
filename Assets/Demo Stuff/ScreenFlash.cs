using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    public Image flashImage;
    public float flashDuration = 0.1f;
    public Color flashColor = Color.white;

    private float timer = 0f;
    private bool flashing = false;
    private Color originalColor;

    void Awake()
    {
        if (flashImage != null)
        {
            originalColor = flashImage.color;
            // Force it invisible instantly
            flashImage.color = Color.clear;
        }
    }

    void Update()
    {
        if (flashing)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                flashing = false;
                if (flashImage != null)
                    flashImage.color = originalColor;
            }
        }
    }

    public void TriggerFlash()
    {
        if (flashImage == null) return;
        flashImage.color = flashColor;
        timer = flashDuration;
        flashing = true;
    }
}
