using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private bool cargandoPartida;
    #region Variables
    public GameObject menuPrincipal;
    public GameObject menuOpciones;
    public GameObject menuTutorial;
    public GameObject menuCreditos;
    #endregion

    private AudioSource[] audiosEnLoop;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = 1f; // Siempre 1 para no afectar a los efectos
        
        // Cachear los audios en loop (música)
        var todosLosAudios = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        System.Collections.Generic.List<AudioSource> loops = new System.Collections.Generic.List<AudioSource>();
        foreach (var a in todosLosAudios) { if (a.loop) loops.Add(a); }
        audiosEnLoop = loops.ToArray();

        ActualizarVolumenMusica();

        // Buena práctica: Forzar que al arrancar solo el principal esté encendido
        Volver();
    }

    public void ActualizarVolumenMusica()
    {
        float volumenMusica = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenMusica", 0.35f));
        if (audiosEnLoop != null)
        {
            foreach (var audio in audiosEnLoop)
            {
                // Multiplicamos por 1.5f para compensar que la pista del menú es inherentemente más baja
                if (audio != null) audio.volume = Mathf.Clamp01(volumenMusica * 1.5f);
            }
        }
    }

    #region Botones Menu
    public void Jugar()
    {
        if (cargandoPartida) return;
        cargandoPartida = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Escenario");
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
        // 1. Apagamos TODOS los paneles primero
        if (menuPrincipal) menuPrincipal.SetActive(false);
        if (menuOpciones) menuOpciones.SetActive(false);
        if (menuTutorial) menuTutorial.SetActive(false);
        if (menuCreditos) menuCreditos.SetActive(false);

        // 2. Encendemos únicamente el que necesitamos
        if (panelDestino) panelDestino.SetActive(true);
    }
}
