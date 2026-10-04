using System;
using UnityEngine;

public class Vida : MonoBehaviour
{
    [Header("Configuración")]
    [Min(1)] public int maxVida = 100;
    public int vidaActual;
    [Header("Referencia a la UI")]
    public BarraVida barraVida;
    public event Action<int, int> OnVidaCambiada;

    private void Awake() { vidaActual = Mathf.Max(1, maxVida); }
    private void Start() { SincronizarBarra(); }

    public void ReiniciarVida()
    {
        maxVida = Mathf.Max(1, maxVida);
        EstablecerVida(maxVida);
    }

    public void EstablecerVida(int valor)
    {
        maxVida = Mathf.Max(1, maxVida);
        vidaActual = Mathf.Clamp(valor, 0, maxVida);
        SincronizarBarra();
        OnVidaCambiada?.Invoke(vidaActual, maxVida);
    }

    public void RecibirDanio(int danio)
    {
        if (danio > 0) EstablecerVida(Mathf.Max(0, vidaActual - danio));
    }

    public void Curar(int cantidad)
    {
        if (cantidad > 0) EstablecerVida((int)Math.Min(maxVida, (long)vidaActual + cantidad));
    }

    public void SincronizarBarra()
    {
        // Un componente desactivado de un antiguo objeto de prueba no debe escribir en la UI.
        if (isActiveAndEnabled && barraVida != null) barraVida.MostrarVida(vidaActual, maxVida);
    }
}
