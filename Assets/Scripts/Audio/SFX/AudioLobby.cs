using Pokimun.Exploracion;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// SFX de la escena Lobby. Se agrega un solo componente y configura todo por nombre de objeto:
    /// emisores 3D en loop, pasos, roce con las flores, abejas y locución de bienvenida.
    /// </summary>
    public sealed class AudioLobby : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Emisor
        {
            [Tooltip("Nombre exacto del objeto en la jerarquía")] public string objeto;
            public string evento;
        }

        [Header("Emisores 3D en loop")]
        public Emisor[] emisores =
        {
            new Emisor { objeto = "estanque de lava", evento = "Play_SFX_Amb_Lava" },
            new Emisor { objeto = "estanque de agua", evento = "Play_SFX_Amb_Estanque" },
            new Emisor { objeto = "Portal combate", evento = "Play_SFX_Amb_Portal" },
            new Emisor { objeto = "Portal busqueda", evento = "Play_SFX_Amb_Portal" },
        };

        [Header("Pasos")]
        public string pasoPorDefecto = "Play_SFX_Paso_Piedra";
        public string pasoTierra = "Play_SFX_Paso_Tierra_Lobby";

        [Header("Jardines")]
        [Tooltip("Roce suave al pasar junto a las flores de las jardineras")]
        public string eventoFlores = "Play_SFX_Flores";
        public string[] nombresFlor = { "Flor" };
        public float radioFlor = 1.1f;
        public string eventoAbejas = "Play_SFX_Abejas";
        [Tooltip("Zumbido de abejas (3D, loop) sobre el jardín azul")]
        public Vector3[] posicionesAbejas = { new Vector3(-11.8f, 0.7f, -3f) };

        [Header("Bienvenida del Lobby")]
        [Tooltip("Evento de Wwise de la locución de bienvenida. Vacío = no suena nada.")]
        public string bienvenida = "Play_VO_Bienvenida_Lobby";
        public float duracionBienvenida = 6.02f;
        [Tooltip("Segundos de espera al entrar, para no pisar la transición ni el fade-in de la música")]
        public float retrasoBienvenida = 3.5f;
        [Tooltip("Solo la primera vez que se entra al Lobby en la sesión")]
        public bool bienvenidaUnaVez = true;
        private static bool yaBienvenida;

        private void Start()
        {
            AudioGlobal.Asegurar();
            foreach (var e in emisores)
            {
                var go = GameObject.Find(e.objeto);
                if (go == null) { Debug.LogWarning($"[AudioLobby] No se encontró '{e.objeto}'"); continue; }
                var em = go.AddComponent<EmisorSonidoLoop>(); em.evento = e.evento;
            }
            var jugador = FindFirstObjectByType<JugadorExploracion>();
            if (jugador != null && jugador.GetComponent<PasosJugador>() == null)
                jugador.gameObject.AddComponent<PasosJugador>().eventoPorDefecto = pasoPorDefecto;

            var pasoTrasero = GameObject.Find("Paso de tierra trasero");
            if (pasoTrasero != null && pasoTrasero.TryGetComponent<Renderer>(out var superficie))
            {
                var zona = pasoTrasero.AddComponent<ZonaSuperficie>();
                zona.superficie = superficie;
                zona.eventoPaso = pasoTierra;
            }

            var roce = gameObject.AddComponent<RoceVegetacion>();
            roce.jugador = jugador != null ? jugador.transform : null;
            roce.grupos = new[] { new RoceVegetacion.Grupo { nombre = "Flores", prefijos = nombresFlor, evento = eventoFlores, radio = radioFlor, intervaloMinimo = 0.6f } };
            for (int i = 0; i < posicionesAbejas.Length; i++)
                RoceVegetacion.EmisorEn("Abejas " + (i + 1), posicionesAbejas[i], eventoAbejas, transform);

            if (!string.IsNullOrEmpty(bienvenida) && (!bienvenidaUnaVez || !yaBienvenida))
                Invoke(nameof(DarBienvenida), retrasoBienvenida);
        }

        private void DarBienvenida()
        {
            if (AudioTP.Voz(bienvenida, AudioGlobal.Asegurar().gameObject, duracionBienvenida, forzar: true))
                yaBienvenida = true;
        }
    }
}
