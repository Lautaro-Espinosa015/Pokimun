#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
#define VOZ_WINDOWS
#elif UNITY_EDITOR_LINUX || (UNITY_STANDALONE_LINUX && !UNITY_EDITOR)
#define VOZ_LINUX
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pokimun.Voz;
using UnityEngine;
#if VOZ_WINDOWS
using UnityEngine.Windows.Speech;
#endif

/// <summary>Proveedor de voz para menú y combate. Solo despacha órdenes desde el hilo principal.</summary>
[DisallowMultipleComponent]
public class GestorVozMultiplataforma : MonoBehaviour
{
    public bool UsaVosk
    {
        get
        {
#if VOZ_LINUX
            return true;
#else
            return false;
#endif
        }
    }

    public struct ComandoReconocido
    {
        public string texto;
        public DateTime inicioUtc;
        public float nivelVoz;
    }

    private struct Deteccion
    {
        public string texto;
        public DateTime inicio, fin;
        public float rms;
    }

    private const int Frecuencia = 16000;
    private const int FramesBloque = 1600;
    private const int SegundosBuffer = 10;
    private static GestorVozMultiplataforma propietarioMicrofono;
    private ComandosVoz comandos;
    private Action<ComandoReconocido> alReconocer;
    private ConcurrentQueue<Deteccion> pendientes;
    private AudioClip clipMicrofono;
    private string dispositivo;
    private float[] muestras;
    private short[] pcmWindows;
    private int posicionAnterior;
    private long framesLeidos;
    private DateTime origenAudio;
    private double ultimaLectura, inicioMicrofono;
    private HistorialNivelVoz niveles;
    private bool activo;

#if VOZ_WINDOWS
    private KeywordRecognizer reconocedorWindows;
#elif VOZ_LINUX
    private sealed class BloqueAudio
    {
        public short[] pcm;
        public DateTime origen;
        public double finSegundos;
        public float rms;
    }

    private sealed class SesionVosk
    {
        public readonly BlockingCollection<BloqueAudio> entrada = new BlockingCollection<BloqueAudio>(30);
        public readonly CancellationTokenSource cancelacion = new CancellationTokenSource();
        public volatile bool lista;
        public volatile string error;
    }

    [Serializable] private class PalabraVosk { public double start; public double end; }
    [Serializable] private class ResultadoVosk { public string text; public PalabraVosk[] result; }
    private SesionVosk sesion;
#endif

    public void Iniciar(IEnumerable<string> palabras, Action<ComandoReconocido> callback, float ventanaNivel = 1f)
    {
        Detener();
        if (!isActiveAndEnabled) return;
        comandos = new ComandosVoz(palabras);
        if (comandos.PalabrasClave.Length == 0) return;
        alReconocer = callback;
        pendientes = new ConcurrentQueue<Deteccion>();
        niveles = new HistorialNivelVoz(ventanaNivel);
        activo = true;
        try
        {
#if VOZ_WINDOWS
            var cola = pendientes;
            reconocedorWindows = new KeywordRecognizer(comandos.PalabrasClave);
            reconocedorWindows.OnPhraseRecognized += args => {
                if (cola.Count >= 8) cola.TryDequeue(out _);
                cola.Enqueue(new Deteccion {
                    texto = args.text, inicio = args.phraseStartTime.ToUniversalTime(),
                    fin = args.phraseStartTime.ToUniversalTime() + args.phraseDuration, rms = -1
                });
            };
            reconocedorWindows.Start();
            // Si falla el medidor RMS, Windows Speech puede seguir reconociendo frases.
            IniciarMicrofono();
            Debug.Log("[Voz] Windows Speech iniciado.", this);
#elif VOZ_LINUX
            string ruta = Path.Combine(Application.streamingAssetsPath, "vosk-model-small-es");
            if (!File.Exists(Path.Combine(ruta, "am", "final.mdl")))
                throw new DirectoryNotFoundException("Falta el modelo español de Vosk en " + ruta);
            var nuevaSesion = new SesionVosk();
            var cola = pendientes;
            string gramatica = comandos.CrearGramatica();
            sesion = nuevaSesion;
            _ = Task.Run(() => ReconocerLinux(nuevaSesion, cola, ruta, gramatica, ventanaNivel));
            Debug.Log("[Voz] Cargando modelo español offline…", this);
#else
            throw new PlatformNotSupportedException("La voz está preparada para Windows y Linux.");
#endif
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Voz] No se pudo iniciar el reconocimiento: " + ex.Message, this);
            Detener();
        }
    }

    private bool IniciarMicrofono()
    {
        try
        {
            if (propietarioMicrofono != null && propietarioMicrofono != this)
                throw new InvalidOperationException("Otro proveedor de voz ya está usando el micrófono.");
            if (Microphone.devices.Length == 0) throw new InvalidOperationException("No se encontró un micrófono.");
            dispositivo = Microphone.devices[0];
            clipMicrofono = Microphone.Start(dispositivo, true, SegundosBuffer, Frecuencia);
            if (clipMicrofono == null) throw new InvalidOperationException("No se pudo abrir el micrófono.");
            propietarioMicrofono = this;
            if (clipMicrofono.frequency != Frecuencia)
                throw new InvalidOperationException("El micrófono no entregó audio a 16000 Hz.");
            muestras = new float[FramesBloque * clipMicrofono.channels];
            pcmWindows = new short[FramesBloque];
            posicionAnterior = 0;
            framesLeidos = 0;
            origenAudio = DateTime.UtcNow;
            ultimaLectura = Time.realtimeSinceStartupAsDouble;
            inicioMicrofono = ultimaLectura;
            return true;
        }
        catch (Exception ex)
        {
            LiberarMicrofono();
            Debug.LogWarning("[Voz] Captura de micrófono no disponible: " + ex.Message, this);
            return false;
        }
    }

    private void Update()
    {
        if (!activo) return;
#if VOZ_LINUX
        if (sesion != null)
        {
            if (sesion.error != null)
            {
                Debug.LogWarning("[Voz] Vosk detenido: " + sesion.error, this);
                Detener();
                return;
            }
            if (sesion.lista && clipMicrofono == null)
            {
                if (!IniciarMicrofono()) { Detener(); return; }
                Debug.Log("[Voz] Vosk escuchando español offline a 16000 Hz.", this);
            }
        }
#endif
        if (clipMicrofono != null)
        {
            try { LeerMicrofono(); }
            catch (Exception ex)
            {
                Debug.LogWarning("[Voz] Se detuvo la captura: " + ex.Message, this);
                Detener();
                return;
            }
        }
        var cola = pendientes;
        while (activo && ReferenceEquals(cola, pendientes) && cola != null && cola.TryDequeue(out Deteccion deteccion))
        {
            if (!comandos.IntentarReconocer(deteccion.texto, out string comando)) continue;
            float rms = deteccion.rms >= 0 ? deteccion.rms :
                niveles.Obtener((deteccion.fin - origenAudio).TotalSeconds);
            alReconocer?.Invoke(new ComandoReconocido {
                texto = comando, inicioUtc = deteccion.inicio, nivelVoz = rms
            });
        }
    }

    private void LeerMicrofono()
    {
        double ahora = Time.realtimeSinceStartupAsDouble;
        if (ahora - ultimaLectura >= SegundosBuffer - .5)
            throw new InvalidOperationException("El buffer de audio se desbordó. Desactiva y reactiva la voz para reiniciarla.");
        ultimaLectura = ahora;
        int posicion = Microphone.GetPosition(dispositivo);
        if (posicion < 0) throw new InvalidOperationException("Se perdió el micrófono.");
        if (ahora - inicioMicrofono > 5 &&
            (!Microphone.IsRecording(dispositivo) || (framesLeidos == 0 && posicion == 0)))
            throw new InvalidOperationException("El micrófono no entrega audio. Revisa el dispositivo y sus permisos.");
        if (framesLeidos == 0 && posicion > 0)
            origenAudio = DateTime.UtcNow.AddSeconds(-(double)posicion / Frecuencia);
        int disponibles = (posicion - posicionAnterior + clipMicrofono.samples) % clipMicrofono.samples;
        while (disponibles >= FramesBloque)
        {
            // GetData continúa desde el principio cuando el bloque cruza el final del clip.
            if (!clipMicrofono.GetData(muestras, posicionAnterior))
                throw new InvalidOperationException("No se pudieron leer las muestras del micrófono.");
            short[] pcm = pcmWindows;
#if VOZ_LINUX
            pcm = new short[FramesBloque]; // El trabajador conserva el bloque hasta consumirlo.
#endif
            float rms = AudioVoz.Convertir(muestras, FramesBloque, clipMicrofono.channels, pcm);
            framesLeidos += FramesBloque;
            double finSegundos = (double)framesLeidos / Frecuencia;
            niveles.Agregar(finSegundos, rms);
#if VOZ_LINUX
            if (sesion != null && !sesion.entrada.TryAdd(new BloqueAudio {
                pcm = pcm, origen = origenAudio, finSegundos = finSegundos, rms = rms
            })) throw new InvalidOperationException("Vosk no alcanza a procesar el audio en este equipo.");
#endif
            posicionAnterior = (posicionAnterior + FramesBloque) % clipMicrofono.samples;
            disponibles -= FramesBloque;
        }
    }

#if VOZ_LINUX
    private static void ReconocerLinux(SesionVosk sesion, ConcurrentQueue<Deteccion> salida,
        string ruta, string gramatica, float ventana)
    {
        try
        {
            Vosk.Vosk.SetLogLevel(-1);
            using (var modelo = new Vosk.Model(ruta))
            {
                if (Vosk.Model.getCPtr(modelo).Handle == IntPtr.Zero)
                    throw new InvalidOperationException("No se pudo cargar el modelo de idioma.");
                using (var reconocedor = new Vosk.VoskRecognizer(modelo, Frecuencia, gramatica))
                {
                    if (Vosk.VoskRecognizer.getCPtr(reconocedor).Handle == IntPtr.Zero)
                        throw new InvalidOperationException("No se pudo crear el reconocedor Vosk.");
                    reconocedor.SetWords(true);
                    var historial = new HistorialNivelVoz(ventana);
                    sesion.lista = true;
                    foreach (BloqueAudio bloque in sesion.entrada.GetConsumingEnumerable(sesion.cancelacion.Token))
                    {
                        historial.Agregar(bloque.finSegundos, bloque.rms);
                        if (!reconocedor.AcceptWaveform(bloque.pcm, bloque.pcm.Length)) continue;
                        var resultado = JsonUtility.FromJson<ResultadoVosk>(reconocedor.Result());
                        if (resultado == null || string.IsNullOrWhiteSpace(resultado.text) ||
                            resultado.result == null || resultado.result.Length == 0) continue;
                        double inicio = resultado.result[0].start;
                        double fin = resultado.result[resultado.result.Length - 1].end;
                        if (salida.Count >= 8) salida.TryDequeue(out _);
                        salida.Enqueue(new Deteccion {
                            texto = resultado.text, inicio = bloque.origen.AddSeconds(inicio),
                            fin = bloque.origen.AddSeconds(fin), rms = historial.Obtener(fin)
                        });
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { sesion.error = ex.Message; }
        finally
        {
            sesion.entrada.Dispose();
            sesion.cancelacion.Dispose();
        }
    }
#endif

    private void LiberarMicrofono()
    {
        if (propietarioMicrofono == this)
        {
            if (!string.IsNullOrEmpty(dispositivo)) Microphone.End(dispositivo);
            propietarioMicrofono = null;
        }
        if (clipMicrofono != null) Destroy(clipMicrofono);
        clipMicrofono = null;
        dispositivo = null;
        muestras = null;
        pcmWindows = null;
    }

    public void Detener()
    {
        activo = false;
        alReconocer = null;
#if VOZ_WINDOWS
        if (reconocedorWindows != null)
        {
            var anterior = reconocedorWindows;
            reconocedorWindows = null;
            try
            {
                try { if (anterior.IsRunning) anterior.Stop(); }
                finally { anterior.Dispose(); }
            }
            catch (Exception ex) { Debug.LogWarning("[Voz] Error al cerrar Windows Speech: " + ex.Message, this); }
        }
#elif VOZ_LINUX
        if (sesion != null)
        {
            try { sesion.cancelacion.Cancel(); }
            catch (ObjectDisposedException) { } // El trabajador ya terminó por un error.
            sesion = null; // El trabajador libera reconocedor, modelo y cola al salir.
        }
#endif
        LiberarMicrofono();
        pendientes = null;
    }

    private void OnDisable() => Detener();
    private void OnDestroy() => Detener();
}
