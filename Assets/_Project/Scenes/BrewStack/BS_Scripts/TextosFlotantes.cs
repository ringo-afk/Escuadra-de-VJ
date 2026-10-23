using UnityEngine;
using TMPro;

public class TextosFlotantes : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset fuente;
    [SerializeField] private int cantidadEnPool = 6;
    [SerializeField] private float tamanoTexto = 4f;
    [SerializeField] private float velocidadSubida = 1.2f;
    [SerializeField] private float duracion = 0.8f;
    [SerializeField] private float alturaInicial = 0.5f;
    [SerializeField] private Color colorBienPuesto = new Color(0.62f, 0.85f, 1f);
    [SerializeField] private Color colorMalPuesto = new Color(0.88f, 0.59f, 0.23f);

    private TextMeshPro[] textos;
    private float[] tiempos;
    private int siguiente;

    private void Awake()
    {
        // se crean todos al inicio y se reutilizan, así no hay Instantiate durante el juego
        textos = new TextMeshPro[cantidadEnPool];
        tiempos = new float[cantidadEnPool];
        for (int i = 0; i < cantidadEnPool; i++)
        {
            GameObject obj = new GameObject("TextoFlotante");
            obj.transform.SetParent(transform, false);
            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            if (fuente != null) tmp.font = fuente;
            tmp.fontSize = tamanoTexto;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.sortingOrder = 20;
            obj.SetActive(false);
            textos[i] = tmp;
        }
    }

    public void Mostrar(int puntos, Vector3 posicion, bool bienPuesto)
    {
        TextMeshPro tmp = textos[siguiente];
        tiempos[siguiente] = 0f;
        siguiente = (siguiente + 1) % textos.Length;

        tmp.text = "+" + puntos;
        tmp.color = bienPuesto ? colorBienPuesto : colorMalPuesto;
        tmp.transform.position = posicion + Vector3.up * alturaInicial;
        tmp.gameObject.SetActive(true);
    }

    private void Update()
    {
        for (int i = 0; i < textos.Length; i++)
        {
            if (!textos[i].gameObject.activeSelf) continue;

            tiempos[i] += Time.deltaTime;
            float avance = tiempos[i] / duracion;
            if (avance >= 1f)
            {
                textos[i].gameObject.SetActive(false);
                continue;
            }

            textos[i].transform.position += Vector3.up * velocidadSubida * Time.deltaTime;
            Color c = textos[i].color;
            c.a = 1f - avance * avance;
            textos[i].color = c;
        }
    }
}
