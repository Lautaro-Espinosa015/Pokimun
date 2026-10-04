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

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Volumen", 0.5f));
        // Buena práctica: Forzar que al arrancar solo el principal esté encendido
        Volver();
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
