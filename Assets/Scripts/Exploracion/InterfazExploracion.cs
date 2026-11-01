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
        private GameObject modal, controles;
        private TextMeshProUGUI estado, pista, mensaje;
        private float hastaMensaje;
        private static readonly Color Fondo = new Color(.025f, .055f, .065f, .94f);
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
            pista = Texto(seguro, "", new Vector2(.5f, .5f), new Vector2(0, -65), new Vector2(800, 70), 22);
            Texto(seguro, "·", new Vector2(.5f, .5f), Vector2.zero, new Vector2(25, 25), 26);
            mensaje = Texto(seguro, "", new Vector2(.5f, 0), new Vector2(0, 110), new Vector2(850, 65), 20);
            controles = new GameObject("Controles", typeof(RectTransform)); controles.transform.SetParent(seguro, false);
            var rc = (RectTransform)controles.transform; rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one; rc.offsetMin = rc.offsetMax = Vector2.zero;
            if (Application.isMobilePlatform)
            {
                gestor.jugador.movimientoTactil = ZonaTactil(false);
                gestor.jugador.giroTactil = ZonaTactil(true);
                CrearBoton(controles.transform, "Interactuar", new Vector2(1, 0), new Vector2(-125, 72), new Vector2(205, 70), gestor.Interactuar);
            }
            else Texto(seguro, "WASD · Mover  |  Mouse · Mirar  |  E · Interactuar  |  Esc · Pausa\nVoz: ir a [destino], detener, mirar izquierda/derecha/atrás, interactuar",
                new Vector2(.5f, 0), new Vector2(0, 35), new Vector2(1050, 65), 17);
            // Los botones se crean después de las zonas táctiles para que capturen sus propios toques.
            CrearBoton(seguro, "Pausa", new Vector2(1, 1), new Vector2(-75, -35), new Vector2(130, 48), () => gestor.EstablecerPausa(true));
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
                "BUSCANDO A IGNIS\nSalí del cobertizo para comenzar" : "SANTUARIO POKIMUN\nElegí combate o búsqueda e interactuá con su portal";
            pista.text = PistaInteraccion;
            if (Time.unscaledTime > hastaMensaje) mensaje.text = gestor.jugador.Navegando ? gestor.jugador.Aviso : "";
        }
        public static string Tiempo(double segundos) => TimeSpan.FromSeconds(segundos).ToString(@"mm\:ss\.ff");
        public void Notificar(string texto) { mensaje.text = texto; hastaMensaje = Time.unscaledTime + 4; }
        public void MostrarPausa(bool visible)
        {
            if (modal != null) Destroy(modal);
            controles.SetActive(!visible);
            if (!visible) return;
            Transform panel = Modal("PAUSA");
            CrearBoton(panel, "Reanudar", new Vector2(.5f, .5f), new Vector2(0, 75), new Vector2(390, 55), () => gestor.EstablecerPausa(false));
            CrearBoton(panel, "Volver al lobby", new Vector2(.5f, .5f), new Vector2(0, 5), new Vector2(390, 55), () => gestor.Cargar("Lobby"));
            CrearBoton(panel, "Menú principal", new Vector2(.5f, .5f), new Vector2(0, -65), new Vector2(390, 55), () => gestor.Cargar("Menus"));
            CrearBoton(panel, "Salir del juego", new Vector2(.5f, .5f), new Vector2(0, -135), new Vector2(390, 55), Salir);
        }
        public void MostrarResultado()
        {
            controles.SetActive(false);
            Transform panel = Modal("¡ENCONTRASTE A IGNIS!");
            Texto(panel, "Tiempo: " + Tiempo(gestor.Partida.Tiempo) + "\nMejor tiempo: " + Tiempo(PlayerPrefs.GetFloat("BusquedaIgnis.MejorTiempo")),
                new Vector2(.5f, .5f), new Vector2(0, 90), new Vector2(500, 90), 27);
            CrearBoton(panel, "Buscar de nuevo", new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(390, 55), () => gestor.Cargar("BusquedaIgnis"));
            CrearBoton(panel, "Volver al lobby", new Vector2(.5f, .5f), new Vector2(0, -65), new Vector2(390, 55), () => gestor.Cargar("Lobby"));
            CrearBoton(panel, "Menú principal", new Vector2(.5f, .5f), new Vector2(0, -130), new Vector2(390, 55), () => gestor.Cargar("Menus"));
            CrearBoton(panel, "Salir del juego", new Vector2(.5f, .5f), new Vector2(0, -195), new Vector2(390, 55), Salir);
        }
        private static void Salir()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        private Transform Modal(string titulo)
        {
            var fondo = Rect("Modal", seguro, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            modal = fondo.gameObject; modal.AddComponent<Image>().color = new Color(0, .01f, .02f, .88f);
            var panel = Rect("Panel", fondo, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(620, 540));
            panel.gameObject.AddComponent<Image>().color = Fondo;
            Texto(panel, titulo, new Vector2(.5f, .5f), new Vector2(0, 205), new Vector2(580, 75), 32);
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
            return t;
        }
        private static void CrearBoton(Transform padre, string texto, Vector2 ancla, Vector2 posicion, Vector2 tamano, UnityAction accion)
        {
            var rt = Rect(texto, padre, ancla, ancla, posicion, tamano);
            rt.gameObject.AddComponent<Image>().color = Boton;
            rt.gameObject.AddComponent<Button>().onClick.AddListener(accion);
            Texto(rt, texto, new Vector2(.5f, .5f), Vector2.zero, tamano, 23);
        }
    }
}
