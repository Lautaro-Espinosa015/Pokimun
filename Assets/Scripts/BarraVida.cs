using UnityEngine;
using UnityEngine.UI;

public class BarraVida : MonoBehaviour
{
    public Slider slider;

    // Configura el valor máximo y actual del Slider
    public void InicializarBarra(int maxVida)
    {
        if (slider != null)
        {
            slider.maxValue = maxVida;
            slider.value = maxVida;
        }
    }

    // Actualiza la posición del Slider con la vida restante
    public void ActualizarVida(int vidaActual)
    {
        if (slider != null)
        {
            slider.value = vidaActual;
        }
    }
}
