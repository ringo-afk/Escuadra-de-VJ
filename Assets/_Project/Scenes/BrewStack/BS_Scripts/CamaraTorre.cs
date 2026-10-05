using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraTorre : MonoBehaviour
{
    [SerializeField] private Torre torre;
    [SerializeField] private float velocidadSeguimiento = 3f;
    // espacio entre el bloque que se balancea y el borde de arriba de la pantalla
    [SerializeField] private float margenSuperior = 2f;

    private Camera cam;
    private float yInicial;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        yInicial = transform.position.y;
    }

    private void LateUpdate()
    {
        float objetivo = torre.YBloqueBalanceo + margenSuperior - cam.orthographicSize;
        objetivo = Mathf.Max(objetivo, yInicial);

        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, objetivo, velocidadSeguimiento * Time.deltaTime);
        transform.position = pos;
    }
}
