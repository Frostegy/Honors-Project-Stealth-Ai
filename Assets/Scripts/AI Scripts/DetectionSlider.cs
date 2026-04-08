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

    private void Update() // this is where we update the slider value and color based on the enemy's current detection level
    {
        if (enemy == null) 
        { 
            return;
        }
            
        detectionSlider.value = enemy.currentDetectionLevel;

       
        if (enemy.currentDetectionLevel < 0.25f)
        {
                sliderFillImage.color = normalColor;
        }  
        else if (enemy.currentDetectionLevel < 1f)
        {
            sliderFillImage.color = suspiciousColor;
        }
        else
        {
                sliderFillImage.color = alertColor;
        }
            
        if (enemy.isFullyDetected)
        {
                sliderFillImage.color = alertColor;
        }
            
        if (enemy.hasLostDetection)
        {
                sliderFillImage.color = normalColor;
        }
            
    }
}
