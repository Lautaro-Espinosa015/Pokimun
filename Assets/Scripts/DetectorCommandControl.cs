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
    private readonly Dictionary<string, Action> comandosMenu = new Dictionary<string, Action>();
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
        comandosMenu.Clear();
        foreach (string palabra in new[] { "roca", "piedra", "rock", "escudo", "defensa", "bloqueo", "shield", "protect" }) comandos[palabra] = AccionTurno.Defensa();
        foreach (string palabra in new[] { "hoja", "papel", "paper", "cura", "sanacion", "sanación", "curacion", "regeneracion", "curar", "heal", "healing" }) comandos[palabra] = AccionTurno.Curar();
        foreach (string palabra in new[] { "tijera", "tijeras", "scissors", "hidro pulso", "piro pulso", "atacar", "attack", "water pulse", "fire pulse" }) comandos[palabra] = AccionTurno.Ataque(JugadaRPS.Tijera);
        
        if (gestorNivel != null)
        {
            comandosMenu["solitario"] = gestorNivel.IniciarPartida;
            comandosMenu["jugar en solitario"] = gestorNivel.IniciarPartida;
            comandosMenu["singol pleyer"] = gestorNivel.IniciarPartida;
            comandosMenu["single player"] = gestorNivel.IniciarPartida;
            comandosMenu["alon"] = gestorNivel.IniciarPartida;
            comandosMenu["alone"] = gestorNivel.IniciarPartida;

            comandosMenu["multijugador"] = gestorNivel.CrearSala;
            comandosMenu["crear partida multijugador"] = gestorNivel.CrearSala;
            comandosMenu["multi pleyer"] = gestorNivel.CrearSala;
            comandosMenu["multiplayer"] = gestorNivel.CrearSala;
            comandosMenu["on lain"] = gestorNivel.CrearSala;
            comandosMenu["online"] = gestorNivel.CrearSala;

            // Comandos de fin de partida (Victoria/Derrota)
            comandosMenu["revancha"] = gestorNivel.IniciarPartida;
            comandosMenu["jugar de nuevo"] = gestorNivel.IniciarPartida;
            comandosMenu["volver a jugar"] = gestorNivel.IniciarPartida;
            comandosMenu["reintentar"] = gestorNivel.IniciarPartida;
            comandosMenu["rimach"] = gestorNivel.IniciarPartida;
            comandosMenu["rematch"] = gestorNivel.IniciarPartida;
            comandosMenu["plei aguen"] = gestorNivel.IniciarPartida;
            comandosMenu["play again"] = gestorNivel.IniciarPartida;
            comandosMenu["ritrai"] = gestorNivel.IniciarPartida;
            comandosMenu["retry"] = gestorNivel.IniciarPartida;
            
            comandosMenu["salir"] = gestorNivel.SalirDelJuego;
            comandosMenu["exit"] = gestorNivel.SalirDelJuego;
            comandosMenu["quit"] = gestorNivel.SalirDelJuego;
            comandosMenu["cuit"] = gestorNivel.SalirDelJuego;
            comandosMenu["lib"] = gestorNivel.SalirDelJuego;
            comandosMenu["leave"] = gestorNivel.SalirDelJuego;

            comandosMenu["volver"] = gestorNivel.VolverAlMenu;
            comandosMenu["volver al menu"] = gestorNivel.VolverAlMenu;
            comandosMenu["regresar a menu"] = gestorNivel.VolverAlMenu;
            comandosMenu["menu"] = gestorNivel.VolverAlMenu;
            
            // Dictado de código de sala
            Action<string> appendChar = (c) => FindFirstObjectByType<FeedbackAtaqueUI>()?.AnadirCaracterCodigo(c);
            
            var abecedarioLetras = new Dictionary<string, string>() {
                {"a", "A"}, {"ei", "A"}, {"alpha", "A"}, {"alfa", "A"},
                {"b", "B"}, {"be", "B"}, {"bi", "B"}, {"bravo", "B"},
                {"c", "C"}, {"ce", "C"}, {"si", "C"}, {"charlie", "C"},
                {"d", "D"}, {"de", "D"}, {"di", "D"}, {"delta", "D"},
                {"e", "E"}, {"echo", "E"},
                {"f", "F"}, {"efe", "F"}, {"ef", "F"}, {"foxtrot", "F"},
                {"g", "G"}, {"ge", "G"}, {"lli", "G"}, {"golf", "G"},
                {"h", "H"}, {"hache", "H"}, {"eich", "H"}, {"hotel", "H"},
                {"i", "I"}, {"ai", "I"}, {"india", "I"},
                {"j", "J"}, {"jota", "J"}, {"yei", "J"}, {"juliet", "J"},
                {"k", "K"}, {"ca", "K"}, {"kei", "K"}, {"kilo", "K"},
                {"l", "L"}, {"ele", "L"}, {"el", "L"}, {"lima", "L"},
                {"m", "M"}, {"eme", "M"}, {"em", "M"}, {"mike", "M"},
                {"n", "N"}, {"ene", "N"}, {"en", "N"}, {"november", "N"},
                {"o", "O"}, {"ou", "O"}, {"oscar", "O"},
                {"p", "P"}, {"pe", "P"}, {"pi", "P"}, {"papa", "P"},
                {"q", "Q"}, {"cu", "Q"}, {"kiu", "Q"}, {"quebec", "Q"},
                {"r", "R"}, {"erre", "R"}, {"ar", "R"}, {"romeo", "R"},
                {"s", "S"}, {"ese", "S"}, {"es", "S"}, {"sierra", "S"},
                {"t", "T"}, {"te", "T"}, {"ti", "T"}, {"tango", "T"},
                {"u", "U"}, {"iu", "U"}, {"uniform", "U"},
                {"v", "V"}, {"ve", "V"}, {"vi", "V"}, {"victor", "V"},
                {"w", "W"}, {"doble ve", "W"}, {"doble u", "W"}, {"whiskey", "W"},
                {"x", "X"}, {"equis", "X"}, {"ex", "X"}, {"x ray", "X"},
                {"y", "Y"}, {"i griega", "Y"}, {"ye", "Y"}, {"wai", "Y"}, {"yankee", "Y"},
                {"z", "Z"}, {"zeta", "Z"}, {"ceta", "Z"}, {"zi", "Z"}, {"zulu", "Z"}
            };
            
            var abecedarioNumeros = new Dictionary<string, string>() {
                {"cero", "0"}, {"zero", "0"}, {"ou", "0"},
                {"uno", "1"}, {"one", "1"}, {"uan", "1"},
                {"dos", "2"}, {"two", "2"}, {"tu", "2"},
                {"tres", "3"}, {"three", "3"}, {"zri", "3"},
                {"cuatro", "4"}, {"four", "4"}, {"for", "4"},
                {"cinco", "5"}, {"five", "5"}, {"faiv", "5"},
                {"seis", "6"}, {"six", "6"},
                {"siete", "7"}, {"seven", "7"},
                {"ocho", "8"}, {"eight", "8"}, {"eit", "8"},
                {"nueve", "9"}, {"nine", "9"}, {"nain", "9"}
            };

            foreach (var kvp in abecedarioLetras)
            {
                string key = kvp.Key;
                string character = kvp.Value;
                comandosMenu[$"letra {key}"] = () => appendChar(character);
                comandosMenu[$"letra {key} "] = () => appendChar(character);
            }
            
            foreach (var kvp in abecedarioNumeros)
            {
                string key = kvp.Key;
                string character = kvp.Value;
                comandosMenu[$"numero {key}"] = () => appendChar(character);
                comandosMenu[$"número {key}"] = () => appendChar(character);
                comandosMenu[$"numero {key} "] = () => appendChar(character);
                comandosMenu[$"número {key} "] = () => appendChar(character);
            }

            comandosMenu["borrar letra"] = () => FindFirstObjectByType<FeedbackAtaqueUI>()?.BorrarCaracterCodigo();
            comandosMenu["borrar numero"] = () => FindFirstObjectByType<FeedbackAtaqueUI>()?.BorrarCaracterCodigo();
            comandosMenu["borrar caracter"] = () => FindFirstObjectByType<FeedbackAtaqueUI>()?.BorrarCaracterCodigo();
            comandosMenu["borrar"] = () => FindFirstObjectByType<FeedbackAtaqueUI>()?.BorrarCaracterCodigo();
            
            Action unirseAction = () => {
                var ui = FindFirstObjectByType<FeedbackAtaqueUI>();
                if (ui != null) ui.UnirseConCodigoVoz((code) => gestorNivel.UnirseSala(code));
            };
            comandosMenu["unirse"] = unirseAction;
            comandosMenu["unirme"] = unirseAction;
            comandosMenu["join"] = unirseAction;
            comandosMenu["lloin"] = unirseAction;
            comandosMenu["entrar"] = unirseAction;
        }
        
        comandosMenu["pausa"] = () => FindFirstObjectByType<PauseManager>()?.Pausar();
        comandosMenu["pausar"] = () => FindFirstObjectByType<PauseManager>()?.Pausar();
        comandosMenu["pos"] = () => FindFirstObjectByType<PauseManager>()?.Pausar();
        comandosMenu["pause"] = () => FindFirstObjectByType<PauseManager>()?.Pausar();

        comandosMenu["reanudar"] = () => FindFirstObjectByType<PauseManager>()?.Reanudar();
        comandosMenu["continuar"] = () => FindFirstObjectByType<PauseManager>()?.Reanudar();
        comandosMenu["quitar pausa"] = () => FindFirstObjectByType<PauseManager>()?.Reanudar();
        comandosMenu["risiom"] = () => FindFirstObjectByType<PauseManager>()?.Reanudar();
        comandosMenu["resume"] = () => FindFirstObjectByType<PauseManager>()?.Reanudar();
        comandosMenu["continue"] = () => FindFirstObjectByType<PauseManager>()?.Reanudar();

        comandosMenu["salir del juego"] = () => FindFirstObjectByType<PauseManager>()?.SalirJuego();
        comandosMenu["cerrar juego"] = () => FindFirstObjectByType<PauseManager>()?.SalirJuego();
        
        string[] palabrasNumeros = { "cero", "diez", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa", "cien" };
        for (int i = 0; i <= 10; i++)
        {
            float vol = i / 10f;
            int volText = i * 10;
            string palabraNum = palabrasNumeros[i];
            
            Action actFX = () => { 
                var o = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude); 
                if(o != null) { o.changeVolumenEfectos(vol); Debug.Log($"[Voz] Volumen Efectos cambiado al {volText}%"); return; } 
                
                var p = FindFirstObjectByType<PauseManager>();
                if (p != null && p.isPaused) {
                    p.CambiarVolumenEfectos(vol);
                    
                    var todosLosSliders = FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach(var s in todosLosSliders) {
                        string n = s.name.ToLower();
                        if (n.Contains("efecto") || n.Contains("fx")) s.SetValueWithoutNotify(vol);
                    }
                    
                    Debug.Log($"[Voz] Volumen Efectos cambiado al {volText}% (Pausa)");
                } else { Debug.Log("[Voz] Comando ignorado: El juego no está pausado o en Opciones."); }
            };
            Action actMusica = () => { 
                var o = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude); 
                if(o != null) { o.changeVolumenMusica(vol); Debug.Log($"[Voz] Volumen Música cambiado al {volText}%"); return; } 
                
                var p = FindFirstObjectByType<PauseManager>();
                if (p != null && p.isPaused) {
                    p.CambiarVolumenMusica(vol);
                    
                    var todosLosSliders = FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach(var s in todosLosSliders) {
                        string n = s.name.ToLower();
                        if (n.Contains("musica") || n.Contains("música")) s.SetValueWithoutNotify(vol);
                    }
                    
                    Debug.Log($"[Voz] Volumen Música cambiado al {volText}% (Pausa)");
                } else { Debug.Log("[Voz] Comando ignorado: El juego no está pausado o en Opciones."); }
            };
            
            comandosMenu[$"efectos {palabraNum}"] = actFX;
            comandosMenu[$"efe equis {palabraNum}"] = actFX;
            comandosMenu[$"musica {palabraNum}"] = actMusica;
            comandosMenu[$"música {palabraNum}"] = actMusica;
        }
        
        falloMicrofono = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            var todasLasPalabras = comandos.Keys.Concat(comandosMenu.Keys).ToArray();
            reconocedor = new KeywordRecognizer(todasLasPalabras);
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
            if (comandosMenu.TryGetValue(frase.texto, out Action accionMenu))
            {
                Debug.Log($"[Voz] Ejecutando acción de menú: {frase.texto}", this);
                accionMenu.Invoke();
                mediciones.Clear();
                continue;
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
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Defensa(), "teclado 1");
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Curar(), "teclado 2");
        else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Ataque(JugadaRPS.Tijera, critico), "teclado 3");
        else if (k.dKey.wasPressedThisFrame || k.digit0Key.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Defensa(), "teclado D/0");
        else if (k.aKey.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Ataque(JugadaRPS.Tijera, critico), "teclado A / gesto preparado");
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
