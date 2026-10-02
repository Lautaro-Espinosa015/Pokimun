using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.InputSystem;

/// <summary>
/// Reconoce los comandos de voz del combate y (temporalmente) dispara las animaciones directamente
/// para la presentación. Comandos: Roca (ataque 1), Hoja (ataque 2), Tijera (ataque 3), Escudo (defensa).
/// También soporta Numpad 1, 2, 3 y 0 para pruebas sin voz usando el Nuevo Input System.
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

    [Header("Referencias (Demo Animaciones)")]
    [Tooltip("Arrastra aquí a tu personaje Ender_Hydros")]
    [SerializeField] private ControladorAnimaciones controladorAnimaciones;

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

    private bool defendiendoActualmente = false;

    private void Start()
    {
        if (controladorAnimaciones == null)
        {
            controladorAnimaciones = FindFirstObjectByType<ControladorAnimaciones>();
        }

        PrepararComandos();
        IniciarMedicionMicrofono();
        IniciarReconocedor();
    }

    private void Update()
    {
        MedirNivelMicrofono();

        // ======= CONTROLES POR TECLADO (NUEVO INPUT SYSTEM) =======
        if (Keyboard.current != null)
        {
            if (Keyboard.current.numpad1Key.wasPressedThisFrame || Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("[Teclado] Ataque Básico 'Hidro Pulso' (Numpad 1)");
                if (controladorAnimaciones != null) controladorAnimaciones.EjecutarAtaque(JugadaRPS.Tijera);
            }
            if (Keyboard.current.numpad2Key.wasPressedThisFrame || Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("[Teclado] Ataque Crítico 'Hidro Pulso' (Numpad 2)");
                if (controladorAnimaciones != null) controladorAnimaciones.EjecutarAtaqueCritico(JugadaRPS.Tijera);
            }
            if (Keyboard.current.numpad3Key.wasPressedThisFrame || Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                Debug.Log("[Teclado] Alternando Defensa (Numpad 3)");
                EjecutarDefensa();
            }
        }
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
        // Comandos de Ataque (El nivel de voz decidirá si es normal o crítico)
        comandos.Add("hidro pulso", () => EjecutarAtaque(JugadaRPS.Tijera));
        comandos.Add("tijera", () => EjecutarAtaque(JugadaRPS.Tijera));

        // Comandos de Defensa
        comandos.Add("bloqueo", EjecutarDefensa);
        comandos.Add("defensa", EjecutarDefensa);
        comandos.Add("piedra", EjecutarDefensa);
    }

    private void IniciarReconocedor()
    {
        try
        {
            reconocedor = new KeywordRecognizer(comandos.Keys.ToArray());
            reconocedor.OnPhraseRecognized += AlReconocerFrase;
            reconocedor.Start();
            Debug.Log("[DetectorCommandControl] Escuchando: Hidro pulso, Tijera, Bloqueo, Defensa, Piedra.");
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
            Debug.LogWarning("[DetectorCommandControl] No se encontró un micrófono.");
            return;
        }

        clipMicrofono = Microphone.Start(null, true, 1, frecuenciaMuestreoMicrofono);
        microfonoDisponible = clipMicrofono != null;
    }

    private void MedirNivelMicrofono()
    {
        if (!microfonoDisponible || clipMicrofono == null) return;

        int posicion = Microphone.GetPosition(null);
        int inicio = posicion - muestrasMicrofono.Length;
        if (posicion <= 0 || inicio < 0 || !clipMicrofono.GetData(muestrasMicrofono, inicio)) return;

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
        Debug.Log($"[Voz] Dijo: '{frase}' | Volumen: {nivelVoz:F2}");

        if (comandos.TryGetValue(frase, out Action comando))
        {
            comando.Invoke();
        }
    }

    private void EjecutarAtaque(JugadaRPS jugada)
    {
        if (controladorAnimaciones == null)
        {
            Debug.LogWarning("[DetectorCommandControl] Faltan animaciones.");
            return;
        }

        // Si el volumen del micrófono pasa el umbral de grito
        bool gritado = microfonoDisponible && ObtenerNivelMaximoReciente() >= umbralGritoRms;
        
        if (gritado)
        {
            Debug.Log($"[Voz] ¡Se detectó un GRITO para {jugada}! Disparando Crítico.");
            controladorAnimaciones.EjecutarAtaqueCritico(jugada);
        }
        else
        {
            Debug.Log($"[Voz] Ataque normal de {jugada}.");
            controladorAnimaciones.EjecutarAtaque(jugada);
        }

        medicionesRecientes.Clear();
    }

    private void EjecutarDefensa()
    {
        if (controladorAnimaciones == null) return;

        // Alterna entre encender y apagar el escudo
        defendiendoActualmente = !defendiendoActualmente;
        controladorAnimaciones.EjecutarDefensa(defendiendoActualmente);
        Debug.Log($"[Voz/Teclado] Defensa cambiada a: {defendiendoActualmente}");
        
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
