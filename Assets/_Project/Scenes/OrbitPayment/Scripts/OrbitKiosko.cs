using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable]
public class OrbitInicioResp
{
    public long id_sesion_juego;
    public string turno;
    public string nombre_turno;
    public int segundos_limite;
    public int turnos_restantes_hoy;
    public string mensaje;
}

[Serializable]
public class OrbitPartidaResp
{
    public long id_partida;
    public int monedas_ganadas;
    public long saldo_actual;
    public int mejor_puntaje_anterior;
    public int nuevo_record;
    public string mensaje;
}

[Serializable]
public class OrbitRespuestaResp
{
    public long id_respuesta;
}

[Serializable]
class OrbitInicioReq
{
    public int usuario;
    public string juego;
    public string clavePartida;
}

[Serializable]
class OrbitPartidaReq
{
    public int usuario;
    public string juego;
    public int puntaje;
    public int duracionSeg;
    public bool completada;
    public string clavePartida;
}

[Serializable]
class OrbitRespuestaReq
{
    public int usuario;
    public int pregunta;
    public bool correcta;
    public string clavePartida;
}

[Serializable]
class OrbitError
{
    public string error;
}

public class OrbitKiosko : MonoBehaviour
{
    private const string Juego = "OPTIC_PAYMENT";
    private const string EscenaMenu = "MainMenuOP";
    private const string EscenaInicio = "Level1";
    private const int UltimoNivel = 3;
    private const int UsuarioEditor = 1;
    private const string UrlEditor = "http://localhost:3000";

    private const int PuntosPorMoneda = 3;
    private const int PuntosPorDocumento = 25;
    private const int PuntosPorPregunta = 50;
    private const int PuntosBaseNivel = 50;
    private const int PuntosExtraPorNivel = 25;
    private const int PenalizacionReinicio = 50;
    // El bono por rapidez se calcula contra 10 min fijos, sin importar cuanto tiempo quede en la bolsa del turno
    private const int ReferenciaRapidez = 600;
    private const int PuntosJuegoCompleto = 150;
    private const int SegundosPorPuntoRapidez = 8;

    private static OrbitKiosko instancia;

    private int usuario;
    private string urlBase;

    private string clavePartida;
    private bool corrida;
    private bool finalizando;
    private bool sinConexion;
    private float inicioTiempo;
    private int limiteSegundos;
    private int puntaje;

    private readonly HashSet<int> nivelesCompletados = new HashSet<int>();
    private readonly HashSet<int> nivelesConPregunta = new HashSet<int>();
    private readonly Queue<PreguntaQuiz> cola = new Queue<PreguntaQuiz>();

    private string aviso;
    private float avisoHasta;
    private string bloqueo;

    private GUIStyle estiloHud;
    private GUIStyle estiloAviso;
    private GUIStyle estiloCaja;
    private GUIStyle estiloBoton;

    public static OrbitKiosko Obtener()
    {
        if (instancia == null)
        {
            GameObject objeto = new GameObject("OrbitKiosko");
            instancia = objeto.AddComponent<OrbitKiosko>();
        }

        return instancia;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Arrancar()
    {
        Obtener();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        if (Application.isEditor)
        {
            usuario = UsuarioEditor;
            urlBase = UrlEditor;
        }
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            usuario = LeerUsuarioDeUrl(Application.absoluteURL);
            urlBase = new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority);
        }
        else
        {
            usuario = UsuarioEditor;
            urlBase = UrlEditor;
        }

        SceneManager.sceneLoaded += AlCargarEscena;
        AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (instancia == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == EscenaMenu)
        {
            bloqueo = null;
            Finalizar(false);
            return;
        }

        if (escena.name == EscenaInicio && !corrida && !sinConexion && !finalizando)
            IniciarCorrida();
    }

    public void PrecargarPreguntas()
    {
        cola.Clear();

        string ruta = "/api/preguntas?juego=" + Juego
                      + "&dificultad=" + NivelActual()
                      + "&cantidad=3";

        StartCoroutine(Enviar<BancoPreguntasOrbit>("GET", ruta, null,
            banco =>
            {
                if (banco == null || banco.preguntas == null)
                    return;

                foreach (PreguntaQuiz pregunta in banco.preguntas)
                    cola.Enqueue(pregunta);
            },
            (mensaje, codigo) => Debug.LogWarning("[OrbitKiosko] Preguntas: " + mensaje)));
    }

    public bool SiguientePregunta(out PreguntaQuiz pregunta)
    {
        if (cola.Count > 0)
        {
            pregunta = cola.Dequeue();
            return true;
        }

        pregunta = null;
        return false;
    }

    public void PreguntaRespondida(int idPregunta, bool correcta, bool delServidor)
    {
        if (correcta && nivelesConPregunta.Add(NivelActual()))
            puntaje += PuntosPorPregunta;

        if (!delServidor || !corrida)
            return;

        OrbitRespuestaReq cuerpo = new OrbitRespuestaReq
        {
            usuario = usuario,
            pregunta = idPregunta,
            correcta = correcta,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<OrbitRespuestaResp>("POST", "/api/respuestas",
            JsonUtility.ToJson(cuerpo), null,
            (mensaje, codigo) => Debug.LogWarning("[OrbitKiosko] Respuesta: " + mensaje)));
    }

    public void NivelCompletado(int monedas, int documentos)
    {
        int nivel = NivelActual();

        if (nivelesCompletados.Add(nivel))
        {
            puntaje += monedas * PuntosPorMoneda
                     + documentos * PuntosPorDocumento
                     + PuntosBaseNivel + (nivel - 1) * PuntosExtraPorNivel;
        }

        if (nivel >= UltimoNivel)
            Finalizar(true);
    }

    // Se llama al reintentar un nivel tras quedarse sin vidas: resta una penalizacion fija
    // al puntaje (nunca baja de 0). Los puntos de niveles ya terminados se conservan.
    public void ReinicioNivel()
    {
        if (!corrida)
            return;

        puntaje = Mathf.Max(0, puntaje - PenalizacionReinicio);
        MostrarAviso("Reiniciaste el nivel: -" + PenalizacionReinicio + " puntos", 4f);
    }

    private void IniciarCorrida()
    {
        puntaje = 0;
        nivelesCompletados.Clear();
        nivelesConPregunta.Clear();
        bloqueo = null;

        if (usuario <= 0)
        {
            sinConexion = true;
            MostrarAviso("Entraste sin sesión del kiosco: tu puntaje no se guardará.", 6f);
            return;
        }

        clavePartida = "op-" + usuario + "-" + Guid.NewGuid().ToString("N");

        OrbitInicioReq cuerpo = new OrbitInicioReq
        {
            usuario = usuario,
            juego = Juego,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<OrbitInicioResp>("POST", "/api/partidas/iniciar",
            JsonUtility.ToJson(cuerpo),
            respuesta =>
            {
                corrida = true;
                inicioTiempo = Time.realtimeSinceStartup;
                limiteSegundos = respuesta.segundos_limite > 0 ? respuesta.segundos_limite : 600;
                MostrarAviso(respuesta.mensaje, 5f);
            },
            (mensaje, codigo) =>
            {
                if (codigo == 0)
                {
                    sinConexion = true;
                    MostrarAviso("Sin conexión con el kiosco: tu puntaje no se guardará.", 6f);
                }
                else
                {
                    Bloquear(mensaje);
                }
            }));
    }

    private void Finalizar(bool completada)
    {
        sinConexion = false;

        if (!corrida)
            return;

        corrida = false;
        finalizando = true;

        int duracion = Mathf.Clamp(
            Mathf.RoundToInt(Time.realtimeSinceStartup - inicioTiempo), 1, 3600);

        if (completada)
            puntaje += PuntosJuegoCompleto + Mathf.Max(0, ReferenciaRapidez - duracion) / SegundosPorPuntoRapidez;

        OrbitPartidaReq cuerpo = new OrbitPartidaReq
        {
            usuario = usuario,
            juego = Juego,
            puntaje = Mathf.Clamp(puntaje, 0, 100000),
            duracionSeg = duracion,
            completada = completada,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<OrbitPartidaResp>("POST", "/api/partidas",
            JsonUtility.ToJson(cuerpo),
            respuesta =>
            {
                finalizando = false;
                MostrarAviso(cuerpo.puntaje + " puntos · +" + respuesta.monedas_ganadas + " monedas", 8f);
            },
            (mensaje, codigo) =>
            {
                finalizando = false;
                MostrarAviso("No se pudo guardar la partida: " + mensaje, 8f);
            }));
    }

    private void Bloquear(string mensaje)
    {
        bloqueo = mensaje;
        Time.timeScale = 0f;
    }

    private void MostrarAviso(string texto, float segundos)
    {
        aviso = texto;
        avisoHasta = Time.realtimeSinceStartup + segundos;
    }

    private void Update()
    {
        if (!corrida || limiteSegundos <= 0)
            return;

        if (Time.realtimeSinceStartup - inicioTiempo < limiteSegundos)
            return;

        Finalizar(false);
        Bloquear("Se acabó el tiempo de la partida.");
    }

    private void OnGUI()
    {
        if (estiloHud == null)
            CrearEstilos();

        float escala = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
        float ancho = Screen.width / escala;

        if (corrida)
        {
            int restante = Mathf.Max(0, limiteSegundos - Mathf.RoundToInt(Time.realtimeSinceStartup - inicioTiempo));
            string texto = "Puntos: " + puntaje + "   Tiempo: " + (restante / 60) + ":" + (restante % 60).ToString("00");
            GUI.Label(new Rect(ancho - 360f, 8f, 350f, 30f), texto, estiloHud);
        }

        if (!string.IsNullOrEmpty(aviso) && Time.realtimeSinceStartup < avisoHasta)
            GUI.Label(new Rect(ancho / 2f - 330f, 8f, 660f, 40f), aviso, estiloAviso);

        if (!string.IsNullOrEmpty(bloqueo))
        {
            Rect caja = new Rect(ancho / 2f - 280f, 240f, 560f, 220f);
            GUI.Box(caja, bloqueo, estiloCaja);

            if (GUI.Button(new Rect(ancho / 2f - 110f, 390f, 220f, 46f), "Volver al menú", estiloBoton))
            {
                bloqueo = null;
                Time.timeScale = 1f;
                SceneManager.LoadScene(EscenaMenu);
            }
        }
    }

    private void CrearEstilos()
    {
        estiloHud = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        estiloHud.normal.textColor = new Color(0.8f, 1f, 0.95f);

        estiloAviso = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        estiloAviso.normal.textColor = Color.white;

        estiloCaja = new GUIStyle(GUI.skin.box)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
            padding = new RectOffset(24, 24, 36, 12)
        };
        estiloCaja.normal.textColor = Color.white;

        estiloBoton = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
    }

    private int NivelActual()
    {
        string nombre = SceneManager.GetActiveScene().name;
        int i = nombre.Length;

        while (i > 0 && char.IsDigit(nombre[i - 1]))
            i--;

        if (i < nombre.Length && int.TryParse(nombre.Substring(i), out int nivel))
            return nivel;

        return 1;
    }

    private static int LeerUsuarioDeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return 0;

        int q = url.IndexOf('?');
        if (q < 0) return 0;

        foreach (string par in url.Substring(q + 1).Split('&'))
        {
            string[] kv = par.Split('=');
            if (kv.Length == 2 && kv[0] == "usuario" && int.TryParse(kv[1], out int id))
                return id;
        }

        return 0;
    }

    private IEnumerator Enviar<T>(string metodo, string ruta, string json,
                                  Action<T> alTerminar, Action<string, long> alFallar)
    {
        using (UnityWebRequest peticion = new UnityWebRequest(urlBase + ruta, metodo))
        {
            peticion.downloadHandler = new DownloadHandlerBuffer();

            if (json != null)
            {
                peticion.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                peticion.SetRequestHeader("Content-Type", "application/json");
            }

            peticion.timeout = 15;

            yield return peticion.SendWebRequest();

            string texto = peticion.downloadHandler.text;

            if (peticion.result == UnityWebRequest.Result.Success)
            {
                alTerminar?.Invoke(JsonUtility.FromJson<T>(texto));
            }
            else
            {
                string mensaje = peticion.responseCode == 0
                    ? "Sin conexión con el servidor del kiosco"
                    : "Error " + peticion.responseCode;

                if (!string.IsNullOrEmpty(texto))
                {
                    try
                    {
                        OrbitError error = JsonUtility.FromJson<OrbitError>(texto);
                        if (error != null && !string.IsNullOrEmpty(error.error))
                            mensaje = error.error;
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                Debug.LogWarning("[OrbitKiosko] " + metodo + " " + ruta + " -> " + mensaje);
                alFallar?.Invoke(mensaje, peticion.responseCode);
            }
        }
    }
}
