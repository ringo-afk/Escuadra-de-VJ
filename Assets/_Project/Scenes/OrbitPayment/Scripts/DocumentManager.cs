using UnityEngine;
using TMPro;

public class DocumentManager : MonoBehaviour
{
    public static DocumentManager instance;

    public TMP_Text documentText;

    private int documentos = 0;

    public int TotalDocumentos
    {
        get { return documentos; }
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ActualizarTexto();
    }

    public void AgregarDocumento()
    {
        documentos++;
        ActualizarTexto();
    }

    void ActualizarTexto()
    {
        documentText.text = "× " + documentos;
    }
}