using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class PointsManager : MonoBehaviour
{
    public TextMeshProUGUI textoPuntos;
    public TextMeshProUGUI textoVacas;

    void Update()
    {
        // GameManager doesn't exist in this branch yet, commenting this out to avoid compilation errors.
        /*
        if (GameManager.Instance != null)
        {
            textoPuntos.text = "Puntos: " + GameManager.Instance.score;
            textoVacas.text = "Vacas: " + GameManager.Instance.cows;
        }
        */
    }
}