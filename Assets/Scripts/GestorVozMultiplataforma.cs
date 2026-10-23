using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using Pokimun.Voz;
using UnityEngine;
/// <summary>Proveedor de voz para menú y combate. Solo despacha órdenes desde el hilo principal.</summary>
[DisallowMultipleComponent]
public class GestorVozMultiplataforma : MonoBehaviour
{
    public bool UsaVosk => PlataformaVoz.UsaVosk;
    public string NombreProveedor => Application.platform == RuntimePlatform.Android ? "Vosk/Android" :
        UsaVosk ? "Vosk/Linux" : "Windows Speech";

    public struct ComandoReconocido
    {
        public string texto;
        public DateTime inicioUtc;
        public float nivelVoz;
    }

    private const int Frecuencia = 16000;
    private const int FramesBloque = 1600;
    private const int SegundosBuffer = 10;
    private static GestorVozMultiplataforma propietarioMicrofono;
    private ComandosVoz comandos;
    private Action<ComandoReconocido> alReconocer;
    private ConcurrentQueue<ResultadoVoz> pendientes;
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

    private IReconocedorVoz reconocedor;
    private Coroutine preparacion;
    private IEnumerator pasosPreparacion;
    private bool pausadoAndroid;
    private string[] palabrasReanudar;
    private Action<ComandoReconocido> callbackReanudar;
    private float ventanaReanudar;
    private float ventanaActual;

    public void Iniciar(IEnumerable<string> palabras, Action<ComandoReconocido> callback, float ventanaNivel = 1f)
    {
        Detener();
        if (!isActiveAndEnabled) return;
        comandos = new ComandosVoz(palabras);
        if (comandos.PalabrasClave.Length == 0) return;
        alReconocer = callback;
        pendientes = new ConcurrentQueue<ResultadoVoz>();
        niveles = new HistorialNivelVoz(ventanaNivel);
        activo = true;
        ventanaActual = ventanaNivel;
        preparacion = StartCoroutine(PrepararReconocedor(ventanaNivel));
    }

    private IEnumerator PrepararReconocedor(float ventana)
    {
        var pasos = PreparacionModeloVoz.Preparar(ruta => {
            reconocedor = PlataformaVoz.Crear(comandos.PalabrasClave, comandos.CrearGramatica(), ruta, pendientes, ventana);
            Debug.Log("[Voz] Iniciando " + NombreProveedor + "…", this);
            // Windows puede reconocer incluso si no está disponible el medidor RMS.
            if (!UsaVosk) IniciarMicrofono();
        });
        pasosPreparacion = pasos;
        using (pasos as IDisposable)
        {
            while (true)
            {
                bool continuar;
                try { continuar = pasos.MoveNext(); }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Voz] No se pudo iniciar el reconocimiento: " + ex.Message, this);
                    preparacion = null;
                    Detener();
                    yield break;
                }
                if (!continuar) break;
                yield return pasos.Current;
            }
        }
        if (ReferenceEquals(pasosPreparacion, pasos)) pasosPreparacion = null;
        preparacion = null;
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
        if (pausadoAndroid) return;
        if (reconocedor != null)
        {
            if (reconocedor.Error != null)
            {
                Debug.LogWarning("[Voz] Reconocimiento detenido: " + reconocedor.Error, this);
                Detener();
                return;
            }
            if (UsaVosk && reconocedor.Listo && clipMicrofono == null)
            {
                if (!IniciarMicrofono()) { Detener(); return; }
                Debug.Log("[Voz] " + NombreProveedor + " escuchando español offline a 16000 Hz.", this);
            }
        }
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
        while (activo && ReferenceEquals(cola, pendientes) && cola != null && cola.TryDequeue(out ResultadoVoz deteccion))
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
            if (UsaVosk) pcm = new short[FramesBloque]; // El trabajador conserva el bloque.
            float rms = AudioVoz.Convertir(muestras, FramesBloque, clipMicrofono.channels, pcm);
            framesLeidos += FramesBloque;
            double finSegundos = (double)framesLeidos / Frecuencia;
            niveles.Agregar(finSegundos, rms);
            reconocedor?.Enviar(pcm, origenAudio, finSegundos, rms);
            posicionAnterior = (posicionAnterior + FramesBloque) % clipMicrofono.samples;
            disponibles -= FramesBloque;
        }
    }

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
        palabrasReanudar = null;
        callbackReanudar = null;
        if (preparacion != null) { StopCoroutine(preparacion); preparacion = null; }
        // Cerrar también la extracción pendiente al cambiar de escena, aunque Unity detenga la coroutine.
        var pasosAnteriores = pasosPreparacion;
        pasosPreparacion = null;
        try { (pasosAnteriores as IDisposable)?.Dispose(); }
        catch (Exception ex) { Debug.LogWarning("[Voz] Error al cerrar preparación: " + ex.Message, this); }
        var anterior = reconocedor;
        reconocedor = null;
        try { anterior?.Dispose(); }
        catch (Exception ex) { Debug.LogWarning("[Voz] Error al cerrar reconocimiento: " + ex.Message, this); }
        LiberarMicrofono();
        pendientes = null;
    }

    private void OnApplicationPause(bool pausa)
    {
        if (Application.platform != RuntimePlatform.Android) return;
        pausadoAndroid = pausa;
        if (pausa && clipMicrofono != null && activo)
        {
            var palabras = comandos.PalabrasClave;
            var callback = alReconocer;
            float ventana = ventanaActual;
            Detener();
            palabrasReanudar = palabras;
            callbackReanudar = callback;
            ventanaReanudar = ventana;
        }
        else if (!pausa && palabrasReanudar != null && isActiveAndEnabled)
        {
            var palabras = palabrasReanudar;
            var callback = callbackReanudar;
            float ventana = ventanaReanudar;
            Iniciar(palabras, callback, ventana);
        }
    }

    private void OnDisable() => Detener();
    private void OnDestroy() => Detener();
}
