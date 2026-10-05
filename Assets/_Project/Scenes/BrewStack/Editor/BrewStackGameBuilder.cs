using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class BrewStackGameBuilder
{
    private const string CARPETA = "Assets/_Project/Scenes/BrewStack";
    private const string RUTA_ESCENA = CARPETA + "/BS_Scenes/BS_Game.unity";
    private const string RUTA_PREFAB_BLOQUE = CARPETA + "/BS_Prefabs/Bloque.prefab";
    private const string RUTA_SPRITE_BLOQUE = CARPETA + "/BS_Sprites/bloque_blanco.png";
    private const string RUTA_FONDO = CARPETA + "/BS_Sprites/fondo_gradiente_espacial.png";
    private const string RUTA_BORDE_BOTON = CARPETA + "/BS_Sprites/borde_boton.png";
    private const string RUTA_FUENTE = CARPETA + "/BS_Fonts/Orbitron-Bold SDF.asset";
    private const string RUTA_PREGUNTAS = CARPETA + "/BS_Data/preguntas.json";

    private const float ANCHO_BASE = 3f;
    private const float ALTO_BASE = 0.5f;
    private const float TAMANO_CAMARA = 6f;

    private static readonly Color colorFondo = new Color(0.04f, 0.07f, 0.16f);
    private static readonly Color colorBase = new Color(0.88f, 0.59f, 0.23f);
    private static readonly Color colorPanel = new Color(0.024f, 0.043f, 0.094f, 0.84f);
    private static readonly Color colorBordeCeleste = new Color(0.62f, 0.85f, 1f, 0.35f);
    private static readonly Color colorTexto = new Color(0.81f, 0.9f, 1f);
    private static readonly Color colorCeleste = new Color(0.62f, 0.85f, 1f);

    [MenuItem("BrewStack/Generar escena de juego")]
    public static void GenerarEscena()
    {
        if (File.Exists(RUTA_ESCENA))
        {
            bool seguir = EditorUtility.DisplayDialog("Generar BS_Game",
                "La escena BS_Game ya existe y se va a reemplazar por una nueva. Los cambios hechos a mano en esa escena se pierden.",
                "Reemplazar", "Cancelar");
            if (!seguir) return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // la escena se crea desde cero cada vez, así no se duplican objetos
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Sprite spriteBloque = CrearSpriteBloque();
        Bloque prefabBloque = CrearPrefabBloque(spriteBloque);

        CrearCamara();
        CrearBase(spriteBloque);
        Torre torre = CrearTorre(prefabBloque);

        Transform canvas = CrearCanvas();
        CrearEventSystem();
        TriviaUI trivia = CrearTrivia(canvas, spriteBloque);

        CrearPruebaFisica(torre, trivia);

        EditorSceneManager.SaveScene(escena, RUTA_ESCENA);
        AssetDatabase.Refresh();
        Debug.Log("BrewStack: escena BS_Game generada en " + RUTA_ESCENA);
    }

    // cuadro blanco de 1x1 unidad, el color se lo pone cada bloque
    private static Sprite CrearSpriteBloque()
    {
        if (!File.Exists(RUTA_SPRITE_BLOQUE))
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixeles = new Color[16];
            for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
            tex.SetPixels(pixeles);
            File.WriteAllBytes(RUTA_SPRITE_BLOQUE, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(RUTA_SPRITE_BLOQUE);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(RUTA_SPRITE_BLOQUE);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 4;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_BLOQUE);
    }

    private static Bloque CrearPrefabBloque(Sprite sprite)
    {
        GameObject obj = new GameObject("Bloque");
        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = sprite;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        obj.AddComponent<BoxCollider2D>();

        Bloque bloque = obj.AddComponent<Bloque>();
        SerializedObject so = new SerializedObject(bloque);
        so.FindProperty("render").objectReferenceValue = render;
        so.FindProperty("rb").objectReferenceValue = rb;
        so.ApplyModifiedPropertiesWithoutUndo();

        // si ya existe se sobrescribe, Unity conserva su GUID
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, RUTA_PREFAB_BLOQUE);
        Object.DestroyImmediate(obj);
        return prefab.GetComponent<Bloque>();
    }

    private static void CrearCamara()
    {
        GameObject obj = new GameObject("Main Camera");
        obj.tag = "MainCamera";
        obj.transform.position = new Vector3(0f, 3f, -10f);

        Camera cam = obj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = TAMANO_CAMARA;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = colorFondo;
        obj.AddComponent<AudioListener>();

        // el fondo va pegado a la cámara para que la siga cuando suba
        Sprite fondo = CargarPrimerSprite(RUTA_FONDO);
        if (fondo == null) return;

        GameObject objFondo = new GameObject("Fondo");
        objFondo.transform.SetParent(obj.transform, false);
        objFondo.transform.localPosition = new Vector3(0f, 0f, 20f);

        SpriteRenderer render = objFondo.AddComponent<SpriteRenderer>();
        render.sprite = fondo;
        render.sortingOrder = -100;

        // que cubra la vista aunque la pantalla sea muy ancha
        float alto = TAMANO_CAMARA * 2f;
        float ancho = alto * 3f;
        Vector2 tamSprite = fondo.bounds.size;
        objFondo.transform.localScale = new Vector3(ancho / tamSprite.x, alto / tamSprite.y, 1f);
    }

    private static Sprite CargarPrimerSprite(string ruta)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ruta))
        {
            if (asset is Sprite sprite) return sprite;
        }
        return null;
    }

    private static void CrearBase(Sprite sprite)
    {
        GameObject obj = new GameObject("Base");
        obj.transform.position = new Vector3(0f, -ALTO_BASE / 2f, 0f);
        obj.transform.localScale = new Vector3(ANCHO_BASE, ALTO_BASE, 1f);

        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = sprite;
        render.color = colorBase;
        obj.AddComponent<BoxCollider2D>();
    }

    private static Torre CrearTorre(Bloque prefabBloque)
    {
        // la torre va justo encima de la base, su pivote se crea al iniciar
        GameObject obj = new GameObject("Torre");
        obj.transform.position = Vector3.zero;

        Torre torre = obj.AddComponent<Torre>();
        SerializedObject so = new SerializedObject(torre);
        so.FindProperty("prefabBloque").objectReferenceValue = prefabBloque;
        so.FindProperty("anchoBase").floatValue = ANCHO_BASE;
        so.ApplyModifiedPropertiesWithoutUndo();
        return torre;
    }

    private static void CrearPruebaFisica(Torre torre, TriviaUI trivia)
    {
        GameObject obj = new GameObject("PruebaFisica");
        PruebaFisica prueba = obj.AddComponent<PruebaFisica>();

        SerializedObject so = new SerializedObject(prueba);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.FindProperty("trivia").objectReferenceValue = trivia;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform CrearCanvas()
    {
        GameObject obj = new GameObject("Canvas");
        obj.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        obj.AddComponent<GraphicRaycaster>();
        return obj.transform;
    }

    private static void CrearEventSystem()
    {
        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        InputSystemUIInputModule modulo = obj.AddComponent<InputSystemUIInputModule>();
        modulo.AssignDefaultActions();
    }

    private static TriviaUI CrearTrivia(Transform canvas, Sprite spriteBlanco)
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE);
        Sprite borde = CargarPrimerSprite(RUTA_BORDE_BOTON);

        // fondo oscuro que tapa el juego mientras se contesta
        GameObject panel = CrearImagen("PanelTrivia", canvas, colorPanel, null);
        Estirar(panel, Vector2.zero, Vector2.one);

        GameObject caja = CrearImagen("Caja", panel.transform, colorBordeCeleste, borde);
        Estirar(caja, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.88f));

        TMP_Text pregunta = CrearTexto("TextoPregunta", caja.transform, "Pregunta", 44f, colorTexto, fuente);
        Estirar(pregunta.gameObject, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.94f));

        GameObject listaBotones = new GameObject("Opciones", typeof(RectTransform));
        listaBotones.transform.SetParent(caja.transform, false);
        Estirar(listaBotones, new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.64f));
        VerticalLayoutGroup layout = listaBotones.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        Button[] botones = new Button[4];
        TMP_Text[] textos = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject objBoton = CrearImagen("Opcion" + (i + 1), listaBotones.transform, colorBordeCeleste, borde);
            botones[i] = objBoton.AddComponent<Button>();
            botones[i].targetGraphic = objBoton.GetComponent<Image>();
            textos[i] = CrearTexto("Texto", objBoton.transform, "Opción", 32f, colorTexto, fuente);
            Estirar(textos[i].gameObject, Vector2.zero, Vector2.one);
            textos[i].rectTransform.offsetMin = new Vector2(24f, 0f);
            textos[i].rectTransform.offsetMax = new Vector2(-24f, 0f);
        }

        GameObject fondoBarra = CrearImagen("BarraTiempoFondo", caja.transform, new Color(1f, 1f, 1f, 0.1f), spriteBlanco);
        Estirar(fondoBarra, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.08f));
        GameObject relleno = CrearImagen("BarraTiempo", fondoBarra.transform, colorCeleste, spriteBlanco);
        Estirar(relleno, Vector2.zero, Vector2.one);

        GameObject obj = new GameObject("Trivia");
        BancoPreguntas banco = obj.AddComponent<BancoPreguntas>();
        SerializedObject soBanco = new SerializedObject(banco);
        soBanco.FindProperty("archivoPreguntas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(RUTA_PREGUNTAS);
        soBanco.ApplyModifiedPropertiesWithoutUndo();

        TriviaUI trivia = obj.AddComponent<TriviaUI>();
        SerializedObject so = new SerializedObject(trivia);
        so.FindProperty("banco").objectReferenceValue = banco;
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("textoPregunta").objectReferenceValue = pregunta;
        so.FindProperty("barraTiempo").objectReferenceValue = relleno.GetComponent<RectTransform>();
        so.FindProperty("imagenBarra").objectReferenceValue = relleno.GetComponent<Image>();

        SerializedProperty propBotones = so.FindProperty("botones");
        SerializedProperty propTextos = so.FindProperty("textosBotones");
        propBotones.arraySize = 4;
        propTextos.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            propBotones.GetArrayElementAtIndex(i).objectReferenceValue = botones[i];
            propTextos.GetArrayElementAtIndex(i).objectReferenceValue = textos[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        return trivia;
    }

    private static GameObject CrearImagen(string nombre, Transform padre, Color color, Sprite sprite)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(padre, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        if (sprite != null && sprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
        }
        return obj;
    }

    private static TMP_Text CrearTexto(string nombre, Transform padre, string texto, float tamano, Color color, TMP_FontAsset fuente)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(padre, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        if (fuente != null) tmp.font = fuente;
        return tmp;
    }

    // ocupa el área entre las dos esquinas, en porcentaje del padre
    private static void Estirar(GameObject obj, Vector2 min, Vector2 max)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
