using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable]
public class SDInicioResp
{
    public long id_sesion_juego;
    public string turno;
    public string nombre_turno;
    public int segundos_limite;
    public int turnos_restantes_hoy;
    public string mensaje;
}

[Serializable]
public class SDPartidaResp
{
    public long id_partida;
    public int monedas_ganadas;
    public long saldo_actual;
    public int mejor_puntaje_anterior;
    public int nuevo_record;
    public string mensaje;
}

[Serializable]
public class SDPreguntaResp
{
    public string id;
    public int dificultad;
    public string texto;
    public string[] opciones;
    public int correcta;
}

[Serializable]
public class SDPreguntasResp
{
    public SDPreguntaResp[] preguntas;
}

[Serializable]
class SDRespuestaReq
{
    public int usuario;
    public int pregunta;
    public bool correcta;
    public string clavePartida;
}

[Serializable]
class SDInicioReq
{
    public int usuario;
    public string juego;
    public string clavePartida;
}

[Serializable]
class SDPartidaReq
{
    public int usuario;
    public string juego;
    public int puntaje;
    public int duracionSeg;
    public bool completada;
    public string clavePartida;
}

[Serializable]
class SDError
{
    public string error;
}

public class SDKiosko : MonoBehaviour
{
    private const string Juego = "SPACE_DEFENDER";
    private const string EscenaMenu = "SD_MainMenu";
    private const string EscenaJuego = "SD_Gameplay";
    private const string EscenaResultados = "SD_Aftermath";
    private const int UsuarioEditor = 1;
    private const string UrlEditor = "http://localhost:3000";

    private const int PuntosPorOleada = 60;
    private const int PuntosPorTrivia = 20;
    private const int MaximoTriviasConPuntos = 6;
    private const int PuntosVictoria = 100;
    private const int VidaPorPunto = 2;

    private static SDKiosko instancia;

    private int usuario;
    private string urlBase;

    private string clavePartida;
    private bool corrida;
    private bool guardando;
    private bool sinConexion;
    private float inicioTiempo;
    private int limiteSegundos;
    private int oleadas;
    private int triviasCorrectas;

    private string aviso;
    private float avisoHasta;
    private string bloqueo;

    private GUIStyle estiloHud;
    private GUIStyle estiloAviso;
    private GUIStyle estiloCaja;
    private GUIStyle estiloBoton;

    public static SDKiosko Obtener()
    {
        if (instancia == null)
        {
            GameObject objeto = new GameObject("SDKiosko");
            instancia = objeto.AddComponent<SDKiosko>();
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

        if (Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor)
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
        if (escena.name == EscenaJuego)
        {
            oleadas = 0;
            triviasCorrectas = 0;

            if (!corrida && !guardando && !sinConexion)
                IniciarCorrida();

            return;
        }

        if (escena.name == EscenaResultados)
            return;

        bloqueo = null;
        sinConexion = false;
        Time.timeScale = 1f;

        if (corrida)
            StartCoroutine(GuardarPartida(false, 0f, null));
    }

    public void OleadaCompletada(int total)
    {
        oleadas = total;
    }

    public void Respuesta(int idPregunta, bool correcta)
    {
        if (correcta)
            triviasCorrectas++;

        if (idPregunta <= 0 || !corrida)
            return;

        SDRespuestaReq cuerpo = new SDRespuestaReq
        {
            usuario = usuario,
            pregunta = idPregunta,
            correcta = correcta,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<SDPartidaResp>("POST", "/api/respuestas",
            JsonUtility.ToJson(cuerpo), null,
            (mensaje, codigo) => Debug.LogWarning("[SDKiosko] Respuesta: " + mensaje)));
    }

    public IEnumerator CargarPreguntas(Action<List<SD_TriviaController.Question>> alTerminar)
    {
        List<SD_TriviaController.Question> lista = null;

        yield return Enviar<SDPreguntasResp>("GET", "/api/trivia?juego=" + Juego, null,
            respuesta =>
            {
                if (respuesta == null || respuesta.preguntas == null)
                    return;

                lista = new List<SD_TriviaController.Question>();

                foreach (SDPreguntaResp pregunta in respuesta.preguntas)
                {
                    if (pregunta.opciones == null || pregunta.opciones.Length != 3)
                        continue;

                    if (pregunta.correcta < 0 || pregunta.correcta > 2)
                        continue;

                    int.TryParse(pregunta.id, out int id);

                    lista.Add(new SD_TriviaController.Question
                    {
                        id = id,
                        text = pregunta.texto,
                        answers = pregunta.opciones,
                        correctIndex = pregunta.correcta,
                        explanation = ""
                    });
                }
            },
            (mensaje, codigo) => Debug.LogWarning("[SDKiosko] Preguntas: " + mensaje));

        alTerminar(lista);
    }

    public static int CalcularPuntaje(int oleadasCompletadas, bool victoria, float vidaRestante, int triviasAcertadas)
    {
        int puntaje = oleadasCompletadas * PuntosPorOleada
                    + Mathf.Min(triviasAcertadas, MaximoTriviasConPuntos) * PuntosPorTrivia;

        if (victoria)
            puntaje += PuntosVictoria + Mathf.RoundToInt(Mathf.Max(0f, vidaRestante)) / VidaPorPunto;

        return puntaje;
    }

    public void Terminar(bool victoria, int oleadasCompletadas, float vidaRestante, string escenaDestino)
    {
        oleadas = oleadasCompletadas;
        StartCoroutine(TerminarRutina(victoria, vidaRestante, escenaDestino));
    }

    private IEnumerator TerminarRutina(bool victoria, float vidaRestante, string escenaDestino)
    {
        SD_RunResults.SetReward(
            CalcularPuntaje(oleadas, victoria, vidaRestante, triviasCorrectas), 0, false);

        if (corrida)
            yield return GuardarPartida(victoria, vidaRestante, null);

        SceneManager.LoadScene(escenaDestino);
    }

    private void IniciarCorrida()
    {
        bloqueo = null;

        if (usuario <= 0)
        {
            sinConexion = true;
            MostrarAviso("Entraste sin sesión del kiosco: tu puntaje no se guardará.", 6f);
            return;
        }

        clavePartida = "sd-" + usuario + "-" + Guid.NewGuid().ToString("N");

        SDInicioReq cuerpo = new SDInicioReq
        {
            usuario = usuario,
            juego = Juego,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<SDInicioResp>("POST", "/api/partidas/iniciar",
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

    private IEnumerator GuardarPartida(bool victoria, float vidaRestante, int? segundos)
    {
        corrida = false;
        guardando = true;

        int duracion = Mathf.Clamp(
            segundos ?? Mathf.RoundToInt(Time.realtimeSinceStartup - inicioTiempo), 1, 3600);

        int puntaje = CalcularPuntaje(oleadas, victoria, vidaRestante, triviasCorrectas);

        SDPartidaReq cuerpo = new SDPartidaReq
        {
            usuario = usuario,
            juego = Juego,
            puntaje = Mathf.Clamp(puntaje, 0, 100000),
            duracionSeg = duracion,
            completada = victoria,
            clavePartida = clavePartida
        };

        yield return Enviar<SDPartidaResp>("POST", "/api/partidas",
            JsonUtility.ToJson(cuerpo),
            respuesta =>
            {
                SD_RunResults.SetReward(cuerpo.puntaje, respuesta.monedas_ganadas, true);
                MostrarAviso(cuerpo.puntaje + " puntos · +" + respuesta.monedas_ganadas + " monedas", 8f);
            },
            (mensaje, codigo) => MostrarAviso("No se pudo guardar la partida: " + mensaje, 8f));

        guardando = false;
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

        StartCoroutine(GuardarPartida(false, 0f, null));
        Bloquear("Se acabó el tiempo del turno.");
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
            string texto = "Tiempo del turno: " + (restante / 60) + ":" + (restante % 60).ToString("00");
            GUI.Label(new Rect(12f, 8f, 350f, 30f), texto, estiloHud);
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
            alignment = TextAnchor.MiddleLeft
        };
        estiloHud.normal.textColor = Color.white;

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
                        SDError error = JsonUtility.FromJson<SDError>(texto);
                        if (error != null && !string.IsNullOrEmpty(error.error))
                            mensaje = error.error;
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                Debug.LogWarning("[SDKiosko] " + metodo + " " + ruta + " -> " + mensaje);
                alFallar?.Invoke(mensaje, peticion.responseCode);
            }
        }
    }
}
