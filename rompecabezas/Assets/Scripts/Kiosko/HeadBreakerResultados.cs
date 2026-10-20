using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace Kiosko
{
    public class HeadBreakerResultados : MonoBehaviour
    {
        private const string ClaveJuego = "HEAD_BREAKER";

        [Header("Textos del Canvas (opcionales)")]
        [SerializeField] private TMP_Text textoMonedas;
        [SerializeField] private TMP_Text textoSaldo;
        [SerializeField] private TMP_Text textoMensaje;
        [SerializeField] private TMP_Text textoRanking;

        [Header("Turnos (3 al dia, 10 min por partida)")]
        [Tooltip("Pide turno al API antes de cada partida. Apagalo solo para pruebas sin la base.")]
        [SerializeField] private bool usarTurnos = true;
        [Tooltip("Si el API esta apagado, deja jugar sin guardar (modo practica).")]
        [SerializeField] private bool permitirSinConexion = true;

        private string claveTurno;
        private bool turnoActivo;
        private float finTurno;

        public bool UsaTurnos => usarTurnos;
        public bool PermiteSinConexion => permitirSinConexion;
        public bool TurnoActivo => turnoActivo;
        public float SegundosRestantes => turnoActivo ? Mathf.Max(0f, finTurno - Time.realtimeSinceStartup) : -1f;

        public event Action<ConfigHeadBreaker> AlCargarConfig;

        public ConfigHeadBreaker Config { get; private set; }

        private DificultadHB dificultadActual;
        private ImagenHB imagenActual;
        private float inicio;
        private bool jugando;
        private PartidaRequest ultimaPartida;

        private void Start()
        {
            SesionKiosko.UsuarioCambio += AlCambiarUsuario;
            CargarConfig();
            ActualizarSaldoYRanking();
        }

        private void OnDestroy()
        {
            SesionKiosko.UsuarioCambio -= AlCambiarUsuario;
        }

        private void AlCambiarUsuario(int _) => ActualizarSaldoYRanking();

        public void CargarConfig()
        {
            KioskoApi.Instancia.ObtenerConfigHeadBreaker(
                cfg =>
                {
                    Config = cfg;
                    AlCargarConfig?.Invoke(cfg);
                    if (cfg.imagenes == null || cfg.imagenes.Length == 0)
                        Mostrar(textoMensaje, "Todavía no hay imágenes; pídele al admin que suba una");
                },
                error => Mostrar(textoMensaje, error));
        }

        public void IniciarTurno(Action alListo, Action<string, bool> alFallar)
        {
            if (!usarTurnos) { alListo?.Invoke(); return; }

            if (!SesionKiosko.HayUsuario)
            {
                alFallar?.Invoke("Inicia sesión en el kiosco para jugar", false);
                return;
            }
            if (KioskoApi.Instancia == null)
            {
                alFallar?.Invoke("No hay conexión con el servidor del kiosco", true);
                return;
            }

            string clave = Guid.NewGuid().ToString();
            KioskoApi.Instancia.IniciarPartida(
                new InicioPartidaRequest { usuario = SesionKiosko.Usuario, juego = ClaveJuego, clavePartida = clave },
                r =>
                {
                    claveTurno = clave;
                    turnoActivo = true;
                    finTurno = Time.realtimeSinceStartup + Mathf.Max(60, r.segundos_limite);
                    Mostrar(textoMonedas, "");
                    Mostrar(textoMensaje, r.mensaje);
                    KioskoWeb.Avisar("turno");
                    alListo?.Invoke();
                },
                error => alFallar?.Invoke(error, KioskoApi.UltimoErrorFueDeRed));
        }

        public void TerminarTurno()
        {
            turnoActivo = false;
            claveTurno = null;
            jugando = false;
        }

        public ImagenHB ImagenAleatoria() =>
            Config?.imagenes == null || Config.imagenes.Length == 0
                ? null
                : Config.imagenes[UnityEngine.Random.Range(0, Config.imagenes.Length)];

        public void IniciarRompecabezas(int idDificultad, int idImagen, Action<Texture2D, int> alTenerImagen,
                                        Action<string> alFallar = null)
        {
            dificultadActual = Config?.dificultades?.FirstOrDefault(d => d.id_dificultad == idDificultad);
            imagenActual = Config?.imagenes?.FirstOrDefault(i => i.id_imagen == idImagen);

            if (dificultadActual == null || imagenActual == null)
            {
                Mostrar(textoMensaje, "Elige una dificultad y una imagen");
                alFallar?.Invoke("Dificultad o imagen inválida");
                return;
            }

            Mostrar(textoMensaje, $"Cargando \"{imagenActual.titulo}\"…");
            KioskoApi.Instancia.DescargarImagen(imagenActual.archivo,
                textura =>
                {
                    inicio = Time.time;
                    jugando = true;
                    Mostrar(textoMensaje, $"{imagenActual.titulo} · {dificultadActual.nombre}");
                    alTenerImagen?.Invoke(textura, dificultadActual.piezas_por_lado);
                },
                error =>
                {
                    Mostrar(textoMensaje, error);
                    alFallar?.Invoke(error);
                });
        }

        public void TerminarRompecabezas(bool completado, int segundosJugados = -1, int puntajeJuego = -1)
        {
            if (!jugando || (usarTurnos && !turnoActivo))
            {
                jugando = false;
                turnoActivo = false;
                if (completado && puntajeJuego >= 0)
                {
                    Mostrar(textoMonedas, $"{puntajeJuego} pts");
                    Mostrar(textoMensaje, "Sin conexión con el kiosko: esta partida no se guardó");
                }
                return;
            }
            jugando = false;

            if (!SesionKiosko.HayUsuario)
            {
                Mostrar(textoMensaje, "Inicia sesión en el kiosco para guardar tu partida");
                return;
            }

            int segundos = segundosJugados > 0 ? segundosJugados : Mathf.RoundToInt(Time.time - inicio);
            segundos = Mathf.Clamp(segundos, 1, 3600);

            ultimaPartida = new PartidaRequest
            {
                usuario = SesionKiosko.Usuario,
                juego = ClaveJuego,
                dificultad = dificultadActual.id_dificultad,
                imagen = imagenActual.id_imagen,
                puntaje = Mathf.Clamp(puntajeJuego >= 0 ? puntajeJuego
                                      : CalcularPuntaje(dificultadActual.piezas_por_lado, segundos, completado),
                                      0, 100000),
                duracionSeg = segundos,
                completada = completado,
                clavePartida = usarTurnos ? claveTurno : Guid.NewGuid().ToString()
            };

            turnoActivo = false;

            Enviar(ultimaPartida);
        }

        public void CancelarPartida() => jugando = false;

        public void Reintentar()
        {
            if (ultimaPartida != null) Enviar(ultimaPartida);
        }

        public static int CalcularPuntaje(int piezasPorLado, int segundos, bool completado)
        {
            if (!completado) return 0;
            return Mathf.Max(100, piezasPorLado * piezasPorLado * 100 - segundos * 5);
        }

        public void ActualizarSaldoYRanking()
        {
            if (!SesionKiosko.HayUsuario) return;

            KioskoApi.Instancia.ObtenerSaldo(SesionKiosko.Usuario,
                s => Mostrar(textoSaldo, $"Saldo: {s.saldo}"), _ => { });

            KioskoApi.Instancia.ObtenerLeaderboard(ClaveJuego, SesionKiosko.Usuario,
                MostrarRanking, error => Mostrar(textoMensaje, error));
        }

        private void Enviar(PartidaRequest partida)
        {
            Mostrar(textoMonedas, "");
            Mostrar(textoMensaje, "Guardando partida…");

            KioskoApi.Instancia.RegistrarPartida(partida,
                r =>
                {
                    Mostrar(textoMonedas, $"{partida.puntaje} pts  ·  +{r.monedas_ganadas} monedas");
                    if (r.saldo_actual > 0) Mostrar(textoSaldo, $"Saldo: {r.saldo_actual}");
                    Mostrar(textoMensaje, !partida.completada ? "Se acabó el tiempo: tu turno terminó" :
                                          r.nuevo_record == 1 ? "¡Nuevo récord!" :
                                          string.IsNullOrEmpty(r.mensaje) ? "Partida guardada" : r.mensaje);
                    ultimaPartida = null;
                    string record = r.nuevo_record == 1 ? "true" : "false";
                    KioskoWeb.Avisar("partida",
                        "{\"monedas\":" + r.monedas_ganadas + ",\"puntaje\":" + partida.puntaje + ",\"record\":" + record + "}");
                    KioskoApi.Instancia.ObtenerLeaderboard(ClaveJuego, SesionKiosko.Usuario, MostrarRanking, _ => { });
                },
                error => Mostrar(textoMensaje, error));
        }

        private void MostrarRanking(LeaderboardRespuesta lb)
        {
            if (textoRanking == null || lb.top == null) return;

            var sb = new StringBuilder("TOP 10 · Head Breaker\n");
            foreach (var fila in lb.top)
            {
                string linea = $"{fila.posicion}. {fila.jugador}  {fila.mejor_puntaje}";
                sb.AppendLine(fila.es_usuario_actual == 1 ? $"<color=#FFC040>{linea}  (tú)</color>" : linea);
            }
            if (lb.miPosicion != null && lb.miPosicion.mi_posicion > 10)
                sb.AppendLine($"…\n{lb.miPosicion.mi_posicion}. Tú  {lb.miPosicion.mi_mejor_puntaje}");

            textoRanking.text = sb.ToString();
        }

        private static void Mostrar(TMP_Text texto, string valor)
        {
            if (texto != null) texto.text = valor;
        }
    }
}
