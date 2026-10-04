using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class PausaSetup : EditorWindow
{
    [MenuItem("Pokimun/Auto-Generar Menú de Pausa Estético")]
    public static void GenerarMenu()
    {
        // Verificar que estamos en la escena correcta
        if (SceneManager.GetActiveScene().name != "Escenario")
        {
            EditorUtility.DisplayDialog("Error", "Abre la escena 'Escenario' antes de ejecutar esto.", "OK");
            return;
        }

        PauseManager pauseManager = Object.FindFirstObjectByType<PauseManager>();
        if (pauseManager == null)
        {
            GestorNivel gestor = Object.FindFirstObjectByType<GestorNivel>();
            if (gestor != null)
            {
                pauseManager = gestor.gameObject.AddComponent<PauseManager>();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No se encontró el GestorNivel ni el PauseManager en la escena.", "OK");
                return;
            }
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No hay Canvas en la escena.", "OK");
            return;
        }

        // Si ya hay un menu de pausa anterior, lo borramos
        if (pauseManager.pauseMenu != null)
        {
            DestroyImmediate(pauseManager.pauseMenu);
        }

        // Cargar el sprite "Boton1.png" (nueva skin) y la fuente
        Sprite botonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/Menus/Image/Boton1.png");
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LuckiestGuy-Regular SDF.asset");
        
        // Auto-configurar FeedbackAtaqueUI para los paneles de derrota/victoria
        FeedbackAtaqueUI[] feedbacks = Object.FindObjectsByType<FeedbackAtaqueUI>(FindObjectsSortMode.None);
        foreach (var fb in feedbacks)
        {
            fb.botonSprite = botonSprite;
            fb.fuentePersonalizada = fontAsset;
            EditorUtility.SetDirty(fb);
        }

        // Crear Fondo Translúcido
        GameObject veloObj = new GameObject("MenuPausa_Estetico", typeof(RectTransform), typeof(Image));
        RectTransform rtVelo = veloObj.GetComponent<RectTransform>();
        rtVelo.SetParent(canvas.transform, false);
        rtVelo.anchorMin = Vector2.zero;
        rtVelo.anchorMax = Vector2.one;
        rtVelo.offsetMin = rtVelo.offsetMax = Vector2.zero;
        veloObj.GetComponent<Image>().color = new Color(.015f, .025f, .045f, .84f);

        // Titulo y botones irán dentro de un Panel Central marrón translúcido
        GameObject panelObj = new GameObject("PanelCentral", typeof(RectTransform), typeof(Image));
        RectTransform rtPanel = panelObj.GetComponent<RectTransform>();
        rtPanel.SetParent(rtVelo, false);
        rtPanel.anchorMin = rtPanel.anchorMax = new Vector2(0.5f, 0.5f);
        rtPanel.sizeDelta = new Vector2(500, 600);
        rtPanel.anchoredPosition = Vector2.zero;
        panelObj.GetComponent<Image>().color = new Color(0.35f, 0.22f, 0.10f, 0.85f); // Marrón translúcido

        // Titulo
        GameObject tituloObj = new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rtTitulo = tituloObj.GetComponent<RectTransform>();
        rtTitulo.SetParent(rtPanel, false);
        rtTitulo.anchoredPosition = new Vector2(0, 220);
        rtTitulo.sizeDelta = new Vector2(500, 100);
        TextMeshProUGUI txtTitulo = tituloObj.GetComponent<TextMeshProUGUI>();
        txtTitulo.text = "PAUSA";
        txtTitulo.fontSize = 70;
        if (fontAsset != null) txtTitulo.font = fontAsset;
        txtTitulo.alignment = TextAlignmentOptions.Center;
        txtTitulo.color = new Color(1f, 0.8f, 0.2f);

        // Crear slider de Música
        GameObject sliderMusObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderMusObj.name = "VolumenMusicaSlider";
        RectTransform rtSliderMus = sliderMusObj.GetComponent<RectTransform>();
        rtSliderMus.SetParent(rtPanel, false);
        rtSliderMus.anchoredPosition = new Vector2(0, 150);
        rtSliderMus.sizeDelta = new Vector2(300, 20);
        Slider sliderMusica = sliderMusObj.GetComponent<Slider>();
        sliderMusica.value = PlayerPrefs.GetFloat("VolumenMusica", 0.5f);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sliderMusica.onValueChanged, 
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction<float>), pauseManager, "CambiarVolumenMusica") as UnityEngine.Events.UnityAction<float>);

        GameObject txtMusObj = new GameObject("TextoMusica", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rtTxtMus = txtMusObj.GetComponent<RectTransform>();
        rtTxtMus.SetParent(rtSliderMus, false);
        rtTxtMus.anchoredPosition = new Vector2(0, 30);
        rtTxtMus.sizeDelta = new Vector2(300, 30);
        TextMeshProUGUI txtMus = txtMusObj.GetComponent<TextMeshProUGUI>();
        txtMus.text = "Música";
        txtMus.fontSize = 24;
        if (fontAsset != null) txtMus.font = fontAsset;
        txtMus.alignment = TextAlignmentOptions.Center;
        txtMus.color = Color.white;

        // Crear slider de Efectos
        GameObject sliderEfxObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderEfxObj.name = "VolumenEfectosSlider";
        RectTransform rtSliderEfx = sliderEfxObj.GetComponent<RectTransform>();
        rtSliderEfx.SetParent(rtPanel, false);
        rtSliderEfx.anchoredPosition = new Vector2(0, 70);
        rtSliderEfx.sizeDelta = new Vector2(300, 20);
        Slider sliderEfectos = sliderEfxObj.GetComponent<Slider>();
        sliderEfectos.value = PlayerPrefs.GetFloat("VolumenEfectos", 0.5f);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sliderEfectos.onValueChanged, 
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction<float>), pauseManager, "CambiarVolumenEfectos") as UnityEngine.Events.UnityAction<float>);

        GameObject txtEfxObj = new GameObject("TextoEfectos", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rtTxtEfx = txtEfxObj.GetComponent<RectTransform>();
        rtTxtEfx.SetParent(rtSliderEfx, false);
        rtTxtEfx.anchoredPosition = new Vector2(0, 30);
        rtTxtEfx.sizeDelta = new Vector2(300, 30);
        TextMeshProUGUI txtEfx = txtEfxObj.GetComponent<TextMeshProUGUI>();
        txtEfx.text = "Efectos y Ataques";
        txtEfx.fontSize = 24;
        if (fontAsset != null) txtEfx.font = fontAsset;
        txtEfx.alignment = TextAlignmentOptions.Center;
        txtEfx.color = Color.white;

        // Crear botones
        CrearBotonEstetico(rtPanel, "Reanudar", new Vector2(0, 30), botonSprite, fontAsset, pauseManager, "Reanudar");
        CrearBotonEstetico(rtPanel, "Menú Principal", new Vector2(0, -70), botonSprite, fontAsset, pauseManager, "Menu");
        CrearBotonEstetico(rtPanel, "Salir del Juego", new Vector2(0, -170), botonSprite, fontAsset, pauseManager, "SalirJuego");

        // Asignar al manager y desactivar
        pauseManager.pauseMenu = veloObj;
        veloObj.SetActive(false);

        // Forzar guardado de la escena
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Éxito", "Menú de Pausa generado con la nueva skin y fuente. También se aplicaron al menú de Victoria/Derrota y se agregó la animación de hover a todos.", "Genial");
    }

    private static void CrearBotonEstetico(Transform padre, string texto, Vector2 pos, Sprite sprite, TMP_FontAsset font, PauseManager manager, string methodName)
    {
        GameObject btnObj = new GameObject("Boton_" + texto, typeof(RectTransform), typeof(Image), typeof(Button), typeof(BotonAnimado));
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(350, 80);

        Image img = btnObj.GetComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(.11f, .26f, .32f);
        }

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, 
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), manager, methodName) as UnityEngine.Events.UnityAction);

        GameObject txtObj = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.SetParent(rt, false);
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = rtTxt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
        txt.text = texto;
        txt.fontSize = 30;
        if (font != null) txt.font = font;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
    }
}
