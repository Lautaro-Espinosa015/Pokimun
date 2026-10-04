using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EstadoJuego { EsperandoInicio, TurnoJugador, TurnoRival, ResolviendoAccion, FinDePartida }

/// <summary>
/// Único punto de entrada al combate. Congela los dos planes, resuelve cada pareja una vez
/// y coordina su presentación. Hydros es el participante 0; Ignis es el 1 (CPU o cliente).
/// </summary>
public class GestorNivel : MonoBehaviour
{
    [Header("Configuración de Turnos")]
    [SerializeField, Min(1)] private int maxTurnos = 5;
    [SerializeField] private int turnoActual = 1;
    [Header("Parámetros de Combate")]
    [SerializeField, Min(0)] private int danioBase = 25;
    [SerializeField, Range(0, 1)] private float factorReduccionDefensa = .5f;
    [SerializeField, Min(0)] private float tiempoPausaRival = 1.2f;
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
    [Header("Presentación de Acciones")]
    [SerializeField, Min(4)] private float tiempoFeedbackAtaque = 4f;
    [SerializeField, Min(.5f)] private float tiempoFeedbackDefensa = 1.5f;
    [SerializeField, Min(1)] private float tiempoResultado = 2.5f;
    [Header("Supervisión")]
    [SerializeField] private bool logsDetallados = true;
    [SerializeField] private EstadoJuego estadoActual = EstadoJuego.EsperandoInicio;

    public Action<int, int> OnTurnoActualizado;
    public Action<string> OnMensajeEstado;
    // Compatibilidad: false incluye derrota/empate/cancelación. Para distinguirlos usar OnResultadoPartida.
    public Action<bool> OnFinPartida;
    public Action<ResultadoPartida> OnResultadoPartida;

    private FeedbackAtaqueUI feedback;
    private GestorRedRelay relay;
    private readonly List<AccionTurno> planLocal = new List<AccionTurno>(2);
    private AccionTurno[] planRemoto;
    private ResultadoRonda resolucion;
    private bool enLinea, pausado, presentacionLocalLista, presentacionRemotaLista;
    private int participanteLocal;
    private float inicioEsperaPresentacion;
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
        // Las animaciones se buscan solamente en el dueño de cada Vida, nunca en un personaje arbitrario.
        if (controladorAnimacionesJugador == null && vidaJugador != null)
            controladorAnimacionesJugador = vidaJugador.GetComponent<ControladorAnimaciones>();
        if (controladorAnimacionesEnemigo == null && vidaEnemigo != null)
            controladorAnimacionesEnemigo = vidaEnemigo.GetComponent<ControladorAnimaciones>();
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
            feedback.MostrarFin("FALTA CONFIGURACIÓN", "Cada personaje necesita su Vida y su propia barra de PS.", null, VolverAlMenu);
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

    private void CrearSala()
    {
        if (relay == null) { EstadoSala("La escena no tiene GestorRedRelay.", false); return; }
        _ = relay.CrearPartidaHost();
    }
    private void UnirseSala(string codigo)
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
        feedback.Limpiar();
        feedback.OcultarSala();
        Log($"INICIO modo={(online ? "multijugador" : "solo")} local={NombreLocal} rondas={maxTurnos} " +
            $"PS=Hydros:{vidaJugador.vidaActual}/{vidaJugador.maxVida},Ignis:{vidaEnemigo.vidaActual}/{vidaEnemigo.maxVida} " +
            $"dañoBase={danioBase} crítico=x{multiplicadorDanioGrito} reducciónEscudo={factorReduccionDefensa:P0}");
    }

    private string NombreLocal => participanteLocal == 0 ? "HYDROS" : "IGNIS";

    private void AbrirRonda(int ronda)
    {
        turnoActual = ronda;
        planLocal.Clear();
        planRemoto = enLinea ? null : ElegirPlanCPU();
        resolucion = null;
        presentacionLocalLista = presentacionRemotaLista = false;
        inicioEsperaPresentacion = 0;
        estadoActual = EstadoJuego.TurnoJugador;
        InicioVentanaEntradaUtc = DateTime.UtcNow;
        OnTurnoActualizado?.Invoke(turnoActual, maxTurnos);
        ActualizarEstado("Elige dos acciones: dos ataques o un ataque y un escudo.");
        Log("RONDA ABIERTA · entrada habilitada · acciones=0/2");
    }

    private AccionTurno[] ElegirPlanCPU()
    {
        var plan = new AccionTurno[2];
        int defensa = UnityEngine.Random.value < probabilidadDefensaCPU ? UnityEngine.Random.Range(0, 2) : -1;
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
        if (!PuedeRecibirAcciones)
        {
            Log($"ENTRADA RECHAZADA origen={origen} estado={estadoActual} pausa={pausado} acciones={planLocal.Count}/2");
            return false;
        }
        if (!ReglasCombate.PuedeAgregar(planLocal, accion, out string motivo))
        {
            ActualizarEstado(motivo);
            Log($"ENTRADA RECHAZADA origen={origen} motivo={motivo}");
            return false;
        }
        planLocal.Add(accion); // Copia por valor: un gesto/voz posterior no altera esta elección.
        Log($"ACCIÓN ACEPTADA actor={NombreLocal} posición={planLocal.Count}/2 tipo={accion.tipo} gesto={accion.jugada} crítico={accion.critico} origen={origen}");
        if (planLocal.Count < 2)
        {
            ActualizarEstado(accion.tipo == AccionCombate.Defender ? "Escudo elegido. Falta un ataque." : "Acción elegida. Falta un ataque o un escudo.");
            return true;
        }
        estadoActual = EstadoJuego.TurnoRival; // Bloquear antes de enviar o iniciar una corrutina.
        ActualizarEstado(enLinea ? "Plan cerrado. Esperando al rival…" : "Plan cerrado. Comienza el intercambio.");
        if (enLinea && !relay.EsHost)
        {
            if (!relay.Enviar(new MensajeCombate { tipo = "plan", ronda = turnoActual, rival = planLocal.ToArray() }))
                CancelarPartida("No se pudo enviar tu plan al rival.");
        }
        else IntentarResolver();
        return true;
    }

    private void IntentarResolver()
    {
        if (estadoActual != EstadoJuego.TurnoRival || planLocal.Count != 2 || !ReglasCombate.PlanValido(planRemoto)) return;
        var mensaje = new MensajeCombate {
            tipo = "resolver", ronda = turnoActual, jugador = planLocal.ToArray(), rival = (AccionTurno[])planRemoto.Clone(),
            vidaJugador = vidaJugador.vidaActual, vidaRival = vidaEnemigo.vidaActual
        };
        if (enLinea && !relay.Enviar(mensaje)) { CancelarPartida("Se perdió la conexión antes de resolver la ronda."); return; }
        ResolverYPresentar(mensaje);
    }

    private void ResolverYPresentar(MensajeCombate mensaje)
    {
        resolucion = ReglasCombate.Resolver(turnoActual, maxTurnos, mensaje.jugador, mensaje.rival,
            vidaJugador.vidaActual, vidaEnemigo.vidaActual, danioBase, multiplicadorDanioGrito, factorReduccionDefensa);
        estadoActual = EstadoJuego.ResolviendoAccion;
        ActualizarEstado("Acciones cerradas · observa el intercambio.");
        Log($"PLANES CERRADOS Hydros=[{ReglasCombate.Resumen(mensaje.jugador[0])}, {ReglasCombate.Resumen(mensaje.jugador[1])}] " +
            $"Ignis=[{ReglasCombate.Resumen(mensaje.rival[0])}, {ReglasCombate.Resumen(mensaje.rival[1])}]");
        StartCoroutine(PresentarRonda());
    }

    private IEnumerator PresentarRonda()
    {
        foreach (ResultadoIntercambio r in resolucion.intercambios)
        {
            ActualizarEstado($"Intercambio {r.pareja}/2 · acciones bloqueadas.");
            yield return PresentarAccion(r.jugador, controladorAnimacionesJugador, "HYDROS", r.pareja, participanteLocal != 0);
            if (tiempoPausaRival > 0) yield return new WaitForSeconds(tiempoPausaRival);
            yield return PresentarAccion(r.rival, controladorAnimacionesEnemigo, "IGNIS", r.pareja, participanteLocal != 1);
            // El resultado se aplica una sola vez, después de ambas presentaciones.
            vidaJugador.EstablecerVida(r.vidaJugadorDespues);
            vidaEnemigo.EstablecerVida(r.vidaRivalDespues);
            if (r.danioAplicado > 0)
            {
                if (r.ganador == 1) controladorAnimacionesEnemigo?.RecibirDano();
                else controladorAnimacionesJugador?.RecibirDano();
            }
            feedback.MostrarResultado(r, "HYDROS", "IGNIS", tiempoResultado);
            Log($"RESULTADO pareja={r.pareja} ganador={(r.ganador == 1 ? "Hydros" : r.ganador == -1 ? "Ignis" : "empate")} " +
                $"crítico={r.critico} escudo={r.bloqueo} base={danioBase} previoEscudo={r.danioSinDefensa} " +
                $"calculado={r.danioCalculado} aplicado={r.danioAplicado} exceso={r.danioCalculado - r.danioAplicado} " +
                $"Hydros={r.vidaJugadorAntes}→{r.vidaJugadorDespues} Ignis={r.vidaRivalAntes}→{r.vidaRivalDespues}");
            yield return new WaitForSeconds(tiempoResultado);
            FinalizarAnimaciones();
        }
        presentacionLocalLista = true;
        if (resolucion.resultado != ResultadoPartida.EnCurso)
        {
            TerminarPartida(resolucion.resultado, resolucion.motivo);
            yield break;
        }
        if (!enLinea) { AbrirRonda(turnoActual + 1); yield break; }
        inicioEsperaPresentacion = Time.realtimeSinceStartup;
        ActualizarEstado("Esperando que el rival termine de ver el intercambio…");
        if (relay.EsHost) IntentarSiguienteRonda();
        else if (!relay.Enviar(new MensajeCombate { tipo = "presentado", ronda = turnoActual,
            vidaJugador = vidaJugador.vidaActual, vidaRival = vidaEnemigo.vidaActual }))
            CancelarPartida("Se perdió la conexión al finalizar la ronda.");
    }

    private IEnumerator PresentarAccion(AccionTurno accion, ControladorAnimaciones animador, string nombre, int numero, bool rival)
    {
        float duracion = accion.tipo == AccionCombate.Atacar ? tiempoFeedbackAtaque : tiempoFeedbackDefensa;
        feedback.MostrarAccion(accion, nombre, numero, duracion, rival);
        if (accion.tipo == AccionCombate.Defender) animador?.EjecutarDefensa(true);
        else if (accion.critico) animador?.EjecutarAtaqueCritico(accion.jugada);
        else animador?.EjecutarAtaque(accion.jugada);
        yield return new WaitForSeconds(duracion);
        if (accion.tipo == AccionCombate.Atacar) animador?.FinalizarAccion();
    }

    private void IniciarComoHost()
    {
        if (estadoActual != EstadoJuego.EsperandoInicio || !ReferenciasValidas()) return;
        PrepararPartida(true, 0);
        var inicio = new MensajeCombate {
            tipo = "inicio", ronda = 1, maxRondas = maxTurnos, danioBase = danioBase,
            vidaMaxJugador = vidaJugador.maxVida, vidaMaxRival = vidaEnemigo.maxVida,
            multiplicadorCritico = multiplicadorDanioGrito, reduccionDefensa = factorReduccionDefensa,
            segundosAtaque = tiempoFeedbackAtaque, segundosDefensa = tiempoFeedbackDefensa, segundosResultado = tiempoResultado
        };
        if (!relay.Enviar(inicio)) { CancelarPartida("No se pudo iniciar la partida con el rival."); return; }
        AbrirRonda(1);
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
        if (m.tipo == "ronda" && !relay.EsHost && presentacionLocalLista && m.ronda == turnoActual + 1 && m.ronda <= maxTurnos)
        {
            if (!VidaCoincide(m)) { CancelarPartida("Los puntos de vida no coinciden entre los equipos."); return; }
            AbrirRonda(m.ronda);
            return;
        }
        if (m.ronda != turnoActual) { Log($"MENSAJE IGNORADO tipo={m.tipo} rondaRecibida={m.ronda}"); return; }
        if (relay.EsHost && m.tipo == "plan" && planRemoto == null &&
            (estadoActual == EstadoJuego.TurnoJugador || estadoActual == EstadoJuego.TurnoRival))
        {
            if (!ReglasCombate.PlanValido(m.rival)) { CancelarPartida("El rival envió un plan de acciones inválido."); return; }
            planRemoto = (AccionTurno[])m.rival.Clone();
            Log("PLAN RIVAL RECIBIDO · dos acciones válidas");
            IntentarResolver();
        }
        else if (!relay.EsHost && m.tipo == "resolver" && estadoActual == EstadoJuego.TurnoRival)
        {
            if (!ReglasCombate.PlanValido(m.jugador) || !ReglasCombate.PlanValido(m.rival) || !PlanCoincide(m.rival) || !VidaCoincide(m))
            { CancelarPartida("La ronda recibida no coincide con las acciones o la vida de esta partida."); return; }
            ResolverYPresentar(m);
        }
        else if (relay.EsHost && m.tipo == "presentado" && estadoActual == EstadoJuego.ResolviendoAccion && resolucion != null)
        {
            var ultimo = resolucion.intercambios[resolucion.intercambios.Length - 1];
            if (m.vidaJugador != ultimo.vidaJugadorDespues || m.vidaRival != ultimo.vidaRivalDespues)
            { CancelarPartida("Los equipos resolvieron una vida diferente."); return; }
            presentacionRemotaLista = true;
            IntentarSiguienteRonda();
        }
    }

    private static bool ConfiguracionValida(MensajeCombate m) => m.ronda == 1 && m.maxRondas > 0 && m.maxRondas <= 1000 &&
        m.danioBase >= 0 && m.danioBase <= 100000 && m.vidaMaxJugador > 0 && m.vidaMaxRival > 0 &&
        m.multiplicadorCritico >= 1 && m.multiplicadorCritico <= 100 && m.reduccionDefensa >= 0 && m.reduccionDefensa <= 1 &&
        m.segundosAtaque >= 4 && m.segundosAtaque <= 30 && m.segundosDefensa >= .5f && m.segundosDefensa <= 30 &&
        m.segundosResultado >= 1 && m.segundosResultado <= 30;
    private bool VidaCoincide(MensajeCombate m) => m.vidaJugador == vidaJugador.vidaActual && m.vidaRival == vidaEnemigo.vidaActual;
    private bool PlanCoincide(AccionTurno[] plan)
    {
        if (planLocal.Count != 2) return false;
        for (int i = 0; i < 2; i++)
            if (plan[i].tipo != planLocal[i].tipo || plan[i].jugada != planLocal[i].jugada || plan[i].critico != planLocal[i].critico) return false;
        return true;
    }

    private void IntentarSiguienteRonda()
    {
        if (!presentacionLocalLista || !presentacionRemotaLista || estadoActual != EstadoJuego.ResolviendoAccion) return;
        if (!relay.Enviar(new MensajeCombate { tipo = "ronda", ronda = turnoActual + 1,
            vidaJugador = vidaJugador.vidaActual, vidaRival = vidaEnemigo.vidaActual }))
        { CancelarPartida("Se perdió la conexión antes de la siguiente ronda."); return; }
        AbrirRonda(turnoActual + 1);
    }

    private void Update()
    {
        if (enLinea && inicioEsperaPresentacion > 0 && Time.realtimeSinceStartup - inicioEsperaPresentacion > 120)
            CancelarPartida("El otro equipo no confirmó el final de la ronda.");
    }

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
        inicioEsperaPresentacion = 0;
        FinalizarAnimaciones();
        bool victoria = (participanteLocal == 0 && resultado == ResultadoPartida.GanaJugador) ||
            (participanteLocal == 1 && resultado == ResultadoPartida.GanaRival);
        string titulo = resultado == ResultadoPartida.Cancelada ? "PARTIDA INTERRUMPIDA" :
            resultado == ResultadoPartida.Empate ? "EMPATE" : victoria ? "¡VICTORIA!" : "DERROTA";
        if (resultado == ResultadoPartida.GanaJugador) { controladorAnimacionesJugador?.CelebrarVictoria(); controladorAnimacionesEnemigo?.CaerDerrotado(); }
        if (resultado == ResultadoPartida.GanaRival) { controladorAnimacionesEnemigo?.CelebrarVictoria(); controladorAnimacionesJugador?.CaerDerrotado(); }
        string detalle = $"{motivo}\nHydros: {vidaJugador?.vidaActual ?? 0} PS  ·  Ignis: {vidaEnemigo?.vidaActual ?? 0} PS\nRondas: {turnoActual}/{maxTurnos}";
        feedback.MostrarFin(titulo, detalle, enLinea ? null : (Action)IniciarPartida, VolverAlMenu);
        OnFinPartida?.Invoke(victoria);
        OnResultadoPartida?.Invoke(resultado);
        Log($"FIN resultado={resultado} motivo={motivo} PS=Hydros:{vidaJugador?.vidaActual},Ignis:{vidaEnemigo?.vidaActual}");
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
