using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SD_AudioSettings : MonoBehaviour
{
    [Header("Mezclador")]
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private const string MusicKey = "SD_MusicVolume";
    private const string SfxKey = "SD_SFXVolume";

    void Start()
    {
        if (audioMixer == null)
        {
            Debug.LogError("Asigna SD_AudioMixer en AudioSettings.");
            return;
        }

        float musicVolume =
            Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));

        float sfxVolume =
            Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));

        ApplyVolume("MusicVolume", musicVolume);
        ApplyVolume("SFXVolume", sfxVolume);

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
            sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        }
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        ApplyVolume("MusicVolume", value);
        PlayerPrefs.SetFloat(MusicKey, value);
    }

    public void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);

        ApplyVolume("SFXVolume", value);
        PlayerPrefs.SetFloat(SfxKey, value);
    }

    void ApplyVolume(string parameterName, float value)
    {
        if (audioMixer == null)
            return;

        // El mezclador utiliza decibelios.
        float decibels = value <= 0.0001f
            ? -80f
            : Mathf.Log10(value) * 20f;

        if (!audioMixer.SetFloat(parameterName, decibels))
        {
            Debug.LogError(
                $"No se encontró el parámetro expuesto: {parameterName}"
            );
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);

        PlayerPrefs.Save();
    }
}