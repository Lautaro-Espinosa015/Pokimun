using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject options;
    public bool isPaused;
    private GestorNivel gestor;

    private static readonly Color AzulFondo = new Color(.015f, .025f, .045f, .84f);
    private static readonly Color AzulBoton = new Color(.11f, .26f, .32f);

    private void Start()
    {
        gestor = FindFirstObjectByType<GestorNivel>();
        Reanudar();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused) Reanudar(); else Pausar();
        }
    }

    public void Pausar()
    {
        if (gestor != null && !gestor.EnBatalla) return;
        
        if (pauseMenu == null)
        {
            CrearMenuPausa();
        }

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

    public void CambiarVolumenMusica(float valor)
    {
        PlayerPrefs.SetFloat("VolumenMusica", valor);
    }

    public void CambiarVolumenEfectos(float valor)
    {
        PlayerPrefs.SetFloat("VolumenEfectos", valor);
    }
    
    public void SalirJuego()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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

    // --- UI Dinámica ---
    private void CrearMenuPausa()
    {
        GameObject canvasObj = new GameObject("Canvas_Pausa", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // Aseguramos que esté sobre todo lo demás
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }
        
        GameObject velo = new GameObject("MenuPausa_Fondo", typeof(RectTransform), typeof(Image));
        pauseMenu = velo;
        RectTransform rtVelo = velo.GetComponent<RectTransform>();
        rtVelo.SetParent(canvas.transform, false);
        rtVelo.anchorMin = Vector2.zero;
        rtVelo.anchorMax = Vector2.one;
        rtVelo.offsetMin = rtVelo.offsetMax = Vector2.zero;
        velo.GetComponent<Image>().color = AzulFondo;

        GameObject panel = new GameObject("PanelCentral", typeof(RectTransform));
        RectTransform rtPanel = panel.GetComponent<RectTransform>();
        rtPanel.SetParent(rtVelo, false);
        rtPanel.anchorMin = rtPanel.anchorMax = new Vector2(0.5f, 0.5f);
        rtPanel.sizeDelta = new Vector2(500, 600);
        rtPanel.anchoredPosition = Vector2.zero;

        CrearTexto(rtPanel, "PAUSA", new Vector2(0, 200), new Vector2(500, 80), 50);

        CrearBoton(rtPanel, "Reanudar", new Vector2(0, 50), new Vector2(400, 80), Reanudar);
        CrearBoton(rtPanel, "Volver al Menú", new Vector2(0, -50), new Vector2(400, 80), Menu);
        CrearBoton(rtPanel, "Salir del Juego", new Vector2(0, -150), new Vector2(400, 80), SalirJuego);

        velo.SetActive(false);
    }

    private void CrearTexto(Transform padre, string texto, Vector2 pos, Vector2 tamano, float fuente)
    {
        GameObject obj = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;

        TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();
        txt.text = texto;
        txt.fontSize = fuente;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
    }

    private void CrearBoton(Transform padre, string texto, Vector2 pos, Vector2 tamano, UnityEngine.Events.UnityAction accion)
    {
        GameObject obj = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;

        obj.GetComponent<Image>().color = AzulBoton;
        Button btn = obj.GetComponent<Button>();
        btn.onClick.AddListener(accion);

        CrearTexto(obj.transform, texto, Vector2.zero, tamano, 30);
    }
}
