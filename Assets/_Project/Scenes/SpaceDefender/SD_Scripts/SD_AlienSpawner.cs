using System.Collections.Generic;
using UnityEngine;

public class SD_AlienSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public SD_Player player;
    public SD_AlienEnemy[] enemyPrefabs;
    public Transform[] spawnPoints;

    [Header("Spawn")]
    public float spawnInterval = 2f;

    public int maxEnemies = 10;

    private float nextSpawnTime;

    private readonly List<SD_AlienEnemy> activeEnemies =
        new List<SD_AlienEnemy>();

    void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);

        if (player == null || !player.alive)
            return;

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (activeEnemies.Count >= maxEnemies)
            return;

        if (Time.time < nextSpawnTime)
            return;

        SpawnEnemy();

        nextSpawnTime =
            Time.time + Mathf.Max(0.1f, spawnInterval);
    }

    void SpawnEnemy()
    {
        SD_AlienEnemy prefab =
            enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        Transform point =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (prefab == null || point == null)
            return;

        SD_AlienEnemy enemy = Instantiate(
            prefab,
            point.position,
            Quaternion.identity
        );

        enemy.Initialize(player);
        activeEnemies.Add(enemy);
    }
}