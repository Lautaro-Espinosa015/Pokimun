using Pokimun.Exploracion;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Pieza 2 - Resecuenciación horizontal (escena BusquedaIgnis).
    /// Cambia el State "Busqueda" de Wwise según la situación del jugador:
    ///   Explorando -> lejos de Ignis
    ///   Tension    -> cerca del escondite actual de Ignis
    ///   Hallazgo   -> el jugador encontró a Ignis
    /// El Music Switch Container de Wwise decide qué sección suena y cómo transiciona.
    /// Los fundidos de entrada/salida entre escenas los maneja MusicaDeEscena.
    /// </summary>
    public sealed class MusicaBusqueda : MusicaDeEscena
    {
        [Header("Referencias del juego")]
        public GestorExploracion gestor;

        [Header("States de Wwise")]
        public AK.Wwise.State estadoExplorando; // Busqueda / Explorando
        public AK.Wwise.State estadoTension;    // Busqueda / Tension
        public AK.Wwise.State estadoHallazgo;   // Busqueda / Hallazgo
        [Tooltip("Opcional: sonido que se dispara cuando el juego ofrece una pista")]
        public AK.Wwise.Event eventoPista;

        [Header("Distancias (metros)")]
        [Tooltip("Más cerca que esto de Ignis -> Tension")] public float entrarTension = 10f;
        [Tooltip("Más lejos que esto -> vuelve a Explorando (mayor que la anterior para evitar parpadeos)")]
        public float salirTension = 14f;

        private enum Situacion { Ninguna, Explorando, Tension, Hallazgo }
        private Situacion actual = Situacion.Ninguna;

        protected override void Start()
        {
            if (gestor == null) gestor = FindFirstObjectByType<GestorExploracion>();
            Cambiar(Situacion.Explorando);
            if (gestor != null)
            {
                gestor.Hallazgo += AlEncontrar;
                gestor.PistaDisponible += AlRecibirPista;
            }
            base.Start();
        }

        private void Update()
        {
            if (Saliendo || gestor == null || gestor.jugador == null || gestor.ignis == null) return;
            if (actual == Situacion.Hallazgo) return;
            // Antes de salir del cobertizo la búsqueda no empezó: siempre Explorando.
            if (gestor.Partida == null || !gestor.Partida.Iniciada) { Cambiar(Situacion.Explorando); return; }

            float d = Vector3.Distance(gestor.jugador.transform.position, gestor.ignis.position);
            if (actual != Situacion.Tension && d < entrarTension) Cambiar(Situacion.Tension);
            else if (actual == Situacion.Tension && d > salirTension) Cambiar(Situacion.Explorando);
        }

        private void AlEncontrar() => Cambiar(Situacion.Hallazgo);
        private void AlRecibirPista()
        {
            if (eventoPista != null && eventoPista.IsValid())
                eventoPista.Post(gestor != null && gestor.ignis != null ? gestor.ignis.gameObject : gameObject);
        }

        private void Cambiar(Situacion nueva)
        {
            if (nueva == actual) return;
            actual = nueva;
            switch (nueva)
            {
                case Situacion.Explorando: estadoExplorando?.SetValue(); break;
                case Situacion.Tension: estadoTension?.SetValue(); break;
                case Situacion.Hallazgo: estadoHallazgo?.SetValue(); break;
            }
        }

        protected override void AlSalir() => Desuscribir();

        protected override void OnDestroy()
        {
            Desuscribir();
            base.OnDestroy();
        }

        private void Desuscribir()
        {
            if (gestor == null) return;
            gestor.Hallazgo -= AlEncontrar;
            gestor.PistaDisponible -= AlRecibirPista;
        }
    }
}
