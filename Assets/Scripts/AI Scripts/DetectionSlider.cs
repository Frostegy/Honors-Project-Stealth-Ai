using UnityEngine;
using UnityEngine.UI;

public class DetectionMeterUI : MonoBehaviour
{
    [Header("References")]
    public AiAgentController enemy;
    public Slider detectionSlider;
    public Image sliderFillImage;

    [Header("Colours")]
    public Color normalColor = Color.green;
    public Color suspiciousColor = Color.yellow;
    public Color alertColor = Color.red;

    private void Update()
    {
        if (enemy == null) return;

        // update the slider every frame by reading directly from the enemy
        detectionSlider.value = enemy.currentDetectionLevel;

        // update the colour based on how detected the player is
        if (enemy.currentDetectionLevel < 0.25f)
            sliderFillImage.color = normalColor;
        else if (enemy.currentDetectionLevel < 1f)
            sliderFillImage.color = suspiciousColor;
        else
            sliderFillImage.color = alertColor;

        // these bools are only true for one frame so we can use them for one off things
        // like playing a sound or flashing the screen when the enemy spots you
        if (enemy.isFullyDetected)
            sliderFillImage.color = alertColor;

        if (enemy.hasLostDetection)
            sliderFillImage.color = normalColor;
    }
}
