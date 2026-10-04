using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[Serializable]
public class MensajeCombate
{
    public int version = 1;
    public string tipo;
    public string partida;
    public int ronda;
    public int maxRondas, danioBase, vidaMaxJugador, vidaMaxRival;
    public int vidaJugador, vidaRival;
    public float multiplicadorCritico, reduccionDefensa, segundosAtaque, segundosDefensa, segundosResultado;
    public AccionTurno[] jugador, rival;
}

/// <summary>
/// Relay conecta a dos participantes; los mensajes de combate se validan y resuelven en el host.
/// Cada equipo carga Escenario antes de conectarse. No se necesitan NetworkObjects para este combate por turnos.
/// </summary>
public class GestorRedRelay : MonoBehaviour
{
    private const string Canal = "Pokimun.Combate.v1";
    private const int MaxBytes = 16384;
    private NetworkManager red;
    private UnityTransport transporte;
    private int operacion;
    private bool ocupado, registrado, cerrando, rivalListo;
    private ulong? rivalId;
    private string partida;
    private float limiteConexion;
    private Task inicializacion;

    public event Action<string, bool> OnEstadoSala;
    public event Action OnRivalListo;
    public event Action<MensajeCombate> OnMensaje;
    public event Action<string> OnConexionPerdida;
    public bool EsHost => red != null && red.IsHost;
    public bool Conectado => red != null && red.IsListening;
    public string IdPartida => partida;

    private void Awake()
    {
        red = GetComponent<NetworkManager>();
        transporte = GetComponent<UnityTransport>();
        if (red == null || transporte == null) return;
        red.OnClientConnectedCallback += AlConectar;
        red.OnClientDisconnectCallback += AlDesconectar;
    }

    private async Task PrepararServicios()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private Task ServiciosListos()
    {
        if (inicializacion == null || inicializacion.IsFaulted || inicializacion.IsCanceled)
            inicializacion = PrepararServicios();
        return inicializacion;
    }

    public async Task<string> CrearPartidaHost()
    {
        if (!PuedeConectar()) return null;
        int intento = ++operacion;
        ocupado = true;
        OnEstadoSala?.Invoke("Creando sala…", true);
        try
        {
            await ServiciosListos();
            if (!Vigente(intento)) return null;
            Allocation asignacion = await RelayService.Instance.CreateAllocationAsync(1);
            if (!Vigente(intento)) return null;
            string codigo = await RelayService.Instance.GetJoinCodeAsync(asignacion.AllocationId);
            if (!Vigente(intento)) return null;
            PrepararRed();
            partida = Guid.NewGuid().ToString("N");
            transporte.SetRelayServerData(new RelayServerData(asignacion, "dtls"));
            if (!red.StartHost()) throw new InvalidOperationException("No se pudo iniciar la sala.");
            RegistrarCanal();
            ocupado = false;
            OnEstadoSala?.Invoke($"CÓDIGO: {codigo}\nCompártelo con el otro jugador. Esperando rival…", true);
            Debug.Log("[Red] Sala creada. Esperando a un participante compatible.", this);
            return codigo;
        }
        catch (Exception ex)
        {
            if (Vigente(intento)) ErrorConexion(ex.Message);
            return null;
        }
    }

    public async Task UnirseComoCliente(string codigoUnion)
    {
        if (!PuedeConectar()) return;
        if (string.IsNullOrWhiteSpace(codigoUnion))
        {
            OnEstadoSala?.Invoke("Escribe el código de la sala.", false);
            return;
        }
        int intento = ++operacion;
        ocupado = true;
        OnEstadoSala?.Invoke("Conectando con la sala…", true);
        try
        {
            await ServiciosListos();
            if (!Vigente(intento)) return;
            JoinAllocation asignacion = await RelayService.Instance.JoinAllocationAsync(codigoUnion.Trim().ToUpperInvariant());
            if (!Vigente(intento)) return;
            PrepararRed();
            transporte.SetRelayServerData(new RelayServerData(asignacion, "dtls"));
            if (!red.StartClient()) throw new InvalidOperationException("No se pudo iniciar la conexión.");
            RegistrarCanal();
            limiteConexion = Time.realtimeSinceStartup + 30f;
        }
        catch (Exception ex) { if (Vigente(intento)) ErrorConexion(ex.Message); }
    }

    private bool PuedeConectar()
    {
        if (red == null || transporte == null)
        {
            OnEstadoSala?.Invoke("Falta configurar NetworkManager o UnityTransport en la escena.", false);
            return false;
        }
        return !ocupado && !red.IsListening && !red.ShutdownInProgress;
    }

    private bool Vigente(int intento) => this != null && intento == operacion;

    private void PrepararRed()
    {
        cerrando = false;
        rivalListo = false;
        rivalId = null;
        partida = null;
        // Ambos jugadores ya están en la misma escena. La carga de escena no depende de Netcode.
        red.NetworkConfig.EnableSceneManagement = false;
    }

    private void RegistrarCanal()
    {
        if (registrado || red.CustomMessagingManager == null) return;
        red.CustomMessagingManager.RegisterNamedMessageHandler(Canal, Recibir);
        registrado = true;
    }

    private void AlConectar(ulong cliente)
    {
        if (cerrando) return;
        RegistrarCanal();
        if (red.IsHost)
        {
            if (cliente == red.LocalClientId) return;
            if (rivalId.HasValue && rivalId.Value != cliente) { red.DisconnectClient(cliente); return; }
            rivalId = cliente;
            limiteConexion = Time.realtimeSinceStartup + 30f;
        }
        else if (cliente == red.LocalClientId)
        {
            rivalId = NetworkManager.ServerClientId;
            Enviar(new MensajeCombate { tipo = "listo" });
        }
    }

    public bool Enviar(MensajeCombate mensaje)
    {
        if (red == null || !red.IsListening || !rivalId.HasValue) return false;
        mensaje.partida = partida;
        string json = JsonUtility.ToJson(mensaje);
        int longitud = FastBufferWriter.GetWriteSize(json);
        if (longitud > MaxBytes) return false;
        using (var writer = new FastBufferWriter(longitud, Allocator.Temp))
        {
            writer.WriteValueSafe(json);
            red.CustomMessagingManager.SendNamedMessage(Canal, rivalId.Value, writer, NetworkDelivery.ReliableSequenced);
        }
        return true;
    }

    private void Recibir(ulong remitente, FastBufferReader reader)
    {
        if (cerrando || !rivalId.HasValue || remitente != rivalId.Value || reader.Length > MaxBytes) return;
        try
        {
            reader.ReadValueSafe(out string json);
            var mensaje = JsonUtility.FromJson<MensajeCombate>(json);
            if (mensaje == null || mensaje.version != 1) { ErrorConexion("La otra copia del juego usa una versión de combate diferente."); return; }
            if (EsHost && mensaje.tipo == "listo")
            {
                if (rivalListo) return;
                rivalListo = true;
                limiteConexion = 0;
                OnRivalListo?.Invoke();
                return;
            }
            if (!EsHost && mensaje.tipo == "inicio" && !rivalListo && !string.IsNullOrEmpty(mensaje.partida))
            {
                partida = mensaje.partida;
                rivalListo = true;
                ocupado = false;
                limiteConexion = 0;
            }
            if (!rivalListo || mensaje.partida != partida) return;
            OnMensaje?.Invoke(mensaje);
        }
        catch (Exception ex) { ErrorConexion("Mensaje de combate inválido: " + ex.Message); }
    }

    private void Update()
    {
        if (limiteConexion > 0 && Time.realtimeSinceStartup >= limiteConexion)
            ErrorConexion("La conexión no se completó. Comprueba el código y la conexión a Internet.");
    }

    private void AlDesconectar(ulong cliente)
    {
        if (cerrando || (EsHost && (!rivalId.HasValue || cliente != rivalId.Value))) return;
        ErrorConexion("El otro jugador se ha desconectado.");
    }

    private void ErrorConexion(string mensaje)
    {
        Debug.LogWarning("[Red] " + mensaje, this);
        CancelarConexion();
        OnEstadoSala?.Invoke(mensaje, false);
        OnConexionPerdida?.Invoke(mensaje);
    }

    public void CancelarConexion()
    {
        ++operacion; // Invalida cualquier respuesta asíncrona pendiente.
        cerrando = true;
        ocupado = rivalListo = false;
        limiteConexion = 0;
        rivalId = null;
        partida = null;
        if (red != null)
        {
            if (registrado && red.CustomMessagingManager != null)
                red.CustomMessagingManager.UnregisterNamedMessageHandler(Canal);
            registrado = false;
            if (red.IsListening) red.Shutdown();
        }
    }

    public void SalirAlMenu()
    {
        CancelarConexion();
        // NetworkManager se conserva entre escenas al conectarse: se elimina al abandonar la partida.
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        CancelarConexion();
        if (red == null) return;
        red.OnClientConnectedCallback -= AlConectar;
        red.OnClientDisconnectCallback -= AlDesconectar;
    }
}
