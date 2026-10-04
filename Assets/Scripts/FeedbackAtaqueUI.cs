using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Presentación del combate. No decide ni aplica daño.</summary>
public class FeedbackAtaqueUI : MonoBehaviour
{
    private GameObject raiz, hud, sala, final;
    private RectTransform tarjeta;
    private CanvasGroup grupo;
    private Image fondo;
    private Outline contorno;
    private TMP_Text aviso, estado, acciones, estadoSala;
    private TMP_InputField codigo;
    private Button solo, crear, unir, cancelar;
    private Coroutine rutina;
    private static readonly Color Azul = new Color(.055f, .09f, .14f, .97f);
    private static readonly Color Verde = new Color(.4f, .9f, .75f);
    private static readonly Color Oro = new Color(1f, .78f, .3f);
    private static readonly Color Rojo = new Color(1f, .55f, .48f);

    public void ActualizarEstado(int ronda, int maxRondas, string personaje, string mensaje, string plan)
    {
        PrepararInterfaz();
        hud.SetActive(true);
        estado.text = $"<b>RONDA {ronda} / {maxRondas}</b>  ·  {personaje}\n<size=80%>{mensaje}</size>";
        acciones.text = plan;
    }

    public void MostrarAccion(AccionTurno accion, string personaje, int numero, float duracion, bool rival)
    {
        string detalle = accion.tipo == AccionCombate.Defender ? "Protege esta pareja de acciones" :
            $"{accion.jugada}" + (accion.critico ? "  ·  POTENCIA CRÍTICA" : "");
        Mostrar($"{personaje}  ·  ACCIÓN {numero}/2", ReglasCombate.Nombre(accion), detalle, duracion,
            accion.critico ? Oro : accion.tipo == AccionCombate.Defender ? Color.cyan : rival ? Rojo : Verde, accion.critico);
    }

    public void MostrarResultado(ResultadoIntercambio r, string nombreJugador, string nombreRival, float duracion)
    {
        string titulo = $"INTERCAMBIO {r.pareja}/2  ·  {ReglasCombate.Nombre(r.jugador)} / {ReglasCombate.Nombre(r.rival)}";
        string principal, detalle;
        if (r.ganador == 0)
        {
            principal = r.jugador.tipo == AccionCombate.Defender ? "AMBOS SE PROTEGEN" : "EMPATE";
            detalle = "Sin daño en este intercambio";
        }
        else
        {
            string atacante = r.ganador == 1 ? nombreJugador : nombreRival;
            string objetivo = r.ganador == 1 ? nombreRival : nombreJugador;
            principal = r.bloqueo ? "ESCUDO · DAÑO REDUCIDO" : r.critico ? "¡GOLPE CRÍTICO!" : $"{atacante} GANA";
            detalle = r.bloqueo
                ? $"{objetivo}: −{r.danioAplicado} PS  ·  Escudo: {r.danioSinDefensa} → {r.danioCalculado}"
                : $"{(r.ganador == 1 ? r.jugador.jugada : r.rival.jugada)} vence a {(r.ganador == 1 ? r.rival.jugada : r.jugador.jugada)}  ·  {objetivo}: −{r.danioAplicado} PS";
            if (r.bloqueo && r.critico) detalle += "  ·  Crítico";
        }
        Mostrar(titulo, principal, detalle, duracion, r.critico ? Oro : r.bloqueo ? Color.cyan : Verde, r.critico);
    }

    // Compatibilidad con eventos existentes en otras escenas.
    public void MostrarAtaque(string nombreAtaque, bool critico, float duracion, bool ataqueDelRival = false) =>
        Mostrar(ataqueDelRival ? "RIVAL" : "JUGADOR", nombreAtaque, critico ? "POTENCIA CRÍTICA" : "ATAQUE", duracion,
            critico ? Oro : ataqueDelRival ? Rojo : Verde, critico);
    public void MostrarDefensa(float duracion, bool defensaActivada = true) =>
        Mostrar("DEFENSA", "ESCUDO", defensaActivada ? "Defensa activada" : "Defensa finalizada", duracion, Color.cyan, false);

    private void Mostrar(string titulo, string principal, string detalle, float duracion, Color color, bool critico)
    {
        PrepararInterfaz();
        OcultarAviso();
        aviso.text = $"<size=62%>{titulo}</size>\n<color=#{ColorUtility.ToHtmlStringRGB(color)}><size={(critico ? 125 : 110)}%><b>{principal}</b></size></color>\n<size=65%>{detalle}</size>";
        fondo.color = Azul;
        contorno.effectColor = color;
        contorno.effectDistance = new Vector2(critico ? 3 : 2, -2);
        rutina = StartCoroutine(MostrarDurante(Mathf.Max(.1f, duracion), critico));
    }

    public void MostrarSala(Action jugarSolo, Action crearSala, Action<string> unirse, Action cancelarSala, Action menu)
    {
        PrepararInterfaz();
        OcultarAviso();
        hud.SetActive(false);
        if (final != null) final.SetActive(false);
        if (sala != null) Destroy(sala);
        sala = Modal("Elegir partida", new Vector2(660, 630), out Transform panel);
        Texto(panel, "BATALLA POKIMUN", new Vector2(0, 260), new Vector2(580, 50), 34);
        Texto(panel, "Elige cómo quieres jugar", new Vector2(0, 210), new Vector2(570, 40), 24);
        solo = Boton(panel, "Jugar en solitario", new Vector2(0, 138), new Vector2(530, 56), jugarSolo);
        crear = Boton(panel, "Crear partida multijugador", new Vector2(0, 65), new Vector2(530, 56), crearSala);
        codigo = CampoCodigo(panel, new Vector2(-112, -10));
        unir = Boton(panel, "Unirse", new Vector2(174, -10), new Vector2(180, 54), () => unirse(codigo.text));
        estadoSala = Texto(panel, "En solitario jugarás contra la CPU.", new Vector2(0, -103), new Vector2(560, 116), 23);
        estadoSala.richText = false;
        cancelar = Boton(panel, "Cancelar conexión", new Vector2(0, -204), new Vector2(530, 48), cancelarSala);
        cancelar.gameObject.SetActive(false);
        Boton(panel, "Volver al menú", new Vector2(0, -266), new Vector2(530, 48), menu);
    }

    public void ActualizarSala(string mensaje, bool ocupada, bool permitirCancelar = true)
    {
        if (estadoSala == null) return;
        estadoSala.text = mensaje;
        solo.interactable = crear.interactable = unir.interactable = codigo.interactable = !ocupada;
        cancelar.gameObject.SetActive(ocupada && permitirCancelar);
    }

    public void OcultarSala()
    {
        if (sala != null) sala.SetActive(false);
        if (hud != null) hud.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void MostrarFin(string titulo, string detalle, Action reiniciar, Action menu)
    {
        PrepararInterfaz();
        OcultarAviso();
        OcultarSala();
        if (final != null) Destroy(final);
        final = Modal("Resultado de la partida", new Vector2(660, 410), out Transform panel);
        Texto(panel, titulo, new Vector2(0, 126), new Vector2(590, 75), 44).color = Oro;
        Texto(panel, detalle, new Vector2(0, 24), new Vector2(570, 125), 26);
        if (reiniciar != null)
            Boton(panel, "Volver a jugar", new Vector2(-145, -133), new Vector2(260, 57), reiniciar);
        Boton(panel, "Menú principal", new Vector2(reiniciar == null ? 0 : 145, -133), new Vector2(260, 57), menu);
    }

    public void Limpiar()
    {
        OcultarAviso();
        if (final != null) final.SetActive(false);
    }

    public void MostrarInterfaz(bool visible)
    {
        if (raiz == null) return;
        raiz.GetComponent<Canvas>().enabled = visible;
        raiz.GetComponent<GraphicRaycaster>().enabled = visible;
        // El EventSystem sigue activo para poder usar los botones del menú de pausa.
    }

    public void OcultarAviso()
    {
        if (rutina != null) StopCoroutine(rutina);
        rutina = null;
        if (grupo != null) grupo.alpha = 0;
    }

    private void PrepararInterfaz()
    {
        if (raiz != null) return;
        raiz = new GameObject("Canvas - Combate", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = raiz.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        if (EventSystem.current == null)
        {
            var eventos = new GameObject("Eventos de combate", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventos.transform.SetParent(raiz.transform, false);
        }
        hud = Panel(raiz.transform, "Ronda y acciones", new Vector2(0, -24), new Vector2(730, 125)).gameObject;
        var rh = (RectTransform)hud.transform;
        rh.anchorMin = rh.anchorMax = new Vector2(.5f, 1);
        rh.pivot = new Vector2(.5f, 1);
        hud.GetComponent<Image>().raycastTarget = false;
        estado = Texto(hud.transform, "", new Vector2(0, 22), new Vector2(700, 74), 29);
        acciones = Texto(hud.transform, "", new Vector2(0, -38), new Vector2(700, 34), 23);
        tarjeta = Panel(raiz.transform, "Aviso de combate", new Vector2(0, -234), new Vector2(690, 168));
        fondo = tarjeta.GetComponent<Image>();
        fondo.raycastTarget = false;
        grupo = tarjeta.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0;
        grupo.interactable = grupo.blocksRaycasts = false;
        contorno = tarjeta.gameObject.AddComponent<Outline>();
        aviso = Texto(tarjeta, "", Vector2.zero, new Vector2(656, 146), 40);
    }

    private GameObject Modal(string nombre, Vector2 tamano, out Transform contenido)
    {
        var velo = Panel(raiz.transform, nombre + " - fondo", Vector2.zero, Vector2.zero);
        velo.anchorMin = Vector2.zero;
        velo.anchorMax = Vector2.one;
        velo.offsetMin = velo.offsetMax = Vector2.zero;
        velo.GetComponent<Image>().color = new Color(.015f, .025f, .045f, .84f);
        contenido = Panel(velo, nombre, Vector2.zero, tamano);
        return velo.gameObject;
    }

    private static RectTransform Panel(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
    {
        var obj = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = tamano;
        rect.anchoredPosition = posicion;
        obj.GetComponent<Image>().color = Azul;
        return rect;
    }

    private static TMP_Text Texto(Transform padre, string contenido, Vector2 posicion, Vector2 tamano, float fuente)
    {
        var obj = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(padre, false);
        rect.sizeDelta = tamano;
        rect.anchoredPosition = posicion;
        var texto = obj.GetComponent<TextMeshProUGUI>();
        texto.text = contenido;
        texto.fontSize = fuente;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = fuente * .72f;
        texto.fontSizeMax = fuente;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(.9f, .95f, 1f);
        texto.raycastTarget = false;
        return texto;
    }

    private static Button Boton(Transform padre, string texto, Vector2 posicion, Vector2 tamano, Action accion)
    {
        var rect = Panel(padre, texto, posicion, tamano);
        rect.GetComponent<Image>().color = new Color(.11f, .26f, .32f);
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = rect.GetComponent<Image>();
        boton.navigation = new Navigation { mode = Navigation.Mode.None };
        boton.onClick.AddListener(() => accion?.Invoke());
        Texto(rect, texto, Vector2.zero, tamano - new Vector2(16, 8), 25);
        return boton;
    }

    private static TMP_InputField CampoCodigo(Transform padre, Vector2 posicion)
    {
        var rect = Panel(padre, "Código de sala", posicion, new Vector2(302, 54));
        rect.GetComponent<Image>().color = new Color(.16f, .2f, .27f);
        var campo = rect.gameObject.AddComponent<TMP_InputField>();
        var texto = Texto(rect, "", Vector2.zero, new Vector2(276, 44), 26);
        campo.textViewport = rect;
        campo.textComponent = (TextMeshProUGUI)texto;
        campo.placeholder = Texto(rect, "Código de sala", Vector2.zero, new Vector2(276, 44), 23);
        campo.characterLimit = 12;
        campo.contentType = TMP_InputField.ContentType.Alphanumeric;
        campo.lineType = TMP_InputField.LineType.SingleLine;
        campo.richText = false;
        return campo;
    }

    private IEnumerator MostrarDurante(float duracion, bool critico)
    {
        float tiempo = 0;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float entrada = Mathf.SmoothStep(0, 1, Mathf.Clamp01(tiempo / .18f));
            grupo.alpha = Mathf.Min(entrada, Mathf.Clamp01((duracion - tiempo) / .28f));
            tarjeta.localScale = Vector3.one * Mathf.Lerp(.9f, 1, entrada) * (critico ? 1 + .025f * Mathf.Sin(tiempo * 8) : 1);
            yield return null;
        }
        grupo.alpha = 0;
        tarjeta.localScale = Vector3.one;
        rutina = null;
    }

    private void OnDestroy() { if (raiz != null) Destroy(raiz); }
}
