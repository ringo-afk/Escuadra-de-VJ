using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Kiosko
{
    [Serializable]
    public class PartidaRequest
    {
        public int usuario;
        public string juego;
        public int dificultad;
        public int imagen;
        public int puntaje;
        public int duracionSeg;
        public bool completada;
        public string clavePartida;
    }

    [Serializable]
    public class PartidaResultado
    {
        public long id_partida;
        public int monedas_ganadas;
        public long saldo_actual;
        public int mejor_puntaje_anterior;
        public int nuevo_record;
        public string mensaje;
    }

    [Serializable]
    public class FilaRanking
    {
        public int posicion;
        public string jugador;
        public int mejor_puntaje;
        public int partidas;
        public int es_usuario_actual;
    }

    [Serializable]
    public class MiPosicion
    {
        public int mi_posicion;
        public int mi_mejor_puntaje;
        public int partidas;
    }

    [Serializable]
    public class LeaderboardRespuesta
    {
        public FilaRanking[] top;
        public MiPosicion miPosicion;
    }

    [Serializable]
    public class SaldoRespuesta
    {
        public string moneda;
        public long saldo;
    }

    [Serializable]
    public class DificultadHB
    {
        public int id_dificultad;
        public string nombre;
        public int piezas_por_lado;
    }

    [Serializable]
    public class ImagenHB
    {
        public int id_imagen;
        public string titulo;
        public string archivo;
        public int ancho_px;
        public int alto_px;
    }

    [Serializable]
    public class ConfigHeadBreaker
    {
        public DificultadHB[] dificultades;
        public ImagenHB[] imagenes;
    }

    [Serializable]
    public class InicioPartidaRequest
    {
        public int usuario;
        public string juego;
        public string clavePartida;
    }

    [Serializable]
    public class InicioPartida
    {
        public long id_sesion_juego;
        public string turno;
        public string nombre_turno;
        public int segundos_limite;
        public int turnos_restantes_hoy;
        public string mensaje;
    }

    [Serializable]
    public class ResumenTurnos
    {
        public bool puede_jugar;
        public string turno_actual;
        public string nombre_turno_actual;
        public int minutos_partida;
        public int turnos_restantes_hoy;
        public string mensaje;
    }

    [Serializable]
    public class TurnoJuego
    {
        public string turno;
        public string nombre;
        public string desde;
        public string hasta;
        public string estado;
    }

    [Serializable]
    public class TurnosRespuesta
    {
        public ResumenTurnos resumen;
        public TurnoJuego[] turnos;
    }

    [Serializable]
    class ErrorApi
    {
        public string error;
    }

    public class KioskoApi : MonoBehaviour
    {
        public static KioskoApi Instancia { get; private set; }

        [Tooltip("Direccion del API cuando pruebas en el Editor de Unity")]
        [SerializeField] private string urlEditor = "http://localhost:3000";

        [Tooltip("Direccion del API en el build WebGL. Vacio = el mismo servidor que sirve la pagina del juego")]
        [SerializeField] private string urlWebGL = "";

        [SerializeField] private int segundosEspera = 15;

        public static bool UltimoErrorFueDeRed { get; private set; }

        public string UrlBase
        {
            get
            {
                if (Application.isEditor || Application.platform != RuntimePlatform.WebGLPlayer)
                    return urlEditor.TrimEnd('/');
                if (!string.IsNullOrEmpty(urlWebGL))
                    return urlWebGL.TrimEnd('/');
                return new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority);
            }
        }

        private void Awake()
        {
            if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegistrarPartida(PartidaRequest partida, Action<PartidaResultado> alTerminar, Action<string> alFallar)
        {
            StartCoroutine(Enviar("POST", "/api/partidas", JsonUtility.ToJson(partida), alTerminar, alFallar));
        }

        public void IniciarPartida(InicioPartidaRequest datos, Action<InicioPartida> alTerminar, Action<string> alFallar)
        {
            StartCoroutine(Enviar("POST", "/api/partidas/iniciar", JsonUtility.ToJson(datos), alTerminar, alFallar));
        }

        public void ObtenerTurnos(int usuario, string claveJuego, Action<TurnosRespuesta> alTerminar, Action<string> alFallar)
        {
            StartCoroutine(Enviar("GET", $"/api/usuarios/{usuario}/turnos?juego={claveJuego}", null, alTerminar, alFallar));
        }

        public void ObtenerLeaderboard(string claveJuego, int usuario, Action<LeaderboardRespuesta> alTerminar, Action<string> alFallar)
        {
            StartCoroutine(Enviar("GET", $"/api/minijuegos/{claveJuego}/leaderboard?usuario={usuario}", null, alTerminar, alFallar));
        }

        public void ObtenerSaldo(int usuario, Action<SaldoRespuesta> alTerminar, Action<string> alFallar)
        {
            StartCoroutine(Enviar("GET", $"/api/usuarios/{usuario}/saldo", null, alTerminar, alFallar));
        }

        public void ObtenerConfigHeadBreaker(Action<ConfigHeadBreaker> alTerminar, Action<string> alFallar)
        {
            StartCoroutine(Enviar("GET", "/api/headbreaker/config", null, alTerminar, alFallar));
        }

        public void DescargarImagen(string archivo, Action<Texture2D> alTerminar, Action<string> alFallar)
        {
            StartCoroutine(BajarTextura($"{UrlBase}/{archivo.TrimStart('/')}", alTerminar, alFallar));
        }

        private IEnumerator BajarTextura(string url, Action<Texture2D> alTerminar, Action<string> alFallar)
        {
            using (var req = UnityWebRequestTexture.GetTexture(url))
            {
                req.timeout = segundosEspera;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                    alTerminar?.Invoke(DownloadHandlerTexture.GetContent(req));
                else
                {
                    Debug.LogWarning($"[KioskoApi] No se pudo descargar {url}: {req.error}");
                    alFallar?.Invoke("No se pudo descargar la imagen del rompecabezas");
                }
            }
        }

        private IEnumerator Enviar<T>(string metodo, string ruta, string json, Action<T> alTerminar, Action<string> alFallar)
        {
            using (var req = new UnityWebRequest(UrlBase + ruta, metodo))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                if (json != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                req.timeout = segundosEspera;

                yield return req.SendWebRequest();

                string texto = req.downloadHandler.text;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    alTerminar?.Invoke(JsonUtility.FromJson<T>(texto));
                }
                else
                {
                    string mensaje = req.responseCode == 0
                        ? "No hay conexión con el servidor del kiosco"
                        : $"Error {req.responseCode}";
                    if (!string.IsNullOrEmpty(texto))
                    {
                        try
                        {
                            var e = JsonUtility.FromJson<ErrorApi>(texto);
                            if (!string.IsNullOrEmpty(e.error)) mensaje = e.error;
                        }
                        catch (ArgumentException) {  }
                    }
                    UltimoErrorFueDeRed = req.responseCode == 0;
                    Debug.LogWarning($"[KioskoApi] {metodo} {ruta} -> {mensaje}");
                    alFallar?.Invoke(mensaje);
                }
            }
        }
    }
}
