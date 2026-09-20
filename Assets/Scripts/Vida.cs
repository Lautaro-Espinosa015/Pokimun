using UnityEngine;

public class Vida : MonoBehaviour
{
    [Header("Configuración")]
    public int maxVida = 100;
    public int vidaActual;

    [Header("Referencia a la UI")]
    public BarraVida barraVida;

    void Start()
    {
        vidaActual = maxVida;

        if (barraVida != null)
        {
            barraVida.InicializarBarra(maxVida);
        }
    }

    // Función principal para recibir daño
    public void RecibirDanio(int danio)
    {
        vidaActual -= danio;

        // Evitamos que la vida baje de 0
        if (vidaActual < 0)
        {
            vidaActual = 0;
        }

        // Actualizamos la barra de vida en pantalla
        if (barraVida != null)
        {
            barraVida.ActualizarVida(vidaActual);
        }

        // Comprobamos si el personaje se debilitó
        if (vidaActual <= 0)
        {
            Debug.Log(gameObject.name + " se ha debilitado.");
        }
    }

    // Función opcional para curar
    public void Curar(int cantidad)
    {
        vidaActual += cantidad;

        if (vidaActual > maxVida)
        {
            vidaActual = maxVida;
        }

        if (barraVida != null)
        {
            barraVida.ActualizarVida(vidaActual);
        }
    }
}
