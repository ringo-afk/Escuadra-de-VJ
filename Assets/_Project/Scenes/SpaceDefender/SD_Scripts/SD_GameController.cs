using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SD_GameController : MonoBehaviour
{
    [System.Serializable]
    public class WaveSettings
    {
        public string waveName;

        [Min(1)]
        public int totalEnemies;

        [Min(0)]
        public int greenWeight;

        [Min(0)]
        public int redWeight;

        [Min(0)]
        public int purpleWeight;
    }

    [Header("Referencias")]
    public SD_Player player;
    public SD_AlienSpawner spawner;
    public SD_TriviaController triviaController;

    [Header("Tiempos")]
    [Min(0.1f)]
    public float spawnInterval = 2f;

    [Min(0f)]
    public float initialDelay = 3f;

    [Min(0f)]
    public float timeBetweenWaves = 5f;

    [Header("Límite de aliens vivos")]
    [Min(1)]
    public int maxAliveEnemies = 10;

    [Header("Oleadas")]
    public WaveSettings[] waves =
    {
        new WaveSettings
        {
            waveName = "Primera oleada",
            totalEnemies = 15,
            greenWeight = 100,
            redWeight = 0,
            purpleWeight = 0
        },

        new WaveSettings
        {
            waveName = "Segunda oleada",
            totalEnemies = 20,
            greenWeight = 80,
            redWeight = 0,
            purpleWeight = 20
        },

        new WaveSettings
        {
            waveName = "Tercera oleada",
            totalEnemies = 30,
            greenWeight = 40,
            redWeight = 40,
            purpleWeight = 20
        },

        new WaveSettings
        {
            waveName = "Oleada final",
            totalEnemies = 40,
            greenWeight = 25,
            redWeight = 55,
            purpleWeight = 20
        }
    };

    [Header("Eventos")]
    public UnityEvent onVictory = new UnityEvent();
    public UnityEvent onDefeat = new UnityEvent();

    public int CurrentWave { get; private set; }
    public int SpawnedThisWave { get; private set; }
    public bool GameFinished { get; private set; }

    public int AliveEnemies
    {
        get
        {
            RemoveDeadEnemies();
            return activeEnemies.Count;
        }
    }

    private readonly List<SD_AlienEnemy> activeEnemies =
        new List<SD_AlienEnemy>();

    void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        StartCoroutine(RunWaves());
    }

    void Update()
    {
        if (GameFinished)
            return;

        if (player == null || !player.alive)
        {
            GameFinished = true;

            StopAllCoroutines();

            Debug.Log("Derrota.");
            onDefeat.Invoke();
        }
    }

    bool ValidateConfiguration()
    {
        if (player == null ||
            spawner == null ||
            triviaController == null ||
            !spawner.IsConfigured())
        {
            Debug.LogError(
                "Asigna el jugador, el spawner, los tres prefabs " +
                "y todos los puntos de aparición."
            );

            return false;
        }

        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("No hay oleadas configuradas.");
            return false;
        }

        foreach (WaveSettings wave in waves)
        {
            if (wave == null ||
                wave.totalEnemies <= 0 ||
                wave.greenWeight < 0 ||
                wave.redWeight < 0 ||
                wave.purpleWeight < 0 ||
                wave.greenWeight + wave.redWeight +
                wave.purpleWeight <= 0)
            {
                Debug.LogError(
                    "Cada oleada necesita un total positivo " +
                    "y probabilidades válidas."
                );

                return false;
            }
        }

        return true;
    }

    IEnumerator RunWaves()
    {
        // Permite preparar la partida y que Player ejecute Start().
        yield return new WaitForSeconds(
            Mathf.Max(0f, initialDelay)
        );

        for (int i = 0; i < waves.Length; i++)
        {
            if (player == null || !player.alive)
                yield break;

            WaveSettings wave = waves[i];

            CurrentWave = i + 1;
            SpawnedThisWave = 0;

            Debug.Log(
                $"{wave.waveName}: {wave.totalEnemies} aliens."
            );

            // Crear solamente el total establecido para esta oleada.
            while (SpawnedThisWave < wave.totalEnemies)
            {
                if (player == null || !player.alive)
                    yield break;

                if (triviaController.HasPendingQuestions)
                {
                    yield return null;
                    continue;
                }
                
                RemoveDeadEnemies();

                // Esperar espacio si hay demasiados aliens vivos.
                if (activeEnemies.Count >=
                    Mathf.Max(1, maxAliveEnemies))
                {
                    yield return null;
                    continue;
                }

                SD_AlienEnemy enemy = spawner.Spawn(
                    player,
                    wave.greenWeight,
                    wave.redWeight,
                    wave.purpleWeight
                );

                if (enemy == null)
                {
                    Debug.LogError(
                        "No se pudo crear el alien. " +
                        "La secuencia de oleadas se detuvo."
                    );

                    yield break;
                }

                activeEnemies.Add(enemy);
                SpawnedThisWave++;

                yield return new WaitForSeconds(
                    Mathf.Max(0.1f, spawnInterval)
                );
            }

            // Ya aparecieron todos: esperar a eliminar los restantes.
            while (AliveEnemies > 0 || triviaController.HasPendingQuestions)
            {
                if (player == null || !player.alive)
                    yield break;

                yield return null;
            }

            if (player == null || !player.alive)
                yield break;

            Debug.Log($"{wave.waveName} completada.");

            // Descanso antes de la siguiente, excepto tras la final.
            if (i < waves.Length - 1)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0f, timeBetweenWaves)
                );
            }
        }

        if (player == null || !player.alive)
            yield break;

        GameFinished = true;

        Debug.Log("¡Victoria! Completaste las cuatro oleadas.");
        onVictory.Invoke();
    }

    void RemoveDeadEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
    }
}