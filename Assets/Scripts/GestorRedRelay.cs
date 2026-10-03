using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class GestorRedRelay : MonoBehaviour
{
    private string codigoGenerado = "";
    private string codigoAIntroducir = "";

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[GestorRedRelay] Listo para conexión. Mi Player ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception excepcion)
        {
            Debug.LogError($"[GestorRedRelay] Error de Auth: {excepcion.Message}");
        }

        // Suscribirse al evento de cuando un jugador se conecta a nuestra partida
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += AlConectarseUnCliente;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarseUnCliente;
        }
    }

    // Evento que se dispara en el Host y en el Cliente cuando alguien se conecta
    private void AlConectarseUnCliente(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"[HOST] Has abierto la sala exitosamente. Esperando al rival...");
            }
            else
            {
                // El HOST ve este mensaje cuando su laptop (el cliente) se une
                Debug.Log($"[HOST] ¡El rival (Cliente ID: {clientId}) se ha unido a tu partida!");
            }
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            // El CLIENTE ve este mensaje cuando logra entrar a la sala del Host
            Debug.Log($"[CLIENTE] ¡Te has conectado exitosamente a la partida del Host!");
        }
    }

    public async Task<string> CrearPartidaHost()
    {
        try
        {
            Allocation asignacion = await RelayService.Instance.CreateAllocationAsync(1);
            codigoGenerado = await RelayService.Instance.GetJoinCodeAsync(asignacion.AllocationId);
            
            RelayServerData relayServerData = new RelayServerData(asignacion, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            NetworkManager.Singleton.StartHost();
            Debug.Log($"[GestorRedRelay] Código de la sala: {codigoGenerado}");
            
            return codigoGenerado;
        }
        catch (Exception excepcion)
        {
            Debug.LogError($"[GestorRedRelay] Error al crear Host: {excepcion.Message}");
            return null;
        }
    }

    public async Task UnirseComoCliente(string codigoUnion)
    {
        if (string.IsNullOrWhiteSpace(codigoUnion))
        {
            Debug.LogError("[GestorRedRelay] ¡No puedes unirte! El código de sala está vacío. Escribe el código que te dio el Host.");
            return;
        }

        try
        {
            JoinAllocation asignacion = await RelayService.Instance.JoinAllocationAsync(codigoUnion.Trim());
            
            RelayServerData relayServerData = new RelayServerData(asignacion, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception excepcion)
        {
            Debug.LogError($"[GestorRedRelay] Error al unirse: {excepcion.Message}");
        }
    }

    // ==========================================
    // INTERFAZ GRÁFICA DE PRUEBA RÁPIDA (OnGUI)
    // ==========================================
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        // Bloque de HOST (La PC de escritorio)
        if (GUILayout.Button("1. Crear Partida (Host)", GUILayout.Height(50)))
        {
            _ = CrearPartidaHost();
        }
        if (!string.IsNullOrEmpty(codigoGenerado))
        {
            GUILayout.Label($"TU CÓDIGO ES: {codigoGenerado}");
            GUILayout.Label("Escribe este código en tu Laptop.");
        }

        GUILayout.Space(30);

        // Bloque de CLIENTE (La Laptop)
        GUILayout.Label("Si eres el cliente (Laptop), ingresa el código aquí:");
        codigoAIntroducir = GUILayout.TextField(codigoAIntroducir, GUILayout.Height(30));
        
        if (GUILayout.Button("2. Unirse a Partida (Client)", GUILayout.Height(50)))
        {
            _ = UnirseComoCliente(codigoAIntroducir);
        }

        GUILayout.EndArea();
    }
}
