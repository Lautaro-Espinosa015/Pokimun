using System;
using System.Collections.Generic;
using UnityEngine;

public class ControladorVozMenu : MonoBehaviour
{
    [Header("Referencia al MenuManager")]
    [Tooltip("Arrastra aquí el objeto que tiene el script MenuManager")]
    public MenuManager menuManager;

    private GestorVozMultiplataforma voz;
    private Dictionary<string, Action> comandosMenu = new Dictionary<string, Action>();

    private void OnEnable()
    {
        // Si no se asignó en el inspector, intenta buscarlo automáticamente
        if (menuManager == null)
        {
            menuManager = FindFirstObjectByType<MenuManager>();
        }

        if (menuManager == null) return;
        ConfigurarComandos();
        IniciarReconocedor();
    }

    private void ConfigurarComandos()
    {
        comandosMenu.Clear();

        // Comandos en Español (Principales y sinónimos)
        comandosMenu.Add("jugar", () => menuManager.Jugar());
        comandosMenu.Add("iniciar", () => menuManager.Jugar());
        comandosMenu.Add("comenzar", () => menuManager.Jugar());
        comandosMenu.Add("empezar", () => menuManager.Jugar());

        comandosMenu.Add("opciones", () => menuManager.Opciones());
        comandosMenu.Add("ajustes", () => menuManager.Opciones());
        comandosMenu.Add("configuración", () => menuManager.Opciones());
        comandosMenu.Add("configuracion", () => menuManager.Opciones()); // Sin tilde por si acaso

        comandosMenu.Add("tutorial", () => menuManager.Tutorial());
        comandosMenu.Add("instrucciones", () => menuManager.Tutorial());
        comandosMenu.Add("ayuda", () => menuManager.Tutorial());
        comandosMenu.Add("como jugar", () => menuManager.Tutorial());

        comandosMenu.Add("creditos", () => menuManager.Creditos());
        comandosMenu.Add("autores", () => menuManager.Creditos());
        comandosMenu.Add("creadores", () => menuManager.Creditos());

        comandosMenu.Add("volver", () => menuManager.Volver());
        comandosMenu.Add("regresar", () => menuManager.Volver());
        comandosMenu.Add("atras", () => menuManager.Volver());
        comandosMenu.Add("atrás", () => menuManager.Volver());

        comandosMenu.Add("salir", () => menuManager.Salir());
        comandosMenu.Add("cerrar", () => menuManager.Salir());
        comandosMenu.Add("abandonar", () => menuManager.Salir());
        
        // Comandos en Inglés (por si acaso / bilingüe)
        comandosMenu.Add("play", () => menuManager.Jugar());
        comandosMenu.Add("start", () => menuManager.Jugar());
        comandosMenu.Add("options", () => menuManager.Opciones());
        comandosMenu.Add("settings", () => menuManager.Opciones());
        comandosMenu.Add("how to play", () => menuManager.Tutorial());
        comandosMenu.Add("credits", () => menuManager.Creditos());
        comandosMenu.Add("back", () => menuManager.Volver());
        comandosMenu.Add("return", () => menuManager.Volver());
        comandosMenu.Add("exit", () => menuManager.Salir());
        comandosMenu.Add("quit", () => menuManager.Salir());

        // Comandos de Pantalla Completa
        Action togglePC = () => {
            var opt = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude);
            if (opt != null) {
                bool isFullScreen = false;
                if (opt.fullscreen != null) isFullScreen = !opt.fullscreen.isOn;
                else isFullScreen = !Screen.fullScreen;

                if (opt.fullscreen != null) opt.fullscreen.SetIsOnWithoutNotify(isFullScreen);
                opt.changeFullscreen(isFullScreen);
                Debug.Log($"[Voz Menu] Pantalla cambiada a: {(isFullScreen ? "Completa" : "Ventana")}");
            } else { Debug.Log("[Voz Menu] Ignorado: Panel de Opciones cerrado."); }
        };
        comandosMenu.Add("pantalla completa", togglePC);
        comandosMenu.Add("cambiar pantalla", togglePC);
        comandosMenu.Add("modo ventana", togglePC);

        // Comandos de Volumen (0 a 100 en pasos de 10)
        string[] palabrasNumeros = { "cero", "diez", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa", "cien" };
        for (int i = 0; i <= 10; i++)
        {
            float vol = i / 10f;
            int volText = i * 10;
            string palabraNum = palabrasNumeros[i];
            
            Action actFX = () => { 
                var o = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude); 
                if(o != null) { o.changeVolumenEfectos(vol); Debug.Log($"[Voz Menu] FX cambiado al {volText}%"); } 
                else { Debug.Log("[Voz Menu] Ignorado: Panel de Opciones cerrado."); }
            };
            Action actMusica = () => { 
                var o = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude); 
                if(o != null) { o.changeVolumenMusica(vol); Debug.Log($"[Voz Menu] Música cambiada al {volText}%"); } 
                else { Debug.Log("[Voz Menu] Ignorado: Panel de Opciones cerrado."); }
            };
            
            comandosMenu.Add($"efectos {palabraNum}", actFX);
            comandosMenu.Add($"efe equis {palabraNum}", actFX);
            comandosMenu.Add($"musica {palabraNum}", actMusica);
            comandosMenu.Add($"música {palabraNum}", actMusica);
        }

        // Comandos de FPS
        Action setFps60 = () => { var o = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude); if (o != null) o.SetFPS(60); else Debug.Log("[Voz Menu] Ignorado."); };
        Action setFps90 = () => { var o = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude); if (o != null) o.SetFPS(90); else Debug.Log("[Voz Menu] Ignorado."); };
        Action setFps120 = () => { var o = FindFirstObjectByType<Options>(FindObjectsInactive.Exclude); if (o != null) o.SetFPS(120); else Debug.Log("[Voz Menu] Ignorado."); };

        comandosMenu.Add("efe pe ese sesenta", setFps60);
        comandosMenu.Add("fotogramas sesenta", setFps60);
        comandosMenu.Add("efe pe ese noventa", setFps90);
        comandosMenu.Add("fotogramas noventa", setFps90);
        comandosMenu.Add("efe pe ese ciento veinte", setFps120);
        comandosMenu.Add("fotogramas ciento veinte", setFps120);
    }

    private void IniciarReconocedor()
    {
        voz = GetComponent<GestorVozMultiplataforma>();
        if (voz == null) voz = gameObject.AddComponent<GestorVozMultiplataforma>();
        voz.Iniciar(comandosMenu.Keys, AlReconocerFrase);
    }

    private void AlReconocerFrase(GestorVozMultiplataforma.ComandoReconocido resultado)
    {
        string frase = resultado.texto;
        Debug.Log($"[ControladorVozMenu] Comando reconocido: '{frase}'");

        // Si la frase que escuchó está en nuestra lista de comandos, ejecuta la acción
        if (comandosMenu.TryGetValue(frase, out Action comando))
        {
            comando.Invoke();
        }
    }

    private void OnDisable()
    {
        voz?.Detener();
    }
}
