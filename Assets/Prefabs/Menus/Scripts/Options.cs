using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    #region Variables volumen
    public Slider volumenMusica;
    public Slider volumenEfectos;
    public float valueMusica;
    public float valueEfectos;
    public Image mute;
    #endregion
    #region Variables PC
    public Toggle fullscreen;
    public TMPro.TextMeshProUGUI fpsText;
    #endregion

    void Start()
    {
        #region Start Volumen
        AudioListener.volume = 1f; // Forzar maestro al 100%
        valueMusica = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenMusica", 0.35f));
        valueEfectos = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenEfectos", 0.85f));
        
        if (volumenMusica != null) volumenMusica.SetValueWithoutNotify(valueMusica);
        if (volumenEfectos != null) volumenEfectos.SetValueWithoutNotify(valueEfectos);
        
        checkMute();
        #endregion
        #region Start PC
        if (fullscreen != null) fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
        int currentFPS = PlayerPrefs.GetInt("LimiteFPS", 120);
        Application.targetFrameRate = currentFPS;
        if (fpsText != null) fpsText.text = $"FPS: {currentFPS}";
        #endregion
    }

    #region Volumen
    public void changeVolumenMusica(float valor)
    {
        valueMusica = Mathf.Clamp01(valor);
        if (volumenMusica != null) volumenMusica.SetValueWithoutNotify(valueMusica);
        PlayerPrefs.SetFloat("VolumenMusica", valueMusica);
        checkMute();
        MenuManager menuManager = FindFirstObjectByType<MenuManager>();
        if (menuManager != null) menuManager.ActualizarVolumenMusica();
    }

    public void changeVolumenEfectos(float valor)
    {
        valueEfectos = Mathf.Clamp01(valor);
        if (volumenEfectos != null) volumenEfectos.SetValueWithoutNotify(valueEfectos);
        PlayerPrefs.SetFloat("VolumenEfectos", valueEfectos);
        checkMute();
    }

    public void checkMute()
    {
        if (mute == null) return;
        mute.enabled = (valueMusica == 0 && valueEfectos == 0);
    }
    #endregion

    #region Pantalla Completa
    public void changeFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    #endregion
    #region FPS
    public void CycleFPS()
    {
        int currentFPS = PlayerPrefs.GetInt("LimiteFPS", 120);
        if (currentFPS == 120) currentFPS = 60;
        else if (currentFPS == 60) currentFPS = 90;
        else currentFPS = 120;
        
        SetFPS(currentFPS);
    }
    
    public void SetFPS(int currentFPS)
    {
        PlayerPrefs.SetInt("LimiteFPS", currentFPS);
        Application.targetFrameRate = currentFPS;
        
        // Actualizar texto del botón si existe
        if (fpsText != null) fpsText.text = $"FPS: {currentFPS}";
        
        Debug.Log($"[Sistema] FPS cambiados a: {currentFPS}");
    }
    #endregion
}
