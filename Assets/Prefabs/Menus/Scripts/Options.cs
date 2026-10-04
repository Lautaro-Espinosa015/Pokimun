using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    #region Variables volumen
    public Slider volumen;
    public float value;
    public Image mute;
    #endregion
    #region Variables PC
    public Toggle fullscreen;
    #endregion

    void Start()
    {
        #region Start Volumen
        value = Mathf.Clamp01(PlayerPrefs.GetFloat("Volumen", 0.5f));
        if (volumen != null) volumen.SetValueWithoutNotify(value);
        AudioListener.volume = value;
        checkMute();
        #endregion
        #region Start PC
        if (fullscreen != null) fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
        #endregion
    }
    #region Volumen
    public void changeVolumen(float valor)
    {
        value = Mathf.Clamp01(valor);
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volumen", value);
        checkMute();
    }
    public void checkMute()
    {
        if (mute == null) return;
        if (value == 0)
        {
            mute.enabled = true;
        }
        else
        {
            mute.enabled = false;
        }
    }
    #endregion

    #region Pantalla Completa
    public void changeFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    #endregion

}
