using System;

namespace Pokimun.Voz
{
    internal struct ResultadoVoz
    {
        public string texto;
        public DateTime inicio, fin;
        public float rms;
    }

    // Solo el gestor accede al micrófono y despacha comandos en el hilo principal.
    internal interface IReconocedorVoz : IDisposable
    {
        bool Listo { get; }
        string Error { get; }
        void Enviar(short[] pcm, DateTime origen, double finSegundos, float rms);
    }

    internal static class PlataformaVoz
    {
        // En el editor se usa el sistema anfitrión, independientemente del destino del build.
        public static bool UsaVosk => UnityEngine.Application.platform == UnityEngine.RuntimePlatform.LinuxEditor ||
            UnityEngine.Application.platform == UnityEngine.RuntimePlatform.LinuxPlayer ||
            UnityEngine.Application.platform == UnityEngine.RuntimePlatform.Android;

        public static IReconocedorVoz Crear(string[] palabras, string gramatica, string ruta,
            System.Collections.Concurrent.ConcurrentQueue<ResultadoVoz> salida, float ventana)
        {
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
            return new ReconocedorWindows(palabras, salida);
#elif UNITY_EDITOR_LINUX || ((UNITY_STANDALONE_LINUX || UNITY_ANDROID) && !UNITY_EDITOR)
            return new ReconocedorVosk(ruta, gramatica, salida, ventana);
#else
            throw new PlatformNotSupportedException("Voz disponible en Windows, Linux y Android.");
#endif
        }
    }
}
