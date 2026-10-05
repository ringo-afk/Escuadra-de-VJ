using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FondoJuego : MonoBehaviour
{
    [SerializeField] private Torre torre;
    [SerializeField] private Color colorAbajo = new Color(0.04f, 0.07f, 0.16f);
    [SerializeField] private Color colorArriba = new Color(0.09f, 0.03f, 0.16f);
    // altura en bloques a la que ya se ve todo el color de arriba
    [SerializeField] private int alturaColorCompleto = 40;
    [SerializeField] private float velocidadCambio = 1f;

    private Camera cam;
    private float avanceMostrado;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        // se usa la altura alcanzada para que no regrese de golpe cuando la torre colapsa
        float objetivo = Mathf.Clamp01((float)torre.AlturaAlcanzada / alturaColorCompleto);
        avanceMostrado = Mathf.MoveTowards(avanceMostrado, objetivo, velocidadCambio * Time.deltaTime);
        cam.backgroundColor = Color.Lerp(colorAbajo, colorArriba, avanceMostrado);
    }
}
