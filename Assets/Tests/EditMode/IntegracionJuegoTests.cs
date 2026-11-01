using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Prueba los componentes del assembly predeterminado sin iniciar Relay ni abrir el micrófono.</summary>
public class IntegracionJuegoTests
{
    private readonly List<GameObject> objetos = new List<GameObject>();
    private Type tipoRelay;
    private FieldInfo salidaEditor;
    private object salidaAnterior;
    private const BindingFlags Privado = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void Preparar()
    {
        tipoRelay = Type.GetType("GestorRedRelay, Assembly-CSharp", true);
        salidaEditor = tipoRelay.GetField("saliendoDelEditor", BindingFlags.Static | BindingFlags.NonPublic);
        salidaAnterior = salidaEditor.GetValue(null);
        salidaEditor.SetValue(null, false);
    }

    [TearDown]
    public void Limpiar()
    {
        foreach (GameObject obj in objetos) UnityEngine.Object.DestroyImmediate(obj);
        objetos.Clear();
        salidaEditor.SetValue(null, salidaAnterior);
    }

    private Component Crear(string nombre)
    {
        var obj = new GameObject("Prueba " + nombre);
        obj.SetActive(false); // No ejecutar Start/OnEnable de los consumidores reales.
        objetos.Add(obj);
        return obj.AddComponent(Type.GetType(nombre + ", Assembly-CSharp", true));
    }

    private static void Desconexion(Component relay)
    {
        relay.GetType().GetMethod("AlDesconectar", Privado).Invoke(relay, new object[] { (ulong)1 });
    }

#if UNITY_EDITOR_WIN
    [TestCase("letra te", "Low", true)]
    [TestCase("LETRA TÉ", "Low", true)]
    [TestCase("letra ti", "Low", true)]
    [TestCase("letra t", "Low", true)]
    [TestCase("letra de", "Low", false)]
    [TestCase("tijera", "Low", false)]
    [TestCase("te", "Low", false)]
    [TestCase("no letra te", "Low", false)]
    [TestCase("letra te", "Rejected", false)]
    [TestCase("tijera", "Medium", true)]
    [TestCase("letra de", "High", true)]
    public void WindowsSoloRelajaLaConfianzaDeLasFrasesCortasDeT(string frase, string confianza, bool esperado)
    {
        var tipo = Type.GetType("Pokimun.Voz.ReconocedorWindows, Assembly-CSharp", true);
        var metodo = tipo.GetMethod("AceptarFrase", BindingFlags.NonPublic | BindingFlags.Static);
        var nivel = Enum.Parse(metodo.GetParameters()[1].ParameterType, confianza);
        Assert.That(metodo.Invoke(null, new object[] { frase, nivel }), Is.EqualTo(esperado));
    }
#endif

    [TestCase(0f, 0f, false, false)]
    [TestCase(1f, 1f, false, true)]
    [TestCase(.15f, .149f, false, true)]
    [TestCase(.15f, .15f, false, false)]
    [TestCase(.15f, .9f, false, false)]
    [TestCase(0f, .9f, true, true)]
    public void CriticoManualRespetaProbabilidadYForzadoDePruebas(float probabilidad, float muestra, bool shift, bool esperado)
    {
        var reglas = Type.GetType("ReglasCombate, Assembly-CSharp", true);
        Assert.That(reglas.GetMethod("EsCriticoManual").Invoke(null, new object[] { probabilidad, muestra, shift }), Is.EqualTo(esperado));
    }

    [Test]
    public void ControlesMovilesNoOfrecenCriticoYPausaQuedaAbajoSinSuperponerse()
    {
        Component controles = Crear("ControlesAndroid");
        controles.GetType().GetMethod("Awake", Privado).Invoke(controles, null);
        var canvas = controles.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rectCanvas = (RectTransform)canvas.transform;
        rectCanvas.sizeDelta = new Vector2(1280, 720);
        var zona = (RectTransform)canvas.transform.Find("Zona segura");
        zona.anchorMin = Vector2.zero;
        zona.anchorMax = Vector2.one;
        Canvas.ForceUpdateCanvases();
        Assert.That(zona.Find("Crítico"), Is.Null);
        var pausa = RectTransformUtility.CalculateRelativeRectTransformBounds(rectCanvas, zona.Find("Pausa"));
        Assert.That(pausa.max.y, Is.LessThan(0), "La pausa debe quedar lejos de la barra de vida superior.");
        foreach (string nombre in new[] { "Escudo", "Cura", "Ataque" })
        {
            var boton = RectTransformUtility.CalculateRelativeRectTransformBounds(rectCanvas, zona.Find(nombre));
            Assert.That(boton.Intersects(pausa), Is.False, nombre);
        }
    }

    [Test]
    public void MenusMovilesReservanAlturaCompletaEnPantallasAnchas()
    {
        var objeto = new GameObject("Canvas prueba", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        objetos.Add(objeto);
        objeto.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = objeto.GetComponent<UnityEngine.UI.CanvasScaler>();
        escala.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
        Canvas.ForceUpdateCanvases();
        Type.GetType("ControlesAndroid, Assembly-CSharp", true).GetMethod("ConfigurarEscala").Invoke(null, new object[] { escala });
        Assert.That(escala.screenMatchMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand));
        Assert.That(escala.referenceResolution.y, Is.EqualTo(1200));
        // Título de instrucciones: centro y=426, altura=300; queda dentro de los 600 disponibles.
        Assert.That(426 + 300 / 2f, Is.LessThan(escala.referenceResolution.y / 2));
    }

    [Test]
    public void SalidaLocalCierraRelayAntesDeDestruirElGestor()
    {
        Component relay = Crear("GestorRedRelay");
        Component gestor = Crear("GestorNivel");
        gestor.GetType().GetField("relay", Privado).SetValue(gestor, relay);
        int avisos = 0;
        tipoRelay.GetEvent("OnConexionPerdida").AddEventHandler(relay, new Action<string>(_ => avisos++));

        gestor.GetType().GetMethod("PrepararSalidaDelJuego").Invoke(gestor, null);
        Desconexion(relay);

        Assert.That(tipoRelay.GetField("cerrando", Privado).GetValue(relay), Is.True);
        Assert.That(gestor.GetType().GetProperty("EnBatalla").GetValue(gestor), Is.False);
        Assert.That(avisos, Is.Zero);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void DesconexionRemotaSigueNotificandoAlJugadorQuePermanece()
    {
        Component relay = Crear("GestorRedRelay");
        string aviso = null;
        tipoRelay.GetEvent("OnConexionPerdida").AddEventHandler(relay, new Action<string>(texto => aviso = texto));
        LogAssert.Expect(LogType.Warning, "[Red] El otro jugador se ha desconectado.");

        Desconexion(relay);

        Assert.That(aviso, Is.EqualTo("El otro jugador se ha desconectado."));
    }

    [Test]
    public void DetenerPlayModeNoSeInterpretaComoDesconexionRemotaLocal()
    {
        Component relay = Crear("GestorRedRelay");
        int avisos = 0;
        tipoRelay.GetEvent("OnConexionPerdida").AddEventHandler(relay, new Action<string>(_ => avisos++));
        tipoRelay.GetMethod("AlCambiarModoEditor", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { UnityEditor.PlayModeStateChange.ExitingPlayMode });

        Desconexion(relay);

        Assert.That(avisos, Is.Zero);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void NuevaSesionDelEditorRestableceLasNotificacionesRemotas()
    {
        salidaEditor.SetValue(null, true);
        tipoRelay.GetMethod("PrepararCicloEditor", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Assert.That(salidaEditor.GetValue(null), Is.False);
    }

    [Test]
    public void LinuxTieneUmbralIndependienteYWindowsConservaElAnterior()
    {
        Component detector = Crear("DetectorCommandControl");
        Component voz = detector.gameObject.AddComponent(Type.GetType("GestorVozMultiplataforma, Assembly-CSharp", true));
        detector.GetType().GetField("voz", Privado).SetValue(detector, voz);
        float windows = (float)detector.GetType().GetField("umbralGritoRms", Privado).GetValue(detector);
        float linux = (float)detector.GetType().GetField("umbralGritoRmsLinux", Privado).GetValue(detector);
        float actual = (float)detector.GetType().GetProperty("UmbralGritoActual", Privado).GetValue(detector);
        bool usaVosk = (bool)voz.GetType().GetProperty("UsaVosk").GetValue(voz);

        Assert.That(windows, Is.EqualTo(.12f));
        Assert.That(linux, Is.EqualTo(.24f));
        Assert.That(actual, Is.EqualTo(usaVosk ? linux : windows));
        Assert.That(linux, Is.GreaterThan(windows));
    }

    [Test]
    public void AndroidIncluyeVoskArm64SinCargarloEnWindowsNiLinux()
    {
        var biblioteca = (UnityEditor.PluginImporter)UnityEditor.AssetImporter.GetAtPath(
            "Assets/Plugins/Android/arm64-v8a/libvosk.so");
        Assert.That(biblioteca, Is.Not.Null);
        Assert.That(biblioteca.GetCompatibleWithAnyPlatform(), Is.False);
        Assert.That(biblioteca.GetCompatibleWithEditor(), Is.False);
        Assert.That(biblioteca.GetCompatibleWithPlatform(UnityEditor.BuildTarget.Android), Is.True);
        Assert.That(biblioteca.GetPlatformData(UnityEditor.BuildTarget.Android, "CPU"), Is.EqualTo("ARM64"));
        Assert.That(biblioteca.GetCompatibleWithPlatform(UnityEditor.BuildTarget.StandaloneWindows64), Is.False);
        Assert.That(biblioteca.GetCompatibleWithPlatform(UnityEditor.BuildTarget.StandaloneLinux64), Is.False);
    }

    [Test]
    public void AndroidPuedeCalibrarseSinModificarWindowsNiLinux()
    {
        Component detector = Crear("DetectorCommandControl");
        detector.GetType().GetField("umbralGritoRmsAndroid", Privado).SetValue(detector, .35f);
        Assert.That(detector.GetType().GetField("umbralGritoRms", Privado).GetValue(detector), Is.EqualTo(.12f));
        Assert.That(detector.GetType().GetField("umbralGritoRmsLinux", Privado).GetValue(detector), Is.EqualTo(.24f));
    }

    private sealed class PreparacionPendiente : System.Collections.IEnumerator, IDisposable
    {
        public int cierres;
        public object Current => null;
        public bool MoveNext() => true;
        public void Reset() => throw new NotSupportedException();
        public void Dispose() { cierres++; }
    }

    [Test]
    public void DetenerVozLiberaUnaPreparacionPendienteUnaSolaVez()
    {
        Component voz = Crear("GestorVozMultiplataforma");
        var pendiente = new PreparacionPendiente();
        voz.GetType().GetField("pasosPreparacion", Privado).SetValue(voz, pendiente);
        voz.GetType().GetMethod("Detener").Invoke(voz, null);
        voz.GetType().GetMethod("Detener").Invoke(voz, null);
        Assert.That(pendiente.cierres, Is.EqualTo(1));
        Assert.That(voz.GetType().GetField("pasosPreparacion", Privado).GetValue(voz), Is.Null);
    }

    [Serializable] private class IndiceModelo { public ArchivoModelo[] archivos; }
    [Serializable] private class ArchivoModelo { public string ruta; public long bytes; public string sha256; }

    [Test]
    public void IndiceAndroidContieneTodosLosArchivosDelModeloConSuFirma()
    {
        const string carpeta = "Assets/StreamingAssets/vosk-model-small-es";
        var indice = JsonUtility.FromJson<IndiceModelo>(System.IO.File.ReadAllText(carpeta + "/archivos.json"));
        var rutas = new HashSet<string>();
        foreach (var archivo in indice.archivos)
        {
            Assert.That(rutas.Add(archivo.ruta), Is.True, "Ruta repetida: " + archivo.ruta);
            string ruta = System.IO.Path.Combine(carpeta, archivo.ruta);
            Assert.That(new System.IO.FileInfo(ruta).Length, Is.EqualTo(archivo.bytes), archivo.ruta);
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var datos = System.IO.File.OpenRead(ruta))
                Assert.That(BitConverter.ToString(sha.ComputeHash(datos)).Replace("-", "").ToLowerInvariant(),
                    Is.EqualTo(archivo.sha256), archivo.ruta);
        }
        foreach (string ruta in System.IO.Directory.GetFiles(carpeta, "*", System.IO.SearchOption.AllDirectories))
        {
            if (ruta.EndsWith(".meta") || ruta.EndsWith("archivos.json")) continue;
            Assert.That(rutas.Contains(ruta.Substring(carpeta.Length + 1).Replace('\\', '/')), Is.True, ruta);
        }
        Assert.That(rutas.Contains("am/final.mdl"), Is.True);
    }

    [TestCase("Standing 2H Magic Attack 04")]
    [TestCase("Standing 2H Magic Area Attack 02")]
    [TestCase("Sword And Shield Power Up")]
    public void HydrosConservaLasOrientacionesOriginalesDeReposoYCombate(string accion)
    {
        const string carpeta = "Assets/ModelosDePokimun/Hydros/Ender_Hydros@";
        var idle = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(carpeta + "Fight Idle.fbx");
        var ataque = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(carpeta + accion + ".fbx");
        Assert.That(idle.clipAnimations, Has.Length.EqualTo(1));
        Assert.That(ataque.clipAnimations, Has.Length.EqualTo(1));
        // La prueba visual del usuario confirma estas configuraciones distintas como correctas.
        Assert.That(idle.clipAnimations[0].keepOriginalOrientation, Is.True);
        Assert.That(ataque.clipAnimations[0].keepOriginalOrientation, Is.False);
        Assert.That(ataque.clipAnimations[0].rotationOffset, Is.EqualTo(idle.clipAnimations[0].rotationOffset));
        Assert.That(ataque.clipAnimations[0].lockRootRotation, Is.EqualTo(idle.clipAnimations[0].lockRootRotation));
    }
}
