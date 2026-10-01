using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra avisos visuales para ataques del jugador, ataques del rival y la defensa.
/// El Canvas se crea en tiempo de ejecución para no depender de una escena concreta.
/// </summary>
public class FeedbackAtaqueUI : MonoBehaviour
{
    private CanvasGroup grupo;
    private RectTransform tarjeta;
    private Image fondo;
    private Outline contorno;
    private TMP_Text texto;
    private Coroutine rutina;

    public void MostrarAtaque(string nombreAtaque, bool critico, float duracion, bool ataqueDelRival = false)
    {
        PrepararInterfaz();

        if (rutina != null)
        {
            StopCoroutine(rutina);
        }

        if (critico)
        {
            texto.text =
                $"<size=78%><color=#FFD166><b>¡GOLPE CRÍTICO!</b></color></size>\n" +
                $"<size=145%><color=#FFFFFF><b>{nombreAtaque}</b></color></size>";
            fondo.color = new Color(0.13f, 0.095f, 0.035f, 0.97f);
            contorno.effectColor = new Color(1f, 0.74f, 0.2f, 1f);
            contorno.effectDistance = new Vector2(3f, -3f);
        }
        else if (ataqueDelRival)
        {
            texto.text =
                $"<size=145%><color=#FF9A8B><b>{nombreAtaque}</b></color></size>\n" +
                "<size=70%><color=#F4D7D2>ATAQUE DEL RIVAL</color></size>";
            fondo.color = new Color(0.15f, 0.065f, 0.075f, 0.97f);
            contorno.effectColor = new Color(1f, 0.4f, 0.34f, 0.95f);
            contorno.effectDistance = new Vector2(2f, -2f);
        }
        else
        {
            texto.text =
                $"<size=145%><color=#A7F3D0><b>{nombreAtaque}</b></color></size>\n" +
                "<size=70%><color=#D6E4F0>ATAQUE</color></size>";
            fondo.color = new Color(0.055f, 0.09f, 0.14f, 0.96f);
            contorno.effectColor = new Color(0.35f, 0.83f, 0.75f, 0.95f);
            contorno.effectDistance = new Vector2(2f, -2f);
        }

        rutina = StartCoroutine(MostrarDurante(Mathf.Max(0.01f, duracion), critico));
    }

    public void MostrarDefensa(float duracion)
    {
        PrepararInterfaz();

        if (rutina != null)
        {
            StopCoroutine(rutina);
        }

        texto.text =
            "<size=145%><color=#73D2DE><b>ESCUDO</b></color></size>\n" +
            "<size=70%><color=#D6E4F0>DEFENSA ACTIVADA</color></size>";
        fondo.color = new Color(0.045f, 0.105f, 0.15f, 0.96f);
        contorno.effectColor = new Color(0.35f, 0.82f, 0.92f, 0.95f);
        contorno.effectDistance = new Vector2(2f, -2f);

        rutina = StartCoroutine(MostrarDurante(Mathf.Max(0.01f, duracion), false));
    }
    private void PrepararInterfaz()
    {
        if (grupo != null)
        {
            return;
        }

        var canvasObject = new GameObject(
            "Canvas - Aviso de ataque",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var tarjetaObject = new GameObject(
            "Tarjeta de ataque",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup));
        tarjetaObject.transform.SetParent(canvasObject.transform, false);

        tarjeta = tarjetaObject.GetComponent<RectTransform>();
        tarjeta.anchorMin = new Vector2(0.5f, 0.5f);
        tarjeta.anchorMax = new Vector2(0.5f, 0.5f);
        tarjeta.pivot = new Vector2(0.5f, 0.5f);
        tarjeta.anchoredPosition = new Vector2(0f, -220f);
        tarjeta.sizeDelta = new Vector2(500f, 150f);

        fondo = tarjetaObject.GetComponent<Image>();
        Sprite spriteRedondeado = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (spriteRedondeado != null)
        {
            fondo.sprite = spriteRedondeado;
            fondo.type = Image.Type.Sliced;
        }
        fondo.raycastTarget = false;

        contorno = tarjetaObject.GetComponent<Outline>();
        contorno.useGraphicAlpha = true;

        grupo = tarjetaObject.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        var textoObject = new GameObject(
            "Nombre del ataque",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textoObject.transform.SetParent(tarjetaObject.transform, false);

        var rectTexto = textoObject.GetComponent<RectTransform>();
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = new Vector2(18f, 10f);
        rectTexto.offsetMax = new Vector2(-18f, -10f);

        texto = textoObject.GetComponent<TextMeshProUGUI>();
        texto.alignment = TextAlignmentOptions.Center;
        texto.verticalAlignment = VerticalAlignmentOptions.Middle;
        texto.enableAutoSizing = true;
        texto.fontSize = 44f;
        texto.fontSizeMin = 24f;
        texto.fontSizeMax = 56f;
        texto.margin = new Vector4(8f, 4f, 8f, 4f);
        texto.raycastTarget = false;
    }

    private IEnumerator MostrarDurante(float duracion, bool critico)
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float entrada = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / 0.18f));
            float salida = Mathf.Clamp01((duracion - tiempo) / 0.28f);
            grupo.alpha = Mathf.Min(entrada, salida);

            float pulso = critico
                ? 1f + 0.035f * Mathf.Sin(tiempo * 9f)
                : 1f;
            tarjeta.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, entrada) * pulso;

            yield return null;
        }

        grupo.alpha = 0f;
        tarjeta.localScale = Vector3.one;
        rutina = null;
    }
}