using UnityEngine;
using UnityEngine.SceneManagement;

// Fondo infinito en horizontal: repite el fondo del nivel (en espejo para
// que no se noten las uniones) siguiendo a la camara. No requiere tocar la escena.
public class FondoInfinito : MonoBehaviour
{
    private SpriteRenderer original;
    private SpriteRenderer[] copias = new SpriteRenderer[2];
    private float ancho;
    private float baseX;
    private Camera camara;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Arrancar()
    {
        SceneManager.sceneLoaded += (escena, modo) => Crear();
        Crear();
    }

    private static void Crear()
    {
        if (!SceneManager.GetActiveScene().name.StartsWith("Level"))
            return;

        if (FindFirstObjectByType<FondoInfinito>() != null)
            return;

        new GameObject("FondoInfinito").AddComponent<FondoInfinito>();
    }

    private void Start()
    {
        camara = Camera.main;
        SpriteRenderer mejor = null;

        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr.sprite == null || sr.sortingOrder > -5)
                continue;

            if (mejor == null || sr.bounds.size.x > mejor.bounds.size.x)
                mejor = sr;
        }

        if (mejor == null || camara == null)
        {
            enabled = false;
            return;
        }

        original = mejor;
        ancho = original.bounds.size.x;
        baseX = original.transform.position.x;

        for (int i = 0; i < copias.Length; i++)
        {
            GameObject o = Instantiate(original.gameObject, original.transform.parent);
            o.name = "FondoCopia" + i;
            copias[i] = o.GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (original == null || camara == null)
            return;

        int centro = Mathf.RoundToInt((camara.transform.position.x - baseX) / ancho);
        Colocar(original, centro);
        Colocar(copias[0], centro - 1);
        Colocar(copias[1], centro + 1);
    }

    private void Colocar(SpriteRenderer sr, int indice)
    {
        Vector3 p = sr.transform.position;
        p.x = baseX + indice * ancho;
        sr.transform.position = p;
        sr.flipX = (Mathf.Abs(indice) % 2) == 1;
    }
}
