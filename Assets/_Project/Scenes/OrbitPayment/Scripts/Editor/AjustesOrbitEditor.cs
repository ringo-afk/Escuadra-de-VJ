#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Crea solo (una vez) el prefab Resources/AjustesOrbit con el boton de ajustes y su panel.
// Si algo sale mal o cambias las imagenes: menu Orbit > Reconstruir prefab de Ajustes.
[InitializeOnLoad]
public static class AjustesOrbitEditor
{
    private const string Carpeta = "Assets/_Project/Scenes/OrbitPayment";
    private const string RutaPrefab = Carpeta + "/Resources/AjustesOrbit.prefab";

    static AjustesOrbitEditor()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
                Construir();
        };
    }

    [MenuItem("Orbit/Reconstruir prefab de Ajustes")]
    public static void Construir()
    {
        Sprite engrane = PrepararSprite(Carpeta + "/UI/SettingsButton.png");
        Sprite cerrar = PrepararSprite(Carpeta + "/UI/SettingsClose.png");
        Sprite audioOn = PrepararSprite(Carpeta + "/UI/AudioOn.png");
        Sprite audioOff = PrepararSprite(Carpeta + "/UI/AudioOff.png");

        if (engrane == null || cerrar == null || audioOn == null || audioOff == null)
        {
            Debug.LogWarning("[AjustesOrbit] Faltan imagenes en " + Carpeta + "/UI; no se creo el prefab.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(Carpeta + "/Resources"))
            AssetDatabase.CreateFolder(Carpeta, "Resources");

        // Raiz: ocupa toda la pantalla pero no bloquea clics (solo sus hijos)
        GameObject raiz = Nuevo("AjustesOrbit", null);
        Estirar(raiz.GetComponent<RectTransform>());
        AjustesOrbit ajustes = raiz.AddComponent<AjustesOrbit>();

        // Boton de engrane, abajo a la derecha
        GameObject botonAjustes = Nuevo("BotonAjustes", raiz.transform);
        RectTransform rtAjustes = botonAjustes.GetComponent<RectTransform>();
        Anclar(rtAjustes, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(110f, 100f));
        Image imgAjustes = botonAjustes.AddComponent<Image>();
        imgAjustes.sprite = engrane;
        imgAjustes.preserveAspect = true;
        Button btnAjustes = botonAjustes.AddComponent<Button>();
        btnAjustes.targetGraphic = imgAjustes;

        // Panel a pantalla completa (fondo oscuro que bloquea clics al juego)
        GameObject panel = Nuevo("Panel", raiz.transform);
        Estirar(panel.GetComponent<RectTransform>());
        Image fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.7f);

        // Marco central
        GameObject marco = Nuevo("Marco", panel.transform);
        Anclar(marco.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 520f));
        Image imgMarco = marco.AddComponent<Image>();
        imgMarco.color = new Color(0.07f, 0.03f, 0.2f, 1f);
        Outline borde = marco.AddComponent<Outline>();
        borde.effectColor = new Color(0.62f, 0.3f, 1f, 1f);
        borde.effectDistance = new Vector2(5f, -5f);

        TMP_Text titulo = NuevoTexto("Titulo", marco.transform, "AJUSTES", 64f, new Vector2(0f, -70f), new Vector2(600f, 90f));
        titulo.color = new Color(0.9f, 0.82f, 1f, 1f);
        titulo.fontStyle = FontStyles.Bold;
        titulo.rectTransform.anchorMin = titulo.rectTransform.anchorMax = new Vector2(0.5f, 1f);

        // Boton de audio (alterna entre activado y silenciado)
        GameObject botonAudio = Nuevo("BotonAudio", marco.transform);
        Anclar(botonAudio.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(210f, 190f));
        Image imgAudio = botonAudio.AddComponent<Image>();
        imgAudio.sprite = audioOn;
        imgAudio.preserveAspect = true;
        Button btnAudio = botonAudio.AddComponent<Button>();
        btnAudio.targetGraphic = imgAudio;

        TMP_Text textoAudio = NuevoTexto("TextoAudio", marco.transform, "Audio: activado", 40f, new Vector2(0f, -155f), new Vector2(600f, 60f));
        textoAudio.color = Color.white;
        textoAudio.rectTransform.anchorMin = textoAudio.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // Boton para cerrar (X), esquina superior derecha del marco
        GameObject botonCerrar = Nuevo("BotonCerrar", marco.transform);
        Anclar(botonCerrar.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(110f, 100f));
        Image imgCerrar = botonCerrar.AddComponent<Image>();
        imgCerrar.sprite = cerrar;
        imgCerrar.preserveAspect = true;
        Button btnCerrar = botonCerrar.AddComponent<Button>();
        btnCerrar.targetGraphic = imgCerrar;

        // Conexiones
        ajustes.panel = panel;
        ajustes.iconoAudio = imgAudio;
        ajustes.spriteAudioOn = audioOn;
        ajustes.spriteAudioOff = audioOff;
        ajustes.textoAudio = textoAudio;

        UnityEventTools.AddPersistentListener(btnAjustes.onClick, ajustes.Abrir);
        UnityEventTools.AddPersistentListener(btnCerrar.onClick, ajustes.Cerrar);
        UnityEventTools.AddPersistentListener(btnAudio.onClick, ajustes.AlternarAudio);

        panel.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("[AjustesOrbit] Prefab creado en " + RutaPrefab);
    }

    // Convierte la imagen en Sprite (una sola pieza) y devuelve el sprite
    private static Sprite PrepararSprite(string ruta)
    {
        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null)
            return null;

        if (importador.textureType != TextureImporterType.Sprite
            || importador.spriteImportMode != SpriteImportMode.Single)
        {
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.alphaIsTransparency = true;
            importador.mipmapEnabled = false;
            importador.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static GameObject Nuevo(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        if (padre != null)
            objeto.transform.SetParent(padre, false);
        return objeto;
    }

    private static TMP_Text NuevoTexto(string nombre, Transform padre, string texto, float tamano, Vector2 posicion, Vector2 medida)
    {
        GameObject objeto = Nuevo(nombre, padre);
        TextMeshProUGUI tmp = objeto.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        RectTransform rt = tmp.rectTransform;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = medida;
        return tmp;
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 medida)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = medida;
    }
}
#endif
