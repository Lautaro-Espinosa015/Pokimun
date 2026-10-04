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
    [Header("Sonidos de Resultados")]
    public AudioClip audioVictoria;
    public AudioClip audioDerrota;
    public AudioClip audioEmpate;
    public AudioClip musicaEscenario;
    
    private AudioSource audioSourceUI;
    private AudioSource audioSourceEscenario;

    private GameObject raiz, hud, sala, final;
    private RectTransform tarjeta;
    private CanvasGroup grupo;
    private Image fondo;
    private Outline contorno;
    private TMP_Text aviso, estado, acciones, estadoSala;
    private TMP_InputField codigo;
    private Button solo, crear, unir, cancelar;
    private Coroutine rutina;
    private static readonly Color FondoPanel = new Color(0.35f, 0.22f, 0.10f, 0.85f); // Marrón translúcido
    private static readonly Color Verde = new Color(.4f, .9f, .75f);
    private static readonly Color Oro = new Color(1f, .78f, .3f);
    private static readonly Color Rojo = new Color(1f, .55f, .48f);
    private static readonly Color Morado = new Color(.6f, .3f, 1f);

    public Sprite botonSprite;
    public TMP_FontAsset fuentePersonalizada;

    private void Awake()
    {
        audioSourceUI = gameObject.AddComponent<AudioSource>();
        audioSourceUI.playOnAwake = false;
        audioSourceUI.spatialBlend = 0f; // Sonido 2D

        audioSourceEscenario = gameObject.AddComponent<AudioSource>();
        audioSourceEscenario.playOnAwake = false;
        audioSourceEscenario.spatialBlend = 0f; // Sonido 2D
        audioSourceEscenario.loop = true;
    }

    private void Start()
    {
        if (musicaEscenario != null)
        {
            audioSourceEscenario.clip = musicaEscenario;
            audioSourceEscenario.Play();
        }
    }

    private void Update()
    {
        if (audioSourceEscenario != null)
        {
            audioSourceEscenario.volume = PlayerPrefs.GetFloat("VolumenMusica", 0.5f);
        }
        if (audioSourceUI != null)
        {
            audioSourceUI.volume = PlayerPrefs.GetFloat("VolumenEfectos", 0.5f);
        }
    }

    public void ActualizarMusicaBatalla(int vidaJugador, int vidaMaxJugador, int vidaEnemigo, int vidaMaxEnemigo)
    {
        if (audioSourceEscenario == null || !audioSourceEscenario.isPlaying) return;

        float pctJugador = (float)vidaJugador / vidaMaxJugador;
        float pctEnemigo = (float)vidaEnemigo / vidaMaxEnemigo;

        // Acelerar la música un 15% si alguno de los dos tiene 30% o menos de vida
        if (pctJugador <= 0.3f || pctEnemigo <= 0.3f)
        {
            audioSourceEscenario.pitch = 1.15f;
        }
        else
        {
            audioSourceEscenario.pitch = 1.0f;
        }
    }

    public void ActualizarEstado(int ronda, int maxRondas, string personaje, string mensaje, string plan)
    {
        PrepararInterfaz();
        hud.SetActive(true);
        estado.text = $"<b>RONDA {ronda} / {maxRondas}</b>  ·  {personaje}\n<size=80%>{mensaje}</size>";
        acciones.text = plan;
    }

    public void MostrarAccion(AccionTurno accion, string personaje, int numero, float duracion, bool rival)
    {
        string detalle = accion.tipo == AccionCombate.Defender ? "Protege esta pareja de acciones" : $"{accion.jugada}";
        string nombreAtaque = ReglasCombate.Nombre(accion, rival) + (accion.critico ? " CRÍTICO" : "");
        
        Color critColor = personaje.ToLower().Contains("ignis") ? Oro : Morado;
        Color baseColor = accion.critico ? critColor : accion.tipo == AccionCombate.Defender ? Color.cyan : accion.tipo == AccionCombate.Curar ? Verde : (rival ? Rojo : Verde);
        
        Mostrar($"{personaje}  ·  ACCIÓN {numero}/2", nombreAtaque, detalle, duracion, baseColor, accion.critico);
    }

    public void MostrarResultado(ResultadoIntercambio r, string atacante, string objetivo, float duracion, bool rival)
    {
        string titulo = $"ACCIÓN {r.numero}/2  ·  {atacante}";
        string principal = "", detalle = "";
        
        Color critColor = atacante.ToLower().Contains("ignis") ? Oro : Morado;
        Color color = r.accion.critico ? critColor : r.bloqueoAplicado ? Color.cyan : r.accion.tipo == AccionCombate.Curar ? Verde : r.accion.tipo == AccionCombate.Defender ? Color.cyan : (rival ? Rojo : Verde);

        if (r.accion.tipo == AccionCombate.Atacar)
        {
            principal = r.bloqueoAplicado ? "ESCUDO · DAÑO REDUCIDO" : r.accion.critico ? "¡GOLPE CRÍTICO!" : $"{atacante} ATACA";
            detalle = $"{objetivo}: −{r.danioAplicado} PS";
            if (r.bloqueoAplicado && r.accion.critico) detalle += "  ·  Crítico";
            if (r.bloqueoAplicado) detalle += "  ·  (Escudo)";
        }
        else if (r.accion.tipo == AccionCombate.Curar)
        {
            principal = "SANACIÓN";
            detalle = $"{atacante}: +{r.curaAplicada} PS";
            color = Verde;
        }
        else if (r.accion.tipo == AccionCombate.Defender)
        {
            principal = "DEFENSA ACTIVADA";
            detalle = $"{atacante} bloquea los ataques del próximo turno.";
            color = Color.cyan;
        }

        Mostrar(titulo, principal, detalle, duracion, color, r.accion.critico);
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
        fondo.color = FondoPanel;
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
        var tmpEstado = (TextMeshProUGUI)estadoSala;
        tmpEstado.font = null; // Usar LiberationSans para ver mayúsculas/minúsculas reales
        tmpEstado.richText = false;
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

    public void MostrarFin(string titulo, string detalle, Action reiniciar, Action menu, Action salir, bool victoria, bool empate)
    {
        PrepararInterfaz();
        OcultarAviso();
        OcultarSala();
        if (final != null) Destroy(final);

        if (audioSourceEscenario != null)
        {
            audioSourceEscenario.Stop();
        }

        if (audioSourceUI != null)
        {
            AudioClip clip = empate ? audioEmpate : (victoria ? audioVictoria : audioDerrota);
            if (clip == null && empate) clip = audioDerrota; // Fallback
            if (clip != null)
            {
                audioSourceUI.clip = clip;
                audioSourceUI.Play();
            }
        }
        final = Modal("Resultado de la partida", new Vector2(660, 480), out Transform panel);
        Texto(panel, titulo, new Vector2(0, 160), new Vector2(590, 75), 44).color = Oro;
        Texto(panel, detalle, new Vector2(0, 50), new Vector2(570, 125), 26);
        
        if (reiniciar != null)
        {
            Boton(panel, "Volver a jugar", new Vector2(-145, -70), new Vector2(260, 57), reiniciar);
            Boton(panel, "Menú principal", new Vector2(145, -70), new Vector2(260, 57), menu);
            Boton(panel, "Salir del juego", new Vector2(0, -150), new Vector2(260, 57), salir);
        }
        else
        {
            Boton(panel, "Menú principal", new Vector2(0, -70), new Vector2(260, 57), menu);
            Boton(panel, "Salir del juego", new Vector2(0, -150), new Vector2(260, 57), salir);
        }
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

    private RectTransform Panel(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
    {
        var obj = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = tamano;
        rect.anchoredPosition = posicion;
        obj.GetComponent<Image>().color = FondoPanel;
        return rect;
    }

    private TMP_FontAsset BuscarFuenteGlobal()
    {
        if (fuentePersonalizada != null) return fuentePersonalizada;
        var pm = FindFirstObjectByType<PauseManager>();
        if (pm != null && pm.pauseMenu != null)
        {
            var txt = pm.pauseMenu.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) return txt.font;
        }
        return null;
    }

    private Sprite BuscarSpriteBotonGlobal()
    {
        if (botonSprite != null) return botonSprite;
        var pm = FindFirstObjectByType<PauseManager>();
        if (pm != null && pm.pauseMenu != null)
        {
            var btn = pm.pauseMenu.GetComponentInChildren<Button>(true);
            if (btn != null && btn.targetGraphic is Image img) return img.sprite;
        }
        return null;
    }

    private TMP_Text Texto(Transform padre, string contenido, Vector2 posicion, Vector2 tamano, float fuente)
    {
        var obj = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(padre, false);
        rect.sizeDelta = tamano;
        rect.anchoredPosition = posicion;
        var texto = obj.GetComponent<TextMeshProUGUI>();
        texto.text = contenido;
        texto.fontSize = fuente;
        
        TMP_FontAsset f = BuscarFuenteGlobal();
        if (f != null) texto.font = f;
        
        texto.enableAutoSizing = true;
        texto.fontSizeMin = fuente * .72f;
        texto.fontSizeMax = fuente;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(.9f, .95f, 1f);
        texto.raycastTarget = false;
        return texto;
    }

    private Button Boton(Transform padre, string texto, Vector2 posicion, Vector2 tamano, Action accion)
    {
        var rect = Panel(padre, texto, posicion, tamano);
        var img = rect.GetComponent<Image>();
        
        Sprite s = BuscarSpriteBotonGlobal();
        if (s != null)
        {
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(.11f, .26f, .32f);
        }
        
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = img;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };
        boton.onClick.AddListener(() => accion?.Invoke());
        rect.gameObject.AddComponent<BotonAnimado>();
        Texto(rect, texto, Vector2.zero, tamano - new Vector2(16, 8), 25);
        return boton;
    }

    private TMP_InputField CampoCodigo(Transform padre, Vector2 posicion)
    {
        var rect = Panel(padre, "Código de sala", posicion, new Vector2(302, 54));
        rect.GetComponent<Image>().color = new Color(.16f, .2f, .27f);
        var campo = rect.gameObject.AddComponent<TMP_InputField>();
        var texto = Texto(rect, "", Vector2.zero, new Vector2(276, 44), 26);
        campo.textViewport = rect;
        
        // Forzar fuente estándar para diferenciar mayúsculas y minúsculas
        var tmpTexto = (TextMeshProUGUI)texto;
        tmpTexto.font = null; // Usará LiberationSans por defecto
        campo.textComponent = tmpTexto;
        
        var placeholderTxt = Texto(rect, "Código de sala", Vector2.zero, new Vector2(276, 44), 23);
        var tmpPlaceholder = (TextMeshProUGUI)placeholderTxt;
        tmpPlaceholder.font = null;
        campo.placeholder = tmpPlaceholder;
        
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
