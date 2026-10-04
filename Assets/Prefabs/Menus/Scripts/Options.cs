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
    #endregion

    void Start()
    {
        #region Start Volumen
        AudioListener.volume = 1f; // Forzar maestro al 100%
        valueMusica = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenMusica", 0.5f));
        valueEfectos = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenEfectos", 0.5f));
        
        if (volumenMusica != null) volumenMusica.SetValueWithoutNotify(valueMusica);
        if (volumenEfectos != null) volumenEfectos.SetValueWithoutNotify(valueEfectos);
        
        checkMute();
        #endregion
        #region Start PC
        if (fullscreen != null) fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
        #endregion
    }

    #region Volumen
    public void changeVolumenMusica(float valor)
    {
        valueMusica = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("VolumenMusica", valueMusica);
        checkMute();
        MenuManager menuManager = FindFirstObjectByType<MenuManager>();
        if (menuManager != null) menuManager.ActualizarVolumenMusica();
    }

    public void changeVolumenEfectos(float valor)
    {
        valueEfectos = Mathf.Clamp01(valor);
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

}
