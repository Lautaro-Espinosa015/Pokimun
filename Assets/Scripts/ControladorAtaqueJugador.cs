using UnityEngine;

/// <summary>
/// Opciones de jugada para el combate estilo Piedra, Papel o Tijeras.
/// </summary>
public enum JugadaRPS
{
    Piedra,
    Papel,
    Tijera
}

/// <summary>
/// Módulo encargado de obtener la jugada o ataque del jugador.
/// Diseñado para ser modular: otro desarrollador puede modificar este script,
/// heredar de él, o conectar un sistema de detección de gestos por cámara / UI.
/// </summary>
public class ControladorAtaqueJugador : MonoBehaviour
{
    [Header("Configuración de Prueba (Demo)")]
    [Tooltip("Jugada que se enviará por defecto al atacar durante las pruebas.")]
    [SerializeField] private JugadaRPS jugadaSeleccionada = JugadaRPS.Piedra;

    /// <summary>
    /// Método principal llamado por el GestorNivel para obtener la elección del jugador.
    /// Puede ser sobreescrito o implementado por otro script para retornar el resultado
    /// de un modelo de visión por computadora, gestos, reconocimiento de voz o botones UI.
    /// </summary>
    /// <returns>La jugada seleccionada (Piedra, Papel o Tijera).</returns>
    public virtual JugadaRPS ObtenerJugadaAtaque()
    {
        Debug.Log($"[ControladorAtaqueJugador] Jugada ejecutada: {jugadaSeleccionada}");
        return jugadaSeleccionada;
    }

    /// <summary>
    /// Permite asignar programáticamente la jugada (ej. al presionar un botón de UI o detectar un gesto).
    /// </summary>
    public void EstablecerJugada(JugadaRPS nuevaJugada)
    {
        jugadaSeleccionada = nuevaJugada;
        Debug.Log($"[ControladorAtaqueJugador] Jugada preparada: {jugadaSeleccionada}");
    }
}
