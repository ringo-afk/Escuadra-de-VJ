using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicaJuego : MonoBehaviour
{
    [SerializeField] private ResultadoFinalUI resultado;
    [SerializeField] private AudioClip musica;
    [Range(0f, 1f)]
    [SerializeField] private float volumenMusica = 0.06f;
    // cuánto baja al mostrar el resultado, para que se oiga el jingle
    [Range(0f, 1f)]
    [SerializeField] private float factorAlResultado = 0.5f;
    [SerializeField] private float velocidadCambio = 0.1f;

    [Header("Otros temas (si faltan se queda el que está sonando)")]
    [SerializeField] private AudioClip musicaTension;
    [SerializeField] private AudioClip musicaResultado;

    [Header("Tensión")]
    [SerializeField] private Torre torre;
    [SerializeField] private JuegoManager juego;
    [SerializeField] private SonidosJuego sonidos;
    [SerializeField] private int nivelTension = 3;

    // dos fuentes para que un tema baje mientras el otro sube
    private readonly AudioSource[] fuentes = new AudioSource[2];
    private int actual;
    private float factor = 1f;
    private bool mostrandoResultado;
    private bool saliendo;

    private void Awake()
    {
        for (int i = 0; i < fuentes.Length; i++)
        {
            fuentes[i] = gameObject.AddComponent<AudioSource>();
            fuentes[i].loop = true;
            fuentes[i].playOnAwake = false;
            fuentes[i].volume = 0f;
        }
    }

    private void OnEnable()
    {
        resultado.AlMostrar += Bajar;
        resultado.AlJugarDeNuevo += Subir;
    }

    private void OnDisable()
    {
        resultado.AlMostrar -= Bajar;
        resultado.AlJugarDeNuevo -= Subir;
    }

    private void Start()
    {
        CambiarA(musica);
    }

    private void Update()
    {
        // mientras la torre se cae se deja el tema que estaba
        if (!mostrandoResultado && !saliendo && torre != null && !torre.Terminada)
        {
            CambiarA(HayTension() && musicaTension != null ? musicaTension : musica);
        }

        // unscaled para que los sliders también se oigan en la pausa
        float objetivo = saliendo ? 0f : volumenMusica * factor * OpcionesAudio.Musica;
        float paso = velocidadCambio * Time.unscaledDeltaTime;
        for (int i = 0; i < fuentes.Length; i++)
        {
            fuentes[i].volume = Mathf.MoveTowards(fuentes[i].volume, i == actual ? objetivo : 0f, paso);
            if (i != actual && fuentes[i].isPlaying && fuentes[i].volume <= 0f) fuentes[i].Stop();
        }
    }

    private bool HayTension()
    {
        if (juego != null && juego.Nivel >= nivelTension) return true;
        float umbral = sonidos != null ? sonidos.UmbralTension : 0.3f;
        return torre.Altura > 0 && torre.Estabilidad < umbral;
    }

    private void CambiarA(AudioClip clip)
    {
        if (clip == null || fuentes[actual].clip == clip) return;

        actual = 1 - actual;
        // si el tema se cambió hace poco la otra fuente lo sigue tocando y se aprovecha
        if (fuentes[actual].clip != clip || !fuentes[actual].isPlaying)
        {
            fuentes[actual].clip = clip;
            fuentes[actual].Play();
        }
    }

    private void Bajar()
    {
        mostrandoResultado = true;
        // con tema propio del resultado no hace falta bajarlo, entra desde cero
        factor = musicaResultado != null ? 1f : factorAlResultado;
        CambiarA(musicaResultado);
    }

    private void Subir()
    {
        mostrandoResultado = false;
        factor = 1f;
    }

    public void SalirA(string escena)
    {
        if (saliendo) return;
        saliendo = true;
        StartCoroutine(BajarYCargar(escena));
    }

    private IEnumerator BajarYCargar(string escena)
    {
        float limite = 1f;
        while ((fuentes[0].volume > 0.001f || fuentes[1].volume > 0.001f) && limite > 0f)
        {
            limite -= Time.unscaledDeltaTime;
            yield return null;
        }
        // por si se sale desde la pausa
        Time.timeScale = 1f;
        SceneManager.LoadScene(escena);
    }
}
