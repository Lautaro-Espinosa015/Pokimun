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
    /// </summary>
    public sealed class AudioGlobal : MonoBehaviour
    {
        public string eventoCruzarPortal = "Play_SFX_Cruzar_Portal";
        public string eventoTransicion = "Play_SFX_Transicion_Escena";
        public float retrasoTransicion = 0.7f;

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
            SceneManager.activeSceneChanged += AlCambiarEscena;
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            SceneManager.activeSceneChanged -= AlCambiarEscena;
        }

        private void AlCambiarEscena(Scene a, Scene b) => Invoke(nameof(SonarTransicion), retrasoTransicion);
        private void SonarTransicion() => AudioTP.Post(eventoTransicion, gameObject);

        private float ultimoPortal;
        private void Update()
        {
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
