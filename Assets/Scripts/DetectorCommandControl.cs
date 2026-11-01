using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Voz y teclado pasan por la misma validación del gestor. Este componente nunca anima ni aplica daño.</summary>
public class DetectorCommandControl : MonoBehaviour
{
    [Header("Referencias del combate")]
    [SerializeField] private GestorNivel gestorNivel;
    [Header("Potencia por voz")]
    [Tooltip("Umbral RMS del crítico en Windows. Se conserva el valor anterior.")]
    [SerializeField, Range(.01f, 1)] private float umbralGritoRms = .12f;
    [Tooltip("Umbral RMS del crítico en Linux. Ajustar según la ganancia del micrófono; mayor valor exige más potencia.")]
    [SerializeField, Range(.01f, 1)] private float umbralGritoRmsLinux = .24f;
    [Tooltip("Valor inicial para Android; calibrar en el teléfono real.")]
    [SerializeField, Range(.01f, 1)] private float umbralGritoRmsAndroid = .24f;
    [SerializeField, Min(.1f)] private float ventanaNivelVoz = 1f;
    private readonly Dictionary<string, AccionTurno> comandos = new Dictionary<string, AccionTurno>();
    private readonly Dictionary<string, Action> comandosMenu = new Dictionary<string, Action>();
    private readonly Queue<Frase> frases = new Queue<Frase>();
    private struct Frase { public string texto; public DateTime inicioUtc; public float nivelVoz; }
    private GestorVozMultiplataforma voz;

    private float UmbralGritoActual => Application.platform == RuntimePlatform.Android ? umbralGritoRmsAndroid :
        voz != null && voz.UsaVosk ? umbralGritoRmsLinux : umbralGritoRms;

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
                {"t", "T"}, {"te", "T"}, {"té", "T"}, {"ti", "T"}, {"tango", "T"},
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
        
        voz = GetComponent<GestorVozMultiplataforma>();
        if (voz == null) voz = gameObject.AddComponent<GestorVozMultiplataforma>();
        voz.Iniciar(comandos.Keys.Concat(comandosMenu.Keys), AlReconocerFrase, ventanaNivelVoz);
    }

    private void Update()
    {
        bool disponible = gestorNivel != null && gestorNivel.PuedeRecibirAcciones && Application.isFocused && !Escribiendo();
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
                continue;
            }

            if (!disponible || !gestorNivel.PuedeRecibirAcciones || frase.inicioUtc < gestorNivel.InicioVentanaEntradaUtc)
            {
                Debug.Log($"[Voz] Descartado '{frase.texto}': selección cerrada, pausada o comando de una ventana anterior.", this);
                continue;
            }
            if (!comandos.TryGetValue(frase.texto, out AccionTurno accion)) continue;
            float rms = frase.nivelVoz;
            float umbral = UmbralGritoActual;
            accion.critico = accion.tipo == AccionCombate.Atacar && rms >= umbral;
            bool aceptado = gestorNivel.IntentarRegistrarAccion(accion, "voz: " + frase.texto);
            Debug.Log($"[Voz] proveedor={voz?.NombreProveedor} frase='{frase.texto}' RMS={rms:F3} umbral={umbral:F3} crítico={accion.critico} aceptado={aceptado}", this);
        }
        if (!disponible || !gestorNivel.PuedeRecibirAcciones || Keyboard.current == null) return;
        Keyboard k = Keyboard.current;
        bool critico = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Defensa(), "teclado 1");
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Curar(), "teclado 2");
        else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame)
            gestorNivel.IntentarAtaqueManual(critico, "teclado 3");
        else if (k.dKey.wasPressedThisFrame || k.digit0Key.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame)
            gestorNivel.IntentarRegistrarAccion(AccionTurno.Defensa(), "teclado D/0");
        else if (k.aKey.wasPressedThisFrame)
            gestorNivel.IntentarAtaqueManual(critico, "teclado A");
    }

    private static bool Escribiendo()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return false;
        var campo = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
        return campo != null && campo.isFocused;
    }

    private void AlReconocerFrase(GestorVozMultiplataforma.ComandoReconocido resultado)
    {
        lock (frases)
        {
            if (frases.Count >= 8) frases.Dequeue();
            frases.Enqueue(new Frase { texto = resultado.texto, inicioUtc = resultado.inicioUtc, nivelVoz = resultado.nivelVoz });
        }
    }

    private void OnDisable()
    {
        voz?.Detener();
        lock (frases) frases.Clear();
    }
}
