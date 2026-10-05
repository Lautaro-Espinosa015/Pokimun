using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EstadoJuego { EsperandoInicio, TurnoJugador, TurnoRival, ResolviendoAccion, FinDePartida }

public class GestorNivel : MonoBehaviour
{
    [Header("Configuración de Turnos")]
    [SerializeField, Min(1)] private int maxTurnos = 5;
    [SerializeField] private int turnoActual = 1;
    [Header("Parámetros de Combate")]
    [SerializeField, Min(0)] private int danioBase = 25;
    [SerializeField, Range(0, 1)] private float factorReduccionDefensa = .3f;
    [SerializeField, Min(1)] private float multiplicadorDanioGrito = 1.5f;
    [SerializeField, Range(0, 1)] private float probabilidadDefensaCPU = .35f;
    [SerializeField, Range(0, 1)] private float probabilidadCriticoCPU = .2f;
    [Header("Referencias de Vida")]
    [SerializeField] private Vida vidaJugador;
    [SerializeField] private Vida vidaEnemigo;
    [Header("Módulo de Ataque del Jugador")]
    [SerializeField] private ControladorAtaqueJugador controladorAtaqueJugador;
    [Header("Animación de Personajes")]
    [SerializeField] private ControladorAnimaciones controladorAnimacionesJugador;
    [SerializeField] private ControladorAnimaciones controladorAnimacionesEnemigo;
    [Header("Efectos Visuales de Personaje")]
    [SerializeField] private EfectoVisualPersonaje efectoVisualJugador;
    [SerializeField] private EfectoVisualPersonaje efectoVisualEnemigo;
    [Header("Presentación de Acciones")]
    [SerializeField, Min(4)] private float tiempoFeedbackAtaque = 4f;
    [SerializeField, Min(.5f)] private float tiempoFeedbackDefensa = 1.5f;
    [SerializeField, Min(1)] private float tiempoResultado = 2.5f;
    [Header("Supervisión")]
    [SerializeField] private bool logsDetallados = true;
    [SerializeField] private EstadoJuego estadoActual = EstadoJuego.EsperandoInicio;

    public Action<int, int> OnTurnoActualizado;
    public Action<string> OnMensajeEstado;
    public Action<bool> OnFinPartida;
    public Action<ResultadoPartida> OnResultadoPartida;

    private FeedbackAtaqueUI feedback;
    private GestorRedRelay relay;
    private readonly List<AccionTurno> planLocal = new List<AccionTurno>(2);
    private ResultadoRonda resolucion;
    private bool enLinea, pausado, esMiTurno;
    private int participanteLocal;
    private int bloqueosJugadorLocal;
    private int bloqueosJugadorRemoto;
    private string idPartida;

    public bool EnLinea => enLinea;
    public bool EnBatalla => estadoActual != EstadoJuego.EsperandoInicio && estadoActual != EstadoJuego.FinDePartida;
    public bool PuedeRecibirAcciones => estadoActual == EstadoJuego.TurnoJugador && !pausado &&
        Time.timeScale > 0 && planLocal.Count < ReglasCombate.AccionesPorRonda;
    public DateTime InicioVentanaEntradaUtc { get; private set; }
    public int RondaActual => turnoActual;

    private void Start()
    {
        Time.timeScale = 1;
        if (controladorAtaqueJugador == null) controladorAtaqueJugador = GetComponent<ControladorAtaqueJugador>();
        feedback = GetComponent<FeedbackAtaqueUI>();
        if (feedback == null) feedback = gameObject.AddComponent<FeedbackAtaqueUI>();
        if (controladorAnimacionesJugador == null && vidaJugador != null)
            controladorAnimacionesJugador = vidaJugador.GetComponent<ControladorAnimaciones>();
        if (controladorAnimacionesEnemigo == null && vidaEnemigo != null)
            controladorAnimacionesEnemigo = vidaEnemigo.GetComponent<ControladorAnimaciones>();
        // Auto-encontrar los efectos visuales si no se asignaron manualmente
        if (efectoVisualJugador == null && vidaJugador != null)
            efectoVisualJugador = vidaJugador.GetComponentInChildren<EfectoVisualPersonaje>();
        if (efectoVisualEnemigo == null && vidaEnemigo != null)
            efectoVisualEnemigo = vidaEnemigo.GetComponentInChildren<EfectoVisualPersonaje>();
        if (efectoVisualJugador == null || efectoVisualEnemigo == null)
        {
            var todos = FindObjectsByType<EfectoVisualPersonaje>(FindObjectsSortMode.None);
            foreach (var ef in todos)
            {
                if (efectoVisualJugador == null && ef.name.ToLower().Contains("hydros")) efectoVisualJugador = ef;
                if (efectoVisualEnemigo == null && ef.name.ToLower().Contains("ignis")) efectoVisualEnemigo = ef;
            }
            // Si solo hay 2 en escena y no se resolvieron por nombre, asignar por orden
            if (todos.Length == 2)
            {
                if (efectoVisualJugador == null) efectoVisualJugador = todos[0];
                if (efectoVisualEnemigo == null) efectoVisualEnemigo = todos[1];
            }
        }
        Debug.Log($"[GestorNivel] EfectoVisualJugador={(efectoVisualJugador != null ? efectoVisualJugador.name : "NULL")}  EfectoVisualEnemigo={(efectoVisualEnemigo != null ? efectoVisualEnemigo.name : "NULL")}");
        relay = FindFirstObjectByType<GestorRedRelay>();
        if (relay != null)
        {
            relay.OnEstadoSala += EstadoSala;
            relay.OnRivalListo += IniciarComoHost;
            relay.OnMensaje += RecibirMensaje;
            relay.OnConexionPerdida += ConexionPerdida;
        }
        if (!ReferenciasValidas()) return;
        MostrarSelector();
    }

    private bool ReferenciasValidas()
    {
        if (vidaJugador == null || vidaEnemigo == null || vidaJugador == vidaEnemigo ||
            !vidaJugador.isActiveAndEnabled || !vidaEnemigo.isActiveAndEnabled ||
            vidaJugador.barraVida == null || vidaEnemigo.barraVida == null || vidaJugador.barraVida == vidaEnemigo.barraVida)
        {
            Debug.LogError("[Combate] Asigna dos componentes Vida activos y una barra distinta a cada personaje en GestorNivel.", this);
            estadoActual = EstadoJuego.FinDePartida;
            feedback.MostrarFin("FALTA CONFIGURACIÓN", "Cada personaje necesita su Vida y su propia barra de PS.", null, VolverAlMenu, SalirDelJuego, false, false);
            return false;
        }
        if (controladorAnimacionesJugador == null || controladorAnimacionesEnemigo == null)
            Debug.LogWarning("[Combate] Falta un ControladorAnimaciones. Se mantendrá la duración de presentación.", this);
        return true;
    }

    private void MostrarSelector()
    {
        estadoActual = EstadoJuego.EsperandoInicio;
        feedback.MostrarSala(IniciarPartida, CrearSala, UnirseSala, CancelarSala, VolverAlMenu);
    }

    public void CrearSala()
    {
        if (relay == null) { EstadoSala("La escena no tiene GestorRedRelay.", false); return; }
        _ = relay.CrearPartidaHost();
    }
    public void UnirseSala(string codigo)
    {
        if (relay == null) { EstadoSala("La escena no tiene GestorRedRelay.", false); return; }
        _ = relay.UnirseComoCliente(codigo);
    }
    private void CancelarSala()
    {
        relay?.CancelarConexion();
        feedback.ActualizarSala("Conexión cancelada. Puedes jugar en solitario o crear otra sala.", false);
    }
    private void EstadoSala(string mensaje, bool ocupada)
    {
        if (estadoActual == EstadoJuego.EsperandoInicio) feedback.ActualizarSala(mensaje, ocupada);
    }

    public void IniciarPartida()
    {
        if (feedback == null || (EnBatalla && enLinea)) return;
        relay?.CancelarConexion();
        if (!ReferenciasValidas()) return;
        PrepararPartida(false, 0);
        AbrirRonda(1);
    }

    private void PrepararPartida(bool online, int local)
    {
        StopAllCoroutines();
        enLinea = online;
        participanteLocal = local;
        pausado = false;
        Time.timeScale = 1;
        esMiTurno = !online || relay.EsHost;
        bloqueosJugadorLocal = 0;
        bloqueosJugadorRemoto = 0;
        string identificador = online ? relay.IdPartida : Guid.NewGuid().ToString("N");
        idPartida = identificador.Substring(0, Mathf.Min(8, identificador.Length));
        maxTurnos = Mathf.Max(1, maxTurnos);
        danioBase = Mathf.Max(0, danioBase);
        multiplicadorDanioGrito = Mathf.Max(1, multiplicadorDanioGrito);
        factorReduccionDefensa = Mathf.Clamp01(factorReduccionDefensa);
        tiempoFeedbackAtaque = Mathf.Max(4, tiempoFeedbackAtaque);
        tiempoFeedbackDefensa = Mathf.Max(.5f, tiempoFeedbackDefensa);
        tiempoResultado = Mathf.Max(1, tiempoResultado);
        vidaJugador.ReiniciarVida();
        vidaEnemigo.ReiniciarVida();
        vidaJugador.barraVida.AsignarNombre(participanteLocal == 0 ? "HYDROS · TÚ" : "HYDROS · RIVAL");
        vidaEnemigo.barraVida.AsignarNombre(participanteLocal == 1 ? "IGNIS · TÚ" : online ? "IGNIS · RIVAL" : "IGNIS · CPU");
        FinalizarAnimaciones();
        efectoVisualJugador?.QuitarTodosLosEfectos();
        efectoVisualEnemigo?.QuitarTodosLosEfectos();
        feedback.Limpiar();
        feedback.OcultarSala();
        Log($"INICIO modo={(online ? "multijugador" : "solo")} local={NombreLocal} rondas={maxTurnos}");
    }

    private string NombreLocal => participanteLocal == 0 ? "HYDROS" : "IGNIS";

    private void AbrirRonda(int ronda)
    {
        turnoActual = ronda;
        planLocal.Clear();
        resolucion = null;
        estadoActual = esMiTurno ? EstadoJuego.TurnoJugador : EstadoJuego.TurnoRival;
        InicioVentanaEntradaUtc = DateTime.UtcNow;
        OnTurnoActualizado?.Invoke(turnoActual, maxTurnos);
        if (esMiTurno) ActualizarEstado("Elige dos acciones: ataca, cúrate o defiéndete.");
        else ActualizarEstado("Turno del rival. Esperando acciones...");
        Log("RONDA ABIERTA");
        
        if (!enLinea && !esMiTurno)
        {
            StartCoroutine(TurnoCPU());
        }
    }

    private IEnumerator TurnoCPU()
    {
        yield return new WaitForSeconds(1f);
        var plan = ElegirPlanCPU();
        ResolverYPresentar(plan, false);
    }

    private AccionTurno[] ElegirPlanCPU()
    {
        var plan = new AccionTurno[2];
        int defensa = turnoActual > 1 && UnityEngine.Random.value < probabilidadDefensaCPU ? UnityEngine.Random.Range(0, 2) : -1;
        for (int i = 0; i < plan.Length; i++)
            plan[i] = i == defensa ? AccionTurno.Defensa() :
                AccionTurno.Ataque((JugadaRPS)UnityEngine.Random.Range(0, 3), UnityEngine.Random.value < probabilidadCriticoCPU);
        return plan;
    }

    public void JugadorSeleccionarAtaque() => JugadorSeleccionarAtaque(ObtenerJugadaAtaqueJugador(), false);
    public void JugadorSeleccionarAtaque(JugadaRPS jugada, bool ataqueGritado) =>
        IntentarRegistrarAccion(AccionTurno.Ataque(jugada, ataqueGritado), "API");
    public void JugadorSeleccionarDefensa() => IntentarRegistrarAccion(AccionTurno.Defensa(), "API");
    public JugadaRPS ObtenerJugadaAtaqueJugador() => controladorAtaqueJugador != null ? controladorAtaqueJugador.ObtenerJugadaAtaque() : JugadaRPS.Piedra;

    public bool IntentarRegistrarAccion(AccionTurno accion, string origen)
    {
        if (!PuedeRecibirAcciones || !esMiTurno)
        {
            Log($"ENTRADA RECHAZADA");
            return false;
        }
        if (!ReglasCombate.PuedeAgregar(planLocal, accion, turnoActual, out string motivo))
        {
            ActualizarEstado(motivo);
            return false;
        }
        planLocal.Add(accion);
        Log($"ACCIÓN ACEPTADA {accion.tipo}");
        if (planLocal.Count < 2)
        {
            ActualizarEstado(accion.tipo == AccionCombate.Defender ? "Escudo elegido. Falta una acción." : "Acción elegida. Falta otra.");
            return true;
        }
        
        estadoActual = EstadoJuego.ResolviendoAccion;
        if (enLinea)
        {
            if (!relay.Enviar(new MensajeCombate { tipo = "turno", ronda = turnoActual, jugador = planLocal.ToArray(), 
                vidaJugador = vidaJugador.vidaActual, vidaRival = vidaEnemigo.vidaActual }))
            { CancelarPartida("Se perdió la conexión al enviar turno."); return false; }
        }
        
        ResolverYPresentar(planLocal.ToArray(), true);
        return true;
    }

    private void ResolverYPresentar(AccionTurno[] plan, bool soyAtacante)
    {
        int vidaA = soyAtacante ? vidaJugador.vidaActual : vidaEnemigo.vidaActual;
        int vidaD = soyAtacante ? vidaEnemigo.vidaActual : vidaJugador.vidaActual;
        int vidaMaxA = soyAtacante ? vidaJugador.maxVida : vidaEnemigo.maxVida;
        
        // Los escudos caducan al volver a ser tu turno, no se acumulan entre rondas.
        int bloqA = 0; 
        int bloqD = soyAtacante ? bloqueosJugadorRemoto : bloqueosJugadorLocal;

        // Quitar visualmente el escudo del que ahora es atacante, ya que caducó
        if (soyAtacante) efectoVisualJugador?.QuitarDefensa();
        else efectoVisualEnemigo?.QuitarDefensa();

        resolucion = ReglasCombate.ResolverTurno(turnoActual, maxTurnos, plan,
            vidaA, vidaD, vidaMaxA, bloqA, bloqD, danioBase, multiplicadorDanioGrito, factorReduccionDefensa, soyAtacante);

        // Almacenar el número de bloqueos antes de la resolución para rastrearlo visualmente
        int bloqueosVisualesDefensor = bloqD;

        if (soyAtacante) bloqueosJugadorLocal = resolucion.bloqueosRestantesAtacante;
        else bloqueosJugadorRemoto = resolucion.bloqueosRestantesAtacante;

        if (soyAtacante) bloqueosJugadorRemoto = resolucion.bloqueosRestantesDefensor;
        else bloqueosJugadorLocal = resolucion.bloqueosRestantesDefensor;

        estadoActual = EstadoJuego.ResolviendoAccion;
        ActualizarEstado("Observa el turno.");
        StartCoroutine(PresentarTurno(plan, soyAtacante, bloqueosVisualesDefensor));
    }

    private IEnumerator PresentarTurno(AccionTurno[] plan, bool soyAtacante, int bloqueosVisualesDefensor)
    {
        ControladorAnimaciones animA = soyAtacante ? controladorAnimacionesJugador : controladorAnimacionesEnemigo;
        ControladorAnimaciones animD = soyAtacante ? controladorAnimacionesEnemigo : controladorAnimacionesJugador;
        string nombreA = soyAtacante ? "HYDROS" : "IGNIS";
        string nombreD = soyAtacante ? "IGNIS" : "HYDROS";
        Vida vidaA = soyAtacante ? vidaJugador : vidaEnemigo;
        Vida vidaD = soyAtacante ? vidaEnemigo : vidaJugador;

        foreach (ResultadoIntercambio r in resolucion.intercambios)
        {
            ActualizarEstado($"Acción {r.numero}/2");
            yield return PresentarAccion(r.accion, animA, nombreA, r.numero, !soyAtacante);
            
            vidaA.EstablecerVida(r.vidaAtacanteDespues);
            vidaD.EstablecerVida(r.vidaDefensorDespues);
            
            // Actualizar la velocidad de la música si hay un jugador crítico (<=30%)
            feedback?.ActualizarMusicaBatalla(vidaJugador.vidaActual, vidaJugador.maxVida, vidaEnemigo.vidaActual, vidaEnemigo.maxVida);
            
            if (r.danioAplicado > 0) animD?.RecibirDano();

            // Si el escudo del defensor se agotó en esta acción, quitar el efecto visual
            if (r.bloqueoAplicado)
            {
                bloqueosVisualesDefensor--;
                if (bloqueosVisualesDefensor <= 0)
                {
                    EfectoVisualPersonaje efDefensor = soyAtacante ? efectoVisualEnemigo : efectoVisualJugador;
                    efDefensor?.QuitarDefensa();
                }
            }

            feedback.MostrarResultado(r, nombreA, nombreD, tiempoResultado, !soyAtacante);
            yield return new WaitForSeconds(tiempoResultado);
            FinalizarAnimaciones();
        }

        if (resolucion.resultado != ResultadoPartida.EnCurso)
        {
            TerminarPartida(resolucion.resultado, resolucion.motivo);
            yield break;
        }

        esMiTurno = !esMiTurno;
        if ((enLinea && relay.EsHost && esMiTurno) || (!enLinea && esMiTurno))
        {
            AbrirRonda(turnoActual + 1);
        }
        else if (enLinea && !relay.EsHost && !esMiTurno)
        {
            AbrirRonda(turnoActual + 1);
        }
        else
        {
            AbrirRonda(turnoActual);
        }
    }

    private IEnumerator PresentarAccion(AccionTurno accion, ControladorAnimaciones animador, string nombre, int numero, bool rival)
    {
        float duracion = accion.tipo == AccionCombate.Atacar ? tiempoFeedbackAtaque : tiempoFeedbackDefensa;
        feedback.MostrarAccion(accion, nombre, numero, duracion, rival);
        // Determinar a qué personaje pertenece este turno para los efectos de cuerpo
        EfectoVisualPersonaje efPersonaje = rival ? efectoVisualEnemigo : efectoVisualJugador;
        if (accion.tipo == AccionCombate.Defender)
        {
            animador?.EjecutarDefensa(true);
            efPersonaje?.MostrarDefensa();
        }
        else if (accion.tipo == AccionCombate.Curar)
        {
            efPersonaje?.MostrarCuracion();
        }
        else if (accion.critico) animador?.EjecutarAtaqueCritico(accion.jugada);
        else if (accion.tipo == AccionCombate.Atacar) animador?.EjecutarAtaque(accion.jugada);
        yield return new WaitForSeconds(duracion);
        if (accion.tipo == AccionCombate.Atacar) animador?.FinalizarAccion();
    }

    private void IniciarComoHost()
    {
        if (!ReferenciasValidas()) return;
        var inicio = new MensajeCombate {
            tipo = "inicio", ronda = 1, maxRondas = maxTurnos, danioBase = danioBase,
            vidaMaxJugador = vidaJugador.maxVida, vidaMaxRival = vidaEnemigo.maxVida,
            multiplicadorCritico = multiplicadorDanioGrito, reduccionDefensa = factorReduccionDefensa,
            segundosAtaque = tiempoFeedbackAtaque, segundosDefensa = tiempoFeedbackDefensa, segundosResultado = tiempoResultado
        };
        
        if (estadoActual == EstadoJuego.EsperandoInicio)
        {
            PrepararPartida(true, 0);
            if (!relay.Enviar(inicio)) { CancelarPartida("No se pudo iniciar la partida con el rival."); return; }
            AbrirRonda(1);
        }
        else
        {
            // El cliente no recibió el paquete y lo volvió a pedir, se lo reenviamos
            relay.Enviar(inicio);
        }
    }

    private void RecibirMensaje(MensajeCombate m)
    {
        if (estadoActual == EstadoJuego.FinDePartida) return;
        if (!relay.EsHost && m.tipo == "inicio" && estadoActual == EstadoJuego.EsperandoInicio)
        {
            if (!ConfiguracionValida(m)) { CancelarPartida("El rival envió una configuración de partida inválida."); return; }
            maxTurnos = m.maxRondas;
            danioBase = m.danioBase;
            multiplicadorDanioGrito = m.multiplicadorCritico;
            factorReduccionDefensa = m.reduccionDefensa;
            tiempoFeedbackAtaque = m.segundosAtaque;
            tiempoFeedbackDefensa = m.segundosDefensa;
            tiempoResultado = m.segundosResultado;
            vidaJugador.maxVida = m.vidaMaxJugador;
            vidaEnemigo.maxVida = m.vidaMaxRival;
            PrepararPartida(true, 1);
            AbrirRonda(1);
            return;
        }
        if (!enLinea) return;
        if (m.tipo == "turno" && !esMiTurno && m.ronda == turnoActual)
        {
            if (!ReglasCombate.PlanValido(m.jugador, m.ronda)) { CancelarPartida("El rival envió un plan de acciones inválido."); return; }
            if (!VidaCoincide(m)) { CancelarPartida("Los puntos de vida no coinciden entre los equipos."); return; }
            ResolverYPresentar(m.jugador, false);
        }
    }

    private static bool ConfiguracionValida(MensajeCombate m) => m.ronda == 1 && m.maxRondas > 0 && m.maxRondas <= 1000 &&
        m.danioBase >= 0 && m.danioBase <= 100000 && m.vidaMaxJugador > 0 && m.vidaMaxRival > 0 &&
        m.multiplicadorCritico >= 1 && m.multiplicadorCritico <= 100 && m.reduccionDefensa >= 0 && m.reduccionDefensa <= 1 &&
        m.segundosAtaque >= 4 && m.segundosAtaque <= 30 && m.segundosDefensa >= .5f && m.segundosDefensa <= 30 &&
        m.segundosResultado >= 1 && m.segundosResultado <= 30;
    private bool VidaCoincide(MensajeCombate m) => m.vidaJugador == vidaEnemigo.vidaActual && m.vidaRival == vidaJugador.vidaActual;

    private void ConexionPerdida(string motivo)
    {
        if (enLinea && EnBatalla) CancelarPartida(motivo);
    }
    private void CancelarPartida(string motivo)
    {
        StopAllCoroutines();
        relay?.CancelarConexion();
        TerminarPartida(ResultadoPartida.Cancelada, motivo);
    }
    private void TerminarPartida(ResultadoPartida resultado, string motivo)
    {
        estadoActual = EstadoJuego.FinDePartida;
        Time.timeScale = 1;
        pausado = false;
        FindFirstObjectByType<PauseManager>()?.Reanudar();
        feedback.MostrarInterfaz(true);
        FinalizarAnimaciones();
        bool victoria = (participanteLocal == 0 && resultado == ResultadoPartida.GanaJugador) ||
            (participanteLocal == 1 && resultado == ResultadoPartida.GanaRival);
        string titulo = resultado == ResultadoPartida.Cancelada ? "PARTIDA INTERRUMPIDA" :
            resultado == ResultadoPartida.Empate ? "EMPATE" : victoria ? "¡VICTORIA!" : "DERROTA";
        if (resultado == ResultadoPartida.GanaJugador) { controladorAnimacionesJugador?.CelebrarVictoria(); controladorAnimacionesEnemigo?.CaerDerrotado(); }
        if (resultado == ResultadoPartida.GanaRival) { controladorAnimacionesEnemigo?.CelebrarVictoria(); controladorAnimacionesJugador?.CaerDerrotado(); }
        string detalle = $"{motivo}\nHydros: {vidaJugador?.vidaActual ?? 0} PS  ·  Ignis: {vidaEnemigo?.vidaActual ?? 0} PS\nRondas: {turnoActual}/{maxTurnos}";
        feedback.MostrarFin(titulo, detalle, enLinea ? null : (Action)IniciarPartida, VolverAlMenu, SalirDelJuego, victoria, resultado == ResultadoPartida.Empate);
        OnFinPartida?.Invoke(victoria);
        OnResultadoPartida?.Invoke(resultado);
        Log($"FIN resultado={resultado} motivo={motivo}");
    }

    public void SalirDelJuego()
    {
        if (relay != null) {
            relay.OnConexionPerdida -= ConexionPerdida;
        }

        var pm = FindFirstObjectByType<PauseManager>();
        if (pm != null) pm.SalirJuego();
        else
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    private void ActualizarEstado(string mensaje)
    {
        string uno = planLocal.Count > 0 ? ReglasCombate.Resumen(planLocal[0]) : "—";
        string dos = planLocal.Count > 1 ? ReglasCombate.Resumen(planLocal[1]) : "—";
        feedback.ActualizarEstado(turnoActual, maxTurnos, NombreLocal + " · TÚ", mensaje, $"1. {uno}      2. {dos}");
        OnMensajeEstado?.Invoke(mensaje);
    }

    public void EstablecerPausa(bool pausa)
    {
        pausado = pausa;
        feedback?.MostrarInterfaz(!pausa);
        if (!pausa) InicioVentanaEntradaUtc = DateTime.UtcNow;
    }
    public void VolverAlMenu()
    {
        StopAllCoroutines();
        estadoActual = EstadoJuego.FinDePartida;
        FinalizarAnimaciones();
        Time.timeScale = 1;
        relay?.SalirAlMenu();
        SceneManager.LoadScene("Menus");
    }
    private void FinalizarAnimaciones()
    {
        controladorAnimacionesJugador?.FinalizarAccion();
        controladorAnimacionesEnemigo?.FinalizarAccion();
    }
    private void Log(string mensaje)
    {
        if (logsDetallados) Debug.Log($"[Combate {idPartida ?? "preparación"} | ronda {turnoActual}/{maxTurnos} | {estadoActual}] {mensaje}", this);
    }
    private void OnDestroy()
    {
        if (relay == null) return;
        relay.OnEstadoSala -= EstadoSala;
        relay.OnRivalListo -= IniciarComoHost;
        relay.OnMensaje -= RecibirMensaje;
        relay.OnConexionPerdida -= ConexionPerdida;
    }
}