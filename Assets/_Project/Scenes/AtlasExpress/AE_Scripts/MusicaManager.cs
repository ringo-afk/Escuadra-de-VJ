using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicaManager : MonoBehaviour
{
    private static MusicaManager instancia;

    void Awake()
    {
        
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject); 

            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            
            Destroy(gameObject);
        }
    }

    // 2. Esta función se ejecuta automáticamente CADA VEZ que se carga una nueva escena
    void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        // Si entramos a la escena de Jugar (o cualquier otra que no sea Menú/Instrucciones)...
        if (escena.name == "AE_Jugar") // <--- PON AQUÍ EL NOMBRE EXACTO DE TU ESCENA DE JUEGO
        {
            // Nos desuscribimos del evento para evitar errores
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Destruimos este objeto de música del menú para que la nueva música del juego pueda sonar
            Destroy(gameObject);
        }
    }
}