using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Boton de ajustes (engrane) con panel de audio. Vive en el prefab Resources/AjustesOrbit
// y se pone solo en el menu y en los niveles, sin tener que editar cada escena.
public class AjustesOrbit : MonoBehaviour
{
    [Header("Referencias (las llena el prefab)")]
    public GameObject panel;
    public Image iconoAudio;
    public Sprite spriteAudioOn;
    public Sprite spriteAudioOff;
    public TMP_Text textoAudio;

    private const string ClaveAudio = "orbit_audio";
    private const string NombrePrefab = "AjustesOrbit";

    private float escalaPrevia = 1f;
    private bool abierto;

    // ---------- Audio (se guarda en el navegador) ----------

    public static bool AudioActivo
    {
        get
        {
            try { return PlayerPrefs.GetInt(ClaveAudio, 1) == 1; }
            catch (Exception) { return true; }
        }
    }

    private static void AplicarAudio()
    {
        AudioListener.volume = AudioActivo ? 1f : 0f;
    }

    // ---------- Arranque automatico en cada escena ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Arrancar()
    {
        SceneManager.sceneLoaded += (escena, modo) => Preparar(escena);
        Preparar(SceneManager.GetActiveScene());
    }

    private static void Preparar(Scene escena)
    {
        AplicarAudio();

        if (escena.name != "MainMenuOP" && !escena.name.StartsWith("Level"))
            return;

        if (FindFirstObjectByType<AjustesOrbit>() != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(NombrePrefab);
        if (prefab == null)
        {
            Debug.LogWarning("[AjustesOrbit] Falta el prefab Resources/" + NombrePrefab);
            return;
        }

        // Canvas propio por encima de todo lo demas
        GameObject objetoCanvas = new GameObject("AjustesCanvas");
        Canvas canvas = objetoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        objetoCanvas.AddComponent<GraphicRaycaster>();

        AsegurarEventSystem();
        Instantiate(prefab, objetoCanvas.transform, false);
    }

    // Si la escena no trae EventSystem los botones no responderian
    private static void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject objeto = new GameObject("EventSystem");
        objeto.AddComponent<EventSystem>();

        Type moduloNuevo = Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

        if (moduloNuevo != null)
            objeto.AddComponent(moduloNuevo);
        else
            objeto.AddComponent<StandaloneInputModule>();
    }

    // ---------- Panel ----------

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        RefrescarAudio();
    }

    public void Abrir()
    {
        if (abierto)
            return;

        abierto = true;
        escalaPrevia = Time.timeScale;
        Time.timeScale = 0f;
        panel.SetActive(true);
        RefrescarAudio();
    }

    public void Cerrar()
    {
        if (!abierto)
            return;

        abierto = false;
        panel.SetActive(false);
        Time.timeScale = escalaPrevia;
    }

    public void AlternarAudio()
    {
        try
        {
            PlayerPrefs.SetInt(ClaveAudio, AudioActivo ? 0 : 1);
            PlayerPrefs.Save();
        }
        catch (Exception) { }

        AplicarAudio();
        RefrescarAudio();
    }

    private void RefrescarAudio()
    {
        bool activo = AudioActivo;

        if (iconoAudio != null)
            iconoAudio.sprite = activo ? spriteAudioOn : spriteAudioOff;

        if (textoAudio != null)
            textoAudio.text = activo ? "Audio: activado" : "Audio: silenciado";
    }

    // Por si la escena se cambia con el panel abierto, no dejar el juego congelado
    private void OnDestroy()
    {
        if (abierto)
            Time.timeScale = escalaPrevia;
    }
}
