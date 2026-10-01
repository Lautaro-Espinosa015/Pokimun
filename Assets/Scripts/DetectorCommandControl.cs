using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

/// <summary>
/// Reconoce los comandos de voz del combate y los envía al GestorNivel.
/// Comandos: Roca (ataque 1), Hoja (ataque 2), Tijera (ataque 3), Escudo (defensa).
/// </summary>
public class DetectorCommandControl : MonoBehaviour
{
    private struct MedicionMicrofono
    {
        public float tiempo;
        public float rms;

        public MedicionMicrofono(float tiempo, float rms)
        {
            this.tiempo = tiempo;
            this.rms = rms;
        }
    }

    [Header("Referencias")]
    [SerializeField] private GestorNivel gestorNivel;

    [Header("Potencia por voz")]
    [Tooltip("Nivel RMS mínimo del micrófono para considerar que el comando fue gritado. Ajustar según el micrófono.")]
    [SerializeField, Range(0.01f, 1f)] private float umbralGritoRms = 0.12f;
    [Tooltip("Cuánto tiempo se conserva el nivel máximo de voz antes de reconocer el comando.")]
    [SerializeField, Min(0.1f)] private float ventanaNivelVoz = 1f;
    [SerializeField, Min(8000)] private int frecuenciaMuestreoMicrofono = 16000;

    private KeywordRecognizer reconocedor;
    private readonly Dictionary<string, Action> comandos = new Dictionary<string, Action>();
    private AudioClip clipMicrofono;
    private float[] muestrasMicrofono = new float[1024];
    private readonly Queue<MedicionMicrofono> medicionesRecientes = new Queue<MedicionMicrofono>();
    private bool microfonoDisponible;

    private void Start()
    {
        if (gestorNivel == null)
        {
            gestorNivel = GetComponent<GestorNivel>();
        }

        if (gestorNivel == null)
        {
            gestorNivel = FindFirstObjectByType<GestorNivel>();
        }

        PrepararComandos();
        IniciarMedicionMicrofono();
        IniciarReconocedor();
    }

    private void Update()
    {
        MedirNivelMicrofono();
    }

    private void OnDisable()
    {
        if (reconocedor != null)
        {
            reconocedor.OnPhraseRecognized -= AlReconocerFrase;
            if (reconocedor.IsRunning)
            {
                reconocedor.Stop();
            }
            reconocedor.Dispose();
            reconocedor = null;
        }

        if (microfonoDisponible)
        {
            Microphone.End(null);
            microfonoDisponible = false;
        }
    }

    private void PrepararComandos()
    {
        comandos.Clear();
        comandos.Add("roca", () => EjecutarAtaque(JugadaRPS.Piedra));
        comandos.Add("hoja", () => EjecutarAtaque(JugadaRPS.Papel));
        comandos.Add("tijera", () => EjecutarAtaque(JugadaRPS.Tijera));
        comandos.Add("escudo", EjecutarDefensa);
    }

    private void IniciarReconocedor()
    {
        try
        {
            reconocedor = new KeywordRecognizer(comandos.Keys.ToArray());
            reconocedor.OnPhraseRecognized += AlReconocerFrase;
            reconocedor.Start();
            Debug.Log("[DetectorCommandControl] Escuchando: Roca, Hoja, Tijera y Escudo.");
        }
        catch (Exception excepcion)
        {
            Debug.LogError($"[DetectorCommandControl] No se pudo iniciar el reconocimiento de voz: {excepcion.Message}");
        }
    }

    private void IniciarMedicionMicrofono()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[DetectorCommandControl] No se encontró un micrófono para medir el volumen; los ataques no podrán potenciarse por grito.");
            return;
        }

        clipMicrofono = Microphone.Start(null, true, 1, frecuenciaMuestreoMicrofono);
        microfonoDisponible = clipMicrofono != null;
        if (!microfonoDisponible)
        {
            Debug.LogWarning("[DetectorCommandControl] No se pudo abrir el micrófono para medir el volumen.");
        }
    }

    private void MedirNivelMicrofono()
    {
        if (!microfonoDisponible || clipMicrofono == null)
        {
            return;
        }

        int posicion = Microphone.GetPosition(null);
        int inicio = posicion - muestrasMicrofono.Length;
        if (posicion <= 0 || inicio < 0 || !clipMicrofono.GetData(muestrasMicrofono, inicio))
        {
            return;
        }

        double sumaCuadrados = 0;
        for (int i = 0; i < muestrasMicrofono.Length; i++)
        {
            sumaCuadrados += muestrasMicrofono[i] * muestrasMicrofono[i];
        }

        float rms = Mathf.Sqrt((float)(sumaCuadrados / muestrasMicrofono.Length));
        float tiempoActual = Time.unscaledTime;
        while (medicionesRecientes.Count > 0 && tiempoActual - medicionesRecientes.Peek().tiempo > ventanaNivelVoz)
        {
            medicionesRecientes.Dequeue();
        }

        medicionesRecientes.Enqueue(new MedicionMicrofono(tiempoActual, rms));
    }

    private void AlReconocerFrase(PhraseRecognizedEventArgs args)
    {
        string frase = args.text.Trim().ToLowerInvariant();
        float nivelVoz = ObtenerNivelMaximoReciente();
        Debug.Log($"[DetectorCommandControl] Reconocido: '{args.text}' (confianza: {args.confidence}, volumen: {nivelVoz:F2}).");

        if (comandos.TryGetValue(frase, out Action comando))
        {
            comando.Invoke();
        }
    }

    private void EjecutarAtaque(JugadaRPS jugada)
    {
        if (gestorNivel == null)
        {
            Debug.LogWarning("[DetectorCommandControl] No hay un GestorNivel asignado.");
            return;
        }

        bool gritado = microfonoDisponible && ObtenerNivelMaximoReciente() >= umbralGritoRms;
        gestorNivel.JugadorSeleccionarAtaque(jugada, gritado);
        medicionesRecientes.Clear();
    }

    private void EjecutarDefensa()
    {
        if (gestorNivel == null)
        {
            Debug.LogWarning("[DetectorCommandControl] No hay un GestorNivel asignado.");
            return;
        }

        gestorNivel.JugadorSeleccionarDefensa();
        medicionesRecientes.Clear();
    }

    private float ObtenerNivelMaximoReciente()
    {
        float nivelMaximo = 0f;
        foreach (MedicionMicrofono medicion in medicionesRecientes)
        {
            nivelMaximo = Mathf.Max(nivelMaximo, medicion.rms);
        }

        return nivelMaximo;
    }
}
