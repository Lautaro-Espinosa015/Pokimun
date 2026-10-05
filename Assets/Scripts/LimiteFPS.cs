using UnityEngine;

public static class LimiteFPS
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configurar()
    {
        // Desactivar VSync para poder forzar un límite de frames manual
        QualitySettings.vSyncCount = 0;
        
        // Limitar el juego a los FPS guardados por el usuario (o 120 por defecto)
        int fps = PlayerPrefs.GetInt("LimiteFPS", 120);
        Application.targetFrameRate = fps;
        
        Debug.Log($"[Sistema] Límite de FPS inicial establecido a {fps}.");
    }
}
