using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Customer customerPrefab;
    [SerializeField] private Transform player;

    [Header("Store Layout")]
    [Tooltip("Where customers appear (the store entrance).")]
    [SerializeField] private Transform entrance;
    [Tooltip("Where customers walk to when they leave. Defaults to the entrance.")]
    [SerializeField] private Transform exitPoint;
    [Tooltip("Spots in front of the paintings where customers stop and look. Use each point's forward arrow to face the painting.")]
    [SerializeField] private Transform[] browsePoints;

    [Header("Timing")]
    [Tooltip("Random time (seconds) between customer arrivals.")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(10f, 25f);
    [SerializeField] private int maxCustomers = 4;
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.8f;

    private readonly List<Customer> activeCustomers = new List<Customer>();
    private float nextSpawnTime;

    void Start()
    {
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (Time.time < nextSpawnTime) return;

        ScheduleNextSpawn();

        if (activeCustomers.Count >= maxCustomers) return;
        if (Random.value > spawnChance) return;

        SpawnCustomer();
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
    }

    void SpawnCustomer()
    {
        if (customerPrefab == null || entrance == null || player == null) return;

        Customer customer = Instantiate(customerPrefab, entrance.position, entrance.rotation);
        customer.Initialize(player, browsePoints, exitPoint != null ? exitPoint : entrance);
        customer.Despawned += OnCustomerDespawned;
        activeCustomers.Add(customer);
    }

    void OnCustomerDespawned(Customer customer)
    {
        customer.Despawned -= OnCustomerDespawned;
        activeCustomers.Remove(customer);
    }
}