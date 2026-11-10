using Pokimun.Exploracion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Pokimun.Audio
{
    /// <summary>
    /// Objeto de audio persistente entre escenas (se crea solo).
    ///  - Cruzar portal: al presionar E mirando un portal (DestinoExploracion con escena destino).
    ///  - Transición de escena: sonido extradiegético al entrar a una escena nueva.
    ///  - Emisor 2D para voces/narración que no deben cortarse al cambiar de escena.
    ///  - Aplica a Wwise el volumen de música y efectos elegido en el menú de pausa.
    /// </summary>
    public sealed class AudioGlobal : MonoBehaviour
    {
        public string eventoCruzarPortal = "Play_SFX_Cruzar_Portal";
        public string eventoTransicion = "Play_SFX_Transicion_Escena";
        public float retrasoTransicion = 0.7f;

        [Header("Volumen desde el menú de pausa")]
        [Tooltip("Mismas claves de PlayerPrefs que usa PanelVolumenPausa (menú, combate y exploración)")]
        public string claveMusica = "VolumenMusica";
        public string claveEfectos = "VolumenEfectos";
        public float musicaPorDefecto = 0.35f, efectosPorDefecto = 0.85f;
        private float ultimaMusica = -1f, ultimosEfectos = -1f, proximaLectura;

        public static AudioGlobal Instancia { get; private set; }

        public static AudioGlobal Asegurar()
        {
            if (Instancia != null) return Instancia;
            var go = new GameObject("AudioGlobal (persistente)");
            DontDestroyOnLoad(go);
            Instancia = go.AddComponent<AudioGlobal>();
            return Instancia;
        }

        private void Awake()
        {
            if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
            Instancia = this;
            SincronizarVolumen();
            SceneManager.activeSceneChanged += AlCambiarEscena;
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            SceneManager.activeSceneChanged -= AlCambiarEscena;
        }

        /// <summary>
        /// Los controles de volumen del menú guardan su valor (0-1) en PlayerPrefs.
        /// Acá se pasan a Wwise como Game Parameters (0-100) que controlan el volumen de los buses:
        /// Volumen_Musica -> bus Musica; Volumen_Efectos -> buses SFX y Ambiente.
        /// </summary>
        private void SincronizarVolumen()
        {
            if (Time.unscaledTime < proximaLectura || !AkUnitySoundEngine.IsInitialized()) return;
            proximaLectura = Time.unscaledTime + 0.15f;
            float m = Mathf.Clamp01(PlayerPrefs.GetFloat(claveMusica, musicaPorDefecto));
            float e = Mathf.Clamp01(PlayerPrefs.GetFloat(claveEfectos, efectosPorDefecto));
            if (!Mathf.Approximately(m, ultimaMusica)) { ultimaMusica = m; AkUnitySoundEngine.SetRTPCValue("Volumen_Musica", m * 100f); }
            if (!Mathf.Approximately(e, ultimosEfectos)) { ultimosEfectos = e; AkUnitySoundEngine.SetRTPCValue("Volumen_Efectos", e * 100f); }
        }

        private void AlCambiarEscena(Scene a, Scene b) => Invoke(nameof(SonarTransicion), retrasoTransicion);
        private void SonarTransicion() => AudioTP.Post(eventoTransicion, gameObject);

        private float ultimoPortal;
        private void Update()
        {
            SincronizarVolumen();

            var teclado = Keyboard.current;
            if (teclado == null || !teclado.eKey.wasPressedThisFrame || Time.unscaledTime - ultimoPortal < 2f) return;
            var cam = Camera.main;
            if (cam == null) return;
            // Misma lógica que GestorExploracion: rayo al centro de la pantalla, alcance de interacción 3.3 m.
            Ray rayo = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            if (!Physics.Raycast(rayo, out var hit, 6.5f, ~(1 << 2), QueryTriggerInteraction.Ignore)) return;
            var destino = hit.collider.GetComponentInParent<DestinoExploracion>();
            var jugador = FindFirstObjectByType<JugadorExploracion>();
            if (destino == null || string.IsNullOrEmpty(destino.escenaDestino) || jugador == null) return;
            if (Vector3.Distance(jugador.transform.position, hit.point) > 3.3f || jugador.Bloqueado) return;
            ultimoPortal = Time.unscaledTime;
            AudioTP.Post(eventoCruzarPortal, gameObject);
        }
    }
}
