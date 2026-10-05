using UnityEngine;
using UnityEngine.InputSystem;

// script temporal para probar la torre, se reemplaza por JuegoManager en el paso 8
public class PruebaFisica : MonoBehaviour
{
    [SerializeField] private Torre torre;
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadError = 0.3f;

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (teclado != null && teclado.rKey.wasPressedThisFrame)
        {
            torre.Reiniciar();
        }

        if (torre.Terminada) return;

        if (!torre.HayBloqueBalanceando)
        {
            torre.PrepararBloque(Random.value >= probabilidadError);
            return;
        }

        bool clic = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool espacio = teclado != null && teclado.spaceKey.wasPressedThisFrame;
        if (clic || espacio)
        {
            torre.SoltarBloque();
        }
    }
}
