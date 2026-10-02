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
        volumen.value = PlayerPrefs.GetFloat("Volumen", 0.5f);
        AudioListener.volume = volumen.value;
        checkMute();
        #endregion
        #region Start PC
        if (Screen.fullScreen)
        {
            fullscreen.isOn = true;
        }
        else
        {
            fullscreen.isOn = false;
        }
        #endregion
    }
    #region Volumen
    public void changeVolumen(float valor)
    {
        value = valor;
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volumen", value);
        checkMute();
    }
    public void checkMute()
    {
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