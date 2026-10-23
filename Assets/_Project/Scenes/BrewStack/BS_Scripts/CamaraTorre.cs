using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraTorre : MonoBehaviour
{
    [SerializeField] private Torre torre;
    [SerializeField] private float velocidadSeguimiento = 3f;
    // espacio entre el bloque que se balancea y el borde de arriba de la pantalla
    [SerializeField] private float margenSuperior = 2f;
    // parte de arriba de la pantalla que tapan los paneles del HUD y la barra de estabilidad
    [Range(0f, 0.5f)]
    [SerializeField] private float franjaHUD = 0.2f;

    [Header("Sacudida")]
    [SerializeField] private float sacudidaMalPuesto = 0.08f;
    [SerializeField] private float duracionMalPuesto = 0.15f;
    [SerializeField] private float sacudidaColapso = 0.35f;
    [SerializeField] private float duracionColapso = 0.5f;

    private Camera cam;
    private float yInicial;
    private float xInicial;
    private float ySeguida;

    private float intensidadSacudida;
    private float duracionSacudida;
    private float tiempoSacudida;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        yInicial = transform.position.y;
        xInicial = transform.position.x;
        ySeguida = yInicial;
    }

    private void OnEnable()
    {
        torre.AlAterrizar += Aterrizo;
        torre.AlColapsar += Colapso;
    }

    private void OnDisable()
    {
        torre.AlAterrizar -= Aterrizo;
        torre.AlColapsar -= Colapso;
    }

    private void LateUpdate()
    {
        float altoPantalla = cam.orthographicSize * 2f;
        float objetivo = torre.YBloqueBalanceo + margenSuperior + altoPantalla * franjaHUD - cam.orthographicSize;
        objetivo = Mathf.Max(objetivo, yInicial);
        // si se quedó atrás alcanza más rápido, así el bloque nuevo no sale debajo del HUD
        float velocidad = objetivo - ySeguida > 1f ? velocidadSeguimiento * 3f : velocidadSeguimiento;
        ySeguida = Mathf.Lerp(ySeguida, objetivo, velocidad * Time.deltaTime);

        // la sacudida se suma encima del seguimiento, así no lo desacomoda
        Vector2 sacudida = Vector2.zero;
        if (tiempoSacudida < duracionSacudida)
        {
            tiempoSacudida += Time.deltaTime;
            float fuerza = intensidadSacudida * (1f - tiempoSacudida / duracionSacudida);
            sacudida = Random.insideUnitCircle * fuerza;
        }

        transform.position = new Vector3(xInicial + sacudida.x, ySeguida + sacudida.y, transform.position.z);
    }

    public void Sacudir(float intensidad, float duracion)
    {
        // si ya hay una más fuerte en curso no se reemplaza por una leve
        float restante = duracionSacudida - tiempoSacudida;
        if (restante > 0f && intensidad < intensidadSacudida) return;

        intensidadSacudida = intensidad;
        duracionSacudida = duracion;
        tiempoSacudida = 0f;
    }

    private void Aterrizo(Torre.Resultado res, Torre.Calidad calidad, Bloque bloque)
    {
        if (res == Torre.Resultado.MalPuesto) Sacudir(sacudidaMalPuesto, duracionMalPuesto);
    }

    private void Colapso()
    {
        Sacudir(sacudidaColapso, duracionColapso);
    }
}
