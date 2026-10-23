using UnityEngine;

public class HealthUI : MonoBehaviour
{
    public GameObject[] corazones;

    public void ActualizarVida(int vida)
    {
        for (int i = 0; i < corazones.Length; i++)
        {
            corazones[i].SetActive(i < vida);
        }
    }
}