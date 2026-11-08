using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>Emisor 3D en loop (lava, estanque, portales). Se detiene con fundido al desactivarse.</summary>
    public sealed class EmisorSonidoLoop : MonoBehaviour
    {
        public string evento;
        [Tooltip("Fundido al detenerse (ms), por ejemplo al cambiar de escena")]
        public int fundidoSalidaMs = 600;
        private bool sonando;

        private void Start() => Iniciar();
        private void OnEnable() { if (sonando == false && didStart) Iniciar(); }

        private void Iniciar()
        {
            if (AudioTP.Post(evento, gameObject) != 0) sonando = true;
        }

        private void OnDisable()
        {
            if (!sonando) return;
            AudioTP.Detener(evento, gameObject, fundidoSalidaMs);
            sonando = false;
        }
    }
}
