using TMPro;
using UnityEngine;

public class SD_Hotbar : MonoBehaviour
{
    [Header("Referencias")]
    public SD_Player player;
    public SD_LaserWeapon weapon;
    public SD_GameController gameController;

    [Header("Textos")]
    public TMP_Text energiaText;
    public TMP_Text municionText;
    public TMP_Text oleadaText;

    void LateUpdate()
    {
        if (player == null ||
            weapon == null ||
            gameController == null ||
            energiaText == null ||
            municionText == null ||
            oleadaText == null)
        {
            return;
        }

        energiaText.text =
            $"{player.energia:0}/{player.energiaMax:0}";

        municionText.text = weapon.IsReloading
            ? $"Recargando..."
            : $"{weapon.CurrentAmmo}/{weapon.startingAmmo}";

        int totalWaves = gameController.waves.Length;

        oleadaText.text = gameController.CurrentWave == 0
            ? $"Oleada: Preparando… / {totalWaves}"
            : $"Oleada: {gameController.CurrentWave}/{totalWaves}";
    }
}