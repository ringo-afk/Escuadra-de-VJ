using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Pregunta
{
    public string id;
    public string categoria;
    public int dificultad;
    public string texto;
    public string[] opciones;
    public int correcta;
}

public class BancoPreguntas : MonoBehaviour
{
    [SerializeField] private TextAsset archivoPreguntas;
    [SerializeField] private int dificultadMinima = 1;
    [SerializeField] private int dificultadMaxima = 3;

    [System.Serializable]
    private class BancoJson
    {
        public List<Pregunta> preguntas = new List<Pregunta>();
    }

    private readonly Dictionary<int, List<Pregunta>> todas = new Dictionary<int, List<Pregunta>>();
    private readonly Dictionary<int, List<Pregunta>> pendientes = new Dictionary<int, List<Pregunta>>();
    private Pregunta ultimaPregunta;
    private int total;

    public int Total => total;

    private void Awake()
    {
        Cargar();
    }

    private void Cargar()
    {
        todas.Clear();
        pendientes.Clear();
        total = 0;

        if (archivoPreguntas == null)
        {
            Debug.LogError("BancoPreguntas: falta asignar el archivo de preguntas");
            return;
        }

        BancoJson banco = JsonUtility.FromJson<BancoJson>(archivoPreguntas.text);
        if (banco == null || banco.preguntas == null)
        {
            Debug.LogError("BancoPreguntas: no se pudo leer el JSON");
            return;
        }

        foreach (Pregunta p in banco.preguntas)
        {
            if (!EsValida(p))
            {
                Debug.LogWarning($"BancoPreguntas: se saltó la pregunta {p.id} porque tiene datos incorrectos");
                continue;
            }

            if (!todas.ContainsKey(p.dificultad))
            {
                todas[p.dificultad] = new List<Pregunta>();
                pendientes[p.dificultad] = new List<Pregunta>();
            }
            todas[p.dificultad].Add(p);
            total++;
        }

        Debug.Log($"BancoPreguntas: {total} preguntas cargadas");
    }

    private bool EsValida(Pregunta p)
    {
        if (p == null || string.IsNullOrEmpty(p.texto)) return false;
        if (p.opciones == null || p.opciones.Length < 2 || p.opciones.Length > 4) return false;
        if (p.correcta < 0 || p.correcta >= p.opciones.Length) return false;
        if (p.dificultad < dificultadMinima || p.dificultad > dificultadMaxima) return false;

        foreach (string opcion in p.opciones)
        {
            if (string.IsNullOrEmpty(opcion)) return false;
        }
        return true;
    }

    public Pregunta SiguientePregunta(int dificultad)
    {
        int d = DificultadDisponible(dificultad);
        if (d == -1) return null;

        List<Pregunta> lista = pendientes[d];
        if (lista.Count == 0)
        {
            lista.AddRange(todas[d]);
            Barajar(lista);

            // que no salga la misma pregunta dos veces seguidas al rebarajar
            if (lista.Count > 1 && lista[lista.Count - 1] == ultimaPregunta)
            {
                lista[lista.Count - 1] = lista[0];
                lista[0] = ultimaPregunta;
            }
        }

        Pregunta p = lista[lista.Count - 1];
        lista.RemoveAt(lista.Count - 1);
        ultimaPregunta = p;
        return p;
    }

    // si no hay preguntas de esa dificultad usa la más cercana, primero la más fácil
    private int DificultadDisponible(int dificultad)
    {
        for (int distancia = 0; distancia <= dificultadMaxima - dificultadMinima; distancia++)
        {
            if (todas.ContainsKey(dificultad - distancia)) return dificultad - distancia;
            if (todas.ContainsKey(dificultad + distancia)) return dificultad + distancia;
        }
        return -1;
    }

    private void Barajar(List<Pregunta> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Pregunta temp = lista[i];
            lista[i] = lista[j];
            lista[j] = temp;
        }
    }

    [ContextMenu("Probar banco")]
    private void ProbarBanco()
    {
        Cargar();
        for (int d = dificultadMinima; d <= dificultadMaxima; d++)
        {
            Pregunta p = SiguientePregunta(d);
            if (p != null)
            {
                Debug.Log($"Dificultad {d}: [{p.id}] {p.texto} | correcta: {p.opciones[p.correcta]}");
            }
        }
    }
}
