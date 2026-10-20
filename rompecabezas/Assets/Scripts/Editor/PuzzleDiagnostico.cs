using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PuzzleDiagnostico
{
    private const int TOTAL = 9;

    [MenuItem("Tools/Puzzle/Diagnosticar escena")]
    public static void Diagnosticar()
    {
        StringBuilder sb = new StringBuilder();
        int problemas = 0;

        sb.AppendLine("=== DIAGNOSTICO HEAD BREAKER ===\n");

        sb.AppendLine("--- componentes con script faltante ---");
        List<GameObject> rotos = BuscarScriptsFaltantes();
        if (rotos.Count == 0)
        {
            sb.AppendLine("ninguno, correcto.");
        }
        else
        {
            foreach (GameObject go in rotos)
            {
                int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                sb.AppendLine($"!! {Ruta(go)}  ->  {n} componente(s) rotos");
            }
            sb.AppendLine("   Corre: Tools > Puzzle > Quitar scripts faltantes");
            problemas += rotos.Count;
        }
        sb.AppendLine();

        PuzzlePiece[] todas = Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Include);
        int activas = 0;
        foreach (PuzzlePiece p in todas)
            if (p.gameObject.activeInHierarchy && p.enabled) activas++;

        sb.AppendLine("--- conteo de piezas ---");
        sb.AppendLine($"PuzzlePiece en la escena : {todas.Length}   (deberian ser {TOTAL})");
        sb.AppendLine($"Que SI se registran al dar Play : {activas}");
        sb.AppendLine("  ^ este numero es el que usa la victoria. Si dice 1, ganas con una sola pieza.\n");
        if (activas != TOTAL) problemas++;

        GameObject goPiezas = Find("Piezas");
        GameObject goSlots  = Find("Slots");
        if (goPiezas == null) { sb.AppendLine("!! No existe el objeto 'Piezas'."); problemas++; }
        if (goSlots  == null) { sb.AppendLine("!! No existe el objeto 'Slots'.");  problemas++; }

        if (goPiezas != null)
        {
            sb.AppendLine("--- pieza por pieza ---");
            sb.AppendLine("nombre    | objeto  | script | componente  | Target Slot");
            for (int i = 1; i <= TOTAL; i++)
            {
                Transform t = goPiezas.transform.Find("Pieza_" + i);
                if (t == null)
                {
                    sb.AppendLine($"Pieza_{i}   | NO EXISTE dentro de 'Piezas'");
                    problemas++;
                    continue;
                }

                PuzzlePiece pp = t.GetComponent<PuzzlePiece>();
                bool objActivo   = t.gameObject.activeInHierarchy;
                bool tieneScript = pp != null;
                bool compActivo  = tieneScript && pp.enabled;
                string slot = (tieneScript && pp.targetSlot != null) ? pp.targetSlot.name : "VACIO";

                sb.AppendLine($"Pieza_{i}   | " +
                              (objActivo   ? "activo " : "APAGADO") + " | " +
                              (tieneScript ? "si    " : "FALTA ") + " | " +
                              (compActivo  ? "activo      " : "DESACTIVADO ") + " | " + slot);

                if (!objActivo || !tieneScript || !compActivo || slot == "VACIO") problemas++;
                if (slot != "VACIO" && slot != "Slot_" + i)
                {
                    sb.AppendLine($"          ^ apunta a {slot}, deberia ser Slot_{i}");
                    problemas++;
                }
            }
            int hijos = goPiezas.transform.childCount;
            sb.AppendLine($"'Piezas' tiene {hijos} hijos" + (hijos == TOTAL ? "" : "  <-- deberian ser 9"));
            if (hijos != TOTAL) problemas++;
            sb.AppendLine();
        }

        sb.AppendLine("--- GameManager ---");
        GameObject goGm = Find("GameManager");
        if (goGm == null) { sb.AppendLine("!! No existe el objeto 'GameManager'."); problemas++; }
        else
        {
            GameManager[] gms = goGm.GetComponents<GameManager>();
            sb.AppendLine($"objeto activo          : {(goGm.activeInHierarchy ? "si" : "NO  <-- problema")}");
            sb.AppendLine($"componentes GameManager: {gms.Length}{(gms.Length == 1 ? "" : "  <-- debe ser 1")}");
            if (!goGm.activeInHierarchy || gms.Length != 1) problemas++;

            if (gms.Length >= 1)
            {
                GameManager gm = gms[0];
                sb.AppendLine($"componente activo      : {(gm.enabled ? "si" : "NO  <-- problema")}");
                sb.AppendLine($"Shuffle Start Positions: {(gm.shuffleStartPositions ? "marcado" : "DESMARCADO  <-- por eso no revuelve")}");
                sb.AppendLine($"Victory Panel          : {(gm.victoryPanel != null ? gm.victoryPanel.name : "VACIO  <-- problema")}");
                sb.AppendLine($"Hud                    : {(gm.hud != null ? gm.hud.name : "vacio (opcional)")}");
                if (!gm.enabled || !gm.shuffleStartPositions || gm.victoryPanel == null) problemas++;
            }
        }
        sb.AppendLine();

        if (goPiezas != null)
        {
            sb.AppendLine("--- posiciones de la bandeja ---");
            bool rep = false;
            for (int i = 1; i <= TOTAL; i++)
            {
                Transform a = goPiezas.transform.Find("Pieza_" + i);
                if (a == null) continue;
                for (int j = i + 1; j <= TOTAL; j++)
                {
                    Transform b = goPiezas.transform.Find("Pieza_" + j);
                    if (b == null) continue;
                    Vector2 pa = ((RectTransform)a).anchoredPosition;
                    Vector2 pb = ((RectTransform)b).anchoredPosition;
                    if ((pa - pb).sqrMagnitude < 1f)
                    {
                        sb.AppendLine($"!! Pieza_{i} y Pieza_{j} comparten posicion {pa}");
                        rep = true; problemas++;
                    }
                }
            }
            if (!rep) sb.AppendLine("nueve posiciones distintas, correcto.");
        }

        sb.AppendLine();
        sb.AppendLine(problemas == 0
            ? "=== Sin problemas detectados. ==="
            : $"=== {problemas} cosa(s) que revisar (marcadas en MAYUSCULAS). ===");

        if (problemas == 0) Debug.Log(sb.ToString());
        else Debug.LogWarning(sb.ToString());
    }

    [MenuItem("Tools/Puzzle/Quitar scripts faltantes")]
    public static void LimpiarScriptsFaltantes()
    {
        List<GameObject> rotos = BuscarScriptsFaltantes();
        if (rotos.Count == 0)
        {
            Debug.Log("[PuzzleDiagnostico] No hay componentes con script faltante.");
            return;
        }

        int total = 0;
        foreach (GameObject go in rotos)
        {
            Undo.RegisterCompleteObjectUndo(go, "Quitar scripts faltantes");
            int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            total += n;
            Debug.Log($"[PuzzleDiagnostico] {Ruta(go)}: quite {n} componente(s) roto(s).");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[PuzzleDiagnostico] Listo: {total} componente(s) eliminados. " +
                  "Guarda con Ctrl+S y vuelve a correr el diagnostico.");
    }

    private static List<GameObject> BuscarScriptsFaltantes()
    {
        List<GameObject> res = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            RecolectarRotos(root.transform, res);
        return res;
    }

    private static void RecolectarRotos(Transform t, List<GameObject> res)
    {
        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
            res.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++) RecolectarRotos(t.GetChild(i), res);
    }

    private static string Ruta(GameObject go)
    {
        string r = go.name;
        Transform p = go.transform.parent;
        while (p != null) { r = p.name + "/" + r; p = p.parent; }
        return r;
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
}
