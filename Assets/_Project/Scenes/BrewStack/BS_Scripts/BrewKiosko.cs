using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable]
public class BrewInicioResp
{
    public long id_sesion_juego;
    public string turno;
    public string nombre_turno;
    public int segundos_limite;
    public int turnos_restantes_hoy;
    public string mensaje;
}

[Serializable]
public class BrewPartidaResp
{
    public long id_partida;
    public int monedas_ganadas;
    public long saldo_actual;
    public int mejor_puntaje_anterior;
    public int nuevo_record;
    public string mensaje;
}

[Serializable]
public class BrewRespuestaResp
{
    public long id_respuesta;
}

[Serializable]
public class BrewTriviaResp
{
    public Pregunta[] preguntas;
}

[Serializable]
class BrewInicioReq
{
    public int usuario;
    public string juego;
    public string clavePartida;
}

[Serializable]
class BrewPartidaReq
{
    public int usuario;
    public string juego;
    public int puntaje;
    public int duracionSeg;
    public bool completada;
    public string clavePartida;
}

[Serializable]
class BrewRespuestaReq
{
    public int usuario;
    public int pregunta;
    public bool correcta;
    public string clavePartida;
}

[Serializable]
class BrewError
{
    public string error;
}

[Serializable]
class BrewAvisoPartida
{
    public int monedas;
    public int puntaje;
    public bool record;
}

public class BrewKiosko : MonoBehaviour
{
    private const string Juego = "BREWSTACK";
    private const string EscenaMenu = "BS_Menu";
    private const int UsuarioEditor = 1;
    private const string UrlEditor = "http://localhost:3000";

    private static BrewKiosko instancia;

    private int usuario;
    private string urlBase;

    private string clavePartida;
    private bool corrida;
    private bool inicioResuelto;
    private bool triviaResuelta;
    private bool sinConexion;
    private float inicioTiempo;
    private int puntaje;
    private bool bloqueado;

    private Pregunta[] triviaCache;
    private readonly List<KeyValuePair<int, bool>> respuestasPendientes = new List<KeyValuePair<int, bool>>();

    private string aviso;
    private float avisoHasta;
    private string textoBloqueo;

    private GUIStyle estiloAviso;
    private GUIStyle estiloCaja;
    private GUIStyle estiloBoton;

    public bool Listo => triviaResuelta && inicioResuelto;
    public bool Bloqueado => bloqueado;

    // el portal abre el juego dentro de un iframe con ?usuario= en la URL
    public static bool EnPortal =>
        Application.platform == RuntimePlatform.WebGLPlayer && LeerUsuarioDeUrl(Application.absoluteURL) > 0;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void BS_AvisarPortal(string tipo, string datos);
#endif

    private static void AvisarPortal(string tipo, string datos)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BS_AvisarPortal(tipo, datos);
#endif
    }

    // si hay partida abierta se guarda antes de pedirle al portal que quite el juego
    public void Cerrar()
    {
        if (corrida) Finalizar(puntaje, false, () => AvisarPortal("cerrar", "{}"));
        else AvisarPortal("cerrar", "{}");
    }

    public static BrewKiosko Obtener()
    {
        if (instancia == null)
        {
            GameObject objeto = new GameObject("BrewKiosko");
            instancia = objeto.AddComponent<BrewKiosko>();
        }

        return instancia;
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
    }

    private void OnDestroy()
    {
        if (instancia == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != EscenaMenu)
            return;

        textoBloqueo = null;
        bloqueado = false;
        Time.timeScale = 1f;

        if (corrida)
            Finalizar(puntaje, false);
    }

    public void PedirTrivia(Action<Pregunta[]> alRecibir)
    {
        if (triviaResuelta)
        {
            alRecibir?.Invoke(triviaCache);
            return;
        }

        StartCoroutine(Enviar<BrewTriviaResp>("GET", "/api/trivia?juego=" + Juego, null,
            banco =>
            {
                triviaCache = banco != null ? banco.preguntas : null;
                triviaResuelta = true;
                alRecibir?.Invoke(triviaCache);
            },
            (mensaje, codigo) =>
            {
                triviaResuelta = true;
                Debug.LogWarning("[BrewKiosko] Trivia: " + mensaje);
                alRecibir?.Invoke(null);
            }));
    }

    public void IniciarPartida()
    {
        inicioResuelto = false;
        bloqueado = false;
        textoBloqueo = null;
        Time.timeScale = 1f;
        puntaje = 0;
        corrida = false;
        respuestasPendientes.Clear();

        if (usuario <= 0)
        {
            sinConexion = true;
            inicioResuelto = true;
            MostrarAviso("Entraste sin sesión del kiosco: tu puntaje no se guardará.", 6f);
            return;
        }

        clavePartida = "bs-" + usuario + "-" + Guid.NewGuid().ToString("N");

        BrewInicioReq cuerpo = new BrewInicioReq
        {
            usuario = usuario,
            juego = Juego,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<BrewInicioResp>("POST", "/api/partidas/iniciar",
            JsonUtility.ToJson(cuerpo),
            respuesta =>
            {
                sinConexion = false;
                corrida = true;
                inicioTiempo = Time.realtimeSinceStartup;
                inicioResuelto = true;
                MostrarAviso(respuesta.mensaje, 5f);
                VaciarPendientes();
                AvisarPortal("turno", "{}");
            },
            (mensaje, codigo) =>
            {
                inicioResuelto = true;

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

    public void Puntaje(int total)
    {
        puntaje = total;
    }

    public void Respuesta(string idPregunta, bool correcta)
    {
        if (!int.TryParse(idPregunta, out int id) || sinConexion || usuario <= 0)
            return;

        if (!inicioResuelto)
        {
            respuestasPendientes.Add(new KeyValuePair<int, bool>(id, correcta));
            return;
        }

        if (!corrida)
            return;

        EnviarRespuesta(id, correcta);
    }

    public void Finalizar(int puntajeFinal, bool completada, Action alTerminar = null)
    {
        if (!corrida)
        {
            alTerminar?.Invoke();
            return;
        }

        corrida = false;

        int duracion = Mathf.Clamp(
            Mathf.RoundToInt(Time.realtimeSinceStartup - inicioTiempo), 1, 3600);

        BrewPartidaReq cuerpo = new BrewPartidaReq
        {
            usuario = usuario,
            juego = Juego,
            puntaje = Mathf.Clamp(puntajeFinal, 0, 100000),
            duracionSeg = duracion,
            completada = completada,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<BrewPartidaResp>("POST", "/api/partidas",
            JsonUtility.ToJson(cuerpo),
            respuesta =>
            {
                MostrarAviso(cuerpo.puntaje + " puntos · +" + respuesta.monedas_ganadas + " monedas", 8f);
                BrewAvisoPartida datos = new BrewAvisoPartida
                {
                    monedas = respuesta.monedas_ganadas,
                    puntaje = cuerpo.puntaje,
                    record = respuesta.nuevo_record == 1
                };
                AvisarPortal("partida", JsonUtility.ToJson(datos));
                alTerminar?.Invoke();
            },
            (mensaje, codigo) =>
            {
                MostrarAviso("No se pudo guardar la partida: " + mensaje, 8f);
                alTerminar?.Invoke();
            }));
    }

    private void VaciarPendientes()
    {
        foreach (KeyValuePair<int, bool> par in respuestasPendientes)
            EnviarRespuesta(par.Key, par.Value);

        respuestasPendientes.Clear();
    }

    private void EnviarRespuesta(int id, bool correcta)
    {
        BrewRespuestaReq cuerpo = new BrewRespuestaReq
        {
            usuario = usuario,
            pregunta = id,
            correcta = correcta,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<BrewRespuestaResp>("POST", "/api/respuestas",
            JsonUtility.ToJson(cuerpo), null,
            (mensaje, codigo) => Debug.LogWarning("[BrewKiosko] Respuesta: " + mensaje)));
    }

    private void Bloquear(string mensaje)
    {
        bloqueado = true;
        textoBloqueo = mensaje;
        Time.timeScale = 0f;
    }

    private void MostrarAviso(string texto, float segundos)
    {
        aviso = texto;
        avisoHasta = Time.realtimeSinceStartup + segundos;
    }

    private void OnGUI()
    {
        if (estiloAviso == null)
            CrearEstilos();

        float escala = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
        float ancho = Screen.width / escala;

        if (!string.IsNullOrEmpty(aviso) && Time.realtimeSinceStartup < avisoHasta)
            GUI.Label(new Rect(ancho / 2f - 330f, 8f, 660f, 40f), aviso, estiloAviso);

        if (!string.IsNullOrEmpty(textoBloqueo))
        {
            Rect caja = new Rect(ancho / 2f - 280f, 240f, 560f, 220f);
            GUI.Box(caja, textoBloqueo, estiloCaja);

            if (GUI.Button(new Rect(ancho / 2f - 110f, 390f, 220f, 46f), "Volver al menú", estiloBoton))
            {
                textoBloqueo = null;
                bloqueado = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene(EscenaMenu);
            }
        }
    }

    private void CrearEstilos()
    {
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
                        BrewError error = JsonUtility.FromJson<BrewError>(texto);
                        if (error != null && !string.IsNullOrEmpty(error.error))
                            mensaje = error.error;
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                Debug.LogWarning("[BrewKiosko] " + metodo + " " + ruta + " -> " + mensaje);
                alFallar?.Invoke(mensaje, peticion.responseCode);
            }
        }
    }
}
