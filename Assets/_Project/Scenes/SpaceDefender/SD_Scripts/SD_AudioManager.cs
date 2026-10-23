using UnityEngine;
using UnityEngine.SceneManagement;

public class SD_AudioManager : MonoBehaviour
{
    public static SD_AudioManager Instance { get; private set; }

    public enum Sound
    {
        AlienDamage,
        Correct,
        Damage,
        Defeat,
        Healing,
        Incorrect,
        Laser,
        NewRound,
        Reload,
        RoundComplete,
        Victory
    }

    [Header("Fuentes")]
    public AudioSource musicSource;
    public AudioSource effectsSource;

    [Header("Música")]
    public AudioClip mainSong;

    [Header("Efectos")]
    public AudioClip alienDamage;
    public AudioClip correct;
    public AudioClip damage;
    public AudioClip defeat;
    public AudioClip healing;
    public AudioClip incorrect;
    public AudioClip laser;
    public AudioClip newRound;
    public AudioClip reload;
    public AudioClip roundComplete;
    public AudioClip victory;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        UpdateMusic(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Limpiar efectos de la escena anterior.
        if (effectsSource != null)
            effectsSource.Stop();

        UpdateMusic(scene.name);
    }

    void UpdateMusic(string sceneName)
    {
        if (musicSource == null || mainSong == null)
            return;

        bool useMainSong =
            sceneName == "SD_MainMenu" ||
            sceneName == "SD_Instrucciones" ||
            sceneName == "SD_Gameplay";

        if (!useMainSong)
        {
            musicSource.Stop();
            return;
        }

        if (musicSource.clip != mainSong)
            musicSource.clip = mainSong;

        musicSource.loop = true;

        // No reiniciar si ya estaba sonando.
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void Play(Sound sound)
    {
        if (effectsSource == null)
            return;

        AudioClip clip = sound switch
        {
            Sound.AlienDamage => alienDamage,
            Sound.Correct => correct,
            Sound.Damage => damage,
            Sound.Defeat => defeat,
            Sound.Healing => healing,
            Sound.Incorrect => incorrect,
            Sound.Laser => laser,
            Sound.NewRound => newRound,
            Sound.Reload => reload,
            Sound.RoundComplete => roundComplete,
            Sound.Victory => victory,
            _ => null
        };

        if (clip != null)
            effectsSource.PlayOneShot(clip);
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }
}