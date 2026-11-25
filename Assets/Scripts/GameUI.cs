using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUI : MonoBehaviour
{
    private Label cropsLabel;
    private Label scoreLabel;
    private int totalCropsPlaced = 0;
    private int totalScore = 0;

  

    private VisualElement energyBarMask;



    void Awake()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        cropsLabel = root.Q<Label>("crop_text_label");
        scoreLabel = root.Q<Label>("Score_Text_Label");


        
        energyBarMask = root.Q<VisualElement>("energy-bar-mask");

        UpdateDisplay();
    }

    public void UpdateHealthBar(int health, int maxHealth)
    {
        if (energyBarMask != null)
        {
            float healthPercent = (float)health / maxHealth * 100f;
            energyBarMask.style.width = Length.Percent(healthPercent);
            Debug.Log("Percentage: " + healthPercent);
        }
    }


   


    public void UpdateCropsPlaced(int cropsPlaced)
    {
        totalCropsPlaced = cropsPlaced;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (cropsLabel != null)
            cropsLabel.text = "Crops placed: " + totalCropsPlaced;

        if (scoreLabel != null)
            scoreLabel.text = "Score: " + totalScore;
    }
    public void AddScore(int points)
    {
        totalScore += points;
        UpdateDisplay();
    }
    public void RemoveScore(int points)
    {
        totalScore -= points;

        if (totalScore < 0)
        {
            totalScore = 0;
        }

        UpdateDisplay();
    }
}
