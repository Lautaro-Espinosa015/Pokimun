using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

/// <summary>
/// Hereda de ControladorAtaqueJugador (Polimorfismo).
/// Se suscribe al HandLandmarkerRunner de MediaPipe para detectar 
/// Piedra, Papel o Tijera analizando las coordenadas de los dedos.
/// </summary>
public class ControladorAtaqueManos : ControladorAtaqueJugador
{
    [Header("Conexión con MediaPipe")]
    [Tooltip("Arrastra aquí el objeto que tiene el script HandLandmarkerRunner")]
    public HandLandmarkerRunner handRunner;

    // Guardaremos internamente la última jugada que la cámara detectó
    private JugadaRPS jugadaDetectadaActualmente = JugadaRPS.Piedra;

    private void OnEnable()
    {
        if (handRunner != null)
        {
            // Nos suscribimos al evento que dispara MediaPipe cada fotograma
            handRunner.OnLandmarksResult += AnalizarGestosMediaPipe;
        }
    }

    private void OnDisable()
    {
        if (handRunner != null)
        {
            handRunner.OnLandmarksResult -= AnalizarGestosMediaPipe;
        }
    }

    /// <summary>
    /// Sobreescribimos el método virtual de tu compañero "lautii".
    /// En lugar de retornar un valor por defecto o presionado por botón,
    /// retornamos la última jugada que la cámara vio.
    /// </summary>
    public override JugadaRPS ObtenerJugadaAtaque()
    {
        Debug.Log($"[ControladorAtaqueManos] El GestorNivel pidió la jugada. Retornando la detectada por la cámara: {jugadaDetectadaActualmente}");
        return jugadaDetectadaActualmente;
    }

    /// <summary>
    /// Este método se llama automáticamente docenas de veces por segundo
    /// cuando MediaPipe ve una mano en la cámara.
    /// </summary>
    private void AnalizarGestosMediaPipe(HandLandmarkerResult resultado)
    {
        // Si no hay manos en la pantalla, no hacemos nada
        if (resultado.handLandmarks == null || resultado.handLandmarks.Count == 0)
        {
            return;
        }

        // Tomamos los puntos (landmarks) de la primera mano que vea
        var mano = resultado.handLandmarks[0].landmarks;

        // MediaPipe tiene 21 puntos. 
        // 8 = Punta del Índice, 6 = Nudillo del Índice
        // 12 = Punta del Medio, 10 = Nudillo del Medio
        // 16 = Punta del Anular, 14 = Nudillo del Anular
        // 20 = Punta del Meñique, 18 = Nudillo del Meñique
        // En la pantalla, un valor 'Y' menor significa que está "más arriba" (hacia el techo).

        bool indiceLevantado = mano[8].y < mano[6].y;
        bool medioLevantado = mano[12].y < mano[10].y;
        bool anularLevantado = mano[16].y < mano[14].y;
        bool meniqueLevantado = mano[20].y < mano[18].y;

        int dedosLevantados = 0;
        if (indiceLevantado) dedosLevantados++;
        if (medioLevantado) dedosLevantados++;
        if (anularLevantado) dedosLevantados++;
        if (meniqueLevantado) dedosLevantados++;

        // Lógica de Piedra, Papel o Tijera
        if (dedosLevantados == 0 || dedosLevantados == 1)
        {
            // Puño cerrado (o solo pulgar)
            jugadaDetectadaActualmente = JugadaRPS.Piedra;
        }
        else if (indiceLevantado && medioLevantado && !anularLevantado && !meniqueLevantado)
        {
            // El clásico símbolo de la paz o tijeras (solo 2 dedos arriba)
            jugadaDetectadaActualmente = JugadaRPS.Tijera;
        }
        else if (dedosLevantados >= 3)
        {
            // Mano abierta
            jugadaDetectadaActualmente = JugadaRPS.Papel;
        }

        // Opcional: Descomenta esto para ver en consola qué detecta en tiempo real
        // Debug.Log($"[MediaPipe] Viendo mano. Gesto actual: {jugadaDetectadaActualmente}");
    }
}
