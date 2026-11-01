using System;

namespace Pokimun.Exploracion
{
    /// <summary>Reloj de una búsqueda. El llamador determina el encuadre, sin pruebas de oclusión.</summary>
    public sealed class ReglasEscondite
    {
        public const int CantidadEscondites = 9;
        public const int EsconditesPorPartida = 3;
        public readonly int[] Seleccionados;
        public int Actual { get; private set; }
        public double Tiempo { get; private set; }
        public bool Iniciada { get; private set; }
        public bool Finalizada { get; private set; }
        private double desdeCambio, desdePista;
        private readonly Random azar;

        public ReglasEscondite(int semilla)
        {
            azar = new Random(semilla);
            var candidatos = new int[CantidadEscondites];
            for (int i = 0; i < candidatos.Length; i++) candidatos[i] = i;
            for (int i = 0; i < EsconditesPorPartida; i++)
            {
                int j = azar.Next(i, candidatos.Length);
                int previo = candidatos[i]; candidatos[i] = candidatos[j]; candidatos[j] = previo;
            }
            Seleccionados = new[] { candidatos[0], candidatos[1], candidatos[2] };
            Actual = Seleccionados[0];
        }

        public void Iniciar() { if (!Finalizada) Iniciada = true; }
        public void Finalizar() { if (Iniciada) Finalizada = true; }

        // Las oportunidades vencidas no se acumulan. Un destino visible nunca recibe una aparición.
        public bool Avanzar(double delta, bool pausada, Func<int, bool> enEncuadre, out bool pista)
        {
            pista = false;
            if (!Iniciada || Finalizada || pausada || delta <= 0 || double.IsNaN(delta) || double.IsInfinity(delta)) return false;
            Tiempo += delta; desdeCambio += delta; desdePista += delta;
            bool fuera = !enEncuadre(Actual);
            bool cambio = false;
            if (desdeCambio >= 45 && fuera)
            {
                int[] opciones = new int[2]; int cantidad = 0;
                foreach (int candidato in Seleccionados)
                    if (candidato != Actual && !enEncuadre(candidato)) opciones[cantidad++] = candidato;
                if (cantidad > 0)
                {
                    Actual = opciones[azar.Next(cantidad)]; desdeCambio = 0; cambio = true;
                }
            }
            if (desdePista >= 15 && !enEncuadre(Actual)) { pista = true; desdePista = 0; }
            return cambio;
        }
    }
}
