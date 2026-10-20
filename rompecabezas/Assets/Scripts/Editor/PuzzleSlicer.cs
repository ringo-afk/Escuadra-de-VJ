using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PuzzleSlicer
{
    private const string FOLDER = "Assets/Resources/Puzzles";
    private const int COLS = 3;
    private const int ROWS = 3;
    private const int MAX_SIZE = 1024;

    [MenuItem("Tools/Puzzle/Rebanar imagenes 3x3")]
    public static void SliceAll()
    {
        if (!AssetDatabase.IsValidFolder(FOLDER))
        {
            Debug.LogError($"[PuzzleSlicer] No existe la carpeta '{FOLDER}'.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { FOLDER });
        if (guids.Length == 0)
        {
            Debug.LogError($"[PuzzleSlicer] No hay texturas en '{FOLDER}'.");
            return;
        }

        int done = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Rebanando puzzles",
                    Path.GetFileName(path), (float)i / guids.Length);
                if (SliceOne(path)) done++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[PuzzleSlicer] {done} imagen(es) rebanadas en {COLS}x{ROWS} " +
                  $"({COLS * ROWS} piezas cada una) y configuradas para WebGL.");
    }

    private static bool SliceOne(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (importer == null || tex == null) return false;

        int w = tex.width;
        int h = tex.height;

        if (w % COLS != 0 || h % ROWS != 0)
            Debug.LogWarning($"[PuzzleSlicer] '{Path.GetFileName(path)}' es {w}x{h}: " +
                             $"no es divisible exacto entre {COLS}x{ROWS}. " +
                             "Quedaran uno o dos pixeles sin usar en el borde.", tex);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.isReadable = false;

        int cw = w / COLS;
        int ch = h / ROWS;
        string baseName = Path.GetFileNameWithoutExtension(path);

        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        for (int r = 0; r < ROWS; r++)
        {
            for (int c = 0; c < COLS; c++)
            {
                SpriteMetaData md = new SpriteMetaData
                {
                    name = $"{baseName}_{r * COLS + c}",
                    rect = new Rect(c * cw, h - (r + 1) * ch, cw, ch),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                };
                metas.Add(md);
            }
        }
        #pragma warning disable 0618
                importer.spritesheet = metas.ToArray();
        #pragma warning restore 0618

        // compresion para WebGL 
        TextureImporterPlatformSettings ps = importer.GetPlatformTextureSettings("WebGL");
        ps.overridden = true;
        ps.maxTextureSize = MAX_SIZE;
        ps.format = TextureImporterFormat.DXT1Crunched;   
        ps.compressionQuality = 50;
        importer.SetPlatformTextureSettings(ps);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return true;
    }
}
