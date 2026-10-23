using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;

    public TMP_Text coinText;

    private int monedas = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ActualizarTexto();
    }

    public void AgregarMoneda()
    {
        monedas++;

        ActualizarTexto();
    }

    void ActualizarTexto()
    {
        coinText.text = "× " + monedas;
    }
    public int TotalMonedas
        {
            get { return monedas; }
        }
}