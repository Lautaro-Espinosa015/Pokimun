using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controles táctiles de la versión Android; reutilizan la validación normal del combate.</summary>
public sealed class ControlesAndroid : MonoBehaviour
{
    private GestorNivel gestor;
    private readonly System.Collections.Generic.List<Button> acciones = new System.Collections.Generic.List<Button>();
    private RectTransform zonaSegura;
    private Rect ultimaZona;

#if UNITY_ANDROID && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
#endif
    private static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        Crear();
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo) => Crear();

    private static void Crear()
    {
        foreach (var escala in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ConfigurarEscala(escala);
        if (FindFirstObjectByType<ControlesAndroid>() != null) return;
        if (FindFirstObjectByType<GestorNivel>() == null) return;
        new GameObject("Controles Android").AddComponent<ControlesAndroid>();
    }

    public static void ConfigurarEscala(CanvasScaler escala)
    {
        var canvas = escala.GetComponent<Canvas>();
        if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace ||
            escala.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize) return;
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // El diseño original llega a y=576: reservar 1200 unidades de alto evita recortar sus títulos.
        escala.referenceResolution = new Vector2(1920, 1200);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    private void Awake()
    {
        gestor = FindFirstObjectByType<GestorNivel>();
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Debajo de los menús de pausa y de fin de partida.
        canvas.sortingOrder = -1;
        var escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1280, 720);
        escala.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        zonaSegura = new GameObject("Zona segura", typeof(RectTransform)).GetComponent<RectTransform>();
        zonaSegura.SetParent(transform, false);
        ActualizarZona();
        CrearBoton("Escudo", -210, () => Registrar(AccionTurno.Defensa()));
        CrearBoton("Cura", 0, () => Registrar(AccionTurno.Curar()));
        CrearBoton("Ataque", 210, () => gestor?.IntentarAtaqueManual());
        var pausa = CrearBoton("Pausa", 0, () => FindFirstObjectByType<PauseManager>()?.Pausar());
        acciones.Remove(pausa);
        var rectPausa = (RectTransform)pausa.transform;
        rectPausa.anchorMin = rectPausa.anchorMax = new Vector2(1, 0);
        rectPausa.pivot = new Vector2(1, 0);
        rectPausa.anchoredPosition = new Vector2(-16, 16);
        rectPausa.sizeDelta = new Vector2(135, 64);
    }

    private Button CrearBoton(string nombre, float x, UnityEngine.Events.UnityAction accion)
    {
        var obj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(zonaSegura, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(x, 16);
        rect.sizeDelta = new Vector2(185, 64);
        obj.GetComponent<Image>().color = new Color(.06f, .15f, .23f, .94f);
        var boton = obj.GetComponent<Button>();
        boton.onClick.AddListener(accion);
        acciones.Add(boton);
        var etiqueta = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        var texto = etiqueta.GetComponent<Text>();
        texto.rectTransform.SetParent(rect, false);
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.offsetMin = texto.rectTransform.offsetMax = Vector2.zero;
        texto.text = nombre;
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 26;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.raycastTarget = false;
        return boton;
    }

    private void Registrar(AccionTurno accion) => gestor?.IntentarRegistrarAccion(accion, "táctil Android");

    private void Update()
    {
        if (Screen.safeArea != ultimaZona) ActualizarZona();
        foreach (var boton in acciones) boton.interactable = gestor != null && gestor.PuedeRecibirAcciones;
    }

    private void ActualizarZona()
    {
        ultimaZona = Screen.safeArea;
        zonaSegura.anchorMin = new Vector2(ultimaZona.xMin / Screen.width, ultimaZona.yMin / Screen.height);
        zonaSegura.anchorMax = new Vector2(ultimaZona.xMax / Screen.width, ultimaZona.yMax / Screen.height);
        zonaSegura.offsetMin = zonaSegura.offsetMax = Vector2.zero;
    }
}
