using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Base de Datos")]
    public DocumentoData[] todosLosDocumentos;          

    [Header("Ruta de los Tripulantes")]
    public Transform puntoA;
    public Transform puntoB;
    public Transform puntoC;

    [Header("Personajes y Documentos")]
    public GameObject[] prefabsTripulantes;
    public GameObject prefabCarpeta;
    public Transform posicionCarpeta;

    [Header("Reglas del Juego")]
    public int totalTripulantes = 7;
    public int aciertosNecesarios = 4;                  

    [Header("Audio")]
    public AudioSource audioSource;                     
    public AudioClip clipTimbre;

    private int tripulantesAtendidos = 0;
    private int documentosCorrectos = 0;
    private TripulanteMovimiento tripulanteActual;
    private GameObject carpetaActual;
    private List<DocumentoData> docsTurno = new List<DocumentoData>();

    IEnumerator Start()
    {
        List<DocumentoData> remotos = null;
        yield return AtlasKiosko.Obtener().CargarDocumentos(totalTripulantes, lista => remotos = lista);

        docsTurno = remotos != null && remotos.Count > 0 ? remotos : SeleccionarDocumentosAleatorios();
        SiguienteTurno();
    }

    private List<DocumentoData> SeleccionarDocumentosAleatorios()
    {
        var pool = new List<DocumentoData>(todosLosDocumentos);

        for (int i = pool.Count - 1; i > 0; i--)        
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool.GetRange(0, Mathf.Min(totalTripulantes, pool.Count));
    }

    public void SiguienteTurno()
    {
        if (tripulantesAtendidos < totalTripulantes)
            GenerarTripulante();
        else
            EvaluarPartida();
    }

    private void GenerarTripulante()
    {
        int indiceAleatorio = Random.Range(0, prefabsTripulantes.Length);
        GameObject nuevoPersonaje = Instantiate(prefabsTripulantes[indiceAleatorio]);

        tripulanteActual = nuevoPersonaje.GetComponent<TripulanteMovimiento>();
        tripulanteActual.InicializarRuta(puntoA, puntoB, puntoC, this);
    }

    public void TripulanteLlegoAlMostrador()
    {
        if (audioSource != null && clipTimbre != null)
            audioSource.PlayOneShot(clipTimbre);

        DocumentoData documentoSorteado = docsTurno[tripulantesAtendidos % docsTurno.Count];

        carpetaActual = Instantiate(prefabCarpeta, posicionCarpeta.position, Quaternion.identity);
        carpetaActual.GetComponent<CarpetaInteractiva>().AsignarDatos(documentoSorteado);
    }

    public void ValidarDocumento(bool esCorrecto)
    {
        if (esCorrecto)
            documentosCorrectos++;

        tripulantesAtendidos++;

        Destroy(carpetaActual);
        tripulanteActual.DocumentoClasificado();
    }

    
    private void EvaluarPartida()
    {
        Debug.Log("Turno terminado: " + documentosCorrectos + "/" + totalTripulantes);
    }
}