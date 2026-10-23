#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
using System;
using System.Collections.Concurrent;
using UnityEngine.Windows.Speech;

namespace Pokimun.Voz
{
    internal sealed class ReconocedorWindows : IReconocedorVoz
    {
        private readonly KeywordRecognizer reconocedor;
        public bool Listo => true;
        public string Error => null;

        public ReconocedorWindows(string[] palabras, ConcurrentQueue<ResultadoVoz> salida)
        {
            // Recibir también hipótesis de confianza baja, pero admitirlas solo para las variantes cortas de T.
            reconocedor = new KeywordRecognizer(palabras, ConfidenceLevel.Low);
            reconocedor.OnPhraseRecognized += args => {
                if (!AceptarFrase(args.text, args.confidence)) return;
                if (salida.Count >= 8) salida.TryDequeue(out _);
                salida.Enqueue(new ResultadoVoz {
                    texto = args.text, inicio = args.phraseStartTime.ToUniversalTime(),
                    fin = args.phraseStartTime.ToUniversalTime() + args.phraseDuration, rms = -1
                });
            };
            try { reconocedor.Start(); }
            catch { reconocedor.Dispose(); throw; }
        }

        internal static bool AceptarFrase(string texto, ConfidenceLevel confianza)
        {
            if (confianza == ConfidenceLevel.High || confianza == ConfidenceLevel.Medium) return true;
            if (confianza != ConfidenceLevel.Low) return false;
            string frase = ComandosVoz.Normalizar(texto);
            return frase == "letra te" || frase == "letra ti" || frase == "letra t";
        }

        public void Enviar(short[] pcm, DateTime origen, double finSegundos, float rms) { }

        public void Dispose()
        {
            try { if (reconocedor.IsRunning) reconocedor.Stop(); }
            finally { reconocedor.Dispose(); }
        }
    }
}
#endif
