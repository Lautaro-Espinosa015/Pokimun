using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject options;
    public bool isPaused;
    private GestorNivel gestor;

    private void Start()
    {
        gestor = FindFirstObjectByType<GestorNivel>();
        Reanudar();
    }
    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (isPaused) Reanudar(); else Pausar();
    }
    public void Pausar()
    {
        if (pauseMenu == null || (gestor != null && !gestor.EnBatalla)) return;
        isPaused = true;
        pauseMenu.SetActive(true);
        if (options != null) options.SetActive(false);
        gestor?.EstablecerPausa(true);
        // Una pausa local no debe congelar al otro equipo. En solitario sí detiene animación y resolución.
        Time.timeScale = gestor != null && gestor.EnLinea ? 1 : 0;
    }
    public void Reanudar()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (options != null) options.SetActive(false);
        isPaused = false;
        Time.timeScale = 1;
        gestor?.EstablecerPausa(false);
    }
    public void Menu()
    {
        Reanudar();
        if (gestor != null) gestor.VolverAlMenu();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("Menus");
    }
    public void Opciones()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (options != null) options.SetActive(true);
    }
    public void Regresar()
    {
        if (options != null) options.SetActive(false);
        if (pauseMenu != null) pauseMenu.SetActive(true);
    }
    private void OnDestroy() { Time.timeScale = 1; }
}
