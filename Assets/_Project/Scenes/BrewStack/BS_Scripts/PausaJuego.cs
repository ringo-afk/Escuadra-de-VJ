using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausaJuego : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject panelBotones;
    [SerializeField] private GameObject panelOpciones;
    [SerializeField] private Button botonPausa;
    [SerializeField] private Button botonContinuar;
    [SerializeField] private Button botonOpciones;
    [SerializeField] private Button botonVolverOpciones;
    [SerializeField] private Button botonMenu;
    [SerializeField] private Button botonMinijuegos;
    [SerializeField] private ResultadoFinalUI resultado;
    [SerializeField] private MusicaJuego musica;

    [Header("Escenas")]
    [SerializeField] private string escenaMenu = "BS_Menu";
    [SerializeField] private string escenaMinijuegos = "MenuMinijuegos";

    private bool enPausa;
    private bool puedePausar = true;
    private bool saliendo;

    public bool EnPausa => enPausa;

    private void Awake()
    {
        botonPausa.onClick.AddListener(Pausar);
        botonContinuar.onClick.AddListener(Continuar);
        botonOpciones.onClick.AddListener(AbrirOpciones);
        botonVolverOpciones.onClick.AddListener(CerrarOpciones);
        botonMenu.onClick.AddListener(IrAlMenu);
        botonMinijuegos.onClick.AddListener(IrAMinijuegos);
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        resultado.AlMostrar += BloquearPausa;
        resultado.AlJugarDeNuevo += PermitirPausa;
    }

    private void OnDisable()
    {
        resultado.AlMostrar -= BloquearPausa;
        resultado.AlJugarDeNuevo -= PermitirPausa;
    }

    // por si se sale de la escena de otra forma, que el siguiente juego no empiece congelado
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // mientras baja la música para salir, Escape ya no reanuda el juego
        if (saliendo) return;

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;
        // en el navegador a pantalla completa Escape solo sale de la pantalla completa, por eso también la P
        if (!teclado.escapeKey.wasPressedThisFrame && !teclado.pKey.wasPressedThisFrame) return;

        if (enPausa) Continuar();
        else Pausar();
    }

    public void Pausar()
    {
        if (enPausa || !puedePausar) return;

        enPausa = true;
        Time.timeScale = 0f;
        panel.SetActive(true);
        CerrarOpciones();
        // si queda un botón seleccionado, Enter lo puede activar aunque esté tapado
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void Continuar()
    {
        if (!enPausa) return;

        enPausa = false;
        Time.timeScale = 1f;
        panel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void AbrirOpciones()
    {
        panelBotones.SetActive(false);
        panelOpciones.SetActive(true);
    }

    private void CerrarOpciones()
    {
        panelBotones.SetActive(true);
        panelOpciones.SetActive(false);
    }

    private void IrAlMenu()
    {
        Salir(escenaMenu);
    }

    private void IrAMinijuegos()
    {
        if (BrewKiosko.EnPortal)
        {
            saliendo = true;
            BrewKiosko.Obtener().Cerrar();
            return;
        }
        Salir(escenaMinijuegos);
    }

    // sigue en pausa mientras baja la música, MusicaJuego regresa el tiempo a 1 antes de cargar
    private void Salir(string escena)
    {
        saliendo = true;
        if (musica != null)
        {
            musica.SalirA(escena);
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(escena);
    }

    private void BloquearPausa()
    {
        puedePausar = false;
        botonPausa.gameObject.SetActive(false);
    }

    private void PermitirPausa()
    {
        puedePausar = true;
        botonPausa.gameObject.SetActive(true);
    }
}
