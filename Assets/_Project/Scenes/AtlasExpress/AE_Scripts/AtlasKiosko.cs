using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable]
public class AtlasInicioResp
{
    public long id_sesion_juego;
    public string turno;
    public string nombre_turno;
    public int segundos_limite;
    public int turnos_restantes_hoy;
    public string mensaje;
}

[Serializable]
public class AtlasPartidaResp
{
    public long id_partida;
    public int monedas_ganadas;
    public long saldo_actual;
    public int mejor_puntaje_anterior;
    public int nuevo_record;
    public string mensaje;
}

[Serializable]
public class AtlasDocumentoResp
{
    public int id_documento;
    public string nombre_pasajero;
    public string tipo_documento;
    public int id_carpeta;
    public string carpeta;
}

[Serializable]
public class AtlasDocumentosResp
{
    public AtlasDocumentoResp[] documentos;
}

[Serializable]
class AtlasInicioReq
{
    public int usuario;
    public string juego;
    public string clavePartida;
}

[Serializable]
class AtlasPartidaReq
{
    public int usuario;
    public string juego;
    public int puntaje;
    public int duracionSeg;
    public bool completada;
    public string clavePartida;
}

[Serializable]
class AtlasError
{
    public string error;
}

public class AtlasKiosko : MonoBehaviour
{
    private const string Juego = "ATLAS_EXPRESS";
    private const string EscenaMenu = "AE_Menu";
    private const string EscenaJuego = "AE_Jugar";
    private const int UsuarioEditor = 1;
    private const string UrlEditor = "http://localhost:3000";

    private const int PuntosPorAcierto = 70;
    private const int PuntosVictoria = 100;
    private const int ReferenciaRapidez = 120;
    private const int SegundosPorPuntoRapidez = 2;

    private static AtlasKiosko instancia;

    private int usuario;
    private string urlBase;

    private string clavePartida;
    private bool corrida;
    private bool guardando;
    private bool sinConexion;
    private float inicioTiempo;
    private int limiteSegundos;
    private int aciertos;

    private string aviso;
    private float avisoHasta;
    private string bloqueo;

    private GUIStyle estiloHud;
    private GUIStyle estiloAviso;
    private GUIStyle estiloCaja;
    private GUIStyle estiloBoton;

    public static AtlasKiosko Obtener()
    {
        if (instancia == null)
        {
            GameObject objeto = new GameObject("AtlasKiosko");
            instancia = objeto.AddComponent<AtlasKiosko>();
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
        if (escena.name == EscenaMenu)
        {
            bloqueo = null;
            sinConexion = false;
            Time.timeScale = 1f;

            if (corrida)
                StartCoroutine(GuardarPartida(false, null));

            return;
        }

        if (escena.name != EscenaJuego)
            return;

        aciertos = 0;

        if (!corrida && !guardando && !sinConexion)
            IniciarCorrida();
    }

    public void DocumentoEvaluado(bool correcto)
    {
        if (correcto)
            aciertos++;
    }

    public IEnumerator CargarDocumentos(int cantidad, Action<List<DocumentoData>> alTerminar)
    {
        List<DocumentoData> lista = null;

        yield return Enviar<AtlasDocumentosResp>("GET",
            "/api/atlas/documentos?cantidad=" + cantidad, null,
            respuesta =>
            {
                if (respuesta == null || respuesta.documentos == null || respuesta.documentos.Length == 0)
                    return;

                lista = new List<DocumentoData>();

                foreach (AtlasDocumentoResp documento in respuesta.documentos)
                {
                    DocumentoData dato = ScriptableObject.CreateInstance<DocumentoData>();
                    dato.nombrePasajero = documento.nombre_pasajero;
                    dato.tipoDocumento = documento.tipo_documento;
                    dato.carpetaCorrecta = (CategoriaCarpeta)documento.id_carpeta;
                    lista.Add(dato);
                }
            },
            (mensaje, codigo) => Debug.LogWarning("[AtlasKiosko] Documentos: " + mensaje));

        alTerminar(lista);
    }

    public void Terminar(int aciertosFinales, int total, float segundos, bool gano, string escenaDestino)
    {
        aciertos = aciertosFinales;
        StartCoroutine(TerminarRutina(Mathf.RoundToInt(segundos), gano, escenaDestino));
    }

    private IEnumerator TerminarRutina(int segundos, bool gano, string escenaDestino)
    {
        PlayerPrefs.SetInt("MonedasGanadas", 0);

        if (corrida)
            yield return GuardarPartida(gano, segundos);

        PlayerPrefs.Save();
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

        clavePartida = "ae-" + usuario + "-" + Guid.NewGuid().ToString("N");

        AtlasInicioReq cuerpo = new AtlasInicioReq
        {
            usuario = usuario,
            juego = Juego,
            clavePartida = clavePartida
        };

        StartCoroutine(Enviar<AtlasInicioResp>("POST", "/api/partidas/iniciar",
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

    private IEnumerator GuardarPartida(bool gano, int? segundos)
    {
        corrida = false;
        guardando = true;

        int duracion = Mathf.Clamp(
            segundos ?? Mathf.RoundToInt(Time.realtimeSinceStartup - inicioTiempo), 1, 3600);

        int puntaje = aciertos * PuntosPorAcierto;

        if (gano)
            puntaje += PuntosVictoria + Mathf.Max(0, ReferenciaRapidez - duracion) / SegundosPorPuntoRapidez;

        AtlasPartidaReq cuerpo = new AtlasPartidaReq
        {
            usuario = usuario,
            juego = Juego,
            puntaje = Mathf.Clamp(puntaje, 0, 100000),
            duracionSeg = duracion,
            completada = gano,
            clavePartida = clavePartida
        };

        yield return Enviar<AtlasPartidaResp>("POST", "/api/partidas",
            JsonUtility.ToJson(cuerpo),
            respuesta =>
            {
                PlayerPrefs.SetInt("MonedasGanadas", respuesta.monedas_ganadas);
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

        StartCoroutine(GuardarPartida(false, null));
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
                        AtlasError error = JsonUtility.FromJson<AtlasError>(texto);
                        if (error != null && !string.IsNullOrEmpty(error.error))
                            mensaje = error.error;
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                Debug.LogWarning("[AtlasKiosko] " + metodo + " " + ruta + " -> " + mensaje);
                alFallar?.Invoke(mensaje, peticion.responseCode);
            }
        }
    }
}
