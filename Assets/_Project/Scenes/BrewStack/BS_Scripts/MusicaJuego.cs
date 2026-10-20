using UnityEngine;

public class MusicaJuego : MonoBehaviour
{
    [SerializeField] private ResultadoFinalUI resultado;
    [SerializeField] private AudioClip musica;
    [Range(0f, 1f)]
    [SerializeField] private float volumenMusica = 0.06f;
    // cuánto baja al mostrar el resultado, para que se oiga el jingle
    [Range(0f, 1f)]
    [SerializeField] private float factorAlResultado = 0.5f;
    [SerializeField] private float velocidadCambio = 0.1f;

    private AudioSource fuente;
    private float volumenObjetivo;

    private void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.clip = musica;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.volume = volumenMusica;
        volumenObjetivo = volumenMusica;
    }

    private void OnEnable()
    {
        resultado.AlMostrar += Bajar;
        resultado.AlJugarDeNuevo += Subir;
    }

    private void OnDisable()
    {
        resultado.AlMostrar -= Bajar;
        resultado.AlJugarDeNuevo -= Subir;
    }

    private void Start()
    {
        if (musica != null) fuente.Play();
    }

    private void Update()
    {
        fuente.volume = Mathf.MoveTowards(fuente.volume, volumenObjetivo, velocidadCambio * Time.deltaTime);
    }

    private void Bajar()
    {
        volumenObjetivo = volumenMusica * factorAlResultado;
    }

    private void Subir()
    {
        volumenObjetivo = volumenMusica;
    }
}
