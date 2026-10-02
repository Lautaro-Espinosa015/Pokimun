using UnityEngine;

public class PauseManager : MonoBehaviour
{
    #region Variables
    public GameObject pauseMenu;
    public GameObject options;
    public bool isPaused = false;
    #endregion
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        pauseMenu.SetActive(true);
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void Reanudar()
    {
        pauseMenu.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void Menu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menus");
    }
    public void Opciones()
    {
        if (pauseMenu) pauseMenu.SetActive(false);
        if (options) options.SetActive(true);
    }
    public void Regresar()
    {
        if (options) options.SetActive(false);
        if (pauseMenu) pauseMenu.SetActive(true);
    }
}
