using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;

public class OpcionesSetup : MonoBehaviour
{
    [MenuItem("Pokimun/Generar Sliders de Opciones (Menú Principal)")]
    public static void GenerarOpciones()
    {
        Options opcionesObj = Object.FindFirstObjectByType<Options>(FindObjectsInactive.Include);
        if (opcionesObj == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró el script Options en la escena. Asegúrate de estar en la escena del Menú Principal.", "OK");
            return;
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LuckiestGuy-Regular SDF.asset");

        // Limpiar sliders anteriores generados por este script si existen
        Transform tMusica = opcionesObj.transform.Find("VolumenMusicaSlider");
        if (tMusica != null) DestroyImmediate(tMusica.gameObject);
        
        Transform tEfectos = opcionesObj.transform.Find("VolumenEfectosSlider");
        if (tEfectos != null) DestroyImmediate(tEfectos.gameObject);

        // Crear Slider Música
        GameObject sliderMusObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderMusObj.name = "VolumenMusicaSlider";
        RectTransform rtSliderMus = sliderMusObj.GetComponent<RectTransform>();
        rtSliderMus.SetParent(opcionesObj.transform, false);
        rtSliderMus.anchoredPosition = new Vector2(0, -30); // Más abajo para no chocar con Pantalla Completa
        rtSliderMus.sizeDelta = new Vector2(400, 30);
        Slider sliderMusica = sliderMusObj.GetComponent<Slider>();
        
        GameObject txtMusObj = new GameObject("TextoMusica", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rtTxtMus = txtMusObj.GetComponent<RectTransform>();
        rtTxtMus.SetParent(rtSliderMus, false);
        rtTxtMus.anchoredPosition = new Vector2(0, 40);
        rtTxtMus.sizeDelta = new Vector2(400, 40);
        TextMeshProUGUI txtMus = txtMusObj.GetComponent<TextMeshProUGUI>();
        txtMus.text = "VOLUMEN MÚSICA";
        txtMus.fontSize = 32;
        if (fontAsset != null) txtMus.font = fontAsset;
        txtMus.alignment = TextAlignmentOptions.Center;
        txtMus.color = Color.white;

        // Crear Slider Efectos
        GameObject sliderEfxObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderEfxObj.name = "VolumenEfectosSlider";
        RectTransform rtSliderEfx = sliderEfxObj.GetComponent<RectTransform>();
        rtSliderEfx.SetParent(opcionesObj.transform, false);
        rtSliderEfx.anchoredPosition = new Vector2(0, -130); // Aún más abajo
        rtSliderEfx.sizeDelta = new Vector2(400, 30);
        Slider sliderEfectos = sliderEfxObj.GetComponent<Slider>();

        GameObject txtEfxObj = new GameObject("TextoEfectos", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rtTxtEfx = txtEfxObj.GetComponent<RectTransform>();
        rtTxtEfx.SetParent(rtSliderEfx, false);
        rtTxtEfx.anchoredPosition = new Vector2(0, 40);
        rtTxtEfx.sizeDelta = new Vector2(400, 40);
        TextMeshProUGUI txtEfx = txtEfxObj.GetComponent<TextMeshProUGUI>();
        txtEfx.text = "VOLUMEN EFECTOS (FX)";
        txtEfx.fontSize = 32;
        if (fontAsset != null) txtEfx.font = fontAsset;
        txtEfx.alignment = TextAlignmentOptions.Center;
        txtEfx.color = Color.white;

        // Conectar los eventos
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sliderMusica.onValueChanged, 
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction<float>), opcionesObj, "changeVolumenMusica") as UnityEngine.Events.UnityAction<float>);
            
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sliderEfectos.onValueChanged, 
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction<float>), opcionesObj, "changeVolumenEfectos") as UnityEngine.Events.UnityAction<float>);

        // Asignar automáticamente en el inspector de Options
        opcionesObj.volumenMusica = sliderMusica;
        opcionesObj.volumenEfectos = sliderEfectos;
        EditorUtility.SetDirty(opcionesObj);

        // Forzar guardado de la escena
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Éxito", "Los sliders de Música y Efectos han sido generados nuevamente. Se han movido hacia abajo para no tapar el botón de Pantalla Completa.", "Genial");
    }
}
