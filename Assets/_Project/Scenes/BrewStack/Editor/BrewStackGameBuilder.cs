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
    private const string RUTA_SPRITE_BLOQUE_BORDE = CARPETA + "/BS_Sprites/bloque_borde.png";
    private const string RUTA_SPRITE_BASE = CARPETA + "/BS_Sprites/base_torre.png";
    private const string RUTA_SPRITE_BRILLO = CARPETA + "/BS_Sprites/brillo_suave.png";
    private const string RUTA_ESTRELLA = CARPETA + "/BS_Sprites/punto_estrella_nitida.png";
    private const string RUTA_ESTRELLA_FUGAZ = CARPETA + "/BS_Sprites/estrella_fugaz.png";
    // la misma música que usa BS_Menu, por referencia
    private const string RUTA_MUSICA = CARPETA + "/BS_Audio/417884__andrewkn__surrealism-ambient-mix.wav";
    private const string RUTA_BORDE_BOTON = CARPETA + "/BS_Sprites/borde_boton.png";
    private const string RUTA_FUENTE = CARPETA + "/BS_Fonts/Orbitron-Bold SDF.asset";
    private const string RUTA_PREGUNTAS = CARPETA + "/BS_Data/preguntas.json";
    private const string RUTA_MENU = CARPETA + "/BS_Scenes/BS_Menu.unity";
    private const string NOMBRE_ESCENA_JUEGO = "BS_Game";

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

        // el cuadro blanco se sigue usando en las barras del HUD y la trivia
        Sprite spriteBloque = CrearSpriteBloque();
        Bloque prefabBloque = CrearPrefabBloque(CrearSpriteBloqueBorde());

        Camera cam = CrearCamara();
        CrearBase();
        Torre torre = CrearTorre(prefabBloque);
        AgregarScriptsCamara(cam, torre);
        CrearFondoEstrellas(cam);

        // el orden importa: lo que se crea después queda encima
        Transform canvas = CrearCanvas();
        CrearEventSystem();
        HUDJuego hud = CrearHUD(canvas, torre, spriteBloque);
        TriviaUI trivia = CrearTrivia(canvas, spriteBloque);
        ResultadoFinalUI resultado = CrearResultado(canvas);

        TextosFlotantes textos = CrearTextosFlotantes();
        CrearJuegoManager(torre, trivia, hud, resultado, textos);
        CrearEfectos(torre);
        CrearSonidos(torre, trivia, resultado);
        CrearMusica(resultado);

        EditorSceneManager.SaveScene(escena, RUTA_ESCENA);
        AssetDatabase.Refresh();
        Debug.Log("BrewStack: escena BS_Game generada en " + RUTA_ESCENA);
    }

    // separado de GenerarEscena porque toca Build Settings, que es compartido con todo el equipo
    [MenuItem("BrewStack/Registrar BS_Game en el proyecto")]
    public static void RegistrarEscena()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("BrewStack: sal de Play antes de registrar la escena");
            return;
        }
        if (!File.Exists(RUTA_ESCENA))
        {
            Debug.LogError("BrewStack: no existe " + RUTA_ESCENA + ", primero usa Generar escena de juego");
            return;
        }

        bool cambioBuild = AgregarABuildSettings();
        bool cambioMenu = CambiarEscenaDelMenu();

        if (!cambioBuild && !cambioMenu)
        {
            Debug.Log("BrewStack: no se hizo nada, BS_Game ya estaba registrada y BS_Menu ya apuntaba a ella");
        }
    }

    private static bool AgregarABuildSettings()
    {
        EditorBuildSettingsScene[] escenas = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene e in escenas)
        {
            if (e.path != RUTA_ESCENA) continue;

            if (!e.enabled)
            {
                Debug.LogWarning("BrewStack: BS_Game ya está en Build Settings pero desactivada. No la toqué, actívala a mano si hace falta.");
            }
            return false;
        }

        // se agrega al final, las demás se quedan igual y en el mismo orden
        EditorBuildSettingsScene[] nuevas = new EditorBuildSettingsScene[escenas.Length + 1];
        escenas.CopyTo(nuevas, 0);
        nuevas[escenas.Length] = new EditorBuildSettingsScene(RUTA_ESCENA, true);
        EditorBuildSettings.scenes = nuevas;

        Debug.Log($"BrewStack: se agregó {RUTA_ESCENA} a Build Settings en la posición {escenas.Length}");
        return true;
    }

    private static bool CambiarEscenaDelMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("BrewStack: se canceló, BS_Menu no se modificó");
            return false;
        }

        var menu = EditorSceneManager.GetSceneByPath(RUTA_MENU);
        bool yaEstabaAbierta = menu.isLoaded;

        // si tiene cambios sin guardar no la toco, para no guardarlos sin querer
        if (yaEstabaAbierta && menu.isDirty)
        {
            Debug.LogError("BrewStack: BS_Menu tiene cambios sin guardar. Guárdala o descártalos y vuelve a intentar.");
            return false;
        }
        if (!yaEstabaAbierta)
        {
            menu = EditorSceneManager.OpenScene(RUTA_MENU, OpenSceneMode.Additive);
        }

        int encontrados = 0;
        bool cambio = false;
        foreach (GameObject raiz in menu.GetRootGameObjects())
        {
            foreach (MenuManager manager in raiz.GetComponentsInChildren<MenuManager>(true))
            {
                encontrados++;
                SerializedObject so = new SerializedObject(manager);
                SerializedProperty prop = so.FindProperty("gameSceneName");
                if (prop.stringValue == NOMBRE_ESCENA_JUEGO) continue;

                string anterior = prop.stringValue;
                prop.stringValue = NOMBRE_ESCENA_JUEGO;
                so.ApplyModifiedPropertiesWithoutUndo();
                cambio = true;
                Debug.Log($"BrewStack: en BS_Menu, {manager.gameObject.name}.gameSceneName cambió de \"{anterior}\" a \"{NOMBRE_ESCENA_JUEGO}\"");
            }
        }

        if (encontrados == 0)
        {
            Debug.LogError("BrewStack: no se encontró ningún MenuManager en BS_Menu");
        }

        if (cambio)
        {
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            Debug.Log("BrewStack: se guardó BS_Menu");
        }

        if (!yaEstabaAbierta)
        {
            EditorSceneManager.CloseScene(menu, true);
        }
        return cambio;
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

    // el tamaño real lo pone Bloque.Configurar en el renderer y el collider, la escala se queda en 1
    private static Bloque CrearPrefabBloque(Sprite sprite)
    {
        GameObject obj = new GameObject("Bloque");
        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = sprite;
        render.drawMode = SpriteDrawMode.Sliced;
        render.size = new Vector2(1f, 0.5f);

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D colisionador = obj.AddComponent<BoxCollider2D>();
        colisionador.size = render.size;

        Bloque bloque = obj.AddComponent<Bloque>();
        SerializedObject so = new SerializedObject(bloque);
        so.FindProperty("render").objectReferenceValue = render;
        so.FindProperty("rb").objectReferenceValue = rb;
        so.FindProperty("colisionador").objectReferenceValue = colisionador;
        so.ApplyModifiedPropertiesWithoutUndo();

        // si ya existe se sobrescribe, Unity conserva su GUID
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, RUTA_PREFAB_BLOQUE);
        Object.DestroyImmediate(obj);
        return prefab.GetComponent<Bloque>();
    }

    private static Camera CrearCamara()
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
        return cam;
    }

    private static void AgregarScriptsCamara(Camera cam, Torre torre)
    {
        CamaraTorre seguimiento = cam.gameObject.AddComponent<CamaraTorre>();
        SerializedObject so = new SerializedObject(seguimiento);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.ApplyModifiedPropertiesWithoutUndo();

        FondoJuego fondo = cam.gameObject.AddComponent<FondoJuego>();
        SerializedObject soFondo = new SerializedObject(fondo);
        soFondo.FindProperty("torre").objectReferenceValue = torre;
        soFondo.FindProperty("colorAbajo").colorValue = colorFondo;
        soFondo.ApplyModifiedPropertiesWithoutUndo();
    }

    // las estrellas del menú en un canvas pegado a la cámara y detrás de todo,
    // así se quedan quietas en pantalla como si estuvieran muy lejos
    private static void CrearFondoEstrellas(Camera cam)
    {
        GameObject obj = new GameObject("CanvasFondo");
        obj.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 50f;
        canvas.sortingOrder = -100;

        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject estrellas = new GameObject("Estrellas", typeof(RectTransform));
        estrellas.layer = obj.layer;
        estrellas.transform.SetParent(obj.transform, false);
        Estirar(estrellas, Vector2.zero, Vector2.one);
        StarField campo = estrellas.AddComponent<StarField>();
        SerializedObject soCampo = new SerializedObject(campo);
        soCampo.FindProperty("container").objectReferenceValue = estrellas.GetComponent<RectTransform>();
        soCampo.FindProperty("starSprite").objectReferenceValue = CargarPrimerSprite(RUTA_ESTRELLA);
        // mismos valores que en BS_Menu
        soCampo.FindProperty("cantidadEstrellas").intValue = 40;
        soCampo.FindProperty("tamañoMinimo").floatValue = 2f;
        soCampo.FindProperty("tamañoMaximo").floatValue = 5f;
        soCampo.ApplyModifiedPropertiesWithoutUndo();

        GameObject fugaces = new GameObject("EstrellasFugaces", typeof(RectTransform));
        fugaces.layer = obj.layer;
        fugaces.transform.SetParent(obj.transform, false);
        Estirar(fugaces, Vector2.zero, Vector2.one);
        ShootingStars fugaz = fugaces.AddComponent<ShootingStars>();
        SerializedObject soFugaz = new SerializedObject(fugaz);
        soFugaz.FindProperty("container").objectReferenceValue = fugaces.GetComponent<RectTransform>();
        soFugaz.FindProperty("streakSprite").objectReferenceValue = CargarPrimerSprite(RUTA_ESTRELLA_FUGAZ);
        soFugaz.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite CargarPrimerSprite(string ruta)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ruta))
        {
            if (asset is Sprite sprite) return sprite;
        }
        return null;
    }

    private static void CrearBase()
    {
        GameObject obj = new GameObject("Base");
        obj.transform.position = new Vector3(0f, -ALTO_BASE / 2f, 0f);

        // en modo Sliced el tamaño va en el renderer y no en la escala, así el borde no se estira
        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = CrearSpriteBase();
        render.drawMode = SpriteDrawMode.Sliced;
        render.size = new Vector2(ANCHO_BASE, ALTO_BASE);

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(ANCHO_BASE, ALTO_BASE);

        // brillo falso: un sprite suave detrás del borde de arriba
        GameObject brillo = new GameObject("Brillo");
        brillo.transform.SetParent(obj.transform, false);
        brillo.transform.localPosition = new Vector3(0f, ALTO_BASE / 2f, 0f);
        brillo.transform.localScale = new Vector3(ANCHO_BASE * 1.8f, 1.4f, 1f);
        SpriteRenderer renderBrillo = brillo.AddComponent<SpriteRenderer>();
        renderBrillo.sprite = CrearSpriteBrillo();
        renderBrillo.color = new Color(colorBase.r, colorBase.g, colorBase.b, 0.35f);
        renderBrillo.sortingOrder = -1;
    }

    // 48x16 px: relleno azul oscuro con degradado, borde ámbar, filo de arriba brillante y dos luces celestes en las orillas
    private static Sprite CrearSpriteBase()
    {
        if (!File.Exists(RUTA_SPRITE_BASE))
        {
            int ancho = 48;
            int alto = 16;
            Color ambar = colorBase;
            Color ambarClaro = new Color(1f, 0.8f, 0.48f);
            Color arriba = new Color(0.1f, 0.14f, 0.26f);
            Color abajo = new Color(0.04f, 0.06f, 0.13f);

            Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    Color c = Color.Lerp(abajo, arriba, y / (alto - 1f));
                    bool orilla = x == 0 || x == ancho - 1 || y == 0;
                    if (orilla) c = ambar * 0.8f;
                    if (y >= alto - 2) c = ambarClaro;
                    else if (y == alto - 3) c = Color.Lerp(c, ambar, 0.4f);
                    bool luz = (x == 2 || x == 3 || x == ancho - 3 || x == ancho - 4) && (y == 7 || y == 8);
                    if (luz) c = colorCeleste;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            GuardarPng(tex, RUTA_SPRITE_BASE);
        }
        ConfigurarSprite(RUTA_SPRITE_BASE, 32, new Vector4(6, 3, 6, 5));
        return AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_BASE);
    }

    // 32x16 px en grises para que el color del bloque lo tiña: orilla clara, degradado adentro y un brillo arriba.
    // Con 32 px por unidad mide 1 x 0.5, el mismo alto del bloque, así el borde de 4 px nunca se estira
    private static Sprite CrearSpriteBloqueBorde()
    {
        if (!File.Exists(RUTA_SPRITE_BLOQUE_BORDE))
        {
            int ancho = 32;
            int alto = 16;
            Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    int distanciaOrilla = Mathf.Min(Mathf.Min(x, ancho - 1 - x), Mathf.Min(y, alto - 1 - y));
                    float v = Mathf.Lerp(0.55f, 0.8f, y / (alto - 1f));
                    if (distanciaOrilla == 0) v = 1f;
                    else if (distanciaOrilla == 1) v = 0.9f;
                    else if (y == alto - 4) v = 0.97f;
                    else if (y == alto - 5) v = Mathf.Lerp(v, 1f, 0.4f);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            GuardarPng(tex, RUTA_SPRITE_BLOQUE_BORDE);
        }
        ConfigurarSprite(RUTA_SPRITE_BLOQUE_BORDE, 32, new Vector4(4, 4, 4, 4));
        return AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_BLOQUE_BORDE);
    }

    // mancha blanca redonda que se desvanece hacia afuera, para brillos
    private static Sprite CrearSpriteBrillo()
    {
        if (!File.Exists(RUTA_SPRITE_BRILLO))
        {
            int tamano = 64;
            Texture2D tex = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
            float centro = (tamano - 1) / 2f;
            for (int y = 0; y < tamano; y++)
            {
                for (int x = 0; x < tamano; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro)) / (tamano / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            GuardarPng(tex, RUTA_SPRITE_BRILLO);
        }
        ConfigurarSprite(RUTA_SPRITE_BRILLO, 64, Vector4.zero);
        return AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_BRILLO);
    }

    private static void GuardarPng(Texture2D tex, string ruta)
    {
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
    }

    // se aplica cada vez, así un PNG que ya existía también queda bien configurado.
    // Full Rect hace falta para que el modo Sliced se dibuje bien
    private static void ConfigurarSprite(string ruta, float pixelesPorUnidad, Vector4 borde)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelesPorUnidad;
        importer.spriteBorder = borde;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        TextureImporterSettings ajustes = new TextureImporterSettings();
        importer.ReadTextureSettings(ajustes);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(ajustes);

        importer.SaveAndReimport();
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

    private static void CrearJuegoManager(Torre torre, TriviaUI trivia, HUDJuego hud, ResultadoFinalUI resultado, TextosFlotantes textos)
    {
        GameObject obj = new GameObject("JuegoManager");
        JuegoManager juego = obj.AddComponent<JuegoManager>();

        SerializedObject so = new SerializedObject(juego);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.FindProperty("trivia").objectReferenceValue = trivia;
        so.FindProperty("hud").objectReferenceValue = hud;
        so.FindProperty("resultado").objectReferenceValue = resultado;
        so.FindProperty("textosFlotantes").objectReferenceValue = textos;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextosFlotantes CrearTextosFlotantes()
    {
        GameObject obj = new GameObject("TextosFlotantes");
        TextosFlotantes textos = obj.AddComponent<TextosFlotantes>();

        SerializedObject so = new SerializedObject(textos);
        so.FindProperty("fuente").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE);
        so.ApplyModifiedPropertiesWithoutUndo();
        return textos;
    }

    private static void CrearMusica(ResultadoFinalUI resultado)
    {
        GameObject obj = new GameObject("Musica");
        MusicaJuego musica = obj.AddComponent<MusicaJuego>();

        SerializedObject so = new SerializedObject(musica);
        so.FindProperty("resultado").objectReferenceValue = resultado;
        so.FindProperty("musica").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_MUSICA);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CrearEfectos(Torre torre)
    {
        GameObject obj = new GameObject("Efectos");
        EfectoColocacion efecto = obj.AddComponent<EfectoColocacion>();

        SerializedObject so = new SerializedObject(efecto);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // los clips se dejan vacíos para que se generen por código, se pueden arrastrar después
    private static void CrearSonidos(Torre torre, TriviaUI trivia, ResultadoFinalUI resultado)
    {
        GameObject obj = new GameObject("Sonidos");
        SonidosJuego sonidos = obj.AddComponent<SonidosJuego>();

        SerializedObject so = new SerializedObject(sonidos);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.FindProperty("trivia").objectReferenceValue = trivia;
        so.FindProperty("resultado").objectReferenceValue = resultado;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HUDJuego CrearHUD(Transform canvas, Torre torre, Sprite spriteBlanco)
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE);

        GameObject hudObj = new GameObject("HUD", typeof(RectTransform));
        hudObj.layer = LayerMask.NameToLayer("UI");
        hudObj.transform.SetParent(canvas, false);
        Estirar(hudObj, Vector2.zero, Vector2.one);

        Sprite borde = CargarPrimerSprite(RUTA_BORDE_BOTON);

        // tres paneles arriba y uno abajo de ellos para la estabilidad, como las cajas del menú
        GameObject panelPuntos = CrearPanelHUD("PanelPuntos", hudObj.transform, new Vector2(0.02f, 0.9f), new Vector2(0.27f, 0.98f), borde);
        TMP_Text puntos = CrearTexto("TextoPuntos", panelPuntos.transform, "Puntos: 0", 34f, colorTexto, fuente);
        Estirar(puntos.gameObject, Vector2.zero, Vector2.one);

        GameObject panelAltura = CrearPanelHUD("PanelAltura", hudObj.transform, new Vector2(0.375f, 0.9f), new Vector2(0.625f, 0.98f), borde);
        TMP_Text altura = CrearTexto("TextoAltura", panelAltura.transform, "Altura: 0", 34f, colorTexto, fuente);
        Estirar(altura.gameObject, Vector2.zero, Vector2.one);

        GameObject panelTiempo = CrearPanelHUD("PanelTiempo", hudObj.transform, new Vector2(0.73f, 0.9f), new Vector2(0.98f, 0.98f), borde);
        TMP_Text tiempo = CrearTexto("TextoTiempo", panelTiempo.transform, "Tiempo: 3:00", 34f, colorTexto, fuente);
        Estirar(tiempo.gameObject, Vector2.zero, Vector2.one);

        GameObject panelEstabilidad = CrearPanelHUD("PanelEstabilidad", hudObj.transform, new Vector2(0.3f, 0.81f), new Vector2(0.7f, 0.885f), borde);
        TMP_Text etiqueta = CrearTexto("TextoEstabilidad", panelEstabilidad.transform, "Estabilidad", 22f, colorTexto, fuente);
        Estirar(etiqueta.gameObject, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.95f));

        GameObject fondoBarra = CrearImagen("BarraEstabilidadFondo", panelEstabilidad.transform, new Color(1f, 1f, 1f, 0.1f), spriteBlanco);
        Estirar(fondoBarra, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.4f));
        fondoBarra.GetComponent<Image>().raycastTarget = false;
        GameObject relleno = CrearImagen("BarraEstabilidad", fondoBarra.transform, colorCeleste, spriteBlanco);
        Estirar(relleno, Vector2.zero, Vector2.one);
        relleno.GetComponent<Image>().raycastTarget = false;

        HUDJuego hud = hudObj.AddComponent<HUDJuego>();
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.FindProperty("textoPuntos").objectReferenceValue = puntos;
        so.FindProperty("textoAltura").objectReferenceValue = altura;
        so.FindProperty("textoTiempo").objectReferenceValue = tiempo;
        so.FindProperty("barraEstabilidad").objectReferenceValue = relleno.GetComponent<RectTransform>();
        so.FindProperty("imagenEstabilidad").objectReferenceValue = relleno.GetComponent<Image>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    // fondo azul marino semitransparente con el borde celeste del menú encima
    private static GameObject CrearPanelHUD(string nombre, Transform padre, Vector2 min, Vector2 max, Sprite borde)
    {
        GameObject panel = CrearImagen(nombre, padre, colorPanel, null);
        Estirar(panel, min, max);
        panel.GetComponent<Image>().raycastTarget = false;

        GameObject marco = CrearImagen("Borde", panel.transform, colorBordeCeleste, borde);
        Estirar(marco, Vector2.zero, Vector2.one);
        marco.GetComponent<Image>().raycastTarget = false;
        return panel;
    }

    private static ResultadoFinalUI CrearResultado(Transform canvas)
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE);
        Sprite borde = CargarPrimerSprite(RUTA_BORDE_BOTON);

        GameObject panel = CrearImagen("PanelResultado", canvas, colorPanel, null);
        Estirar(panel, Vector2.zero, Vector2.one);

        GameObject caja = CrearImagen("Caja", panel.transform, colorBordeCeleste, borde);
        Estirar(caja, new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.85f));

        TMP_Text titulo = CrearTexto("Titulo", caja.transform, "Fin de la partida", 56f, colorCeleste, fuente);
        Estirar(titulo.gameObject, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));

        TMP_Text motivo = CrearTexto("TextoMotivo", caja.transform, "Motivo", 32f, colorTexto, fuente);
        Estirar(motivo.gameObject, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.81f));

        TMP_Text altura = CrearTexto("TextoAltura", caja.transform, "Altura: 0 bloques", 36f, colorTexto, fuente);
        Estirar(altura.gameObject, new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.69f));

        TMP_Text puntos = CrearTexto("TextoPuntos", caja.transform, "Puntos: 0", 36f, colorTexto, fuente);
        Estirar(puntos.gameObject, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.59f));

        TMP_Text aciertos = CrearTexto("TextoAciertos", caja.transform, "Aciertos: 0%", 36f, colorTexto, fuente);
        Estirar(aciertos.gameObject, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.49f));

        TMP_Text record = CrearTexto("TextoRecord", caja.transform, "¡Nuevo récord!", 40f, colorBase, fuente);
        Estirar(record.gameObject, new Vector2(0.05f, 0.29f), new Vector2(0.95f, 0.38f));

        Button jugar = CrearBoton("BotonJugarDeNuevo", caja.transform, "Jugar de nuevo", borde, fuente);
        Estirar(jugar.gameObject, new Vector2(0.08f, 0.07f), new Vector2(0.48f, 0.2f));

        Button volver = CrearBoton("BotonVolver", caja.transform, "Volver", borde, fuente);
        Estirar(volver.gameObject, new Vector2(0.52f, 0.07f), new Vector2(0.92f, 0.2f));

        GameObject obj = new GameObject("Resultado");
        ResultadoFinalUI resultado = obj.AddComponent<ResultadoFinalUI>();
        SerializedObject so = new SerializedObject(resultado);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("textoMotivo").objectReferenceValue = motivo;
        so.FindProperty("textoAltura").objectReferenceValue = altura;
        so.FindProperty("textoPuntos").objectReferenceValue = puntos;
        so.FindProperty("textoAciertos").objectReferenceValue = aciertos;
        so.FindProperty("textoRecord").objectReferenceValue = record.gameObject;
        so.FindProperty("botonJugarDeNuevo").objectReferenceValue = jugar;
        so.FindProperty("botonVolver").objectReferenceValue = volver;
        so.ApplyModifiedPropertiesWithoutUndo();
        return resultado;
    }

    private static Button CrearBoton(string nombre, Transform padre, string texto, Sprite borde, TMP_FontAsset fuente)
    {
        GameObject obj = CrearImagen(nombre, padre, colorBordeCeleste, borde);
        Button boton = obj.AddComponent<Button>();
        boton.targetGraphic = obj.GetComponent<Image>();

        TMP_Text tmp = CrearTexto("Texto", obj.transform, texto, 32f, colorTexto, fuente);
        Estirar(tmp.gameObject, Vector2.zero, Vector2.one);
        return boton;
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
