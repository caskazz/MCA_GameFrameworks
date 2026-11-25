using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [SerializeField] private int maxEnergy = 10;
    private int currentEnergy;
    private GameUI gameUI;

    void Start()
    {
        currentEnergy = maxEnergy;
        gameUI = FindAnyObjectByType<GameUI>();

        if (gameUI != null)
            gameUI.UpdateHealthBar(currentEnergy, maxEnergy);
    }

    public void TakeDamage()
    {
        if (currentEnergy <= 0) return;

        currentEnergy--;

        if (gameUI != null)
            gameUI.UpdateHealthBar(currentEnergy, maxEnergy);

        Debug.Log("Health: " + currentEnergy);

        if (currentEnergy == 0)
        {
            Debug.Log("Player has no health left!");
        }
    }
}

