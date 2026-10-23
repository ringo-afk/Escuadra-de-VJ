
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    public AudioClip sonidoClick;

    private Button boton;

    void Awake()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(ReproducirClick);
    }

    void ReproducirClick()
    {
        if (sonidoClick != null)
        {
            AudioSource.PlayClipAtPoint(
                sonidoClick,
                Vector3.zero,
                0.9f
            );
        }
    }

    void OnDestroy()
    {
        if (boton != null)
        {
            boton.onClick.RemoveListener(ReproducirClick);
        }
    }
}
