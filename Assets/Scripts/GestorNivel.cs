using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Estados posibles durante el flujo de la partida por turnos.
/// </summary>
public enum EstadoJuego
{
    EsperandoInicio,
    TurnoJugador,
    TurnoRival,
    ResolviendoAccion,
    FinDePartida
}

/// <summary>
/// Acciones principales que puede tomar cualquier participante en su turno.
/// </summary>
public enum AccionCombate
{
    Atacar,
    Defender
}

/// <summary>
/// Gestor principal del nivel para el juego por turnos.
/// Administra el flujo de turnos, el límite de 5 rondas, la resolución de combate con Piedra/Papel/Tijeras,
/// y la condición de victoria/derrota según la vida del rival o del jugador.
/// </summary>
public class GestorNivel : MonoBehaviour
{
    [Header("Configuración de Turnos")]
    [Tooltip("Número máximo de turnos o rondas permitidas antes de finalizar la partida.")]
    [SerializeField] private int maxTurnos = 5;
    [SerializeField] private int turnoActual = 1;

    [Header("Parámetros de Combate")]
    [Tooltip("Cantidad base de daño infligido al acertar un ataque.")]
    [SerializeField] private int danioBase = 25;
    [Tooltip("Porcentaje de reducción de daño cuando el objetivo está en postura defensiva (0.5 = 50%).")]
    [Range(0f, 1f)]
    [SerializeField] private float factorReduccionDefensa = 0.5f;
    [Tooltip("Tiempo de espera en segundos para simular el turno y respuesta de la CPU.")]
    [SerializeField] private float tiempoPausaRival = 1.2f;

    [Header("Referencias de Vida")]
    [SerializeField] private Vida vidaJugador;
    [SerializeField] private Vida vidaEnemigo;

    [Header("Módulo de Ataque del Jugador")]
    [Tooltip("Script externo que determina la jugada del jugador. Puede ser implementado por otro compañero.")]
    [SerializeField] private ControladorAtaqueJugador controladorAtaqueJugador;

    [Header("Estado de la Partida")]
    [SerializeField] private EstadoJuego estadoActual = EstadoJuego.EsperandoInicio;
    [SerializeField] private bool jugadorDefendiendo = false;
    [SerializeField] private bool rivalDefendiendo = false;

    // Eventos opcionales para conectar con la UI u otros sistemas
    public System.Action<int, int> OnTurnoActualizado; // (turnoActual, maxTurnos)
    public System.Action<string> OnMensajeEstado;     // Notificación de texto para la pantalla
    public System.Action<bool> OnFinPartida;          // true = Victoria, false = Derrota

    private void Start()
    {
        // Si no se asignó en el Inspector, intentamos encontrarlo en el mismo GameObject
        if (controladorAtaqueJugador == null)
        {
            controladorAtaqueJugador = GetComponent<ControladorAtaqueJugador>();
            if (controladorAtaqueJugador == null)
            {
                // Agregamos un componente por defecto para que funcione de forma autónoma
                controladorAtaqueJugador = gameObject.AddComponent<ControladorAtaqueJugador>();
            }
        }

        IniciarPartida();
    }

    private void Update()
    {
        // Controles de prueba rápida para la Demo mediante teclado
        ManejarInputsDebug();
    }

    /// <summary>
    /// Configura e inicia la partida en el turno 1.
    /// </summary>
    public void IniciarPartida()
    {
        turnoActual = 1;
        jugadorDefendiendo = false;
        rivalDefendiendo = false;
        estadoActual = EstadoJuego.TurnoJugador;

        NotificarMensaje($"¡Comienza la partida! Turno {turnoActual}/{maxTurnos}. Es tu turno.");
        OnTurnoActualizado?.Invoke(turnoActual, maxTurnos);
    }

    #region Turno del Jugador

    /// <summary>
    /// Ejecuta la acción de Ataque del jugador.
    /// Llama al método del script modular ControladorAtaqueJugador para obtener su jugada (Piedra, Papel o Tijera).
    /// Puede ser llamado desde botones de la UI (OnClick).
    /// </summary>
    public void JugadorSeleccionarAtaque()
    {
        if (estadoActual != EstadoJuego.TurnoJugador) return;

        estadoActual = EstadoJuego.ResolviendoAccion;
        jugadorDefendiendo = false;

        // Invocamos el método del script modular para obtener la jugada
        JugadaRPS jugadaJugador = ObtenerJugadaAtaqueJugador();

        // La CPU escoge una jugada para competir en Piedra, Papel o Tijeras
        JugadaRPS jugadaRival = ObtenerJugadaAleatoriaCPU();

        NotificarMensaje($"Tú elegiste [{jugadaJugador}] vs CPU eligió [{jugadaRival}].");

        // Comparación de Piedra, Papel o Tijeras
        int resultado = CompararRPS(jugadaJugador, jugadaRival);

        if (resultado > 0)
        {
            // Jugador gana el RPS -> Daño al enemigo
            int danio = CalcularDanio(danioBase, rivalDefendiendo);
            NotificarMensaje($"¡Ganaste el choque! Infliges {danio} de daño al rival.");
            if (vidaEnemigo != null)
            {
                vidaEnemigo.RecibirDanio(danio);
            }
        }
        else if (resultado < 0)
        {
            // Rival gana el choque -> El ataque falla o es bloqueado
            NotificarMensaje("El rival predijo tu jugada. ¡Tu ataque falló!");
        }
        else
        {
            // Empate en RPS
            NotificarMensaje("¡Empate en la jugada! Ambos ataques chocan sin dañarse.");
        }

        // Comprobamos si el enemigo cayó derrotado
        if (VerificarFinDePartida()) return;

        // Pasamos al turno del rival
        StartCoroutine(RutinaTurnoRival());
    }

    /// <summary>
    /// Ejecuta la acción de Defensa del jugador para reducir el daño en el turno del rival.
    /// Puede ser llamado desde botones de la UI (OnClick).
    /// </summary>
    public void JugadorSeleccionarDefensa()
    {
        if (estadoActual != EstadoJuego.TurnoJugador) return;

        estadoActual = EstadoJuego.ResolviendoAccion;
        jugadorDefendiendo = true;

        NotificarMensaje("Te has puesto en guardia para defenderte del próximo ataque.");

        // Pasamos al turno del rival
        StartCoroutine(RutinaTurnoRival());
    }

    /// <summary>
    /// Obtiene la jugada del jugador a través del script modular externo.
    /// </summary>
    public JugadaRPS ObtenerJugadaAtaqueJugador()
    {
        if (controladorAtaqueJugador != null)
        {
            return controladorAtaqueJugador.ObtenerJugadaAtaque();
        }

        Debug.LogWarning("[GestorNivel] No hay ControladorAtaqueJugador asignado. Usando Piedra por defecto.");
        return JugadaRPS.Piedra;
    }

    #endregion

    #region Turno del Rival (CPU)

    /// <summary>
    /// Corutina que simula la toma de decisiones y acción del rival con una pequeña pausa natural.
    /// </summary>
    private IEnumerator RutinaTurnoRival()
    {
        estadoActual = EstadoJuego.TurnoRival;
        yield return new WaitForSeconds(tiempoPausaRival);

        // La CPU decide aleatoriamente entre Atacar (70% prob) o Defenderse (30% prob)
        AccionCombate accionRival = (Random.value > 0.3f) ? AccionCombate.Atacar : AccionCombate.Defender;

        if (accionRival == AccionCombate.Defender)
        {
            rivalDefendiendo = true;
            NotificarMensaje("El rival ha tomado una postura defensiva.");
        }
        else
        {
            rivalDefendiendo = false;
            JugadaRPS ataqueRival = ObtenerJugadaAleatoriaCPU();
            NotificarMensaje($"El rival lanza un ataque con [{ataqueRival}].");

            // Si el jugador decidió defenderse en su turno, absorbe gran parte del daño
            if (jugadorDefendiendo)
            {
                int danioMitigado = CalcularDanio(danioBase, true);
                NotificarMensaje($"¡Tu defensa amortiguó el golpe! Solo recibes {danioMitigado} de daño.");
                if (vidaJugador != null)
                {
                    vidaJugador.RecibirDanio(danioMitigado);
                }
            }
            else
            {
                // Si el jugador no defendió, se resuelve un enfrentamiento RPS con su jugada preparada
                JugadaRPS jugadaContramedida = ObtenerJugadaAtaqueJugador();
                int resultado = CompararRPS(ataqueRival, jugadaContramedida);

                if (resultado > 0)
                {
                    int danioCompleto = danioBase;
                    NotificarMensaje($"El ataque del rival te golpeó de lleno. Recibes {danioCompleto} de daño.");
                    if (vidaJugador != null)
                    {
                        vidaJugador.RecibirDanio(danioCompleto);
                    }
                }
                else if (resultado < 0)
                {
                    NotificarMensaje("¡Contraatacaste a tiempo con tu jugada y esquivaste el ataque rival!");
                }
                else
                {
                    NotificarMensaje("¡Choque simultáneo! Los ataques de ambos se neutralizan.");
                }
            }
        }

        // Comprobamos si el jugador fue derrotado
        if (VerificarFinDePartida()) yield break;

        // Avanzamos de ronda / turno
        AvanzarRonda();
    }

    #endregion

    #region Lógica de Rondas y Fin de Partida

    /// <summary>
    /// Incrementa el contador de turnos y reinicia estados temporales.
    /// </summary>
    private void AvanzarRonda()
    {
        turnoActual++;
        jugadorDefendiendo = false; // Se resetea la guardia para la nueva ronda

        if (turnoActual > maxTurnos)
        {
            DeterminarGanadorPorTurnos();
            return;
        }

        estadoActual = EstadoJuego.TurnoJugador;
        OnTurnoActualizado?.Invoke(turnoActual, maxTurnos);
        NotificarMensaje($"--- Turno {turnoActual}/{maxTurnos} --- ¡Es tu turno de actuar!");
    }

    /// <summary>
    /// Comprueba si se ha alcanzado la condición de victoria por vida a 0.
    /// </summary>
    /// <returns>True si la partida concluyó, False en caso contrario.</returns>
    private bool VerificarFinDePartida()
    {
        if (vidaEnemigo != null && vidaEnemigo.vidaActual <= 0)
        {
            FinalizarPartida(victoriaJugador: true, "¡Victoria! Has dejado al enemigo con 0 de vida.");
            return true;
        }

        if (vidaJugador != null && vidaJugador.vidaActual <= 0)
        {
            FinalizarPartida(victoriaJugador: false, "¡Derrota! Tu vida ha llegado a 0.");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determina quién gana si se alcanza el límite máximo de turnos sin que nadie llegue a 0 de vida.
    /// </summary>
    private void DeterminarGanadorPorTurnos()
    {
        int hpJugador = vidaJugador != null ? vidaJugador.vidaActual : 0;
        int hpEnemigo = vidaEnemigo != null ? vidaEnemigo.vidaActual : 0;

        if (hpJugador > hpEnemigo)
        {
            FinalizarPartida(victoriaJugador: true, $"¡Límite de {maxTurnos} turnos alcanzado! Ganas por mayor cantidad de vida ({hpJugador} vs {hpEnemigo}).");
        }
        else if (hpEnemigo > hpJugador)
        {
            FinalizarPartida(victoriaJugador: false, $"¡Límite de {maxTurnos} turnos alcanzado! El rival gana por vida restante ({hpEnemigo} vs {hpJugador}).");
        }
        else
        {
            FinalizarPartida(victoriaJugador: false, $"¡Límite de {maxTurnos} turnos alcanzado! Empate total ({hpJugador} vs {hpEnemigo}).");
        }
    }

    /// <summary>
    /// Cierra el ciclo de juego y dispara eventos correspondientes.
    /// </summary>
    private void FinalizarPartida(bool victoriaJugador, string motivo)
    {
        estadoActual = EstadoJuego.FinDePartida;
        NotificarMensaje($"[FIN DE PARTIDA] {motivo}");
        OnFinPartida?.Invoke(victoriaJugador);
    }

    #endregion

    #region Utilidades y Piedra Papel Tijeras

    /// <summary>
    /// Compara dos jugadas de Piedra, Papel o Tijera.
    /// </summary>
    /// <returns>1 si gana j1, -1 si gana j2, 0 si hay empate.</returns>
    private int CompararRPS(JugadaRPS j1, JugadaRPS j2)
    {
        if (j1 == j2) return 0;

        if ((j1 == JugadaRPS.Piedra && j2 == JugadaRPS.Tijera) ||
            (j1 == JugadaRPS.Papel && j2 == JugadaRPS.Piedra) ||
            (j1 == JugadaRPS.Tijera && j2 == JugadaRPS.Papel))
        {
            return 1;
        }

        return -1;
    }

    /// <summary>
    /// Calcula el daño a aplicar considerando si el receptor está defendiendo.
    /// </summary>
    private int CalcularDanio(int danio, bool estaDefendiendo)
    {
        if (estaDefendiendo)
        {
            return Mathf.RoundToInt(danio * (1f - factorReduccionDefensa));
        }
        return danio;
    }

    /// <summary>
    /// Genera una jugada aleatoria para la CPU.
    /// </summary>
    private JugadaRPS ObtenerJugadaAleatoriaCPU()
    {
        return (JugadaRPS)Random.Range(0, 3);
    }

    private void NotificarMensaje(string mensaje)
    {
        Debug.Log($"[GestorNivel] {mensaje}");
        OnMensajeEstado?.Invoke(mensaje);
    }

    /// <summary>
    /// Permite probar todo el flujo en el editor presionando teclas:
    /// [A] Atacar
    /// [D] Defender
    /// [1, 2, 3] Cambiar jugada preparada (Piedra, Papel, Tijera)
    /// </summary>
    private void ManejarInputsDebug()
    {
        // Soporte tanto para nuevo Input System como para el Legacy Input Manager
        bool presionoA = false;
        bool presionoD = false;
        bool presiono1 = false;
        bool presiono2 = false;
        bool presiono3 = false;

        if (Keyboard.current != null)
        {
            presionoA = Keyboard.current.aKey.wasPressedThisFrame;
            presionoD = Keyboard.current.dKey.wasPressedThisFrame;
            presiono1 = Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
            presiono2 = Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
            presiono3 = Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
        }
        else
        {
            presionoA = Input.GetKeyDown(KeyCode.A);
            presionoD = Input.GetKeyDown(KeyCode.D);
            presiono1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            presiono2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            presiono3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
        }

        if (presionoA)
        {
            JugadorSeleccionarAtaque();
        }
        else if (presionoD)
        {
            JugadorSeleccionarDefensa();
        }

        if (controladorAtaqueJugador != null)
        {
            if (presiono1) controladorAtaqueJugador.EstablecerJugada(JugadaRPS.Piedra);
            if (presiono2) controladorAtaqueJugador.EstablecerJugada(JugadaRPS.Papel);
            if (presiono3) controladorAtaqueJugador.EstablecerJugada(JugadaRPS.Tijera);
        }
    }

    #endregion
}
