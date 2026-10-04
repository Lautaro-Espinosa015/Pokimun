using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

/// <summary>Voz y teclado pasan por la misma validación del gestor. Este componente nunca anima ni aplica daño.</summary>
public class DetectorCommandControl : MonoBehaviour
{
    [Header("Referencias del combate")]
    [SerializeField] private GestorNivel gestorNivel;
    [Header("Potencia por voz")]
    [SerializeField, Range(.01f, 1)] private float umbralGritoRms = .12f;
    [SerializeField, Min(.1f)] private float ventanaNivelVoz = 1f;
    [SerializeField, Min(8000)] private int frecuenciaMuestreoMicrofono = 16000;
    private readonly Dictionary<string, AccionTurno> comandos = new Dictionary<string, AccionTurno>();
    private readonly Queue<Frase> frases = new Queue<Frase>();
    private readonly Queue<Medicion> mediciones = new Queue<Medicion>();
    private struct Frase { public string texto; public DateTime inicioUtc; }
    private struct Medicion { public float tiempo, rms; }
    private AudioClip clipMicrofono;
    private float[] muestras;
    private bool falloMicrofono;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private KeywordRecognizer reconocedor;
#endif

    private void OnEnable()
    {
        if (gestorNivel == null) gestorNivel = GetComponent<GestorNivel>();
        if (gestorNivel == null) gestorNivel = FindFirstObjectByType<GestorNivel>();
        comandos.Clear();
        foreach (string palabra in new[] { "roca", "piedra" }) comandos[palabra] = AccionTurno.Ataque(JugadaRPS.Piedra);
        foreach (string palabra in new[] { "hoja", "papel" }) comandos[palabra] = AccionTurno.Ataque(JugadaRPS.Papel);
        foreach (string palabra in new[] { "tijera", "tijeras", "hidro pulso" }) comandos[palabra] = AccionTurno.Ataque(JugadaRPS.Tijera);
        foreach (string palabra in new[] { "escudo", "defensa", "bloqueo" }) comandos[palabra] = AccionTurno.Defensa();
        falloMicrofono = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            reconocedor = new KeywordRecognizer(comandos.Keys.ToArray());
            reconocedor.OnPhraseRecognized += AlReconocerFrase;
            reconocedor.Start();
            Debug.Log("[Voz] Comandos: roca/piedra, hoja/papel, tijera/hidro pulso y escudo. Volumen alto potencia el ataque.", this);
        }
        catch (Exception ex) { Debug.LogWarning("[Voz] No se pudo iniciar el reconocimiento. El teclado sigue disponible. " + ex.Message, this); }
#else
        Debug.LogWarning("[Voz] El reconocimiento configurado requiere Windows. Usa el teclado en esta plataforma.", this);
#endif
    }

    private void Update()
    {
        bool disponible = gestorNivel != null && gestorNivel.PuedeRecibirAcciones && Application.isFocused && !Escribiendo();
        if (disponible) MedirMicrofono();
        else mediciones.Clear();
        // Los callbacks se drenan en el hilo principal. No se guardan órdenes para la siguiente ronda.
        while (true)
        {
            Frase frase;
            lock (frases)
            {
                if (frases.Count == 0) break;
                frase = frases.Dequeue();
            }
            if (!disponible || !gestorNivel.PuedeRecibirAcciones || frase.inicioUtc < gestorNivel.InicioVentanaEntradaUtc)
            {
                Debug.Log($"[Voz] Descartado '{frase.texto}': selección cerrada, pausada o comando de una ventana anterior.", this);
                continue;
            }
            if (!comandos.TryGetValue(frase.texto, out AccionTurno accion)) continue;
            float rms = NivelReciente();
            accion.critico = accion.tipo == AccionCombate.Atacar && clipMicrofono != null && rms >= umbralGritoRms;
            bool aceptado = gestorNivel.IntentarRegistrarAccion(accion, "voz: " + frase.texto);
            Debug.Log($"[Voz] frase='{frase.texto}' RMS={rms:F3} umbral={umbralGritoRms:F3} crítico={accion.critico} aceptado={aceptado}", this);
            mediciones.Clear();
        }
        if (!disponible || !gestorNivel.PuedeRecibirAcciones || Keyboard.current == null) return;
        Keyboard k = Keyboard.current;
        bool critico = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Ataque(JugadaRPS.Piedra, critico), "teclado 1");
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Ataque(JugadaRPS.Papel, critico), "teclado 2");
        else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Ataque(JugadaRPS.Tijera, critico), "teclado 3");
        else if (k.dKey.wasPressedThisFrame || k.digit0Key.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Defensa(), "teclado D/0");
        else if (k.aKey.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Ataque(gestorNivel.ObtenerJugadaAtaqueJugador(), critico), "teclado A / gesto preparado");
    }

    private static bool Escribiendo()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return false;
        var campo = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
        return campo != null && campo.isFocused;
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void AlReconocerFrase(PhraseRecognizedEventArgs args)
    {
        lock (frases)
        {
            if (frases.Count >= 8) frases.Dequeue();
            frases.Enqueue(new Frase { texto = args.text.Trim().ToLowerInvariant(), inicioUtc = args.phraseStartTime.ToUniversalTime() });
        }
    }
#endif

    private void MedirMicrofono()
    {
        if (falloMicrofono) return;
        try
        {
            if (clipMicrofono == null)
            {
                if (Microphone.devices.Length == 0) throw new InvalidOperationException("No se encontró un micrófono.");
                clipMicrofono = Microphone.Start(null, true, 2, frecuenciaMuestreoMicrofono);
                if (clipMicrofono == null) throw new InvalidOperationException("No se pudo abrir el micrófono.");
                muestras = new float[1024 * clipMicrofono.channels];
            }
            int posicion = Microphone.GetPosition(null);
            if (posicion <= 0) return;
            int inicio = (posicion - 1024 + clipMicrofono.samples) % clipMicrofono.samples;
            if (!clipMicrofono.GetData(muestras, inicio)) return;
            double cuadrados = 0;
            foreach (float muestra in muestras) cuadrados += muestra * muestra;
            mediciones.Enqueue(new Medicion { tiempo = Time.unscaledTime, rms = Mathf.Sqrt((float)(cuadrados / muestras.Length)) });
            ExpirarMediciones();
        }
        catch (Exception ex)
        {
            falloMicrofono = true;
            Debug.LogWarning("[Voz] Medición de potencia no disponible: " + ex.Message, this);
        }
    }

    private void ExpirarMediciones()
    {
        while (mediciones.Count > 0 && Time.unscaledTime - mediciones.Peek().tiempo > ventanaNivelVoz) mediciones.Dequeue();
    }
    private float NivelReciente()
    {
        ExpirarMediciones();
        float maximo = 0;
        foreach (Medicion medicion in mediciones) maximo = Mathf.Max(maximo, medicion.rms);
        return maximo;
    }

    private void OnDisable()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (reconocedor != null)
        {
            reconocedor.OnPhraseRecognized -= AlReconocerFrase;
            if (reconocedor.IsRunning) reconocedor.Stop();
            reconocedor.Dispose();
            reconocedor = null;
        }
#endif
        if (clipMicrofono != null) { Microphone.End(null); Destroy(clipMicrofono); clipMicrofono = null; }
        mediciones.Clear();
        lock (frases) frases.Clear();
    }
}
