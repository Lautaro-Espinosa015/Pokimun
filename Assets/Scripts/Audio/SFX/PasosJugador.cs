using Pokimun.Exploracion;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Pasos del jugador: dispara un paso cada cierta distancia realmente recorrida.
    /// El sonido depende de la superficie (ZonaSuperficie) o del valor por defecto de la escena.
    /// Se mide el desplazamiento real del objeto (no la velocidad del CharacterController,
    /// que queda "congelada" cuando el controlador se desactiva, por ejemplo al encontrar a Ignis).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PasosJugador : MonoBehaviour
    {
        public string eventoPorDefecto = "Play_SFX_Paso_Piedra";
        [Tooltip("Metros entre pasos caminando")] public float distanciaCaminando = 1.5f;
        [Tooltip("Metros entre pasos corriendo")] public float distanciaCorriendo = 2.0f;
        [Tooltip("Velocidad a partir de la cual se considera correr (m/s)")] public float velocidadCorrer = 5f;

        private CharacterController motor;
        private JugadorExploracion jugador;
        private Vector3 posicionAnterior;
        private float acumulado;

        private void Awake()
        {
            motor = GetComponent<CharacterController>();
            jugador = GetComponent<JugadorExploracion>();
            posicionAnterior = transform.position;
        }

        private void Update()
        {
            Vector3 delta = transform.position - posicionAnterior; delta.y = 0;
            posicionAnterior = transform.position;

            bool quieto = motor == null || !motor.enabled || (jugador != null && jugador.Bloqueado) || Time.deltaTime <= 0f;
            float rapidez = quieto ? 0f : delta.magnitude / Time.deltaTime;
            if (rapidez < 0.4f || !motor.isGrounded) { acumulado = Mathf.Min(acumulado, 0.6f); return; }

            acumulado += delta.magnitude;
            float paso = rapidez >= velocidadCorrer ? distanciaCorriendo : distanciaCaminando;
            if (acumulado >= paso)
            {
                acumulado = 0f;
                AudioTP.Post(ZonaSuperficie.EventoEn(transform.position, eventoPorDefecto), gameObject);
            }
        }
    }
}
