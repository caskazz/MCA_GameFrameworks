using System.Collections.Generic;
using UnityEngine;

public class ChildSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChildFollower childPrefab;
    [SerializeField] private Transform target;

    [Header("Spawn Locations")]
    [Tooltip("Optional. If empty, children spawn on a random ring around the target.")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnDistance = 15f;
    [SerializeField] private float maxSpawnDistance = 25f;

    [Header("Timing")]
    [Tooltip("Random time (seconds) between spawn attempts.")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(5f, 15f);
    [SerializeField] private int maxActiveChildren = 3;
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.7f;

    private readonly List<ChildFollower> activeChildren = new List<ChildFollower>();
    private float nextSpawnTime;

    void Start()
    {
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (Time.time < nextSpawnTime) return;

        ScheduleNextSpawn();

        if (activeChildren.Count >= maxActiveChildren) return;
        if (Random.value > spawnChance) return;

        SpawnChild();
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
    }

    void SpawnChild()
    {
        if (childPrefab == null || target == null) return;

        Vector3 position = GetSpawnPosition();
        ChildFollower child = Instantiate(childPrefab, position, Quaternion.identity);
        child.Initialize(target);
        child.Despawned += OnChildDespawned;
        activeChildren.Add(child);
    }

    Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;

        Vector2 circle = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 offset = new Vector3(circle.x, 0f, circle.y) * distance;
        return target.position + offset;
    }

    void OnChildDespawned(ChildFollower child)
    {
        child.Despawned -= OnChildDespawned;
        activeChildren.Remove(child);
    }
}