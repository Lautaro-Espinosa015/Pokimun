using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Pieza 1 - Remezcla vertical (escena Lobby).
    /// Reproduce la música del santuario y envía a Wwise cuán cerca está el jugador
    /// de cada zona (0 = lejos, 100 = encima). En Wwise, cada Game Parameter controla
    /// el volumen de una capa (Music Track) del segmento.
    /// Los fundidos de entrada/salida entre escenas los maneja MusicaDeEscena.
    /// </summary>
    public sealed class MusicaSantuario : MusicaDeEscena
    {
        [System.Serializable]
        public sealed class Zona
        {
            [Tooltip("Nombre solo para el Inspector, ej: Ignis")] public string nombre;
            [Tooltip("Objeto de la escena: Estatua Ignis, estanque de agua, Portal...")] public Transform centro;
            [Tooltip("Game Parameter de Wwise que controla esta capa")] public AK.Wwise.RTPC parametro;
            [Tooltip("A esta distancia (metros) la capa ya está al máximo")] public float radioInterior = 3f;
            [Tooltip("A partir de esta distancia la capa queda en 0")] public float radioExterior = 15f;
            [HideInInspector] public float valorActual;
        }

        [Header("Jugador / oyente")]
        [Tooltip("Si queda vacío se usa la cámara principal (donde está el AkAudioListener)")]
        public Transform oyente;

        [Header("Capas que dependen de la cercanía")]
        public Zona[] zonas;

        [Tooltip("Suavizado del cambio de capas (más alto = más rápido)")]
        public float suavizado = 2f;

        protected override void Start()
        {
            if (oyente == null && Camera.main != null) oyente = Camera.main.transform;
            foreach (var z in zonas) EnviarValor(z, 0f, true);
            base.Start();
        }

        private void Update()
        {
            if (Saliendo || oyente == null) return;
            foreach (var z in zonas)
            {
                if (z.centro == null) continue;
                float d = Vector3.Distance(oyente.position, z.centro.position);
                // 100 dentro del radio interior, 0 fuera del exterior, transición lineal en el medio.
                float objetivo = 100f * (1f - Mathf.InverseLerp(z.radioInterior, z.radioExterior, d));
                EnviarValor(z, Mathf.MoveTowards(z.valorActual, objetivo, suavizado * 100f * Time.unscaledDeltaTime), false);
            }
        }

        private void EnviarValor(Zona z, float valor, bool forzar)
        {
            if (!forzar && Mathf.Approximately(valor, z.valorActual)) return;
            z.valorActual = valor;
            z.parametro?.SetGlobalValue(valor);
        }

#if UNITY_EDITOR
        // Dibuja los radios en la vista Scene para ajustarlos fácilmente.
        private void OnDrawGizmosSelected()
        {
            if (zonas == null) return;
            foreach (var z in zonas)
            {
                if (z.centro == null) continue;
                Gizmos.color = new Color(1f, .6f, .1f, .8f); Gizmos.DrawWireSphere(z.centro.position, z.radioInterior);
                Gizmos.color = new Color(.2f, .6f, 1f, .5f); Gizmos.DrawWireSphere(z.centro.position, z.radioExterior);
            }
        }
#endif
    }
}
