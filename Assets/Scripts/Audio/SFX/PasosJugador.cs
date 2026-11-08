using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Pasos del jugador: dispara un paso cada cierta distancia recorrida.
    /// El sonido depende de la superficie (ZonaSuperficie) o del valor por defecto de la escena.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PasosJugador : MonoBehaviour
    {
        public string eventoPorDefecto = "Play_SFX_Paso_Piedra";
        [Tooltip("Metros entre pasos caminando")] public float distanciaCaminando = 1.5f;
        [Tooltip("Metros entre pasos corriendo")] public float distanciaCorriendo = 2.0f;
        [Tooltip("Velocidad a partir de la cual se considera correr (m/s)")] public float velocidadCorrer = 5f;

        private CharacterController motor;
        private float acumulado;

        private void Awake() => motor = GetComponent<CharacterController>();

        private void Update()
        {
            if (motor == null || !motor.enabled) return;
            Vector3 v = motor.velocity; v.y = 0;
            float rapidez = v.magnitude;
            if (rapidez < 0.4f || !motor.isGrounded) { acumulado = Mathf.Min(acumulado, 0.6f); return; }
            acumulado += rapidez * Time.deltaTime;
            float paso = rapidez >= velocidadCorrer ? distanciaCorriendo : distanciaCaminando;
            if (acumulado >= paso)
            {
                acumulado = 0f;
                AudioTP.Post(ZonaSuperficie.EventoEn(transform.position, eventoPorDefecto), gameObject);
            }
        }
    }
}
