using System;
using System.Collections.Generic;

namespace Pokimun.Voz
{
    public static class AudioVoz
    {
        /// <summary>Convierte frames intercalados de Unity a PCM16 mono y devuelve su RMS.</summary>
        public static float Convertir(float[] origen, int frames, int canales, short[] destino)
        {
            if (canales < 1 || frames < 0 || origen == null || destino == null ||
                (long)frames * canales > origen.Length || frames > destino.Length)
                throw new ArgumentException("Buffers de audio o número de canales incorrectos.");
            double cuadrados = 0;
            for (int frame = 0; frame < frames; frame++)
            {
                double suma = 0;
                for (int canal = 0; canal < canales; canal++)
                {
                    float muestra = origen[frame * canales + canal];
                    if (!float.IsNaN(muestra) && !float.IsInfinity(muestra)) suma += muestra;
                }
                double mono = Math.Max(-1, Math.Min(1, suma / canales));
                destino[frame] = (short)Math.Round(mono * (mono < 0 ? 32768 : 32767));
                cuadrados += mono * mono;
            }
            return frames == 0 ? 0 : (float)Math.Sqrt(cuadrados / frames);
        }
    }

    public sealed class HistorialNivelVoz
    {
        private struct Medicion { public double segundos; public float rms; }
        private readonly Queue<Medicion> mediciones = new Queue<Medicion>();
        private readonly double ventana;
        public HistorialNivelVoz(double ventanaSegundos) { ventana = Math.Max(.1, ventanaSegundos); }

        public void Agregar(double segundos, float rms)
        {
            mediciones.Enqueue(new Medicion { segundos = segundos, rms = rms });
            while (mediciones.Count > 0 && segundos - mediciones.Peek().segundos > ventana + 3)
                mediciones.Dequeue();
        }

        public float Obtener(double finSegundos)
        {
            float maximo = 0;
            foreach (Medicion medicion in mediciones)
                if (medicion.segundos >= finSegundos - ventana && medicion.segundos <= finSegundos + .15)
                    maximo = Math.Max(maximo, medicion.rms);
            return maximo;
        }
    }
}
