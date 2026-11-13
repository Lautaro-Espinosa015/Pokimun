using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pokimun.Exploracion
{
    public sealed class InterfazExploracion : MonoBehaviour
    {
        public string PistaInteraccion { get; set; }
        private GestorExploracion gestor;
        private RectTransform seguro;
        private GameObject modal, controles, panelPista, panelMensaje;
        private TextMeshProUGUI estado, pista, mensaje;
        private float hastaMensaje;
        private string ultimoAviso;
        public bool AyudaAbierta { get; private set; }
        private MapaExploracion mapa;
        public bool MapaAbierto => mapa != null && mapa.Abierto;
        private static Color Fondo => TemaExploracion.Marron;
        private static readonly Color Boton = new Color(.12f, .28f, .27f, .96f);

        public void Crear(GestorExploracion valor)
        {
            gestor = valor;
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var go = new GameObject("Interfaz exploración", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 110;
            var escala = go.GetComponent<CanvasScaler>(); escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1280, 720); escala.matchWidthOrHeight = .5f;
            seguro = Rect("Zona segura", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            estado = Texto(seguro, "", new Vector2(.5f, 1), new Vector2(0, -50), new Vector2(950, 80), 24);
            TemaExploracion.Texto(estado, true);
            var fondoPista = Rect("Ayuda de interacción", seguro, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 190), new Vector2(760, 60));
            fondoPista.gameObject.AddComponent<Image>().color = Fondo;
            TemaExploracion.Panel(fondoPista.GetComponent<Image>());
            fondoPista.GetComponent<Image>().raycastTarget = false; panelPista = fondoPista.gameObject;
            pista = Texto(fondoPista, "", new Vector2(.5f, .5f), Vector2.zero, new Vector2(740, 55), 22);
            Texto(seguro, "·", new Vector2(.5f, .5f), Vector2.zero, new Vector2(25, 25), 26);
            var tarjeta = Rect("Mensaje de aventura", seguro, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,90), new Vector2(760,65));
            TemaExploracion.Panel(tarjeta.gameObject.AddComponent<Image>());tarjeta.GetComponent<Image>().raycastTarget=false;panelMensaje=tarjeta.gameObject;
            mensaje = Texto(tarjeta, "", new Vector2(.5f,.5f), Vector2.zero, new Vector2(720,55), 21);
            controles = new GameObject("Controles", typeof(RectTransform)); controles.transform.SetParent(seguro, false);
            var rc = (RectTransform)controles.transform; rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one; rc.offsetMin = rc.offsetMax = Vector2.zero;
            if (TemaExploracion.EsAndroid)
            {
                gestor.jugador.movimientoTactil = ZonaTactil(false);
                gestor.jugador.giroTactil = ZonaTactil(true);
                CrearBoton(controles.transform, "Interactuar", new Vector2(1, 0), new Vector2(-125, 72), new Vector2(205, 70), gestor.Interactuar);
                CrearBoton(controles.transform, "Correr / andar", new Vector2(0, 0), new Vector2(115, 215), new Vector2(195, 55), gestor.jugador.AlternarCarrera);
                // Los botones se crean después de las zonas táctiles para capturar sus propios toques.
                CrearBoton(controles.transform, "Pausa", new Vector2(1, 1), new Vector2(-75, -35), new Vector2(130, 48), () => gestor.EstablecerPausa(true));
                CrearBoton(controles.transform, "Ayuda", new Vector2(0, 1), new Vector2(100, -35), new Vector2(160, 48), gestor.AlternarAyuda);
                if (gestor.busqueda)
                    CrearBoton(controles.transform, "Mapa", new Vector2(1, 1), new Vector2(-225, -35), new Vector2(140, 48), gestor.AlternarMapa);
            }
            Notificar(gestor.busqueda ? "La aventura empieza al salir del cobertizo. Abrí el mapa para orientarte." : "Bienvenido al santuario. Explorá sus jardines o elegí un portal.");
        }
        private EntradaTactilExploracion ZonaTactil(bool giro)
        {
            var zona = Rect(giro ? "Mirar (derecha)" : "Mover (izquierda)", controles.transform,
                new Vector2(giro ? .5f : 0, 0), new Vector2(giro ? 1 : .5f, 1), Vector2.zero, Vector2.zero);
            zona.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            var entrada = zona.gameObject.AddComponent<EntradaTactilExploracion>(); entrada.giro = giro;
            if (!giro)
            {
                var baseJoy = Rect("Joystick", zona, Vector2.zero, Vector2.zero, new Vector2(105, 105), new Vector2(155, 155));
                baseJoy.gameObject.AddComponent<Image>().color = new Color(.8f, 1, .9f, .12f);
                var pomo = Rect("Palanca", baseJoy, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60));
                var imagen = pomo.gameObject.AddComponent<Image>(); imagen.color = new Color(.8f, 1, .9f, .45f); imagen.raycastTarget = false;
                baseJoy.GetComponent<Image>().raycastTarget = false; entrada.indicador = pomo;
            }
            return entrada;
        }
        public void Actualizar()
        {
            Rect r = Screen.safeArea;
            seguro.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            seguro.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            estado.text = gestor.busqueda ? gestor.Partida.Iniciada ? "BUSCANDO A IGNIS  ·  " + Tiempo(gestor.Partida.Tiempo) :
                "BUSCANDO A IGNIS\nUna nueva aventura te espera" : "SANTUARIO POKIMUN\nExplorá, descubrí y elegí tu aventura";
            pista.text = PistaInteraccion;
            pista.color = ColorDestino(PistaInteraccion);
            panelPista.SetActive(!gestor.Pausado && !string.IsNullOrEmpty(PistaInteraccion));
            string aviso = gestor.jugador.Aviso;
            if(aviso != ultimoAviso) { ultimoAviso=aviso;if(!string.IsNullOrEmpty(aviso))Notificar(aviso); }
            panelMensaje.SetActive(!gestor.Pausado && (Time.unscaledTime < hastaMensaje || gestor.jugador.Navegando) && !string.IsNullOrEmpty(mensaje.text));
        }
        public static string Tiempo(double segundos) => TimeSpan.FromSeconds(segundos).ToString(@"mm\:ss\.ff");
        public void MostrarMapa(bool visible)
        {
            if (visible)
            {
                if (mapa == null) mapa = gameObject.AddComponent<MapaExploracion>();
                mapa.Abrir(gestor, seguro); panelPista.SetActive(false);
                panelMensaje.SetActive(false);
            }
            else if (mapa != null) mapa.Cerrar();
            controles.SetActive(!visible);
        }
        private static Color ColorDestino(string texto)
        {
            string nombre=(texto??"").ToLowerInvariant();
            if(nombre.Contains("hydros"))return new Color(.6f,.3f,1);
            if(nombre.Contains("jardin azul")||nombre.Contains("jardín azul"))return new Color(.35f,.72f,1);
            return new Color(.9f,.95f,1);
        }
        public void Notificar(string texto) { mensaje.text = texto; mensaje.color=ColorDestino(texto); hastaMensaje = Time.unscaledTime + 5; if(panelMensaje!=null)panelMensaje.SetActive(!gestor.Pausado); }
        public void MostrarAyuda(bool visible)
        {
            if(!visible)
            {
                if(!AyudaAbierta)return;AyudaAbierta=false;
                if(modal!=null){Destroy(modal);modal=null;}controles.SetActive(true);return;
            }
            AyudaAbierta=true;controles.SetActive(false);panelPista.SetActive(false);panelMensaje.SetActive(false);
            Transform panel=Modal("EXPLORÁ A TU MANERA",590,800);
            string movimiento=TemaExploracion.EsAndroid?
                "MOVIMIENTO\nJoystick izquierdo para caminar. Deslizá a la derecha para mirar.\nUsá los botones para correr e interactuar.":
                "MOVIMIENTO\nWASD: caminar · Shift: correr · E: interactuar\nH: ayuda · Esc: pausa"+(gestor.busqueda?" · M: mapa":"");
            Texto(panel,movimiento,new Vector2(.5f,.5f),new Vector2(0,105),new Vector2(710,125),24);
            Texto(panel,"TU VOZ TAMBIÉN GUÍA LA AVENTURA\n«Avanzar», «retroceder», «detener», «caminar» o «correr».\n«Mirar izquierda», «mirar derecha» o «mirar atrás».\n«Ir a [zona]» te lleva hasta allí y te orienta al llegar.",new Vector2(.5f,.5f),new Vector2(0,-50),new Vector2(710,165),23);
            Texto(panel,gestor.busqueda?"Decí «interactuar» para investigar.\nSi ves a Ignis de cerca, decí «encontrar a Ignis».":"Probá: «ir al jardín azul», «ir al jardín rojo»,\n«ir al agua» o «ir a la lava».",new Vector2(.5f,.5f),new Vector2(0,-165),new Vector2(710,65),21);
            CrearBoton(panel,"Volver a la aventura",new Vector2(.5f,.5f),new Vector2(0,-240),new Vector2(420,55),()=>gestor.EstablecerAyuda(false));
        }
        public void MostrarPausa(bool visible)
        {
            if (modal != null) Destroy(modal);
            controles.SetActive(!visible);
            if (!visible) return;
            panelMensaje.SetActive(false);panelPista.SetActive(false);
            Transform panel = Modal("PAUSA", 640);
            PanelVolumenPausa.Crear(panel, new Vector2(0, 120), true);
            CrearBoton(panel, "Reanudar", new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(390, 55), () => gestor.EstablecerPausa(false));
            if (gestor.busqueda)
                CrearBoton(panel, "Volver al lobby", new Vector2(.5f, .5f), new Vector2(0, -85), new Vector2(390, 55), () => gestor.Cargar("Lobby"));
            float yMenu = gestor.busqueda ? -150 : -85;
            CrearBoton(panel, "Menú principal", new Vector2(.5f, .5f), new Vector2(0, yMenu), new Vector2(390, 55), () => gestor.Cargar("Menus"));
            CrearBoton(panel, "Salir del juego", new Vector2(.5f, .5f), new Vector2(0, yMenu - 65), new Vector2(390, 55), Salir);
        }
        public void MostrarResultado()
        {
            controles.SetActive(false);
            panelMensaje.SetActive(false);panelPista.SetActive(false);
            Transform panel = Modal("¡ENCONTRASTE A IGNIS!");
            Texto(panel, "Tiempo: " + Tiempo(gestor.Partida.Tiempo) + "\nMejor tiempo: " + Tiempo(PlayerPrefs.GetFloat("BusquedaIgnis.MejorTiempo")),
                new Vector2(.5f, .5f), new Vector2(0, 90), new Vector2(500, 90), 27);
            CrearBoton(panel, "Buscar de nuevo", new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(390, 55), () => gestor.Cargar("BusquedaIgnis"));
            CrearBoton(panel, "Volver al lobby", new Vector2(.5f, .5f), new Vector2(0, -65), new Vector2(390, 55), () => gestor.Cargar("Lobby"));
            CrearBoton(panel, "Menú principal", new Vector2(.5f, .5f), new Vector2(0, -130), new Vector2(390, 55), () => gestor.Cargar("Menus"));
            CrearBoton(panel, "Salir del juego", new Vector2(.5f, .5f), new Vector2(0, -195), new Vector2(390, 55), Salir);
        }
        internal static void Salir()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        private Transform Modal(string titulo, float altura = 540, float ancho = 620)
        {
            var fondo = Rect("Modal", seguro, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            modal = fondo.gameObject; modal.AddComponent<Image>().color = new Color(0, .01f, .02f, .88f);
            var panel = Rect("Panel", fondo, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(ancho, altura));
            TemaExploracion.Panel(panel.gameObject.AddComponent<Image>());
            var tituloTexto=Texto(panel, titulo, new Vector2(.5f, .5f), new Vector2(0, altura / 2 - 65), new Vector2(ancho-40, 75), 32);TemaExploracion.Texto(tituloTexto,true);
            return panel;
        }
        private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max, Vector2 posicion, Vector2 tamano)
        {
            var rt = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(padre, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.anchoredPosition = posicion; rt.sizeDelta = tamano;
            return rt;
        }
        private static TextMeshProUGUI Texto(Transform padre, string texto, Vector2 ancla, Vector2 posicion, Vector2 tamano, float fuente)
        {
            var rt = Rect("Texto", padre, ancla, ancla, posicion, tamano);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.text = texto; t.fontSize = fuente;
            t.color = new Color(.9f, .96f, .88f); t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            TemaExploracion.Texto(t);
            return t;
        }
        private static void CrearBoton(Transform padre, string texto, Vector2 ancla, Vector2 posicion, Vector2 tamano, UnityAction accion)
        {
            var rt = Rect(texto, padre, ancla, ancla, posicion, tamano);
            rt.gameObject.AddComponent<Image>().color = Boton;
            var boton=rt.gameObject.AddComponent<Button>();boton.onClick.AddListener(accion);TemaExploracion.Boton(boton);
            Texto(rt, texto, new Vector2(.5f, .5f), Vector2.zero, tamano, 23);
        }
    }
}
