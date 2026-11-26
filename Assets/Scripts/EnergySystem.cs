using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [SerializeField] private int maxEnergy = 10;
    private int currentEnergy;
    private GameUI gameUI;

    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;

    void Start()
    {
        currentEnergy = maxEnergy;
        gameUI = FindAnyObjectByType<GameUI>();

        if (gameUI != null)
            gameUI.UpdateEnergyBar(currentEnergy, maxEnergy);
    }

    public void TakeDamage()
    {
        TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (currentEnergy <= 0) return;

        currentEnergy = Mathf.Max(0, currentEnergy - amount);

        if (gameUI != null)
            gameUI.UpdateEnergyBar(currentEnergy, maxEnergy);

        Debug.Log("Health: " + currentEnergy);

        if (currentEnergy == 0)
        {
            Debug.Log("Player has no health left!");
        }
    }

    public void Recover(int amount)
    {
        if (amount <= 0) return;
        if (currentEnergy >= maxEnergy) return;

        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);

        if (gameUI != null)
            gameUI.UpdateEnergyBar(currentEnergy, maxEnergy);
    }
}