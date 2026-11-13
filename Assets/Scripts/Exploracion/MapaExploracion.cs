using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Pokimun.Exploracion
{
    /// <summary>Vista cenital del escenario con destinos públicos, sin datos del rival.</summary>
    public sealed class MapaExploracion : MonoBehaviour
    {
        public bool Abierto => raiz != null && raiz.activeSelf;
        private GameObject raiz;
        private GestorExploracion gestor;
        private RectTransform plano, ruta;
        private DestinoExploracion elegido;
        private TextMeshProUGUI detalle;
        private Button ir;
        private const float Ancho = 384, Alto = 450;
        private static Sprite circulo;
        private static Color Fondo => TemaExploracion.Marron;
        private static string Nombre(string s) => s.Replace("caido", "caído").Replace("jardin", "jardín").Replace("tocon", "tocón");

        public void Abrir(GestorExploracion g, Transform padre)
        {
            Cerrar(); gestor = g; elegido = null;
            var pantalla = Rect("Mapa del bosque", padre, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            raiz = pantalla.gameObject; pantalla.gameObject.AddComponent<Image>().color = new Color(0, .015f, .025f, .95f);
            var panel = Rect("Panel del mapa", pantalla, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(1160, 650));
            panel.gameObject.AddComponent<Image>().color = Fondo;
            TemaExploracion.Panel(panel.GetComponent<Image>());
            TemaExploracion.Texto(Texto(panel, "MAPA DEL BOSQUE", new Vector2(-190, 285), new Vector2(680, 48), 30),true);
            Texto(panel, "¿Adónde vamos? Elegí un destino; la aventura te espera.", new Vector2(0, 242), new Vector2(1080, 35), 21);
            Boton(panel, TemaExploracion.EsAndroid ? "Cerrar" : "Cerrar · M / Esc", new Vector2(435, 285), new Vector2(235, 45), () => g.EstablecerMapa(false));
            plano = Rect("Bosque · norte arriba", panel, Vector2.one * .5f, Vector2.one * .5f, new Vector2(-300, -8), new Vector2(Ancho, Alto));
            var imagenMapa = plano.gameObject.AddComponent<RawImage>();
            imagenMapa.texture = Resources.Load<Texture2D>("Mapas/BosqueCenital"); imagenMapa.raycastTarget = false;
            Texto(panel, "NORTE ↑", new Vector2(-300, 222), new Vector2(180, 24), 18);
            ruta = Rect("Ruta prevista", plano, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(Ancho, Alto));
            var destinos = g.destinos.Where(d => d.nombreVoz != "lobby").ToArray();
            int numero = 0;
            for (int i = 0; i < destinos.Length; i++)
            {
                var destino = destinos[i]; bool cobertizo = destino.nombreVoz == "cobertizo";
                string codigo = cobertizo ? "C" : (++numero).ToString();
                Vector3 referencia = destino.mirada != null ? destino.mirada.position : destino.transform.position;
                var marca = Boton(plano, codigo, Mapear(referencia), new Vector2(38, 38), () => Elegir(destino));
                marca.image.sprite = Circulo();
                marca.image.color = cobertizo ? new Color(.5f, .27f, .10f) : new Color(.065f, .2f, .14f, .94f);
                Boton(panel, codigo + " · " + Nombre(destino.nombreVoz), new Vector2(260, 190 - i * 40), new Vector2(510, 35), () => Elegir(destino));
            }
            var jugador = Texto(plano, "▲", Mapear(g.jugador.transform.position), new Vector2(30, 30), 28);
            jugador.color = new Color(.25f, .9f, 1); jugador.rectTransform.localRotation = Quaternion.Euler(0, 0, -g.jugador.transform.eulerAngles.y);
            Texto(panel, "▲ Tu posición y orientación\nC · Cobertizo   |   Números · Zonas de búsqueda", new Vector2(-300, -280), new Vector2(475, 65), 18);
            detalle = Texto(panel, "Elegí tu próximo destino.\nIgnis guarda su escondite en secreto.", new Vector2(260, -226), new Vector2(510, 60), 20);
            ir = Boton(panel, "Ir a la zona seleccionada", new Vector2(260, -286), new Vector2(510, 45), Viajar);
            ir.interactable = false;
        }
        public void Elegir(DestinoExploracion destino)
        {
            if (!Abierto || !gestor.destinos.Contains(destino) || destino.nombreVoz == "lobby") return;
            elegido = destino;
            foreach (Transform hijo in ruta) { hijo.gameObject.SetActive(false); Destroy(hijo.gameObject); }
            var camino = new NavMeshPath();
            bool accesible = NavMesh.SamplePosition(gestor.jugador.transform.position, out var inicio, 3, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(destino.PuntoLlegada, out var fin, 1, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(inicio.position, fin.position, NavMesh.AllAreas, camino) && camino.status == NavMeshPathStatus.PathComplete;
            if (accesible)
                for (int i = 1; i < camino.corners.Length; i++) Banda(ruta, Mapear(camino.corners[i - 1]), Mapear(camino.corners[i]), new Color(.8f, .78f, .35f), 4);
            detalle.text = accesible ? "También podés decir: «ir a " + Nombre(destino.nombreVoz) + "».\nAl llegar, investigá con «interactuar»." : "Ese camino no está disponible. Probemos otra zona.";
            ir.GetComponentInChildren<TextMeshProUGUI>().text = "Ir a " + Nombre(destino.nombreVoz); ir.interactable = accesible;
        }
        public void Viajar()
        {
            if (!Abierto || elegido == null || !ir.interactable) return;
            var destino = elegido; gestor.EstablecerMapa(false);
            if (!gestor.IrADestino(destino)) gestor.interfaz.Notificar(gestor.jugador.Aviso);
        }
        public void Cerrar()
        {
            if (raiz != null) { raiz.SetActive(false); Destroy(raiz); raiz = null; }
            elegido = null;
        }
        private void OnDestroy() => Cerrar();
        private static Vector2 Mapear(Vector3 p) => new Vector2((Mathf.Clamp01((p.x + 17) / 34) - .5f) * Ancho, (Mathf.Clamp01((p.z + 18) / 40) - .5f) * Alto);
        private static Sprite Circulo()
        {
            if (circulo != null) return circulo;
            var textura = new Texture2D(48, 48, TextureFormat.RGBA32, false) { name = "Marcador circular", hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                textura.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(23.5f - Vector2.Distance(new Vector2(x, y), new Vector2(23.5f, 23.5f)))));
            textura.Apply(); circulo = Sprite.Create(textura, new Rect(0, 0, 48, 48), Vector2.one * .5f);
            circulo.hideFlags = HideFlags.HideAndDontSave; return circulo;
        }
        private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max, Vector2 p, Vector2 tamano)
        {
            var rt = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(padre, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.anchoredPosition = p; rt.sizeDelta = tamano; return rt;
        }
        private static TextMeshProUGUI Texto(Transform padre, string texto, Vector2 p, Vector2 tamano, float fuente)
        {
            var rt = Rect("Texto", padre, Vector2.one * .5f, Vector2.one * .5f, p, tamano);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.text = texto; t.fontSize = fuente; t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(.94f, .95f, .85f); t.raycastTarget = false;TemaExploracion.Texto(t); return t;
        }
        private static Button Boton(Transform padre, string nombre, Vector2 p, Vector2 tamano, UnityEngine.Events.UnityAction accion)
        {
            var rt = Rect(nombre, padre, Vector2.one * .5f, Vector2.one * .5f, p, tamano);
            rt.gameObject.AddComponent<Image>().color = new Color(.17f, .31f, .28f); var b = rt.gameObject.AddComponent<Button>(); b.onClick.AddListener(accion);TemaExploracion.Boton(b);
            Texto(rt, nombre, Vector2.zero, tamano - new Vector2(8, 2), 20); return b;
        }
        private static void Banda(Transform padre, Vector2 a, Vector2 b, Color color, float ancho)
        {
            var rt = Rect("Tramo", padre, Vector2.one * .5f, Vector2.one * .5f, (a + b) / 2, new Vector2(Vector2.Distance(a, b), ancho));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            var imagen = rt.gameObject.AddComponent<Image>(); imagen.color = color; imagen.raycastTarget = false;
        }
    }
}
