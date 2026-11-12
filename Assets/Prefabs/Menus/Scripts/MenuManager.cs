using Pokimun.Audio;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private bool cargandoPartida;
    #region Variables
    public GameObject menuPrincipal;
    public GameObject menuOpciones;
    public GameObject menuTutorial;
    public GameObject menuCreditos;
    public DateTime InicioPanelUtc { get; private set; } = DateTime.UtcNow;
    #endregion

    private const string MusicaMenu = "Play_Musica_Menu";
    private uint musicaEnCurso;

    private void Start()
    {
        Time.timeScale = 1f;
        var audio = AudioGlobal.Asegurar();
        musicaEnCurso = AudioTP.Post(MusicaMenu, gameObject);
        AudioTP.Cargar(BotonAnimado.EventoHover);
        AudioTP.Cargar(BotonAnimado.EventoClick);
        foreach (var panel in new[] { menuPrincipal, menuOpciones, menuTutorial, menuCreditos })
        {
            if (panel == null) continue;
            foreach (var animacion in panel.GetComponentsInChildren<BotonAnimado>(true))
                if (animacion.TryGetComponent<Button>(out var boton))
                    animacion.ActivarSonido(boton, audio.gameObject);
        }
        Volver();
    }

    private void OnDestroy()
    {
        if (musicaEnCurso != 0 && AkUnitySoundEngine.IsInitialized())
            AkUnitySoundEngine.StopPlayingID(musicaEnCurso);
    }

    #region Botones Menu
    public void Jugar()
    {
        if (cargandoPartida) return;
        cargandoPartida = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lobby");
    }

    public void Opciones()
    {
        MostrarPanel(menuOpciones);
    }

    public void Tutorial()
    {
        MostrarPanel(menuTutorial);
    }

    public void Creditos()
    {
        MostrarPanel(menuCreditos);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion

    public void Volver()
    {
        MostrarPanel(menuPrincipal);
    }

    // Método robusto para evitar overlapping y bugs visuales
    private void MostrarPanel(GameObject panelDestino)
    {
        InicioPanelUtc = DateTime.UtcNow;
        // 1. Apagamos TODOS los paneles primero
        if (menuPrincipal) menuPrincipal.SetActive(false);
        if (menuOpciones) menuOpciones.SetActive(false);
        if (menuTutorial) menuTutorial.SetActive(false);
        if (menuCreditos) menuCreditos.SetActive(false);

        // 2. Encendemos únicamente el que necesitamos
        if (panelDestino) panelDestino.SetActive(true);
    }
}
