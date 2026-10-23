using UnityEngine;

public class SD_AlienSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public SD_AlienEnemy greenPrefab;
    public SD_AlienEnemy redPrefab;
    public SD_AlienEnemy purplePrefab;

    [Header("Puntos de aparición")]
    public Transform[] spawnPoints;

    [Header("Trivia")]
    public SD_TriviaController triviaController;
    
    public bool IsConfigured()
    {
        if (greenPrefab == null ||
            redPrefab == null ||
            purplePrefab == null)
        {
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
            return false;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                return false;
        }

        return true;
    }

    public SD_AlienEnemy Spawn(
        SD_Player player,
        int greenWeight,
        int redWeight,
        int purpleWeight)
    {
        int totalWeight =
            greenWeight + redWeight + purpleWeight;

        if (totalWeight <= 0)
            return null;

        int randomValue = Random.Range(0, totalWeight);

        SD_AlienEnemy selectedPrefab;

        if (randomValue < greenWeight)
        {
            selectedPrefab = greenPrefab;
        }
        else if (randomValue < greenWeight + redWeight)
        {
            selectedPrefab = redPrefab;
        }
        else
        {
            selectedPrefab = purplePrefab;
        }

        Transform point =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        SD_AlienEnemy enemy = Instantiate(
            selectedPrefab,
            point.position,
            Quaternion.identity
        );

        enemy.Initialize(player);
        enemy.triviaController = triviaController;
        
        return enemy;
    }
}