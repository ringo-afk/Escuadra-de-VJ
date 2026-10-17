using UnityEngine;

// volumen que se guarda entre partidas, lo usan el menú y el juego
public static class OpcionesAudio
{
    // con prefijo porque los PlayerPrefs se comparten con los otros minijuegos
    private const string KEY_MUSICA = "BS_VolumenMusica";
    private const string KEY_EFECTOS = "BS_VolumenEfectos";
    private const string KEY_SILENCIO = "BS_Silencio";
    private const float VOLUMEN_INICIAL = 0.8f;

    public static float VolumenMusica
    {
        get { return PlayerPrefs.GetFloat(KEY_MUSICA, VOLUMEN_INICIAL); }
        set { PlayerPrefs.SetFloat(KEY_MUSICA, Mathf.Clamp01(value)); }
    }

    public static float VolumenEfectos
    {
        get { return PlayerPrefs.GetFloat(KEY_EFECTOS, VOLUMEN_INICIAL); }
        set { PlayerPrefs.SetFloat(KEY_EFECTOS, Mathf.Clamp01(value)); }
    }

    public static bool Silencio
    {
        get { return PlayerPrefs.GetInt(KEY_SILENCIO, 0) == 1; }
        set { PlayerPrefs.SetInt(KEY_SILENCIO, value ? 1 : 0); }
    }

    // lo que se multiplica al volumen de cada fuente
    public static float Musica => Silencio ? 0f : VolumenMusica;
    public static float Efectos => Silencio ? 0f : VolumenEfectos;

    public static void Guardar()
    {
        PlayerPrefs.Save();
    }
}
