using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BarraVida : MonoBehaviour
{
    public Slider slider;
    private TMP_Text valor;
    private Image relleno;
    private string nombrePersonaje = "PS";

    public void AsignarNombre(string nombre)
    {
        nombrePersonaje = nombre;
        if (slider != null) MostrarVida(Mathf.RoundToInt(slider.value), Mathf.RoundToInt(slider.maxValue));
    }

    public void InicializarBarra(int maxVida) => MostrarVida(maxVida, maxVida);
    public void ActualizarVida(int vidaActual) => MostrarVida(vidaActual, slider != null ? Mathf.RoundToInt(slider.maxValue) : 100);

    public void MostrarVida(int actual, int maxima)
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) return;
        maxima = Mathf.Max(1, maxima);
        slider.minValue = 0;
        slider.maxValue = maxima;
        slider.wholeNumbers = true;
        slider.interactable = false;
        slider.SetValueWithoutNotify(Mathf.Clamp(actual, 0, maxima));
        if (relleno == null && slider.fillRect != null) relleno = slider.fillRect.GetComponent<Image>();
        if (relleno != null)
        {
            float proporcion = slider.value / maxima;
            relleno.color = proporcion > .5f ? new Color(.25f, .83f, .55f) :
                proporcion > .25f ? new Color(1f, .75f, .22f) : new Color(.95f, .3f, .3f);
        }
        if (valor == null)
        {
            var obj = new GameObject("Nombre y PS", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(slider.transform, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 6);
            rect.sizeDelta = new Vector2(0, 32);
            valor = obj.GetComponent<TextMeshProUGUI>();
            valor.fontSize = 25;
            valor.enableAutoSizing = true;
            valor.fontSizeMin = 15;
            valor.fontSizeMax = 25;
            valor.alignment = TextAlignmentOptions.Center;
            valor.color = Color.white;
            valor.outlineWidth = .2f;
            valor.outlineColor = new Color32(10, 20, 35, 255);
            valor.raycastTarget = false;
        }
        valor.text = $"<b>{nombrePersonaje}</b>   {Mathf.RoundToInt(slider.value)} / {maxima} PS";
    }
}
