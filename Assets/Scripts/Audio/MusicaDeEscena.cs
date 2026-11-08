using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pokimun.Audio
{
    /// <summary>
    /// Base para la música de cada escena. Se encarga de las transiciones:
    ///  - Al empezar la escena, la música entra con un fundido (fade-in).
    ///  - Al cambiar de escena, el objeto sobrevive al cambio, baja el volumen
    ///    hasta 0 (fade-out), detiene el evento y recién ahí se destruye.
    ///  - Si "Esperar Salida Anterior" está activo, la música nueva espera a que
    ///    la anterior termine de apagarse antes de empezar a subir.
    /// </summary>
    public abstract class MusicaDeEscena : MonoBehaviour
    {
        [Header("Wwise")]
        public AK.Wwise.Event playMusica;

        [Header("Transición entre escenas (segundos)")]
        [Tooltip("Tiempo que tarda la música en subir desde 0 al entrar a la escena")]
        public float fundidoEntrada = 2f;
        [Tooltip("Tiempo que tarda la música en bajar a 0 al salir de la escena")]
        public float fundidoSalida = 2f;
        [Tooltip("La música nueva espera a que la anterior termine de apagarse")]
        public bool esperarSalidaAnterior = true;

        // Momento (tiempo real) en que termina de apagarse la última música que salió.
        private static float finSalidaAnterior;

        private Scene escenaPropia;
        protected bool Saliendo { get; private set; }

        protected virtual void Start()
        {
            escenaPropia = gameObject.scene;
            // Sobrevive al cambio de escena para poder hacer el fade-out completo.
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += AlCambiarEscena;
            StartCoroutine(Entrar());
        }

        private IEnumerator Entrar()
        {
            // Un frame de espera: deja que la música de la escena anterior registre su salida.
            yield return null;
            if (esperarSalidaAnterior)
            {
                float espera = finSalidaAnterior - Time.unscaledTime;
                if (espera > 0f) yield return new WaitForSecondsRealtime(espera);
            }
            if (Saliendo || playMusica == null || !playMusica.IsValid()) yield break;

            playMusica.Post(gameObject);
            if (fundidoEntrada > 0f)
            {
                // Truco estándar de Wwise: pausar al instante y reanudar con transición = fade-in.
                playMusica.ExecuteAction(gameObject, AkActionOnEventType.AkActionOnEventType_Pause, 0,
                    AkCurveInterpolation.AkCurveInterpolation_Linear);
                playMusica.ExecuteAction(gameObject, AkActionOnEventType.AkActionOnEventType_Resume,
                    Mathf.RoundToInt(fundidoEntrada * 1000f), AkCurveInterpolation.AkCurveInterpolation_Sine);
            }
            AlEmpezarMusica();
        }

        private void AlCambiarEscena(Scene anterior, Scene nueva)
        {
            if (Saliendo || nueva == escenaPropia) return;
            Saliendo = true;
            AlSalir();
            if (playMusica != null && playMusica.IsValid())
            {
                // Stop con transición = fade-out hasta 0 y luego se detiene.
                playMusica.ExecuteAction(gameObject, AkActionOnEventType.AkActionOnEventType_Stop,
                    Mathf.RoundToInt(fundidoSalida * 1000f), AkCurveInterpolation.AkCurveInterpolation_Sine);
            }
            finSalidaAnterior = Time.unscaledTime + fundidoSalida;
            StartCoroutine(DestruirLuego(fundidoSalida + 0.5f));
        }

        private IEnumerator DestruirLuego(float segundos)
        {
            yield return new WaitForSecondsRealtime(segundos);
            Destroy(gameObject);
        }

        /// <summary>Se llama justo después de postear la música.</summary>
        protected virtual void AlEmpezarMusica() { }

        /// <summary>Se llama al comenzar el fade-out (cambio de escena).</summary>
        protected virtual void AlSalir() { }

        protected virtual void OnDestroy()
        {
            SceneManager.activeSceneChanged -= AlCambiarEscena;
        }
    }
}
