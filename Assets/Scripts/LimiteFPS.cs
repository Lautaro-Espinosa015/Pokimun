using UnityEngine;

public static class LimiteFPS
{
    private const string Preferencia = "LimiteFPS";
    public static bool EsAndroid => Application.platform == RuntimePlatform.Android;
    private const int Predeterminado = 60;

    public static int Leer() => Validar(PlayerPrefs.GetInt(Preferencia, Predeterminado));

    private static int Validar(int fps) => fps switch
    {
        30 or 60 => fps,
        90 or 120 when !EsAndroid => fps,
        _ => Predeterminado
    };

    public static int GuardarYAplicar(int fps)
    {
        fps = Validar(fps);
        PlayerPrefs.SetInt(Preferencia, fps);
        PlayerPrefs.Save();
        Application.targetFrameRate = fps;
        return fps;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configurar()
    {
        QualitySettings.vSyncCount = 0;
        int fps = Leer();
        Application.targetFrameRate = fps;
        
        Debug.Log($"[Sistema] Límite de FPS inicial establecido a {fps}.");
    }
}
