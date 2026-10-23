using UnityEngine;


public enum CategoriaCarpeta
{
    RecursosHumanos,
    Finanzas,
    Operaciones
}


public class DocumentoData : ScriptableObject
{
    [Header("Información del Documento")]
    public string nombrePasajero;
    public string tipoDocumento;

    [Header("Lógica de Juego")]
   
    public CategoriaCarpeta carpetaCorrecta;

    [Header("Visuales (Opcional)")]
    public Sprite sello;
}