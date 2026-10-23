using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PuzzleSetup
{
    private const int PIEZAS = 9;

    [MenuItem("Tools/Puzzle/Reparar escena")]
    public static void Repair()
    {
        List<string> ok = new List<string>();
        List<string> fail = new List<string>();

        GameObject goPiezas  = Find("Piezas");
        GameObject goSlots   = Find("Slots");
        GameObject goHud     = Find("HUD");
        GameObject goGm      = Find("GameManager");
        GameObject goGen     = Find("PuzzleGenerator");
        GameObject goVictory = Find("VictoryPanel");
        GameObject goPausa   = Find("PanelPausa");
        GameObject goModelo  = Find("Modelo");

        foreach (var par in new[]{
            ("Piezas", (Object)goPiezas), ("Slots", goSlots), ("HUD", goHud),
            ("GameManager", goGm), ("PuzzleGenerator", goGen), ("VictoryPanel", goVictory) })
        {
            if (par.Item2 == null) fail.Add($"No encontre el objeto '{par.Item1}' en la escena.");
        }
        if (goPiezas == null || goSlots == null || goGm == null)
        {
            Report(ok, fail);
            return;
        }

        Image[] imgPiezas = new Image[PIEZAS];
        for (int i = 1; i <= PIEZAS; i++)
        {
            Transform tp = goPiezas.transform.Find("Pieza_" + i);
            Transform ts = goSlots.transform.Find("Slot_" + i);

            if (tp == null) { fail.Add($"Falta el objeto 'Pieza_{i}' dentro de Piezas."); continue; }
            if (ts == null) { fail.Add($"Falta el objeto 'Slot_{i}' dentro de Slots."); continue; }

            GameObject gp = tp.gameObject;

            Image img = gp.GetComponent<Image>();
            if (img == null) img = Undo.AddComponent<Image>(gp);
            img.raycastTarget = true;
            imgPiezas[i - 1] = img;

            if (gp.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(gp);

            PuzzlePiece pp = gp.GetComponent<PuzzlePiece>();
            bool recienAgregado = pp == null;
            if (recienAgregado) pp = Undo.AddComponent<PuzzlePiece>(gp);

            Undo.RecordObject(pp, "Reparar pieza");
            pp.targetSlot = ts.GetComponent<RectTransform>();
            if (recienAgregado)
            {
                pp.snapDistance   = 120f;
                pp.snapDuration   = 0.12f;
                pp.returnDuration = 0.25f;
                pp.dragScale      = 1.08f;
                pp.dragAlpha      = 0.9f;
            }
            EditorUtility.SetDirty(pp);
            ok.Add($"Pieza_{i} -> Slot_{i}");
        }

        for (int i = 1; i <= PIEZAS; i++)
        {
            Transform ts = goSlots.transform.Find("Slot_" + i);
            if (ts == null) continue;
            Image si = ts.GetComponent<Image>();
            if (si != null && si.raycastTarget)
            {
                Undo.RecordObject(si, "Slot raycast off");
                si.raycastTarget = false;
                EditorUtility.SetDirty(si);
                ok.Add($"Slot_{i}: apague Raycast Target");
            }
        }

        HudController hud = null;
        if (goHud != null)
        {
            hud = goHud.GetComponent<HudController>();
            if (hud == null) { hud = Undo.AddComponent<HudController>(goHud); ok.Add("Agregue HudController al objeto HUD"); }

            Undo.RecordObject(hud, "Reparar HUD");
            Assign(ref hud.progressText,    Comp<TMP_Text>("TxtProgreso"),    "HUD.Progress Text",     ok, fail);
            Assign(ref hud.timerText,       Comp<TMP_Text>("TxtTiempo"),      "HUD.Timer Text",        ok, fail);
            Assign(ref hud.victoryTimeText, Comp<TMP_Text>("TxtTiempoFinal"), "HUD.Victory Time Text", ok, fail);
            if (goPausa != null && hud.pausePanel == null) { hud.pausePanel = goPausa; ok.Add("HUD.Pause Panel -> PanelPausa"); }
            EditorUtility.SetDirty(hud);
        }

        PuzzleGenerator gen = null;
        if (goGen != null)
        {
            gen = goGen.GetComponent<PuzzleGenerator>();
            if (gen == null) { gen = Undo.AddComponent<PuzzleGenerator>(goGen); ok.Add("Agregue PuzzleGenerator"); }

            Undo.RecordObject(gen, "Reparar generador");
            gen.resourcesSubfolder = "Puzzles";
            gen.filePrefix         = "puzzle_";
            gen.imageCount         = PIEZAS;
            gen.numberPadding      = 2;
            gen.avoidRepeatLast    = 3;

            if (goModelo != null && gen.backgroundImage == null)
            {
                gen.backgroundImage = goModelo.GetComponent<Image>();
                ok.Add("PuzzleGenerator.Background Image -> Modelo");
            }

            if (goSlots != null && gen.slotsRoot == null)
            {
                gen.slotsRoot = goSlots.GetComponent<RectTransform>();
                ok.Add("PuzzleGenerator.Slots Root -> Slots");
            }
            if (goPiezas != null && gen.piecesRoot == null)
            {
                gen.piecesRoot = goPiezas.GetComponent<RectTransform>();
                ok.Add("PuzzleGenerator.Pieces Root -> Piezas");
            }
            if (gen.slotSprite == null)
            {
                Sprite slotSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/slot_9s.png");
                if (slotSpr != null) { gen.slotSprite = slotSpr; ok.Add("PuzzleGenerator.Slot Sprite -> slot_9s"); }
            }

            EditorUtility.SetDirty(gen);
        }

        GameManager[] gms = goGm.GetComponents<GameManager>();
        if (gms.Length > 1)
            fail.Add($"El objeto GameManager tiene {gms.Length} componentes Game Manager. " +
                     "Quita los sobrantes a mano: con dos, el singleton destruye el objeto al entrar en Play.");

        GameManager gm = goGm.GetComponent<GameManager>();
        if (gm == null) { gm = Undo.AddComponent<GameManager>(goGm); ok.Add("Agregue GameManager"); }

        AudioSource src = goGm.GetComponent<AudioSource>();
        if (src == null) { src = Undo.AddComponent<AudioSource>(goGm); ok.Add("Agregue Audio Source"); }
        Undo.RecordObject(src, "Reparar audio");
        src.playOnAwake  = false;
        src.loop         = false;
        src.spatialBlend = 0f;
        EditorUtility.SetDirty(src);

        Undo.RecordObject(gm, "Reparar GameManager");
        if (goVictory != null && gm.victoryPanel == null) { gm.victoryPanel = goVictory; ok.Add("GameManager.Victory Panel -> VictoryPanel"); }
        if (hud  != null && gm.hud == null)             { gm.hud = hud;                  ok.Add("GameManager.Hud -> HUD"); }
        if (gen  != null && gm.puzzleGenerator == null) { gm.puzzleGenerator = gen;      ok.Add("GameManager.Puzzle Generator -> PuzzleGenerator"); }
        if (gm.audioSource == null)                     { gm.audioSource = src;          ok.Add("GameManager.Audio Source -> este objeto"); }

        AudioClip place   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_place.wav");
        AudioClip victory = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_victory.wav");
        if (place   != null && gm.sfxPlace   == null) { gm.sfxPlace   = place;   ok.Add("GameManager.Sfx Place -> sfx_place"); }
        if (victory != null && gm.sfxVictory == null) { gm.sfxVictory = victory; ok.Add("GameManager.Sfx Victory -> sfx_victory"); }
        if (place == null || victory == null)
            fail.Add("No encontre los WAV en 'Assets/Audio/'. Asignalos a mano si estan en otra carpeta.");

        gm.shuffleStartPositions = true;
        EditorUtility.SetDirty(gm);

        if (goVictory != null && goVictory.activeSelf)
        {
            Undo.RecordObject(goVictory, "Desactivar victoria");
            goVictory.SetActive(false);
            ok.Add("Desactive VictoryPanel (debe empezar oculto)");
        }
        if (goPausa != null && goPausa.activeSelf)
        {
            Undo.RecordObject(goPausa, "Desactivar pausa");
            goPausa.SetActive(false);
            ok.Add("Desactive PanelPausa");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Report(ok, fail);
    }

    private static void Assign<T>(ref T campo, T valor, string nombre,
                                  List<string> ok, List<string> fail) where T : Object
    {
        if (valor == null) { fail.Add($"No encontre el objeto para {nombre}."); return; }
        if (campo != null) return;                 
        campo = valor;
        ok.Add($"{nombre} -> {valor.name}");
    }

    private static T Comp<T>(string nombre) where T : Component
    {
        GameObject go = Find(nombre);
        return go != null ? go.GetComponent<T>() : null;
    }

    private static GameObject Find(string nombre)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = Recurse(root.transform, nombre);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private static Transform Recurse(Transform padre, string nombre)
    {
        if (padre.name == nombre) return padre;
        for (int i = 0; i < padre.childCount; i++)
        {
            Transform r = Recurse(padre.GetChild(i), nombre);
            if (r != null) return r;
        }
        return null;
    }

    private static void Report(List<string> ok, List<string> fail)
    {
        if (ok.Count > 0)
            Debug.Log("[PuzzleSetup] LISTO (" + ok.Count + "):\n  " + string.Join("\n  ", ok));
        if (fail.Count > 0)
            Debug.LogWarning("[PuzzleSetup] REVISA A MANO (" + fail.Count + "):\n  " + string.Join("\n  ", fail));
        if (fail.Count == 0)
            Debug.Log("[PuzzleSetup] Escena reparada sin pendientes. Guarda con Ctrl+S y dale Play.");
    }
}
