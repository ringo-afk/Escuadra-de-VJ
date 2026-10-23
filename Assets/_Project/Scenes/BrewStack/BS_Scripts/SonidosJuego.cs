using UnityEngine;

public class SonidosJuego : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Torre torre;
    [SerializeField] private TriviaUI trivia;
    [SerializeField] private ResultadoFinalUI resultado;

    [Header("Clips (si están vacíos se generan por código)")]
    [SerializeField] private AudioClip clipAcierto;
    [SerializeField] private AudioClip clipError;
    [SerializeField] private AudioClip clipGolpe;
    [SerializeField] private AudioClip clipPerfecto;
    [SerializeField] private AudioClip clipTension;
    [SerializeField] private AudioClip clipJingle;

    [Header("Volumen")]
    [Range(0f, 1f)]
    [SerializeField] private float volumenGeneral = 0.8f;

    [Header("Combo de perfectos")]
    [SerializeField] private float pasoTono = 0.08f;
    [SerializeField] private int maxPasos = 8;

    [Header("Tensión")]
    [Range(0f, 1f)]
    [SerializeField] private float umbralTension = 0.3f;

    private const int FRECUENCIA = 22050;

    private AudioSource fuenteEfectos;
    private AudioSource fuentePerfecto;
    private AudioSource fuenteTension;
    private int combo;
    private bool tensionArmada = true;

    private void Awake()
    {
        // fuentes separadas: el perfecto cambia de tono y la tensión se tiene que poder parar
        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuentePerfecto = gameObject.AddComponent<AudioSource>();
        fuenteTension = gameObject.AddComponent<AudioSource>();
        foreach (AudioSource f in new[] { fuenteEfectos, fuentePerfecto, fuenteTension })
        {
            f.playOnAwake = false;
            f.loop = false;
        }

        if (clipAcierto == null) clipAcierto = GenerarAcierto();
        if (clipError == null) clipError = GenerarError();
        if (clipGolpe == null) clipGolpe = GenerarGolpe();
        if (clipPerfecto == null) clipPerfecto = GenerarPerfecto();
        if (clipTension == null) clipTension = GenerarTension();
        if (clipJingle == null) clipJingle = GenerarJingle();
    }

    private void OnEnable()
    {
        torre.AlAterrizar += Aterrizo;
        trivia.AlMarcarRespuesta += Respondio;
        resultado.AlMostrar += MostroResultado;
    }

    private void OnDisable()
    {
        torre.AlAterrizar -= Aterrizo;
        trivia.AlMarcarRespuesta -= Respondio;
        resultado.AlMostrar -= MostroResultado;
    }

    private void Update()
    {
        bool enPeligro = !torre.Terminada && torre.Altura > 0 && torre.Estabilidad < umbralTension;

        if (enPeligro && tensionArmada)
        {
            // suena una vez al cruzar el umbral, no en bucle
            tensionArmada = false;
            fuenteTension.clip = clipTension;
            fuenteTension.volume = volumenGeneral;
            fuenteTension.Play();
        }
        else if (!enPeligro)
        {
            if (fuenteTension.isPlaying) fuenteTension.Stop();
            tensionArmada = true;
        }
    }

    private void Aterrizo(Torre.Resultado res, Torre.Calidad calidad, Bloque bloque)
    {
        fuenteEfectos.PlayOneShot(clipGolpe, volumenGeneral);

        if (calidad == Torre.Calidad.Perfecto)
        {
            fuentePerfecto.pitch = 1f + pasoTono * Mathf.Min(combo, maxPasos);
            fuentePerfecto.PlayOneShot(clipPerfecto, volumenGeneral);
            combo++;
        }
        else
        {
            combo = 0;
        }
    }

    private void Respondio(bool acierto)
    {
        fuenteEfectos.PlayOneShot(acierto ? clipAcierto : clipError, volumenGeneral);
    }

    private void MostroResultado()
    {
        combo = 0;
        fuenteTension.Stop();
        fuenteEfectos.PlayOneShot(clipJingle, volumenGeneral);
    }

    // sonidos de reemplazo, sencillos pero suficientes para probar

    private AudioClip GenerarAcierto()
    {
        float[] a = Tono(880f, 880f, 0.08f, 0.5f);
        float[] b = Tono(1320f, 1320f, 0.14f, 0.5f);
        return CrearClip("acierto", Unir(a, b));
    }

    private AudioClip GenerarError()
    {
        return CrearClip("error", Tono(240f, 140f, 0.28f, 0.5f, true));
    }

    private AudioClip GenerarGolpe()
    {
        float[] golpe = Tono(110f, 60f, 0.12f, 0.8f);
        float[] ruido = Ruido(0.06f, 0.4f);
        return CrearClip("golpe", Mezclar(golpe, ruido));
    }

    private AudioClip GenerarPerfecto()
    {
        float[] nota = Tono(1568f, 1568f, 0.3f, 0.4f);
        float[] brillo = Tono(3136f, 3136f, 0.2f, 0.15f);
        return CrearClip("perfecto", Mezclar(nota, brillo));
    }

    private AudioClip GenerarTension()
    {
        int muestras = (int)(1.6f * FRECUENCIA);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / FRECUENCIA;
            float avance = (float)i / muestras;
            // tono grave que vibra, con entrada y salida suaves
            float temblor = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 7f * t);
            float volumen = Mathf.Min(avance * 6f, 1f) * Mathf.Min((1f - avance) * 4f, 1f);
            datos[i] = Mathf.Sin(2f * Mathf.PI * 98f * t) * temblor * volumen * 0.5f;
        }
        return CrearClip("tension", datos);
    }

    private AudioClip GenerarJingle()
    {
        float[] notas = { 523f, 659f, 784f, 1047f };
        float[] datos = new float[0];
        for (int i = 0; i < notas.Length; i++)
        {
            float duracion = i == notas.Length - 1 ? 0.4f : 0.12f;
            datos = Unir(datos, Tono(notas[i], notas[i], duracion, 0.45f));
        }
        return CrearClip("jingle", datos);
    }

    // tono que va de una frecuencia a otra y se apaga solo
    private float[] Tono(float frecInicio, float frecFin, float duracion, float volumen, bool cuadrado = false)
    {
        int muestras = (int)(duracion * FRECUENCIA);
        float[] datos = new float[muestras];
        float fase = 0f;
        for (int i = 0; i < muestras; i++)
        {
            float avance = (float)i / muestras;
            float frec = Mathf.Lerp(frecInicio, frecFin, avance);
            fase += 2f * Mathf.PI * frec / FRECUENCIA;

            float onda = Mathf.Sin(fase);
            if (cuadrado) onda = Mathf.Sign(onda) * 0.5f;

            float entrada = Mathf.Min(i / (0.005f * FRECUENCIA), 1f);
            datos[i] = onda * volumen * entrada * (1f - avance) * (1f - avance);
        }
        return datos;
    }

    private float[] Ruido(float duracion, float volumen)
    {
        int muestras = (int)(duracion * FRECUENCIA);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float avance = (float)i / muestras;
            datos[i] = Random.Range(-1f, 1f) * volumen * (1f - avance) * (1f - avance);
        }
        return datos;
    }

    private float[] Unir(float[] a, float[] b)
    {
        float[] r = new float[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }

    private float[] Mezclar(float[] a, float[] b)
    {
        float[] r = new float[Mathf.Max(a.Length, b.Length)];
        for (int i = 0; i < r.Length; i++)
        {
            float va = i < a.Length ? a[i] : 0f;
            float vb = i < b.Length ? b[i] : 0f;
            r[i] = Mathf.Clamp(va + vb, -1f, 1f);
        }
        return r;
    }

    private AudioClip CrearClip(string nombre, float[] datos)
    {
        AudioClip clip = AudioClip.Create(nombre, datos.Length, 1, FRECUENCIA, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
