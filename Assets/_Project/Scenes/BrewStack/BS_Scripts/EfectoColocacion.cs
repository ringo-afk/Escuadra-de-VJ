using System.Collections;
using UnityEngine;

public class EfectoColocacion : MonoBehaviour
{
    [SerializeField] private Torre torre;
    [SerializeField] private Color colorEfecto = new Color(0.62f, 0.85f, 1f);

    [Header("Destello")]
    [SerializeField] private float duracionDestello = 0.15f;
    [Range(0f, 1f)]
    [SerializeField] private float alphaDestello = 0.8f;

    [Header("Anillo (tamaño en anchos del bloque)")]
    [SerializeField] private float duracionAnillo = 0.45f;
    [SerializeField] private float tamanoAnilloPerfecto = 1.6f;
    [SerializeField] private float tamanoAnilloCasi = 1.1f;

    [Header("Chispas (solo en perfecto)")]
    [SerializeField] private int cantidadChispas = 10;
    [SerializeField] private float velocidadChispas = 4f;
    [SerializeField] private float duracionChispas = 0.5f;
    [SerializeField] private float tamanoChispa = 0.12f;

    private Sprite spriteAnillo;
    private Sprite spriteCuadro;

    private void Awake()
    {
        // los sprites se hacen por código para no depender de assets extra
        spriteAnillo = CrearSpriteAnillo(128, 0.12f);
        spriteCuadro = CrearSpriteCuadro();
    }

    private void OnEnable()
    {
        torre.AlAterrizar += Aterrizo;
    }

    private void OnDisable()
    {
        torre.AlAterrizar -= Aterrizo;
    }

    private void Aterrizo(Torre.Resultado resultado, Torre.Calidad calidad, Bloque bloque)
    {
        if (calidad == Torre.Calidad.Normal || bloque == null) return;

        bool perfecto = calidad == Torre.Calidad.Perfecto;
        StartCoroutine(Destello(bloque));
        StartCoroutine(Anillo(bloque.transform.position, bloque.Ancho * (perfecto ? tamanoAnilloPerfecto : tamanoAnilloCasi)));
        if (perfecto)
        {
            StartCoroutine(Chispas(bloque.transform.position));
        }
    }

    // el destello es un hijo del bloque, así hereda su escala y no hay que tocarla
    private IEnumerator Destello(Bloque bloque)
    {
        SpriteRenderer renderBloque = bloque.GetComponent<SpriteRenderer>();

        GameObject obj = new GameObject("Destello");
        obj.transform.SetParent(bloque.transform, false);
        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = renderBloque.sprite;
        render.sortingOrder = renderBloque.sortingOrder + 1;

        float t = 0f;
        while (t < duracionDestello && obj != null)
        {
            t += Time.deltaTime;
            render.color = new Color(1f, 1f, 1f, alphaDestello * (1f - t / duracionDestello));
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    private IEnumerator Anillo(Vector3 posicion, float tamanoFinal)
    {
        GameObject obj = new GameObject("Anillo");
        obj.transform.position = posicion;
        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = spriteAnillo;
        render.sortingOrder = 10;

        float t = 0f;
        while (t < duracionAnillo)
        {
            t += Time.deltaTime;
            float avance = t / duracionAnillo;
            float tamano = Mathf.Lerp(tamanoFinal * 0.3f, tamanoFinal, 1f - (1f - avance) * (1f - avance));
            obj.transform.localScale = new Vector3(tamano, tamano, 1f);

            Color c = colorEfecto;
            c.a = 1f - avance;
            render.color = c;
            yield return null;
        }

        Destroy(obj);
    }

    private IEnumerator Chispas(Vector3 posicion)
    {
        Transform[] chispas = new Transform[cantidadChispas];
        SpriteRenderer[] renders = new SpriteRenderer[cantidadChispas];
        Vector3[] direcciones = new Vector3[cantidadChispas];

        for (int i = 0; i < cantidadChispas; i++)
        {
            GameObject obj = new GameObject("Chispa");
            obj.transform.position = posicion;
            obj.transform.localScale = Vector3.one * tamanoChispa;
            renders[i] = obj.AddComponent<SpriteRenderer>();
            renders[i].sprite = spriteCuadro;
            renders[i].color = colorEfecto;
            renders[i].sortingOrder = 11;
            chispas[i] = obj.transform;

            float angulo = (360f / cantidadChispas) * i + Random.Range(-15f, 15f);
            direcciones[i] = Quaternion.Euler(0f, 0f, angulo) * Vector3.right * Random.Range(0.6f, 1f);
        }

        float t = 0f;
        while (t < duracionChispas)
        {
            t += Time.deltaTime;
            float avance = t / duracionChispas;
            for (int i = 0; i < cantidadChispas; i++)
            {
                chispas[i].position += direcciones[i] * velocidadChispas * (1f - avance) * Time.deltaTime;
                chispas[i].localScale = Vector3.one * tamanoChispa * (1f - avance);
                Color c = colorEfecto;
                c.a = 1f - avance;
                renders[i].color = c;
            }
            yield return null;
        }

        for (int i = 0; i < cantidadChispas; i++)
        {
            Destroy(chispas[i].gameObject);
        }
    }

    // círculo hueco blanco de 1 unidad de diámetro
    private Sprite CrearSpriteAnillo(int tamano, float grosor)
    {
        Texture2D tex = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        float centro = (tamano - 1) / 2f;
        float radioExterior = tamano / 2f;
        float radioInterior = radioExterior * (1f - grosor * 2f);

        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro));
                // un pixel de borde suave para que no se vea dentado
                float alpha = Mathf.Clamp01(radioExterior - d) * Mathf.Clamp01(d - radioInterior);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tamano, tamano), new Vector2(0.5f, 0.5f), tamano);
    }

    private Sprite CrearSpriteCuadro()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixeles = new Color[16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
        tex.SetPixels(pixeles);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
