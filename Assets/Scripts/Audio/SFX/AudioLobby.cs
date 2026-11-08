using Pokimun.Exploracion;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// SFX de la escena Lobby. Se agrega un solo componente y configura todo por nombre de objeto:
    /// emisores 3D en loop (lava, estanque, portales), pasos sobre piedra y la narración de Hydros.
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

        [Header("Narración (voz en off de Hydros)")]
        public string narracion = "Play_VO_Narracion_Hydros";
        public float duracionNarracion = 12.2f;
        [Tooltip("Segundos de espera al entrar, para no pisar la transición ni el fade-in de la música")]
        public float retrasoNarracion = 3.5f;
        [Tooltip("Solo la primera vez que se entra al Lobby en la sesión")]
        public bool narrarUnaVez = true;
        private static bool yaNarrado;

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

            if (!narrarUnaVez || !yaNarrado) Invoke(nameof(Narrar), retrasoNarracion);
        }

        private void Narrar()
        {
            yaNarrado = true;
            AudioTP.Voz(narracion, AudioGlobal.Asegurar().gameObject, duracionNarracion, forzar: true);
        }
    }
}
