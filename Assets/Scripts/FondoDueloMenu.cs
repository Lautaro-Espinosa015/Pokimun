using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class FondoDueloMenu : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Menus") return;
        var clip = Resources.Load<VideoClip>("FondoDuelo");
        if (clip == null) return;
        var fondo = new GameObject("Fondo del duelo", typeof(Canvas), typeof(VideoPlayer), typeof(FondoDueloMenu));
        SceneManager.MoveGameObjectToScene(fondo, escena);
        fondo.GetComponent<FondoDueloMenu>().Preparar(clip);
    }

    private VideoPlayer video;
    private RawImage imagen;

    private void Preparar(VideoClip clip)
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;
        var visual = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        visual.transform.SetParent(transform, false);
        var ajuste = visual.GetComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ajuste.aspectRatio = (float)clip.width / clip.height;
        imagen = visual.GetComponent<RawImage>();
        imagen.raycastTarget = false;
        imagen.enabled = false;
        video = GetComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.clip = clip;
        video.isLooping = true;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.renderMode = VideoRenderMode.APIOnly;
        video.sendFrameReadyEvents = true;
        video.frameReady += Mostrar;
        video.errorReceived += AlFallar;
        video.Play();
    }

    private void Mostrar(VideoPlayer reproductor, long cuadro)
    {
        imagen.texture = reproductor.texture;
        imagen.enabled = true;
        reproductor.sendFrameReadyEvents = false;
    }

    private void AlFallar(VideoPlayer reproductor, string motivo)
    {
        imagen.enabled = false;
        Debug.LogWarning("[Menú] Fondo de duelo no disponible: " + motivo, this);
    }
}
