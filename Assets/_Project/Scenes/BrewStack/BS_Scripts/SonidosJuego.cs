using UnityEngine;

public class SonidosJuego : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Torre torre;
    [SerializeField] private TriviaUI trivia;
    [SerializeField] private ResultadoFinalUI resultado;
    [SerializeField] private JuegoManager juego;

    [Header("Clips (si están vacíos se generan por código)")]
    [SerializeField] private AudioClip clipAcierto;
    [SerializeField] private AudioClip clipError;
    [SerializeField] private AudioClip clipGolpe;
    [SerializeField] private AudioClip clipPerfecto;
    [SerializeField] private AudioClip clipTension;
    [SerializeField] private AudioClip clipJingle;
    [SerializeField] private AudioClip clipSoltar;
    [SerializeField] private AudioClip clipCaida;
    [SerializeField] private AudioClip clipColapso;
    [SerializeField] private AudioClip clipPregunta;
    [SerializeField] private AudioClip clipTic;
    [SerializeField] private AudioClip clipNivel;
    [SerializeField] private AudioClip clipRecord;
    [SerializeField] private AudioClip clipAlturaMaxima;

    [Header("Volumen")]
    [Range(0f, 1f)]
    [SerializeField] private float volumenGeneral = 0.8f;

    [Header("Combo de perfectos")]
    [SerializeField] private float pasoTono = 0.08f;
    [SerializeField] private int maxPasos = 8;

    [Header("Tensión")]
    [Range(0f, 1f)]
    [SerializeField] private float umbralTension = 0.3f;

    [Header("Tic de tiempo")]
    [SerializeField] private int segundosTicPregunta = 3;
    [SerializeField] private int segundosTicPartida = 10;
    // el de la partida va más grave para no confundirlo con el de la pregunta
    [SerializeField] private float tonoTicPartida = 0.7f;
    [SerializeField] private float esperaRecord = 0.85f;

    private const int FRECUENCIA = 22050;

    private AudioSource fuenteEfectos;
    private AudioSource fuentePerfecto;
    private AudioSource fuenteTension;
    private AudioSource fuenteTic;
    private int combo;
    private bool tensionArmada = true;
    private int ultimoTicPregunta;
    private int ultimoTicPartida;

    public float UmbralTension => umbralTension;

    private void Awake()
    {
        // fuentes separadas: el perfecto y el tic cambian de tono y la tensión se tiene que poder parar
        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuentePerfecto = gameObject.AddComponent<AudioSource>();
        fuenteTension = gameObject.AddComponent<AudioSource>();
        fuenteTic = gameObject.AddComponent<AudioSource>();
        foreach (AudioSource f in new[] { fuenteEfectos, fuentePerfecto, fuenteTension, fuenteTic })
        {
            f.playOnAwake = false;
            f.loop = false;
        }
        fuenteTic.pitch = tonoTicPartida;

        if (clipAcierto == null) clipAcierto = GenerarAcierto();
        if (clipError == null) clipError = GenerarError();
        if (clipGolpe == null) clipGolpe = GenerarGolpe();
        if (clipPerfecto == null) clipPerfecto = GenerarPerfecto();
        if (clipTension == null) clipTension = GenerarTension();
        if (clipJingle == null) clipJingle = GenerarJingle();
        if (clipSoltar == null) clipSoltar = GenerarSoltar();
        if (clipCaida == null) clipCaida = GenerarCaida();
        if (clipColapso == null) clipColapso = GenerarColapso();
        if (clipPregunta == null) clipPregunta = GenerarPregunta();
        if (clipTic == null) clipTic = GenerarTic();
        if (clipNivel == null) clipNivel = GenerarNivel();
        if (clipRecord == null) clipRecord = GenerarRecord();
        if (clipAlturaMaxima == null) clipAlturaMaxima = GenerarAlturaMaxima();
    }

    private void OnEnable()
    {
        torre.AlAterrizar += Aterrizo;
        torre.AlSoltar += Solto;
        torre.AlColapsar += Colapso;
        torre.AlLlegarAlturaMaxima += LlegoAlturaMaxima;
        trivia.AlMarcarRespuesta += Respondio;
        trivia.AlMostrarPregunta += MostroPregunta;
        resultado.AlMostrar += MostroResultado;
        if (juego != null) juego.AlCambiarNivel += CambioNivel;
    }

    private void OnDisable()
    {
        torre.AlAterrizar -= Aterrizo;
        torre.AlSoltar -= Solto;
        torre.AlColapsar -= Colapso;
        torre.AlLlegarAlturaMaxima -= LlegoAlturaMaxima;
        trivia.AlMarcarRespuesta -= Respondio;
        trivia.AlMostrarPregunta -= MostroPregunta;
        resultado.AlMostrar -= MostroResultado;
        if (juego != null) juego.AlCambiarNivel -= CambioNivel;
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

        RevisarTic();
    }

    // un tic cada vez que cambia el segundo, solo en los últimos segundos
    private void RevisarTic()
    {
        if (trivia.EsperandoRespuesta)
        {
            int segundo = Mathf.CeilToInt(trivia.TiempoRestante);
            if (segundo != ultimoTicPregunta)
            {
                ultimoTicPregunta = segundo;
                if (segundo <= segundosTicPregunta) fuenteEfectos.PlayOneShot(clipTic, volumenGeneral);
            }
        }

        if (juego != null && !juego.Terminada)
        {
            int segundo = Mathf.CeilToInt(juego.TiempoRestante);
            if (segundo != ultimoTicPartida)
            {
                ultimoTicPartida = segundo;
                if (segundo <= segundosTicPartida && segundo > 0) fuenteTic.PlayOneShot(clipTic, volumenGeneral);
            }
        }
    }

    private void Aterrizo(Torre.Resultado res, Torre.Calidad calidad, Bloque bloque)
    {
        // si se cayó no pegó en la torre, por eso no lleva golpe
        AudioClip clip = res == Torre.Resultado.SeCayo ? clipCaida : clipGolpe;
        fuenteEfectos.PlayOneShot(clip, volumenGeneral);

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

        // el récord suena cuando termina el jingle
        if (resultado.EsRecord) Invoke(nameof(SonarRecord), esperaRecord);
    }

    private void SonarRecord()
    {
        fuenteEfectos.PlayOneShot(clipRecord, volumenGeneral);
    }

    private void Solto()
    {
        fuenteEfectos.PlayOneShot(clipSoltar, volumenGeneral);
    }

    private void Colapso()
    {
        fuenteTension.Stop();
        fuenteEfectos.PlayOneShot(clipColapso, volumenGeneral);
    }

    private void LlegoAlturaMaxima()
    {
        fuenteEfectos.PlayOneShot(clipAlturaMaxima, volumenGeneral);
    }

    private void MostroPregunta()
    {
        fuenteEfectos.PlayOneShot(clipPregunta, volumenGeneral);
    }

    private void CambioNivel(int nivel)
    {
        fuenteEfectos.PlayOneShot(clipNivel, volumenGeneral);
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

    private AudioClip GenerarSoltar()
    {
        float[] aire = Ruido(0.12f, 0.15f);
        float[] tono = Tono(900f, 350f, 0.12f, 0.25f);
        return CrearClip("soltar", Mezclar(aire, tono));
    }

    private AudioClip GenerarCaida()
    {
        return CrearClip("caida", Tono(700f, 120f, 0.55f, 0.4f));
    }

    private AudioClip GenerarColapso()
    {
        float[] retumbo = Tono(90f, 30f, 1.1f, 0.45f);
        float[] ruido = Ruido(0.9f, 0.25f);
        float[] golpes = Unir(Unir(Tono(140f, 70f, 0.15f, 0.3f), Tono(120f, 60f, 0.15f, 0.3f)), Tono(100f, 50f, 0.2f, 0.3f));
        return CrearClip("colapso", Mezclar(Mezclar(retumbo, ruido), golpes));
    }

    private AudioClip GenerarPregunta()
    {
        float[] subida = Tono(300f, 900f, 0.1f, 0.35f);
        float[] nota = Tono(900f, 900f, 0.14f, 0.25f);
        return CrearClip("pregunta", Unir(subida, nota));
    }

    private AudioClip GenerarTic()
    {
        return CrearClip("tic", Mezclar(Tono(2000f, 2000f, 0.03f, 0.35f), Ruido(0.012f, 0.25f)));
    }

    private AudioClip GenerarNivel()
    {
        float[] notas = { 392f, 523f, 659f, 784f };
        float[] datos = new float[0];
        for (int i = 0; i < notas.Length; i++)
        {
            float duracion = i == notas.Length - 1 ? 0.3f : 0.08f;
            datos = Unir(datos, Tono(notas[i], notas[i], duracion, 0.4f, true));
        }
        return CrearClip("nivel", datos);
    }

    private AudioClip GenerarRecord()
    {
        float[] notas = { 659f, 784f, 988f, 1319f };
        float[] duraciones = { 0.1f, 0.1f, 0.1f, 0.6f };
        float[] datos = new float[0];
        for (int i = 0; i < notas.Length; i++)
        {
            // la octava de arriba le da brillo
            float[] nota = Mezclar(Tono(notas[i], notas[i], duraciones[i], 0.35f), Tono(notas[i] * 2f, notas[i] * 2f, duraciones[i], 0.1f));
            datos = Unir(datos, nota);
        }
        return CrearClip("record", datos);
    }

    private AudioClip GenerarAlturaMaxima()
    {
        float[] acorde = Mezclar(Tono(523f, 523f, 1.2f, 0.22f), Tono(659f, 659f, 1.2f, 0.22f));
        acorde = Mezclar(acorde, Tono(784f, 784f, 1.2f, 0.22f));
        acorde = Mezclar(acorde, Tono(1047f, 1047f, 1.2f, 0.15f));
        return CrearClip("altura_maxima", acorde);
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
