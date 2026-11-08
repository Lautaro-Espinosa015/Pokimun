using System.Collections.Generic;
using Pokimun.Exploracion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pokimun.Audio
{
    /// <summary>
    /// SFX y voces de la escena BusquedaIgnis:
    ///  - Portal de regreso (loop 3D) y pasos: tierra (bosque), madera (cobertizo), charco (afuera de la puerta).
    ///  - Puerta del cobertizo al salir, caja de madera con E, follaje al atravesar arbustos.
    ///  - Voces de Ignis (3D, desde su escondite) y de Hydros (2D), y el hallazgo.
    /// </summary>
    public sealed class AudioBusqueda : MonoBehaviour
    {
        public GestorExploracion gestor;

        [Header("Ambiente / pasos")]
        public string portalObjeto = "lobby";
        public string eventoPortal = "Play_SFX_Amb_Portal";
        public string pasoPorDefecto = "Play_SFX_Paso_Tierra";
        public string pasoMadera = "Play_SFX_Paso_Madera";
        public string pasoCharco = "Play_SFX_Paso_Charco";
        [Tooltip("Ubicación del charco (justo afuera de la puerta del cobertizo)")]
        public Vector3 posicionCharco = new Vector3(0.6f, 0.02f, -5.6f);
        public float radioCharco = 1.3f;

        [Header("Interacciones 3D")]
        public string eventoPuerta = "Play_SFX_Puerta_Cobertizo";
        public string eventoCaja = "Play_SFX_Caja_Madera";
        public string eventoArbusto = "Play_SFX_Arbusto";
        public string eventoEncontrar = "Play_SFX_Encontrar_Ignis";
        public float radioArbusto = 1.4f;

        [Header("Voces de Ignis (3D)")]
        public string[] vozQueFueEso = { "Play_VO_Ignis_QueFueEso_01", "Play_VO_Ignis_QueFueEso_02" };
        public string vozCreoQueSeFue = "Play_VO_Ignis_CreoQueSeFue";
        public string vozRespiracion = "Play_VO_Ignis_Respiracion";
        public string vozRisa = "Play_VO_Ignis_Risa";
        public string vozMeEncontro = "Play_VO_Ignis_MeEncontro";
        public float distanciaCerca = 10f, distanciaLejos = 16f, distanciaMuyCerca = 5f;

        [Header("Voces de Hydros (2D)")]
        public string[] vozBuscando = { "Play_VO_Hydros_DondeEstas", "Play_VO_Hydros_SeQueEstasAqui" };
        public string vozTeEncontre = "Play_VO_Hydros_TeEncontre";
        public Vector2 intervaloBuscando = new Vector2(22f, 38f);

        private readonly List<Transform> arbustos = new List<Transform>();
        private readonly HashSet<Transform> dentroArbusto = new HashSet<Transform>();
        private readonly List<Transform> cajas = new List<Transform>();
        private GameObject puerta;
        private bool estabaIniciada, cerca, avisoCerca;
        private float proximaRespiracion, proximaBusqueda, ultimoArbusto, ultimaCaja, ultimoQueFue, ultimoSeFue;

        private void Start()
        {
            AudioGlobal.Asegurar();
            if (gestor == null) gestor = FindFirstObjectByType<GestorExploracion>();

            var portal = GameObject.Find(portalObjeto);
            if (portal != null) portal.AddComponent<EmisorSonidoLoop>().evento = eventoPortal;

            var jugador = gestor != null ? gestor.jugador : FindFirstObjectByType<JugadorExploracion>();
            if (jugador != null && jugador.GetComponent<PasosJugador>() == null)
                jugador.gameObject.AddComponent<PasosJugador>().eventoPorDefecto = pasoPorDefecto;

            if (gestor != null && gestor.interiorCobertizo != null)
            {
                var madera = new GameObject("Zona pasos madera (cobertizo)").AddComponent<ZonaSuperficie>();
                madera.eventoPaso = pasoMadera; madera.limite = gestor.interiorCobertizo; madera.prioridad = 1;
            }
            var charco = new GameObject("Charco").AddComponent<ZonaSuperficie>();
            charco.transform.position = posicionCharco;
            charco.eventoPaso = pasoCharco; charco.radio = radioCharco; charco.prioridad = 2; charco.dibujarCharco = true;

            // Objetos de la escena buscados por nombre
            Vector3 suma = Vector3.zero; int puertas = 0;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name.StartsWith("Arbusto Escondite")) arbustos.Add(t);
                else if (t.name == "Caja de madera") cajas.Add(t);
                else if (t.name == "Marco de puerta") { suma += t.position; puertas++; }
            }
            puerta = new GameObject("Emisor puerta cobertizo");
            puerta.transform.position = puertas > 0 ? suma / puertas : posicionCharco;

            if (gestor != null)
            {
                gestor.Hallazgo += AlEncontrar;
                gestor.PistaDisponible += AlPista;
                gestor.CambioEscondite += AlCambiarEscondite;
            }
            proximaBusqueda = Time.time + Random.Range(intervaloBuscando.x, intervaloBuscando.y);
        }

        private void OnDestroy()
        {
            if (gestor == null) return;
            gestor.Hallazgo -= AlEncontrar; gestor.PistaDisponible -= AlPista; gestor.CambioEscondite -= AlCambiarEscondite;
        }

        private void Update()
        {
            if (gestor == null || gestor.jugador == null) return;
            Vector3 pj = gestor.jugador.transform.position;
            var partida = gestor.Partida;

            // Puerta: al salir del cobertizo empieza la búsqueda
            bool iniciada = partida != null && partida.Iniciada;
            if (iniciada && !estabaIniciada) AudioTP.Post(eventoPuerta, puerta);
            estabaIniciada = iniciada;

            // Arbustos: al atravesarlos
            foreach (var a in arbustos)
            {
                Vector3 d = a.position - pj; d.y = 0;
                bool dentro = d.sqrMagnitude < radioArbusto * radioArbusto;
                if (dentro && !dentroArbusto.Contains(a))
                {
                    dentroArbusto.Add(a);
                    if (Time.time - ultimoArbusto > 0.8f) { ultimoArbusto = Time.time; AudioTP.Post(eventoArbusto, a.gameObject); }
                }
                else if (!dentro) dentroArbusto.Remove(a);
            }

            // Caja de madera: E mirándola de cerca
            var teclado = Keyboard.current;
            if (teclado != null && teclado.eKey.wasPressedThisFrame && !gestor.jugador.Bloqueado && Time.time - ultimaCaja > 1.5f)
            {
                var cam = gestor.jugador.camara;
                foreach (var c in cajas)
                {
                    Vector3 hacia = c.position - cam.transform.position;
                    if (hacia.magnitude < 3f && Vector3.Angle(cam.transform.forward, hacia) < 30f)
                    { ultimaCaja = Time.time; AudioTP.Post(eventoCaja, c.gameObject); break; }
                }
            }

            if (partida == null || !partida.Iniciada || partida.Finalizada || gestor.ignis == null || gestor.Pausado) return;

            // Voces según la distancia a Ignis
            GameObject ignis = gestor.ignis.gameObject;
            float dist = Vector3.Distance(pj, gestor.ignis.position);
            if (!cerca && dist < distanciaCerca)
            {
                cerca = true;
                if (Time.time - ultimoQueFue > 15f && AudioTP.Voz(vozQueFueEso[Random.Range(0, vozQueFueEso.Length)], ignis, 1.3f))
                { ultimoQueFue = Time.time; avisoCerca = true; }
                proximaRespiracion = Time.time + Random.Range(4f, 7f);
            }
            else if (cerca && dist > distanciaLejos)
            {
                cerca = false;
                if (avisoCerca && Time.time - ultimoSeFue > 20f && AudioTP.Voz(vozCreoQueSeFue, ignis, 2.5f)) ultimoSeFue = Time.time;
                avisoCerca = false;
            }
            if (cerca && dist < distanciaMuyCerca && Time.time >= proximaRespiracion)
            {
                AudioTP.Voz(vozRespiracion, ignis, 1.8f);
                proximaRespiracion = Time.time + Random.Range(7f, 11f);
            }
            // Hydros llama a Ignis de vez en cuando (solo si está lejos)
            if (!cerca && Time.time >= proximaBusqueda)
            {
                AudioTP.Voz(vozBuscando[Random.Range(0, vozBuscando.Length)], AudioGlobal.Asegurar().gameObject, 1.7f);
                proximaBusqueda = Time.time + Random.Range(intervaloBuscando.x, intervaloBuscando.y);
            }
        }

        private void AlPista()
        {
            if (gestor.ignis != null) AudioTP.Voz(vozRisa, gestor.ignis.gameObject, 1.7f);
        }

        private void AlCambiarEscondite() { cerca = false; avisoCerca = false; }

        private void AlEncontrar()
        {
            if (gestor.ignis == null) return;
            AudioTP.Post(eventoEncontrar, gestor.ignis.gameObject);
            Invoke(nameof(DecirTeEncontre), 0.7f);
            Invoke(nameof(DecirMeEncontro), 2.3f);
        }
        private void DecirTeEncontre() => AudioTP.Voz(vozTeEncontre, AudioGlobal.Asegurar().gameObject, 1.2f, forzar: true);
        private void DecirMeEncontro() { if (gestor != null && gestor.ignis != null) AudioTP.Voz(vozMeEncontro, gestor.ignis.gameObject, 1.2f, forzar: true); }
    }
}
