#if UNITY_EDITOR_LINUX || ((UNITY_STANDALONE_LINUX || UNITY_ANDROID) && !UNITY_EDITOR)
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Pokimun.Voz
{
    internal sealed class ReconocedorVosk : IReconocedorVoz
    {
        private struct Bloque
        {
            public short[] pcm;
            public DateTime origen;
            public double fin;
            public float rms;
        }

        [Serializable] private class Palabra { public double start; public double end; }
        [Serializable] private class Resultado { public string text; public Palabra[] result; }
        private readonly BlockingCollection<Bloque> entrada = new BlockingCollection<Bloque>(30);
        private readonly CancellationTokenSource cancelacion = new CancellationTokenSource();
        private volatile bool listo;
        private volatile string error;
        public bool Listo => listo;
        public string Error => error;

        public ReconocedorVosk(string ruta, string gramatica, ConcurrentQueue<ResultadoVoz> salida, float ventana)
        {
            _ = Task.Run(() => Procesar(ruta, gramatica, salida, ventana));
        }

        public void Enviar(short[] pcm, DateTime origen, double finSegundos, float rms)
        {
            if (!entrada.TryAdd(new Bloque { pcm = pcm, origen = origen, fin = finSegundos, rms = rms }))
                throw new InvalidOperationException("Vosk no alcanza a procesar el audio en este equipo.");
        }

        private void Procesar(string ruta, string gramatica, ConcurrentQueue<ResultadoVoz> salida, float ventana)
        {
            try
            {
                Vosk.Vosk.SetLogLevel(-1);
                using (var modelo = new Vosk.Model(ruta))
                {
                    if (Vosk.Model.getCPtr(modelo).Handle == IntPtr.Zero)
                        throw new InvalidOperationException("No se pudo cargar el modelo de idioma.");
                    using (var reconocedor = new Vosk.VoskRecognizer(modelo, 16000, gramatica))
                    {
                        if (Vosk.VoskRecognizer.getCPtr(reconocedor).Handle == IntPtr.Zero)
                            throw new InvalidOperationException("No se pudo crear el reconocedor Vosk.");
                        reconocedor.SetWords(true);
                        var historial = new HistorialNivelVoz(ventana);
                        listo = true;
                        foreach (Bloque bloque in entrada.GetConsumingEnumerable(cancelacion.Token))
                        {
                            historial.Agregar(bloque.fin, bloque.rms);
                            if (!reconocedor.AcceptWaveform(bloque.pcm, bloque.pcm.Length)) continue;
                            var resultado = JsonUtility.FromJson<Resultado>(reconocedor.Result());
                            if (resultado == null || string.IsNullOrWhiteSpace(resultado.text) ||
                                resultado.result == null || resultado.result.Length == 0) continue;
                            double inicio = resultado.result[0].start;
                            double fin = resultado.result[resultado.result.Length - 1].end;
                            if (salida.Count >= 8) salida.TryDequeue(out _);
                            salida.Enqueue(new ResultadoVoz {
                                texto = resultado.text, inicio = bloque.origen.AddSeconds(inicio),
                                fin = bloque.origen.AddSeconds(fin), rms = historial.Obtener(fin)
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { error = ex.Message; }
            finally { entrada.Dispose(); cancelacion.Dispose(); }
        }

        public void Dispose()
        {
            try { cancelacion.Cancel(); }
            catch (ObjectDisposedException) { }
            // El trabajador libera sus recursos al salir; no bloquea el hilo de Unity.
        }
    }
}
#endif
