using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BrewStackGameBuilder
{
    private const string CARPETA = "Assets/_Project/Scenes/BrewStack";
    private const string RUTA_ESCENA = CARPETA + "/BS_Scenes/BS_Game.unity";
    private const string RUTA_PREFAB_BLOQUE = CARPETA + "/BS_Prefabs/Bloque.prefab";
    private const string RUTA_SPRITE_BLOQUE = CARPETA + "/BS_Sprites/bloque_blanco.png";
    private const string RUTA_FONDO = CARPETA + "/BS_Sprites/fondo_gradiente_espacial.png";

    private const float ANCHO_BASE = 3f;
    private const float ALTO_BASE = 0.5f;
    private const float TAMANO_CAMARA = 6f;

    private static readonly Color colorFondo = new Color(0.04f, 0.07f, 0.16f);
    private static readonly Color colorBase = new Color(0.88f, 0.59f, 0.23f);

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
        CrearPruebaFisica(torre);

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

    private static void CrearPruebaFisica(Torre torre)
    {
        GameObject obj = new GameObject("PruebaFisica");
        PruebaFisica prueba = obj.AddComponent<PruebaFisica>();

        SerializedObject so = new SerializedObject(prueba);
        so.FindProperty("torre").objectReferenceValue = torre;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
