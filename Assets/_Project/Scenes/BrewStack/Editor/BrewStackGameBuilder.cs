using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
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
    private const string RUTA_LOGO_BREWSTACK = CARPETA + "/BS_Sprites/logo_brewstack.png";
    private const string RUTA_LOGO_ESLABON = CARPETA + "/BS_Sprites/logo_eslabon.png";
    private const string CARPETA_AUDIO = CARPETA + "/BS_Audio";
    private const float CALIDAD_AUDIO = 0.5f;
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
        CrearAvisoNivel(canvas, hud);
        ResultadoFinalUI resultado = CrearResultado(canvas);

        TextosFlotantes textos = CrearTextosFlotantes();
        JuegoManager juego = CrearJuegoManager(torre, trivia, hud, resultado, textos);
        CrearEfectos(torre);
        SonidosJuego sonidos = CrearSonidos(torre, trivia, resultado, juego);
        MusicaJuego musica = CrearMusica(resultado, torre, juego, sonidos);
        ConfigurarAudios();

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

    // agrega a BS_Menu lo que le falta, busca todo por nombre para que correrlo otra vez no duplique nada
    [MenuItem("BrewStack/Actualizar menú")]
    public static void ActualizarMenu()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("BrewStack: sal de Play antes de actualizar el menú");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var menu = EditorSceneManager.GetSceneByPath(RUTA_MENU);
        bool yaEstabaAbierta = menu.isLoaded;
        if (yaEstabaAbierta && menu.isDirty)
        {
            Debug.LogError("BrewStack: BS_Menu tiene cambios sin guardar. Guárdala o descártalos y vuelve a intentar.");
            return;
        }
        if (!yaEstabaAbierta)
        {
            menu = EditorSceneManager.OpenScene(RUTA_MENU, OpenSceneMode.Additive);
        }

        // los objetos nuevos se crean en la escena activa, por eso se cambia mientras tanto
        var escenaActiva = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SetActiveScene(menu);

        MenuManager manager = null;
        foreach (GameObject raiz in menu.GetRootGameObjects())
        {
            if (manager == null) manager = raiz.GetComponentInChildren<MenuManager>(true);
        }

        if (manager == null)
        {
            Debug.LogError("BrewStack: no se encontró ningún MenuManager en BS_Menu");
        }
        else
        {
            SerializedObject so = new SerializedObject(manager);
            ActualizarInstrucciones(so, manager);
            ActualizarLogos(so);
            ActualizarOpciones(so, manager);
            ActualizarMusicaMenu(so, manager);
            so.ApplyModifiedPropertiesWithoutUndo();
            ConfigurarAudios();

            // al final, para que también lo tengan los botones que se agregaron arriba
            foreach (GameObject raiz in menu.GetRootGameObjects())
            {
                foreach (Button boton in raiz.GetComponentsInChildren<Button>(true))
                {
                    if (boton.GetComponent<EscalaAlPasar>() == null) boton.gameObject.AddComponent<EscalaAlPasar>();
                }
            }

            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            Debug.Log("BrewStack: se actualizó y guardó BS_Menu");
        }

        if (escenaActiva.IsValid() && escenaActiva != menu) EditorSceneManager.SetActiveScene(escenaActiva);
        if (!yaEstabaAbierta)
        {
            EditorSceneManager.CloseScene(menu, true);
        }
    }

    // las tarjetas toman los números de los scripts, si cambian solo hay que volver a correr el menú
    private static void ActualizarInstrucciones(SerializedObject so, MenuManager manager)
    {
        Transform panel = ((GameObject)so.FindProperty("comoSeJuegaPanel").objectReferenceValue).transform;
        Transform grid1 = panel.Find("InstruccionesGrid");
        if (grid1 == null)
        {
            Debug.LogError("BrewStack: no se encontró InstruccionesGrid en el panel Cómo se juega");
            return;
        }

        int minutos = Mathf.RoundToInt(LeerCampo<JuegoManager>("tiempoSesion") / 60f);
        int segundosPregunta = Mathf.RoundToInt(LeerCampo<TriviaUI>("tiempoPregunta"));
        int bien = Mathf.RoundToInt(LeerCampo<JuegoManager>("puntosBienPuesto"));
        int mal = Mathf.RoundToInt(LeerCampo<JuegoManager>("puntosMalPuesto"));
        int bono = Mathf.RoundToInt(LeerCampo<JuegoManager>("factorBono") * 100f);
        int malSeguidos = Mathf.RoundToInt(LeerCampo<Torre>("maxMalPuestosSeguidos"));
        int nivel2 = Mathf.RoundToInt(LeerCampo<TriviaUI>("alturaDificultad2"));
        int nivel3 = Mathf.RoundToInt(LeerCampo<TriviaUI>("alturaDificultad3"));
        int alturaMaxima = Mathf.RoundToInt(LeerCampo<Torre>("alturaMaxima"));

        string[] textos =
        {
            $"Tienes {minutos} minutos. El bloque va de lado a lado: haz clic o presiona espacio para soltarlo.",
            $"Antes de cada bloque hay una pregunta y tienes {segundosPregunta} segundos. Si aciertas, el bloque sale ancho y lento. Si fallas, angosto y rápido.",
            $"Bien puesto: +{bien} puntos. Mal puesto: +{mal} puntos y la torre se inclina.",
            "Si más de la mitad del bloque queda fuera de la torre, se cae y no suma puntos.",
            $"La torre colapsa con {malSeguidos} bloques mal puestos o caídos seguidos, o si se inclina demasiado.",
            $"Al final ganas un bono de hasta {bono}% de tus puntos, según tu porcentaje de aciertos.",
            $"Las preguntas suben de nivel: nivel 2 a los {nivel2} bloques y nivel 3 a los {nivel3} bloques.",
            $"La altura máxima es de {alturaMaxima} bloques. Si la alcanzas, la partida termina."
        };

        // no caben las 8 tarjetas juntas, la segunda página es una copia de la primera
        Transform grid2 = panel.Find("InstruccionesGrid2");
        if (grid2 == null)
        {
            grid2 = Object.Instantiate(grid1.gameObject, panel).transform;
            grid2.name = "InstruccionesGrid2";
            for (int i = 0; i < grid2.childCount; i++) grid2.GetChild(i).name = "Instruccion" + (i + 5);
        }
        grid2.SetSiblingIndex(grid1.GetSiblingIndex() + 1);

        Transform[] grids = { grid1, grid2 };
        for (int g = 0; g < grids.Length; g++)
        {
            GridLayoutGroup layout = grids[g].GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(420f, 120f);
            LayoutElement elemento = grids[g].GetComponent<LayoutElement>();
            if (elemento != null) elemento.preferredHeight = layout.cellSize.y * 2f + layout.spacing.y;

            for (int i = 0; i < 4; i++)
            {
                Transform tarjeta = grids[g].Find("Instruccion" + (g * 4 + i + 1));
                TMP_Text texto = tarjeta != null ? tarjeta.GetComponentInChildren<TMP_Text>(true) : null;
                if (texto == null)
                {
                    Debug.LogError("BrewStack: falta la tarjeta Instruccion" + (g * 4 + i + 1));
                    continue;
                }
                texto.text = textos[g * 4 + i];
                texto.enableAutoSizing = true;
                texto.fontSizeMin = 16f;
                texto.fontSizeMax = 22f;
            }
        }
        grid1.gameObject.SetActive(true);
        grid2.gameObject.SetActive(false);

        Transform volver = panel.Find("VolverButton");
        Button pagina = BuscarODuplicar(panel, "PaginaButton", volver);
        pagina.transform.SetAsLastSibling();
        volver.SetAsLastSibling();
        TMP_Text textoPagina = pagina.GetComponentInChildren<TMP_Text>(true);
        textoPagina.text = "Siguiente";
        CambiarClic(pagina, manager.CambiarPaginaComoSeJuega);

        SerializedProperty paginas = so.FindProperty("paginasComoSeJuega");
        paginas.arraySize = 2;
        paginas.GetArrayElementAtIndex(0).objectReferenceValue = grid1.gameObject;
        paginas.GetArrayElementAtIndex(1).objectReferenceValue = grid2.gameObject;
        so.FindProperty("textoBotonPagina").objectReferenceValue = textoPagina;
    }

    private static void ActualizarLogos(SerializedObject so)
    {
        Transform principal = ((GameObject)so.FindProperty("mainMenuPanel").objectReferenceValue).transform;
        Transform titulo = principal.Find("TitleText");
        Transform canvas = principal.parent;

        // a la izquierda del título, como hijo para que se mueva con él
        PonerLogo(RUTA_LOGO_BREWSTACK, "LogoBrewStack", titulo, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), 220f);

        // abajo a la derecha, justo después del fondo para que los paneles queden encima
        GameObject eslabon = PonerLogo(RUTA_LOGO_ESLABON, "LogoEslabon", canvas, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 32f), 160f);
        Transform fondo = canvas.Find("Background");
        if (eslabon != null && fondo != null) eslabon.transform.SetSiblingIndex(fondo.GetSiblingIndex() + 1);
    }

    // otro botón a lo alto ya no cabe en el menú, por eso Opciones va junto a Salir en una fila
    private static void ActualizarOpciones(SerializedObject so, MenuManager manager)
    {
        Transform principal = ((GameObject)so.FindProperty("mainMenuPanel").objectReferenceValue).transform;
        Transform historial = ((GameObject)so.FindProperty("historialPanel").objectReferenceValue).transform;
        Transform canvas = principal.parent;

        Transform fila = principal.Find("FilaInferior");
        if (fila == null)
        {
            GameObject obj = new GameObject("FilaInferior", typeof(RectTransform));
            obj.layer = LayerMask.NameToLayer("UI");
            obj.transform.SetParent(principal, false);
            HorizontalLayoutGroup layout = obj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            obj.AddComponent<LayoutElement>().preferredHeight = 64f;
            fila = obj.transform;
        }
        fila.SetAsLastSibling();

        Transform salir = principal.Find("SalirButton");
        if (salir != null) salir.SetParent(fila, false);
        else salir = fila.Find("SalirButton");

        Button opciones = BuscarODuplicar(fila, "OpcionesButton", principal.Find("HistorialButton"));
        opciones.GetComponentInChildren<TMP_Text>(true).text = "Opciones";
        CambiarClic(opciones, manager.MostrarOpciones);
        opciones.transform.SetAsFirstSibling();
        if (salir != null) salir.SetAsLastSibling();

        // copia del panel de historial para que se vea igual, sin la lista
        Transform panel = canvas.Find("OpcionesPanel");
        if (panel == null)
        {
            panel = Object.Instantiate(historial.gameObject, canvas).transform;
            panel.name = "OpcionesPanel";
            Transform contenido = panel.Find("HistorialContent");
            if (contenido != null) Object.DestroyImmediate(contenido.gameObject);
        }
        panel.SetSiblingIndex(historial.GetSiblingIndex() + 1);
        panel.gameObject.SetActive(false);
        panel.Find("TitleText").GetComponent<TMP_Text>().text = "OPCIONES";

        Sprite blanco = CrearSpriteBloque();
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE);
        Slider musica = FilaSlider(panel, "FilaMusica", "Música", blanco, fuente);
        Slider efectos = FilaSlider(panel, "FilaEfectos", "Efectos", blanco, fuente);

        Transform volver = panel.Find("VolverButton");
        Button silencio = BuscarODuplicar(panel, "SilencioButton", volver);
        // el clic lo pone PanelOpciones al iniciar, la copia traía el de Volver
        CambiarClic(silencio, null);
        CambiarClic(volver.GetComponent<Button>(), manager.MostrarMainMenu);

        musica.transform.parent.SetAsLastSibling();
        efectos.transform.parent.SetAsLastSibling();
        silencio.transform.SetAsLastSibling();
        volver.SetAsLastSibling();

        PanelOpciones componente = panel.GetComponent<PanelOpciones>();
        if (componente == null) componente = panel.gameObject.AddComponent<PanelOpciones>();
        ConectarOpciones(componente, musica, efectos, silencio);
        so.FindProperty("opcionesPanel").objectReferenceValue = panel.gameObject;
    }

    private static Slider FilaSlider(Transform panel, string nombre, string etiqueta, Sprite blanco, TMP_FontAsset fuente)
    {
        Transform fila = panel.Find(nombre);
        if (fila == null)
        {
            GameObject obj = new GameObject(nombre, typeof(RectTransform));
            obj.layer = LayerMask.NameToLayer("UI");
            obj.transform.SetParent(panel, false);
            obj.AddComponent<LayoutElement>().preferredHeight = 60f;

            TMP_Text texto = CrearTexto("Etiqueta", obj.transform, etiqueta, 24f, colorTexto, fuente);
            Estirar(texto.gameObject, new Vector2(0.06f, 0f), new Vector2(0.36f, 1f));
            texto.alignment = TextAlignmentOptions.Left;

            Slider nuevo = CrearSlider("Slider", obj.transform, blanco);
            Estirar(nuevo.gameObject, new Vector2(0.4f, 0.15f), new Vector2(0.94f, 0.85f));
            fila = obj.transform;
        }
        return fila.GetComponentInChildren<Slider>(true);
    }

    // hoy la música del menú es un AudioSource con Play On Awake, ahora la arranca MusicaMenu con fade
    private static void ActualizarMusicaMenu(SerializedObject so, MenuManager manager)
    {
        AudioSource efectos = (AudioSource)so.FindProperty("sfxSource").objectReferenceValue;
        AudioSource fuente = null;
        foreach (AudioSource a in manager.GetComponents<AudioSource>())
        {
            if (a != efectos) fuente = a;
        }
        if (fuente == null) fuente = manager.gameObject.AddComponent<AudioSource>();

        fuente.playOnAwake = false;
        fuente.loop = true;
        AudioClip tema = BuscarMusica("musica_menu", "se queda la música ambiental de antes");
        if (tema != null) fuente.clip = tema;
        else if (fuente.clip == null) fuente.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_MUSICA);

        MusicaMenu musica = manager.GetComponent<MusicaMenu>();
        if (musica == null) musica = manager.gameObject.AddComponent<MusicaMenu>();
        SerializedObject soMusica = new SerializedObject(musica);
        soMusica.FindProperty("fuente").objectReferenceValue = fuente;
        soMusica.ApplyModifiedPropertiesWithoutUndo();
        so.FindProperty("musicaMenu").objectReferenceValue = musica;
    }

    private static GameObject PonerLogo(string ruta, string nombre, Transform padre, Vector2 ancla, Vector2 pivote, Vector2 posicion, float tamano)
    {
        Transform existente = padre.Find(nombre);
        if (!File.Exists(ruta))
        {
            Debug.LogWarning($"BrewStack: no existe {ruta}, el menú se generó sin ese logo");
            if (existente != null) Object.DestroyImmediate(existente.gameObject);
            return null;
        }

        ConfigurarSprite(ruta, 100, Vector4.zero);
        GameObject obj = existente != null ? existente.gameObject : CrearImagen(nombre, padre, Color.white, null);
        Image img = obj.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = ancla;
        rect.anchorMax = ancla;
        rect.pivot = pivote;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(tamano, tamano);
        return obj;
    }

    // la música larga pesa mucho en el build Web, con Vorbis a media calidad se oye igual y pesa mucho menos
    private static void ConfigurarAudios()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { CARPETA_AUDIO }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(ruta);
            AudioImporterSampleSettings ajustes = importer.defaultSampleSettings;
            bool listo = ajustes.compressionFormat == AudioCompressionFormat.Vorbis && Mathf.Approximately(ajustes.quality, CALIDAD_AUDIO);
            if (listo && !importer.ContainsSampleSettingsOverride("WebGL")) continue;

            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = CALIDAD_AUDIO;
            importer.defaultSampleSettings = ajustes;
            // si alguien puso otro formato solo para Web, se quita para que use el de arriba
            importer.ClearSampleSettingOverride("WebGL");
            importer.SaveAndReimport();
            Debug.Log("BrewStack: " + ruta + " quedó en Vorbis al " + Mathf.RoundToInt(CALIDAD_AUDIO * 100f) + "%");
        }
    }

    // lee el valor que trae el script por defecto, el builder no cambia estos campos
    private static float LeerCampo<T>(string campo) where T : Component
    {
        GameObject temp = new GameObject("Temp");
        temp.hideFlags = HideFlags.HideAndDontSave;
        SerializedProperty prop = new SerializedObject(temp.AddComponent<T>()).FindProperty(campo);
        float valor = prop.propertyType == SerializedPropertyType.Integer ? prop.intValue : prop.floatValue;
        Object.DestroyImmediate(temp);
        return valor;
    }

    // copia un botón que ya existe para que se vea igual que los del menú
    private static Button BuscarODuplicar(Transform padre, string nombre, Transform original)
    {
        Transform existente = padre.Find(nombre);
        if (existente == null)
        {
            existente = Object.Instantiate(original.gameObject, padre).transform;
            existente.name = nombre;
        }
        return existente.GetComponent<Button>();
    }

    // con accion en null solo quita lo que tenía
    private static void CambiarClic(Button boton, UnityEngine.Events.UnityAction accion)
    {
        while (boton.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(boton.onClick, 0);
        }
        if (accion != null) UnityEventTools.AddPersistentListener(boton.onClick, accion);
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

    private static JuegoManager CrearJuegoManager(Torre torre, TriviaUI trivia, HUDJuego hud, ResultadoFinalUI resultado, TextosFlotantes textos)
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
        return juego;
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

    private static MusicaJuego CrearMusica(ResultadoFinalUI resultado, Torre torre, JuegoManager juego, SonidosJuego sonidos)
    {
        GameObject obj = new GameObject("Musica");
        MusicaJuego musica = obj.AddComponent<MusicaJuego>();

        AudioClip temaJuego = BuscarMusica("musica_juego", "se usa la música ambiental de antes");
        if (temaJuego == null) temaJuego = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_MUSICA);

        SerializedObject so = new SerializedObject(musica);
        so.FindProperty("resultado").objectReferenceValue = resultado;
        so.FindProperty("musica").objectReferenceValue = temaJuego;
        so.FindProperty("musicaTension").objectReferenceValue = BuscarMusica("musica_tension", "la música no cambia con la tensión");
        so.FindProperty("musicaResultado").objectReferenceValue = BuscarMusica("musica_resultado", "en el resultado sigue la música del juego");
        so.FindProperty("torre").objectReferenceValue = torre;
        so.FindProperty("juego").objectReferenceValue = juego;
        so.FindProperty("sonidos").objectReferenceValue = sonidos;
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soResultado = new SerializedObject(resultado);
        soResultado.FindProperty("musica").objectReferenceValue = musica;
        soResultado.ApplyModifiedPropertiesWithoutUndo();
        return musica;
    }

    // la extensión puede ser cualquiera (mp3, ogg, wav), se busca por nombre dentro de BS_Audio
    private static AudioClip BuscarMusica(string nombre, string queHace)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:AudioClip", new[] { CARPETA_AUDIO }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) == nombre) return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }
        Debug.LogWarning($"BrewStack: falta {nombre} en {CARPETA_AUDIO}, {queHace}");
        return null;
    }

    // slider de 0 a 1 con fondo, relleno y manija, el tamaño lo pone quien lo llama
    private static Slider CrearSlider(string nombre, Transform padre, Sprite spriteBlanco)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(padre, false);

        GameObject fondo = CrearImagen("Fondo", obj.transform, new Color(1f, 1f, 1f, 0.12f), spriteBlanco);
        Estirar(fondo, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));

        GameObject areaRelleno = new GameObject("AreaRelleno", typeof(RectTransform));
        areaRelleno.transform.SetParent(obj.transform, false);
        Estirar(areaRelleno, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
        GameObject relleno = CrearImagen("Relleno", areaRelleno.transform, colorCeleste, spriteBlanco);
        Estirar(relleno, Vector2.zero, Vector2.one);
        relleno.GetComponent<Image>().raycastTarget = false;

        GameObject areaManija = new GameObject("AreaManija", typeof(RectTransform));
        areaManija.transform.SetParent(obj.transform, false);
        Estirar(areaManija, Vector2.zero, Vector2.one);
        GameObject manija = CrearImagen("Manija", areaManija.transform, colorTexto, spriteBlanco);
        RectTransform rectManija = manija.GetComponent<RectTransform>();
        rectManija.anchorMin = Vector2.zero;
        rectManija.anchorMax = new Vector2(0f, 1f);
        rectManija.sizeDelta = new Vector2(20f, 0f);

        Slider slider = obj.AddComponent<Slider>();
        slider.fillRect = relleno.GetComponent<RectTransform>();
        slider.handleRect = rectManija;
        slider.targetGraphic = manija.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        return slider;
    }

    private static void ConectarOpciones(PanelOpciones opciones, Slider musica, Slider efectos, Button silencio)
    {
        SerializedObject so = new SerializedObject(opciones);
        so.FindProperty("sliderMusica").objectReferenceValue = musica;
        so.FindProperty("sliderEfectos").objectReferenceValue = efectos;
        so.FindProperty("botonSilencio").objectReferenceValue = silencio;
        so.FindProperty("textoSilencio").objectReferenceValue = silencio.GetComponentInChildren<TMP_Text>(true);
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
    private static SonidosJuego CrearSonidos(Torre torre, TriviaUI trivia, ResultadoFinalUI resultado, JuegoManager juego)
    {
        GameObject obj = new GameObject("Sonidos");
        SonidosJuego sonidos = obj.AddComponent<SonidosJuego>();

        SerializedObject so = new SerializedObject(sonidos);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.FindProperty("trivia").objectReferenceValue = trivia;
        so.FindProperty("resultado").objectReferenceValue = resultado;
        so.FindProperty("juego").objectReferenceValue = juego;
        so.ApplyModifiedPropertiesWithoutUndo();
        return sonidos;
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

        GameObject panelNivel = CrearPanelHUD("PanelNivel", hudObj.transform, new Vector2(0.02f, 0.81f), new Vector2(0.16f, 0.885f), borde);
        TMP_Text nivel = CrearTexto("TextoNivel", panelNivel.transform, "Nivel 1", 30f, colorTexto, fuente);
        Estirar(nivel.gameObject, Vector2.zero, Vector2.one);

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
        so.FindProperty("textoNivel").objectReferenceValue = nivel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    // va aparte y después de la trivia para que se vea encima de la pregunta, en la franja de abajo
    private static void CrearAvisoNivel(Transform canvas, HUDJuego hud)
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE);
        TMP_Text aviso = CrearTexto("TextoAvisoNivel", canvas, "¡Nivel 2!", 44f, colorBase, fuente);
        Estirar(aviso.gameObject, new Vector2(0.1f, 0.015f), new Vector2(0.9f, 0.11f));
        aviso.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("textoAvisoNivel").objectReferenceValue = aviso;
        so.ApplyModifiedPropertiesWithoutUndo();
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
        obj.AddComponent<EscalaAlPasar>();

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
            objBoton.AddComponent<EscalaAlPasar>();
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
