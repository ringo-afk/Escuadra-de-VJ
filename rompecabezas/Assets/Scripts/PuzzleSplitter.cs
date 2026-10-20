using System.Collections.Generic;
using UnityEngine;

public class PiezasRompecabezas
{
    public Sprite[] piezas;
    public Sprite[] siluetas;
    public float proporcion = 1f;
}

public static class PuzzleSplitter
{
    private const float MargenRel = 0.27f;
    private const float RadioRel = 0.15f;
    private const float DesplazamientoRel = 0.10f;

    public static Rect AreaUtil(Texture2D tex, int n)
    {
        int lado = Mathf.Min(tex.width, tex.height);
        int celda = Mathf.Max(1, lado / n);
        int usado = celda * n;
        int x0 = (tex.width - usado) / 2;
        int y0 = (tex.height - usado) / 2;
        return new Rect(x0, y0, usado, usado);
    }

    public static Texture2D Preparar(Texture2D origen, int maxLado)
    {
        int w = origen.width;
        int h = origen.height;
        float escala = Mathf.Min(1f, (float)maxLado / Mathf.Max(w, h));
        int nw = Mathf.Max(1, Mathf.RoundToInt(w * escala));
        int nh = Mathf.Max(1, Mathf.RoundToInt(h * escala));

        RenderTexture rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(origen, rt);

        RenderTexture previa = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D copia = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        copia.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
        copia.Apply(false, false);
        RenderTexture.active = previa;
        RenderTexture.ReleaseTemporary(rt);

        copia.name = origen.name;
        copia.wrapMode = TextureWrapMode.Clamp;
        copia.filterMode = FilterMode.Bilinear;
        return copia;
    }

    public static Sprite[] Cortar(Texture2D tex, int n)
    {
        Rect area = AreaUtil(tex, n);
        int celda = Mathf.RoundToInt(area.width / n);
        Sprite[] piezas = new Sprite[n * n];

        for (int fila = 0; fila < n; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                float x = area.x + col * celda;
                float y = area.y + (n - 1 - fila) * celda;

                Sprite s = Sprite.Create(tex, new Rect(x, y, celda, celda),
                                         new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                s.name = tex.name + "_" + n + "x" + n + "_" + (fila * n + col);
                piezas[fila * n + col] = s;
            }
        }
        return piezas;
    }

    public static PiezasRompecabezas CortarJigsaw(Texture2D tex, int n)
    {
        Rect area = AreaUtil(tex, n);
        int celda = Mathf.RoundToInt(area.width / n);
        int margen = Mathf.CeilToInt(celda * MargenRel);
        int lado = celda + margen * 2;
        float radio = celda * RadioRel;
        float hondo = radio * 0.7f;
        float mitad = celda * 0.5f;
        float grosor = Mathf.Max(2f, celda * 0.014f);

        Color32[] fuente = tex.GetPixels32();
        int fw = tex.width;
        int fh = tex.height;

        System.Random azar = new System.Random();
        int[,] vert = new int[n, Mathf.Max(1, n - 1)];
        int[,] horiz = new int[Mathf.Max(1, n - 1), n];
        float[,] despV = new float[n, Mathf.Max(1, n - 1)];
        float[,] despH = new float[Mathf.Max(1, n - 1), n];

        for (int f = 0; f < n; f++)
            for (int c = 0; c < n - 1; c++)
            {
                vert[f, c] = azar.Next(2) == 0 ? 1 : -1;
                despV[f, c] = ((float)azar.NextDouble() * 2f - 1f) * DesplazamientoRel * celda;
            }
        for (int f = 0; f < n - 1; f++)
            for (int c = 0; c < n; c++)
            {
                horiz[f, c] = azar.Next(2) == 0 ? 1 : -1;
                despH[f, c] = ((float)azar.NextDouble() * 2f - 1f) * DesplazamientoRel * celda;
            }

        PiezasRompecabezas res = new PiezasRompecabezas
        {
            piezas = new Sprite[n * n],
            siluetas = new Sprite[n * n],
            proporcion = (float)lado / celda
        };

        Vector2[] centros = new Vector2[4];
        int[] signos = new int[4];

        for (int fila = 0; fila < n; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                signos[0] = col < n - 1 ? vert[fila, col] : 0;
                signos[1] = col > 0 ? -vert[fila, col - 1] : 0;
                signos[2] = fila > 0 ? -horiz[fila - 1, col] : 0;
                signos[3] = fila < n - 1 ? horiz[fila, col] : 0;

                float offDer = col < n - 1 ? despV[fila, col] : 0f;
                float offIzq = col > 0 ? despV[fila, col - 1] : 0f;
                float offArr = fila > 0 ? despH[fila - 1, col] : 0f;
                float offAba = fila < n - 1 ? despH[fila, col] : 0f;

                centros[0] = new Vector2(mitad + hondo * signos[0], offDer);
                centros[1] = new Vector2(-mitad - hondo * signos[1], offIzq);
                centros[2] = new Vector2(offArr, mitad + hondo * signos[2]);
                centros[3] = new Vector2(offAba, -mitad - hondo * signos[3]);

                int origenX = Mathf.RoundToInt(area.x) + col * celda;
                int origenY = Mathf.RoundToInt(area.y) + (n - 1 - fila) * celda;

                Color32[] pix = new Color32[lado * lado];
                Color32[] sil = new Color32[lado * lado];

                for (int py = 0; py < lado; py++)
                {
                    for (int px = 0; px < lado; px++)
                    {
                        float x = px + 0.5f - lado * 0.5f;
                        float y = py + 0.5f - lado * 0.5f;

                        float qx = Mathf.Abs(x) - mitad;
                        float qy = Mathf.Abs(y) - mitad;
                        float d = Mathf.Min(Mathf.Max(qx, qy), 0f)
                                  + Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));

                        for (int e = 0; e < 4; e++)
                        {
                            if (signos[e] == 0) continue;
                            float dx = x - centros[e].x;
                            float dy = y - centros[e].y;
                            float dc = Mathf.Sqrt(dx * dx + dy * dy) - radio;
                            d = signos[e] > 0 ? Mathf.Min(d, dc) : Mathf.Max(d, -dc);
                        }

                        float alfa = Mathf.Clamp01(0.5f - d);

                        int sx = Mathf.Clamp(origenX - margen + px, 0, fw - 1);
                        int sy = Mathf.Clamp(origenY - margen + py, 0, fh - 1);
                        Color32 c0 = fuente[sy * fw + sx];

                        float borde = Mathf.Clamp01(1f + d / grosor);
                        float oscuro = 1f - 0.45f * borde;
                        int idx = py * lado + px;
                        byte a = (byte)Mathf.RoundToInt(alfa * 255f);
                        pix[idx] = new Color32((byte)(c0.r * oscuro), (byte)(c0.g * oscuro), (byte)(c0.b * oscuro), a);

                        float relleno = d > -grosor ? 1f : 0.5f;
                        sil[idx] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alfa * relleno * 255f));
                    }
                }

                res.piezas[fila * n + col] = CrearSprite(pix, lado, tex.name + "_jig_" + n + "x" + n + "_" + (fila * n + col));
                res.siluetas[fila * n + col] = CrearSprite(sil, lado, tex.name + "_sil_" + (fila * n + col));
            }
        }
        return res;
    }

    private static Sprite CrearSprite(Color32[] pix, int lado, string nombre)
    {
        Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        t.SetPixels32(pix);
        t.Apply(false, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.name = nombre;

        Sprite s = Sprite.Create(t, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.name = nombre;
        return s;
    }

    public static Sprite FormaDemo(int lado, int signoDer, int signoIzq, Color color)
    {
        float celda = lado * 0.62f;
        float mitad = celda * 0.5f;
        float radio = celda * 0.17f;
        float hondo = radio * 0.7f;
        float grosor = Mathf.Max(2f, lado * 0.02f);
        Vector2 cDer = new Vector2(mitad + hondo * signoDer, 0f);
        Vector2 cIzq = new Vector2(-mitad - hondo * signoIzq, 0f);

        Color32[] pix = new Color32[lado * lado];
        for (int py = 0; py < lado; py++)
        {
            for (int px = 0; px < lado; px++)
            {
                float x = px + 0.5f - lado * 0.5f;
                float y = py + 0.5f - lado * 0.5f;
                float qx = Mathf.Abs(x) - mitad;
                float qy = Mathf.Abs(y) - mitad;
                float d = Mathf.Min(Mathf.Max(qx, qy), 0f)
                          + Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));

                if (signoDer != 0)
                {
                    float dc = Mathf.Sqrt((x - cDer.x) * (x - cDer.x) + (y - cDer.y) * (y - cDer.y)) - radio;
                    d = signoDer > 0 ? Mathf.Min(d, dc) : Mathf.Max(d, -dc);
                }
                if (signoIzq != 0)
                {
                    float dc = Mathf.Sqrt((x - cIzq.x) * (x - cIzq.x) + (y - cIzq.y) * (y - cIzq.y)) - radio;
                    d = signoIzq > 0 ? Mathf.Min(d, dc) : Mathf.Max(d, -dc);
                }

                float alfa = Mathf.Clamp01(0.5f - d);
                float borde = Mathf.Clamp01(1f + d / grosor);
                float oscuro = 1f - 0.35f * borde;
                pix[py * lado + px] = new Color(color.r * oscuro, color.g * oscuro, color.b * oscuro, alfa * color.a);
            }
        }
        return CrearSprite(pix, lado, "demo_forma");
    }

    public static Sprite ImagenCompleta(Texture2D tex, int n)
    {
        Sprite s = Sprite.Create(tex, AreaUtil(tex, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.name = tex.name + "_modelo";
        return s;
    }

    public static void Revolver<T>(IList<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = lista[i];
            lista[i] = lista[j];
            lista[j] = tmp;
        }
    }
}
