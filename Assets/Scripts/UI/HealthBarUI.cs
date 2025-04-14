using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
    
    public void UpdateHealthBar(float fillAmount)
    {
        if (foregroundImage != null)
        {
            foregroundImage.fillAmount = fillAmount;
        }
    }
} 