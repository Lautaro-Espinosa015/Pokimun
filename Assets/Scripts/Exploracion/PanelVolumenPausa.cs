using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Controles compartidos de pausa. Usa las mismas preferencias que el menú principal y el combate.</summary>
public sealed class PanelVolumenPausa : MonoBehaviour
{
    public static void Crear(Transform padre, Vector2 posicion, bool estiloExploracion = false)
    {
        var raiz = Rect("Volumen", padre, posicion, new Vector2(480, 145));
        raiz.gameObject.AddComponent<PanelVolumenPausa>();
        Fila(raiz, "Música", "VolumenMusica", .35f, 40);
        Fila(raiz, "Efectos y ambiente", "VolumenEfectos", .85f, -40);
        if(estiloExploracion)
        {
            foreach(var texto in raiz.GetComponentsInChildren<TMP_Text>())Pokimun.Exploracion.TemaExploracion.Texto(texto);
            foreach(var imagen in raiz.GetComponentsInChildren<Image>())
                imagen.color=imagen.name=="Nivel"?new Color(.75f,.5f,.16f):imagen.name=="Manija"?Pokimun.Exploracion.TemaExploracion.Oro:new Color(.13f,.085f,.045f);
        }
    }
    private static void Fila(Transform padre, string nombre, string clave, float defecto, float y)
    {
        var etiqueta = Rect(nombre, padre, new Vector2(0, y + 23), new Vector2(480, 28)).gameObject.AddComponent<TextMeshProUGUI>();
        etiqueta.fontSize = 21; etiqueta.color = new Color(.9f, .96f, .88f); etiqueta.alignment = TextAlignmentOptions.Left; etiqueta.raycastTarget = false;
        var rt = Rect("Control " + nombre, padre, new Vector2(0, y - 8), new Vector2(480, 30));
        rt.gameObject.AddComponent<Image>().color = new Color(.08f, .13f, .15f);
        var slider = rt.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
        var relleno = Rect("Nivel", rt, Vector2.zero, Vector2.zero);
        relleno.anchorMin = Vector2.zero; relleno.anchorMax = Vector2.one; relleno.offsetMin = new Vector2(5, 6); relleno.offsetMax = new Vector2(-5, -6);
        relleno.gameObject.AddComponent<Image>().color = new Color(.24f, .58f, .55f);
        var area = Rect("Área del control", rt, Vector2.zero, Vector2.zero);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(10, 0); area.offsetMax = new Vector2(-10, 0);
        var manija = Rect("Manija", area, Vector2.zero, new Vector2(24, 6));
        var imagen = manija.gameObject.AddComponent<Image>(); imagen.color = new Color(.88f, .94f, .78f);
        slider.fillRect = relleno; slider.handleRect = manija; slider.targetGraphic = imagen;
        slider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(clave, defecto)));
        etiqueta.text = nombre + "  " + Mathf.RoundToInt(slider.value * 100) + "%";
        slider.onValueChanged.AddListener(valor => {
            valor = Mathf.Clamp01(valor); PlayerPrefs.SetFloat(clave, valor);
            etiqueta.text = nombre + "  " + Mathf.RoundToInt(valor * 100) + "%";
        });
    }
    private static RectTransform Rect(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        var rt = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = posicion; rt.sizeDelta = tamano; return rt;
    }
    private void OnDisable() => PlayerPrefs.Save();
    private void OnApplicationPause(bool pausa) { if (pausa) PlayerPrefs.Save(); }
}
